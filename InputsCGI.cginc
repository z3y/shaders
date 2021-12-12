#define DECLARE_TEX2D_CUSTOM_SAMPLER(tex) SamplerState sampler##tex; Texture2D tex
#define DECLARE_TEX2D_CUSTOM(tex)                                    Texture2D tex

static float2 parallaxOffset;

DECLARE_TEX2D_CUSTOM_SAMPLER(_MainTex);
half4 _MainTex_ST;
half4 _Color;

DECLARE_TEX2D_CUSTOM_SAMPLER(_BumpMap);
half _BumpScale;

DECLARE_TEX2D_CUSTOM(_MetallicGlossMap);
half _Glossiness;
half _GlossinessMin;
half _Metallic;
half _MetallicMin;
half _Occlusion;

DECLARE_TEX2D_CUSTOM_SAMPLER(_DetailAlbedoMap);
DECLARE_TEX2D_CUSTOM_SAMPLER(_DetailNormalMap);
half4 _DetailAlbedoMap_ST;
half _DetailMap_UV;
half _DetailAlbedoScale;
half _DetailNormalScale;
half _DetailSmoothnessScale;

half _Cutoff;
half _GSAA;
half _specularAntiAliasingVariance;
half _specularAntiAliasingThreshold;
half _SpecularOcclusion;

DECLARE_TEX2D_CUSTOM(_EmissionMap);
half _EmissionMultBase;
half3 _EmissionColor;


DECLARE_TEX2D_CUSTOM(_ParallaxMap);
half _ParallaxSteps;
half _ParallaxOffset;
half _Parallax;

#include "SurfaceData.cginc"
#include "Defines.cginc"
