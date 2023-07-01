#include "Packages/com.z3y.shaders/ShaderLibrary/LightFunctions.hlsl"
#include "Packages/com.z3y.shaders/ShaderLibrary/SSR.hlsl"

#if defined(LTCGI_EXISTS) && defined(LTCGI) 
    #define UNITY_PI PI
    #define UNITY_HALF_PI PI/2.
    #define UNITY_TWO_PI PI*2
    #include "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc"
#endif

#ifdef _AREALIT
    #include "Assets/AreaLit/Shader/Lighting.hlsl"
#endif

namespace CustomLighting
{

    void ApplyAlphaClip(inout SurfaceDescription surfaceDescription)
    {
        #if defined(_ALPHATEST_ON) && defined(ALPHATOCOVERAGE_ON)
            surfaceDescription.Alpha = (surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold) / max(fwidth(surfaceDescription.Alpha), surfaceDescription.AlphaClipSharpness) + 0.5f;
        #endif

        #if defined(_ALPHATEST_ON) && !defined(ALPHATOCOVERAGE_ON)
            clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
        #endif
    }

    void GetBTN(Varyings unpacked, SurfaceDescription surfaceDescription, out float3 normalWS, out float3 bitangentWS, out float3 tangentWS)
    {
        #if !defined(_NORMAL_DROPOFF_OS) && !defined(_NORMAL_DROPOFF_WS)
            #define _NORMAL_DROPOFF_TS 1
        #else
            #define _NORMAL_DROPOFF_TS 0
        #endif

        //float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale(); // moved to vertex
        float crossSign = unpacked.tangentWS.w;
        bitangentWS = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
        tangentWS = unpacked.tangentWS.xyz;

        #if _NORMAL_DROPOFF_TS

            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE) && defined(GENERATION_CODE) && !defined(LIGHTMAP_ON)
                if (!unpacked.cullFace)
                {
                    surfaceDescription.Normal.xy *= -1;
                }
            #endif

            half3x3 tangentToWorld = half3x3(unpacked.tangentWS.xyz, bitangentWS, unpacked.normalWS.xyz);
            normalWS = TransformTangentToWorld(surfaceDescription.Normal, tangentToWorld);

            #ifdef _ANISOTROPY
                // TODO: do properly
                tangentWS = TransformTangentToWorld(surfaceDescription.Tangent, tangentToWorld);
                tangentWS = Orthonormalize(tangentWS, normalWS);
                bitangentWS = normalize(cross(normalWS, tangentWS));
            #endif


        #elif _NORMAL_DROPOFF_OS
            #ifdef GENERATION_GRAPH
                normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
            #else
                normalWS = TransformObjectToWorldNormal(surfaceDescription.Normal);
            #endif
        #elif _NORMAL_DROPOFF_WS
            #ifdef GENERATION_GRAPH
                normalWS = surfaceDescription.NormalWS;
            #else
                normalWS = surfaceDescription.Normal;
            #endif
        #endif

        normalWS = SafeNormalize(normalWS);
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE) && defined(GENERATION_CODE) && !defined(LIGHTMAP_ON)
            if (!unpacked.cullFace)
            {
                normalWS *= -1;
            }
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


    

    void LightPBR(inout half3 color, inout half3 specular, CustomLightData lightData, Varyings unpacked, SurfaceDescription surfaceDescription, ShaderData sd)
    {
        float3 lightDirection = lightData.direction;
        float3 lightHalfVector = normalize(lightDirection + sd.viewDirectionWS);
        half lightNoL = saturate(dot(sd.normalWS, lightDirection));
        half lightLoH = saturate(dot(lightDirection, lightHalfVector));
        half lightNoH = saturate(dot(sd.normalWS, lightHalfVector));

        half3 lightColor = lightData.attenuation * lightData.color;
        half3 lightFinalColor = lightNoL * lightColor;

#ifdef UNITY_PASS_FORWARDBASE
        #if !defined(QUALITY_LOW) && !defined(LIGHTMAP_ON)
            lightFinalColor *= Filament::Fd_Burley(sd.perceptualRoughness, sd.NoV, lightNoL, lightLoH);
        #endif
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
        half clampedRoughness = max(sd.perceptualRoughness * sd.perceptualRoughness, 0.002);
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
                half4 bakedColorTex = SAMPLE_TEXTURE2D_LOD(unity_Lightmap, custom_bilinear_clamp_sampler, lightmapUV, 0);
            #endif

            #if defined(PIPELINE_BUILTIN)
            half3 lightMap = DecodeLightmap(bakedColorTex);
            #endif
            #if defined(PIPELINE_URP)
            half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);
            half3 lightMap = DecodeLightmap(bakedColorTex, decodeInstructions);
            #endif

            #if defined(DIRLIGHTMAP_COMBINED)
                float4 lightMapDirection = SAMPLE_TEXTURE2D_LOD(unity_LightmapInd, custom_bilinear_clamp_sampler, lightmapUV, 0);
                #ifndef BAKERY_MONOSH

                    #if 0
                        lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, sd.normalWS);
                    #else
                        half halfLambert = dot(sd.normalWS, lightMapDirection.xyz - 0.5) + 0.5;
                        half mult = halfLambert / max(1e-4h, lightMapDirection.w);
                        mult *= mult * mult;
                        lightMap = lightMap * min(mult, 2.0);
                    #endif
                #endif
            #endif

            #if defined(BAKERY_MONOSH)
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

        #if defined(_LIGHTMAPPED_SPECULAR)
            float3 bakedDominantDirection = 0.0;
            half3 bakedSpecularColor = 0.0;

            #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON) && !defined(BAKERY_SH) && !defined(BAKERY_RNM) && !defined(BAKERY_MONOSH)
                bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
                half directionality = max(0.001, length(bakedDominantDirection));
                bakedDominantDirection /= directionality;
                bakedSpecularColor = lightMap * directionality;
            #endif

            #ifndef LIGHTMAP_ON
                bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
                bakedDominantDirection = normalize(bakedDominantDirection);
            #endif

            half3 halfDir = Unity_SafeNormalize(bakedDominantDirection + sd.viewDirectionWS);
            half nh = saturate(dot(sd.normalWS, halfDir));

           
            #ifdef _ANISOTROPY
                half at = max(clampedRoughness * (1.0 + surfaceDescription.Anisotropy), 0.001);
                half ab = max(clampedRoughness * (1.0 - surfaceDescription.Anisotropy), 0.001);
                lightmappedSpecular += max(Filament::D_GGX_Anisotropic(nh, halfDir, sd.tangentWS, sd.bitangentWS, at, ab) * bakedSpecularColor, 0.0);
            #else
                lightmappedSpecular += max(Filament::D_GGX(nh, clampedRoughness) * bakedSpecularColor, 0.0);
            #endif
            // DebugColor = lightmappedSpecular;
            // #define USE_DEBUGCOLOR
        #endif


        indirectDiffuse = max(0.0, indirectDiffuse);

        return indirectDiffuse;
    }

    half3 GetReflections(Varyings unpacked, SurfaceDescription surfaceDescription, ShaderData sd)
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

            #ifndef QUALITY_LOW
                reflDir = lerp(reflDir, sd.normalWS, roughness * roughness);
            #endif

            #if defined(PIPELINE_BUILTIN) && defined(USE_URP_BOX_PROJECTION)
                half3 reflectionSpecular = GlossyEnvironmentReflection(reflDir, unpacked.positionWS, sd.perceptualRoughness, 1.0f);
            #elif defined(PIPELINE_BUILTIN)
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

        #ifndef QUALITY_LOW
            float horizon = min(1.0 + dot(reflDir, sd.normalWS), 1.0);
            reflectionSpecular *= horizon * horizon;
        #endif

            indirectSpecular += reflectionSpecular;
        #endif

        return indirectSpecular;
    }


    half4 ApplyPBRLighting(Varyings unpacked, SurfaceDescription surfaceDescription)
    {
        GIData giData = (GIData)0;

        // alpha
        ApplyAlphaClip(surfaceDescription);

        // shader data
        ShaderData sd = InitializeShaderData(unpacked, surfaceDescription);

        #ifdef LOD_FADE_CROSSFADE
        LODDitheringTransition(ComputeFadeMaskSeed(sd.viewDirectionWS, unpacked.positionCS.xy), unity_LODFade.x);
        #endif

        // main light
        CustomLightData mainLightData = GetCustomMainLightData(unpacked);

        // lightmap and lightmapped specular
        giData.IndirectDiffuse = GetLightmap(unpacked, surfaceDescription, sd, mainLightData, giData.Reflections);

        #if defined(PIPELINE_URP)
        //MixRealtimeAndBakedGI(mainLightData, unpacked.normalWS, giData.IndirectDiffuse);
        uint meshRenderingLayers = GetMeshRenderingLightLayer();
        #endif

        // main light and specular

        #if defined(UNITY_PASS_FORWARDBASE) && !defined(DIRECTIONAL_COOKIE) && !defined(SHADOWS_SCREEN) && !defined(_SPECULARHIGHLIGHTS_OFF) && !defined(SHADOWS_SHADOWMASK) && !defined(LIGHTMAP_SHADOW_MIXING)
            #ifdef DIRECTIONAL // always defined
                bool lightEnabled = any(mainLightData.direction);
                UNITY_BRANCH // avoid calculating directional light in some cases, used in the if statement below
            #else
                bool lightEnabled = false;
            #endif
        #else
            bool lightEnabled = true;
        #endif
        if (lightEnabled)
        {
            LightPBR(giData.Light, giData.Specular, mainLightData, unpacked, surfaceDescription, sd);
        }

        // non important lights per pixel
        #if defined(VERTEXLIGHT_ON) && !defined(DISABLE_NONIMPORTANT_LIGHTS_PER_PIXEL)
            NonImportantLightsPerPixel(giData.Light, giData.Specular, unpacked.positionWS, sd.normalWS, sd.viewDirectionWS, sd.NoV, sd.f0, sd);
        #endif

        // additional light and specular (urp and non important lights)
        #if defined(_ADDITIONAL_LIGHTS) && defined(PIPELINE_URP)
            uint pixelLightCount = GetAdditionalLightsCount();
            #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
            half4 shadowMask = SAMPLE_SHADOWMASK(unpacked.lightmapUV.xy);
            #else
            half4 shadowMask = 0.0f;
            #endif

            LIGHT_LOOP_BEGIN(pixelLightCount)
                Light light = GetAdditionalLight(lightIndex, unpacked.positionWS, shadowMask);
                CustomLightData additionalLightData;
                additionalLightData.color = light.color;
                additionalLightData.direction = light.direction;
                additionalLightData.attenuation = light.distanceAttenuation * light.shadowAttenuation;

                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                {
                    LightPBR(giData.Light, giData.Specular, additionalLightData, unpacked, surfaceDescription, sd);
                }
            LIGHT_LOOP_END
        #endif
        
        #if defined(VERTEXLIGHT_ON) && defined(DISABLE_NONIMPORTANT_LIGHTS_PER_PIXEL)
            giData.Light += unpacked.vertexLight;
        #endif

        #ifndef LIGHTMAP_ON
            // light probes
            #ifdef VARYINGS_NEED_SH
                giData.IndirectDiffuse += GetLightProbes(sd.normalWS, unpacked.positionWS, unpacked.sh);
            #else
                giData.IndirectDiffuse += GetLightProbes(sd.normalWS, unpacked.positionWS, 0);
            #endif
        #endif

        // reflection probes
        giData.Reflections += GetReflections(unpacked, surfaceDescription, sd);

        // occlusion
        #ifdef LIGHTMAP_ON
            half specularOcclusion = lerp(1.0f, saturate(sqrt(dot(giData.IndirectDiffuse, 1.0)) * surfaceDescription.Occlusion), surfaceDescription.SpecularOcclusion);
        #else
            half specularOcclusion = surfaceDescription.Occlusion;
        #endif
        giData.Reflections *= Filament::computeSpecularAO(sd.NoV, specularOcclusion, sd.perceptualRoughness * sd.perceptualRoughness);


        #if defined(LTCGI) && defined(LTCGI_EXISTS)
            float2 ltcgi_lmuv;
            #if defined(LIGHTMAP_ON)
                ltcgi_lmuv = unpacked.texCoord1.xy;
            #else
                ltcgi_lmuv = float2(0, 0);
            #endif

            float3 ltcgiSpecular = 0;
            float3 ltcgiDiffuse = 0;
            LTCGI_Contribution(unpacked.positionWS.xyz, sd.normalWS, sd.viewDirectionWS, sd.perceptualRoughness, ltcgi_lmuv, ltcgiDiffuse, ltcgiSpecular);
            #ifndef LTCGI_DIFFUSE_DISABLED   
                giData.Light += ltcgiDiffuse;
            #endif
            giData.Reflections += ltcgiSpecular;
        #endif

        #ifdef _SSR
            SSRInput ssr;
            ssr.wPos = float4(unpacked.positionWS.xyz, 1);
            ssr.viewDir = sd.viewDirectionWS;
            ssr.rayDir = float4(reflect(-sd.viewDirectionWS, sd.normalWS).xyz, 1);
            ssr.faceNormal = sd.normalWS;
            ssr.hitRadius = 0.02;
            ssr.blur = 8;
            ssr.maxSteps = 50;
            ssr.smoothness = 1- sd.perceptualRoughness;
            ssr.edgeFade = 0.2;
            ssr.scrnParams = _CameraOpaqueTexture_TexelSize.zw; //TODO: fix for urp
            ssr.NoiseTex = BlueNoise;
            ssr.NoiseTex_dim = BlueNoise_TexelSize.zw;
            float4 ssReflections = getSSRColor(ssr);
            float horizon = min(1.0 + dot(ssr.rayDir.xyz, ssr.faceNormal), 1.0);
            ssReflections.rgb *= horizon * horizon;
            giData.Reflections = lerp(giData.Reflections, ssReflections.rgb, ssReflections.a);
        #endif

        #ifdef _AREALIT
            AreaLightFragInput areaLitInput = (AreaLightFragInput)0;
            half4 areaLitDiffuse;
            half4 areaLitSpecular;
            #ifdef _SPECULARHIGHLIGHTS_OFF
                bool areaLitSpecularEnabled = false;
            #else
                bool areaLitSpecularEnabled = true;
            #endif
            areaLitInput.pos = float4(unpacked.positionWS.xyz, 1);
            areaLitInput.normal = sd.normalWS;
            areaLitInput.view = sd.viewDirectionWS;
            areaLitInput.roughness = sd.perceptualRoughness * sd.perceptualRoughness;
            areaLitInput.occlusion = 1;
            float4 screenPos = ComputeGrabScreenPos(unpacked.positionCS).xyzz;
            areaLitInput.screenPos = screenPos.xy / (screenPos.w+0.0000000001);
            ShadeAreaLights(areaLitInput, areaLitDiffuse, areaLitSpecular, true, areaLitSpecularEnabled);
            giData.Reflections += areaLitSpecular;
            giData.Light += areaLitDiffuse;
        #endif

        // fresnel
        giData.Reflections *= sd.energyCompensation * sd.brdf;

        giData.Specular *= PI;

        // modify lighting
        #ifdef USE_MODIFYLIGHTING
        ModifyLighting(giData, unpacked, sd, surfaceDescription);
        #endif

        #if defined(_ALPHAPREMULTIPLY_ON)
            surfaceDescription.Albedo.rgb *= surfaceDescription.Alpha;
            surfaceDescription.Alpha = lerp(surfaceDescription.Alpha, 1.0, surfaceDescription.Metallic);
        #endif

        #if defined(_ALPHAMODULATE_ON)
            surfaceDescription.Albedo.rgb = lerp(1.0, surfaceDescription.Albedo.rgb, surfaceDescription.Alpha);
        #endif

        half4 finalColor = half4(surfaceDescription.Albedo * (1.0 - surfaceDescription.Metallic) * (giData.IndirectDiffuse * surfaceDescription.Occlusion + giData.Light)
                        + giData.Reflections + giData.Specular + surfaceDescription.Emission, surfaceDescription.Alpha);

        // fog
        #if defined(FOG_ANY) && defined(PIPELINE_BUILTIN)
            UNITY_APPLY_FOG(unpacked.fogCoord, finalColor);
        #endif


        // modify final color
        #ifdef USE_MODIFYFINALCOLOR
        ModifyFinalColor(finalColor, giData, unpacked, sd, surfaceDescription);
        #endif

        #define FIX_BLACK_LEVEL
        #if !defined(SHADER_API_MOBILE) && defined(UNITY_PASS_FORWARDBASE)
            #ifdef DITHERING
                finalColor.rgb -= ditherNoiseFuncHigh(input.uv01.xy) * 0.001;
            #else
                #ifdef FIX_BLACK_LEVEL
                // the reason why this exists is because post processing applies dithering additively with a noise in range [-1, 1]
                // when applying dithering to 0 the visible range of the result is [0, 1]
                // shifts the colors down one level so when the dithering gets applied black color will be in range [-2,0]
                // doesnt fix color grading, it would require changes in post processing shaders
                finalColor.rgb -= 0.0002;
                #endif
            #endif
        #endif

        #ifdef USE_DEBUGCOLOR
        return DebugColor.rgbb;
        #endif

        return finalColor;
    }
}