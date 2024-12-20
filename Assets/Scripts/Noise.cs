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
	/// <param name="seed">种子用于生成相同的地图</param>
	/// <param name="scale">缩放比例，用于避免整数</param>
	/// <param name="octaves">八度音阶的数量</param>
	/// <param name="persistance">持久性值</param>
	/// <param name="lacunarity">间隙度值</param>
	/// <param name="offset">向量偏移</param>
	/// <returns></returns>
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
		float[,] noiseMap = new float[mapWidth,mapHeight];
		
		//八度音阶采样偏移
		System.Random prng = new System.Random (seed);//伪随机数生成器
		//八度音阶偏移向量数组
		Vector2[] octaveOffsets = new Vector2[octaves];
		
		float maxPossibleHeight = 0;
		float amplitude = 1;//设置振幅
		float frequency = 1;//设置频率，越高采样点之间的间隙越远，高度值变化更加迅速
		
		//循环生成每个八度的偏移量，对于每个八度生成一个随机数（范围在 -100000 到 100000 之间），并与 offset.x 和 offset.y 进行相加
		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next (-100000, 100000) + offset.x;
			float offsetY = prng.Next (-100000, 100000) - offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);//这些偏移量存储在 octaveOffsets 数组中
			
			//找到可能的最大高度值
			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}
		
		//判断比例大小避免除零错误
		if (scale <= 0) {
			scale = 0.0001f;
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
				for (int i = 0; i < octaves; i++) {
					//通过乘以频率来调整采样坐标，每个八度音阶从不同的地方采样，同时做数学处理保证改变scale时从中心改变
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;
					//调用柏林函数的数学库叠加八度音阶，将其范围改到[-1,1]
					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;
					//更新八度音阶
					amplitude *= persistance;
					frequency *= lacunarity;
				}
				//更新最大和最小的噪声值
				if (noiseHeight > maxLocalNoiseHeight) {
					maxLocalNoiseHeight = noiseHeight;
				} else if (noiseHeight < minLocalNoiseHeight) {
					minLocalNoiseHeight = noiseHeight;
				}
				noiseMap [x, y] = noiseHeight;//应用噪声高度
			}
		}
		
		//反向插值使得噪声值归一化,每个噪音块的最大最小值可能会有略微不同的差异，所以拼接会有瑕疵
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				if (normalizeMode == NormalizeMode.Local) {//如果归一化模式为本地，则根据每个块自己的最大最小值归一化
					noiseMap [x, y] = Mathf.InverseLerp (minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap [x, y]);
				} else
				{
					var normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
					noiseMap [x, y] = Mathf.Clamp(normalizedHeight,0, int.MaxValue);
				}
			}
		}

		return noiseMap;
	}

}
