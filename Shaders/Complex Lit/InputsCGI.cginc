Texture2D _MainTex; SamplerState sampler_MainTex;
Texture2D _MetallicGlossMap; SamplerState sampler_MetallicGlossMap;
Texture2D _BumpMap; SamplerState sampler_BumpMap;

Texture2D _DetailMask; SamplerState sampler_DetailMask;

Texture2D _DetailNormalMap; SamplerState sampler_DetailNormalMap;
Texture2D _DetailAlbedoMap; SamplerState sampler_DetailAlbedoMap;

Texture2D _DetailNormalMap2; SamplerState sampler_DetailNormalMap2;
Texture2D _DetailAlbedoMap2; SamplerState sampler_DetailAlbedoMap2;

Texture2D _DetailNormalMap3; SamplerState sampler_DetailNormalMap3;
Texture2D _DetailAlbedoMap3; SamplerState sampler_DetailAlbedoMap3;

Texture2D _EmissionMap; SamplerState sampler_EmissionMap;

Texture2DArray _MainTexArray; SamplerState sampler_MainTexArray;
Texture2DArray _BumpMapArray; SamplerState sampler_BumpMapArray;
Texture2DArray _MetallicGlossMapArray; SamplerState sampler_MetallicGlossMapArray;

half _Glossiness;
half _GlossinessMin;
half _Metallic;
half _MetallicMin;
half _Occlusion;

half _BumpScale;
half _Reflectance;

uint _DetailMaskUV;
half4 _DetailMask_ST;
half4 _DetailMask_TexelSize;

half4 _DetailAlbedoMap_ST;
uint _DetailMapUV;
half _DetailDepth;
half _DetailAlbedoScale;
half _DetailNormalScale;
half _DetailSmoothnessScale;

half4 _DetailAlbedoMap2_ST;
uint _DetailMapUV2;
half _DetailDepth2;
half _DetailAlbedoScale2;
half _DetailNormalScale2;
half _DetailSmoothnessScale2;

half4 _DetailAlbedoMap3_ST;
uint _DetailMapUV3;
half _DetailDepth3;
half _DetailAlbedoScale3;
half _DetailNormalScale3;
half _DetailSmoothnessScale3;

half _AlbedoSaturation;
half _Cutoff;

half _Texture;
half _AudioLinkEmission;
half _DetailAlbedoAlpha;

half _EmissionMultBase;
half _EmissionDepth;
uint _EmissionMap_UV;

Texture2D _EmissionMap2;
uint _EmissionMap2_UV;
float4 _EmissionMap_ST;
float4 _EmissionMap2_ST;
half3 _Emission2Color;
half _EmissionDepth2;

half _EmissionGIMultiplier;


UNITY_INSTANCING_BUFFER_START(InstancedProps)
    UNITY_DEFINE_INSTANCED_PROP(float, _TextureIndex)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(half3, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(InstancedProps)