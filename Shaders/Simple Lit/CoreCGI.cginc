sampler3D _DitherMaskLOD;
half4 frag (v2f i, uint facing : SV_IsFrontFace) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i)

    #if defined(LOD_FADE_CROSSFADE)
		UnityApplyDitherCrossFade(i.pos);
	#endif

    


    SurfaceData surf;
    InitializeDefaultSurfaceData(surf);
    InitializeLitSurfaceData(surf, i);


#if defined(UNITY_PASS_SHADOWCASTER)

    #if defined(_MODE_CUTOUT)
        if(surf.alpha < _Cutoff) discard;
    #endif

    #if defined(_ALPHADITHER)
        half dither = tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, surf.alpha * 0.9375)).a;
        if(dither < 0.1) discard;
    #endif

    SHADOW_CASTER_FRAGMENT(i);
#else

    #if defined (_MODE_CUTOUT)
        surf.alpha = (surf.alpha - _Cutoff) / max(fwidth(surf.alpha), 0.0001) + 0.5;
    #endif

    #ifdef NEED_CENTROID_NORMAL
    UNITY_FLATTEN
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

    #if !defined(SHADER_API_MOBILE) && !defined(LIGHTMAP_ON)
    UNITY_FLATTEN
    if (!facing)
    {
        worldNormal *= -1;
        bitangent *= -1;
        tangent *= -1;
    }
    #endif


    #ifndef SHADER_API_MOBILE
    if (_GSAA)
        surf.perceptualRoughness = GSAA_Filament(worldNormal, surf.perceptualRoughness);
    #endif
    
    float3 tangentNormalInv = surf.tangentNormal;
    #if defined(_NORMAL_MAP) || defined(_DETAILNORMAL_MAP)
        surf.tangentNormal.g *= -1; // still need to figure out why its inverted by default
        worldNormal = normalize(surf.tangentNormal.x * tangent + surf.tangentNormal.y * bitangent + surf.tangentNormal.z * worldNormal);
        tangent = normalize(cross(worldNormal, bitangent));
        bitangent = normalize(cross(worldNormal, tangent));
    #else
        worldNormal = normalize(worldNormal);
    #endif


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
        half3 lightCol = lightAttenuation * _LightColor0.rgb;
        pixelLight = (lightNoL * lightCol);

        #if !defined(SHADER_API_MOBILE)
            pixelLight *= Fd_Burley(surf.perceptualRoughness, NoV, lightNoL, lightLoH);
        #endif
    #endif

    half3 vertexLight = 0;
    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
        vertexLight = i.vertexLight;
    #endif

    #if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
        VertexLightInformation vLights = (VertexLightInformation)0;
        InitVertexLightData(i.worldPos, worldNormal, vLights);
    #endif

    

    half3 indirectDiffuse = 1;
    #if defined(LIGHTMAP_ON)

        half3 lightMap = 0;
        float2 lightmapUV = i.coord0.zw * unity_LightmapST.xy + unity_LightmapST.zw;

        half4 bakedColorTex = 0;
        lightMap = tex2DFastBicubicLightmap(lightmapUV, bakedColorTex);

        #ifdef BAKERY_RNM
            half3 rnm0 = DecodeLightmap(BakeryTex2D(_RNM0, lightmapUV, _RNM0_TexelSize));
            half3 rnm1 = DecodeLightmap(BakeryTex2D(_RNM1, lightmapUV, _RNM0_TexelSize));
            half3 rnm2 = DecodeLightmap(BakeryTex2D(_RNM2, lightmapUV, _RNM0_TexelSize));

            lightMap =    saturate(dot(rnmBasis0, tangentNormalInv)) * rnm0
                        + saturate(dot(rnmBasis1, tangentNormalInv)) * rnm1
                        + saturate(dot(rnmBasis2, tangentNormalInv)) * rnm2;
        #endif

        #ifdef BAKERY_SH
            half3 L0 = lightMap;

            half3 nL1x = BakeryTex2D(_RNM0, lightmapUV, _RNM0_TexelSize) * 2 - 1;
            half3 nL1y = BakeryTex2D(_RNM1, lightmapUV, _RNM0_TexelSize) * 2 - 1;
            half3 nL1z = BakeryTex2D(_RNM2, lightmapUV, _RNM0_TexelSize) * 2 - 1;
            half3 L1x = nL1x * L0 * 2;
            half3 L1y = nL1y * L0 * 2;
            half3 L1z = nL1z * L0 * 2;

            #ifdef BAKERY_SHNONLINEAR
                float lumaL0 = dot(L0, float(1));
                float lumaL1x = dot(L1x, float(1));
                float lumaL1y = dot(L1y, float(1));
                float lumaL1z = dot(L1z, float(1));
                float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), worldNormal);

                lightMap = L0 + worldNormal.x * L1x + worldNormal.y * L1y + worldNormal.z * L1z;
                float regularLumaSH = dot(lightMap, 1);
                lightMap *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));
            #else
                lightMap = L0 + worldNormal.x * L1x + worldNormal.y * L1y + worldNormal.z * L1z;
            #endif
            
        #endif

        #if defined(DIRLIGHTMAP_COMBINED) && !defined(SHADER_API_MOBILE)
            float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
            lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, worldNormal);
        #endif

        #if defined(DYNAMICLIGHTMAP_ON) && !defined(SHADER_API_MOBILE)
            float3 realtimeLightMap = getRealtimeLightmap(i.coord1.xy, worldNormal);
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

    half3 f0 = 0.16 * surf.reflectance * surf.reflectance * (1.0 - surf.metallic) + surf.albedo.rgb * surf.metallic;

    half2 dfg = SampleDFG(NoV, surf.perceptualRoughness).rg;
    half3 energyCompensation = EnvBRDFEnergyCompensation(dfg, f0);



    half roughness = surf.perceptualRoughness * surf.perceptualRoughness;
    half clampedRoughness = max(roughness, 0.002);

    #if !defined(SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
        half NoH = saturate(dot(worldNormal, lightHalfVector));

        half3 F = F_Schlick(lightLoH, f0);
        half D = D_GGX(NoH, clampedRoughness);
        half V = V_SmithGGXCorrelated(NoV, lightNoL, clampedRoughness);

        #ifndef SHADER_API_MOBILE
        F *= energyCompensation;
        #endif

        directSpecular = max(0, (D * V) * F) * pixelLight * UNITY_PI;
    #endif


    #if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
        [unroll(4)]
        for(int j = 0; j < 4; j++)
        {
            UNITY_BRANCH
            if(vLights.Attenuation[j] > 0)
            {
                vLights.Direction[j] = normalize(vLights.Direction[j]);
                half vLightNoL = saturate(dot(worldNormal, vLights.Direction[j]));
                half3 vLightCol = vLightNoL * vLights.ColorFalloff[j];
                vertexLight += vLightCol;

                #ifndef SPECULAR_HIGHLIGHTS_OFF
                    float3 vLightHalfVector = Unity_SafeNormalize(vLights.Direction[j] + viewDir);
                    half vNoH = saturate(dot(worldNormal, vLightHalfVector));
                    half vLoH = saturate(dot(vLights.Direction[j], vLightHalfVector));

                    half3 Fv = F_Schlick(vLoH, f0);
                    half Dv = D_GGX(vNoH, clampedRoughness);
                    half Vv = V_SmithGGXCorrelatedFast(NoV, vLightNoL, clampedRoughness);
                    directSpecular += max(0, (Dv * Vv) * Fv) * vLightCol * UNITY_PI;
                #endif
            }
        }
    #endif



    #if defined(BAKEDSPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERYLM_ENABLED)
    {
        float3 bakedDominantDirection = 1;
        half3 bakedSpecularColor = 0;

        #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
            bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
            bakedSpecularColor = indirectDiffuse;
        #endif

        #ifndef LIGHTMAP_ON
            bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
            bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
        #endif

        bakedDominantDirection = normalize(bakedDominantDirection);
        directSpecular += GetSpecularHighlights(worldNormal, bakedSpecularColor, bakedDominantDirection, f0, viewDir, clampedRoughness, NoV, energyCompensation);
    }
    #endif
    
    half3 fresnel = F_Schlick(NoV, f0);
    #if defined(BAKERY_LMSPEC) && defined(UNITY_PASS_FORWARDBASE) && defined(LIGHTMAP_ON)

        #ifdef BAKERY_RNM
        {
            float3 viewDirT = -normalize(i.parallaxViewDir);
            float3 dominantDirT = rnmBasis0 * dot(rnm0, lumaConv) +
                                  rnmBasis1 * dot(rnm1, lumaConv) +
                                  rnmBasis2 * dot(rnm2, lumaConv);

            float3 dominantDirTN = normalize(dominantDirT);
            half3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
                               saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
                               saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;

            half3 halfDir = Unity_SafeNormalize(dominantDirTN - viewDirT);
            half NoH = saturate(dot(tangentNormalInv, halfDir));
            half spec = D_GGX(NoH, clampedRoughness);
            directSpecular += spec * specColor * fresnel;
        }
        #endif

        #ifdef BAKERY_SH
        {
            float3 dominantDir = float3(dot(nL1x, lumaConv), dot(nL1y, lumaConv), dot(nL1z, lumaConv));
            half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + viewDir);
            half NoH = saturate(dot(worldNormal, halfDir));
            half spec = D_GGX(NoH, clampedRoughness);
            float3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
            dominantDir = normalize(dominantDir);
            directSpecular += max(spec * sh, 0.0) * fresnel;
        }
        #endif

    #endif

  
    #if defined(UNITY_PASS_FORWARDBASE)
        #if !defined(REFLECTIONS_OFF)
            float3 reflDir = reflect(-viewDir, worldNormal);

            #ifndef SHADER_API_MOBILE
                reflDir = lerp(reflDir, worldNormal, roughness * roughness);
            #endif

            Unity_GlossyEnvironmentData envData;
            envData.roughness = surf.perceptualRoughness;
            envData.reflUVW = getBoxProjection(reflDir, i.worldPos.xyz, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);

            half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
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

            half specularOcclusion = lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), _SpecularOcclusion);
            #ifndef SHADER_API_MOBILE
                float horizon = min(1 + dot(reflDir, worldNormal), 1.0);
                dfg.x *= specularOcclusion;
                indirectSpecular = indirectSpecular * horizon * horizon * energyCompensation * EnvBRDFMultiscatter(dfg, f0);
            #else
                indirectSpecular = probe0 * EnvBRDFApprox(surf.perceptualRoughness, NoV, f0) * specularOcclusion;
            #endif

        #endif

        #if defined(_MASK_MAP)
        indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);
        #endif
    #endif
    

    #if defined(_ALPHAPREMULTIPLY_ON)
        surf.albedo.rgb *= surf.alpha;
        surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
    #endif

    #if defined(_ALPHAMODULATE_ON)
        surf.albedo.rgb = lerp(1.0, surf.albedo.rgb, surf.alpha);
    #endif
    
    half4 finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + (pixelLight + vertexLight)) + indirectSpecular + directSpecular + surf.emission, surf.alpha);

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