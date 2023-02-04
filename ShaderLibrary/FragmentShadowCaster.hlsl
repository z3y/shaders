#include "LightFunctions.hlsl"

half4 frag (Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);


    SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
    #ifdef USE_SURFACEDESCRIPTION
    SurfaceDescriptionFunction(input, surfaceDescription);
    #endif

    #if defined(_ALPHATEST_ON)
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    #ifdef _ALPHAPREMULTIPLY_ON
        surfaceDescription.Alpha = lerp(surfaceDescription.Alpha, 1.0, surfaceDescription.Metallic);
    #endif

    #if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAFADE_ON)
        half dither = Unity_Dither(surfaceDescription.Alpha, input.positionCS.xy);
        if (dither < 0.0) discard;
    #endif

    return 0;
}