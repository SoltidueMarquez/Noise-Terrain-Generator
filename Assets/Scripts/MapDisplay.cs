using UnityEngine;
using System.Collections;

/// <summary>
/// 噪声地图可视化
/// </summary>
public class MapDisplay : MonoBehaviour {
	[Tooltip("纹理渲染器")] public Renderer textureRender;
	[Tooltip("网格过滤器")] public MeshFilter meshFilter;
	[Tooltip("网格渲染器")] public MeshRenderer meshRenderer;
	
	/// <summary>
	/// 将纹理应用到纹理渲染器上(游戏未运行时编辑器状态的应用)
	/// </summary>
	/// <param name="texture"></param>
	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);
	}
	
	/// <summary>
	/// 绘制网格
	/// </summary>
	/// <param name="meshData"></param>
	/// <param name="texture"></param>
	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh ();
		//将网格缩放比例设置为统一缩放比例
		meshFilter.transform.localScale = Vector3.one * FindObjectOfType<MapGenerator>().meshSettings.meshScale;
	}
}
