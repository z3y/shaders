half4 frag (Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);


    SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
    #ifdef USE_SURFACEDESCRIPTION
    SurfaceDescriptionFunction(input, surfaceDescription);
    #endif

    half4 finalColor = half4(surfaceDescription.Albedo, surfaceDescription.Alpha);

    #ifdef FOG_ANY
        UNITY_APPLY_FOG(input.fogCoord, finalColor);
    #endif

    return finalColor;
}