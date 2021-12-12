#define grayscaleVec half3(0.2125, 0.7154, 0.0721)
#define TAU 6.28318530718
#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))

// #define NEED_CENTROID_NORMAL

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