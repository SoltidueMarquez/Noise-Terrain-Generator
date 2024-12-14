using UnityEngine;

/// <summary>
/// 噪声地图可视化
/// </summary>
public class MapDisplay : MonoBehaviour {
	public Renderer textureRender;
	
	/// <summary>
	/// //将纹理应用到纹理渲染器上(游戏未运行时编辑器状态的应用)
	/// </summary>
	/// <param name="texture"></param>
	public void DrawTexture(Texture2D texture) {
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);
	}
	
}
