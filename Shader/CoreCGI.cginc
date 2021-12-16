half4 frag (v2f i, uint facing : SV_IsFrontFace) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i)

    #if defined(LOD_FADE_CROSSFADE)
		UnityApplyDitherCrossFade(i.pos);
	#endif

    #if defined(PARALLAX)
        parallaxOffset = ParallaxOffset(i);
    #endif


    SurfaceData surf;
    InitializeDefaultSurfaceData(surf);
    InitializeLitSurfaceData(surf, i);


#if defined(UNITY_PASS_SHADOWCASTER)

    #if defined(_MODE_CUTOUT)
        if(surf.alpha < _Cutoff) discard;
    #endif

    #if defined (_MODE_FADE) || defined (_MODE_TRANSPARENT)
        if(surf.alpha < 0.5) discard;
    #endif

    SHADOW_CASTER_FRAGMENT(i);
#else

    #if defined (_MODE_CUTOUT)
        surf.alpha = (surf.alpha - _Cutoff) / max(fwidth(surf.alpha), 0.0001) + 0.5;
    #endif

    #ifdef NEED_CENTROID_NORMAL
    if ( dot(i.worldNormal, i.worldNormal) >= 1.01 )
    {
        i.worldNormal = i.centroidWorldNormal;
    }
    #endif

    float3 worldNormal = i.worldNormal;
    float3 bitangent = i.bitangent;
    float3 tangent = i.tangent;

    half3 indirectSpecular = 0;
    half3 directSpecular = 0;

    #if !defined(SHADER_API_MOBILE)
    if(!facing)
    {
        worldNormal *= -1;
        bitangent *= -1;
        tangent *= -1;
    }
    #endif


    #ifdef GEOMETRIC_SPECULAR_AA
    surf.perceptualRoughness = GSAA_Filament(worldNormal, surf.perceptualRoughness);
    #endif

    surf.tangentNormal.g *= -1; // still need to figure out why its inverted by default
    worldNormal = normalize(surf.tangentNormal.x * tangent + surf.tangentNormal.y * bitangent + surf.tangentNormal.z * worldNormal);
    tangent = normalize(cross(worldNormal, bitangent));
    bitangent = normalize(cross(worldNormal, tangent));


    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos.xyz);
    half NoV = abs(dot(worldNormal, viewDir)) + 1e-5;

    half3 pixelLight = 0;
    #ifdef USING_LIGHT_MULTI_COMPILE
        bool lightExists = any(_WorldSpaceLightPos0.xyz);
        float3 lightDirection = Unity_SafeNormalize(UnityWorldSpaceLightDir(i.worldPos.xyz));
        float3 lightHalfVector = Unity_SafeNormalize(lightDirection + viewDir);
        half lightNoL = saturate(dot(worldNormal, lightDirection));
        half lightLoH = saturate(dot(lightDirection, lightHalfVector));
        UNITY_LIGHT_ATTENUATION(lightAttenuation, i, i.worldPos.xyz);
        pixelLight = (lightNoL * lightAttenuation * _LightColor0.rgb);
        #if !defined(SHADER_API_MOBILE)
        pixelLight *= Fd_Burley(surf.perceptualRoughness, NoV, lightNoL, lightLoH);
        #endif
    #endif

    #ifdef VERTEXLIGHT_ON
    half3 vertexLight = i.vertexLight;
    #else
    half3 vertexLight = 0;
    #endif

    

    half3 indirectDiffuse = 1;
    #if defined(LIGHTMAP_ON)

        float2 lightmapUV = i.coord0.zw * unity_LightmapST.xy + unity_LightmapST.zw;
        half4 bakedColorTex = 0;

        half3 lightMap = tex2DFastBicubicLightmap(lightmapUV, bakedColorTex);

        #if defined(DIRLIGHTMAP_COMBINED) && !defined(SHADER_API_MOBILE)
            float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
            lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, worldNormal);
        #endif

        #if defined(DYNAMICLIGHTMAP_ON) && !defined(SHADER_API_MOBILE)
            float3 realtimeLightMap = getRealtimeLightmap(i.coord1.xy, worldNormal, parallaxOffset);
            lightMap += realtimeLightMap; 
        #endif

        #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
            pixelLight = 0;
            vertexLight = 0;
            lightMap = SubtractMainLightWithRealtimeAttenuationFromLightmap (lightMap, lightAttenuation, bakedColorTex, worldNormal);
        #endif

        indirectDiffuse = lightMap;
    #else

        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            UNITY_BRANCH
            if (unity_ProbeVolumeParams.x == 1)
            {
                indirectDiffuse = SHEvalLinearL0L1_SampleProbeVolume(float4(worldNormal, 1), i.worldPos);
            }
            else
            {
        #endif
                #ifdef NONLINEAR_LIGHTPROBESH
                    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                    indirectDiffuse.r = shEvaluateDiffuseL1Geomerics_local(L0.r, unity_SHAr.xyz, worldNormal);
                    indirectDiffuse.g = shEvaluateDiffuseL1Geomerics_local(L0.g, unity_SHAg.xyz, worldNormal);
                    indirectDiffuse.b = shEvaluateDiffuseL1Geomerics_local(L0.b, unity_SHAb.xyz, worldNormal);
                    indirectDiffuse = max(0, indirectDiffuse);
                #else
                    indirectDiffuse = max(0, ShadeSH9(float4(worldNormal, 1)));
                #endif
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            }
        #endif

    #endif

    #if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
        pixelLight *= UnityComputeForwardShadows(i.coord0.zw, i.worldPos, i.screenPos);
    #endif

    
    half3 f0 = 0.16 * surf.reflectance * surf.reflectance * (1 - surf.metallic) + surf.albedo.rgb * surf.metallic;
    half3 fresnel = F_Schlick(NoV, f0);
    
    

    half clampedRoughness = max(surf.perceptualRoughness * surf.perceptualRoughness, 0.002);

    #if !defined(SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
        float NoH = saturate(dot(worldNormal, lightHalfVector));

        float3 F = F_Schlick(lightLoH, f0);
        float D = GGXTerm(NoH, clampedRoughness);
        float V = V_SmithGGXCorrelated(NoV, lightNoL, clampedRoughness);

        directSpecular = max(0, (D * V) * F) * pixelLight * UNITY_PI;
    #endif

    #if defined(BAKEDSPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERY_VOLUME) && !defined(BAKERY_RNM) && !defined(_BAKERY_SH)
    {
        float3 bakedDominantDirection = 1;
        float3 bakedSpecularColor = 0;

        #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
            bakedDominantDirection = (lightMapDirection.xyz) * 2 - 1;
            bakedSpecularColor = indirectDiffuse;
        #endif

        #ifndef LIGHTMAP_ON
            bakedSpecularColor = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
            bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
        #endif

        bakedDominantDirection = normalize(bakedDominantDirection);
        directSpecular += GetSpecularHighlights(worldNormal, bakedSpecularColor, bakedDominantDirection, f0, viewDir, clampedRoughness, NoV);
    }
    #endif

    #if defined(BAKERY_RNM)
    {
        float3 eyeVecT = 0;
        #ifdef BAKERY_LMSPEC
            eyeVecT = -normalize(i.parallaxViewDir);
        #endif

        float3 prevSpec = directSpecular;
        BakeryRNM(indirectDiffuse, directSpecular, lightmapUV, surf.tangentNormal, surf.perceptualRoughness, eyeVecT);
        directSpecular *= fresnel;
        directSpecular += prevSpec;
    }
    #endif

    #ifdef BAKERY_SH
    {
        float3 prevSpec = directSpecular;
        BakerySH(indirectDiffuse, directSpecular, lightmapUV, worldNormal, -viewDir, surf.perceptualRoughness);
        directSpecular *= fresnel;
        directSpecular += prevSpec;
    }
    #endif

    #if !defined(SHADER_API_MOBILE)
    fresnel *= saturate(pow(length(indirectDiffuse), _SpecularOcclusion));
    #endif
    #if defined(UNITY_PASS_FORWARDBASE)

        #if !defined(REFLECTIONS_OFF)
            float3 reflDir = reflect(-viewDir, worldNormal);

            Unity_GlossyEnvironmentData envData;
            envData.roughness = surf.perceptualRoughness;
            envData.reflUVW = getBoxProjection(reflDir, i.worldPos.xyz, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);

            float3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
            indirectSpecular = probe0;

            #if defined(UNITY_SPECCUBE_BLENDING) && !defined(SHADER_API_MOBILE)
                UNITY_BRANCH
                if (unity_SpecCube0_BoxMin.w < 0.99999)
                {
                    envData.reflUVW = getBoxProjection(reflDir, i.worldPos.xyz, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
                    float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
                    indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
                }
            #endif

            float horizon = min(1 + dot(reflDir, worldNormal), 1);
            indirectSpecular = indirectSpecular * lerp(fresnel, f0, surf.perceptualRoughness) * horizon * horizon;
        #endif

        indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);
    #endif
    

    #if defined(_MODE_TRANSPARENT)
        surf.albedo.rgb *= surf.alpha;
        surf.alpha = lerp(surf.alpha, 1, surf.metallic);
    #endif

    
    half4 finalColor = half4(surf.albedo.rgb * (1 - surf.metallic) * (indirectDiffuse * surf.occlusion + (pixelLight + vertexLight)) + indirectSpecular + directSpecular + surf.emission, surf.alpha);

    #if defined (_MODE_FADE) && defined(UNITY_PASS_FORWARDADD)
        finalColor.rgb *= surf.alpha;
    #endif

    #ifdef UNITY_PASS_META
        UnityMetaInput metaInput;
        UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
        metaInput.Emission = surf.emission;
        metaInput.Albedo = surf.albedo.rgb;
        return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
    #endif

    #ifdef NEED_FOG
        UNITY_APPLY_FOG(i.fogCoord, finalColor);
    #endif

    return finalColor;
#endif
}