Shader "Lit/Simple"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)

        _MetallicGlossMap("Packed Mask", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _GlossinessMin ("Smoothness Min", Range(0,1)) = 0
        _Metallic ("Metallic", Range(0,1)) = 0
        _MetallicMin ("Metallic Min", Range(0,1)) = 0
        _Occlusion ("Occlusion", Range(0,1)) = 0

        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Scale", Float) = 1

        [ToggleOff(SPECULAR_HIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1
        [ToggleOff(REFLECTIONS_OFF)] _GlossyReflections ("Reflection Probes", Float) = 1

        [Toggle(GEOMETRIC_SPECULAR_AA)] _GSAA ("Geometric Specular AA", Int) = 0
        [PowerSlider(2)] _specularAntiAliasingVariance ("Variance", Range(0.0, 1.0)) = 0.15
        [PowerSlider(2)] _specularAntiAliasingThreshold ("Threshold", Range(0.0, 1.0)) = 0.1

        [Toggle(EMISSION)] _EnableEmission ("Emission", Int) = 0
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [Gamma][HDR] _EmissionColor ("Emission Color", Color) = (1,1,1)
        _EmissionMultBase ("Multiply Base", Range(0,1)) = 0
        _EmissionGIMultiplier("GI Multiplier", Float) = 1


        [Toggle(BAKEDSPECULAR)] _BakedSpecular ("Baked Specular ", Int) = 0
        [Toggle(NONLINEAR_LIGHTPROBESH)] _NonLinearLightProbeSH("Non-linear Light Probe SH", Int) = 0

        [Toggle(LTCGI)] _LTCGI("LTCGI", Int) = 0
        [Toggle(LTCGI_DIFFUSE_OFF)] _LTCGI_DIFFUSE_OFF("LTCGI Disable Diffuse", Int) = 0

        [Enum(None, 0, SH, 1, RNM, 2)] Bakery ("Bakery Mode", Int) = 0
        [HideInInspector] [NonModifiableTextureData] _DFG ("DFG Lut", 2D) = "black" {}

    }

CGINCLUDE
#ifndef UNITY_PBS_USE_BRDF1
    #ifndef SHADER_API_MOBILE
        #define SHADER_API_MOBILE
    #endif
#endif
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest // probably doesnt do anything

/*RantBegin
It really sucks that this Unity version doesn't let us use pragmas in cgincs and I have to edit the shader
It also sucks that I cant use an #ifdef with #pragma skip_variants
RantEnd*/

//ShaderConfigBegin



#define VERTEXLIGHT_PS



#define BAKERY_SHNONLINEAR
#define BICUBIC_LIGHTMAP
#pragma skip_variants LOD_FADE_CROSSFADE

#define UNITY_LIGHT_PROBE_PROXY_VOLUME 0


//ShaderConfigEnd
ENDCG

    SubShader
    {
        CGINCLUDE
        #pragma target 4.5
        #define _NORMAL_MAP
        #define _MASK_MAP
        ENDCG

        Tags { "RenderType"="Opaque" "Queue"="Geometry" "LTCGI" = "_LTCGI" }

        Pass
        {
            Name "FORWARDBASE"
            Tags { "LightMode"="ForwardBase" }
            ZWrite On
            Cull Back
            Blend One Zero

            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ VERTEXLIGHT_ON // already defined in vertex by multi_compile_fwdbase
            
            #pragma shader_feature_local _ BAKERY_SH BAKERY_RNM
            #pragma shader_feature_local SPECULAR_HIGHLIGHTS_OFF
            #pragma shader_feature_local REFLECTIONS_OFF
            #pragma shader_feature_local EMISSION
            #pragma shader_feature_local BAKEDSPECULAR
            #pragma shader_feature_local GEOMETRIC_SPECULAR_AA
            #pragma shader_feature_local NONLINEAR_LIGHTPROBESH

            #pragma shader_feature_local LTCGI
            #pragma shader_feature_local LTCGI_DIFFUSE_OFF

            #include "PassCGI.cginc"
            ENDCG
        }

        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode"="ForwardAdd" }
            Fog { Color (0,0,0,0) }
            Blend One One
            Cull Back
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #pragma shader_feature_local SPECULAR_HIGHLIGHTS_OFF
            #pragma shader_feature_local GEOMETRIC_SPECULAR_AA
            #pragma shader_feature_local NONLINEAR_LIGHTPROBESH
            #include "PassCGI.cginc"

            ENDCG
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Cull Back
            ZTest LEqual

            CGPROGRAM
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
            #include "PassCGI.cginc"
            ENDCG
        }

        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }
            Cull Off

            CGPROGRAM
            #pragma shader_feature_local EMISSION
            #include "PassCGI.cginc"
            ENDCG
        }
    }
    CustomEditor "z3y.Shaders.SimpleLitSmartGUI"
    Fallback "VRChat/Mobile/Lightmapped"
}
