#pragma warning (disable : 1519)

#if defined(UNITY_INSTANCING_ENABLED) || defined(STEREO_INSTANCING_ON) || defined(INSTANCING_ON)
    #define UNITY_ANY_INSTANCING_ENABLED 1
#endif

#ifdef STEREO_INSTANCING_ON
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #define FOG_ANY
#endif

#if defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING)
#define VARYINGS_NEED_SHADOWCOORD
#define VARYINGS_NEED_TEXCOORD1
#endif

#ifdef LIGHTMAP_ON
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#ifdef DYNAMICLIGHTMAP_ON
#define ATTRIBUTES_NEED_TEXCOORD2
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


#define USE_EXTERNAL_CORERP 0

#if USE_EXTERNAL_CORERP
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#else
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/Color.hlsl"
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/Packing.hlsl"
    // #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/EntityLighting.hlsl"
// #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/Texture.hlsl"
#endif


#ifdef PIPELINE_BUILTIN
    
    #ifdef FORCE_SPECCUBE_BOX_PROJECTION
        #define UNITY_SPECCUBE_BOX_PROJECTION
    #endif
    
    #include "Packages/com.z3y.shaders/ShaderLibrary/UnityCG/ShaderVariablesMatrixDefsLegacyUnity.hlsl"
    
    #undef GLOBAL_CBUFFER_START // dont need reg
    #define GLOBAL_CBUFFER_START(name) CBUFFER_START(name)
    #include "UnityShaderVariables.cginc"
    half4 _LightColor0;
    half4 _SpecColor;
    #include "Packages/com.z3y.shaders/ShaderLibrary/UnityCG/UnityCGSupport.hlsl"
    #include "AutoLight.cginc"

    #include "Packages/com.z3y.shaders/ShaderLibrary/Graph/Functions.hlsl"
    #include "UnityShaderUtilities.cginc"

    #ifdef UNITY_PASS_META
        #include "UnityMetaPass.cginc"
    #endif
#endif

#ifdef PIPELINE_URP
#define Unity_SafeNormalize SafeNormalize
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        #define VARYINGS_NEED_SHADOWCOORD
    #endif
#endif

#if USE_EXTERNAL_CORERP
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#else
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/UnityInstancing.hlsl"
    #include "Packages/com.z3y.shaders/ShaderLibrary/CoreRP/SpaceTransforms.hlsl"
#endif


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
};

#ifdef GENERATION_CODE

struct VertexDescription
{
    float3 VertexPosition;
    float3 VertexNormal;
    float3 VertexTangent;
};

struct SurfaceDescription
{
    half3 Albedo;
    half3 Normal;
    half Metallic;
    half3 Emission;
    half Smoothness;
    half Occlusion;
    half Alpha;
    half AlphaClipThreshold;
    half Reflectance;
    half GSAAVariance;
    half GSAAThreshold;
    half3 Tangent;
    half Anisotropy;
};

SurfaceDescription InitializeSurfaceDescription()
{
    SurfaceDescription surfaceDescription = (SurfaceDescription)0;
    
    surfaceDescription.Albedo = float(1);
    surfaceDescription.Normal = float3(0,0,1);
    surfaceDescription.Metallic = float(0);
    surfaceDescription.Emission = float(0);
    surfaceDescription.Smoothness = float(0.5);
    surfaceDescription.Occlusion = float(1);
    surfaceDescription.Alpha = float(1);
    surfaceDescription.AlphaClipThreshold = float(0.5);
    surfaceDescription.Reflectance = float(0.5);

    surfaceDescription.GSAAVariance = float(0.15);
    surfaceDescription.GSAAThreshold = float(0.1);

    surfaceDescription.Anisotropy = float(0);
    surfaceDescription.Tangent = float3(0,0,1);

    return surfaceDescription;
}

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
    uint vertexId : SV_VertexID;
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
    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
    uint cullFace : FRONT_FACE_SEMANTIC;
    #endif

    #if defined(EDITOR_VISUALIZATION)
    float2 vizUV : VIZUV;
    #endif
    #if defined(EDITOR_VISUALIZATION)
    float4 lightCoord : LIGHTCOORD;
    #endif
    #if defined(LIGHTMAP_ON) && defined(DYNAMICLIGHTMAP_ON)
    float4 lightmapUV : LIGHTMAPUV;
    #elif defined(LIGHTMAP_ON)
    float2 lightmapUV : LIGHTMAPUV;
    #endif
};
#endif // #ifdef GENERATION_CODE

CustomLightData GetCustomMainLightData(Varyings unpacked)
{
    CustomLightData data = (CustomLightData)0;

    #if defined(PIPELINE_BUILTIN) && defined(USING_LIGHT_MULTI_COMPILE)
        data.direction = Unity_SafeNormalize(UnityWorldSpaceLightDir(unpacked.positionWS));
        data.color = _LightColor0.rgb;

        // attenuation
        // my favorite macro from UnityCG /s
        LegacyVaryings legacyVaryings = (LegacyVaryings)0;
        legacyVaryings.pos = unpacked.positionCS;
        #ifdef VARYINGS_NEED_SHADOWCOORD
        legacyVaryings._ShadowCoord = unpacked.shadowCoord;
        #endif
        UNITY_LIGHT_ATTENUATION(lightAttenuation, legacyVaryings, unpacked.positionWS.xyz);

        #if defined(UNITY_PASS_FORWARDBASE) && !defined(SHADOWS_SCREEN)
            lightAttenuation = 1.0;
        #endif
        data.attenuation = lightAttenuation;
    
        #if defined(LIGHTMAP_SHADOW_MIXING) && defined(LIGHTMAP_ON)
            data.color *= UnityComputeForwardShadows(unpacked.lightmapUV.xy, unpacked.positionWS, unpacked.shadowCoord);
        #endif
    
    #endif

    #if defined(PIPELINE_URP)

        #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            float4 shadowCoord = unpacked.shadowCoord;
        #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
            float4 shadowCoord = TransformWorldToShadowCoord(unpacked.positionWS);
        #else
            float4 shadowCoord = float4(0, 0, 0, 0);
        #endif

        Light mainLight = GetMainLight(shadowCoord);

        data.color = mainLight.color;
        data.direction = mainLight.direction;
        data.attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
    #endif

    return data;
}

float3 GetViewDirectionWS(float3 positionWS)
{
    #ifdef PIPELINE_BUILTIN
        return normalize(UnityWorldSpaceViewDir(positionWS));
    #else
        return normalize(GetCameraPositionWS() - positionWS);
    #endif
}