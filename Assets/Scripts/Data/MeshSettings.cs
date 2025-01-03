using UnityEngine;

namespace Data
{
	[CreateAssetMenu]
	public class MeshSettings : UpdatableData {
		
		[Tooltip("支持的细节层次数量")] public const int numSupportedLODs = 5;
		[Tooltip("支持的地形块大小个数")] public const int numSupportedChunkSizes = 9;
		[Tooltip("支持平面着色的地形块大小个数")] public const int numSupportedFlatshadedChunkSizes = 3;
		[Tooltip("支持的地形块边长数组")] public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

		[Tooltip("统一缩放比例")] public float meshScale = 2.5f;
		[Tooltip("是否使用平面着色")] public bool useFlatShading;
		
		[Tooltip("块大小索引"), Range(0, numSupportedChunkSizes - 1)]
		public int chunkSizeIndex;
		[Tooltip("平面着色细节层次索引"), Range(0,numSupportedFlatshadedChunkSizes - 1)]
		public int flatShadedChunkSizeIndex;
		
		/// <summary>
		/// 在最高分辨率下渲染的网格每行的顶点数，即在细节级别为0时渲染的网格，
		/// 包括为计算法线而创建的两个额外顶点(实际并不包含在最终的网格中)
		/// </summary>
		public int numVertsPerLine => supportedChunkSizes [(useFlatShading) ? flatShadedChunkSizeIndex : chunkSizeIndex] + 1;
		
		/// <summary>
		/// 网格世界尺寸
		/// </summary>
		public float meshWorldSize => (numVertsPerLine - 3) * meshScale;
	}
}
