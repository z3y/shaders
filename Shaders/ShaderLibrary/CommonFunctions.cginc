#ifndef COMMON_FUNCTIONS_INCLUDED
#define COMMON_FUNCTIONS_INCLUDED


// Partially taken from Google Filament, Xiexe, Catlike Coding and Unity
// https://google.github.io/filament/Filament.html
// https://github.com/Xiexe/Unity-Lit-Shader-Templates
// https://catlikecoding.com/

#define GRAYSCALE float3(0.2125, 0.7154, 0.0721)
#define TAU float(6.28318530718)
#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))

#include "SurfaceData.cginc"
#include "BicubicSampling.cginc"

struct appdata_all
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float4 uv2 : TEXCOORD2;
    float4 uv3 : TEXCOORD3;
    float4 tangent : TANGENT;
    half4 color : COLOR;

    uint vertexId : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#include "EnvironmentBRDF.cginc"


#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    #define LIGHTMAP_ANY
#endif

#ifdef LIGHTMAP_ANY
    #if defined(BAKERY_RNM) || defined(BAKERY_SH) || defined(BAKERY_VERTEXLM)
        #define BAKERYLM_ENABLED
        #undef DIRLIGHTMAP_COMBINED
    #endif
#else
    #undef BAKERY_SH
    #undef BAKERY_RNM
#endif

half RemapMinMax(half value, half remapMin, half remapMax)
{
    return value * (remapMax - remapMin) + remapMin;
}

float pow5(float x)
{
    float x2 = x * x;
    return x2 * x2 * x;
}

float sq(float x)
{
    return x * x;
}

half3 F_Schlick(half u, half3 f0)
{
    return f0 + (1.0 - f0) * pow(1.0 - u, 5.0);
}

float F_Schlick(float f0, float f90, float VoH)
{
    return f0 + (f90 - f0) * pow5(1.0 - VoH);
}

half Fd_Burley(half roughness, half NoV, half NoL, half LoH)
{
    // Burley 2012, "Physically-Based Shading at Disney"
    half f90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float lightScatter = F_Schlick(1.0, f90, NoL);
    float viewScatter  = F_Schlick(1.0, f90, NoV);
    return lightScatter * viewScatter;
}

float3 UnpackScaleNormalHemiOctahedron(float4 normalMap, float bumpScale)
{
    #if defined(SHADER_API_MOBILE)
        return UnpackScaleNormal(normalMap, bumpScale).xyz;
    #endif
    // https://twitter.com/Stubbesaurus/status/937994790553227264
    half2 f = normalMap.ag * 2.0 - 1.0;
    normalMap.xyz = float3(f.x, f.y, 1 - abs(f.x) - abs(f.y));
    float t = saturate(-normalMap.z);
    normalMap.xy += normalMap.xy >= 0.0 ? -t : t;
    normalMap.xy *= bumpScale;
    return normalize(normalMap);
}


float3 getBoxProjection (float3 direction, float3 position, float4 cubemapPosition, float3 boxMin, float3 boxMax)
{
    #if defined(UNITY_SPECCUBE_BOX_PROJECTION)
        if (cubemapPosition.w > 0.0)
        {
            float3 factors = ((direction > 0.0 ? boxMax : boxMin) - position) / direction;
            float scalar = min(min(factors.x, factors.y), factors.z);
            direction = direction * scalar + (position - cubemapPosition.xyz);
        }
    #endif

    return direction;
}

half computeSpecularAO(half NoV, half ao, half roughness)
{
    return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
}

half D_GGX(half NoH, half roughness)
{
    half a = NoH * roughness;
    half k = roughness / (1.0 - NoH * NoH + a * a);
    return k * k * (1.0 / UNITY_PI);
}

float V_SmithGGXCorrelatedFast(half NoV, half NoL, half roughness) {
    half a = roughness;
    float GGXV = NoL * (NoV * (1.0 - a) + a);
    float GGXL = NoV * (NoL * (1.0 - a) + a);
    return 0.5 / (GGXV + GGXL);
}

float V_SmithGGXCorrelated(half NoV, half NoL, half roughness)
{
    #ifdef SHADER_API_MOBILE
    return V_SmithGGXCorrelatedFast(NoV, NoL, roughness);
    #else
    half a2 = roughness * roughness;
    float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    return 0.5 / (GGXV + GGXL);
    #endif
}

half V_Kelemen(half LoH)
{
    // Kelemen 2001, "A Microfacet Based Coupled Specular-Matte BRDF Model with Importance Sampling"
    return saturate(0.25 / (LoH * LoH));
}

half _specularAntiAliasingVariance;
half _specularAntiAliasingThreshold;
float GSAA_Filament(float3 worldNormal, float perceptualRoughness)
{
    // Kaplanyan 2016, "Stable specular highlights"
    // Tokuyoshi 2017, "Error Reduction and Simplification for Shading Anti-Aliasing"
    // Tokuyoshi and Kaplanyan 2019, "Improved Geometric Specular Antialiasing"

    // This implementation is meant for deferred rendering in the original paper but
    // we use it in forward rendering as well (as discussed in Tokuyoshi and Kaplanyan
    // 2019). The main reason is that the forward version requires an expensive transform
    // of the half vector by the tangent frame for every light. This is therefore an
    // approximation but it works well enough for our needs and provides an improvement
    // over our original implementation based on Vlachos 2015, "Advanced VR Rendering".

    float3 du = ddx(worldNormal);
    float3 dv = ddy(worldNormal);

    float variance = _specularAntiAliasingVariance * (dot(du, du) + dot(dv, dv));

    float roughness = perceptualRoughness * perceptualRoughness;
    float kernelRoughness = min(2.0 * variance, _specularAntiAliasingThreshold);
    float squareRoughness = saturate(roughness * roughness + kernelRoughness);

    return sqrt(sqrt(squareRoughness));
}

float shEvaluateDiffuseL1Geomerics_local(float L0, float3 L1, float3 n)
{
    // average energy
    float R0 = L0;
    
    // avg direction of incoming light
    float3 R1 = 0.5f * L1;
    
    // directional brightness
    float lenR1 = length(R1);
    
    // linear angle between normal and direction 0-1
    //float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
    //float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
    float q = dot(normalize(R1), n) * 0.5 + 0.5;
    q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
    
    // power for q
    // lerps from 1 (linear) to 3 (cubic) based on directionality
    float p = 1.0f + 2.0f * lenR1 / R0;
    
    // dynamic range constant
    // should vary between 4 (highly directional) and 0 (ambient)
    float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
    
    return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
}


#ifdef DYNAMICLIGHTMAP_ON
float3 getRealtimeLightmap(float2 uv, float3 worldNormal)
{   
    half4 bakedCol = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, uv);
    float3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);

    #ifdef DIRLIGHTMAP_COMBINED
        half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
        realtimeLightmap += DecodeDirectionalLightmap (realtimeLightmap, realtimeDirTex, worldNormal);
    #endif

    return realtimeLightmap;
}
#endif

half3 GetSpecularHighlights(float3 worldNormal, half3 lightColor, float3 lightDirection, half3 f0, float3 viewDir, half clampedRoughness, half NoV, half3 energyCompensation)
{
    float3 halfVector = Unity_SafeNormalize(lightDirection + viewDir);

    half NoH = saturate(dot(worldNormal, halfVector));
    half NoL = saturate(dot(worldNormal, lightDirection));
    half LoH = saturate(dot(lightDirection, halfVector));

    half3 F = F_Schlick(LoH, f0);
    half D = D_GGX(NoH, clampedRoughness);
    half V = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);

    #ifndef SHADER_API_MOBILE
    F *= energyCompensation;
    #endif

    return max(0, (D * V) * F) * lightColor * NoL;
}

float Unity_Dither(float In, float2 ScreenPosition)
{
    float2 uv = ScreenPosition * _ScreenParams.xy;
    const half4 DITHER_THRESHOLDS[4] =
    {
        half4(1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0),
        half4(13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0),
        half4(4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0),
        half4(16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0)
    };

    return In - DITHER_THRESHOLDS[uint(uv.x) % 4][uint(uv.y) % 4];
}

void AACutout(inout half alpha, half cutoff)
{
    alpha = (alpha - cutoff) / max(fwidth(alpha), 0.0001) + 0.5;
}

void FlipBTN(uint facing, inout float3 worldNormal, inout float3 bitangent, inout float3 tangent)
{
    #if !defined(LIGHTMAP_ON)
        UNITY_FLATTEN
        if (!facing)
        {
            worldNormal *= -1.0;
            bitangent *= -1.0;
            tangent *= -1.0;
        }
    #endif
}

void TangentToWorldNormal(float3 normalTS, inout float3 normalWS, inout float3 tangent, inout float3 bitangent)
{
    normalWS = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normalWS);
    tangent = normalize(cross(normalWS, bitangent));
    bitangent = normalize(cross(normalWS, tangent));
}

half NormalDotViewDir(float3 normalWS, float3 viewDir)
{
    return abs(dot(normalWS, viewDir)) + 1e-5f;
}

half3 GetF0(half reflectance, half metallic, half3 albedo)
{
    return 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo * metallic;
}

struct LightData
{
    half3 Color;
    float3 Direction;
    half NoL;
    half LoH;
    half NoH;
    float3 HalfVector;
    half3 FinalColor;
    half3 Specular;
    half Attenuation;
};

half3 MainLightSpecular(LightData lightData, half NoV, half clampedRoughness, half3 f0)
{
    half3 F = F_Schlick(lightData.LoH, f0) * DFGEnergyCompensation;
    half D = D_GGX(lightData.NoH, clampedRoughness);
    half V = V_SmithGGXCorrelated(NoV, lightData.NoL, clampedRoughness);

    return max(0.0, (D * V) * F) * lightData.FinalColor;
}

#if defined(UNITY_PASS_FORWARDBASE) && defined(DIRECTIONAL) && !(defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING))
    #define BRANCH_DIRECTIONAL

    #ifdef SPECULAR_HIGHLIGHTS_OFF
        #undef BRANCH_DIRECTIONAL
    #endif
#endif

void InitializeLightData(inout LightData lightData, float3 normalWS, float3 viewDir, half NoV, half clampedRoughness, half perceptualRoughness, half3 f0, v2f input)
{
    #ifdef USING_LIGHT_MULTI_COMPILE
        #ifdef BRANCH_DIRECTIONAL
        UNITY_BRANCH
        if (any(_WorldSpaceLightPos0.xyz))
        {
        //printf("directional branch");
        #endif
            lightData.Direction = normalize(UnityWorldSpaceLightDir(input.worldPos));
            lightData.HalfVector = Unity_SafeNormalize(lightData.Direction + viewDir);
            lightData.NoL = saturate(dot(normalWS, lightData.Direction));
            lightData.LoH = saturate(dot(lightData.Direction, lightData.HalfVector));
            lightData.NoH = saturate(dot(normalWS, lightData.HalfVector));
            
            UNITY_LIGHT_ATTENUATION(lightAttenuation, input, input.worldPos.xyz);
            lightData.Attenuation = lightAttenuation;
            lightData.Color = lightAttenuation * _LightColor0.rgb;
            lightData.FinalColor = (lightData.NoL * lightData.Color);

            


            #ifndef SHADER_API_MOBILE
                lightData.FinalColor *= Fd_Burley(perceptualRoughness, NoV, lightData.NoL, lightData.LoH);
            #endif

            #if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
                lightData.FinalColor *= UnityComputeForwardShadows(input.uv[1].zw, input.worldPos, input.screenPos);
            #endif

            lightData.Specular = MainLightSpecular(lightData, NoV, clampedRoughness, f0);
        #ifdef BRANCH_DIRECTIONAL
        }
        else
        {
            lightData = (LightData)0;
        }
        #endif
    #else
        lightData = (LightData)0;
    #endif
}

half3 GetReflections(float3 normalWS, float3 positionWS, float3 viewDir, half3 f0, half roughness, half NoV, SurfaceData surf, half3 indirectDiffuse)
{
    half3 indirectSpecular = 0;
    #if defined(UNITY_PASS_FORWARDBASE)

        float3 reflDir = reflect(-viewDir, normalWS);
        reflDir = lerp(reflDir, normalWS, roughness * roughness);

        Unity_GlossyEnvironmentData envData;
        envData.roughness = surf.perceptualRoughness;
        envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);

        half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
        indirectSpecular = probe0;

        #if defined(UNITY_SPECCUBE_BLENDING) && !defined(SHADER_API_MOBILE)
            UNITY_BRANCH
            if (unity_SpecCube0_BoxMin.w < 0.99999)
            {
                envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
                float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
                indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
            }
        #endif

        float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
        float2 dfg = DFGLut;
        #ifdef LIGHTMAP_ANY
            dfg.x *= lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), _SpecularOcclusion);
        #endif
        indirectSpecular = indirectSpecular * horizon * horizon * DFGEnergyCompensation * EnvBRDFMultiscatter(dfg, f0);
        indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);

    #endif

    return indirectSpecular;
}

half3 GetLightProbes(float3 normalWS, float3 positionWS)
{
    half3 indirectDiffuse = 0;
    #ifndef LIGHTMAP_ANY
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            UNITY_BRANCH
            if (unity_ProbeVolumeParams.x == 1.0)
            {
                indirectDiffuse = SHEvalLinearL0L1_SampleProbeVolume(float4(normalWS, 1.0), positionWS);
            }
            else
            {
        #endif
                #ifdef NONLINEAR_LIGHTPROBESH
                    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                    indirectDiffuse.r = shEvaluateDiffuseL1Geomerics_local(L0.r, unity_SHAr.xyz, normalWS);
                    indirectDiffuse.g = shEvaluateDiffuseL1Geomerics_local(L0.g, unity_SHAg.xyz, normalWS);
                    indirectDiffuse.b = shEvaluateDiffuseL1Geomerics_local(L0.b, unity_SHAb.xyz, normalWS);
                #else
                    indirectDiffuse = ShadeSH9(float4(normalWS, 1.0));
                #endif
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            }
        #endif
    #endif
    return indirectDiffuse;
}


#include "Bakery.cginc"


#endif