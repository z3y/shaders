#define grayscaleVec half3(0.2125, 0.7154, 0.0721)
#define TAU 6.28318530718
#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
#define VERTEXLIGHT_PS

// #define NEED_CENTROID_NORMAL

// #define SHADER_API_MOBILE


#if defined(SHADER_API_MOBILE)
    // #undef _MASK_MAP
    #undef _NORMAL_MAP
    #define SPECULAR_HIGHLIGHTS_OFF
    // #define REFLECTIONS_OFF

    #undef _DETAILALBEDO_MAP
    #undef _DETAILNORMAL_MAP
    #undef GEOMETRIC_SPECULAR_AA
    #undef BICUBIC_LIGHTMAP
    #undef NONLINEAR_LIGHTPROBESH
    #undef PARALLAX
    #undef BAKEDSPECULAR
    #undef BAKERY_RNM
    #undef BAKERY_SH
    #undef DIRLIGHTMAP_COMBINED
    #undef DYNAMICLIGHTMAP_ON
    #undef NEED_CENTROID_NORMAL
    #undef _TEXTURE_STOCHASTIC
    #undef VERTEXLIGHT_PS
#endif

#if !defined(_TEXTURE_ARRAY) && !defined(_TEXTURE_ARRAY_INSTANCED)
    #define _TEXTURE_DEFAULT
#endif

#if defined(_TEXTURE_ARRAY) || defined(_TEXTURE_ARRAY_INSTANCED)
    #undef PARALLAX
#endif

#if defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_META) || defined(UNITY_PASS_FORWARDADD)
    #define NEED_TANGENT_BITANGENT
    #define NEED_WORLD_POS
    #define NEED_WORLD_NORMAL
#endif

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2) 
    #define NEED_FOG
#endif

#ifdef UNITY_PASS_META
    #include "UnityMetaPass.cginc"
#endif

#if defined(PARALLAX)
    #define NEED_PARALLAX_DIR
#endif


#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META) || defined(_TEXTURE_ARRAY)
    #define NEED_UV2
#endif

#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
    #define NEED_SCREEN_POS
#endif

#ifdef VERTEXLIGHT_ON
    #define NEED_WORLD_NORMAL
    #define NEED_WORLD_POS
#endif



#if !defined(LIGHTMAP_ON) || !defined(UNITY_PASS_FORWARDBASE)
    #undef BAKERY_SH
    #undef BAKERY_RNM
#endif

#ifdef LIGHTMAP_ON
    #undef BAKERY_VOLUME
#endif

#ifdef LIGHTMAP_ON
    #if defined(BAKERY_RNM) || defined(BAKERY_SH) || defined(BAKERY_VERTEXLM)
        #define BAKERYLM_ENABLED
        #undef DIRLIGHTMAP_COMBINED
    #endif
#endif

#ifdef SHADER_API_MOBILE
    #undef BAKERY_BICUBIC
    #undef BAKERY_BICUBIC
#endif

#if defined(BAKERY_SH) || defined(BAKERY_RNM) || defined(BAKERY_VOLUME)

    #ifdef BAKERY_SH
        #define BAKERY_SHNONLINEAR
    #endif

    #define NEED_PARALLAX_DIR
    
    #ifdef BAKEDSPECULAR
        #define _BAKERY_LMSPEC
        #define BAKERY_LMSPEC
    #endif

    #include "Bakery.cginc"
#endif