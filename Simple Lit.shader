Shader "Simple Lit"
{
    Properties
    {

        [KeywordEnum(Opaque, Cutout, Fade, Transparent)] _Mode ("Rendering Mode", Int) = 0

        _Cutoff ("Alpha Cuttoff", Range(0, 1)) = 0.5

        _MainTex ("Base Map", 2D) = "white" {}
            _Color ("Base Color", Color) = (1,1,1,1)

        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _GlossinessMin ("Smoothness Min", Range(0,1)) = 0
        [Gamma] _Metallic ("Metallic", Range(0,1)) = 0
        _MetallicMin ("Metallic Min", Range(0,1)) = 0
        _Occlusion ("Occlusion", Range(0,1)) = 0

        _MetallicGlossMap ("Packed Mask", 2D) = "white" {}

        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
            _BumpScale ("Bump Scale", Range(0,8)) = 1

        [Toggle(SPECULAR_HIGHLIGHTS)] _SpecularHighlights("Specular Highlights", Float) = 1
        [Toggle(REFLECTIONS)] _GlossyReflections("Reflections", Float) = 1
            _SpecularOcclusion ("Specular Occlusion", Range(0,1)) = 0

        [ToggleUI] _GSAA ("Geometric Specular AA", Int) = 0
            [PowerSlider(2)] _specularAntiAliasingVariance ("Variance", Range(0.0, 1.0)) = 0.15
            [PowerSlider(2)] _specularAntiAliasingThreshold ("Threshold", Range(0.0, 1.0)) = 0.1

        [Toggle(EMISSION)] _EnableEmission ("Emission", Int) = 0
            _EmissionMap ("Emission Map", 2D) = "white" {}
            [ToggleUI] _EmissionMultBase ("Multiply Base", Int) = 0
            [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)


        _DetailAlbedoMap ("Albedo Map", 2D) = "linearGrey" {}
            [Normal] _DetailNormalMap ("Normal Map", 2D) = "bump" {}
            [Enum(UV0, 0, UV1, 1)]  _DetailMap_UV ("Detail UV", Int) = 0
            _DetailAlbedoScale ("Albedo Scale", Range(0.0, 2.0)) = 0
            _DetailNormalScale ("Normal Scale", Range(0.0, 2.0)) = 0
            _DetailSmoothnessScale ("Smoothness Scale", Range(0.0, 2.0)) = 0

        [Toggle(PARALLAX)] _EnableParallax ("Parallax", Int) = 0
            _Parallax ("Height Scale", Range (0, 0.2)) = 0.02
            _ParallaxMap ("Height Map", 2D) = "white" {}
            [IntRange] _ParallaxSteps ("Parallax Steps", Range(1,50)) = 25
            _ParallaxOffset ("Parallax Offset", Range(-1, 1)) = 0


        [Toggle(NONLINEAR_LIGHTPROBESH)] _NonLinearLightProbeSH ("Non-linear Light Probe SH", Int) = 0
        [Toggle(BAKEDSPECULAR)] _BakedSpecular ("Lightmapped Specular ", Int) = 1

        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Op", Int) = 0
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOpAlpha ("Blend Op Alpha", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Int) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Int) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2
        [Enum(Off, 0, On, 1)] _AlphaToMask ("Alpha To Coverage", Int) = 0


        [KeywordEnum(None, SH, RNM)] Bakery ("Bakery Mode", Int) = 0
            _RNM0("RNM0", 2D) = "black" {}
            _RNM1("RNM1", 2D) = "black" {}
            _RNM2("RNM2", 2D) = "black" {}


        [Toggle(_MASK_MAP)] _MASK_MAPtoggle ("_MASK_MAP", Int) = 0
        [Toggle(_NORMAL_MAP)] _NORMAL_MAPtoggle ("_NORMAL_MAP", Int) = 0
        [Toggle(_DETAILALBEDO_MAP)] _DETAILALBEDO_MAPtoggle ("_DETAILALBEDO_MAP", Int) = 0
        [Toggle(_DETAILNORMAL_MAP)] _DETAILNORMAL_MAPtoggle ("_DETAILNORMAL_MAP", Int) = 0

        

    }
    SubShader
    {
        CGINCLUDE
        #pragma target 4.5
        #pragma vertex vert
        #pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest
        ENDCG

        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "FORWARDBASE"
            Tags { "LightMode"="ForwardBase" }
            ZWrite [_ZWrite]
            Cull [_Cull]
            ZTest [_ZTest]
            AlphaToMask [_AlphaToMask]
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // #pragma multi_compile _ LOD_FADE_CROSSFADE
            // #pragma multi_compile _ VERTEXLIGHT_ON


            #pragma shader_feature_local _ _MODE_CUTOUT _MODE_FADE _MODE_TRANSPARENT
            #pragma shader_feature_local _ BAKERY_SH BAKERY_RNM BAKERY_VOLUME
            #define BICUBIC_LIGHTMAP
            #pragma shader_feature_local SPECULAR_HIGHLIGHTS
            #pragma shader_feature_local REFLECTIONS
            #pragma shader_feature_local EMISSION
            #pragma shader_feature_local PARALLAX
            #pragma shader_feature_local NONLINEAR_LIGHTPROBESH
            #pragma shader_feature_local BAKEDSPECULAR

            #pragma shader_feature_local _MASK_MAP
            #pragma shader_feature_local _NORMAL_MAP
            #pragma shader_feature_local _DETAILALBEDO_MAP
            #pragma shader_feature_local _DETAILNORMAL_MAP

            #include "PassCGI.cginc"
            ENDCG
        }

        Pass
        {
            Name "FORWARDADD"
            Tags { "LightMode"="ForwardAdd" }
            Fog { Color (0,0,0,0) }
            ZWrite Off
            BlendOp [_BlendOp], [_BlendOpAlpha]
            Blend One One
            Cull [_Cull]
            ZTest [_ZTest]
            AlphaToMask [_AlphaToMask]

            CGPROGRAM
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma shader_feature_local _ _MODE_CUTOUT _MODE_FADE _MODE_TRANSPARENT
            #pragma shader_feature_local SPECULAR_HIGHLIGHTS
            #pragma shader_feature_local PARALLAX
            #pragma shader_feature_local NONLINEAR_LIGHTPROBESH

            #pragma shader_feature_local _MASK_MAP
            #pragma shader_feature_local _NORMAL_MAP
            #pragma shader_feature_local _DETAILALBEDO_MAP
            #pragma shader_feature_local _DETAILNORMAL_MAP
            
            #include "PassCGI.cginc"

            ENDCG
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            AlphaToMask Off
            ZWrite On
            Cull [_Cull]
            ZTest LEqual

            CGPROGRAM
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            // #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2

            #pragma shader_feature_local _ _MODE_CUTOUT _MODE_FADE _MODE_TRANSPARENT

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

            #pragma shader_feature_local _ _MODE_CUTOUT _MODE_FADE _MODE_TRANSPARENT
            #pragma shader_feature_local EMISSION            

            #include "PassCGI.cginc"
            ENDCG
        }
    }
    // CustomEditor "z3y.ShaderEditor.LitUI"
}
