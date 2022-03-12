#ifndef BAKERY_INCLUDED
#define BAKERY_INCLUDED

Texture2D _RNM0, _RNM1, _RNM2;
SamplerState sampler_RNM0, sampler_RNM1, sampler_RNM2;
float4 _RNM0_TexelSize;

void BakeryRNMLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalTS, float3 viewDirTS, float3 viewDir, half roughness, half3 f0)
{
#ifdef BAKERY_RNM
    normalTS.g *= -1;
    float3 rnm0 = DecodeLightmap(_RNM0.Sample(sampler_RNM0, lightmapUV));
    float3 rnm1 = DecodeLightmap(_RNM1.Sample(sampler_RNM1, lightmapUV));
    float3 rnm2 = DecodeLightmap(_RNM2.Sample(sampler_RNM2, lightmapUV));

    const float3 rnmBasis0 = float3(0.816496580927726f, 0.0f, 0.5773502691896258f);
    const float3 rnmBasis1 = float3(-0.4082482904638631f, 0.7071067811865475f, 0.5773502691896258f);
    const float3 rnmBasis2 = float3(-0.4082482904638631f, -0.7071067811865475f, 0.5773502691896258f);

    lightMap =    saturate(dot(rnmBasis0, normalTS)) * rnm0
                + saturate(dot(rnmBasis1, normalTS)) * rnm1
                + saturate(dot(rnmBasis2, normalTS)) * rnm2;

    #ifdef BAKERY_LMSPEC
        float3 viewDirT = -normalize(viewDirTS);
        float3 dominantDirT = rnmBasis0 * dot(rnm0, GRAYSCALE) +
                                rnmBasis1 * dot(rnm1, GRAYSCALE) +
                                rnmBasis2 * dot(rnm2, GRAYSCALE);

        float3 dominantDirTN = normalize(dominantDirT);
        half3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
                            saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
                            saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;

        half3 halfDir = Unity_SafeNormalize(dominantDirTN - viewDirT);
        half NoH = saturate(dot(normalTS, halfDir));
        half spec = D_GGX(NoH, roughness);
        directSpecular += spec * specColor * EnvBRDFMultiscatter(DFGLut, f0);
    #endif

#endif
}

void BakerySHLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalWS, float3 viewDir, half roughness, half3 f0)
{
    #ifdef BAKERY_SH

        half3 L0 = lightMap;
        float3 nL1x = _RNM0.Sample(sampler_RNM0, lightmapUV) * 2.0 - 1.0;
        float3 nL1y = _RNM1.Sample(sampler_RNM1, lightmapUV) * 2.0 - 1.0;
        float3 nL1z = _RNM2.Sample(sampler_RNM2, lightmapUV) * 2.0 - 1.0;
        float3 L1x = nL1x * L0 * 2.0;
        float3 L1y = nL1y * L0 * 2.0;
        float3 L1z = nL1z * L0 * 2.0;

        #ifdef BAKERY_SHNONLINEAR
            float lumaL0 = dot(L0, float(1));
            float lumaL1x = dot(L1x, float(1));
            float lumaL1y = dot(L1y, float(1));
            float lumaL1z = dot(L1z, float(1));
            float lumaSH = shEvaluateDiffuseL1Geomerics_local(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWS);

            lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
            float regularLumaSH = dot(lightMap, 1.0);
            lightMap *= lerp(1.0, lumaSH / regularLumaSH, saturate(regularLumaSH * 16.0));
        #else
            lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
        #endif

        #ifdef BAKERY_LMSPEC
            float3 dominantDir = float3(dot(nL1x, GRAYSCALE), dot(nL1y, GRAYSCALE), dot(nL1z, GRAYSCALE));
            float3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + viewDir);
            half NoH = saturate(dot(normalWS, halfDir));
            half spec = D_GGX(NoH, roughness);
            half3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
            dominantDir = normalize(dominantDir);

            directSpecular += max(spec * sh, 0.0) * EnvBRDFMultiscatter(DFGLut, f0);
        #endif
        
    #endif
}


#endif