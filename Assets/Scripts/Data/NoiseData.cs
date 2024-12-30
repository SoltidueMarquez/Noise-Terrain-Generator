using UnityEngine;

namespace Data
{
	[CreateAssetMenu]
	public class NoiseData : UpdatableData {
		[Header("噪音设置")]
		[Tooltip("噪声归一化模式")] public Noise.NormalizeMode normalizeMode;
		
		[Tooltip("噪声缩放指数")] public float noiseScale;
		[Tooltip("八度音阶的数量")] public int octaves;
		[Range(0, 1), Tooltip("持久性值")] public float persistance;
		[Tooltip("间隙度值")] public float lacunarity;
		

		[Tooltip("种子")] public int seed;//如果 seed 是固定的，则每次运行程序时，生成的随机数序列都是一样的
		[Tooltip("向量偏移量")] public Vector2 offset;


		protected override void OnValidate() {
			if (lacunarity < 1) {
				lacunarity = 1;
			}
			if (octaves < 0) {
				octaves = 0;
			}

			base.OnValidate ();
		}

	}
}
