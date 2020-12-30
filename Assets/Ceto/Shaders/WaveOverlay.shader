Shader "Ceto/WaveOverlay"
{

	CGINCLUDE
#include "UnityCG.cginc"

		float4x4 Ceto_ProjectorVP;
	float4x4 Ceto_T2S;
	float4x4 Ceto_Overlay_Rotation;

	sampler2D Ceto_Overlay_Height;
	sampler2D Ceto_Overlay_HeightMask;

	sampler2D Ceto_Overlay_Normal;
	sampler2D Ceto_Overlay_NormalMask;

	sampler2D Ceto_Overlay_Foam;
	sampler2D Ceto_Overlay_FoamMask;

	sampler2D Ceto_Overlay_Clip;

	float Ceto_Overlay_Alpha;
	float Ceto_Overlay_MaskAlpha;
	float Ceto_Overlay_MaskMode;
	float2 Ceto_TextureFoam;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uvtex : TEXCOORD0;
		float2 uvmask : TEXCOORD1;
	};

	struct appdata
	{
		float4 vertex : POSITION;
		float4 texcoord : TEXCOORD0;
		float4 texcoord1 : TEXCOORD1;
	};

	void ApplyMaskAndAlpha(inout float value, inout float mask, float alpha)
	{

		if (Ceto_Overlay_MaskMode == 0.0)
		{
			value *= alpha;
		}
		else if (Ceto_Overlay_MaskMode == 1.0)
		{
			value *= alpha * mask;
			mask = 0;
		}
		else if (Ceto_Overlay_MaskMode == 2.0)
		{
			value *= alpha * (1.0 - mask);
		}
		else if (Ceto_Overlay_MaskMode == 3.0)
		{
			value *= alpha * mask;
		}

	}

	void ApplyMaskAndAlpha(inout float3 value, inout float mask, float alpha)
	{

		value *= alpha * mask;
		return;

		if (Ceto_Overlay_MaskMode == 0.0)
		{
			value *= alpha;
		}
		else if (Ceto_Overlay_MaskMode == 1.0)
		{
			value *= alpha * mask;
			mask = 0;
		}
		else if (Ceto_Overlay_MaskMode == 2.0)
		{
			value *= alpha * (1.0 - mask);
		}
		else if (Ceto_Overlay_MaskMode == 3.0)
		{
			value *= alpha * mask;
		}

	}

	v2f vertWaveOverlay(appdata v)
	{
		v2f OUT;

		OUT.pos = mul(Ceto_ProjectorVP, v.vertex);

		OUT.pos = mul(Ceto_T2S, OUT.pos);

		OUT.uvtex = v.texcoord.xy;

		OUT.uvmask = v.texcoord1.xy;

		return OUT;
	}

	float4 fragWaveOverlayHeight(v2f IN) : COLOR
	{

		float height = tex2D(Ceto_Overlay_Height, IN.uvtex).a;
		float mask = tex2D(Ceto_Overlay_HeightMask, IN.uvmask).a;

		mask = saturate(mask * Ceto_Overlay_MaskAlpha);

		ApplyMaskAndAlpha(height, mask, Ceto_Overlay_Alpha);

		return float4(height, mask, 0, 0); //RG format
	}

		float4 fragWaveOverlayNormal(v2f IN) : COLOR
	{

		float3 norm = UnpackNormal(tex2D(Ceto_Overlay_Normal, IN.uvtex)).xzy;
		float mask = tex2D(Ceto_Overlay_NormalMask, IN.uvmask).a;

		mask = saturate(mask * Ceto_Overlay_MaskAlpha);

		ApplyMaskAndAlpha(norm, mask, Ceto_Overlay_Alpha);

		float3x3 m = Ceto_Overlay_Rotation;

		norm = mul(m, norm);

		return float4(norm, mask);

	}
		
	float4 fragWaveOverlayFoam(v2f IN) : COLOR
	{

		float foam = tex2D(Ceto_Overlay_Foam, IN.uvtex).a;
		float mask = tex2D(Ceto_Overlay_FoamMask, IN.uvmask).a;

		mask = saturate(mask * Ceto_Overlay_MaskAlpha); //OYM：saturate means clamps it's input between 0 and 1.

		ApplyMaskAndAlpha(foam, mask, Ceto_Overlay_Alpha);

		return float4(foam.xx * Ceto_TextureFoam, 0, mask);

	}
	


		ENDCG

		SubShader
	{
		//OYM： 0
		Pass
		{
			ZTest Always Cull off ZWrite Off
			Fog { Mode off }

			blend one one
			BlendOp add

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vertWaveOverlay
			#pragma fragment fragWaveOverlayHeight //OYM： 处理高度
			#pragma enable_d3d11_debug_symbols
			ENDCG

		}
			//OYM： 1
			Pass
		{
			ZTest Always
			Cull off
			ZWrite Off
			Fog { Mode off }

			blend one one
			BlendOp add

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vertWaveOverlay
			#pragma fragment fragWaveOverlayNormal //OYM： 处理法线
			#pragma enable_d3d11_debug_symbols
			ENDCG

		}
			//OYM： 2
			Pass
		{

			ZTest Always
			Cull off
			ZWrite Off
			Fog { Mode off }

			blend one one
			BlendOp add

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vertWaveOverlay
			#pragma fragment fragWaveOverlayFoam //OYM： 处理浮沫
			#pragma enable_d3d11_debug_symbols
			ENDCG

		}

			//OYM： 3
			Pass
		{
			ZTest Always
			Cull off
			ZWrite Off
			Fog { Mode off }

			blend one one
			BlendOp add
			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vertWaveOverlay
			#pragma fragment fragWaveOverlayClip //OYM： 处理剪切
			#pragma enable_d3d11_debug_symbols
			ENDCG

		}
			//OYM： 4
			Pass
		{
			ZTest Always
			Cull off
			ZWrite Off
			Fog{ Mode off }

			blend one one
			BlendOp max

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vertWaveOverlay
			#pragma fragment fragWaveOverlayHeight //OYM： 处理高度
			#pragma enable_d3d11_debug_symbols
			ENDCG

		}

			//OYM： 5
			Pass
		{

			ZTest Always
			Cull off
			ZWrite Off
			Fog{ Mode off }

			blend one one
			BlendOp max

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vertWaveOverlay
			#pragma fragment fragWaveOverlayFoam //OYM： 处理
			#pragma enable_d3d11_debug_symbols
			ENDCG

		}
	}
}





