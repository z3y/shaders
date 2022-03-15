Texture2D _MainTex; SamplerState sampler_MainTex;
Texture2D _MetallicGlossMap; SamplerState sampler_MetallicGlossMap;
Texture2D _BumpMap; SamplerState sampler_BumpMap;

Texture2D _EmissionMap; SamplerState sampler_EmissionMap;

half _Glossiness;
half _GlossinessMin;
half _Metallic;
half _MetallicMin;
half _Occlusion;

half _BumpScale;

half4 _EmissionMap_TexelSize;
half _EmissionMultBase;
half _EmissionGIMultiplier;


float4 _MainTex_ST;
half4 _Color;
half3 _EmissionColor;