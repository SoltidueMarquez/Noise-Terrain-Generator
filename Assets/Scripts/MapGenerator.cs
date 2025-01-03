using System;
using System.Collections.Generic;
using System.Threading;
using Data;
using UnityEngine;

[Serializable]
public enum DrawMode {NoiseMap, FalloffMap, Mesh};
public class MapGenerator : MonoBehaviour {

	[Tooltip("绘制模式")] public DrawMode drawMode;
	
	[Tooltip("网格设置")] public MeshSettings meshSettings;
	[Tooltip("噪音高度数据")] public HeightMapSettings heightMapSettings;
	[Tooltip("纹理数据")] public TextureData textureData;
	
	[Tooltip("地形材质")] public Material terrainMaterial;

	[Tooltip("细节层次索引"), Range(0, MeshSettings.numSupportedLODs - 1)]
	public int editorPreviewLOD;

	[Tooltip("是否自动更新")] public bool autoUpdate;
	[Tooltip("衰减贴图")] float[,] falloffMap;
	
	[Tooltip("地形地图线程信息队列")] Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
	[Tooltip("地形网格线程信息队列")] Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	private void Start() {
		textureData.ApplyToMaterial(terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
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
	/// 绘制地形方法
	/// </summary>
	public void DrawMapInEditor() {
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);//更新地形材质
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);//声明地形数据
		
		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.values));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values,meshSettings, editorPreviewLOD));
		} else if (drawMode == DrawMode.FalloffMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine)));
		}
	}

	/// <summary>
	/// 请求高度地图数据
	/// </summary>
	/// <param name="centre"></param>
	/// <param name="callback">回调函数</param>
	public void RequestHeightMap(Vector2 centre, Action<HeightMap> callback) {
		ThreadStart threadStart = delegate {
			HeightMapThread (centre, callback);
		};
		//创建线程启动地形数据线程委托
		new Thread (threadStart).Start ();
	}

	/// <summary>
	/// 高度地图数据线程
	/// </summary>
	/// <param name="centre"></param>
	/// <param name="callback">回调函数</param>
	void HeightMapThread(Vector2 centre, Action<HeightMap> callback) {
		var heightMap = HeightMapGenerator.GenerateHeightMap (meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, centre); //生成地形数据
		lock (heightMapThreadInfoQueue) {//资源锁
			heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap> (callback, heightMap));
		}
	}

	/// <summary>
	/// 请求地形网格数据
	/// </summary>
	/// <param name="heightMap"></param>
	/// <param name="lod">细节层次</param>
	/// <param name="callback"></param>
	public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread(heightMap, lod, callback);
		};

		new Thread (threadStart).Start();
	}

	/// <summary>
	/// 地形网格数据线程
	/// </summary>
	/// <param name="heightMap"></param>
	/// <param name="lod">细节层次</param>
	/// <param name="callback"></param>
	void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback) {
		var meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values,meshSettings, lod);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}
	
	void Update() {
		if (heightMapThreadInfoQueue.Count > 0) {
			for (int i = 0; i < heightMapThreadInfoQueue.Count; i++) {//遍历高度图线程队列的所有元素，调用回调函数
				MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue ();
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

	//使用验证方法控制值，该方法会在脚本变量值在inspector中改变时自动调用
	void OnValidate() {
		//每次某个值更新时先去除原先的订阅，再加上新的订阅，避免重复调用
		if (meshSettings != null) {
			meshSettings.onValuesUpdated -= OnValuesUpdated;
			meshSettings.onValuesUpdated += OnValuesUpdated;
		}
		if (heightMapSettings != null) {
			heightMapSettings.onValuesUpdated -= OnValuesUpdated;
			heightMapSettings.onValuesUpdated += OnValuesUpdated;
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
