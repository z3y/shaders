inline half OneMinusReflectivityFromMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in unity_ColorSpaceDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline half3 DiffuseAndSpecularFromMetallic (half3 albedo, half metallic, out half3 specColor, out half oneMinusReflectivity)
{
    specColor = lerp (unity_ColorSpaceDielectricSpec.rgb, albedo, metallic);
    oneMinusReflectivity = OneMinusReflectivityFromMetallic(metallic);
    return albedo * oneMinusReflectivity;
}

half3 UnityLightmappingAlbedo (half3 diffuse, half3 specular, half smoothness)
{
    half roughness = 1.0 - smoothness;
    half3 res = diffuse;
    res += specular * roughness * 0.5;
    return res;
}

half _BakeryAlphaDither;

half4 frag (Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);


    SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
    #ifdef USE_SURFACEDESCRIPTION
    SurfaceDescriptionFunction(input, surfaceDescription);
    #endif

    UnityMetaInput o;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

    half3 specColor;
    half oneMinisReflectivity;
    half3 diffuseColor = DiffuseAndSpecularFromMetallic(surfaceDescription.Albedo, surfaceDescription.Metallic, specColor, oneMinisReflectivity);

    #ifdef EDITOR_VISUALIZATION
        o.Albedo = diffuseColor;
        o.VizUV = input.vizUV;
        o.LightCoord = input.lightCoord;
    #else
        o.Albedo = UnityLightmappingAlbedo(diffuseColor, specColor, surfaceDescription.Smoothness);
    #endif
        o.SpecularColor = specColor;
        o.Emission = surfaceDescription.Emission;

    #ifndef EDITOR_VISUALIZATION

    #if defined(_ALPHATEST_ON)
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    // bakery alpha
    if (unity_MetaFragmentControl.w != 0)
    {
        #ifdef _ALPHAPREMULTIPLY_ON
        if (_BakeryAlphaDither > 0.5)
        {
            half dither = Unity_Dither(surfaceDescription.Alpha, input.positionCS.xy);
            return dither < 0.0 ? 0 : 1;
        }
        #endif
        return surfaceDescription.Alpha;
    }
    #endif
    
    return UnityMetaFragment(o);
}