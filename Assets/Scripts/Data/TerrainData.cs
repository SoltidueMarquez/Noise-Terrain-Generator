using UnityEngine;

namespace Data
{
	[CreateAssetMenu]
	public class TerrainData : UpdatableData {
		[Tooltip("统一缩放比例")] public float uniformScale = 2.5f;

		[Tooltip("是否使用平面着色")] public bool useFlatShading;
		[Tooltip("是否应用衰减贴图")] public bool useFalloff;

		[Header("网格设置")]
		[Tooltip("网格高度乘数")] public float meshHeightMultiplier;
		[Tooltip("不同高度收乘数影响的程度")] public AnimationCurve meshHeightCurve;
	}
}
