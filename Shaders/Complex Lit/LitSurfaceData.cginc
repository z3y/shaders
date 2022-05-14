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


#define LAYERALBEDOSAMPLER sampler_DetailAlbedoMap
#if !defined(_LAYER1ALBEDO)
    #define LAYERALBEDOSAMPLER sampler_DetailAlbedoMap2
    #if !defined(_LAYER2ALBEDO)
        #define LAYERALBEDOSAMPLER sampler_DetailAlbedoMap3
    #endif
#endif

#define LAYERNORMALSAMPLER sampler_DetailNormalMap
#if !defined(_LAYER1NORMAL)
    #define LAYERNORMALSAMPLER sampler_DetailNormalMap2
    #if !defined(_LAYER2NORMAL)
        #define LAYERNORMALSAMPLER sampler_DetailNormalMap3
    #endif
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
        surf.perceptualRoughness = RemapInverseLerp(surf.perceptualRoughness, _GlossinessRemapping.x, _GlossinessRemapping.y);
        surf.metallic = RemapMinMax(maskMap.r, _MetallicMin, _Metallic);
        surf.occlusion = lerp(1.0, maskMap.g, _Occlusion);
    #else
        surf.perceptualRoughness = 1.0 - _Glossiness;
        surf.metallic = _Metallic;
        surf.occlusion = 1.0;
    #endif

    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        surf.perceptualRoughness = 1.0 - (RemapMinMax(mainTexture.a, _GlossinessMin, _Glossiness));
        surf.perceptualRoughness = RemapInverseLerp(surf.perceptualRoughness, _GlossinessRemapping.x, _GlossinessRemapping.y);
    #endif
  

    half4 normalMap = float4(0.5, 0.5, 1.0, 1.0);
    #ifdef _NORMAL_MAP
        normalMap = SampleTexture(TEXARGS(_BumpMap), TEXARGS(sampler_BumpMap), mainUV);
        surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
    #endif


    #if defined(LAYERS)
        half4 sampledMask = 1.0;

        //#ifdef _LAYERMASK
            float2 detailMaskUV = (i.uv[_DetailMaskUV].xy * _DetailMask_ST.xy) + _DetailMask_ST.zw;
            sampledMask = SampleTexture(_DetailMask, sampler_DetailMask, detailMaskUV);
        //#endif
        // fuck this shader compiler
        // this line has nothing to do with the normal map sampler

        // layer 1
        float2 detailUV1 = TRANSFORM_TEX(i.uv[_DetailMapUV].xy, _DetailAlbedoMap) - (_DetailDepth * i.viewDirTS.xy / i.viewDirTS.z);
        #ifdef _LAYER1ALBEDO
        {
            half detailMask = sampledMask.r;
            half4 sampledDetailAlbedo = SampleTexture(_DetailAlbedoMap, LAYERALBEDOSAMPLER, detailUV1);
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
        }
        #endif

        #ifdef _LAYER1NORMAL
        {
            half detailMask = sampledMask.r;
            float4 detailNormalMap = SampleTexture(_DetailNormalMap, LAYERNORMALSAMPLER, detailUV1);
            float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale);
            #if defined(_DETAILBLEND_LERP)
                surf.tangentNormal = lerp(surf.tangentNormal, detailNormal, detailMask);
            #else
                surf.tangentNormal = lerp(surf.tangentNormal, BlendNormals(surf.tangentNormal, detailNormal), detailMask);
            #endif
        }
        #endif


        // layer 2
        float2 detailUV2 = TRANSFORM_TEX(i.uv[_DetailMapUV2].xy, _DetailAlbedoMap2) - (_DetailDepth2 * i.viewDirTS.xy / i.viewDirTS.z);
        #ifdef _LAYER2ALBEDO
        {
            half detailMask = sampledMask.g;
            half4 sampledDetailAlbedo = SampleTexture(_DetailAlbedoMap2, LAYERALBEDOSAMPLER, detailUV2);
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
        }
        #endif

        #ifdef _LAYER2NORMAL
        {
            half detailMask = sampledMask.g;
            float4 detailNormalMap = SampleTexture(_DetailNormalMap2, LAYERNORMALSAMPLER, detailUV2);
            float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale2);
            #if defined(_DETAILBLEND_LERP)
                surf.tangentNormal = lerp(surf.tangentNormal, detailNormal, detailMask);
            #else
                surf.tangentNormal = lerp(surf.tangentNormal, BlendNormals(surf.tangentNormal, detailNormal), detailMask);
            #endif
        }
        #endif



        // layer 3
        float2 detailUV3 = TRANSFORM_TEX(i.uv[_DetailMapUV3].xy, _DetailAlbedoMap3) - (_DetailDepth3 * i.viewDirTS.xy / i.viewDirTS.z);
        #ifdef _LAYER3ALBEDO
        {
            half detailMask = sampledMask.b;
            half4 sampledDetailAlbedo = SampleTexture(_DetailAlbedoMap3, LAYERALBEDOSAMPLER, detailUV3);
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
        }
        #endif



        

        #ifdef _LAYER3NORMAL
        {
            half detailMask = sampledMask.b;
            float4 detailNormalMap = SampleTexture(_DetailNormalMap3, LAYERNORMALSAMPLER, detailUV3);
            float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale3);
            #if defined(_DETAILBLEND_LERP)
                surf.tangentNormal = lerp(surf.tangentNormal, detailNormal, detailMask);
            #else
                surf.tangentNormal = lerp(surf.tangentNormal, BlendNormals(surf.tangentNormal, detailNormal), detailMask);
            #endif
        }
        #endif


        

        


        
    #endif

    surf.albedo.rgb = lerp(dot(surf.albedo.rgb, GRAYSCALE), surf.albedo.rgb, _AlbedoSaturation);
    
    #if defined(EMISSION)
        half3 emissionMap = 1.0;
        float2 emissionUV = TRANSFORM_TEX(i.uv[_EmissionMap_UV].zw, _EmissionMap);

        emissionMap = SampleTexture(_EmissionMap, sampler_EmissionMap, emissionUV - (_EmissionDepth * i.viewDirTS.xy / i.viewDirTS.z)).rgb;

        emissionMap = lerp(emissionMap, emissionMap * surf.albedo.rgb, _EmissionMultBase);
    
        surf.emission = emissionMap * UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _EmissionColor);
        #if defined(AUDIOLINK)
            surf.emission *= AudioLinkLerp(uint2(1, _AudioLinkEmission)).r;
        #endif

        #ifdef UNITY_PASS_META
            surf.emission *= _EmissionGIMultiplier;
        #endif
    #endif

    surf.tangentNormal.g *= -1.0;

    surf.reflectance = _Reflectance;
}