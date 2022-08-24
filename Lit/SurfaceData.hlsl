Texture2D _MainTex; SamplerState sampler_MainTex;
Texture2D _MetallicGlossMap; SamplerState sampler_MetallicGlossMap;
Texture2D _BumpMap; SamplerState sampler_BumpMap;

Texture2D _DetailHeightBlend;
Texture2D _EmissionMap; SamplerState sampler_EmissionMap;
float4 _EmissionMap_ST; uint _EmissionMap_UV;

half _Cutoff;
half _Glossiness;
half2 _GlossinessRemapping;
half2 _GlossinessRange;
half2 _MetallicRemapping;
half _Reflectance;
half _BumpScale;
half _Metallic;
half _OcclusionStrength;
half _AlbedoSaturation;

half _HeightBlend;
half _HeightBlendInvert;

half _EmissionMultBase;
half _EmissionGIMultiplier;

float _ParallaxOffset;
float _Parallax;
uint _ParallaxSteps;
Texture2D _ParallaxMap;
SamplerState sampler_ParallaxMap;
float4 _ParallaxMap_TexelSize;

Texture2D _DetailMask; SamplerState sampler_DetailMask;
uint _DetailMap_UV;
uint _DetailMask_UV;
float4 _DetailMask_ST;

Texture2D _DetailAlbedoMap; SamplerState sampler_DetailAlbedoMap;
Texture2D _DetailNormalMap; SamplerState sampler_DetailNormalMap;
half4 _DetailColor;
float4 _DetailAlbedoMap_ST;
half _DetailNormalScale;

half _DetailMetallic;
half _DetailGlossiness;

UNITY_INSTANCING_BUFFER_START(InstancedProps)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(half3, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(InstancedProps)

#ifdef _AUDIOLINK_EMISSION
    #include "../ShaderLibrary/AudioLink.cginc"
    uniform uint _AudioLinkEmissionBand;
#endif


void InitializeSurfaceData(inout SurfaceData surf, v2f i, uint facing)
{
    float4 mainColor = UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _Color);
    float4 mainTextureST = UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _MainTex_ST);
    float2 mainUV = i.uv01.xy * mainTextureST.xy + mainTextureST.zw;

    float2 uvs[] = { i.uv01.xy, i.uv01.zw, i.uv23.xy, i.uv23.zw };

    #ifdef _PARALLAXMAP
        float2 parallaxOffset = ParallaxOcclusionMapping(_Parallax, mainUV, i.viewDirTS, _ParallaxMap, sampler_ParallaxMap, _ParallaxMap_TexelSize, _ParallaxSteps, _ParallaxOffset);
        mainUV += parallaxOffset;
    #endif

    half4 mainTexture = _MainTex.Sample(sampler_MainTex, mainUV);

    mainTexture.rgb = lerp(dot(mainTexture.rgb, GRAYSCALE), mainTexture.rgb, _AlbedoSaturation);

    mainTexture *= mainColor;

    surf.albedo = mainTexture.rgb;
    surf.alpha = mainTexture.a;

    

    surf.reflectance = _Reflectance;

    #ifdef _MASKMAP
        half4 maskMap = _MetallicGlossMap.Sample(sampler_MetallicGlossMap, mainUV);
        surf.perceptualRoughness = 1.0 - RemapMinMax(maskMap.a, _GlossinessRange.x, _GlossinessRange.y);
        surf.perceptualRoughness = RemapInverseLerp(surf.perceptualRoughness, _GlossinessRemapping.x, _GlossinessRemapping.y);
        surf.metallic = RemapMinMax(maskMap.r, _MetallicRemapping.x, _MetallicRemapping.y);
        surf.occlusion = lerp(1.0, maskMap.g, _OcclusionStrength);
    #else
        surf.perceptualRoughness = 1.0 - _Glossiness;
        surf.metallic = _Metallic;
        surf.occlusion = 1.0;
    #endif

    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        surf.perceptualRoughness = 1.0 - RemapMinMax(mainTexture.a, _GlossinessRange.x, _GlossinessRange.y);
        surf.perceptualRoughness = RemapInverseLerp(surf.perceptualRoughness, _GlossinessRemapping.x, _GlossinessRemapping.y);
    #endif

#ifdef _NORMALMAP
    half4 normalMap = _BumpMap.Sample(sampler_BumpMap, mainUV);
    surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
#endif

    #ifdef _EMISSION
        half3 emissionColor = UNITY_ACCESS_INSTANCED_PROP(InstancedProps, _EmissionColor);
        surf.emission = emissionColor;
        surf.emission = lerp(surf.emission, surf.emission * surf.albedo, _EmissionMultBase);

        float2 emissionTileOffset = uvs[_EmissionMap_UV] * _EmissionMap_ST.xy + _EmissionMap_ST.zw;
        surf.emission *= _EmissionMap.Sample(sampler_EmissionMap, emissionTileOffset);

            
        #ifdef _AUDIOLINK_EMISSION
            surf.emission *= AudioLinkLerp(uint2(1, _AudioLinkEmissionBand)).r;
        #endif
    #endif


    #ifdef UNITY_PASS_META
        surf.emission *= _EmissionGIMultiplier;
    #endif


    float2 detailUV = uvs[_DetailMap_UV].xy * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    float2 detaildx = ddx(detailUV);
    float2 detaildy = ddy(detailUV);
    
    half detailMask = 1;
    #ifdef _DETAIL_BLENDMASK
        float2 detailMaskUV = uvs[_DetailMask_UV].xy * _DetailMask_ST.xy + _DetailMask_ST.zw;
        detailMask = _DetailMask.Sample(sampler_DetailMask, detailMaskUV);
    #endif

    #ifdef REQUIRE_COLOR
        detailMask *= i.vertexColor.r;
    #endif


    #ifdef _DETAIL_HEIGHTBLEND
        half heightBlend = _DetailHeightBlend.Sample(sampler_MainTex, mainUV);
        detailMask *= saturate(heightBlend * heightBlend * _HeightBlend);
        detailMask = _HeightBlendInvert ? detailMask : 1.0f - detailMask;
    #endif

    #ifdef _DECAL
    UNITY_BRANCH
    if (!any(abs(detailUV - 0.5f) > 0.5f))
    {
    #endif
    

    #ifdef _DETAIL_ALBEDOMAP

        half4 sampledDetailAlbedo = _DetailAlbedoMap.SampleGrad(sampler_DetailAlbedoMap, detailUV, detaildx, detaildy) * _DetailColor;

        detailMask *= sampledDetailAlbedo.a;
            
        #if defined(_DETAILBLEND_SCREEN)
            surf.albedo = lerp(surf.albedo, BlendMode_Screen(surf.albedo, sampledDetailAlbedo.rgb), detailMask);
        #elif defined(_DETAILBLEND_MULX2)
            surf.albedo = lerp(surf.albedo, BlendMode_MultiplyX2(surf.albedo, sampledDetailAlbedo.rgb), detailMask);
        #elif defined(_DETAILBLEND_LERP)
            surf.albedo = lerp(surf.albedo, sampledDetailAlbedo.rgb, detailMask);
        #else // default overlay
            surf.albedo = lerp(surf.albedo, BlendMode_Overlay_sRGB(surf.albedo, sampledDetailAlbedo.rgb), detailMask);
        #endif

        
    #endif

    #ifdef _DETAIL_NORMALMAP
        float4 detailNormalMap = _DetailNormalMap.SampleGrad(sampler_DetailNormalMap, detailUV, detaildx, detaildy);
        float3 detailNormal = UnpackScaleNormal(detailNormalMap, _DetailNormalScale);
        #if defined(_DETAILBLEND_LERP)
            surf.tangentNormal = lerp(surf.tangentNormal, detailNormal, detailMask);
        #else
            surf.tangentNormal = lerp(surf.tangentNormal, BlendNormals(surf.tangentNormal, detailNormal), detailMask);
        #endif
    #endif

    #ifdef _DETAILBLEND_LERP
        
        half detailRoughness = 1.0f - _DetailGlossiness;
        half detailMetallic = _DetailMetallic;
        half detailOcclusion = 1.0f;

        surf.perceptualRoughness = lerp(surf.perceptualRoughness, detailRoughness, detailMask);
        surf.metallic = lerp(surf.metallic, detailMetallic, detailMask);
        surf.occlusion = lerp(surf.occlusion, detailOcclusion, detailMask);

    #endif

    #ifdef _DECAL
    }
    #endif

}