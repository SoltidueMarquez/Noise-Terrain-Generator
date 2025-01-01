//根据表面高度在不同纹理之间进行混合
Shader "Custom/Terrain" {
	Properties {
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCount = 8;	//最多八层纹理
		const static float epsilon = 1E-4;	//常量静态极小值浮点数

		int layerCount;								//图层数量
		float3 baseColours[maxLayerCount];			//色调数组
		float baseStartHeights[maxLayerCount];		//起始高度数组
		float baseBlends[maxLayerCount];			//混合强度数组
		float baseColourStrength[maxLayerCount];	//色调强度数组
		float baseTextureScales[maxLayerCount];		//纹理缩放数组
		
		//最小和最大高度
		float minHeight;
		float maxHeight;

		sampler2D testTexture;	//纹理
		float testScale;		//纹理大小

		UNITY_DECLARE_TEX2DARRAY(baseTextures);		//声明纹理数组

		struct Input {
			float3 worldPos;//输入世界坐标
			float3 worldNormal;//输入法线
		};

		float inverseLerp(float a, float b, float value) {//反向线性插值
			return saturate((value-a)/(b-a));
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {//三平面映射避免纹理不自然拉伸
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}
		
		void surf (Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight,maxHeight, IN.worldPos.y);//对高度进行反向线性插值
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			for (int i = 0; i < layerCount; i ++) {//遍历所有的层级
				float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);//当前像素高度低于起始高度一半的基础混合值时时绘制高度为极小值(避免除零)，高于则插值

				float3 baseColour = baseColours[i] * baseColourStrength[i];
				float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1-baseColourStrength[i]);

				o.Albedo = o.Albedo * (1-drawStrength) + (baseColour+textureColour) * drawStrength;//根据计算出的drawStrength，混合当前遍历到的颜色与之前的颜色。
			}
		}
		
		ENDCG
	}
	FallBack "Diffuse"
}
