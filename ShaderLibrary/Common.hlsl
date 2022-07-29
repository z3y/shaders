#ifndef COMMON_INCLUDED
#define COMMON_INCLUDED
// include after v2f

// Partially taken from Google Filament, Xiexe, Catlike Coding and Unity
// https://google.github.io/filament/Filament.html
// https://github.com/Xiexe/Unity-Lit-Shader-Templates
// https://catlikecoding.com/

#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
#define GRAYSCALE float3(0.2125, 0.7154, 0.0721)
#define TAU 6.28318530718 // two pi
#define INV_HALF_PI     0.636619772367
#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
#define FLT_MIN         1.175494351e-38 // Minimum representable positive floating-point number
#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number

#include "SurfaceData.hlsl"
#include "EnvironmentBRDF.hlsl"
#include "Sampling.hlsl"

half _SpecularOcclusion;
half _specularAntiAliasingVariance;
half _specularAntiAliasingThreshold;

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
    float Attenuation;
};

SamplerState custom_bilinear_clamp_sampler;

half RemapMinMax(half value, half remapMin, half remapMax)
{
    return value * (remapMax - remapMin) + remapMin;
}

half InverseLerp(half a, half b, half value)
{
    return (value - a) / (b - a);
}

half RemapInverseLerp(half inMin, half inMax, half outMin, half outMax, half v)
{
    half t = InverseLerp(inMin, inMax, v);
    t = saturate(t);
    return lerp(outMin, outMax, t);
}

half RemapInverseLerp(half value, half remapMin, half remapMax)
{
    return RemapInverseLerp(remapMin, remapMax, 0, 1, value);
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

float3 BoxProjection(float3 direction, float3 position, float4 cubemapPosition, float3 boxMin, float3 boxMax)
{
    #if defined(UNITY_SPECCUBE_BOX_PROJECTION) || defined(FORCE_SPECCUBE_BOX_PROJECTION)
        UNITY_FLATTEN
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

half PerceptualRoughnessToRoughness(half perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

half PerceptualRoughnessToRoughnessClamped(half perceptualRoughness)
{
    half a = perceptualRoughness * perceptualRoughness;
    return max(a, 0.002);
}

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

float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
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

bool isReflectionProbe()
{
    // thx 3
    return unity_CameraProjection._m11 == 1 && UNITY_MATRIX_P[0][0] == 1;
}



#ifdef DYNAMICLIGHTMAP_ON
float3 RealtimeLightmap(float2 uv, float3 worldNormal)
{   
    half4 bakedCol = SampleBicubic(unity_DynamicLightmap, custom_bilinear_clamp_sampler, uv);
    float3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);
    #ifdef DIRLIGHTMAP_COMBINED
        half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
        realtimeLightmap += DecodeDirectionalLightmap (realtimeLightmap, realtimeDirTex, worldNormal);
    #endif
    return realtimeLightmap;
}
#endif

half3 SpecularHighlights(float3 worldNormal, half3 lightColor, float3 lightDirection, half3 f0, float3 viewDir, half clampedRoughness, half NoV, half3 energyCompensation)
{
    float3 halfVector = Unity_SafeNormalize(lightDirection + viewDir);

    half NoH = saturate(dot(worldNormal, halfVector));
    half NoL = saturate(dot(worldNormal, lightDirection));
    half LoH = saturate(dot(lightDirection, halfVector));

    half3 F = F_Schlick(LoH, f0) * energyCompensation;
    half D = D_GGX(NoH, clampedRoughness);
    half V = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);

    return max(0, (D * V) * F) * lightColor * NoL * UNITY_PI;
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


// noise functions from xiexe https://github.com/Xiexe/PBR_Standard_Dithered_GSAA_SLO
// the reason why this is used as an alternative to unity's dithering that is always present when there is post processing
// post processing doesnt handle black colors properly, especially visible with OLED displays, black colors are gray
#define MOD3 float3(443.8975,397.2973, 491.1871)
float ditherNoiseFuncLow(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * MOD3 + _Time.y);
    p3 += dot(p3, p3.yzx + 19.19);
    return frac((p3.x + p3.y) * p3.z);
}
float3 ditherNoiseFuncHigh(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * (MOD3 + _Time.y));
    p3 += dot(p3, p3.yxz + 19.19);
    return frac(float3((p3.x + p3.y)*p3.z, (p3.x + p3.z)*p3.y, (p3.y + p3.z)*p3.x));
}
// MIT License
// Copyright (c) 2018 Xiexe
// Copyright (c) 2018 TCL

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.



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

half3 GetF0(SurfaceData surf)
{
    return 0.16 * surf.reflectance * surf.reflectance * (1.0 - surf.metallic) + surf.albedo * surf.metallic;
}

half3 MainLightSpecular(LightData lightData, half NoV, half perceptualRoughness, half3 f0)
{
    half clampedRoughness = PerceptualRoughnessToRoughnessClamped(perceptualRoughness);
    half3 F = F_Schlick(lightData.LoH, f0) * DFGEnergyCompensation;
    half D = D_GGX(lightData.NoH, clampedRoughness);
    half V = V_SmithGGXCorrelated(NoV, lightData.NoL, clampedRoughness);

    return max(0.0, (D * V) * F) * lightData.FinalColor * UNITY_PI;
}

void InitializeMainLightData(inout LightData lightData, float3 normalWS, float3 viewDir, half NoV, half perceptualRoughness, half3 f0, v2f input)
{
    #ifdef USING_LIGHT_MULTI_COMPILE
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
            lightData.FinalColor *= UnityComputeForwardShadows(input.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw, input.worldPos, input._ShadowCoord);
        #endif

        lightData.Specular = MainLightSpecular(lightData, NoV, perceptualRoughness, f0);
    #else
        lightData = (LightData)0;
    #endif
}



half3 GetReflections(float3 normalWS, float3 positionWS, float3 viewDir, half3 f0, half NoV, SurfaceData surf, half3 indirectDiffuse)
{
    half roughness = PerceptualRoughnessToRoughness(surf.perceptualRoughness);
    half3 indirectSpecular = 0;
    #if defined(UNITY_PASS_FORWARDBASE)

        float3 reflDir = reflect(-viewDir, normalWS);
        #ifndef SHADER_API_MOBILE
        reflDir = lerp(reflDir, normalWS, roughness * roughness);
        #endif

        Unity_GlossyEnvironmentData envData;
        envData.roughness = surf.perceptualRoughness;
        envData.reflUVW = BoxProjection(reflDir, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);

        half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
        indirectSpecular = probe0;

        #ifndef _BLENDREFLECTIONPROBES_OFF
        #if defined(UNITY_SPECCUBE_BLENDING)
            UNITY_BRANCH
            if (unity_SpecCube0_BoxMin.w < 0.99999)
            {
                envData.reflUVW = BoxProjection(reflDir, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
                float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
                indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
            }
        #endif
        #endif

        float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
        indirectSpecular *= horizon * horizon;
        
        //#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
            surf.occlusion *= lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), _SpecularOcclusion);
        //#endif

        indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);
    #endif

    return indirectSpecular;
}

half3 GetLightProbes(float3 normalWS, float3 positionWS)
{
    half3 indirectDiffuse = 0;
    #ifndef LIGHTMAP_ON
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
                    indirectDiffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normalWS);
                    indirectDiffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normalWS);
                    indirectDiffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normalWS);
                #else
                indirectDiffuse = ShadeSH9(float4(normalWS, 1.0));
                #endif
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            }
        #endif
    #endif
    return indirectDiffuse;
}

#include "Bakery.hlsl"

half3 GetIndirectDiffuseAndSpecular(v2f i, SurfaceData surf, inout half3 directSpecular, half3 f0, float3 worldNormal, float3 viewDir, half NoV, inout LightData lightData)
{
    half3 lightmappedSpecular = 0;
    half3 indirectDiffuse = 0;
    #ifdef UNITY_PASS_FORWARDBASE
        #if defined(LIGHTMAP_ON)

            float2 lightmapUV = i.uv[1].xy * unity_LightmapST.xy + unity_LightmapST.zw;
            half4 bakedColorTex = SampleBicubic(unity_Lightmap, custom_bilinear_clamp_sampler, lightmapUV, GetTexelSize(unity_Lightmap));
            half3 lightMap = DecodeLightmap(bakedColorTex);

            #ifdef BAKERY_RNM
                BakeryRNMLightmapAndSpecular(lightMap, lightmapUV, lightmappedSpecular, surf.tangentNormal, i.viewDirTS, viewDir, PerceptualRoughnessToRoughnessClamped(surf.perceptualRoughness), f0);
            #endif

            #ifdef BAKERY_SH
                BakerySHLightmapAndSpecular(lightMap, lightmapUV, lightmappedSpecular, worldNormal, viewDir, PerceptualRoughnessToRoughnessClamped(surf.perceptualRoughness), f0);
            #endif

            #if defined(DIRLIGHTMAP_COMBINED)
                float4 lightMapDirection = unity_LightmapInd.Sample(custom_bilinear_clamp_sampler, lightmapUV);
                lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, worldNormal);
            #endif

            indirectDiffuse = lightMap;
        #endif

        #if defined(DYNAMICLIGHTMAP_ON)
            float2 realtimeUV = i.uv[2] * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw
            float3 realtimeLightMap = getRealtimeLightmap(realtimeUV, worldNormal);
            indirectDiffuse += realtimeLightMap; 
        #endif
        
#ifdef LIGHTMAP_ON
        #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
            lightData.FinalColor = 0.0;
            lightData.Specular = 0.0;
            indirectDiffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap (indirectDiffuse, lightData.Attenuation, bakedColorTex, worldNormal);
        #endif
#endif

        #if !defined(DYNAMICLIGHTMAP_ON) && !defined(LIGHTMAP_ON)
            #ifdef LIGHTPROBE_VERTEX
                indirectDiffuse = ShadeSHPerPixel(worldNormal, i.lightProbe, i.worldPos.xyz);
            #else
                indirectDiffuse = GetLightProbes(worldNormal, i.worldPos.xyz);
            #endif
        #endif

        indirectDiffuse = max(0.0, indirectDiffuse);


        #if defined(_LIGHTMAPPED_SPECULAR)
        {
            float3 bakedDominantDirection = 1.0;
            half3 bakedSpecularColor = 0.0;

            #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON) && !defined(BAKERY_SH) && !defined(BAKERY_RNM)
                bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
                bakedSpecularColor = indirectDiffuse;
            #endif

            #ifndef LIGHTMAP_ON
                bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
            #endif

            bakedDominantDirection = normalize(bakedDominantDirection);
            lightmappedSpecular += SpecularHighlights(worldNormal, bakedSpecularColor, bakedDominantDirection, f0, viewDir, PerceptualRoughnessToRoughnessClamped(surf.perceptualRoughness), NoV, DFGEnergyCompensation);
        }
        #endif

    #endif

   

    directSpecular += lightmappedSpecular;
    return indirectDiffuse;
}

#include "Parallax.hlsl"
#include "NonImportantLights.hlsl"
#include "BlendModes.hlsl"
#include "ACES.hlsl"

#endif