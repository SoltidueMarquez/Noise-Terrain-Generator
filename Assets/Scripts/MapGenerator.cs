using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[Serializable]
public enum DrawMode {NoiseMap, ColourMap, Mesh, FalloffMap};
public class MapGenerator : MonoBehaviour {
	[Tooltip("绘制模式")] public DrawMode drawMode;
	[Tooltip("噪声归一化模式")] public Noise.NormalizeMode normalizeMode;
	[Tooltip("地形方块边长")]public const int mapChunkSize = 241;
	[Range(0, 6), Tooltip("细节层次")] public int editorPreviewLOD;
	
	[Header("噪音设置")]
	[Tooltip("噪声缩放指数")] public float noiseScale;
	[Tooltip("八度音阶的数量")] public int octaves;
	[Range(0, 1), Tooltip("持久性值")] public float persistance;
	[Tooltip("间隙度值")] public float lacunarity;

	[Tooltip("种子")] public int seed;//如果 seed 是固定的，则每次运行程序时，生成的随机数序列都是一样的
	[Tooltip("向量偏移量")] public Vector2 offset;
	
	[Header("网格设置")]
	[Tooltip("网格高度乘数")] public float meshHeightMultiplier;
	[Tooltip("不同高度收乘数影响的程度")] public AnimationCurve meshHeightCurve;
	
	[Header("纹理设置")]
	[Tooltip("地形类型数组")] public TerrainType[] regions;
	
	[Header("应用设置")]
	[Tooltip("是否应用衰减贴图")] public bool useFalloff;
	[Tooltip("衰减贴图")] float[,] falloffMap;
	
	[Tooltip("是否自动更新")] public bool autoUpdate;
	
	[Tooltip("地形地图线程信息队列")] Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	[Tooltip("地形网格线程信息队列")] Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void Awake() {//生成衰减贴图
		falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize);
	}
	
	/// <summary>
	/// 绘制地形方法
	/// </summary>
	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData (Vector2.zero);//声明地形数据
		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap (mapData.colourMap, mapChunkSize, mapChunkSize));
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
	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
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
	MapData GenerateMapData(Vector2 centre) {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];//声明一维颜色地图
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				if (useFalloff) {//如果使用衰减地图，则让噪音图减去衰减贴图
					noiseMap [x, y] = Mathf.Clamp01(noiseMap [x, y] - falloffMap [x, y]);
				}
				float currentHeight = noiseMap [x, y];//获取当前地图点高度
				for (int i = 0; i < regions.Length; i++) {//遍历所有地形类型设置当前点的类型
					if (currentHeight >= regions [i].height) {//如果当前区域小于等于地形类型规定的高度就设置颜色地图
						colourMap [y * mapChunkSize + x] = regions [i].colour;
					} else {
						break;
					}
				}
			}
		}

		return new MapData (noiseMap, colourMap);
	}

	//使用验证方法控制值，该方法会在脚本变量值在inspector中改变时自动调用
	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
		
		falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize);
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
/// 地形类型结构体
/// </summary>
[System.Serializable]
public struct TerrainType {
	[Tooltip("地形名称")] public string name;
	[Tooltip("地形高度")] public float height;
	[Tooltip("对应颜色")] public Color colour;
}

/// <summary>
/// 地图数据
/// </summary>
public struct MapData {
	public readonly float[,] heightMap;//高度图
	public readonly Color[] colourMap;//颜色图

	public MapData (float[,] heightMap, Color[] colourMap)
	{
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
}