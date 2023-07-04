
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


#ifdef GENERATION_CODE

struct SurfaceDescriptionInputs
{
    float3 normalWS;
    float3 normalOS;
    float3 normalVS;
    float3 normalTS;
    float3 tangentWS;
    float3 bitangentWS;
    float3 tangentOS;
    float3 tangentVS;
    float3 tangentTS;
    float3 bitangentOS;
    float3 bitangentVS;
    float3 bitangentTS;
    float3 viewDirectionWS;
    float3 viewDirectionOS;
    float3 viewDirectionVS;
    float3 viewDirectionTS;
    float3 positionWS;
    float3 positionOS;
    float3 positionVS;
    float3 positionTS;
    float3 absolutePositionWS;
    float4 screenPosition;
    float4 uv0;
    float4 uv1;
    float4 uv2;
    float4 uv3;
    float3 vertexColor;
    bool FaceSign;
};

SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output = (SurfaceDescriptionInputs)0;

    #if defined(VARYINGS_NEED_NORMAL)
        // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
        float3 unnormalizedNormalWS = input.normalWS;
        const float renormFactor = 1.0 / length(unnormalizedNormalWS);

        output.normalWS = renormFactor * input.normalWS.xyz; // we want a unit length Normal Vector node in shader graph
        output.normalOS = normalize(mul(output.normalWS, (float3x3)UNITY_MATRIX_M)); // transposed multiplication by inverse matrix to handle normal scale
        output.normalVS = mul(output.normalWS, (float3x3)UNITY_MATRIX_I_V); // transposed multiplication by inverse matrix to handle normal scale
        output.normalTS = float3(0.0f, 0.0f, 1.0f);

        #if defined(VARYINGS_NEED_TANGENT)
            // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.tangentWS = renormFactor * input.tangentWS.xyz;
            output.tangentOS = TransformWorldToObjectDir(output.tangentWS);
            output.tangentVS = TransformWorldToViewDir(output.tangentWS);
            output.tangentTS = float3(1.0f, 0.0f, 0.0f);

            #if defined(VARYINGS_NEED_BITANGENT)
                // use bitangent on the fly like in hdrp
                // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
                //float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale(); moved to vertex
                float3 crossSign = input.tangentWS.w;
                float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

                output.bitangentWS = renormFactor * bitang;
                output.bitangentOS = TransformWorldToObjectDir(output.bitangentWS);
                output.bitangentVS = TransformWorldToViewDir(output.bitangentWS);
                output.bitangentTS = float3(0.0f, 1.0f, 0.0f);
            #endif
        #endif
    #endif

    #ifdef VARYINGS_NEED_TANGENT
        output.viewDirectionWS = GetViewDirectionWS(input.positionWS);
        output.viewDirectionOS = TransformWorldToObjectDir(output.viewDirectionWS);
        output.viewDirectionVS = TransformWorldToViewDir(output.viewDirectionWS);

        #if defined(VARYINGS_NEED_NORMAL) && defined(VARYINGS_NEED_BITANGENT)
            float3x3 tangentSpaceTransform = float3x3(output.tangentWS, output.bitangentWS, output.normalWS);
            output.viewDirectionTS = mul(tangentSpaceTransform, output.viewDirectionWS);
        #endif
    #endif

    output.positionWS = input.positionWS;
    output.positionOS = TransformWorldToObject(input.positionWS);
    output.positionVS = TransformWorldToView(input.positionWS);
    output.positionTS = float3(0.0f, 0.0f, 0.0f);
    output.absolutePositionWS = GetAbsolutePositionWS(input.positionWS);

    output.screenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

    #if defined(VARYINGS_NEED_TEXCOORD0)
        output.uv0 = input.texCoord0;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD1)
        output.uv1 = input.texCoord1;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD2)
        output.uv2 = input.texCoord2;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD3)
        output.uv3 = input.texCoord3;
    #endif

    #if defined(VARYINGS_NEED_COLOR)
        output.vertexColor = input.color;
    #endif

    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        output.FaceSign = IS_FRONT_VFACE(input.cullFace, true, false);
    #endif

    return output;
}


#endif