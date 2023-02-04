#ifndef UNITY_PBS_USE_BRDF1
    #define SHADER_API_MOBILE
#endif

#if defined(SHADER_API_MOBILE)
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