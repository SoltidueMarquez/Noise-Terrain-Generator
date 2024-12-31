//根据表面高度在不同纹理之间进行混合
Shader "Custom/Terrain" {
	Properties {

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxColourCount = 8;//最多八种颜色

		int baseColourCount;//基础颜色数目
		float3 baseColours[maxColourCount];
		float baseStartHeights[maxColourCount];
		
		//最小和最大高度
		float minHeight;
		float maxHeight;

		struct Input {
			float3 worldPos;//输入世界坐标
		};

		float inverseLerp(float a, float b, float value) {//反向线性插值
			return saturate((value-a)/(b-a));
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight,maxHeight, IN.worldPos.y);//对高度进行反向线性插值
			for (int i = 0; i < baseColourCount; i ++) {//遍历所有的基础颜色
				float drawStrength = saturate(sign(heightPercent - baseStartHeights[i]));
				o.Albedo = o.Albedo * (1-drawStrength) + baseColours[i] * drawStrength;//根据计算出的drawStrength，混合当前遍历到的颜色与之前的颜色。
			}
		}
		
		ENDCG
	}
	FallBack "Diffuse"
}
