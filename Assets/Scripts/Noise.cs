using System;
using UnityEngine;

public static class Noise {
	/// <summary>
	/// 归一化模式
	/// </summary>
	public enum NormalizeMode {Local, Global};

	/// <summary>
	/// 噪音生成函数，使用柏林噪音与八度音阶叠加
	/// </summary>
	/// <param name="mapWidth">地图宽度</param>
	/// <param name="mapHeight">地图高度</param>
	/// <param name="settings">噪音设置</param>
	/// <param name="sampleCentre"></param>
	/// <returns></returns>
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre) {
		float[,] noiseMap = new float[mapWidth,mapHeight];
		
		//八度音阶采样偏移
		System.Random prng = new System.Random(settings.seed);//伪随机数生成器
		//八度音阶偏移向量数组
		Vector2[] octaveOffsets = new Vector2[settings.octaves];
		
		float maxPossibleHeight = 0;
		float amplitude = 1;//设置振幅
		float frequency = 1;//设置频率，越高采样点之间的间隙越远，高度值变化更加迅速
		
		//循环生成每个八度的偏移量，对于每个八度生成一个随机数（范围在 -100000 到 100000 之间），并与 offset.x 和 offset.y 进行相加
		for (int i = 0; i < settings.octaves; i++) {
			float offsetX = prng.Next (-100000, 100000) + settings.offset.x + sampleCentre.x;
			float offsetY = prng.Next (-100000, 100000) - settings.offset.y - sampleCentre.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);//这些偏移量存储在 octaveOffsets 数组中
			
			//找到可能的最大高度值
			maxPossibleHeight += amplitude;
			amplitude *= settings.persistance;
		}
		
		//记录局部最大和最小的噪声值
		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		//设置噪声地图
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;	//当前高度
				
				//使用循环来遍历所有的八度音阶
				for (int i = 0; i < settings.octaves; i++) {
					//通过乘以频率来调整采样坐标，每个八度音阶从不同的地方采样，同时做数学处理保证改变scale时从中心改变
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;
					//调用柏林函数的数学库叠加八度音阶，将其范围改到[-1,1]
					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;
					//更新八度音阶
					amplitude *= settings.persistance;
					frequency *= settings.lacunarity;
				}
				//更新最大和最小的噪声值
				if (noiseHeight > maxLocalNoiseHeight) {
					maxLocalNoiseHeight = noiseHeight;
				} 
				if (noiseHeight < minLocalNoiseHeight) {
					minLocalNoiseHeight = noiseHeight;
				}
				noiseMap [x, y] = noiseHeight;//应用噪声高度
				
				if (settings.normalizeMode == NormalizeMode.Global) {
					float normalizedHeight = (noiseMap [x, y] + 1) / (maxPossibleHeight / 0.9f);
					noiseMap [x, y] = Mathf.Clamp (normalizedHeight, 0, int.MaxValue);
				}
			}
		}
		
		//反向插值使得噪声值归一化,每个噪音块的最大最小值可能会有略微不同的差异，所以拼接会有瑕疵
		if (settings.normalizeMode == NormalizeMode.Local) {
			for (int y = 0; y < mapHeight; y++) {
				for (int x = 0; x < mapWidth; x++) {
					noiseMap [x, y] = Mathf.InverseLerp (minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap [x, y]);
				}
			}
		}

		return noiseMap;
	}

}

[Serializable]
public class NoiseSettings
{
	[Header("噪音设置")]
	[Tooltip("噪声归一化模式")] public Noise.NormalizeMode normalizeMode;
		
	[Tooltip("噪声缩放指数")] public float scale = 50;
	[Tooltip("八度音阶的数量")] public int octaves = 6;
	[Range(0, 1), Tooltip("持久性值")] public float persistance = 0.6f;
	[Tooltip("间隙度值")] public float lacunarity = 2;
		
	[Tooltip("种子")] public int seed;//如果 seed 是固定的，则每次运行程序时，生成的随机数序列都是一样的
	[Tooltip("向量偏移量")] public Vector2 offset;
	
	public void ValidateValues() {//值维护更新方法
		scale = Mathf.Max(scale, 0.01f);
		octaves = Mathf.Max(octaves, 1);
		lacunarity = Mathf.Max(lacunarity, 1);
		persistance = Mathf.Clamp01(persistance);
	}
}