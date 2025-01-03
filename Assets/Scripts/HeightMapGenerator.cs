using Data;
using UnityEngine;

public static class HeightMapGenerator {

	/// <summary>
	/// 生成高度图
	/// </summary>
	/// <param name="width">高度图宽度</param>
	/// <param name="height">高度图高度</param>
	/// <param name="settings">高度图设置</param>
	/// <param name="sampleCentre">采样中心</param>
	/// <returns></returns>
	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre) {
		var values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);//生成噪音图

		var heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);//创建高度曲线副本避免多线程错误

		var minValue = float.MaxValue;
		var maxValue = float.MinValue;

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;//计算高度
				//更新最大最小值
				if (values [i, j] > maxValue) {
					maxValue = values [i, j];
				}
				if (values [i, j] < minValue) {
					minValue = values [i, j];
				}
			}
		}

		return new HeightMap (values, minValue, maxValue);
	}

}

/// <summary>
/// 地图高度数据
/// </summary>
public struct HeightMap {
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] values, float minValue, float maxValue)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}


