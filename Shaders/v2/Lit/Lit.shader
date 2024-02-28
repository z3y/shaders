Shader "Hidden/z3y/Lit v2"
{

Properties
{
    [Header(Legacy version of the shader only kept for)]
    [Header(compatbility with older projects)]
    ResetFix (" ", Float) = 0
    [HideInInspector] [NonModifiableTextureData] _DFG ("DFG Lut", 2D) = "black" {}
    [HideInInspector] Foldout_RenderingOptions ("Rendering Options", Float) = 0
        
    [HideInInspector] [Enum(Opaque, 0, Cutout A2C, 1, Fade, 2, Transparent, 3, Additive, 4, Multiply, 5)] _Mode ("Rendering Mode", Int) = 0


    [HideInInspector] _Cutoff ("Alpha Cuttoff", Range(0.001, 1)) = 0.5
    [HideInInspector] _CutoutSharpness ("Sharpness", Range(1, 0.0001)) = 0.0001
    [HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Int) = 1
    [HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Int) = 0
    [HideInInspector] [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Int) = 1
    [HideInInspector] [HideInInspector] [Enum(Off, 0, On, 1)] _AlphaToMask ("Alpha To Coverage", Int) = 0
    [HideInInspector] [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2
        

    [HideInInspector] Foldout_SurfaceInputs ("Surface Inputs", Float) = 1
    [HideInInspector] _MainTex ("Albedo", 2D) = "white" {}
    [HideInInspector] _Color ("Color", Color) = (1,1,1,1)
    [HideInInspector] _AlbedoSaturation ("Saturation", Float) = 1



    [HideInInspector] _Metallic ("Metallic", Range(0,1)) = 0
    [HideInInspector] _Glossiness ("Smoothness", Range(0,1)) = 0.5
   [HideInInspector]  _Reflectance ("Reflectance", Range(0.0, 1.0)) = 0.5

    [HideInInspector] _MetallicGlossMap("Mask Map", 2D) = "white" {}
    [HideInInspector] _MetallicRemapping ("Metallic Remap", Vector) = (0,1,0,1)
    [HideInInspector] _GlossinessRange ("Smoothness Range", Vector) = (0,1,0,1)
    [HideInInspector] _GlossinessRemapping ("Smoothness Remap", Vector) = (0,1,0,1)
    [HideInInspector] _OcclusionStrength ("Occlusion", Range(0,1)) = 1



    [HideInInspector] [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
    [HideInInspector] _BumpScale ("Scale", Float) = 1
    
	[HideInInspector] [Toggle(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)] _SmoothnessAlbedoAlpha ("Smoothness Albedo Alpha", Int) = 0
	[HideInInspector] _ParallaxMap ("Height Map", 2D) = "white" {}
    [HideInInspector] _Parallax ("Scale", Range (0, 0.2)) = 0.02
    [HideInInspector] _ParallaxOffset ("Parallax Offset", Range(-1, 1)) = 0
    [HideInInspector] [IntRange] _ParallaxSteps ("Steps", Range(1, 32)) = 16


    [HideInInspector] _SpecularOcclusion ("Specular Occlusion", Range(0,3)) = 1


    [HideInInspector] Foldout_Emission ("Emission", Float) = 0

    [HideInInspector] [Toggle(_EMISSION)] _EmissionToggle ("Enable Emission", Int) = 0

    [HideInInspector] _EmissionMap ("Emission Map", 2D) = "white" {}
    [HideInInspector] [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)
    [HideInInspector] [HideInInspector] _EmissionColorLDR ("Emission Color", Color) = (1,1,1)
    [HideInInspector] [ToggleUI] _UseEmissionIntensity ("Use Emission Intensity", Int) = 0
    [HideInInspector] _EmissionIntensity ("Intensity", Float) = 1

    [HideInInspector] [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _EmissionMap_UV ("UV", Int) = 0
    [HideInInspector] _EmissionMultBase ("Multiply Base", Range(0,1)) = 0
    [HideInInspector] _EmissionGIMultiplier ("GI Multiplier", Float) = 1

    [HideInInspector] [Toggle(_AUDIOLINK_EMISSION)] _AudioLinkEmissionToggle ("Audio Link", Float) = 0
    [HideInInspector] [Enum(Bass, 0, Low Mids, 1, High Mids, 2, Treble, 3)] _AudioLinkEmissionBand ("Band", Int) = 0

    [HideInInspector] Foldout_DetailFoldout ("Detail Inputs", Float) = 0
    [HideInInspector] [Enum(Overlay, 0, Screen, 1, Multiply X2, 2, Replace, 3)] _DetailBlendMode ("Blend Mode", Int) = 0

    [HideInInspector] _DetailMask ("Blend Mask", 2D) = "white" {}
    [HideInInspector] [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _DetailMask_UV ("UV", Int) = 0


    [HideInInspector] _DetailAlbedoMap ("Albedo", 2D) = "white" {}
    [HideInInspector] _DetailColor ("Color", Color) = (1,1,1,1)
    [HideInInspector] [Normal]_DetailNormalMap ("Normal Map", 2D) = "bump" {}
    [HideInInspector] _DetailNormalScale ("Scale", Float) = 1

    [HideInInspector] _DetailMetallic ("Metallic", Range(0,1)) = 0
    [HideInInspector] _DetailGlossiness ("Smoothness", Range(0,1)) = 0.5

    [HideInInspector] [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _DetailMap_UV ("UV", Int) = 0
    [HideInInspector] [Toggle(_DECAL)] _IsDecal ("Use as Decal", Float) = 0


    [HideInInspector] _DetailHeightBlend ("Height Blend", 2D) = "white" {}
    [HideInInspector] _HeightBlend ("Blend", Float) = 5
    [HideInInspector] [Toggle] _HeightBlendInvert ("Blend Invert", Float) = 0

    [HideInInspector] Foldout_WindFoldout ("Wind", Float) = 0
    [HideInInspector] [Toggle(_WIND)] _WindToggle ("Enable Wind", Float) = 0
    [HideInInspector] _WindNoise ("Noise RGB", 2D) = "black" {}
    [HideInInspector] _WindScale ("Noise Scale", Float) = 0.02
    [HideInInspector] [PowerSlider(2)] _WindSpeed ("Speed", Range(0,5)) = 0.05
    [HideInInspector] _WindIntensity ("Intensity XYZ", Vector) = (0.1,0.1,0.1,0)

    [HideInInspector] Foldout_AvancedSettings ("Additional Settings", Float) = 0

    [HideInInspector] [ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1
    [HideInInspector] [ToggleOff(_GLOSSYREFLECTIONS_OFF)] _GlossyReflections ("Reflections", Float) = 1
    [HideInInspector] [Toggle(FORCE_SPECCUBE_BOX_PROJECTION)] _ForceBoxProjection ("Force Box Projection", Float) = 0
    [HideInInspector] [ToggleUI] _BlendReflectionProbes ("Blend Reflection Probes", Float) = 1

    [HideInInspector] [Toggle(_GEOMETRICSPECULAR_AA)] _GSAA ("Geometric Specular AA", Int) = 0
    [HideInInspector] [PowerSlider(2)] _specularAntiAliasingVariance ("Variance", Range(0.0, 1.0)) = 0.15
    [HideInInspector] [PowerSlider(2)] _specularAntiAliasingThreshold ("Threshold", Range(0.0, 1.0)) = 0.1

    [HideInInspector] [Toggle(LTCGI)] _LTCGI("LTCGI", Int) = 0
    [HideInInspector] [Toggle(LTCGI_DIFFUSE_OFF)] _LTCGI_DIFFUSE_OFF("LTCGI Disable Diffuse", Int) = 0
    [HideInInspector] [Toggle(_LIGHTMAPPED_SPECULAR)] _LightmappedSpecular ("Lightmapped Specular ", Int) = 0
    [HideInInspector] [Toggle(_BICUBICLIGHTMAP)] _BicubicLightmap ("Bicubic Lightmap", Float) = 0

    [HideInInspector] [Enum(None, 0, SH, 1, RNM, 2, MONOSH, 3)] Bakery ("Bakery Mode", Int) = 0
    [HideInInspector] [ToggleOff(BAKERY_SHNONLINEAR_OFF)] _BAKERY_SHNONLINEAR ("Non-linear Lightmap SH", Float) = 1
    [HideInInspector] [Toggle(NONLINEAR_LIGHTPROBESH)] _NonLinearLightProbeSH ("Non-linear Light Probe SH", Int) = 0

    // wip
    // [Toggle(_REFRACTION)] _Refraction ("Refraction", Int) = 0
    // _RefractionRatio ("Ratio", Range(0,1)) = 0.95
    // _RefractionIntensity ("Intensity", Range(0,1)) = 1

    [HideInInspector] _AnisotropyMap ("Anisotropy", 2D) = "black" {}
    [HideInInspector] _Anisotropy ("", Range(-1.0, 1.0)) = 0


}


CGINCLUDE

#pragma target 4.5
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"
#ifdef UNITY_PASS_META
    #include "UnityMetaPass.cginc"
#endif

// #ifdef _SPECULARHIGHLIGHTS_OFF
//     #define LTCGI_SPECULAR_OFF
// #endif

//ConfigStart
#define _ALLOW_LPPV
#define FIX_BLACK_LEVEL
#pragma skip_variants LOD_FADE_CROSSFADE
#pragma skip_variants LTCGI
#pragma skip_variants LTCGI_DIFFUSE_OFF

//ConfigEnd

#include "../ShaderLibrary/Defines.hlsl"
#include "Vertex.hlsl"
#include "Fragment.hlsl"

ENDCG

SubShader
{
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
        #pragma skip_variants LIGHTPROBE_SH
        
        #pragma multi_compile _ LOD_FADE_CROSSFADE
        #pragma multi_compile_fragment _ VERTEXLIGHT_ON

        #pragma shader_feature_local _ BAKERY_SH BAKERY_RNM BAKERY_MONOSH

        #ifndef SHADER_API_MOBILE
            #pragma shader_feature_local_fragment _BICUBICLIGHTMAP
            #pragma shader_feature_local_fragment NONLINEAR_LIGHTPROBESH
            #pragma shader_feature_local_fragment BAKERY_SHNONLINEAR_OFF

            #pragma shader_feature_local_fragment LTCGI
            #pragma shader_feature_local_fragment LTCGI_DIFFUSE_OFF

            #pragma shader_feature_local _PARALLAXMAP
        #endif
        #ifdef SHADER_API_MOBILE
            #pragma shader_feature_local FORCE_SPECCUBE_BOX_PROJECTION
        #endif

        #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
        #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
        #pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
        #pragma shader_feature_local _GEOMETRICSPECULAR_AA
        #pragma shader_feature_local _LIGHTMAPPED_SPECULAR
        #pragma shader_feature_local _EMISSION
        //#pragma shader_feature_local _REFRACTION
        #pragma shader_feature_local _MASKMAP
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        #pragma shader_feature_local _ _DETAILBLEND_SCREEN _DETAILBLEND_MULX2 _DETAILBLEND_LERP
        #pragma shader_feature_local _DETAIL_BLENDMASK
        #pragma shader_feature_local _DETAIL_ALBEDOMAP
        #pragma shader_feature_local _DETAIL_NORMALMAP
        #pragma shader_feature_local _DETAIL_HEIGHTBLEND
        #pragma shader_feature_local _DECAL
        #pragma shader_feature_local _AUDIOLINK_EMISSION
        #pragma shader_feature_local _ANISOTROPY

        #pragma shader_feature_local _WIND
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


        #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
        #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
        #pragma shader_feature_local _GEOMETRICSPECULAR_AA

        #ifndef SHADER_API_MOBILE
            #pragma shader_feature_local _PARALLAXMAP
        #endif
        #pragma shader_feature_local _MASKMAP
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        #pragma shader_feature_local _ _DETAILBLEND_SCREEN _DETAILBLEND_MULX2 _DETAILBLEND_LERP
        #pragma shader_feature_local _DETAIL_BLENDMASK
        #pragma shader_feature_local _DETAIL_ALBEDOMAP
        #pragma shader_feature_local _DETAIL_NORMALMAP
        #pragma shader_feature_local _DETAIL_HEIGHTBLEND
        #pragma shader_feature_local _DECAL
        #pragma shader_feature_local _ANISOTROPY

        #pragma shader_feature_local _WIND
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


        #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAFADE_ON

        #pragma shader_feature_local _WIND
        ENDCG
    }

    Pass
    {
        Name "META"
        Tags { "LightMode"="Meta" }
        Cull Off

        CGPROGRAM
        #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
        #pragma shader_feature_local _EMISSION
        #pragma shader_feature_local _ _DETAILBLEND_SCREEN _DETAILBLEND_MULX2 _DETAILBLEND_LERP
        #pragma shader_feature_local _DETAIL_BLENDMASK
        #pragma shader_feature_local _DETAIL_ALBEDOMAP
        #pragma shader_feature_local _DETAIL_HEIGHTBLEND
        #pragma shader_feature_local _DECAL
        ENDCG
    }
}
CustomEditor "z3y.Shaders.LitGUI"
Fallback "Mobile/Quest Lite"
}
