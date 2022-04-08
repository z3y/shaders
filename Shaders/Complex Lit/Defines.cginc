#if defined(SHADER_API_MOBILE)
    // keywords
    #undef BAKERY_SH
    #undef BAKERY_RNM
    #undef BAKEDSPECULAR
    #undef PARALLAX
    #undef NONLINEAR_LIGHTPROBESH
    #undef _LAYER1NORMAL
    #undef _LAYER2NORMAL
    #undef _LAYER3NORMAL
    #undef AUDIOLINK
    #undef LTCGI
    #undef LTCGI_DIFFUSE_OFF

    // multicompiles
    #undef VERTEXLIGHT_PS
    #undef DIRLIGHTMAP_COMBINED
    #undef SHADOWS_SCREEN
    #undef DYNAMICLIGHTMAP_ON
    #undef LOD_FADE_CROSSFADE

#ifndef LIGHTMAP_ON
    #define LIGHTPROBE_VERTEX
#endif

#endif

#ifdef UNITY_PASS_META
    #include "UnityMetaPass.cginc"
#endif

#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
    #define NEED_SCREEN_POS
#endif

#if defined(_LAYER1ALBEDO) || defined (_LAYER2ALBEDO) || defined (_LAYER3ALBEDO) || defined (_LAYER1NORMAL) || defined (_LAYER2NORMAL) || defined (_LAYER3NORMAL)
#define LAYERS
#endif


#if defined(LTCGI) && defined(ALLOW_LTCGI)
    #ifdef SPECULAR_HIGHLIGHTS_OFF
        #define LTCGI_SPECULAR_OFF
    #endif
#include "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc"
#endif