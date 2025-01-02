using System;
using System.Collections.Generic;
using System.Threading;
using Data;
using UnityEngine;
using TerrainData = Data.TerrainData;

[Serializable]
public enum DrawMode {NoiseMap, FalloffMap, Mesh};
public class MapGenerator : MonoBehaviour {

	[Tooltip("绘制模式")] public DrawMode drawMode;
	
	[Tooltip("地形数据")] public TerrainData terrainData;
	[Tooltip("噪音数据")] public NoiseData noiseData;
	[Tooltip("纹理数据")] public TextureData textureData;
	
	[Tooltip("地形材质")] public Material terrainMaterial;

	[Tooltip("块大小索引"), Range(0, MeshGenerator.numSupportedChunkSizes - 1)]
	public int chunkSizeIndex;

	[Tooltip("平面着色细节层次索引"), Range(0, MeshGenerator.numSupportedFlatshadedChunkSizes - 1)]
	public int flatShadedChunkSizeIndex;

	[Tooltip("细节层次索引"), Range(0, MeshGenerator.numSupportedLODs - 1)]
	public int editorPreviewLOD;

	[Tooltip("是否自动更新")] public bool autoUpdate;
	[Tooltip("衰减贴图")] float[,] falloffMap;
	
	[Tooltip("地形地图线程信息队列")] Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	[Tooltip("地形网格线程信息队列")] Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	private void Awake()
	{
		textureData.ApplyToMaterial (terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);//更新地形材质
	}

	void OnValuesUpdated() {
		if (!Application.isPlaying) {//如果不是在游戏运行模式的话，绘制一张新地图
			DrawMapInEditor();
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial (terrainMaterial);
	}
	
	/// <summary>
	/// 地图块边长(公共静态整数)
	/// 为了补偿网格边界计算加上2时会变为241，再-1会变成240，
	/// 但是240对于平面着色会有点多，所以平面着色时使用96(不能被10整除)
	/// </summary>
	public int mapChunkSize {//根据索引返回地图块边长
		get {
			if (terrainData.useFlatShading) {
				return MeshGenerator.supportedFlatshadedChunkSizes[flatShadedChunkSizeIndex] -1;
			} else {
				return MeshGenerator.supportedChunkSizes[chunkSizeIndex] -1;
			}
		}
	}

	/// <summary>
	/// 绘制地形方法
	/// </summary>
	public void DrawMapInEditor() {
		textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);//更新地形材质
		MapData mapData = GenerateMapData (Vector2.zero);//声明地形数据
		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD,terrainData.useFlatShading));
		} else if (drawMode == DrawMode.FalloffMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
		}
	}

	/// <summary>
	/// 请求地形地图数据
	/// </summary>
	/// <param name="centre"></param>
	/// <param name="callback">回调函数</param>
	public void RequestMapData(Vector2 centre, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread (centre, callback);
		};
		//创建线程启动地形数据线程委托
		new Thread (threadStart).Start ();
	}

	/// <summary>
	/// 地形地图数据线程
	/// </summary>
	/// <param name="centre"></param>
	/// <param name="callback">回调函数</param>
	void MapDataThread(Vector2 centre, Action<MapData> callback) {
		MapData mapData = GenerateMapData(centre);//生成地形数据
		lock (mapDataThreadInfoQueue) {//资源锁
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	/// <summary>
	/// 请求地形网格数据
	/// </summary>
	/// <param name="mapData"></param>
	/// <param name="lod">细节层次</param>
	/// <param name="callback"></param>
	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadStart).Start ();
	}

	/// <summary>
	/// 地形网格数据线程
	/// </summary>
	/// <param name="mapData"></param>
	/// <param name="lod">细节层次</param>
	/// <param name="callback"></param>
	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod,terrainData.useFlatShading);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}
	
	void Update() {
		if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {//遍历地图线程队列的所有元素，调用回调函数
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}
	
	/// <summary>
	/// 生成地形数据
	/// </summary>
	/// <returns></returns>
	MapData GenerateMapData(Vector2 centre)
	{
		float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed,
			noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity,
			centre + noiseData.offset, noiseData.normalizeMode);//+2是为了补偿边界用于计算边界法线

		if (terrainData.useFalloff){ //如果使用衰减地图，则让噪音图减去衰减贴
			falloffMap ??= FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);//生成与地图大小相匹配的衰减地图
			
			for (int y = 0; y < mapChunkSize + 2; y++) {
				for (int x = 0; x < mapChunkSize+2; x++) {
					if (terrainData.useFalloff) {
						noiseMap [x, y] = Mathf.Clamp01 (noiseMap [x, y] - falloffMap [x, y]);
					}
				
				}
			}
		}

		return new MapData(noiseMap);
	}

	//使用验证方法控制值，该方法会在脚本变量值在inspector中改变时自动调用
	void OnValidate() {
		//每次某个值更新时先去除原先的订阅，再加上新的订阅，避免重复调用
		if (terrainData != null) {
			terrainData.onValuesUpdated -= OnValuesUpdated;
			terrainData.onValuesUpdated += OnValuesUpdated;
		}
		if (noiseData != null) {
			noiseData.onValuesUpdated -= OnValuesUpdated;
			noiseData.onValuesUpdated += OnValuesUpdated;
		}
		if (textureData != null) {
			textureData.onValuesUpdated -= OnTextureValuesUpdated;
			textureData.onValuesUpdated += OnTextureValuesUpdated;
		}

	}
	
	/// <summary>
	/// 地图线程信息，设置为泛型一边同时处理地图数据与网格数据
	/// </summary>
	/// <typeparam name="T"></typeparam>
	struct MapThreadInfo<T> {
		public readonly Action<T> callback;//回调
		public readonly T parameter;//参数

		public MapThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
		
	}
}

/// <summary>
/// 地图数据
/// </summary>
public struct MapData {
	public readonly float[,] heightMap;//高度图

	public MapData (float[,] heightMap)
	{
		this.heightMap = heightMap;
	}
}