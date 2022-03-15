half4 frag (v2f i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i)

    SurfaceData surf;
    InitializeDefaultSurfaceData(surf);
    InitializeLitSurfaceData(surf, i);

#if defined(UNITY_PASS_SHADOWCASTER)
    SHADOW_CASTER_FRAGMENT(i);
#else


    float3 worldNormal = i.worldNormal;
    float3 bitangent = i.bitangent;
    float3 tangent = i.tangent;
    #if defined(NEED_CENTROID_NORMAL)
        if (dot(i.centroidWorldNormal, worldNormal) >= 1.01)
        {
            worldNormal = i.centroidWorldNormal;
        }
    #endif

    half3 indirectSpecular = 0.0;
    half3 directSpecular = 0.0;
    half3 otherSpecular = 0.0;

    #ifdef GEOMETRIC_SPECULAR_AA
        surf.perceptualRoughness = GSAA_Filament(worldNormal, surf.perceptualRoughness);
    #endif
    
    half roughness = surf.perceptualRoughness * surf.perceptualRoughness;
    half clampedRoughness = max(roughness, 0.002);

    TangentToWorldNormal(surf.tangentNormal, worldNormal, tangent, bitangent);

    float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
    half NoV = NormalDotViewDir(worldNormal, viewDir);

    half3 f0 = GetF0(surf.reflectance, surf.metallic, surf.albedo.rgb);
    DFGLut = SampleDFG(NoV, surf.perceptualRoughness).rg;
    DFGEnergyCompensation = EnvBRDFEnergyCompensation(DFGLut, f0);

    LightData lightData;
    InitializeLightData(lightData, worldNormal, viewDir, NoV, clampedRoughness, surf.perceptualRoughness, f0, i);

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
        vertexLight = i.vertexLight;
    #endif

    #if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
        NonImportantLightsPerPixel(lightData.FinalColor, directSpecular, i.worldPos, worldNormal, viewDir, NoV, f0, clampedRoughness);
    #endif


    half3 indirectDiffuse;
    #if defined(LIGHTMAP_ANY)

        float2 lightmapUV = i.uv[1].zw;
        half4 bakedColorTex = SampleBicubic(unity_Lightmap, samplerunity_Lightmap, lightmapUV);
        half3 lightMap = DecodeLightmap(bakedColorTex);

        #ifdef BAKERY_RNM
            BakeryRNMLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, surf.tangentNormal, i.viewDirTS, viewDir, clampedRoughness, f0);
        #endif

        #ifdef BAKERY_SH
            BakerySHLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, worldNormal, viewDir, clampedRoughness, f0);
        #endif

        #if defined(DIRLIGHTMAP_COMBINED)
            float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
            lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, worldNormal);
        #endif

        #if defined(DYNAMICLIGHTMAP_ON)
            float3 realtimeLightMap = getRealtimeLightmap(i.uv[2].zw, worldNormal);
            lightMap += realtimeLightMap; 
        #endif

        #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
            lightData.FinalColor = 0.0;
            lightData.Specular = 0.0;
            lightMap = SubtractMainLightWithRealtimeAttenuationFromLightmap (lightMap, lightData.Attenuation, bakedColorTex, worldNormal);
        #endif

        indirectDiffuse = lightMap;
    #else
        indirectDiffuse = GetLightProbes(worldNormal, i.worldPos.xyz);
    #endif
    indirectDiffuse = max(0.0, indirectDiffuse);


    #if !defined(SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
        directSpecular += lightData.Specular;
    #endif

    #if defined(BAKEDSPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERYLM_ENABLED)
    {
        float3 bakedDominantDirection = 1.0;
        half3 bakedSpecularColor = 0.0;

        #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
            bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
            bakedSpecularColor = indirectDiffuse;
        #endif

        #ifndef LIGHTMAP_ANY
            bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
            bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
        #endif

        bakedDominantDirection = normalize(bakedDominantDirection);
        directSpecular += GetSpecularHighlights(worldNormal, bakedSpecularColor, bakedDominantDirection, f0, viewDir, clampedRoughness, NoV, DFGEnergyCompensation);
    }
    #endif

    #ifdef LTCGI
            float2 ltcgi_lmuv;
    #if defined(LIGHTMAP_ON)
            ltcgi_lmuv = i.uv[1].xy;
    #else
            ltcgi_lmuv = float2(0, 0);
    #endif
            float3 ltcgiSpecular = 0;
            LTCGI_Contribution(i.worldPos, worldNormal, viewDir, surf.perceptualRoughness, ltcgi_lmuv, indirectDiffuse
    #ifndef SPECULAR_HIGHLIGHTS_OFF
                , ltcgiSpecular
    #endif
            );
            indirectSpecular += ltcgiSpecular * F_Schlick(NoV, f0);
    #endif

  
    #if !defined(REFLECTIONS_OFF)
        indirectSpecular += GetReflections(worldNormal, i.worldPos.xyz, viewDir, f0, roughness, NoV, surf, indirectDiffuse);
    #endif

    otherSpecular *= EnvBRDFMultiscatter(DFGLut, f0) * DFGEnergyCompensation;
    
    half4 finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + (lightData.FinalColor)) + indirectSpecular + (directSpecular * UNITY_PI) + otherSpecular + surf.emission, surf.alpha);

    #ifdef UNITY_PASS_META
        UnityMetaInput metaInput;
        UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
        metaInput.Emission = surf.emission;
        metaInput.Albedo = surf.albedo.rgb;
        return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
    #endif

    
    UNITY_APPLY_FOG(i.fogCoord, finalColor);

    return finalColor;
#endif
}