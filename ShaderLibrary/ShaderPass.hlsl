#pragma warning (disable : 1519)
#undef BUILTIN_TARGET_API

#if defined(UNITY_INSTANCING_ENABLED) || defined(STEREO_INSTANCING_ON) || defined(INSTANCING_ON)
    #define UNITY_ANY_INSTANCING_ENABLED 1
#endif

#ifdef STEREO_INSTANCING_ON
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

#ifdef BUILD_TARGET_ANDROID
#define UNITY_PBS_USE_BRDF1
#define QUALITY_LOW
#endif

#ifndef UNITY_PBS_USE_BRDF1
    #define QUALITY_LOW
#endif

#ifndef QUALITY_LOW
    // #define VERTEXLIGHT_PS
#endif

#ifdef SHADER_API_MOBILE
    #define QUALITY_LOW
#endif

#define SPECULAR_OCCLUSION_V2

#ifdef QUALITY_LOW
    #undef _SSR
    #undef REQUIRE_DEPTH_TEXTURE
    #undef REQUIRE_OPAQUE_TEXTURE
    #undef LTCGI
    #undef _GEOMETRICSPECULAR_AA
    #define BAKERY_SHNONLINEAR_OFF
    #undef UNITY_SPECCUBE_BLENDING
    #undef NONLINEAR_LIGHTPROBESH
    #define DISABLE_LIGHT_PROBE_PROXY_VOLUME
    #undef _PARALLAXMAP
    #undef _AREALIT

    #if defined(LIGHTMAP_ON) && !defined(SHADOWS_SHADOWMASK) && !defined(LIGHTMAP_SHADOW_MIXING)
    #undef DIRECTIONAL
    #endif
    #define DISABLE_NONIMPORTANT_LIGHTS_PER_PIXEL
#endif

#ifdef LTCGI_DIFFUSE_OFF
    #define LTCGI_DIFFUSE_DISABLED
    #undef LTCGI_DIFFUSE_OFF
#endif


#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #define FOG_ANY
#endif

#if defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD)
    #define UNITY_PASS_FORWARD
#endif

#if defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING)
#define VARYINGS_NEED_SHADOWCOORD
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#ifdef LIGHTMAP_ON
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#ifdef DYNAMICLIGHTMAP_ON
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(UNITY_PASS_FORWARDBASE) && !defined(LIGHTMAP_ON) && defined(QUALITY_LOW)
    #define VARYINGS_NEED_SH
    #define UNITY_SAMPLE_FULL_SH_PER_PIXEL 0
#elif !defined(LIGHTMAP_ON)
    #define UNITY_SAMPLE_FULL_SH_PER_PIXEL 1
#endif

#if (defined(UNITY_PASS_FORWARD) || defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD)) && !defined(SHADING_UNLIT)
#define VARYINGS_NEED_NORMAL
#define VARYINGS_NEED_TANGENT
#endif

#if defined(UNITY_PASS_META)
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(UNITY_PASS_SHADOWCASTER)
#define ATTRIBUTES_NEED_NORMAL
#endif

#ifdef _ALPHAPREMULTIPLY_ON
#define _SURFACE_TYPE_TRANSPARENT
#endif

#define Unity_SafeNormalize SafeNormalize

#ifdef _SSR
#define REQUIRE_DEPTH_TEXTURE
#define REQUIRE_OPAQUE_TEXTURE
#endif

#ifdef PIPELINE_BUILTIN
#ifndef UNITY_PASS_FORWARDBASE
#undef _SSR
#endif
#endif

#ifdef UNITY_PASS_SHADOWCASTER
    #undef _PARALLAXMAP
#endif

#if defined(SHADER_STAGE_FRAGMENT) && defined(_AREALIT)
#define VARYINGS_NEED_CULLFACE
#endif


//should get moved to a separate file eventually
#ifdef VRCHAT_SDK
float _VRChatMirrorMode;
float _VRChatCameraMode;

bool IsInMirror()
{
    return _VRChatMirrorMode != 0;
}
#else
bool IsInMirror()
{
    return false;
}
#endif


#if defined(LTCGI) && defined(LIGHTMAP_ON)
    #define VARYINGS_NEED_TEXCOORD1
#endif




#ifdef PIPELINE_BUILTIN
#define USE_EXTERNAL_CORERP 0
#endif
#ifdef PIPELINE_URP
#define USE_EXTERNAL_CORERP 1
#endif

#if USE_EXTERNAL_CORERP
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#else
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/Color.hlsl"
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/Packing.hlsl"
    // #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/EntityLighting.hlsl"
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/Texture.hlsl"
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/CommonMaterial.hlsl"
#endif

#ifdef PIPELINE_BUILTIN
    
    #ifdef FORCE_SPECCUBE_BOX_PROJECTION
        #define UNITY_SPECCUBE_BOX_PROJECTION
    #endif
    
    #include "Packages/com.z3y.shaders/ShaderLibrary/UnityCG/ShaderVariablesMatrixDefsLegacyUnity.hlsl"
    
    #undef GLOBAL_CBUFFER_START // dont need reg
    #define GLOBAL_CBUFFER_START(name) CBUFFER_START(name)
    
    #undef SAMPLE_DEPTH_TEXTURE
    #undef SAMPLE_DEPTH_TEXTURE_LOD
    #undef UNITY_MATRIX_P
    #undef UNITY_MATRIX_MVP
    #undef UNITY_MATRIX_MV
    #undef UNITY_MATRIX_T_MV
    #undef UNITY_MATRIX_IT_MV

    #include "UnityShaderVariables.cginc"
    half4 _LightColor0;
    half4 _SpecColor;
    #include "Packages/com.z3y.shaders/ShaderLibrary/UnityCG/UnityCG.hlsl"
    #include "AutoLight.cginc"

    #include "Packages/com.z3y.shaders/ShaderLibrary/Graph/Functions.hlsl"
    #include "UnityShaderUtilities.cginc"

    #ifdef UNITY_PASS_META
        #include "UnityMetaPass.cginc"
    #endif
#endif

#ifdef PIPELINE_URP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        #define VARYINGS_NEED_SHADOWCOORD
    #endif
    #ifdef UNITY_PASS_META
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
    #endif
#endif

#if USE_EXTERNAL_CORERP
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#else
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/UnityInstancing.hlsl"
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/SpaceTransforms.hlsl"
#endif

static float3 DebugColor = 0;

#define CustomLightData UnityLightData
struct CustomLightData
{
    half3 color;
    float3 direction;
    half attenuation;
};
    
struct ShaderData
{
    float3 normalWS;
    float3 bitangentWS;
    float3 tangentWS;
    float3 viewDirectionWS;
    half perceptualRoughness;
    half clampedRoughness;
    half NoV;
    half3 f0;
    half3 brdf;
    half3 energyCompensation;
    float3 reflectionDirection;
    half specularOcclusion;
};

struct GIData
{
    half3 IndirectDiffuse;
    half3 Light;
    half3 Reflections;
    half3 Specular;
};

// unity macros need workaround
struct LegacyAttributes
{
    float4 vertex;
    float3 normal;
};
struct LegacyVaryings
{
    float4 pos;
    float4 _ShadowCoord;
};

#ifdef GENERATION_GRAPH
    #define Albedo BaseColor
    #define NormalTS Normal

    #define Position VertexPosition
    #define Normal VertexNormal
    #define Tangent VertexTangent
#endif


#ifdef GENERATION_CODE

struct VertexDescription
{
    float3 VertexPosition;
    float3 VertexNormal;
    float3 VertexTangent;
};

#ifndef CUSTOM_VARYING0
#define CUSTOM_VARYING0
#endif
#ifndef CUSTOM_VARYING1
#define CUSTOM_VARYING1
#endif
#ifndef CUSTOM_VARYING2
#define CUSTOM_VARYING2
#endif
#ifndef CUSTOM_VARYING3
#define CUSTOM_VARYING3
#endif
#ifndef CUSTOM_VARYING4
#define CUSTOM_VARYING4
#endif
#ifndef CUSTOM_VARYING5
#define CUSTOM_VARYING5
#endif
#ifndef CUSTOM_VARYING6
#define CUSTOM_VARYING6
#endif

struct SurfaceDescription
{
    static SurfaceDescription ctor()
    {
        SurfaceDescription surfaceDescription;
        
        surfaceDescription.Albedo = 1.0;
        surfaceDescription.Normal = half3(0,0,1);
        surfaceDescription.Metallic = 0.0;
        surfaceDescription.Emission = half3(0.0, 0.0, 0.0);
        surfaceDescription.Smoothness = 0.5;
        surfaceDescription.Occlusion = 1.0;
        surfaceDescription.Alpha = 1.0;
        surfaceDescription.AlphaClipThreshold = 0.5;
        surfaceDescription.AlphaClipSharpness = 0.0001;
        surfaceDescription.Reflectance = 0.5;

        surfaceDescription.GSAAVariance = 0.15;
        surfaceDescription.GSAAThreshold = 0.1;

        surfaceDescription.Anisotropy = 0.0;
        surfaceDescription.Tangent = half3(1,1,1);
        surfaceDescription.SpecularOcclusion = 1.0;

        return surfaceDescription;
    }

    half3 Albedo;
    half3 Normal;
    half Metallic;
    half3 Emission;
    half Smoothness;
    half Occlusion;
    half Alpha;
    half AlphaClipThreshold;
    half AlphaClipSharpness;
    half Reflectance;
    half GSAAVariance;
    half GSAAThreshold;
    half3 Tangent;
    half Anisotropy;
    half SpecularOcclusion;
};

SurfaceDescription InitializeSurfaceDescription()
{
    return SurfaceDescription::ctor();
};


#ifdef VARYINGS_NEED_NORMAL
#define ATTRIBUTES_NEED_NORMAL
#endif
#ifdef VARYINGS_NEED_TANGENT
#define ATTRIBUTES_NEED_TANGENT
#endif
#ifdef VARYINGS_NEED_COLOR
#define ATTRIBUTES_NEED_COLOR
#endif
#ifdef VARYINGS_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD0
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD1
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_TEXCOORD2
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

struct Attributes
{
    float3 positionOS : POSITION;

    #if defined(ATTRIBUTES_NEED_NORMAL)
    float3 normalOS : NORMAL;
    #endif
    #if defined(ATTRIBUTES_NEED_TANGENT)
    float4 tangentOS : TANGENT;
    #endif

    #if defined(ATTRIBUTES_NEED_COLOR)
    float4 color : COLOR;
    #endif

    #if defined(ATTRIBUTES_NEED_TEXCOORD0)
    float4 uv0 : TEXCOORD0;
    #endif
    #if defined(ATTRIBUTES_NEED_TEXCOORD1)
    float4 uv1 : TEXCOORD1;
    #endif
    #if defined(ATTRIBUTES_NEED_TEXCOORD2)
    float4 uv2 : TEXCOORD2;
    #endif
    #if defined(ATTRIBUTES_NEED_TEXCOORD3)
    float4 uv3 : TEXCOORD3;
    #endif


    #if UNITY_ANY_INSTANCING_ENABLED
    uint instanceID : INSTANCEID_SEMANTIC;
    #endif

    #if defined(ATTRIBUTES_NEED_VERTEXID)
    uint vertexId : VERTEXID_SEMANTIC;
    #endif
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : POSITIONWS;

    #if defined(VARYINGS_NEED_NORMAL)
    float3 normalWS : NORMAL;
    #endif
    #if defined(VARYINGS_NEED_TANGENT)
    float4 tangentWS : TANGENT;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD0)
    float4 texCoord0 : TEXCOORD0;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD1)
    float4 texCoord1 : TEXCOORD1;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD2)
    float4 texCoord2 : TEXCOORD2;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD3)
    float4 texCoord3 : TEXCOORD3;
    #endif

    #if defined(VARYINGS_NEED_COLOR)
        #if defined(VARYINGS_NEED_COLOR_NOINTERP)
        nointerpolation
        #endif
        #if defined(VARYINGS_NEED_COLOR_CENTROID)
        centroid
        #endif
    float4 color : COLOR;
    #endif

    #if defined(VARYINGS_NEED_INTERP0)
        #if defined(VARYINGS_NEED_INTERP0_NOINTERP)
        nointerpolation
        #endif
        #if defined(VARYINGS_NEED_INTERP0_CENTROID)
        centroid
        #endif
    float4 interp0 : CUSTOMINTERP0;
    #endif

    #if defined(VARYINGS_NEED_INTERP1)
        #if defined(VARYINGS_NEED_INTERP1_NOINTERP)
        nointerpolation
        #endif
        #if defined(VARYINGS_NEED_INTERP1_CENTROID)
        centroid
        #endif
    float4 interp1 : CUSTOMINTERP1;
    #endif

    #if defined(VARYINGS_NEED_INTERP2)
        #if defined(VARYINGS_NEED_INTERP2_NOINTERP)
        nointerpolation
        #endif
        #if defined(VARYINGS_NEED_INTERP2_CENTROID)
        centroid
        #endif
    float4 interp2 : CUSTOMINTERP2;
    #endif

    #if defined(VARYINGS_NEED_INTERP3)
        #if defined(VARYINGS_NEED_INTERP3_NOINTERP)
        nointerpolation
        #endif
        #if defined(VARYINGS_NEED_INTERP3_CENTROID)
        centroid
        #endif
    float4 interp3 : CUSTOMINTERP3;
    #endif

    #if defined(VARYINGS_NEED_INTERP4)
        #if defined(VARYINGS_NEED_INTERP4_NOINTERP)
        nointerpolation
        #endif
        #if defined(VARYINGS_NEED_INTERP4_CENTROID)
        centroid
        #endif
    float4 interp4 : CUSTOMINTERP4;
    #endif

    CUSTOM_VARYING0
    CUSTOM_VARYING1
    CUSTOM_VARYING2
    CUSTOM_VARYING3
    CUSTOM_VARYING4
    CUSTOM_VARYING5
    CUSTOM_VARYING6

    #if defined(FOG_ANY)
    float fogCoord : FOG_COORD;
    #endif
    #if defined(VARYINGS_NEED_SHADOWCOORD)
    float4 shadowCoord : SHADOWCOORD;
    #endif
    #if UNITY_ANY_INSTANCING_ENABLED
    uint instanceID : SV_InstanceID;
    #endif
    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
    uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
    #endif
    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
    uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
    #endif


    #if defined(EDITOR_VISUALIZATION)
    float2 vizUV : VIZUV;
    #endif
    #if defined(EDITOR_VISUALIZATION)
    float4 lightCoord : LIGHTCOORD;
    #endif
    #if defined(LIGHTMAP_ON) && defined(DYNAMICLIGHTMAP_ON)
    centroid float4 lightmapUV : LIGHTMAPUV;
    #elif defined(LIGHTMAP_ON)
    centroid float2 lightmapUV : LIGHTMAPUV;
    #endif

    #ifdef VARYINGS_NEED_SH
        half3 sh : SHCOORD;
    #endif

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
        half3 vertexLight : VERTEXLIGHT;
    #endif

    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
    #endif
};
#endif // #ifdef GENERATION_CODE



// Need to paste this here because its not included
// Light falloff

float ftLightFalloff(float4x4 ftUnityLightMatrix, float3 worldPos)
{
    float3 lightCoord = mul(ftUnityLightMatrix, float4(worldPos, 1)).xyz / ftUnityLightMatrix._11;
    float distSq = dot(lightCoord, lightCoord);
    float falloff = saturate(1.0f - pow(sqrt(distSq) * ftUnityLightMatrix._11, 4.0f)) / (distSq + 1.0f);
    return falloff;
}

float ftLightFalloff(float4 lightPosRadius, float3 worldPos)
{
    float3 lightCoord = worldPos - lightPosRadius.xyz;
    float distSq = dot(lightCoord, lightCoord);
    float falloff = saturate(1.0f - pow(sqrt(distSq * lightPosRadius.w), 4.0f)) / (distSq + 1.0f);
    return falloff;
}

inline float4 ComputeGrabScreenPos(float4 pos)
{
	#if UNITY_UV_STARTS_AT_TOP
	float scale = -1.0;
	#else
	float scale = 1.0;
	#endif
	float4 o = pos * 0.5f;
	o.xy = float2(o.x, o.y*scale) + o.w;
#ifdef UNITY_SINGLE_PASS_STEREO
	o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
	o.zw = pos.zw;
	return o;
}