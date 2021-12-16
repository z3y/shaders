void InitializeLitSurfaceData(inout SurfaceData surf, v2f i)
{

    half2 mainUV = i.coord0.xy * _MainTex_ST.xy + _MainTex_ST.zw + parallaxOffset;
    

    half4 mainTexture = _MainTex.Sample(sampler_MainTex, mainUV);

    mainTexture *= _Color;
    
    surf.albedo = mainTexture.rgb;
    surf.alpha = mainTexture.a;


    half4 maskMap = 1;
    #ifdef _MASK_MAP
        maskMap = _MetallicGlossMap.Sample(sampler_MainTex, mainUV);
        surf.perceptualRoughness = 1 - (RemapMinMax(maskMap.a, _GlossinessMin, _Glossiness));
        surf.metallic = RemapMinMax(maskMap.r, _MetallicMin, _Metallic);
        surf.occlusion = lerp(1, maskMap.g, _Occlusion);
    #else
        surf.perceptualRoughness = 1 - _Glossiness;
        surf.metallic = _Metallic;
        surf.occlusion = 1;
    #endif

    

    half4 normalMap = float4(0.5, 0.5, 1, 1);
    #ifdef _NORMAL_MAP
        normalMap = _BumpMap.Sample(sampler_BumpMap, mainUV);
        surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
    #endif


    float4 detailNormalMap = float4(0.5, 0.5, 1, 1);
    #if defined(_DETAILALBEDO_MAP) || defined(_DETAILNORMAL_MAP)

        float2 detailUV[2];
        detailUV[0] = i.coord0.xy * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw + parallaxOffset;
        detailUV[1] = i.coord0.zw * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw + parallaxOffset;

        float detailMask = maskMap.b;
        float4 detailMap = 0.5;
        float3 detailAlbedo = 0;
        float detailSmoothness = 0;
        
        #if defined(_DETAILALBEDO_MAP)
            float4 detailAlbedoTex = _DetailAlbedoMap.Sample(sampler_DetailAlbedoMap, detailUV[_DetailMapUV]) * 2.0 - 1.0;
            detailAlbedo = detailAlbedoTex.rgb;
            detailSmoothness = detailAlbedoTex.a;
        #endif

        #if defined(_DETAILNORMAL_MAP)
            detailNormalMap = _DetailNormalMap.Sample(sampler_DetailNormalMap, detailUV[_DetailMapUV]);
        #endif
        
        #if defined(_DETAILALBEDO_MAP)
            // Goal: we want the detail albedo map to be able to darken down to black and brighten up to white the surface albedo.
            // The scale control the speed of the gradient. We simply remap detailAlbedo from [0..1] to [-1..1] then perform a lerp to black or white
            // with a factor based on speed.
            // For base color we interpolate in sRGB space (approximate here as square) as it get a nicer perceptual gradient

            float3 albedoDetailSpeed = saturate(abs(detailAlbedo) * _DetailAlbedoScale);
            float3 baseColorOverlay = lerp(sqrt(surf.albedo.rgb), (detailAlbedo < 0.0) ? float3(0.0, 0.0, 0.0) : float3(1.0, 1.0, 1.0), albedoDetailSpeed * albedoDetailSpeed);
            baseColorOverlay *= baseColorOverlay;							   
            // Lerp with details mask
            surf.albedo.rgb = lerp(surf.albedo.rgb, saturate(baseColorOverlay), detailMask);
        #endif

        #if defined(_DETAILALBEDO_MAP)
            float perceptualSmoothness = (1 - surf.perceptualRoughness);
            // See comment for baseColorOverlay
            float smoothnessDetailSpeed = saturate(abs(detailSmoothness) * _DetailSmoothnessScale);
            float smoothnessOverlay = lerp(perceptualSmoothness, (detailSmoothness < 0.0) ? 0.0 : 1.0, smoothnessDetailSpeed);
            // Lerp with details mask
            perceptualSmoothness = lerp(perceptualSmoothness, saturate(smoothnessOverlay), detailMask);

            surf.perceptualRoughness = (1 - perceptualSmoothness);
        #endif

        #if defined(_DETAILNORMAL_MAP)
            float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale * maskMap.b);
            surf.tangentNormal = BlendNormals(surf.tangentNormal, detailNormal);
        #endif
        
    #endif

    #ifndef SHADER_API_MOBILE
    surf.albedo.rgb = lerp(dot(surf.albedo.rgb, grayscaleVec), surf.albedo.rgb, _AlbedoSaturation);
    #endif
    
    #if defined(EMISSION)
        float3 emissionMap = 1;
        emissionMap = _EmissionMap.Sample(sampler_MainTex, mainUV).rgb;
        
        UNITY_BRANCH
        if(_EmissionMultBase) emissionMap *= surf.albedo.rgb;

        surf.emission = emissionMap * pow(_EmissionColor, 2.2);
    #endif
}