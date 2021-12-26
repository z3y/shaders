// #pragma warning (default : 3206)

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"

#include "InputsCGI.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;

    #ifdef _TEXTURE_ARRAY
        float3 uv0 : TEXCOORD0;
    #else
        float2 uv0 : TEXCOORD0;
    #endif
    float2 uv1 : TEXCOORD1;

    #ifdef NEED_UV2
        float2 uv2 : TEXCOORD2;
    #endif

    #ifdef NEED_TANGENT_BITANGENT
        float4 tangent : TANGENT;
    #endif

    #ifdef NEED_VERTEX_COLOR
        half4 color : COLOR;
    #endif

    uint vertexId : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float4 coord0 : TEXCOORD0;

    #ifdef NEED_UV2
        float3 coord1 : TEXCOORD1;
    #endif

    #ifdef NEED_TANGENT_BITANGENT
        float3 bitangent : TEXCOORD2;
        float3 tangent : TEXCOORD3;
    #endif

    #ifdef NEED_WORLD_NORMAL
        float3 worldNormal : TEXCOORD4;
    #endif

    #ifdef NEED_WORLD_POS
        float4 worldPos : TEXCOORD5;
    #endif

    #ifdef NEED_PARALLAX_DIR
        float3 parallaxViewDir : TEXCOORD6;
    #endif

    #ifdef NEED_VERTEX_COLOR
        centroid half4 color : COLOR;
    #endif

    #if defined(NEED_CENTROID_NORMAL) && defined(NEED_WORLD_NORMAL)
        centroid float3 centroidWorldNormal : TEXCOORD8;
    #endif

    #ifdef NEED_SCREEN_POS
        float4 screenPos : TEXCOORD9;
    #endif

    #ifdef NEED_FOG
        UNITY_FOG_COORDS(10)
    #endif

    #if !defined(UNITY_PASS_SHADOWCASTER)
        UNITY_SHADOW_COORDS(11)
    #endif

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
        half3 vertexLight : TEXCOORD12;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata v)
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

    o.coord0.xy = v.uv0.xy;
    o.coord0.zw = v.uv1;

    #ifdef NEED_UV2
        o.coord1.xy = v.uv2;

        #ifdef _TEXTURE_ARRAY
            o.coord1.z = v.uv0.z;
        #endif
    #endif


    #ifdef NEED_WORLD_NORMAL
        o.worldNormal = UnityObjectToWorldNormal(v.normal);
    #endif

    #ifdef NEED_TANGENT_BITANGENT
        o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
        o.bitangent = cross(o.tangent, o.worldNormal) * v.tangent.w;
    #endif

    #ifdef NEED_WORLD_POS
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
    #endif

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

    #ifdef NEED_PARALLAX_DIR
        TANGENT_SPACE_ROTATION;
        o.parallaxViewDir = mul (rotation, ObjSpaceViewDir(v.vertex));
    #endif

    #ifdef UNITY_PASS_SHADOWCASTER
        o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
        o.pos = UnityApplyLinearShadowBias(o.pos);
        TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
    #else
        UNITY_TRANSFER_SHADOW(o, o.coord0.zw);
    #endif

    #ifdef NEED_FOG
        UNITY_TRANSFER_FOG(o,o.pos);
    #endif

    #ifdef NEED_SCREEN_POS
        o.screenPos = ComputeScreenPos(o.pos);
    #endif


    #if defined(NEED_CENTROID_NORMAL) && defined(NEED_WORLD_NORMAL)
        o.centroidWorldNormal = o.worldNormal;
    #endif

    return o;
}

#include "FunctionsCGI.cginc"
#include "LitSurfaceData.cginc"
#include "CoreCGI.cginc"