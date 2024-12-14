using UnityEngine;

/// <summary>
/// 纹理生成器
/// </summary>
public static class TextureGenerator {
	// 绘制颜色地图返回贴图
	public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D (width, height);
		texture.filterMode = FilterMode.Point;//纹理过滤模式设置为点模式，防止模糊
		texture.wrapMode = TextureWrapMode.Clamp;//纹理的包裹模式设置为钳制模式防止纹理堆叠
		texture.SetPixels (colourMap);
		texture.Apply ();
		return texture;
	}

	// 绘制噪声地图方法
	public static Texture2D TextureFromHeightMap(float[,] heightMap) {
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);
		//使用黑白颜色插值来绘制纹理像素
		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, heightMap [x, y]);
			}
		}

		return TextureFromColourMap (colourMap, width, height);
	}

}
