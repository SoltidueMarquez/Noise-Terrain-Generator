using System;
using UnityEngine;
using System.Linq;

namespace Data
{
	public class TextureData : UpdatableData {
		
		const int textureSize = 512;	//纹理尺寸常数
		const TextureFormat textureFormat = TextureFormat.RGB565;//十六位颜色纹理格式
		
		public Layer[] layers;
		
		//保存的最大最小高度
		float savedMinHeight;
		float savedMaxHeight;

		/// <summary>
		/// 设置材质
		/// </summary>
		/// <param name="material"></param>
		public void ApplyToMaterial(Material material) {
			material.SetInt ("layerCount", layers.Length);//设置图层数量
			material.SetColorArray ("baseColours", layers.Select(x => x.tint).ToArray());//设置色调数组
			material.SetFloatArray ("baseStartHeights", layers.Select(x => x.startHeight).ToArray());//设置起始高度数组
			material.SetFloatArray ("baseBlends", layers.Select(x => x.blendStrength).ToArray());//设置混合强度数组
			material.SetFloatArray ("baseColourStrength", layers.Select(x => x.tintStrength).ToArray());//设置色调强度数组
			material.SetFloatArray ("baseTextureScales", layers.Select(x => x.textureScale).ToArray());//设置纹理缩放数组
			var texturesArray = GenerateTextureArray (layers.Select (x => x.texture).ToArray ());//生成2D纹理数组
			material.SetTexture ("baseTextures", texturesArray);//设置2D纹理数组

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
		
		Texture2DArray GenerateTextureArray(Texture2D[] textures) {
			var textureArray = new Texture2DArray (textureSize, textureSize, textures.Length, textureFormat, true);
			for (var i = 0; i < textures.Length; i++) {//遍历所有纹理，设置像素
				textureArray.SetPixels (textures[i].GetPixels(), i);
			}
			textureArray.Apply ();
			return textureArray;
		}
	}
	
	/// <summary>
	/// 自定义纹理层
	/// </summary>
	[Serializable]
	public class Layer {
		[Tooltip("纹理")] public Texture2D texture;
		[Tooltip("色调")] public Color tint;
		[Tooltip("强度"), Range(0,1)] public float tintStrength;
		[Tooltip("起始高度"), Range(0,1)] public float startHeight;
		[Tooltip("混合值强度"), Range(0, 1)] public float blendStrength;
		[Tooltip("纹理缩放比例")] public float textureScale;
	}
}
