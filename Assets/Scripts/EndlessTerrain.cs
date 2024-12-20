using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 无线地形生成
/// </summary>
public class EndlessTerrain : MonoBehaviour
{
	const float scale = 5f;
	
	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	[Tooltip("细节层次信息")] public LODInfo[] detailLevels;
	[Tooltip("观察者所能看到的最远视距")] public static float maxViewDst;
	
	[Tooltip("观察者")] public Transform viewer;
	[Tooltip("地形材质")] public Material mapMaterial;
	
	[Tooltip("观察者位置")] public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	
	[Tooltip("地形生成器")] static MapGenerator mapGenerator;
	[Tooltip("地图块大小")] int chunkSize;
	[Tooltip("可见地图块个数")] int chunksVisibleInViewDst;
	
	[Tooltip("坐标与地形块的字典")] private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	[Header("动态更新")]
	[SerializeField, Tooltip("上次更新时可见的地形块列表")] static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();//查找MapGenerator
		
		maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		chunkSize = MapGenerator.mapChunkSize - 1;//实际的网格大小是MapGenerator的mapChunkSize-1
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);//可见地图块数量等于视距能被地图块大小整除的次数
		
		UpdateVisibleChunks ();
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / scale;

		//只有当观察者移动超过一个阈值距离之后才更新地块(平方计算)
		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}
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
				} else {//不包含就实例化一个新的地形块添加进字典
					terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
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
		Vector2 position;//位置
		Bounds bounds;//网格边界

		MeshRenderer meshRenderer;//网格渲染器
		MeshFilter meshFilter;//网格过滤器
		
		LODInfo[] detailLevels;//细节层次设置
		LODMesh[] lodMeshes;//细节层次网格

		MapData mapData;//地图数据
		bool mapDataReceived;//是否收到了地图数据
		int previousLODIndex = -1;//之前的细节层次索引值

		/// <summary>
		/// 实例化地形块
		/// </summary>
		/// <param name="coord">块坐标</param>
		/// <param name="size">块大小</param>
		/// <param name="detailLevels">细节层次信息</param>
		/// <param name="parent">父物体</param>
		/// <param name="material"></param>
		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
			this.detailLevels = detailLevels;
			
			position = coord * size;//位置等于块坐标乘以块大小
			bounds = new Bounds(position,Vector2.one * size);//设置地形块边界用于距离计算
			Vector3 positionV3 = new Vector3(position.x,0,position.y);//实例化块的位置
			
			//创建地形块，添加对应组件
			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;
			
			meshObject.transform.position = positionV3 * scale;//设置网格对象的位置
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * scale;
			SetVisible(false);
			
			//创建细节层次网格数组
			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++) {//为每个细节层次都创建一个网格
				lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
			}

			mapGenerator.RequestMapData(position,OnMapDataReceived);//请求生成地图数据
		}
		
		/// <summary>
		/// 接收地图数据
		/// </summary>
		/// <param name="mapData"></param>
		void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;//设置地图数据
			mapDataReceived = true;

			//创建并应用纹理
			Texture2D texture = TextureGenerator.TextureFromColourMap (mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
			meshRenderer.material.mainTexture = texture;

			UpdateTerrainChunk ();
		}

		/// <summary>
		/// 计算边界上最接近观察者的点到观察者的距离，更新是否可视化
		/// </summary>
		public void UpdateTerrainChunk() {
			if (mapDataReceived) {//如果收到了地图数据
				float viewerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

				if (visible) {//如果可见，设置细节层次索引
					int lodIndex = 0;
					for (int i = 0; i < detailLevels.Length - 1; i++) {
						if (viewerDstFromNearestEdge > detailLevels [i].visibleDstThreshold) {
							lodIndex = i + 1;
						} else {
							break;
						}
					}
					//如果细节层次索引和之前的不一样就重新加载网格
					if (lodIndex != previousLODIndex) {
						LODMesh lodMesh = lodMeshes [lodIndex];
						if (lodMesh.hasMesh) {
							previousLODIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh;
						} else if (!lodMesh.hasRequestedMesh) {//没有数据就重新请求
							lodMesh.RequestMesh (mapData);
						}
					}
					
					terrainChunksVisibleLastUpdate.Add (this);//将自己添加到上次可见地形块列表中
				}
				SetVisible (visible);
			}
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
	
	/// <summary>
	/// 细节层次网格类
	/// </summary>
	class LODMesh {
		public Mesh mesh;//网格对象
		public bool hasRequestedMesh;//是否已经请求了网格
		public bool hasMesh;//是否收到了网格
		int lod;//当前网格的细节层次2
		Action updateCallback;//回调函数用于更新地形块

		public LODMesh(int lod, System.Action updateCallback) {
			this.lod = lod;
			this.updateCallback = updateCallback;
		}

		/// <summary>
		/// 收到了网格数据后的回调函数
		/// </summary>
		/// <param name="meshData"></param>
		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh ();//创建网格
			hasMesh = true;//设置收到了数据
			updateCallback();//执行回调函数
		}

		/// <summary>
		/// 请求网格
		/// </summary>
		/// <param name="mapData"></param>
		public void RequestMesh(MapData mapData) {
			hasRequestedMesh = true;//已经请求了网格
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);//调用生成器的请求方法
		}

	}

	[Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThreshold;
	}
}
