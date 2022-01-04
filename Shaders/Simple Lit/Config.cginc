
// handle non important lights in the pixel shader, looks the same as important point lights but done in one pass
// requires removing skip_variant in the .shader file
#define VERTEXLIGHT_PS


// from Valve GDC Talk
// http://media.steampowered.com/apps/valve/2015/Alex_Vlachos_Advanced_VR_Rendering_GDC2015.pdf

// used for specular antialiasing 
// #define NEED_CENTROID_NORMAL

// requires normal maps with hemi octahedron encoding
// #define HEMIOCTAHEDRON_DECODING


// features can globaly be defined here without using keywords
// #define NONLINEAR_LIGHTPROBESH
// #define BAKERY_RNM
// #define BAKERY_SH




// disabled features for mobile/quest
// only ones that are probably safe to enable if needed, the rest is disabled
#if defined(SHADER_API_MOBILE)
    // #undef _MASK_MAP
    #undef _NORMAL_MAP
    #define SPECULAR_HIGHLIGHTS_OFF
    // #define REFLECTIONS_OFF
    #undef GEOMETRIC_SPECULAR_AA
    #undef _DETAILALBEDO_MAP
#endif