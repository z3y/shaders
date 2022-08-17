Shader "Lit"
{

Properties
{
    ResetFix ("", Float) = 0
    [NonModifiableTextureData] _DFG ("DFG Lut", 2D) = "black" {}
    Foldout_RenderingOptions ("Rendering Options", Float) = 0
        
    [Enum(Opaque, 0, Cutout, 1, Fade, 2, Transparent, 3, Additive, 4, Multiply, 5)] _Mode ("Rendering Mode", Int) = 0


    _Cutoff ("Alpha Cuttoff", Range(0.001, 1)) = 0.5
    [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Int) = 1
    [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Int) = 0
    [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Int) = 1
    [HideInInspector] [Enum(Off, 0, On, 1)] _AlphaToMask ("Alpha To Coverage", Int) = 0
    [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2
        

    Foldout_SurfaceInputs ("Surface Inputs", Float) = 1
    _MainTex ("Albedo", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1,1)
    _AlbedoSaturation ("Saturation", Float) = 1



    _MetallicGlossMap("Mask Map", 2D) = "white" {}
    [HideIf(_MetallicGlossMap)] [Indent(2)] _Metallic ("Metallic", Range(0,1)) = 0
    [HideIf(_MetallicGlossMap)] [HideIf(_SmoothnessAlbedoAlpha is 1)] [Indent(2)] _Glossiness ("Smoothness", Range(0,1)) = 0.5
    [ShowIf(_MetallicGlossMap)] [Indent(2)] [MinMax] _MetallicRemapping ("Metallic Remap", Vector) = (0,1,0,1)
    [ShowIf(_MetallicGlossMap)] [HideIf(_SmoothnessAlbedoAlpha is 1)] [Indent(2)] [MinMax] _GlossinessRange ("Smoothness Range", Vector) = (0,1,0,1)
    [ShowIf(_MetallicGlossMap)] [HideIf(_SmoothnessAlbedoAlpha is 1)] [Indent(2)] [MinMax] _GlossinessRemapping ("Smoothness Remap", Vector) = (0,1,0,1)
    [ShowIf(_SmoothnessAlbedoAlpha is 1)] [Indent(2)] [MinMax] _GlossinessRange ("Smoothness Range", Vector) = (0,1,0,1)
    [ShowIf(_SmoothnessAlbedoAlpha is 1)] [Indent(2)] [MinMax] _GlossinessRemapping ("Smoothness Remap", Vector) = (0,1,0,1)
    [ShowIf(_MetallicGlossMap)] [Indent(2)] _OcclusionStrength ("Occlusion", Range(0,1)) = 1
    [Indent(2)] _Reflectance ("Reflectance", Range(0.0, 1.0)) = 0.5

    [ExtraProperty(_BumpScale)]
    [Toggle(_NORMALMAP)] [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
    _BumpScale ("Scale", Float) = 1
    
	[Toggle(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)] _SmoothnessAlbedoAlpha ("Smoothness Albedo Alpha", Int) = 0
	[Toggle(_PARALLAXMAP)] [Linear] [ExtraProperty(_Parallax)] _ParallaxMap ("Height Map", 2D) = "white" {}
    [PowerSlider(2)] _Parallax ("Scale", Range (0, 0.2)) = 0.02
    [Indent(2)] [ShowIf(_ParallaxMap)] _ParallaxOffset ("Parallax Offset", Range(-1, 1)) = 0
    [Indent(2)] [ShowIf(_ParallaxMap)] [IntRange] _ParallaxSteps ("Steps", Range(1, 32)) = 16


    [TilingOffset] _MainTexTileOffset ("_MainTex", Float) = 0
    [Tooltip(Removes reflections based on lightmap or lightprobe intensity)] _SpecularOcclusion ("Specular Occlusion", Range(0,1)) = 0


    Foldout_Emission ("Emission", Float) = 0

    //[Toggle(_EMISSION)] _EmissionToggle ("Enable Emission", Int) = 0

    [ExtraProperty(_EmissionColor)] _EmissionMap ("Emission Map", 2D) = "white" {}
    [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)
    [TilingOffset] _EmissionMapTilingOffset ("_EmissionMap", Float) = 0
    [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _EmissionMap_UV ("UV", Int) = 0
    _EmissionMultBase ("Multiply Base", Range(0,1)) = 0
    _EmissionGIMultiplier ("GI Multiplier", Float) = 1

    [MarkupSpace(10)]
    //[Toggle(_AUDIOLINK_EMISSION)] _AudioLinkEmissionToggle ("Audio Link", Float) = 0
    [ShowIf(_AudioLinkEmissionToggle is 1)] [Enum(Bass, 0, Low Mids, 1, High Mids, 2, Treble, 3)] _AudioLinkEmissionBand ("Band", Int) = 0
    [HideInInspector] _AudioTexture ("Audio Link Render Texture", 2D) = "_AudioTexture" {}



    Foldout_DetailFoldout ("Detail Inputs", Float) = 0
    [AdvancedKeywordEnum(_, 0, _DETAILBLEND_SCREEN, 1, _DETAILBLEND_MULX2, 2, _DETAILBLEND_LERP, 3)]
    [Enum(Overlay, 0, Screen, 1, Multiply X2, 2, Replace, 3)] _DetailBlendMode ("Blend Mode", Int) = 0

    [Toggle(_DETAIL_BLENDMASK)] _DetailMask ("Blend Mask", 2D) = "white" {}
    //[TilingOffset] _DetailMaskTileOffset ("_DetailMask", Float) = 0
    [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _DetailMask_UV ("UV", Int) = 0


    [MarkupSpace(10)]
    [Toggle(_DETAIL_ALBEDOMAP)] [ExtraProperty(_DetailColor)] _DetailAlbedoMap ("Albedo", 2D) = "white" {}
    _DetailColor ("Color", Color) = (1,1,1,1)
    [Toggle(_DETAIL_NORMALMAP)] [Normal] [ExtraProperty(_DetailNormalScale)] _DetailNormalMap ("Normal Map", 2D) = "bump" {}
    _DetailNormalScale ("Scale", Float) = 1

    [ShowIf(_DetailBlendMode is 3)] _DetailMetallic ("Metallic", Range(0,1)) = 0
    [ShowIf(_DetailBlendMode is 3)] _DetailGlossiness ("Smoothness", Range(0,1)) = 0.5

    [TilingOffset] _DetailMapTileOffset ("_DetailAlbedoMap", Float) = 0
    [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _DetailMap_UV ("UV", Int) = 0
    //[Tooltip(Use the Detail textures as Decal, only sampling within the UV range)]
    //[Toggle(_DECAL)] _IsDecal ("Use as Decal", Float) = 0


    [MarkupSpace(10)]
    [Tooltip(Height Map for the Main Texture)]
    [ExtraProperty(_HeightBlend)] [Toggle(_DETAIL_HEIGHTBLEND)] _DetailHeightBlend ("Height Blend", 2D) = "white" {}
    _HeightBlend ("Blend", Float) = 5
    [Toggle] _HeightBlendInvert ("Blend Invert", Float) = 0

    // [Foldout] _DecalFoldout ("Decals", Float) = 0

    // [VerticalScopeBegin(box)]
    // [Toggle(_DECAL0)] _Decal0 ("Decal 0", Int) = 0
    // [AdvancedKeywordEnum(_, 0, _DECAL0BLEND_SCREEN, 1, _DECAL0BLEND_MULX2, 2, _DECAL0BLEND_LERP, 3)]
    // [Enum(Overlay, 0, Screen, 1, Multiply X2, 2, Replace, 3)] _DECAL0BlendMode ("Blend Mode", Int) = 0
    // [ExtraProperty(_Decal0AlbedoColor)] _Decal0Albedo ("Albedo (RGB) Mask (A)", 2D) = "white" {}
    // _Decal0AlbedoColor ("Color", Color) = (1,1,1,1)
    // [ExtraProperty(_Decal0NormalScale)] _Decal0NormalMap ("Normal Map", 2D) = "bump" {}
    // _Decal0NormalScale ("Scale", Float) = 1
    // [TilingOffset] _Decal0AlbedoTileOffset ("_Decal0Albedo", Float) = 0
    // [VerticalScopeEnd]
    // [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _Decal0_UV ("UV", Int) = 0




    [Foldout] _WindFoldout ("Wind", Float) = 0
    [Toggle(_WIND)] _WindToggle ("Enable Wind", Float) = 0
    _WindNoise ("Noise RGB", 2D) = "black" {}
    _WindScale ("Noise Scale", Float) = 0.02
    [PowerSlider(2)] _WindSpeed ("Speed", Range(0,5)) = 0.05
    _WindIntensity ("Intensity XYZ", Vector) = (0.1,0.1,0.1,0)
    [Label(helpBox)]_WindInfo ("Vertex Colors RGB for XYZ Intensity Mask", Float) = 0


    // [Foldout] _SSSFoldout ("Subsurface Scattering", Float) = 0

    // [Toggle(_SSS)] _SSSToggle ("Enable Subsurface Scattering", Int) = 0


    [Foldout] _AvancedSettings ("Additional Settings", Float) = 1

    [ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1
    [ToggleOff(_GLOSSYREFLECTIONS_OFF)] _GlossyReflections ("Reflections", Float) = 1
    [Toggle(FORCE_SPECCUBE_BOX_PROJECTION)] [Tooltip(Force Box Projection on Quest)] _ForceBoxProjection ("Force Box Projection", Float) = 0
    [ToggleOff(_BLENDREFLECTIONPROBES_OFF)] _BlendReflectionProbes ("Blend Reflection Probes", Float) = 1
    [Tooltip(Allow Lightprobe Proxy Volumes)] [Toggle(_ALLOW_LPPV)] _Allow_LPPV_Toggle ("Allow LPPV", Float) = 0

    [Toggle(_GEOMETRICSPECULAR_AA)] [Tooltip(Reduce specular shimmering)] _GSAA ("Geometric Specular AA", Int) = 0
    [ShowIf(_GSAA is 1)] [PowerSlider(2)] _specularAntiAliasingVariance ("Variance", Range(0.0, 1.0)) = 0.15
    [ShowIf(_GSAA is 1)] [PowerSlider(2)] _specularAntiAliasingThreshold ("Threshold", Range(0.0, 1.0)) = 0.1


    [MarkupSpace(10)]
    [Toggle(LTCGI)] _LTCGI("LTCGI", Int) = 0
    [Toggle(LTCGI_DIFFUSE_OFF)] _LTCGI_DIFFUSE_OFF("LTCGI Disable Diffuse", Int) = 0

    [Space(10)]

    

    [Toggle(_LIGHTMAPPED_SPECULAR)] [Tooltip(Specular Highlights from Directional Lightmaps or dominant light probe direction)] _LightmappedSpecular ("Lightmapped Specular ", Int) = 0
    [Toggle(_BICUBICLIGHTMAP)] [Tooltip(Smoother lightmap)] _BicubicLightmap ("Bicubic Lightmap", Float) = 0

    [Tooltip(Removes banding. Not needed if Post Processing is used)] [Toggle(DITHERING)] _Dithering ("Dithering", Float) = 0
    [Toggle(ACES_TONEMAPPING)] _ACES ("ACES", Float) = 0

    [Space(10)]

    [AdvancedKeywordEnum(_, 0, BAKERY_SH, 1, BAKERY_RNM, 2, BAKERY_MONOSH, 3)] [Enum(None, 0, SH, 1, RNM, 2, MONOSH, 3)] Bakery ("Bakery Mode", Int) = 0
    [ToggleOff(BAKERY_SHNONLINEAR_OFF)] _BAKERY_SHNONLINEAR ("Non-linear Lightmap SH", Float) = 1
    [Toggle(NONLINEAR_LIGHTPROBESH)] _NonLinearLightProbeSH ("Non-linear Light Probe SH", Int) = 0


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
        
        #pragma multi_compile_fragment _ VERTEXLIGHT_ON

        #pragma shader_feature_local _ BAKERY_SH BAKERY_RNM BAKERY_MONOSH
        #pragma shader_feature_local_fragment NONLINEAR_LIGHTPROBESH
        #pragma shader_feature_local_fragment BAKERY_SHNONLINEAR_OFF

        #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
        #pragma shader_feature_local FORCE_SPECCUBE_BOX_PROJECTION
        #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
        #pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
        #pragma shader_feature_local_fragment _BLENDREFLECTIONPROBES_OFF
        #pragma shader_feature_local_fragment _BICUBICLIGHTMAP
        #pragma shader_feature_local _GEOMETRICSPECULAR_AA
        #pragma shader_feature_local _LIGHTMAPPED_SPECULAR
        #pragma shader_feature_local _EMISSION
        #pragma shader_feature_local _ALLOW_LPPV

        #pragma shader_feature_local_fragment LTCGI
        #pragma shader_feature_local_fragment LTCGI_DIFFUSE_OFF

        #pragma shader_feature_local _PARALLAXMAP
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

        #pragma shader_feature_local _WIND
        #pragma shader_feature_local _SSS

        // "Global Keywords"
        #pragma shader_feature_local_fragment DITHERING
        #pragma shader_feature_local ACES_TONEMAPPING

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

        #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
        #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
        #pragma shader_feature_local _GEOMETRICSPECULAR_AA

        #pragma shader_feature_local _PARALLAXMAP
        #pragma shader_feature_local _MASKMAP
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        #pragma shader_feature_local _ _DETAILBLEND_SCREEN _DETAILBLEND_MULX2 _DETAILBLEND_LERP
        #pragma shader_feature_local _DETAIL_BLENDMASK
        #pragma shader_feature_local _DETAIL_ALBEDOMAP
        #pragma shader_feature_local _DETAIL_NORMALMAP
        #pragma shader_feature_local _DETAIL_HEIGHTBLEND
        #pragma shader_feature_local _DECAL

        #pragma shader_feature_local _WIND
        #pragma shader_feature_local _SSS

        // "Global Keywords"
        #pragma shader_feature_local ACES_TONEMAPPING

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
