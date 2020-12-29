Shader "Ceto/OceanTopSide_Transparent" 
{
	Properties 
	{
		[HideInInspector] _CullFace ("__cf", Float) = 2.0
	}
	SubShader 
	{

		Tags { "OceanMask"="Ceto_ProjectedGrid_Top" "RenderType"="Ceto_ProjectedGrid_Top" "IgnoreProjector"="True" "Queue"="Transparent-101"  }
		LOD 200
		
		GrabPass { "Ceto_RefractionGrab" }
		
		zwrite on
		//cull back

		cull [_CullFace]

		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma surface OceanSurfTop OceanBRDF noforwardadd nolightmap keepalpha //OYM：用OceanSurfTop的方法,用LightingOceanBRDF的自定义光照通道, 只支持一次光照,没有光照图,保持alpha通道
		#pragma vertex OceanVert
		#pragma target 3.0
		
		#pragma multi_compile __ CETO_REFLECTION_ON
		#pragma multi_compile __ CETO_UNDERWATER_ON
		#pragma multi_compile __ CETO_USE_OCEAN_DEPTHS_BUFFER
		#pragma multi_compile __ CETO_USE_4_SPECTRUM_GRIDS
		#pragma multi_compile __ CETO_STERO_CAMERA
		
		//#define CETO_REFLECTION_ON
		//#define CETO_UNDERWATER_ON
		//#define CETO_USE_OCEAN_DEPTHS_BUFFER
		//#define CETO_USE_4_SPECTRUM_GRIDS
		
		//#define CETO_DISABLE_SPECTRUM_SLOPE
		//#define CETO_DISABLE_SPECTRUM_FOAM
		//#define CETO_DISABLE_NORMAL_OVERLAYS
		//#define CETO_DISABLE_FOAM_OVERLAYS
		//#define CETO_DISABLE_EDGE_FADE
		//#define CETO_DISABLE_FOAM_TEXTURE
		//#define CETO_DISABLE_CAUSTICS
		
		//#define CETO_BRDF_FRESNEL
		//#define CETO_NICE_BRDF
		#define CETO_OCEAN_TOPSIDE
		#define CETO_TRANSPARENT_QUEUE
		
		#include "./OceanShaderHeader.cginc"
		#include "./OceanDisplacement.cginc"
		#include "./OceanBRDF.cginc"
		#include "./OceanUnderWater.cginc"
		#include "./OceanSurfaceShaderBody.cginc"

		ENDCG
		
		//OYM： 这个pass好像...没人用?
		Pass 
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			zwrite on 
			ztest lequal

			cull [_CullFace]
			
			CGPROGRAM
			#pragma vertex OceanVertShadow
			#pragma fragment OceanFragShadow
			#pragma target 3.0
		
			#pragma multi_compile_shadowcaster

			#pragma multi_compile __ CETO_USE_4_SPECTRUM_GRIDS
			#pragma multi_compile __ CETO_STERO_CAMERA

			//#define CETO_USE_4_SPECTRUM_GRIDS
	
			#include "UnityCG.cginc"
						
			#include "./OceanShaderHeader.cginc"
			#include "./OceanDisplacement.cginc"
			#include "./OceanShadowCasterBody.cginc"
			
			ENDCG	
		}
		
	} 
	
	FallBack Off
}















