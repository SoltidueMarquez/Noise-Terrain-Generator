using UnityEngine;

/// <summary>
/// 噪声地图可视化
/// </summary>
public class MapDisplay : MonoBehaviour {
	public Renderer textureRender;

	/// <summary>
	/// 绘制噪声地图方法
	/// </summary>
	/// <param name="noiseMap"></param>
	public void DrawNoiseMap(float[,] noiseMap) {
		//获取宽高
		var width = noiseMap.GetLength (0);
		var height = noiseMap.GetLength (1);
		
		//使用黑白颜色插值来绘制纹理像素
		Texture2D texture = new Texture2D (width, height);
		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, noiseMap [x, y]);
			}
		}
		texture.SetPixels (colourMap);
		texture.Apply ();
		
		//将纹理应用到纹理渲染器上(游戏未运行时编辑器状态的应用)
		textureRender.sharedMaterial.mainTexture = texture;
		textureRender.transform.localScale = new Vector3 (width, 1, height);
	}
	
}
