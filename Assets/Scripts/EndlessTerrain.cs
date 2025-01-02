using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 无线地形生成
/// </summary>
public class EndlessTerrain : MonoBehaviour
{
	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
	const float colliderGenerationDistanceThreshold = 5f;//玩家距离地形块多近才会生成碰撞器
	
	[Tooltip("碰撞器细节层次信息")] public int colliderLODIndex;
	
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
	[Tooltip("上次更新时可见的地形块列表")] static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();//查找MapGenerator
		
		maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		chunkSize = mapGenerator.mapChunkSize - 1;//实际的网格大小是MapGenerator的mapChunkSize-1
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);//可见地图块数量等于视距能被地图块大小整除的次数
		
		UpdateVisibleChunks ();
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;
		//如果当前观察者的位置不等于原先的位置，则更新每个可见的地形块的碰撞体
		if (viewerPosition != viewerPositionOld) {
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh();
			}
		}
		
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
		var alreadyUpdatedChunkCoords = new HashSet<Vector2> ();//先前所有可见块的坐标
		for (int i = visibleTerrainChunks.Count-1; i >= 0; i--) {//更新所有可见的块，倒序遍历执行移除
			alreadyUpdatedChunkCoords.Add (visibleTerrainChunks[i].coord);
			visibleTerrainChunks[i].UpdateTerrainChunk();
		}
		//获取当前块的归一化坐标
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);
		//从负的最大视距开始到正的最大视距结束，遍历每一个坐标，
		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) {//如果尚未更新的块坐标不包含即将更新的块坐标
					//在尚未创建出地形块的地方创建出新的地形块
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {//如果地形块字典包含“可视块坐标”
						terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk();//更新当前块的是否可视化
					} else {//不包含就实例化一个新的地形块添加进字典
						terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, colliderLODIndex, transform, mapMaterial));
					}	
				}
			}
		}
	}

	/// <summary>
	/// 地形块对象
	/// </summary>
	[Serializable]
	public class TerrainChunk {

		public Vector2 coord;//坐标
		
		GameObject meshObject;//网格对象
		Vector2 position;//位置
		Bounds bounds;//网格边界

		MeshRenderer meshRenderer;//网格渲染器
		MeshFilter meshFilter;//网格过滤器
		MeshCollider meshCollider;//网格碰撞器
		
		LODInfo[] detailLevels;//细节层次设置
		LODMesh[] lodMeshes;//细节层次网格
		int colliderLODIndex;//碰撞器细节层次信息

		MapData mapData;//地图数据
		bool mapDataReceived;//是否收到了地图数据
		int previousLODIndex = -1;//之前的细节层次索引值
		bool hasSetCollider;//是否已经设置了碰撞器

		/// <summary>
		/// 实例化地形块
		/// </summary>
		/// <param name="coord">块坐标</param>
		/// <param name="size">块大小</param>
		/// <param name="detailLevels">细节层次信息</param>
		/// <param name="colliderLODIndex">碰撞器细节层次信息</param>
		/// <param name="parent">父物体</param>
		/// <param name="material"></param>
		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
			this.coord = coord;
			this.detailLevels = detailLevels;
			this.colliderLODIndex = colliderLODIndex;
			
			position = coord * size;//位置等于块坐标乘以块大小
			bounds = new Bounds(position,Vector2.one * size);//设置地形块边界用于距离计算
			Vector3 positionV3 = new Vector3(position.x,0,position.y);//实例化块的位置
			
			//创建地形块，添加对应组件
			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.material = material;
			
			meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;//设置网格对象的位置
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
			SetVisible(false);
			
			//创建细节层次网格数组
			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++) {//为每个细节层次都创建一个网格
				lodMeshes[i] = new LODMesh(detailLevels[i].lod);
				lodMeshes[i].updateCallback += UpdateTerrainChunk;//回调函数加上更新地形块方法
				if (i == colliderLODIndex) {//如果是碰撞器的细节层次索引
					lodMeshes[i].updateCallback += UpdateCollisionMesh;//就在回调函数上增加更新碰撞网格方法
				}
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

			UpdateTerrainChunk ();
		}

		/// <summary>
		/// 计算边界上最接近观察者的点到观察者的距离，更新是否可视化
		/// </summary>
		public void UpdateTerrainChunk() {
			if (mapDataReceived) {//如果收到了地图数据
				float viewerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
				
				bool wasVisible = IsVisible ();
				var visible = viewerDstFromNearestEdge <= maxViewDst;

				if(visible) {//如果可见，设置细节层次索引
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
						var lodMesh = lodMeshes[lodIndex];
						if (lodMesh.hasMesh) {
							previousLODIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh;
						} else if (!lodMesh.hasRequestedMesh) {//没有数据就重新请求
							lodMesh.RequestMesh (mapData);
						}
					}
				}
				
				//如果可见性发生了变化
				if (wasVisible != visible) {
					if (visible) {//如果当前可见，就添加进可见地形块列表
						visibleTerrainChunks.Add(this);
					} else {//否则就移出
						visibleTerrainChunks.Remove(this);
					}
					SetVisible(visible);
				}
			}
		}
		
		/// <summary>
		/// 生成碰撞体网格，为了性能，希望尽可能晚地创建碰撞器
		/// </summary>
		public void UpdateCollisionMesh() {
			if (!hasSetCollider) {//如果还没有设置碰撞器
				//视点到边缘的平方距离等于边界乘以视点的位置的平方距离
				float sqrDstFromViewerToEdge = bounds.SqrDistance (viewerPosition);
				//如果观察者的到边缘的平方距离小于可见阈值，请求对应细节层次的网格数据
				if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
					if (!lodMeshes [colliderLODIndex].hasRequestedMesh) {
						lodMeshes [colliderLODIndex].RequestMesh (mapData);
					}
				}
				//如果观察者的到边缘的平方距离小于生成阈值，设置碰撞器的网格等于索引细节层次的网格
				if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
					if (lodMeshes [colliderLODIndex].hasMesh) {//只有接收到了网格才操作
						meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
						hasSetCollider = true;
					}
				}
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
		public event Action updateCallback;//回调函数用于更新地形块

		public LODMesh(int lod) {
			this.lod = lod;
		}

		/// <summary>
		/// 收到了网格数据后的回调函数
		/// </summary>
		/// <param name="meshData"></param>
		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh();//创建网格
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

	/// <summary>
	/// 细节层次信息
	/// </summary>
	[Serializable]
	public struct LODInfo
	{
		[Tooltip("细节层整数"), Range(0, MeshGenerator.numSupportedLODs - 1)] public int lod;
		[Tooltip("可见距离阈值")] public float visibleDstThreshold;
		[Tooltip("可见距离阈值的平方")] public float sqrVisibleDstThreshold => visibleDstThreshold * visibleDstThreshold;
	}
}
