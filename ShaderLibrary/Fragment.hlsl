#include "Packages/com.z3y.shaders/ShaderLibrary/CustomLighting.hlsl"

half4 frag (Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
    #ifdef USE_SURFACEDESCRIPTION
    SurfaceDescriptionFunction(input, surfaceDescription);
    #endif

    half4 finalColor = CustomLighting::ApplyPBRLighting(input, surfaceDescription);

    #if defined(FOG_ANY) && defined(PIPELINE_BUILTIN)
        UNITY_APPLY_FOG(input.fogCoord, finalColor);
    #endif

    return finalColor;
}