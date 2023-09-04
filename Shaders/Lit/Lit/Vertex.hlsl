#include "Wind.hlsl"

#ifdef _DETAIL_ALBEDOMAP
    #define REQUIRE_COLOR
#endif

struct v2f
{
    float4 pos : SV_POSITION;
    float4 uv01 : TEXCOORD0;
    float4 uv23 : TEXCOORD1;
    float3 bitangent : TEXCOORD3;
    float3 tangent : TEXCOORD4;
    float3 worldNormal : TEXCOORD5;
    float4 worldPos : TEXCOORD6;

    #if defined(REQUIRE_VIEWDIRTS)
        float3 viewDirTS : TEXCOORD7;
    #endif

    #if !defined(UNITY_PASS_SHADOWCASTER)
        UNITY_FOG_COORDS(8)
        UNITY_SHADOW_COORDS(9)
    #endif

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
        half3 vertexLight : TEXCOORD10;
    #endif

    #if defined(REQUIRE_COLOR)
        half4 vertexColor : TEXCOORD11;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

#include "../ShaderLibrary/Common.hlsl"

v2f vert (appdata_all v)
{
    v2f o;
    UNITY_INITIALIZE_OUTPUT(v2f, o);
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv01 = float4(v.uv0.xy, v.uv1.xy);
    o.uv23 = float4(v.uv2.xy, v.uv3.xy);

    #ifdef _WIND
        v.vertex.xyz += GetWindOffset(v.vertex.xyz, v.color);
    #endif
    
    #ifdef UNITY_PASS_META
        o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
    #else
        #if !defined(UNITY_PASS_SHADOWCASTER)
            o.pos = UnityObjectToClipPos(v.vertex);
        #endif
    #endif

    #ifdef REQUIRE_COLOR
    o.vertexColor = v.color;
    #endif
    

    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
    o.bitangent = cross(o.worldNormal, o.tangent) * v.tangent.w;
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

    #ifdef UNITY_PASS_SHADOWCASTER
        //o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
        //o.pos = UnityApplyLinearShadowBias(o.pos);
        TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
    #else
        UNITY_TRANSFER_SHADOW(o, v.uv1);
        UNITY_TRANSFER_FOG(o, o.pos);
    #endif

    #if defined(REQUIRE_VIEWDIRTS)
        TANGENT_SPACE_ROTATION;
        o.viewDirTS = mul(rotation, ObjSpaceViewDir(v.vertex));
    #endif

    return o;
}