static float arrayIndex;

half4 SampleTexture(Texture2D t, SamplerState s, float2 uv)
{
    return t.Sample(s, uv);
}

half4 SampleTexture(Texture2DArray t, SamplerState s, float2 uv)
{
    return t.Sample(s, float3(uv, arrayIndex));
}

#if defined(_TEXTURE_ARRAY)
    #define TEXARGS(tex) tex##Array
#else
    #define TEXARGS(tex) tex
#endif

void InitializeLitSurfaceData(inout SurfaceData surf, v2f i)
{
    arrayIndex = i.uv[3].z;
    float2 parallaxOffset = 0.0;
    #if defined(PARALLAX)
        float2 parallaxUV = i.uv[0].zw;
        parallaxOffset = ParallaxOffset(i.viewDirTS, parallaxUV);
    #endif

    half2 mainUV = i.uv[0].zw + parallaxOffset;
    half4 mainTexture = SampleTexture(TEXARGS(_MainTex), TEXARGS(sampler_MainTex), mainUV);

    mainTexture *= UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _Color);
    
    surf.albedo = mainTexture.rgb;
    surf.alpha = mainTexture.a;


    half4 maskMap = 1.0;
    #ifdef _MASK_MAP
        maskMap = SampleTexture(TEXARGS(_MetallicGlossMap), TEXARGS(sampler_MetallicGlossMap), mainUV);
        surf.perceptualRoughness = 1.0 - (RemapMinMax(maskMap.a, _GlossinessMin, _Glossiness));
        surf.metallic = RemapMinMax(maskMap.r, _MetallicMin, _Metallic);
        surf.occlusion = lerp(1.0, maskMap.g, _Occlusion);
    #else
        surf.perceptualRoughness = 1.0 - _Glossiness;
        surf.metallic = _Metallic;
        surf.occlusion = 1.0;
    #endif

    

    half4 normalMap = float4(0.5, 0.5, 1.0, 1.0);
    #ifdef _NORMAL_MAP
        normalMap = SampleTexture(TEXARGS(_BumpMap), TEXARGS(sampler_BumpMap), mainUV);
        surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
    #endif


    #if defined(_LAYER1)

        half4 sampledMask = 1.0;

        #ifndef _LAYER2
        UNITY_BRANCH
        if (_DetailMask_TexelSize.w > 1.0)
        {
        #endif
            float2 detailMaskUV = (i.uv[_DetailMaskUV].xy * _DetailMask_ST.xy) + _DetailMask_ST.zw + parallaxOffset;
            sampledMask = SampleTexture(_DetailMask, sampler_DetailMask, detailMaskUV);
        #ifndef _LAYER2
        }
        #endif

        // layer 1
        #ifdef _LAYER1
        {
            half detailMask = sampledMask.r;
            float2 detailUV = (i.uv[_DetailMapUV].xy * _DetailAlbedoMap_ST.xy) + _DetailAlbedoMap_ST.zw + parallaxOffset + ParallaxOffsetUV(_DetailDepth, i.viewDirTS);
            half4 sampledDetailAlbedo = SampleTexture(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUV);
            #if defined(_DETAILBLEND_SCREEN)
                surf.albedo = lerp(surf.albedo, BlendMode_Screen(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_Screen(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale);
            #elif defined(_DETAILBLEND_MULX2)
                surf.albedo = lerp(surf.albedo, BlendMode_MultiplyX2(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_MultiplyX2(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale);
            #elif defined(_DETAILBLEND_LERP)
                surf.albedo = lerp(surf.albedo, sampledDetailAlbedo.rgb, detailMask * _DetailAlbedoScale);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a, detailMask * _DetailSmoothnessScale);
            #else // default overlay
                surf.albedo = lerp(surf.albedo, BlendMode_Overlay_sRGB(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_Overlay(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale);
            #endif

            UNITY_BRANCH
            if (_DetailNormalMap_TexelSize.w > 1.0)
            {
                float4 detailNormalMap = SampleTexture(_DetailNormalMap, sampler_DetailNormalMap, detailUV);
                float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale);
                #if defined(_DETAILBLEND_LERP)
                    surf.tangentNormal = lerp(surf.tangentNormal, detailNormal, detailMask);
                #else
                    surf.tangentNormal = lerp(surf.tangentNormal, BlendNormals(surf.tangentNormal, detailNormal), detailMask);
                #endif
            }
        }
        #endif

        // layer 2
        #ifdef _LAYER2
        {
            half detailMask = sampledMask.g;
            float2 detailUV = (i.uv[_DetailMapUV2].xy * _DetailAlbedoMap2_ST.xy) + _DetailAlbedoMap2_ST.zw + parallaxOffset + ParallaxOffsetUV(_DetailDepth2, i.viewDirTS);
            half4 sampledDetailAlbedo = SampleTexture(_DetailAlbedoMap2, sampler_DetailAlbedoMap, detailUV);
            #if defined(_DETAILBLEND_SCREEN)
                surf.albedo = lerp(surf.albedo, BlendMode_Screen(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale2);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_Screen(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale2);
            #elif defined(_DETAILBLEND_MULX2)
                surf.albedo = lerp(surf.albedo, BlendMode_MultiplyX2(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale2);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_MultiplyX2(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale2);
            #elif defined(_DETAILBLEND_LERP)
                surf.albedo = lerp(surf.albedo, sampledDetailAlbedo.rgb, detailMask * _DetailAlbedoScale2);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a, detailMask * _DetailSmoothnessScale2);
            #else // default overlay
                surf.albedo = lerp(surf.albedo, BlendMode_Overlay_sRGB(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale2);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_Overlay(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale2);
            #endif

            UNITY_BRANCH
            if (_DetailNormalMap2_TexelSize.w > 1.0)
            {
                float4 detailNormalMap = SampleTexture(_DetailNormalMap2, sampler_DetailNormalMap, detailUV);
                float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale2);
                #if defined(_DETAILBLEND_LERP)
                    surf.tangentNormal = lerp(surf.tangentNormal, detailNormal, detailMask);
                #else
                    surf.tangentNormal = lerp(surf.tangentNormal, BlendNormals(surf.tangentNormal, detailNormal), detailMask);
                #endif
            }
        }
        #endif

        // layer 3
        #ifdef _LAYER3
        {
            half detailMask = sampledMask.b;
            float2 detailUV = (i.uv[_DetailMapUV3].xy * _DetailAlbedoMap3_ST.xy) + _DetailAlbedoMap3_ST.zw + parallaxOffset + ParallaxOffsetUV(_DetailDepth3, i.viewDirTS);
            half4 sampledDetailAlbedo = SampleTexture(_DetailAlbedoMap3, sampler_DetailAlbedoMap, detailUV);
            #if defined(_DETAILBLEND_SCREEN)
                surf.albedo = lerp(surf.albedo, BlendMode_Screen(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale3);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_Screen(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale3);
            #elif defined(_DETAILBLEND_MULX2)
                surf.albedo = lerp(surf.albedo, BlendMode_MultiplyX2(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale3);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_MultiplyX2(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale3);
            #elif defined(_DETAILBLEND_LERP)
                surf.albedo = lerp(surf.albedo, sampledDetailAlbedo.rgb, detailMask * _DetailAlbedoScale3);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a, detailMask * _DetailSmoothnessScale3);
            #else // default overlay
                surf.albedo = lerp(surf.albedo, BlendMode_Overlay_sRGB(surf.albedo, sampledDetailAlbedo.rgb), detailMask * _DetailAlbedoScale3);
                surf.perceptualRoughness = lerp(surf.perceptualRoughness, BlendMode_Overlay(surf.perceptualRoughness, 1.0 - sampledDetailAlbedo.a), detailMask * _DetailSmoothnessScale3);
            #endif

            UNITY_BRANCH
            if (_DetailNormalMap3_TexelSize.w > 1.0)
            {
                float4 detailNormalMap = SampleTexture(_DetailNormalMap3, sampler_DetailNormalMap, detailUV);
                float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale3);
                #if defined(_DETAILBLEND_LERP)
                    surf.tangentNormal = lerp(surf.tangentNormal, detailNormal, detailMask);
                #else
                    surf.tangentNormal = lerp(surf.tangentNormal, BlendNormals(surf.tangentNormal, detailNormal), detailMask);
                #endif
            }
        }
        #endif


        
    #endif

    surf.albedo.rgb = lerp(dot(surf.albedo.rgb, GRAYSCALE), surf.albedo.rgb, _AlbedoSaturation);
    
    #if defined(EMISSION)
        half3 emissionMap = 1.0;

        UNITY_BRANCH
        if (_EmissionMap_TexelSize.w > 1.0)
        {
            emissionMap = SampleTexture(_EmissionMap, sampler_EmissionMap, mainUV + ParallaxOffsetUV(_EmissionDepth, i.viewDirTS)).rgb;
        }

        emissionMap = lerp(emissionMap, emissionMap * surf.albedo.rgb, _EmissionMultBase);
    
        surf.emission = emissionMap * UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _EmissionColor);
        #if defined(AUDIOLINK)
            surf.emission *= AudioLinkLerp(uint2(1, _AudioLinkEmission)).r;
        #endif

        half3 emissionPulse = sin(_Time.y * _EmissionPulseSpeed) + 1;
        surf.emission = lerp(surf.emission, surf.emission * emissionPulse, _EmissionPulseIntensity);

        #ifdef UNITY_PASS_META
            surf.emission *= _EmissionGIMultiplier;
        #endif
    #endif

    surf.tangentNormal.g *= _FlipNormal ? 1.0 : -1.0;

    surf.reflectance = _Reflectance;
}