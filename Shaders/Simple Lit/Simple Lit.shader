Shader "Simple Lit"
{
    Properties
    {
        [Enum(Opaque, 0, Cutout, 1, Fade, 2, Transparent, 3, Additive, 4, Multiply, 5)] _Mode ("Rendering Mode", Int) = 0

Foldout_SurfaceInputs("Main Maps", Int) = 1
        _Cutoff ("Alpha Cuttoff", Range(0, 1)) = 0.5

        _MainTex ("Base Map", 2D) = "white" {}
        _MainTexArray ("Base Map Array", 2DArray) = "" {}
            _AlbedoSaturation ("Saturation", Float) = 1
            _Color ("Base Color", Color) = (1,1,1,1)

        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _GlossinessMin ("Smoothness Min", Range(0,1)) = 0
        [Gamma] _Metallic ("Metallic", Range(0,1)) = 0
        _MetallicMin ("Metallic Min", Range(0,1)) = 0
        _Occlusion ("Occlusion", Range(0,1)) = 0
        _Reflectance ("Reflectance", Range(0.0, 1.0)) = 0.5

        _MetallicGlossMap ("Packed Mask", 2D) = "white" {}
        _MetallicGlossMapArray ("Packed Mask Array", 2DArray) = "" {}

        // properties used only for texture packing
        _IsPackingMetallicGlossMap ("", Float) = 0
        _MetallicMap ("Metallic Map", 2D) = "black" {}
        [Enum(Red, 0, Green, 1, Blue, 2, Alpha, 3)]  _MetallicMapChannel ("Channel", Int) = 0
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        [Enum(Red, 0, Green, 1, Blue, 2, Alpha, 3)]  _OcclusionMapChannel ("Channel", Int) = 0
        _DetailMaskMap ("Detail Mask", 2D) = "white" {}
        [Enum(Red, 0, Green, 1, Blue, 2, Alpha, 3)]  _DetailMaskMapChannel ("Channel", Int) = 0
        _SmoothnessMap ("Smoothness Map", 2D) = "white" {}
        [Enum(Red, 0, Green, 1, Blue, 2, Alpha, 3)]  _SmoothnessMapChannel ("Channel", Int) = 0
        [ToggleUI] _SmoothnessMapInvert ("Invert", Float) = 0
        [ToggleUI] _DetailMaskMapInvert ("Invert", Float) = 0
        [ToggleUI] _OcclusionMapInvert ("Invert", Float) = 0
        [ToggleUI] _MetallicMapInvert ("Invert", Float) = 0
        


        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
            _BumpMapArray ("Normal Map Array", 2DArray) = "" {}
            _BumpScale ("Scale", Float) = 1

        [ToggleOff(SPECULAR_HIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1
        [ToggleOff(REFLECTIONS_OFF)] _GlossyReflections ("Reflection Probes", Float) = 1

        _SpecularOcclusion ("Specular Occlusion", Range(0,1)) = 0

        [Toggle(GEOMETRIC_SPECULAR_AA)] _GSAA ("Geometric Specular AA", Int) = 0
            [PowerSlider(2)] _specularAntiAliasingVariance ("Variance", Range(0.0, 1.0)) = 0.15
            [PowerSlider(2)] _specularAntiAliasingThreshold ("Threshold", Range(0.0, 1.0)) = 0.1

        Foldout_EmissionInputs("Emission", Int) = 0
        [Toggle(EMISSION)] _EnableEmission ("Emission", Int) = 0
            _EmissionMap ("Emission Map", 2D) = "white" {}
            [Gamma][HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)
            _EmissionDepth("Depth", Float) = 0
            _EmissionMultBase ("Multiply Base", Range(0,1)) = 0
            [Space(10)]_EmissionPulseIntensity ("Pulse Intensity", Range(0,1)) = 0
            _EmissionPulseSpeed ("Pulse Speed", Float) = 1
            [Enum(Disabled, 1000, Bass, 0, Low Mids, 1, High Mids, 2, Treble, 3, Autocorrelator, 27, Filtered Bass, 28)] _AudioLinkEmission ("Audio Link", Int) = 1000

            _EmissionGIMultiplier("GI Multiplier", Float) = 1

Foldout_DetailInputs("Detail Maps", Int) = 0
        _DetailAlbedoMap ("Albedo & Smoothness", 2D) = "linearGrey" {}
        [Enum(Overlay, 0, Screen, 1, Multiply X2, 2, Replace, 3)] _DetailBlendMode ("Blend Mode", Int) = 0
        [Enum(Detail Smoothness, 0, Detail Mask, 1)] _DetailAlbedoAlpha ("Albedo Alpha", Int) = 0
        [Normal] _DetailNormalMap ("Normal Map", 2D) = "bump" {}
            [Enum(UV0, 0, UV1, 1, UV2, 2)]  _DetailMapUV ("Detail UV", Int) = 0
            _DetailDepth("Depth", Float) = 0
            _DetailAlbedoScale ("Albedo Scale", Range(0.0, 1.0)) = 1
            _DetailNormalScale ("Scale", Float) = 1
            _DetailSmoothnessScale ("Smoothness", Range(0.0, 1.0)) = 0

        // properties used only for texture packing
        _IsPackingDetailAlbedo ("", Float) = 0
        _DetailAlbedoPacking ("Albedo Map", 2D) = "linearGrey" {}
        _DetailSmoothnessPacking ("Smoothness Map", 2D) = "white" {}
        [Enum(Red, 0, Green, 1, Blue, 2, Alpha, 3)]  _DetailSmoothnessPackingChannel ("", Int) = 0
        [ToggleUI] _DetailSmoothnessPackingInvert ("Invert", Float) = 0

        _ParallaxMap ("Height Map", 2D) = "white" {}
            _Parallax ("Scale", Range (0, 0.2)) = 0.02
            _ParallaxOffset ("Parallax Offset", Range(-1, 1)) = 0

Foldout_RenderingOptions("Rendering Options", Int) = 0

        [Toggle(BAKEDSPECULAR)] _BakedSpecular ("Baked Specular ", Int) = 0
        [Toggle(NONLINEAR_LIGHTPROBESH)] _NonLinearLightProbeSH("Non-linear Light Probe SH", Int) = 0

        [Toggle(LTCGI)] _LTCGI("LTCGI", Int) = 0
        [Toggle(LTCGI_DIFFUSE_OFF)] _LTCGI_DIFFUSE_OFF("LTCGI Disable Diffuse", Int) = 0

        [HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Int) = 1
        [HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Int) = 0
        [HideInInspector] [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Int) = 1
        [HideInInspector] [Enum(Off, 0, On, 1)] _AlphaToMask ("Alpha To Coverage", Int) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2

        [Enum(None, 0, SH, 1, RNM, 2)] Bakery ("Bakery Mode", Int) = 0
            _RNM0("RNM0", 2D) = "black" {}
            _RNM1("RNM1", 2D) = "black" {}
            _RNM2("RNM2", 2D) = "black" {}

        [Enum(Default, 0, Texture Array, 1, Texture Array Instanced, 2)] _Texture ("Sampling Mode", Int) = 0
            _TextureIndex("Array Index", Int) = 0

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
        ENDCG

        Tags { "RenderType"="Opaque" "Queue"="Geometry" "LTCGI" = "_LTCGI" }

        Pass
        {
            Name "FORWARDBASE"
            Tags { "LightMode"="ForwardBase" }
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ VERTEXLIGHT_ON // already defined in vertex by multi_compile_fwdbase
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            

            #pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local _ BAKERY_SH BAKERY_RNM
            #pragma shader_feature_local SPECULAR_HIGHLIGHTS_OFF
            #pragma shader_feature_local REFLECTIONS_OFF
            #pragma shader_feature_local EMISSION
            #pragma shader_feature_local BAKEDSPECULAR
            #pragma shader_feature_local PARALLAX
            #pragma shader_feature_local GEOMETRIC_SPECULAR_AA
            #pragma shader_feature_local NONLINEAR_LIGHTPROBESH

            #pragma shader_feature_local _TEXTURE_ARRAY
            #pragma shader_feature_local _MASK_MAP
            #pragma shader_feature_local _NORMAL_MAP
            #pragma shader_feature_local _DETAILALBEDO_MAP
            #pragma shader_feature_local _DETAILNORMAL_MAP
            #pragma shader_feature_local _ _DETAILBLEND_SCREEN _DETAILBLEND_MULX2 _DETAILBLEND_LERP

            #pragma shader_feature_local AUDIOLINK
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
            Blend [_SrcBlend] One
            Cull [_Cull]
            ZWrite Off
            ZTest LEqual
            AlphaToMask [_AlphaToMask]

            CGPROGRAM
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local SPECULAR_HIGHLIGHTS_OFF
            #pragma shader_feature_local PARALLAX
            #pragma shader_feature_local GEOMETRIC_SPECULAR_AA
            #pragma shader_feature_local NONLINEAR_LIGHTPROBESH

            #pragma shader_feature_local _TEXTURE_ARRAY
            #pragma shader_feature_local _MASK_MAP
            #pragma shader_feature_local _NORMAL_MAP
            #pragma shader_feature_local _DETAILALBEDO_MAP
            #pragma shader_feature_local _DETAILNORMAL_MAP
            #pragma shader_feature_local _ _DETAILBLEND_SCREEN _DETAILBLEND_MULX2 _DETAILBLEND_LERP
            

            
            #include "PassCGI.cginc"

            ENDCG
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Cull [_Cull]
            ZTest LEqual

            CGPROGRAM
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2

            #pragma shader_feature_local _TEXTURE_ARRAY
            #pragma shader_feature_local _MASK_MAP

            #pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _MODE_FADE

            #include "PassCGI.cginc"
            ENDCG
        }

        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }
            Cull Off

            CGPROGRAM
            #pragma shader_feature EDITOR_VISUALIZATION

            #pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local EMISSION
            #pragma shader_feature_local AUDIOLINK

            #pragma shader_feature_local _TEXTURE_ARRAY
            #pragma shader_feature_local _MASK_MAP
            #pragma shader_feature_local _DETAILALBEDO_MAP
            #pragma shader_feature_local _ _DETAILBLEND_SCREEN _DETAILBLEND_MULX2 _DETAILBLEND_LERP    

            #include "PassCGI.cginc"
            ENDCG
        }
    }
    CustomEditor "z3y.Shaders.SimpleLitSmartGUI"
}
