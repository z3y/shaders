#if defined(SHADER_API_MOBILE)
    // stipped keywords
    #undef NONLINEAR_LIGHTPROBESH
    #undef PARALLAX
    #undef LTCGI
    #undef LTCGI_DIFFUSE_OFF
    
    // defines
    #undef VERTEXLIGHT_PS
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


#ifdef LTCGI
    #ifdef SPECULAR_HIGHLIGHTS_OFF
        #define LTCGI_SPECULAR_OFF
    #endif
#include "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc"
#endif