using UnityEngine;

public static class Noise {

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale) {
		var noiseMap = new float[mapWidth,mapHeight];//声明一个地图大小的噪音数组
		//判断比例大小避免除零错误
		if (scale <= 0) {
			scale = 0.0001f;
		}
		//设置噪声地图
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				var sampleX = x / scale;
				var sampleY = y / scale;
				//调用柏林函数的数学库
				var perlinValue = Mathf.PerlinNoise (sampleX, sampleY);
				noiseMap [x, y] = perlinValue;
			}
		}
		return noiseMap;
	}

}
