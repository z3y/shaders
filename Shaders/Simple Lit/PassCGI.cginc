// #pragma warning (default : 3206)

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"

struct v2f
{
    float4 pos : SV_POSITION;
    float4 uv[4] : TEXCOORD0;
    float3 bitangent : TEXCOORD4;
    float3 tangent : TEXCOORD5;
    float3 worldNormal : TEXCOORD6;
    float4 worldPos : TEXCOORD7;

    #if defined(PARALLAX) || defined(BAKERYLM_ENABLED) || defined(EMISSION) || defined(_DETAILALBEDO_MAP) || defined(_DETAILNORMAL_MAP)
        float3 parallaxViewDir : TEXCOORD8;
    #endif

    #ifdef NEED_VERTEX_COLOR
        centroid half4 color : COLOR;
    #endif

    #if defined(NEED_CENTROID_NORMAL)
        centroid float3 centroidWorldNormal : TEXCOORD10;
    #endif

    #ifdef NEED_SCREEN_POS
        float4 screenPos : TEXCOORD11;
    #endif

    #if !defined(UNITY_PASS_SHADOWCASTER)
        UNITY_FOG_COORDS(12)
        UNITY_SHADOW_COORDS(13)
    #endif

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
        half3 vertexLight : TEXCOORD14;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

#include "InputsCGI.cginc"
#include "../ShaderLibrary/SurfaceData.cginc"
#include "Defines.cginc"
#include "../ShaderLibrary/CommonFunctions.cginc"

v2f vert (appdata_all v)
{
    v2f o;
    UNITY_INITIALIZE_OUTPUT(v2f, o);
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #ifdef UNITY_PASS_META
        o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
    #else
        #if !defined(UNITY_PASS_SHADOWCASTER)
            o.pos = UnityObjectToClipPos(v.vertex);
        #endif
    #endif

    o.uv[0].xy = v.uv0.xy;
    o.uv[1].xy = v.uv1.xy;
    o.uv[2].xy = v.uv2.xy;
    o.uv[3].xy = v.uv3.xy;

    float4 mainST = UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _MainTex_ST);
    o.uv[0].zw = v.uv0.xy * mainST.xy + mainST.zw;
    o.uv[1].zw = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    o.uv[2].zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;

    #ifdef _TEXTURE_ARRAY
        o.uv[3].z = _Texture == 2.0 ? UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _TextureIndex) : v.uv0.z;
    #endif

    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
    o.bitangent = cross(o.tangent, o.worldNormal) * v.tangent.w;
    o.worldPos = mul(unity_ObjectToWorld, v.vertex);

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
		o.vertexLight = Shade4PointLights
        (
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb,
			unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, o.worldPos,  o.worldNormal
		);
	#endif

    #ifdef NEED_VERTEX_COLOR
        o.color = v.color;
    #endif

    #if defined(PARALLAX) || defined(BAKERYLM_ENABLED) || defined(EMISSION) || defined(_DETAILALBEDO_MAP) || defined(_DETAILNORMAL_MAP)
        TANGENT_SPACE_ROTATION;
        o.parallaxViewDir = mul (rotation, ObjSpaceViewDir(v.vertex));
    #endif

    #ifdef UNITY_PASS_SHADOWCASTER
        o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
        o.pos = UnityApplyLinearShadowBias(o.pos);
        TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
    #else
        UNITY_TRANSFER_SHADOW(o, o.uv[1].xy);
        UNITY_TRANSFER_FOG(o,o.pos);
    #endif

    #ifdef NEED_SCREEN_POS
        o.screenPos = ComputeScreenPos(o.pos);
    #endif


    #if defined(NEED_CENTROID_NORMAL)
        o.centroidWorldNormal = o.worldNormal;
    #endif

    return o;
}

#include "../ShaderLibrary/BicubicSampling.cginc"
#include "../ShaderLibrary/MultistepParallax.cginc"
#include "../ShaderLibrary/NonImportantLights.cginc"
#include "../ShaderLibrary/EnvironmentBRDF.cginc"
#include "../ShaderLibrary/BlendModes.cginc"
#ifdef AUDIOLINK
#include "../ShaderLibrary/AudioLink.cginc"
#endif
#include "LitSurfaceData.cginc"
#include "CoreCGI.cginc"