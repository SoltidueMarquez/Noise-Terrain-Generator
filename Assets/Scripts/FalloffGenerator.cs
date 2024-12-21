using UnityEngine;

public static class FalloffGenerator {

	/// <summary>
	/// 衰减地图生成
	/// </summary>
	/// <param name="size"></param>
	/// <returns>二维浮点数组，衰减贴图</returns>
	public static float[,] GenerateFalloffMap(int size) {
		float[,] map = new float[size,size];//声明size边长的二维数组
		//遍历地图上的每一个点
		for (int i = 0; i < size; i++) {
			for (int j = 0; j < size; j++) {
				float x = i / (float)size * 2 - 1;//将x归一化到[-1,1]
				float y = j / (float)size * 2 - 1;//将y归一化到[-1,1]
				//取x和y中绝对值较大的那一个
				float value = Mathf.Max (Mathf.Abs (x), Mathf.Abs (y));
				map [i, j] = Evaluate(value);
			}
		}

		return map;
	}

	/// <summary>
	/// 曲线参数方程，缓解衰减地图的衰减幅度
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	static float Evaluate(float value) {
		float a = 3;
		float b = 2.2f;

		return Mathf.Pow (value, a) / (Mathf.Pow (value, a) + Mathf.Pow (b - b * value, a));
	}
}
