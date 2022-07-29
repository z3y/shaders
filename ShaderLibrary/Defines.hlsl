#ifndef UNITY_PBS_USE_BRDF1
    #define SHADER_API_MOBILE
#endif

#if defined(SHADER_API_MOBILE)
    // #undef BAKERY_RNM
    // #undef BAKERY_SH
    #undef _DETAIL_NORMALMAP
    #undef _PARALLAXMAP
    #undef NONLINEAR_LIGHTPROBESH
    #undef _BICUBICLIGHTMAP
    #undef LTCGI
    #undef LTCGI_DIFFUSE_OFF
#endif

#if defined(BAKERY_RNM) || defined(_PARALLAXMAP)
    #define REQUIRE_VIEWDIRTS
#endif

#ifndef SHADER_API_MOBILE
    #define VERTEXLIGHT_PS
#endif

#if defined(LTCGI)
    #ifdef _SPECULARHIGHLIGHTS_OFF
        #define LTCGI_SPECULAR_OFF
    #endif
    #include "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc"
#endif