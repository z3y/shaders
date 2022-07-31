void InitializeDefaultVertex(appdata_all v, inout v2f o)
{
    UNITY_INITIALIZE_OUTPUT(v2f, o);
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    #ifdef UNITY_PASS_META
        o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
    #else
        #if !defined(UNITY_PASS_SHADOWCASTER)
            o.pos = UnityObjectToClipPos(v.vertex);
        #endif
    #endif

    o.uv[0] = v.uv0.xy;
    o.uv[1] = v.uv1.xy;
    o.uv[2] = v.uv2.xy;
    o.uv[3] = v.uv3.xy;

    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
    o.bitangent = cross(o.tangent, o.worldNormal) * v.tangent.w;
    o.worldPos = mul(unity_ObjectToWorld, v.vertex);

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
		o.vertexLight = Shade4PointLights
        (
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb,
			unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, o.worldPos,  o.worldNormal
		);
	#endif

    #ifdef UNITY_PASS_SHADOWCASTER
        o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
        o.pos = UnityApplyLinearShadowBias(o.pos);
        TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
    #else
        UNITY_TRANSFER_SHADOW(o, o.uv[1]);
        UNITY_TRANSFER_FOG(o,o.pos);
    #endif

    #if defined(REQUIRE_VERTEXCOLOR)
        o.color = v.color;
    #endif

    #if defined(REQUIRE_VIEWDIRTS)
        TANGENT_SPACE_ROTATION;
        o.viewDirTS = mul (rotation, ObjSpaceViewDir(v.vertex));
    #endif

    #if defined(REQUIRE_SCREENPOS)
        o.screenPos = ComputeScreenPos(o.pos);
    #endif
}

