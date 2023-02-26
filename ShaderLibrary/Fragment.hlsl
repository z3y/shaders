#include "Packages/com.z3y.shaders/ShaderLibrary/CustomLighting.hlsl"

half4 frag (Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #ifdef LOD_FADE_CROSSFADE
    UNITY_APPLY_DITHER_CROSSFADE(input.positionCS);
    #endif

    SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
    #ifdef USE_SURFACEDESCRIPTION
    SurfaceDescriptionFunction(input, surfaceDescription);
    #endif

    half4 finalColor = CustomLighting::ApplyPBRLighting(input, surfaceDescription);


    return finalColor;
}