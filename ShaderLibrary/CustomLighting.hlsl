#include "Packages/com.z3y.shaders/ShaderLibrary/LightFunctions.hlsl"

namespace CustomLighting
{

    void ApplyAlphaClip(inout SurfaceDescription surfaceDescription)
    {
        #if defined(_ALPHATEST_ON) && defined(ALPHATOCOVERAGE_ON)
            surfaceDescription.Alpha = (surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold) / max(fwidth(surfaceDescription.Alpha), 0.01f) + 0.5f;
        #endif

        #if defined(_ALPHATEST_ON) && !defined(ALPHATOCOVERAGE_ON)
            clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
        #endif
    }

    void GetBTN(Varyings unpacked, SurfaceDescription surfaceDescription, out float3 normalWS, out float3 bitangentWS, out float3 tangentWS)
    {
        //TODO: define in generator when it gets properly supported
        #define _NORMAL_DROPOFF_TS 1

        #if _NORMAL_DROPOFF_TS
            // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
            float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
            bitangentWS = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
            half3x3 tangentToWorld = half3x3(unpacked.tangentWS.xyz, bitangentWS, unpacked.normalWS.xyz);
            normalWS = TransformTangentToWorld(surfaceDescription.Normal, tangentToWorld);
            tangentWS = unpacked.tangentWS.xyz;

            #ifdef _ANISOTROPY
                tangentWS = TransformTangentToWorld(surfaceDescription.Tangent, tangentToWorld);
                tangentWS = Orthonormalize(tangentWS, normalWS);
                bitangentWS = normalize(cross(normalWS, tangentWS));
            #endif

            normalWS = normalize(normalWS);

        #elif _NORMAL_DROPOFF_OS
            normalWS = TransformObjectToWorldNormal(surfaceDescription.Normal);
        #elif _NORMAL_DROPOFF_WS
            normalWS = surfaceDescription.Normal;
        #endif
    }

    float3 GetViewDirectionWS(float3 positionWS)
    {
        #ifdef PIPELINE_BUILTIN
            return normalize(UnityWorldSpaceViewDir(positionWS));
        #else
            return normalize(GetCameraPositionWS() - positionWS);
        #endif
    }

    
    ShaderData InitializeShaderData(Varyings unpacked, SurfaceDescription surfaceDescription)
    {
        #ifdef _GEOMETRICSPECULAR_AA
            half rough = 1.0f - surfaceDescription.Smoothness;
            rough = Filament::GeometricSpecularAA(unpacked.normalWS, rough, surfaceDescription.GSAAVariance, surfaceDescription.GSAAThreshold);
            surfaceDescription.Smoothness = 1.0f - rough;
        #endif

        ShaderData sd = (ShaderData)0;
        GetBTN(unpacked, surfaceDescription, sd.normalWS, sd.bitangentWS, sd.tangentWS);

        sd.viewDirectionWS = GetViewDirectionWS(unpacked.positionWS);
        sd.NoV = abs(dot(sd.normalWS, sd.viewDirectionWS)) + 1e-5f;

        sd.perceptualRoughness = 1.0f - surfaceDescription.Smoothness;
        sd.f0 = 0.16 * surfaceDescription.Reflectance * surfaceDescription.Reflectance * (1.0 - surfaceDescription.Metallic) + surfaceDescription.Albedo * surfaceDescription.Metallic;

        EnvironmentBRDF(sd.NoV, sd.perceptualRoughness, sd.f0, sd.brdf, sd.energyCompensation);

        return sd;
    }


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

    void LightPBR(inout half3 color, inout half3 specular, CustomLightData lightData, Varyings unpacked, SurfaceDescription surfaceDescription, ShaderData sd)
    {
        float3 lightDirection = lightData.direction;
        float3 lightHalfVector = normalize(lightDirection + sd.viewDirectionWS);
        half lightNoL = saturate(dot(sd.normalWS, lightDirection));
        half lightLoH = saturate(dot(lightDirection, lightHalfVector));
        half lightNoH = saturate(dot(sd.normalWS, lightHalfVector));

        half3 lightColor = lightData.attenuation * lightData.color;
        half3 lightFinalColor = lightNoL * lightColor;

        #ifndef SHADER_API_MOBILE //TODO: redefine to something that will resemble lower shader quality
            lightFinalColor *= Filament::Fd_Burley(sd.perceptualRoughness, sd.NoV, lightNoL, lightLoH);
        #endif

        color += lightFinalColor;

        #ifndef _SPECULARHIGHLIGHTS_OFF

            half clampedRoughness = max(sd.perceptualRoughness * sd.perceptualRoughness, 0.002);

            #ifdef _ANISOTROPY
                half at = max(clampedRoughness * (1.0 + surfaceDescription.Anisotropy), 0.001);
                half ab = max(clampedRoughness * (1.0 - surfaceDescription.Anisotropy), 0.001);

                float3 l = lightData.direction;
                float3 t = sd.tangentWS;
                float3 b = sd.bitangentWS;
                float3 v = sd.viewDirectionWS;

                half ToV = dot(t, v);
                half BoV = dot(b, v);
                half ToL = dot(t, l);
                half BoL = dot(b, l);
                half ToH = dot(t, lightHalfVector);
                half BoH = dot(b, lightHalfVector);

                half3 F = Filament::F_Schlick(lightLoH, sd.f0) * sd.energyCompensation;
                half D = Filament::D_GGX_Anisotropic(lightNoH, lightHalfVector, t, b, at, ab);
                half V = Filament::V_SmithGGXCorrelated_Anisotropic(at, ab, ToV, BoV, ToL, BoL, sd.NoV, lightNoL);
            #else
                half3 F = Filament::F_Schlick(lightLoH, sd.f0) * sd.energyCompensation;
                half D = Filament::D_GGX(lightNoH, clampedRoughness);
                half V = Filament::V_SmithGGXCorrelated(sd.NoV, lightNoL, clampedRoughness);
            #endif

            specular += max(0.0, (D * V) * F) * lightFinalColor;
        #endif
    }

    half3 GetLightmap(Varyings unpacked, SurfaceDescription surfaceDescription, ShaderData sd, inout CustomLightData light, inout half3 lightmappedSpecular)
    {
        #if !defined(UNITY_PASS_FORWARDBASE) && defined(PIPELINE_BUILTIN)
            return 0.0f;
        #endif

        half3 indirectDiffuse = 0;
        #if defined(LIGHTMAP_ON)
            float2 lightmapUV = unpacked.lightmapUV.xy;

            #if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
                half bakedAtten = UnitySampleBakedOcclusion(lightmapUV.xy, unpacked.positionWS);
                float zDist = dot(_WorldSpaceCameraPos - unpacked.positionWS, UNITY_MATRIX_V[2].xyz);
                float fadeDist = UnityComputeShadowFadeDistance(unpacked.positionWS, zDist);
                half atten = UnityMixRealtimeAndBakedShadows(light.attenuation, bakedAtten, UnityComputeShadowFade(fadeDist));
            #endif


            #if defined(BICUBIC_LIGHTMAP)
                float4 lightmapTexelSize = BicubicSampling::GetTexelSize(unity_Lightmap);
                half4 bakedColorTex = BicubicSampling::SampleBicubic(unity_Lightmap, custom_bilinear_clamp_sampler, lightmapUV, lightmapTexelSize);
            #else
                half4 bakedColorTex = unity_Lightmap.SampleLevel(custom_bilinear_clamp_sampler, lightmapUV, 0);
            #endif

            #if defined(PIPELINE_BUILTIN)
            half3 lightMap = DecodeLightmap(bakedColorTex);
            #endif
            #if defined(PIPELINE_URP)
            half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);
            half3 lightMap = DecodeLightmap(bakedColorTex, decodeInstructions);
            #endif

            #if defined(DIRLIGHTMAP_COMBINED)
                float4 lightMapDirection = unity_LightmapInd.SampleLevel(custom_bilinear_clamp_sampler, lightmapUV, 0);
                #ifndef BAKERY_MONOSH
                    lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, sd.normalWS);
                #endif
            #endif

            #if defined(BAKERY_MONOSH)
                half clampedRoughness = max(sd.perceptualRoughness * sd.perceptualRoughness, 0.002);
                BakeryMonoSH(lightMap, lightmappedSpecular, lightmapUV, sd.normalWS, sd.viewDirectionWS, clampedRoughness, surfaceDescription, sd.tangentWS, sd.bitangentWS);
            #endif

            #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
                light = (CustomLightData)0;
                lightMap = SubtractMainLightWithRealtimeAttenuationFromLightmap(lightMap, atten, float4(0,0,0,0), sd.normalWS);
            #endif

            indirectDiffuse = lightMap;
        #endif

        #if defined(DYNAMICLIGHTMAP_ON)
            indirectDiffuse += RealtimeLightmap(unpacked.lightmapUV.zw, sd.normalWS);
        #endif


        indirectDiffuse = max(0.0, indirectDiffuse);

        return indirectDiffuse;
    }

    half3 GetReflections(Varyings unpacked, SurfaceDescription surfaceDescription, ShaderData sd, half3 indirectDiffuse)
    {
        #if !defined(UNITY_PASS_FORWARDBASE) && defined(PIPELINE_BUILTIN)
            return 0.0f;
        #endif

        half3 indirectSpecular = 0;

        half roughness = sd.perceptualRoughness * sd.perceptualRoughness;

        #if !defined(_GLOSSYREFLECTIONS_OFF) && (defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARD))
            float3 reflDir = reflect(-sd.viewDirectionWS, sd.normalWS);

            #ifdef _ANISOTROPY
                float3 anisotropicDirection = surfaceDescription.Anisotropy >= 0.0 ? sd.bitangentWS : sd.tangentWS;
                float3 anisotropicTangent = cross(anisotropicDirection, sd.viewDirectionWS);
                float3 anisotropicNormal = cross(anisotropicTangent, anisotropicDirection);
                float bendFactor = abs(surfaceDescription.Anisotropy) * saturate(1.0 - (pow(1.0 - sd.perceptualRoughness,5 )));
                float3 bentNormal = normalize(lerp(sd.normalWS, anisotropicNormal, bendFactor));
                reflDir = reflect(-sd.viewDirectionWS, bentNormal);
            #endif

            #ifndef SHADER_API_MOBILE
                reflDir = lerp(reflDir, sd.normalWS, roughness * roughness);
            #endif

            #ifdef PIPELINE_BUILTIN
                Unity_GlossyEnvironmentData envData;
                envData.roughness = sd.perceptualRoughness;

                
                envData.reflUVW = BoxProjectedCubemapDirection(reflDir, unpacked.positionWS.xyz, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);

                half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
                half3 reflectionSpecular = probe0;

                #if defined(UNITY_SPECCUBE_BLENDING)
                    UNITY_BRANCH
                    if (unity_SpecCube0_BoxMin.w < 0.99999)
                    {
                        envData.reflUVW = BoxProjectedCubemapDirection(reflDir, unpacked.positionWS.xyz, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

                        float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
                        reflectionSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
                    }
                #endif
            #endif

            #if defined(PIPELINE_URP)
                #if  UNITY_VERSION >= 202120
                    half3 reflectionSpecular = GlossyEnvironmentReflection(reflDir, unpacked.positionWS, sd.perceptualRoughness, 1.0f);
                #else
                    half3 reflectionSpecular = GlossyEnvironmentReflection(reflDir, sd.perceptualRoughness, 1.0f);
                #endif
            #endif

            float horizon = min(1.0 + dot(reflDir, sd.normalWS), 1.0);
            reflectionSpecular *= horizon * horizon;

            #ifdef LIGHTMAP_ON
                half specularOcclusion = saturate(sqrt(dot(indirectDiffuse, 1.0)) * surfaceDescription.Occlusion);
            #else
                half specularOcclusion = surfaceDescription.Occlusion;
            #endif

            reflectionSpecular *= Filament::computeSpecularAO(sd.NoV, specularOcclusion, roughness);

            indirectSpecular += reflectionSpecular;
        #endif

        return indirectSpecular;
    }


    half4 ApplyPBRLighting(Varyings unpacked, SurfaceDescription surfaceDescription)
    {
        half3 indirectDiffuse = 0;
        half3 lightFinalColor = 0;
        half3 indirectSpecular = 0;
        half3 directSpecular = 0;

        // alpha
        ApplyAlphaClip(surfaceDescription);

        // shader data
        ShaderData sd = InitializeShaderData(unpacked, surfaceDescription);

        // main light
        CustomLightData mainLightData = GetCustomMainLightData(unpacked);

        // lightmap and lightmapped specular
        indirectDiffuse = GetLightmap(unpacked, surfaceDescription, sd, mainLightData, indirectSpecular);

        //MixRealtimeAndBakedGI(mainLightData, unpacked.normalWS, indirectDiffuse);

        // main light and specular
        LightPBR(lightFinalColor, directSpecular, mainLightData, unpacked, surfaceDescription, sd);

        // additional light and specular (urp and non important lights)
        #if defined(_ADDITIONAL_LIGHTS) && defined(PIPELINE_URP)
            uint pixelLightCount = GetAdditionalLightsCount();
            for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
            {
                Light light = GetAdditionalLight(lightIndex, unpacked.positionWS);
                CustomLightData additionalLightData;
                additionalLightData.color = light.color;
                additionalLightData.direction = light.direction;
                additionalLightData.attenuation = light.distanceAttenuation * light.shadowAttenuation;
                LightPBR(lightFinalColor, directSpecular, additionalLightData, unpacked, surfaceDescription, sd);
            }
        #endif

        // light probes
        indirectDiffuse += GetLightProbes(sd.normalWS, unpacked.positionWS);

        // reflection probes
        indirectSpecular += GetReflections(unpacked, surfaceDescription, sd, indirectDiffuse);

        // fresnel
        indirectSpecular *= sd.energyCompensation * sd.brdf;
        directSpecular *= PI;

        #if defined(_ALPHAPREMULTIPLY_ON)
            surfaceDescription.Albedo.rgb *= surfaceDescription.Alpha;
            surfaceDescription.Alpha = lerp(surfaceDescription.Alpha, 1.0, surfaceDescription.Metallic);
        #endif

        #if defined(_ALPHAMODULATE_ON)
            surfaceDescription.Albedo.rgb = lerp(1.0, surfaceDescription.Albedo.rgb, surfaceDescription.Alpha);
        #endif

        half4 finalColor = half4(surfaceDescription.Albedo * (1.0 - surfaceDescription.Metallic) * (indirectDiffuse * surfaceDescription.Occlusion + (lightFinalColor))
                        + indirectSpecular + directSpecular + surfaceDescription.Emission, surfaceDescription.Alpha);

        return finalColor;
    }
}