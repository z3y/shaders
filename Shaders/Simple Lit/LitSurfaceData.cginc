half4 SampleTexture(Texture2D t, SamplerState s, float2 uv)
{
    return t.Sample(s, uv);
}

#define TEXARGS(tex) tex

void InitializeLitSurfaceData(inout SurfaceData surf, v2f i)
{
    half2 mainUV = i.uv[0].zw;
    half4 mainTexture = SampleTexture(TEXARGS(_MainTex), TEXARGS(sampler_MainTex), mainUV);
    mainTexture *= _Color;
    surf.albedo = mainTexture.rgb;
    surf.alpha = 1.0;


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
    
    #if defined(EMISSION)
        half3 emissionMap = 1.0;

        UNITY_BRANCH
        if (_EmissionMap_TexelSize.w > 1.0)
        {
            emissionMap = SampleTexture(_EmissionMap, sampler_EmissionMap, mainUV).rgb;
        }

        emissionMap = lerp(emissionMap, emissionMap * surf.albedo.rgb, _EmissionMultBase);
        surf.emission = emissionMap * _EmissionColor;

        #ifdef UNITY_PASS_META
            surf.emission *= _EmissionGIMultiplier;
        #endif
    #endif

    surf.tangentNormal.g *= -1.0;
    surf.reflectance = 0.5;
}