﻿using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 无线地形生成
/// </summary>
public class EndlessTerrain : MonoBehaviour
{
	[Tooltip("观察者所能看到的最远视距")] public const float maxViewDst = 450;
	[Tooltip("观察者")] public Transform viewer;
	[Tooltip("地形材质")] public Material mapMaterial;
	
	[Tooltip("观察者位置")] public static Vector2 viewerPosition;
	[Tooltip("地形生成器")] static MapGenerator mapGenerator;
	[Tooltip("地图块大小")] int chunkSize;
	[Tooltip("可见地图块个数")] int chunksVisibleInViewDst;
	
	[Tooltip("坐标与地形块的字典")] private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	[Header("动态更新")]
	[SerializeField, Tooltip("上次更新时可见的地形块列表")] List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();//查找MapGenerator
		chunkSize = MapGenerator.mapChunkSize - 1;//实际的网格大小是MapGenerator的mapChunkSize-1
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);//可见地图块数量等于视距能被地图块大小整除的次数
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
		UpdateVisibleChunks ();
	}
		
	/// <summary>
	/// 更新可见块，不会销毁，每次更新重新设置视野范围内所有块的可见性
	/// </summary>
	void UpdateVisibleChunks() {
		//遍历上次更新中所有可见的块，全部设置为不可见
		foreach (var t in terrainChunksVisibleLastUpdate){ t.SetVisible (false); }
		terrainChunksVisibleLastUpdate.Clear ();
		
		//获取当前块的归一化坐标
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);
		//从负的最大视距开始到正的最大视距结束，遍历每一个坐标，
		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				//在尚未创建出地形块的地方创建出新的地形块
				if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {//如果地形块字典包含“可视块坐标”
					terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();//更新当前块的是否可视化
					if (terrainChunkDictionary [viewedChunkCoord].IsVisible ()) {//如果可见则加入可见地形块列表
						terrainChunksVisibleLastUpdate.Add (terrainChunkDictionary [viewedChunkCoord]);
					}
				} else {//不包含就实例化一个新的地形块添加进字典
					terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, transform, mapMaterial));
				}

			}
		}
	}

	/// <summary>
	/// 地形块对象
	/// </summary>
	[Serializable]
	public class TerrainChunk {

		GameObject meshObject;//网格对象
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;

		/// <summary>
		/// 实例化地形块
		/// </summary>
		/// <param name="coord">块坐标</param>
		/// <param name="size">块大小</param>
		/// <param name="parent">父物体</param>
		/// <param name="material"></param>
		public TerrainChunk(Vector2 coord, int size, Transform parent, Material material) {
			position = coord * size;//位置等于块坐标乘以块大小
			bounds = new Bounds(position,Vector2.one * size);//设置地形块边界用于距离计算
			Vector3 positionV3 = new Vector3(position.x,0,position.y);//实例化块的位置
			
			//创建地形块，添加对应组件
			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;
			
			meshObject.transform.position = positionV3;//设置网格对象的位置
			meshObject.transform.parent = parent;
			SetVisible(false);
			
			mapGenerator.RequestMapData(OnMapDataReceived);//请求生成地图数据
		}
		
		/// <summary>
		/// 接收地图数据
		/// </summary>
		/// <param name="mapData"></param>
		void OnMapDataReceived(MapData mapData) {
			mapGenerator.RequestMeshData (mapData, OnMeshDataReceived);
		}

		/// <summary>
		/// 接收网格数据
		/// </summary>
		/// <param name="meshData"></param>
		void OnMeshDataReceived(MeshData meshData) {
			meshFilter.mesh = meshData.CreateMesh ();//直接创建网格过滤器
		}

		/// <summary>
		/// 计算边界上最接近观察者的点到观察者的距离，更新是否可视化
		/// </summary>
		public void UpdateTerrainChunk() {
			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));
			bool visible = viewerDstFromNearestEdge <= maxViewDst;
			SetVisible (visible);
		}

		/// <summary>
		/// 设置可视化
		/// </summary>
		/// <param name="visible"></param>
		public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
		}

		/// <summary>
		/// 返回目前是否可见
		/// </summary>
		/// <returns></returns>
		public bool IsVisible() {
			return meshObject.activeSelf;
		}

	}
}