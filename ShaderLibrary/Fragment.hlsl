#include "Packages/com.z3y.shaders/ShaderLibrary/CustomLighting.hlsl"


#ifdef GENERATION_CODE
    half4 frag (Varyings unpacked) : SV_Target
    {
#else
    half4 frag (PackedVaryings packedInput) : SV_Target
    {
        Varyings unpacked = UnpackVaryings(packedInput);
#endif
        UNITY_SETUP_INSTANCE_ID(unpacked);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);


    #ifdef GENERATION_CODE
        SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
        #ifdef USE_SURFACEDESCRIPTION
        SurfaceDescriptionFunction(unpacked, surfaceDescription);
        #endif
    #else
        SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
        SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
    #endif

        half4 finalColor = CustomLighting::ApplyPBRLighting(unpacked, surfaceDescription);

        return finalColor;
}