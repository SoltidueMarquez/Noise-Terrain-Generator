using UnityEngine;
using UnityEngine.Serialization;

namespace Data
{
	[CreateAssetMenu]
	public class HeightMapSettings : UpdatableData {
		
		[Tooltip("噪音设置")] public NoiseSettings noiseSettings;

		[Tooltip("是否应用衰减贴图")] public bool useFalloff;
		
		[Header("网格设置")]
		[Tooltip("网格高度乘数")] public float heightMultiplier;
		[Tooltip("不同高度收乘数影响的程度")] public AnimationCurve heightCurve;
		
		//地形最小与最大高度
		public float minHeight => heightMultiplier * heightCurve.Evaluate (0);
		public float maxHeight => heightMultiplier * heightCurve.Evaluate (1);


#if UNITY_EDITOR
		protected override void OnValidate() {//执行噪音设置中的值维护更新方法
			noiseSettings.ValidateValues ();
			base.OnValidate ();
		}
#endif
	}
}
