namespace Filament
{
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

    half computeSpecularAO(half NoV, half ao, half roughness)
    {
        return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
    }

    half D_GGX(half NoH, half roughness)
    {
        half a = NoH * roughness;
        half k = roughness / (1.0 - NoH * NoH + a * a);
        return k * k * (1.0 / PI);
    }

    float D_GGX_Anisotropic(float NoH, float3 h, float3 t, float3 b, float at, float ab)
    {
        half ToH = dot(t, h);
        half BoH = dot(b, h);
        half a2 = at * ab;
        float3 v = float3(ab * ToH, at * BoH, a2 * NoH);
        float v2 = dot(v, v);
        half w2 = a2 / v2;
        return a2 * w2 * w2 * (1.0 / PI);
    }


    float V_SmithGGXCorrelatedFast(half NoV, half NoL, half roughness)
    {
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

    float V_SmithGGXCorrelated_Anisotropic(float at, float ab, float ToV, float BoV, float ToL, float BoL, float NoV, float NoL)
    {
        float lambdaV = NoL * length(float3(at * ToV, ab * BoV, NoV));
        float lambdaL = NoV * length(float3(at * ToL, ab * BoL, NoL));
        float v = 0.5 / (lambdaV + lambdaL);
        return saturate(v);
    }

    half V_Kelemen(half LoH)
    {
        // Kelemen 2001, "A Microfacet Based Coupled Specular-Matte BRDF Model with Importance Sampling"
        return saturate(0.25 / (LoH * LoH));
    }

    
    half GeometricSpecularAA(float3 worldNormal, half perceptualRoughness, half varianceIn, half thresholdIn)
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

        half variance = varianceIn * (dot(du, du) + dot(dv, dv));

        half roughness = perceptualRoughness * perceptualRoughness;
        half kernelRoughness = min(2.0 * variance, thresholdIn);
        half squareRoughness = saturate(roughness * roughness + kernelRoughness);

        return sqrt(sqrt(squareRoughness));
    }
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

half3 EnvironmentBRDFApproximation(half perceptualRoughness, half NoV, half3 f0)
{
    // original code from https://blog.selfshadow.com/publications/s2013-shading-course/lazarov/s2013_pbs_black_ops_2_notes.pdf
    half g = 1 - perceptualRoughness;
    half4 t = half4(1 / 0.96, 0.475, (0.0275 - 0.25 * 0.04) / 0.96, 0.25);
    t *= half4(g, g, g, g);
    t += half4(0.0, 0.0, (0.015 - 0.75 * 0.04) / 0.96, 0.75);
    half a0 = t.x * min(t.y, exp2(-9.28 * NoV)) + t.z;
    half a1 = t.w;
    return saturate(lerp(a0, a1, f0));
}

#ifndef SHADER_API_MOBILE
TEXTURE2D(_DFG);
SAMPLER(sampler_DFG);
#endif

void EnvironmentBRDF(half NoV, half perceptualRoughness, half3 f0, out half3 brdf, out half3 energyCompensation)
{
    #ifdef SHADER_API_MOBILE
        energyCompensation = 1.0;
        brdf = EnvironmentBRDFApproximation(perceptualRoughness, NoV, f0);
    #else
        float2 dfg = _DFG.SampleLevel(sampler_DFG, float2(NoV, perceptualRoughness), 0).rg;
        brdf = lerp(dfg.xxx, dfg.yyy, f0);
        energyCompensation = 1.0 + f0 * (1.0 / dfg.y - 1.0);
    #endif
}

SamplerState custom_bilinear_clamp_sampler;

#include "Bicubic.hlsl"

#ifdef DYNAMICLIGHTMAP_ON
half3 RealtimeLightmap(float2 uv, float3 worldNormal)
{   
    //half4 bakedCol = SampleBicubic(unity_DynamicLightmap, custom_bilinear_clamp_sampler, uv);
    half4 bakedCol = BicubicSampling::SampleBicubic(unity_DynamicLightmap, custom_bilinear_clamp_sampler, uv, BicubicSampling::GetTexelSize(unity_DynamicLightmap));
    
    half3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);
    #ifdef DIRLIGHTMAP_COMBINED
        float4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
        realtimeLightmap += DecodeDirectionalLightmap(realtimeLightmap, realtimeDirTex, worldNormal);
    #endif
    return realtimeLightmap;
}
#endif

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

#ifdef DISABLE_LIGHT_PROBE_PROXY_VOLUME
    #define UNITY_LIGHT_PROBE_PROXY_VOLUME 0
#endif

half3 GetLightProbes(float3 normalWS, float3 positionWS)
{
    #if !defined(UNITY_PASS_FORWARDBASE) && defined(PIPELINE_BUILTIN)
        return 0.0f;
    #endif

    half3 indirectDiffuse = 0;
    #if !defined(DYNAMICLIGHTMAP_ON) && !defined(LIGHTMAP_ON) && defined(UNITY_PASS_FORWARDBASE)
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

    #if defined(PIPELINE_URP)
        indirectDiffuse = SampleSH(normalWS);
    #endif

    indirectDiffuse = max(0.0, indirectDiffuse);
    return indirectDiffuse;
}

#ifdef PIPELINE_BUILTIN


#endif

#include "Bakery.hlsl"

#ifdef LOD_FADE_CROSSFADE
    TEXTURE2D(BlueNoiseCrossFade);
    SAMPLER(samplerBlueNoiseCrossFade);
    void UnityApplyBlueNoiseDitherCrossFade(float2 vpos)
    {
        vpos /= 16.; // the dither mask texture is 16x16
        vpos.y = frac(vpos.y) * 0.0625 + unity_LODFade.y; // quantized lod fade by 16 levels
        clip(SAMPLE_TEXTURE2D(BlueNoiseCrossFade, samplerBlueNoiseCrossFade, vpos).r - 0.5);
    }

    #ifdef UNITY_DITHER_CROSSFADE
        sampler2D _DitherMaskLOD2D;
        void UnityApplyDitherCrossFade(float2 vpos)
        {
            vpos /= 4; // the dither mask texture is 4x4
            vpos.y = frac(vpos.y) * 0.0625 /* 1/16 */ + unity_LODFade.y; // quantized lod fade by 16 levels
            clip(tex2D(_DitherMaskLOD2D, vpos).a - 0.5);
        }
    #endif
    #ifdef UNITY_DITHER_CROSSFADE
        #define UNITY_APPLY_DITHER_CROSSFADE(vpos)  UnityApplyDitherCrossFade(vpos)
    #else
        #define UNITY_APPLY_DITHER_CROSSFADE(vpos)  UnityApplyBlueNoiseDitherCrossFade(vpos)
    #endif

#else
    #define UNITY_APPLY_DITHER_CROSSFADE(vpos)
#endif