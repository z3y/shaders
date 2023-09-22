
namespace z3y
{
    public static class LitImporterConstants
    {
        public const string DefaultPropertiesInclude = @"
[Enum(Opaque, 0, Cutout, 1, Fade, 2, Transparent, 3, Additive, 4, Multiply, 5)]_Mode(""Rendering Mode"", Float) = 0
[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend(""Source Blend"", Float) = 1
[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend(""Destination Blend"", Float) = 0
[Enum(Off, 0, On, 1)] _ZWrite(""ZWrite"", Float) = 1
[Enum(Off, 0, On, 1)] _AlphaToMask(""AlphaToMask"", Float) = 0
[Enum(UnityEngine.Rendering.CullMode)] _Cull(""Cull"", Float) = 2
";

        public const string DefaultPropertiesIncludeAfter = @"
[ToggleUI] _BakeryAlphaDither(""Bakery Alpha Dither"", Float) = 0
[ToggleOff(_GLOSSYREFLECTIONS_OFF)] _GlossyReflections(""Reflections"", Float) = 1
[ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularHighlights(""Specular Highlights"", Float) = 1
[HideInInspector] [NonModifiableTextureData] [NoScaleOffset] _DFG(""DFG"", 2D) = ""white"" {}
[HideInInspector] [NonModifiableTextureData] [NoScaleOffset] BlueNoise(""BlueNoise"", 2D) = ""white"" {}
";

        public const string AreaLitProperties = @"
[ToggleGroupStart] [Toggle(_AREALIT)] _AreaLitToggle (""Area Lit"", Float) = 0
[Indent] [NoScaleOffset] _LightMesh(""Light Mesh"", 2D) = ""black"" {}
[NoScaleOffset] _LightTex0(""Light Texture 0"", 2D) = ""white"" {}
[NoScaleOffset] _LightTex1(""Light Texture 1"", 2D) = ""black"" {}
[NoScaleOffset] _LightTex2(""Light Texture 2"", 2D) = ""black"" {}
[NoScaleOffset] _LightTex3(""Light Texture 3"", 2DArray) = ""black"" {}
[ToggleGroupEnd] [UnIndent] [ToggleOff] _OpaqueLights(""Opaque Lights"", Float) = 1.0
";

        public const string DefaultConfigFile = @"
DEFINES_START
    // comment out to set globally
    
    // #define BAKERY_MONOSH // enable mono
    // #define _LIGHTMAPPED_SPECULAR // enable lightmapped specular
    // #undef APPROXIMATE_AREALIGHT_SPECULAR // lower the smoothness in areas where theres less directionality in directional lightmaps, defined by default
    // #define BICUBIC_LIGHTMAP // enable bicubic lightmap
    // #define BAKERY_SHNONLINEAR_OFF // disable non linear lightmap sh, enabled by default
    // #define NONLINEAR_LIGHTPROBESH // enable non linear lightprobe sh
    // #define DISABLE_LIGHT_PROBE_PROXY_VOLUME // disable LPPV
    // #undef UNITY_SPECCUBE_BLENDING // disable blending of 2 reflection probes
DEFINES_END

";
    }

}