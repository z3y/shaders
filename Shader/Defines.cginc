#define grayscaleVec half3(0.2125, 0.7154, 0.0721)
#define TAU 6.28318530718
#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))

// #define NEED_CENTROID_NORMAL

// #define SHADER_API_MOBILE
#if defined(SHADER_API_MOBILE)
    #undef BAKERY_SH
    #undef BAKERY_RNM
    #undef BICUBIC_LIGHTMAP
    #define SPECULAR_HIGHLIGHTS_OFF
    #undef PARALLAX
    #undef NONLINEAR_LIGHTPROBESH
    #undef BAKEDSPECULAR
    #undef _DETAILALBEDO_MAP
    #undef _DETAILNORMAL_MAP
#endif

#if defined(TEXTUREARRAY)
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

#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
    #define NEED_SCREEN_POS
#endif

#define TRANSFORMTEX_VERTEX

#if defined(_DETAILALBEDO_MAP) || defined(_DETAILNORMAL_MAP) || defined(PARALLAX)
    #undef TRANSFORMTEX_VERTEX
#endif

#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
    #undef TRANSFORMTEX_VERTEX
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