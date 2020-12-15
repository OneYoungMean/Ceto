// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'



Shader "Ceto/OceanMask" 
{

	SubShader 
	{
		Tags { "OceanMask"="Ceto_ProjectedGrid_Top" "Queue"="Geometry+1"}
		Pass 
		{
		
			zwrite on  //OYM：固体专用, 如果要绘制半透明效果，请切换到ZWrite Off。
			Fog { Mode Off }//OYM： 关闭雾气
			Lighting off //OYM： 关闭灯光
			
			cull back //OYM： 剔除背面

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0 //OYM： 生成代码用到的api(https://docs.unity3d.com/2019.3/Documentation/Manual/SL-ShaderCompileTargets.html)
			#pragma vertex OceanVertMask //OYM： vertex方法
			#pragma fragment OceanFragMask //OYM： frag方法

			#pragma multi_compile __ CETO_USE_4_SPECTRUM_GRIDS //OYM： 你可以写 着色器共享通用代码但在启用或禁用给定关键字时具有不同功能的代码段。Unity编译这些着色器片段时，它将为启用和禁用关键字的不同组合创建单独的着色器程序。这些单独的着色器程序称为着色器变体。
			//#define CETO_USE_4_SPECTRUM_GRIDS
			
			#define CETO_OCEAN_TOPSIDE //OYM： 定义海洋表面
		    
			#include "./OceanShaderHeader.cginc"
			#include "./OceanDisplacement.cginc"
			#include "./OceanMasking.cginc"
			#include "./OceanMaskBody.cginc"		
			
			ENDCG   
		}
	}

	SubShader 
	{
		Tags { "OceanMask"="Ceto_ProjectedGrid_Under" "Queue"="Geometry+2"}
		Pass 
		{
			zwrite on
			Fog { Mode Off }
			Lighting off
			
			cull front

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex OceanVertMask
			#pragma fragment OceanFragMask

			#pragma multi_compile __ CETO_USE_4_SPECTRUM_GRIDS
			//#define CETO_USE_4_SPECTRUM_GRIDS
			
			#define CETO_OCEAN_UNDERSIDE

			#include "./OceanShaderHeader.cginc"
			#include "./OceanDisplacement.cginc"
			#include "./OceanMasking.cginc"
			#include "./OceanMaskBody.cginc"		
			
			ENDCG
		}
	}
	
	SubShader 
	{
		
		Tags { "OceanMask"="Ceto_Ocean_Bottom" "Queue"="Background"}
		Pass 
		{
			zwrite off
			Fog { Mode Off }
			Lighting off

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "./OceanShaderHeader.cginc"
			#include "./OceanMasking.cginc"

			struct v2f 
			{
				float4  pos : SV_POSITION;
				float depth : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f o;

				float4 worldPos = mul(OBJECT_TO_WORLD, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);

				o.depth = COMPUTE_DEPTH_01;
				
				return o;
		 	} 
			
			float4 frag(v2f IN) : SV_Target
			{
			    return float4(BOTTOM_MASK, IN.depth, 0, 0);
			}	
			ENDCG
		} 
	}     
}











