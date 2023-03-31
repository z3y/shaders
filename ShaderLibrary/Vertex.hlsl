

#if defined(UNITY_PASS_SHADOWCASTER) && defined(PIPELINE_URP)
float3 _LightDirection;
#endif

#ifdef GENERATION_CODE
Varyings vert(Attributes input)
#else
Varyings BuildVaryings(Attributes input)
#endif
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #ifdef USE_MODIFYATTRIBUTES
    ModifyAttributes(input);
    #endif

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    #ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    #endif
    #ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    #endif

    // apply vertex position modifiers
    VertexDescription description = (VertexDescription)0;
    description.VertexPosition = positionWS;
    #ifdef ATTRIBUTES_NEED_NORMAL
    description.VertexNormal = normalWS;
    #endif
    #ifdef ATTRIBUTES_NEED_TANGENT
    description.VertexTangent = tangentWS.xyz;
    #endif
    #ifdef USE_VERTEXDESCRIPTION
    VertexDescriptionFunction(input, description);
    #endif
    positionWS = description.VertexPosition;
    #ifdef ATTRIBUTES_NEED_NORMAL
    normalWS = description.VertexNormal;
    #endif
    #ifdef ATTRIBUTES_NEED_TANGENT
    tangentWS.xyz = description.VertexTangent;
    #endif


    LegacyAttributes v = (LegacyAttributes)0;
    LegacyVaryings o = (LegacyVaryings)0;
    v.vertex = float4(input.positionOS.xyz, 1);
    #ifdef ATTRIBUTES_NEED_NORMAL
    v.normal = input.normalOS.xyz;
    #endif


    #if defined(UNITY_PASS_META) && defined(PIPELINE_BUILTIN)
        output.positionCS = UnityMetaVertexPosition(float4(TransformWorldToObject(positionWS), 0), input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
    #elif defined(UNITY_PASS_META) && defined(PIPELINE_URP)
        output.positionCS = MetaVertexPosition(float4(TransformWorldToObject(positionWS), 0), input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
    #else
        output.positionCS = TransformWorldToHClip(positionWS);
    #endif

    #if defined(UNITY_PASS_SHADOWCASTER) && defined(PIPELINE_BUILTIN)
        output.positionCS = TransformWorldToHClip(ApplyShadowBiasNormal(positionWS, normalWS));
        output.positionCS = UnityApplyLinearShadowBias(output.positionCS);
    #endif

    #if defined(UNITY_PASS_SHADOWCASTER) && defined(PIPELINE_URP)
        output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
        #if UNITY_REVERSED_Z
            output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
        #else
            output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
        #endif
    #endif

    #ifdef VARYINGS_NEED_NORMAL
    output.normalWS = normalWS;
    #endif
    #ifdef VARYINGS_NEED_TANGENT
    output.tangentWS = tangentWS;
    #endif
    output.positionWS = positionWS;
    
    #if defined(VARYINGS_NEED_TEXCOORD0)
        output.texCoord0 = input.uv0;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD1)
        output.texCoord1 = input.uv1;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD2)
        output.texCoord2 = input.uv2;
    #endif
    #if defined(VARYINGS_NEED_TEXCOORD3)
        output.texCoord3 = input.uv3;
    #endif

    
    #if defined(LIGHTMAP_ON)
        output.lightmapUV.xy = input.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #endif
    #if defined(DYNAMICLIGHTMAP_ON)
        output.lightmapUV.zw = input.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;;
    #endif

    #if defined(VARYINGS_NEED_COLOR)
        output.color = input.color;
    #endif

    #if defined(FOG_ANY) && defined(PIPELINE_BUILTIN)
        UNITY_TRANSFER_FOG(output, output.positionCS);
    #endif


    #if defined(VARYINGS_NEED_SHADOWCOORD) && defined(PIPELINE_BUILTIN)
        o.pos = output.positionCS;
        o._ShadowCoord = output.shadowCoord;
        UNITY_TRANSFER_SHADOW(o, input.uv1.xy);
        output.shadowCoord = o._ShadowCoord;
        output.positionCS = o.pos;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(PIPELINE_URP)
        VertexPositionInputs vertexPositionInputs = (VertexPositionInputs)0.;
        vertexPositionInputs.positionCS = output.positionCS;
        vertexPositionInputs.positionWS = output.positionWS;
        output.shadowCoord = GetShadowCoord(vertexPositionInputs);
    #endif

    #ifdef EDITOR_VISUALIZATION
        output.vizUV = 0;
        output.lightCoord = 0;
        if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
            output.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, input.uv0.xy, input.uv1.xy, input.uv2.xy, unity_EditorViz_Texture_ST);
        else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
        {
            output.vizUV = input.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
            output.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1)));
        }
    #endif

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
		output.vertexLight = Shade4PointLights
        (
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb,
			unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, output.positionWS, output.normalWS
		);
	#endif

    #ifdef USE_MODIFYVARYINGS
    ModifyVaryings(input, description, output);
    #endif

    return output;
}


#ifdef GENERATION_GRAPH
PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}
#endif