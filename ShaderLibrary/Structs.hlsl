
#define GetCustomMainLightData GetUnityLightData
CustomLightData GetCustomMainLightData(Varyings unpacked)
{
    CustomLightData data = (CustomLightData)0;

#if defined(PIPELINE_BUILTIN) && defined(USING_LIGHT_MULTI_COMPILE)
    data.direction = Unity_SafeNormalize(UnityWorldSpaceLightDir(unpacked.positionWS));
    data.color = _LightColor0.rgb;

    // attenuation
    // my favorite macro from UnityCG /s
    LegacyVaryings legacyVaryings = (LegacyVaryings)0;
    legacyVaryings.pos = unpacked.positionCS;
#ifdef VARYINGS_NEED_SHADOWCOORD
    legacyVaryings._ShadowCoord = unpacked.shadowCoord;
#endif
    UNITY_LIGHT_ATTENUATION(lightAttenuation, legacyVaryings, unpacked.positionWS.xyz);

#if defined(UNITY_PASS_FORWARDBASE) && !defined(SHADOWS_SCREEN)
    lightAttenuation = 1.0;
#endif
    data.attenuation = lightAttenuation;

#if defined(LIGHTMAP_SHADOW_MIXING) && defined(LIGHTMAP_ON)
    data.color *= UnityComputeForwardShadows(unpacked.lightmapUV.xy, unpacked.positionWS, unpacked.shadowCoord);
#endif

#endif

#if defined(PIPELINE_URP)

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord = unpacked.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    float4 shadowCoord = TransformWorldToShadowCoord(unpacked.positionWS);
#else
    float4 shadowCoord = float4(0, 0, 0, 0);
#endif

    Light mainLight = GetMainLight(shadowCoord);

    data.color = mainLight.color;
    data.direction = mainLight.direction;
    data.attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
#endif

    return data;
}

float3 GetViewDirectionWS(float3 positionWS)
{
#ifdef PIPELINE_BUILTIN
    return normalize(UnityWorldSpaceViewDir(positionWS));
#else
    return normalize(GetCameraPositionWS() - positionWS);
#endif
}
