// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#ifndef CETO_SURFACESHADER_BODY_INCLUDED //OYM： if no define
#define CETO_SURFACESHADER_BODY_INCLUDED

struct appdata_ceto
{
	float4 vertex : POSITION;    //OYM： 位置
	float4 tangent : TANGENT; //OYM： 切线
	float3 normal : NORMAL; //OYM： 法线
	float2 texcoord : TEXCOORD0; //OYM： 纹理
};

struct Input 
{
	float4 wPos;
	float4 screenUV;
	float4 grabUV;
	float4 texUV;
};

void OceanVert(inout appdata_ceto v, out Input OUT) //OYM： 这里是vertex方法,它会初始化input变量
{
	
	UNITY_INITIALIZE_OUTPUT(Input, OUT); //OYM： 将给定类型的变量名称初始化为零
	
	float4 uv = float4(v.vertex.xy, v.texcoord.xy); //OYM： x,y坐标与u,v坐标

	float4 oceanPos;
	float3 displacement;
	OceanPositionAndDisplacement(uv, oceanPos, displacement); //OYM： 返回海洋相对于屏幕的UV
	
	v.vertex.xyz = oceanPos.xyz + displacement;//OYM： 对顶点进行变形
	
	v.tangent = float4(1, 0, 0, 1);  //OYM： 初始化切线
	
#ifdef CETO_OCEAN_TOPSIDE //OYM： 初始化法线
		v.normal = float3(0,1,0);
	#else
		v.normal = float3(0,-1,0);
	#endif
	
		OUT.wPos = float4(v.vertex.xyz, COMPUTE_DEPTH_01); //OYM： 判断屏幕深度,如果在相机后面就是0 
	OUT.texUV = uv;

	float4 screenPos = UnityObjectToClipPos(v.vertex);//将位置从对象转换为均匀空间

	float4 screenUV = ComputeScreenPos(screenPos); //OYM： 给定投影/相机空间中的位置（我认为-本质上是顶点着色器中的o.pos），它将返回该点在屏幕上的位置
	screenUV = UNITY_PROJ_COORD(screenUV);  //OYM： 某种特殊的针对平台的处理,貌似已经没有用了

	float4 grabUV = ComputeGrabScreenPos(screenPos);//OYM： 给定屏幕的坐标,返回其UV,大部分时候这两个值都一样,当出现图片在相机后可能会相反
	grabUV = UNITY_PROJ_COORD(grabUV); 

	OUT.screenUV = screenUV;
	OUT.grabUV = grabUV;
	
} 

void OceanSurfTop(Input IN, inout SurfaceOutputOcean o)  //OYM： 主要的绘制方法应该都在这里
{
	
	float4 uv = IN.texUV; //OYM： 这里是材质的UV
	float3 worldPos = IN.wPos.xyz;//OYM： 这里是世界的坐标 
	float depth = IN.wPos.w;//OYM： 深度

	float4 screenUV;
	screenUV.xy = IN.screenUV.xy / IN.screenUV.w;  //OYM：计算屏幕UV
	screenUV.zw = IN.grabUV.xy / IN.grabUV.w; //OYM： 计算抓取UV
	
	float4 st = WorldPosToProjectorSpace(worldPos); //OYM： 世界坐标转投影坐标
	OceanClip(st, worldPos); //OYM： 海面裁剪
			
	half3 norm1, norm2, norm3;
	half3 unmaskedNorm;
	fixed4 foam;
	
	half3 view = normalize(_WorldSpaceCameraPos-worldPos);
	float dist = length(_WorldSpaceCameraPos-worldPos);

	#ifdef CETO_USE_4_SPECTRUM_GRIDS

		//If 4 grids are being used use 3 normals where...
		//norm1 is grid 0 + 1
		//norm2 is grid 0 + 1 + 2
		//norm3 is grid 0 + 1 + 2 + 3
		//unmaskedNorm2 is same as norm2 but without any making from overlays
		//This is done so the shader can use normals of different detail
		OceanNormalAndFoam(uv, st, worldPos, norm1, norm2, norm3, unmaskedNorm, foam);
		
		if(dot(view, norm1) < 0.0) norm1 = reflect(norm1, view);
		if(dot(view, norm2) < 0.0) norm2 = reflect(norm2, view);
		if(dot(view, norm3) < 0.0) norm3 = reflect(norm3, view);

	#else

		//If 2 or 1 grid is being use just use one normal
		//It then needs to be applied to norm1, nor2 and norm3.
		half3 norm;
		OceanNormalAndFoam(uv, st, worldPos, norm, unmaskedNorm, foam);

		if (dot(view, norm) < 0.0) norm = reflect(norm, view);

		norm1 = norm;
		norm2 = norm;
		norm3 = norm;

	#endif
	
	fixed3 sky = ReflectionColor(norm2, screenUV.xy);

	float4 disortionUV = DisortScreenUV(norm2, screenUV, depth, dist, view);

	float3 worldDepthPos = WorldDepthPos(disortionUV.xy);

	fixed3 caustics = CausticsFromAbove(disortionUV.xy, unmaskedNorm, worldPos, worldDepthPos, dist);
	
	fixed3 sea = OceanColorFromAbove(disortionUV, worldPos, depth, caustics);

	sea += SubSurfaceScatter(view, norm1, worldPos.y);
	
	fixed fresnel = FresnelAirWater(view, norm3);

	fixed foamAmount = FoamAmount(worldPos, foam);
	
	fixed3 col = fixed3(0,0,0);
	
	col += sky * fresnel; //OYM： 天空的附加
	
	col += sea * (1.0 - fresnel); //OYM： 海底的附加

	col = AddFoamColor(uv,foamAmount, col); //OYM：这里修改foam颜色,并贴到海面上

	float edgeFade = EdgeFade(screenUV.xy, view, worldPos, worldDepthPos);
	col = ApplyEdgeFade(col, screenUV.zw, edgeFade, o.Alpha, o.LightMask); //OYM： 在这里修改光照

	o.Albedo = col;
	o.Normal = TangentSpaceNormal(norm3);
	o.DNormal = norm3;
	o.Fresnel = fresnel;
	o.Foam = foamAmount;

}

void OceanSurfUnder(Input IN, inout SurfaceOutputOcean o) 
{

	float4 uv = IN.texUV;
	float3 worldPos = IN.wPos.xyz;
	float depth = IN.wPos.w;

	float4 screenUV;
	screenUV.xy = IN.screenUV.xy / IN.screenUV.w;
	screenUV.zw = IN.grabUV.xy / IN.grabUV.w;
	
	float4 st = WorldPosToProjectorSpace(worldPos);
	OceanClip(st, worldPos);
	
	half3 norm, unmaskedNorm;
	fixed4 foam;
	OceanNormalAndFoam(uv, st, worldPos, norm, unmaskedNorm, foam);
	norm.y *= -1.0;
		
	half3 view = normalize(_WorldSpaceCameraPos-worldPos);
	float dist = length(_WorldSpaceCameraPos-worldPos);

	if (dot(view, norm) < 0.0) norm = reflect(norm, view);

	float4 disortionUV = DisortScreenUV(norm, screenUV, depth, dist, view);
	
	fixed3 sky = SkyColorFromBelow(disortionUV);
	
	fixed3 sea = DefaultUnderSideColor();
	
	float fresnel = FresnelWaterAir(view, norm);

	fixed foamAmount = FoamAmount(worldPos, foam);

	fixed3 col = fixed3(0,0,0);
	
	col += sea * fresnel;
	
	col += sky * (1.0-fresnel);
	
	col = AddFoamColor(uv,foamAmount, col);

	o.Albedo = col;
	o.Normal = TangentSpaceNormal(norm);
	o.DNormal = norm;
	o.Fresnel = fresnel;
	o.Foam = foamAmount;
	o.Alpha = 1.0;
	o.LightMask = 0.0;

}

		
#endif



