using UnityEngine;

namespace Data
{
	public class TextureData : UpdatableData {
		[Tooltip("基础颜色")] public Color[] baseColours;
		[Tooltip("基础起始高度")] [Range(0,1)] public float[] baseStartHeights;

		//保存的最大最小高度
		float savedMinHeight;
		float savedMaxHeight;

		/// <summary>
		/// 设置材质
		/// </summary>
		/// <param name="material"></param>
		public void ApplyToMaterial(Material material) {
			material.SetInt ("baseColourCount", baseColours.Length);
			material.SetColorArray ("baseColours", baseColours);
			material.SetFloatArray ("baseStartHeights", baseStartHeights);
			UpdateMeshHeights (material, savedMinHeight, savedMaxHeight);
		}

		/// <summary>
		/// 更新网格高度
		/// </summary>
		/// <param name="material">材质</param>
		/// <param name="minHeight">最小高度</param>
		/// <param name="maxHeight">最大高度</param>
		public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {
			savedMinHeight = minHeight;
			savedMaxHeight = maxHeight;

			material.SetFloat ("minHeight", minHeight);
			material.SetFloat ("maxHeight", maxHeight);
		}
	}
}
