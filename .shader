Shader "Lit/Base"
{
	Properties
	{
		CustomEditor_Alpha ("", Int) = 0
		[Enum(Opaque, 0, Cutout, 1, Fade, 2, Transparent, 3, Additive, 4, Multiply, 5)] _Mode ("Rendering Mode", Int) = 0
		_Cutoff ("Alpha Cuttoff", Range(0, 1)) = 0.5
		
		CustomEditor_SurfaceData ("", Int) = 0
		Foldout_SufraceData ("Surface Data", Int) = 1
		_MainTex ("Base Map", 2D) = "white" {}
		_Color ("Base Color", Color) = (1,1,1,1)
		_AlbedoSaturation ("Saturation", Float) = 1
		
		[Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Scale", Float) = 1
		
		_MetallicGlossMap ("Packed Mask", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0
		_GlossinessMinMax ("Smoothness", Vector) = (0,1,0)
		_MetallicMinMax ("Metallic", Vector) = (0,1,0)
		_Occlusion ("Occlusion", Range(0,1)) = 0
		_Reflectance ("Reflectance", Range(0.0, 1.0)) = 0.5
		
		_ParallaxMap ("Parallax Map", 2D) = "black" {}
		[PowerSlider(2)] _Parallax ("Scale", Range(0.0, 1.0)) = 0.02
		_MinSteps ("Min Steps", Range(1,32)) = 5
		_MaxSteps ("Max Steps", Range(1,32)) = 15
		_ParallaxFadingMip("Fading Mip", Range(0,16)) = 5
		
		CustomEditor_LitRenderingOptions("", Int) = 0
		
		Foldout_RenderingOptions("Rendering Options", Int) = 0
		[Enum(None, 0, SH, 1, RNM, 2)] Bakery ("Bakery Mode", Int) = 0
		[Toggle(_BAKED_SPECULAR)] _BakedSpecular ("Baked Specular ", Int) = 0
		[Toggle(_NONLINEAR_LIGHTPROBESH)] _NonLinearLightProbeSH("Non-linear Light Probe SH", Int) = 0
		
		[ToggleOff(_SPECULAR_HIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1
		[ToggleOff(_REFLECTIONS_OFF)] _GlossyReflections ("Reflection Probes", Float) = 1
		
		[Toggle(_GEOMETRICSPECULAR_AA)] _GSAA ("Geometric Specular AA", Int) = 0
		[PowerSlider(2)] _specularAntiAliasingVariance ("Variance", Range(0.0, 1.0)) = 0.15
		[PowerSlider(2)] _specularAntiAliasingThreshold ("Threshold", Range(0.0, 1.0)) = 0.1
		
		[HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Int) = 1
		[HideInInspector] [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Int) = 0
		[HideInInspector] [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Int) = 1
		[HideInInspector] [Enum(Off, 0, On, 1)] _AlphaToMask ("Alpha To Coverage", Int) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2
		
		[NonModifiableTextureData] _DFG ("DFG Lut", 2D) = "black" {}
		
	}
	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry"
			
		}
		
		Pass
		{
			Name "ForwardBase"
			Tags
			{
				"LightMode"="ForwardBase"
			}
			ZWrite [_ZWrite]
			Cull [_Cull]
			AlphaToMask [_AlphaToMask]
			Blend [_SrcBlend] [_DstBlend]
			
			CGPROGRAM
			#pragma target 4.5
			#pragma vertex Vertex
			#pragma fragment Fragment
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#pragma multi_compile_fwdbase
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma multi_compile_fragment _ VERTEXLIGHT_ON // already defined in vertex by multi_compile_fwdbase
			#pragma shader_feature_local _BAKED_SPECULAR
			#pragma shader_feature_local _NONLINEAR_LIGHTPROBESH
			#pragma shader_feature_local _SPECULAR_HIGHLIGHTS_OFF
			#pragma shader_feature_local _REFLECTIONS_OFF
			#pragma shader_feature_local _GEOMETRICSPECULAR_AA
			#pragma shader_feature_local _ BAKERY_SH BAKERY_RNM
			
			#pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
			
			#pragma shader_feature_local _NORMAL_MAP
			#pragma shader_feature_local _MASK_MAP
			#pragma shader_feature_local _PARALLAX_MAP
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				float4 tangent : TANGENT;
				float4 color : COLOR;
				uint vertexId : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
			#define REQUIRE_SCREENPOS
			#endif
			
			#if defined(BAKERY_RNM) && defined(_BAKED_SPECULAR)
			#define REQUIRE_VIEWDIRTS
			#endif
			
			#ifdef _PARALLAX_MAP
			#define REQUIRE_VIEWDIRTS
			#endif
			
			struct VertexData
			{
				float4 pos : SV_POSITION;
				float2 uv[4] : TEXCOORD0;
				
				float3 tangent : TEXCOORD4;
				float3 bitangent : TEXCOORD5;
				float3 worldNormal : TEXCOORD6;
				float4 worldPos : TEXCOORD7;
				
				#ifdef REQUIRE_VIEWDIRTS
				float3 viewDirTS : TEXCOORD8;
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				float4 screenPos : TEXCOORD9;
				#endif
				
				#if !defined(UNITY_PASS_SHADOWCASTER)
				UNITY_FOG_COORDS(10)
				UNITY_SHADOW_COORDS(11)
				#endif
				
				#if defined(VERTEXLIGHT_ON)
				half3 vertexLight : TEXCOORD12;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			static VertexData vertexData;
			
			half _Cutoff;
			
			Texture2D _MainTex;
			SamplerState sampler_MainTex;
			float4 _MainTex_ST;
			half4 _Color;
			
			Texture2D _MetallicGlossMap;
			SamplerState sampler_MetallicGlossMap;
			
			Texture2D _BumpMap;
			SamplerState sampler_BumpMap;
			half _BumpScale;
			
			half _Glossiness;
			half _Metallic;
			half2 _GlossinessMinMax;
			half2 _MetallicMinMax;
			half _Occlusion;
			half _Reflectance;
			half _AlbedoSaturation;
			
			Texture2D _ParallaxMap;
			half _Parallax;
			float4 _ParallaxMap_TexelSize;
			
			struct SurfaceData
			{
				half3 albedo;
				half3 tangentNormal;
				half3 emission;
				half metallic;
				half perceptualRoughness;
				half occlusion;
				half reflectance;
				half alpha;
			};
			
			static SurfaceData surf;
			
			void InitializeDefaultSurfaceData()
			{
				surf.albedo = 1.0;
				surf.tangentNormal = half3(0,0,1);
				surf.emission = 0.0;
				surf.metallic = 0.0;
				surf.perceptualRoughness = 0.0;
				surf.occlusion = 1.0;
				surf.reflectance = 0.5;
				surf.alpha = 1.0;
			}
			struct ShaderData
			{
				// probably just gonna put here everything if needed
				float3 worldNormal;
				float3 worldNormalUnmodified;
				float3 bitangent;
				float3 tangent;
				half NoV;
				float3 viewDir;
				float2 parallaxUVOffset;
				float2 DFGLut;
				half3 f0;
				half3 energyCompensation;
				uint facing;
			};
			
			static ShaderData shaderData;
			
			// Partially taken from Google Filament, Xiexe, Catlike Coding and Unity
			// https://google.github.io/filament/Filament.html
			// https://github.com/Xiexe/Unity-Lit-Shader-Templates
			// https://catlikecoding.com/
			
			#define GRAYSCALE float3(0.2125, 0.7154, 0.0721)
			#define TAU float(6.28318530718)
			#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
			
			#ifndef BICUBIC_SAMPLING_INCLUDED
			#define BICUBIC_SAMPLING_INCLUDED
			#if defined(SHADER_API_MOBILE)
			#undef BICUBIC_LIGHTMAP
			#endif
			
			// https://ndotl.wordpress.com/2018/08/29/baking-artifact-free-lightmaps
			// bicubicw0, bicubicw1, bicubicw2, and bicubicw3 are the four cubic B-spline basis functions
			float bicubicw0(float a)
			{
				//    return (1.0f/6.0f)*(-a*a*a + 3.0f*a*a - 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);   // optimized
			}
			
			float bicubicw1(float a)
			{
				//    return (1.0f/6.0f)*(3.0f*a*a*a - 6.0f*a*a + 4.0f);
				return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
			}
			
			float bicubicw2(float a)
			{
				//    return (1.0f/6.0f)*(-3.0f*a*a*a + 3.0f*a*a + 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
			}
			
			float bicubicw3(float a)
			{
				return (1.0f/6.0f)*(a*a*a);
			}
			
			// bicubicg0 and bicubicg1 are the two amplitude functions
			float bicubicg0(float a)
			{
				return bicubicw0(a) + bicubicw1(a);
			}
			
			float bicubicg1(float a)
			{
				return bicubicw2(a) + bicubicw3(a);
			}
			
			// bicubich0 and bicubich1 are the two offset functions
			float bicubich0(float a)
			{
				// note +0.5 offset to compensate for CUDA linear filtering convention
				return -1.0f + bicubicw1(a) / (bicubicw0(a) + bicubicw1(a)) + 0.5f;
			}
			
			float bicubich1(float a)
			{
				return 1.0f + bicubicw3(a) / (bicubicw2(a) + bicubicw3(a)) + 0.5f;
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float2 widthHeight, float2 texelSize)
			{
				#if defined(SHADER_API_MOBILE) || !defined(BICUBIC_LIGHTMAP)
				return t.Sample(s, uv);
				#else
				
				float2 xy = uv * widthHeight - 0.5;
				float2 pxy = floor(xy);
				float2 fxy = xy - pxy;
				
				// note: we could store these functions in a lookup table texture, but maths is cheap
				float bicubicg0x = bicubicg0(fxy.x);
				float bicubicg1x = bicubicg1(fxy.x);
				float bicubich0x = bicubich0(fxy.x);
				float bicubich1x = bicubich1(fxy.x);
				float bicubich0y = bicubich0(fxy.y);
				float bicubich1y = bicubich1(fxy.y);
				
				float4 t0 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich0y) * texelSize);
				float4 t1 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich0y) * texelSize);
				float4 t2 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich1y) * texelSize);
				float4 t3 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich1y) * texelSize);
				
				return bicubicg0(fxy.y) * (t0 + t1) + bicubicg1(fxy.y) * (t2 + t3);
				#endif
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float4 texelSize)
			{
				return SampleBicubic(t, s, uv, texelSize.zw, texelSize.xy);
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv)
			{
				float2 widthHeight;
				t.GetDimensions(widthHeight.x, widthHeight.y);
				float2 texelSize = 1.0 / widthHeight;
				return SampleBicubic(t, s, uv, widthHeight, texelSize);
			}
			#endif
			
			#ifndef ENVIRONMENTBRDF_INCLUDED
			#define ENVIRONMENTBRDF_INCLUDED
			Texture2D _DFG;
			SamplerState sampler_DFG;
			
			half4 SampleDFG(half NoV, half perceptualRoughness)
			{
				return _DFG.Sample(sampler_DFG, float3(NoV, perceptualRoughness, 0));
			}
			
			half3 EnvBRDF(half2 dfg, half3 f0)
			{
				return f0 * dfg.x + dfg.y;
			}
			
			half3 EnvBRDFMultiscatter(half2 dfg, half3 f0)
			{
				return lerp(dfg.xxx, dfg.yyy, f0);
			}
			
			half3 EnvBRDFEnergyCompensation(half2 dfg, half3 f0)
			{
				return 1.0 + f0 * (1.0 / dfg.y - 1.0);
			}
			
			#endif
			
			#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			#define LIGHTMAP_ANY
			#endif
			
			#ifdef LIGHTMAP_ANY
			#if defined(BAKERY_RNM) || defined(BAKERY_SH) || defined(BAKERY_VERTEXLM)
			#define BAKERYLM_ENABLED
			#undef DIRLIGHTMAP_COMBINED
			#endif
			#else
			#undef BAKERY_SH
			#undef BAKERY_RNM
			#endif
			
			#ifndef SHADER_API_MOBILE
			#define VERTEXLIGHT_PS
			#endif
			
			half RemapMinMax(half value, half remapMin, half remapMax)
			{
				return value * (remapMax - remapMin) + remapMin;
			}
			
			float pow5(float x)
			{
				float x2 = x * x;
				return x2 * x2 * x;
			}
			
			float sq(float x)
			{
				return x * x;
			}
			
			half3 F_Schlick(half u, half3 f0)
			{
				return f0 + (1.0 - f0) * pow(1.0 - u, 5.0);
			}
			
			float F_Schlick(float f0, float f90, float VoH)
			{
				return f0 + (f90 - f0) * pow5(1.0 - VoH);
			}
			
			// Input [0, 1] and output [0, PI/2]
			// 9 VALU
			float FastACosPos(float inX)
			{
				float x = abs(inX);
				float res = (0.0468878 * x + -0.203471) * x + 1.570796; // p(x)
				res *= sqrt(1.0 - x);
				
				return res;
			}
			
			float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias)
			{
				float2 ddx_ = scale * uvdx;
				float2 ddy_ = scale * uvdy;
				float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));
				
				return max(0.5 * log2(d) - bias, 0.0);
			}
			
			float ComputeTextureLOD(float2 uv)
			{
				float2 ddx_ = ddx(uv);
				float2 ddy_ = ddy(uv);
				
				return ComputeTextureLOD(ddx_, ddy_, 1.0, 0.0);
			}
			
			half Fd_Burley(half roughness, half NoV, half NoL, half LoH)
			{
				// Burley 2012, "Physically-Based Shading at Disney"
				half f90 = 0.5 + 2.0 * roughness * LoH * LoH;
				float lightScatter = F_Schlick(1.0, f90, NoL);
				float viewScatter  = F_Schlick(1.0, f90, NoV);
				return lightScatter * viewScatter;
			}
			
			float3 getBoxProjection (float3 direction, float3 position, float4 cubemapPosition, float3 boxMin, float3 boxMax)
			{
				#if defined(UNITY_SPECCUBE_BOX_PROJECTION)
				if (cubemapPosition.w > 0.0)
				{
					float3 factors = ((direction > 0.0 ? boxMax : boxMin) - position) / direction;
					float scalar = min(min(factors.x, factors.y), factors.z);
					direction = direction * scalar + (position - cubemapPosition.xyz);
				}
				#endif
				
				return direction;
			}
			
			half computeSpecularAO(half NoV, half ao, half roughness)
			{
				return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
			}
			
			half D_GGX(half NoH, half roughness)
			{
				half a = NoH * roughness;
				half k = roughness / (1.0 - NoH * NoH + a * a);
				return k * k * (1.0 / UNITY_PI);
			}
			
			float V_SmithGGXCorrelatedFast(half NoV, half NoL, half roughness) {
				half a = roughness;
				float GGXV = NoL * (NoV * (1.0 - a) + a);
				float GGXL = NoV * (NoL * (1.0 - a) + a);
				return 0.5 / (GGXV + GGXL);
			}
			
			float V_SmithGGXCorrelated(half NoV, half NoL, half roughness)
			{
				#ifdef SHADER_API_MOBILE
				return V_SmithGGXCorrelatedFast(NoV, NoL, roughness);
				#else
				half a2 = roughness * roughness;
				float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
				float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
				return 0.5 / (GGXV + GGXL);
				#endif
			}
			
			half V_Kelemen(half LoH)
			{
				// Kelemen 2001, "A Microfacet Based Coupled Specular-Matte BRDF Model with Importance Sampling"
				return saturate(0.25 / (LoH * LoH));
			}
			
			half _specularAntiAliasingVariance;
			half _specularAntiAliasingThreshold;
			float GSAA_Filament(float3 worldNormal, float perceptualRoughness)
			{
				// Kaplanyan 2016, "Stable specular highlights"
				// Tokuyoshi 2017, "Error Reduction and Simplification for Shading Anti-Aliasing"
				// Tokuyoshi and Kaplanyan 2019, "Improved Geometric Specular Antialiasing"
				
				// This implementation is meant for deferred rendering in the original paper but
				// we use it in forward rendering as well (as discussed in Tokuyoshi and Kaplanyan
				// 2019). The main reason is that the forward version requires an expensive transform
				// of the half vector by the tangent frame for every light. This is therefore an
				// approximation but it works well enough for our needs and provides an improvement
				// over our original implementation based on Vlachos 2015, "Advanced VR Rendering".
				
				float3 du = ddx(worldNormal);
				float3 dv = ddy(worldNormal);
				
				float variance = _specularAntiAliasingVariance * (dot(du, du) + dot(dv, dv));
				
				float roughness = perceptualRoughness * perceptualRoughness;
				float kernelRoughness = min(2.0 * variance, _specularAntiAliasingThreshold);
				float squareRoughness = saturate(roughness * roughness + kernelRoughness);
				
				return sqrt(sqrt(squareRoughness));
			}
			
			float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
			{
				// average energy
				float R0 = L0;
				
				// avg direction of incoming light
				float3 R1 = 0.5f * L1;
				
				// directional brightness
				float lenR1 = length(R1);
				
				// linear angle between normal and direction 0-1
				//float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
				//float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
				float q = dot(normalize(R1), n) * 0.5 + 0.5;
				q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
				
				// power for q
				// lerps from 1 (linear) to 3 (cubic) based on directionality
				float p = 1.0f + 2.0f * lenR1 / R0;
				
				// dynamic range constant
				// should vary between 4 (highly directional) and 0 (ambient)
				float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
				
				return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
			}
			
			float3 Unity_NormalReconstructZ(float2 In)
			{
				float reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
				float3 normalVector = float3(In.x, In.y, reconstructZ);
				return normalize(normalVector);
			}
			
			#ifdef DYNAMICLIGHTMAP_ON
			float3 getRealtimeLightmap(float2 uv, float3 worldNormal)
			{
				half4 bakedCol = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, uv);
				float3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);
				
				#ifdef DIRLIGHTMAP_COMBINED
				half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
				realtimeLightmap += DecodeDirectionalLightmap (realtimeLightmap, realtimeDirTex, worldNormal);
				#endif
				
				return realtimeLightmap;
			}
			#endif
			
			half3 GetSpecularHighlights(float3 worldNormal, half3 lightColor, float3 lightDirection, half3 f0, float3 viewDir, half clampedRoughness, half NoV, half3 energyCompensation)
			{
				float3 halfVector = Unity_SafeNormalize(lightDirection + viewDir);
				
				half NoH = saturate(dot(worldNormal, halfVector));
				half NoL = saturate(dot(worldNormal, lightDirection));
				half LoH = saturate(dot(lightDirection, halfVector));
				
				half3 F = F_Schlick(LoH, f0);
				half D = D_GGX(NoH, clampedRoughness);
				half V = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
				
				#ifndef SHADER_API_MOBILE
				F *= energyCompensation;
				#endif
				
				return max(0, (D * V) * F) * lightColor * NoL;
			}
			
			float Unity_Dither(float In, float2 ScreenPosition)
			{
				float2 uv = ScreenPosition * _ScreenParams.xy;
				const half4 DITHER_THRESHOLDS[4] =
				{
					half4(1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0),
					half4(13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0),
					half4(4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0),
					half4(16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0)
				};
				
				return In - DITHER_THRESHOLDS[uint(uv.x) % 4][uint(uv.y) % 4];
			}
			
			void AACutout(inout half alpha, half cutoff)
			{
				alpha = (alpha - cutoff) / max(fwidth(alpha), 0.0001) + 0.5;
			}
			
			void FlipBTN(uint facing, inout float3 worldNormal, inout float3 bitangent, inout float3 tangent)
			{
				#if !defined(LIGHTMAP_ON)
				UNITY_FLATTEN
				if (!facing)
				{
					worldNormal *= -1.0;
					bitangent *= -1.0;
					tangent *= -1.0;
				}
				#endif
			}
			
			void TangentToWorldNormal(float3 normalTS, inout float3 normalWS, inout float3 tangent, inout float3 bitangent)
			{
				normalWS = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normalWS);
				tangent = normalize(cross(normalWS, bitangent));
				bitangent = normalize(cross(normalWS, tangent));
			}
			
			half NormalDotViewDir(float3 normalWS, float3 viewDir)
			{
				return abs(dot(normalWS, viewDir)) + 1e-5f;
			}
			
			half3 GetF0(half reflectance, half metallic, half3 albedo)
			{
				return 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo * metallic;
			}
			
			struct LightData
			{
				half3 Color;
				float3 Direction;
				half NoL;
				half LoH;
				half NoH;
				float3 HalfVector;
				half3 FinalColor;
				half3 Specular;
				half Attenuation;
			};
			static LightData lightData;
			
			half3 MainLightSpecular(LightData lightData, half NoV, half clampedRoughness, half3 f0)
			{
				half3 F = F_Schlick(lightData.LoH, f0) * shaderData.energyCompensation;
				half D = D_GGX(lightData.NoH, clampedRoughness);
				half V = V_SmithGGXCorrelated(NoV, lightData.NoL, clampedRoughness);
				
				return max(0.0, (D * V) * F) * lightData.FinalColor;
			}
			
			#if defined(UNITY_PASS_FORWARDBASE) && defined(DIRECTIONAL) && !(defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING))
			#define BRANCH_DIRECTIONAL
			
			#ifdef _SPECULAR_HIGHLIGHTS_OFF
			#undef BRANCH_DIRECTIONAL
			#endif
			#endif
			
			void InitializeLightData(inout LightData lightData, float3 normalWS, float3 viewDir, half NoV, half clampedRoughness, half perceptualRoughness, half3 f0)
			{
				#ifdef USING_LIGHT_MULTI_COMPILE
				#ifdef BRANCH_DIRECTIONAL
				UNITY_BRANCH
				if (any(_WorldSpaceLightPos0.xyz))
				{
					//printf("directional branch");
					#endif
					lightData.Direction = normalize(UnityWorldSpaceLightDir(vertexData.worldPos));
					lightData.HalfVector = Unity_SafeNormalize(lightData.Direction + viewDir);
					lightData.NoL = saturate(dot(normalWS, lightData.Direction));
					lightData.LoH = saturate(dot(lightData.Direction, lightData.HalfVector));
					lightData.NoH = saturate(dot(normalWS, lightData.HalfVector));
					
					UNITY_LIGHT_ATTENUATION(lightAttenuation, vertexData, vertexData.worldPos.xyz);
					lightData.Attenuation = lightAttenuation;
					lightData.Color = lightAttenuation * _LightColor0.rgb;
					lightData.FinalColor = (lightData.NoL * lightData.Color);
					
					#ifndef SHADER_API_MOBILE
					lightData.FinalColor *= Fd_Burley(perceptualRoughness, NoV, lightData.NoL, lightData.LoH);
					#endif
					
					#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
					float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
					lightData.FinalColor *= UnityComputeForwardShadows(lightmapUV, vertexData.worldPos, vertexData.screenPos);
					#endif
					
					lightData.Specular = MainLightSpecular(lightData, NoV, clampedRoughness, f0);
					#ifdef BRANCH_DIRECTIONAL
				}
				else
				{
					lightData = (LightData)0;
				}
				#endif
				#else
				lightData = (LightData)0;
				#endif
			}
			
			#if defined(VERTEXLIGHT_ON) && defined(UNITY_PASS_FORWARDBASE)
			void NonImportantLightsPerPixel(inout half3 lightColor, inout half3 directSpecular, float3 positionWS, float3 normalWS, float3 viewDir, half NoV, half3 f0, half clampedRoughness)
			{
				float4 toLightX = unity_4LightPosX0 - positionWS.x;
				float4 toLightY = unity_4LightPosY0 - positionWS.y;
				float4 toLightZ = unity_4LightPosZ0 - positionWS.z;
				
				float4 lengthSq = 0.0;
				lengthSq += toLightX * toLightX;
				lengthSq += toLightY * toLightY;
				lengthSq += toLightZ * toLightZ;
				
				float4 attenuation = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
				float4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25.0));
				attenuation = min(attenuation, atten2 * atten2);
				
				[unroll(4)]
				for(uint i = 0; i < 4; i++)
				{
					UNITY_BRANCH
					if (attenuation[i] > 0.0)
					{
						float3 direction = normalize(float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]) - positionWS);
						half NoL = saturate(dot(normalWS, direction));
						half3 color = NoL * attenuation[i] * unity_LightColor[i];
						lightColor += color;
						
						#ifndef _SPECULAR_HIGHLIGHTS_OFF
						float3 halfVector = Unity_SafeNormalize(direction + viewDir);
						half vNoH = saturate(dot(normalWS, halfVector));
						half vLoH = saturate(dot(direction, halfVector));
						
						half3 Fv = F_Schlick(vLoH, f0);
						half Dv = D_GGX(vNoH, clampedRoughness);
						half Vv = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
						directSpecular += max(0.0, (Dv * Vv) * Fv) * color;
						#endif
					}
				}
			}
			#endif
			
			half3 GetReflections(float3 normalWS, float3 positionWS, float3 viewDir, half3 f0, half roughness, half NoV, half3 indirectDiffuse)
			{
				half3 indirectSpecular = 0;
				#if defined(UNITY_PASS_FORWARDBASE)
				
				float3 reflDir = reflect(-viewDir, normalWS);
				reflDir = lerp(reflDir, normalWS, roughness * roughness);
				
				Unity_GlossyEnvironmentData envData;
				envData.roughness = surf.perceptualRoughness;
				envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
				
				half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
				indirectSpecular = probe0;
				
				#if defined(UNITY_SPECCUBE_BLENDING) && !defined(SHADER_API_MOBILE)
				UNITY_BRANCH
				if (unity_SpecCube0_BoxMin.w < 0.99999)
				{
					envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
					float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
					indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
				}
				#endif
				
				float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
				float2 dfg = shaderData.DFGLut;
				#ifdef LIGHTMAP_ANY
				dfg.x *= lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), 1.0);
				#endif
				indirectSpecular = indirectSpecular * horizon * horizon * shaderData.energyCompensation * EnvBRDFMultiscatter(dfg, f0);
				indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);
				
				#endif
				
				return indirectSpecular;
			}
			
			half3 GetLightProbes(float3 normalWS, float3 positionWS)
			{
				half3 indirectDiffuse = 0;
				#ifndef LIGHTMAP_ANY
				#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				UNITY_BRANCH
				if (unity_ProbeVolumeParams.x == 1.0)
				{
					indirectDiffuse = SHEvalLinearL0L1_SampleProbeVolume(float4(normalWS, 1.0), positionWS);
				}
				else
				{
					#endif
					#ifdef _NONLINEAR_LIGHTPROBESH
					float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					indirectDiffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normalWS);
					indirectDiffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normalWS);
					indirectDiffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normalWS);
					#else
					indirectDiffuse = ShadeSH9(float4(normalWS, 1.0));
					#endif
					#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				}
				#endif
				#endif
				return indirectDiffuse;
			}
			
			#ifndef BAKERY_INCLUDED
			#define BAKERY_INCLUDED
			
			Texture2D _RNM0, _RNM1, _RNM2;
			SamplerState sampler_RNM0, sampler_RNM1, sampler_RNM2;
			float4 _RNM0_TexelSize;
			
			#if !defined(SHADER_API_MOBILE)
			#define BAKERY_SHNONLINEAR
			#endif
			
			void BakeryRNMLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalTS, float3 viewDirTS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_RNM
				normalTS.g *= -1;
				float3 rnm0 = DecodeLightmap(_RNM0.Sample(sampler_RNM0, lightmapUV));
				float3 rnm1 = DecodeLightmap(_RNM1.Sample(sampler_RNM1, lightmapUV));
				float3 rnm2 = DecodeLightmap(_RNM2.Sample(sampler_RNM2, lightmapUV));
				
				const float3 rnmBasis0 = float3(0.816496580927726f, 0.0f, 0.5773502691896258f);
				const float3 rnmBasis1 = float3(-0.4082482904638631f, 0.7071067811865475f, 0.5773502691896258f);
				const float3 rnmBasis2 = float3(-0.4082482904638631f, -0.7071067811865475f, 0.5773502691896258f);
				
				lightMap =    saturate(dot(rnmBasis0, normalTS)) * rnm0
				+ saturate(dot(rnmBasis1, normalTS)) * rnm1
				+ saturate(dot(rnmBasis2, normalTS)) * rnm2;
				
				#ifdef _BAKED_SPECULAR
				float3 viewDirT = -normalize(viewDirTS);
				float3 dominantDirT = rnmBasis0 * dot(rnm0, GRAYSCALE) +
				rnmBasis1 * dot(rnm1, GRAYSCALE) +
				rnmBasis2 * dot(rnm2, GRAYSCALE);
				
				float3 dominantDirTN = normalize(dominantDirT);
				half3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
				saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
				saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;
				
				half3 halfDir = Unity_SafeNormalize(dominantDirTN - viewDirT);
				half NoH = saturate(dot(normalTS, halfDir));
				half spec = D_GGX(NoH, roughness);
				directSpecular += spec * specColor;
				#endif
				
				#endif
			}
			
			void BakerySHLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalWS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_SH
				
				half3 L0 = lightMap;
				float3 nL1x = _RNM0.Sample(sampler_RNM0, lightmapUV) * 2.0 - 1.0;
				float3 nL1y = _RNM1.Sample(sampler_RNM1, lightmapUV) * 2.0 - 1.0;
				float3 nL1z = _RNM2.Sample(sampler_RNM2, lightmapUV) * 2.0 - 1.0;
				float3 L1x = nL1x * L0 * 2.0;
				float3 L1y = nL1y * L0 * 2.0;
				float3 L1z = nL1z * L0 * 2.0;
				
				#ifdef BAKERY_SHNONLINEAR
				float lumaL0 = dot(L0, float(1));
				float lumaL1x = dot(L1x, float(1));
				float lumaL1y = dot(L1y, float(1));
				float lumaL1z = dot(L1z, float(1));
				float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWS);
				
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				float regularLumaSH = dot(lightMap, 1.0);
				lightMap *= lerp(1.0, lumaSH / regularLumaSH, saturate(regularLumaSH * 16.0));
				#else
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				#endif
				
				#ifdef _BAKED_SPECULAR
				float3 dominantDir = float3(dot(nL1x, GRAYSCALE), dot(nL1y, GRAYSCALE), dot(nL1z, GRAYSCALE));
				float3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + viewDir);
				half NoH = saturate(dot(normalWS, halfDir));
				half spec = D_GGX(NoH, roughness);
				half3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
				dominantDir = normalize(dominantDir);
				
				directSpecular += max(spec * sh, 0.0);
				#endif
				
				#endif
			}
			#endif
			
			void InitializeShaderData(uint facing)
			{
				shaderData.facing = facing;
				FlipBTN(facing, vertexData.worldNormal, vertexData.bitangent, vertexData.tangent);
				
				#ifdef _GEOMETRICSPECULAR_AA
				surf.perceptualRoughness = GSAA_Filament(vertexData.worldNormal, surf.perceptualRoughness);
				#endif
				shaderData.worldNormal = vertexData.worldNormal;
				shaderData.bitangent = vertexData.bitangent;
				shaderData.tangent = vertexData.tangent;
				
				surf.tangentNormal.g *= -1;
				TangentToWorldNormal(surf.tangentNormal, shaderData.worldNormal, shaderData.tangent, shaderData.bitangent);
				
				shaderData.viewDir = normalize(UnityWorldSpaceViewDir(vertexData.worldPos));
				shaderData.NoV = NormalDotViewDir(shaderData.worldNormal, shaderData.viewDir);
				shaderData.f0 = GetF0(surf.reflectance, surf.metallic, surf.albedo.rgb);
				shaderData.DFGLut = SampleDFG(shaderData.NoV, surf.perceptualRoughness).rg;
				shaderData.energyCompensation = EnvBRDFEnergyCompensation(shaderData.DFGLut, shaderData.f0);
			}
			
			#ifndef POM_INCLUDED
			#define POM_INCLUDED
			// com.unity.render-pipelines.core copyright © 2020 Unity Technologies ApS
			// Licensed under the Unity Companion License for Unity-dependent projects--see https://unity3d.com/legal/licenses/Unity_Companion_License.
			
			// This is implementation of parallax occlusion mapping (POM)
			// This function require that the caller define a callback for the height sampling name ComputePerPixelHeightDisplacement
			// A PerPixelHeightDisplacementParam is used to provide all data necessary to calculate the heights to ComputePerPixelHeightDisplacement it doesn't need to be
			// visible by the POM algorithm.
			// This function is compatible with tiled uv.
			// it return the offset to apply to the UVSet provide in PerPixelHeightDisplacementParam
			// viewDirTS is view vector in texture space matching the UVSet
			// ref: https://www.gamedev.net/resources/_/technical/graphics-programming-and-theory/a-closer-look-at-parallax-occlusion-mapping-r3262
			
			#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
			#define FLT_MIN         1.175494351e-38 // Minimum representable positive floating-point number
			#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number
			#define INV_HALF_PI     0.636619772367
			
			float _ParallaxFadingMip;
			float _MinSteps;
			float _MaxSteps;
			
			struct PerPixelHeightDisplacementParam
			{
				float2 uv;
				SamplerState sampl;
				Texture2D height;
			};
			
			float2 GetMinUvSize(float2 baseUV, float4 texelSize)
			{
				float2 minUvSize = float2(FLT_MAX, FLT_MAX);
				
				minUvSize = min(baseUV * texelSize.zw, minUvSize);
				
				return minUvSize;
			}
			
			float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
			{
				// Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
				// Tiling is automatically handled correctly here.
				return param.height.SampleLevel(param.sampl, param.uv + texOffsetCurrent, lod).r;
			}
			
			float2 ParallaxOcclusionMapping(float lod, float lodThreshold, uint numSteps, float uvSpaceScale, float3 viewDirTS, PerPixelHeightDisplacementParam ppdParam, out float outHeight)
			{
				// Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
				float stepSize = 1.0 / (float)numSteps;
				
				// View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
				// The length of viewDirTS vector determines the furthest amount of displacement:
				// float parallaxLimit = -length(viewDirTS.xy) / viewDirTS.z;
				// float2 parallaxDir = normalize(Out.viewDirTS.xy);
				// float2 parallaxMaxOffsetTS = parallaxDir * parallaxLimit;
				// Above code simplify to
				float2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z);
				float2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;
				
				// Do a first step before the loop to init all value correctly
				float2 texOffsetCurrent = float2(0.0, 0.0);
				float prevHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				texOffsetCurrent += texOffsetPerStep;
				float currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				float rayHeight = 1.0 - stepSize; // Start at top less one sample
				
				// Linear search
				for (uint stepIndex = 0; stepIndex < numSteps; ++stepIndex)
				{
					// Have we found a height below our ray height ? then we have an intersection
					if (currHeight > rayHeight)
					break; // end the loop
					
					prevHeight = currHeight;
					rayHeight -= stepSize;
					texOffsetCurrent += texOffsetPerStep;
					
					// Sample height map which in this case is stored in the alpha channel of the normal map:
					currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				}
				
				// Found below and above points, now perform line interesection (ray) with piecewise linear heightfield approximation
				
				// Refine the search with secant method
				#define POM_SECANT_METHOD 1
				#if POM_SECANT_METHOD
				
				float pt0 = rayHeight + stepSize;
				float pt1 = rayHeight;
				float delta0 = pt0 - prevHeight;
				float delta1 = pt1 - currHeight;
				
				float delta;
				float2 offset;
				
				// Secant method to affine the search
				// Ref: Faster Relief Mapping Using the Secant Method - Eric Risser
				[unroll]
				for (uint i = 0; i < 3; ++i)
				{
					// intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
					float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
					// Retrieve offset require to find this intersectionHeight
					offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
					
					currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
					
					delta = intersectionHeight - currHeight;
					
					if (abs(delta) <= 0.01)
					break;
					
					// intersectionHeight < currHeight => new lower bounds
					if (delta < 0.0)
					{
						delta1 = delta;
						pt1 = intersectionHeight;
					}
					else
					{
						delta0 = delta;
						pt0 = intersectionHeight;
					}
				}
				
				#else // regular POM intersection
				
				//float pt0 = rayHeight + stepSize;
				//float pt1 = rayHeight;
				//float delta0 = pt0 - prevHeight;
				//float delta1 = pt1 - currHeight;
				//float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
				//float2 offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
				
				// A bit more optimize
				float delta0 = currHeight - rayHeight;
				float delta1 = (rayHeight + stepSize) - prevHeight;
				float ratio = delta0 / (delta0 + delta1);
				float2 offset = texOffsetCurrent - ratio * texOffsetPerStep;
				
				currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
				
				#endif
				
				outHeight = currHeight;
				
				// Fade the effect with lod (allow to avoid pop when switching to a discrete LOD mesh)
				offset *= (1.0 - saturate(lod - lodThreshold));
				
				return offset;
			}
			
			float2 ParallaxOcclusionMappingUVOffset(float2 uv, float scale, float3 viewDirTS, Texture2D tex, SamplerState sampl, float4 texelSize)
			{
				float3 viewDirUV = normalize(float3(viewDirTS.xy * scale, viewDirTS.z));
				
				float unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);
				uint numSteps = (uint)lerp(_MinSteps, _MaxSteps, unitAngle);
				
				float2 minUvSize = GetMinUvSize(uv, texelSize);
				float lod = ComputeTextureLOD(minUvSize);
				
				PerPixelHeightDisplacementParam ppdParam;
				
				ppdParam.uv = uv;
				ppdParam.height = tex;
				ppdParam.sampl = sampl;
				
				float height = 0;
				float2 offset = ParallaxOcclusionMapping(lod, _ParallaxFadingMip, numSteps, scale, viewDirUV, ppdParam, height);
				
				return offset;
			}
			#endif
			void InitializeSurfaceData()
			{
				float2 mainUV = TRANSFORM_TEX(vertexData.uv[0], _MainTex);
				#ifdef _PARALLAX_MAP
				shaderData.parallaxUVOffset = ParallaxOcclusionMappingUVOffset(mainUV, _Parallax, vertexData.viewDirTS, _ParallaxMap, sampler_MainTex, _ParallaxMap_TexelSize);
				#endif
				mainUV += shaderData.parallaxUVOffset;
				
				half4 mainTex = _MainTex.Sample(sampler_MainTex, mainUV);
				mainTex.rgb = lerp(dot(mainTex.rgb, GRAYSCALE), mainTex.rgb, _AlbedoSaturation);
				mainTex *= _Color;
				surf.albedo = mainTex.rgb;
				surf.alpha = mainTex.a;
				
				#ifdef _NORMAL_MAP
				half4 normalMap = _BumpMap.Sample(sampler_BumpMap, mainUV);
				surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
				#endif
				
				#ifdef _MASK_MAP
				half4 maskMap = _MetallicGlossMap.Sample(sampler_MetallicGlossMap, mainUV);
				surf.perceptualRoughness = 1.0 - (RemapMinMax(maskMap.a, _GlossinessMinMax.x, _GlossinessMinMax.y));
				surf.metallic = RemapMinMax(maskMap.r, _MetallicMinMax.x, _MetallicMinMax.y);
				surf.occlusion = lerp(1.0, maskMap.g, _Occlusion);
				#else
				surf.perceptualRoughness = 1.0 - _Glossiness;
				surf.metallic = _Metallic;
				#endif
				
				surf.reflectance = _Reflectance;
			}
			
			VertexData Vertex(VertexInput v)
			{
				VertexData o;
				UNITY_INITIALIZE_OUTPUT(VertexData, o);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				#ifdef UNITY_PASS_META
				o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
				#else
				#if !defined(UNITY_PASS_SHADOWCASTER)
				o.pos = UnityObjectToClipPos(v.vertex);
				#endif
				#endif
				
				o.uv[0].xy = v.uv0.xy;
				o.uv[1].xy = v.uv1.xy;
				o.uv[2].xy = v.uv2.xy;
				o.uv[3].xy = v.uv3.xy;
				
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
				o.bitangent = cross(o.tangent, o.worldNormal) * v.tangent.w;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				o.vertexLight = Shade4PointLights
				(
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb,
				unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, o.worldPos,  o.worldNormal
				);
				#endif
				
				#if defined(REQUIRE_VIEWDIRTS)
				TANGENT_SPACE_ROTATION;
				o.viewDirTS = mul(rotation, ObjSpaceViewDir(v.vertex));
				#endif
				
				#ifdef UNITY_PASS_SHADOWCASTER
				o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
				o.pos = UnityApplyLinearShadowBias(o.pos);
				TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
				#else
				UNITY_TRANSFER_SHADOW(o, o.uv[1].xy);
				UNITY_TRANSFER_FOG(o,o.pos);
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				o.screenPos = ComputeScreenPos(o.pos);
				#endif
				
				return o;
			}
			
			half4 Fragment(VertexData input, uint facing : SV_IsFrontFace) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(vertexData)
				vertexData = input;
				
				#if defined(LOD_FADE_CROSSFADE)
				UnityApplyDitherCrossFade(vertexData.pos);
				#endif
				
				InitializeDefaultSurfaceData();
				InitializeSurfaceData();
				
				shaderData = (ShaderData)0;
				InitializeShaderData(facing);
				
				#if defined(UNITY_PASS_SHADOWCASTER)
				#if defined(_MODE_CUTOUT)
				if (surf.alpha < _Cutoff) discard;
				#endif
				
				#ifdef _ALPHAPREMULTIPLY_ON
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAPREMULTIPLY_ON) || defined(_MODE_FADE)
				half dither = Unity_Dither(surf.alpha, input.pos.xy);
				if (dither < 0.0) discard;
				#endif
				
				SHADOW_CASTER_FRAGMENT(vertexData);
				#else
				
				half3 indirectSpecular = 0.0;
				half3 directSpecular = 0.0;
				half3 otherSpecular = 0.0;
				
				half roughness = surf.perceptualRoughness * surf.perceptualRoughness;
				half clampedRoughness = max(roughness, 0.002);
				
				InitializeLightData(lightData, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, clampedRoughness, surf.perceptualRoughness, shaderData.f0);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				lightData.FinalColor += vertexData.vertexLight;
				#endif
				
				#if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
				NonImportantLightsPerPixel(lightData.FinalColor, directSpecular, vertexData.worldPos, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, shaderData.f0, clampedRoughness);
				#endif
				
				half3 indirectDiffuse;
				#if defined(LIGHTMAP_ANY)
				
				float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
				half4 bakedColorTex = SampleBicubic(unity_Lightmap, samplerunity_Lightmap, lightmapUV);
				half3 lightMap = DecodeLightmap(bakedColorTex);
				
				#ifdef BAKERY_RNM
				BakeryRNMLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, surf.tangentNormal, vertexData.viewDirTS, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#ifdef BAKERY_SH
				BakerySHLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, shaderData.worldNormal, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#if defined(DIRLIGHTMAP_COMBINED)
				float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
				lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, shaderData.worldNormal);
				#endif
				
				#if defined(DYNAMICLIGHTMAP_ON)
				float2 realtimeLightmapUV = vertexData.uv[2] * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				float3 realtimeLightMap = getRealtimeLightmap(realtimeLightmapUV, shaderData.worldNormal);
				lightMap += realtimeLightMap;
				#endif
				
				#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
				lightData.FinalColor = 0.0;
				lightData.Specular = 0.0;
				lightMap = SubtractMainLightWithRealtimeAttenuationFromLightmap (lightMap, lightData.Attenuation, bakedColorTex, shaderData.worldNormal);
				#endif
				
				indirectDiffuse = lightMap;
				#else
				indirectDiffuse = GetLightProbes(shaderData.worldNormal, vertexData.worldPos.xyz);
				#endif
				indirectDiffuse = max(0.0, indirectDiffuse);
				
				#if !defined(_SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
				directSpecular += lightData.Specular;
				#endif
				
				#if defined(_BAKED_SPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERYLM_ENABLED)
				{
					float3 bakedDominantDirection = 1.0;
					half3 bakedSpecularColor = 0.0;
					
					#if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
					bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
					bakedSpecularColor = indirectDiffuse;
					#endif
					
					#ifndef LIGHTMAP_ANY
					bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
					#endif
					
					bakedDominantDirection = normalize(bakedDominantDirection);
					directSpecular += GetSpecularHighlights(shaderData.worldNormal, bakedSpecularColor, bakedDominantDirection, shaderData.f0, shaderData.viewDir, clampedRoughness, shaderData.NoV, shaderData.energyCompensation);
				}
				#endif
				
				#if !defined(_REFLECTIONS_OFF) && defined(UNITY_PASS_FORWARDBASE)
				indirectSpecular += GetReflections(shaderData.worldNormal, vertexData.worldPos.xyz, shaderData.viewDir, shaderData.f0, roughness, shaderData.NoV, indirectDiffuse);
				#endif
				
				otherSpecular *= EnvBRDFMultiscatter(shaderData.DFGLut, shaderData.f0) * shaderData.energyCompensation;
				
				#if defined(_ALPHAPREMULTIPLY_ON)
				surf.albedo.rgb *= surf.alpha;
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAMODULATE_ON)
				surf.albedo.rgb = lerp(1.0, surf.albedo.rgb, surf.alpha);
				#endif
				
				#if defined(_MODE_CUTOUT)
				AACutout(surf.alpha, _Cutoff);
				#endif
				
				half4 finalColor = 0;
				//final color
				finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + (lightData.FinalColor))
				+ indirectSpecular + (directSpecular * UNITY_PI) + otherSpecular + surf.emission, surf.alpha);
				
				#ifdef UNITY_PASS_META
				UnityMetaInput metaInput;
				UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
				metaInput.Emission = surf.emission;
				metaInput.Albedo = surf.albedo;
				
				return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
				#endif
				
				UNITY_APPLY_FOG(vertexData.fogCoord, finalColor);
				
				return finalColor;
				#endif
			}
			
			ENDCG
		}
		Pass
		{
			Name "ForwardAdd"
			Tags
			{
				"LightMode"="ForwardAdd"
			}
			Fog
			{
				Color (0,0,0,0)
			}
			Blend [_SrcBlend] One
			Cull [_Cull]
			ZWrite Off
			ZTest LEqual
			AlphaToMask [_AlphaToMask]
			
			CGPROGRAM
			#pragma target 4.5
			#pragma vertex Vertex
			#pragma fragment Fragment
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma shader_feature_local _SPECULAR_HIGHLIGHTS_OFF
			#pragma shader_feature_local _GEOMETRICSPECULAR_AA
			
			#pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
			
			#pragma shader_feature_local _NORMAL_MAP
			#pragma shader_feature_local _MASK_MAP
			#pragma shader_feature_local _PARALLAX_MAP
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				float4 tangent : TANGENT;
				float4 color : COLOR;
				uint vertexId : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
			#define REQUIRE_SCREENPOS
			#endif
			
			#if defined(BAKERY_RNM) && defined(_BAKED_SPECULAR)
			#define REQUIRE_VIEWDIRTS
			#endif
			
			#ifdef _PARALLAX_MAP
			#define REQUIRE_VIEWDIRTS
			#endif
			
			struct VertexData
			{
				float4 pos : SV_POSITION;
				float2 uv[4] : TEXCOORD0;
				
				float3 tangent : TEXCOORD4;
				float3 bitangent : TEXCOORD5;
				float3 worldNormal : TEXCOORD6;
				float4 worldPos : TEXCOORD7;
				
				#ifdef REQUIRE_VIEWDIRTS
				float3 viewDirTS : TEXCOORD8;
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				float4 screenPos : TEXCOORD9;
				#endif
				
				#if !defined(UNITY_PASS_SHADOWCASTER)
				UNITY_FOG_COORDS(10)
				UNITY_SHADOW_COORDS(11)
				#endif
				
				#if defined(VERTEXLIGHT_ON)
				half3 vertexLight : TEXCOORD12;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			static VertexData vertexData;
			
			half _Cutoff;
			
			Texture2D _MainTex;
			SamplerState sampler_MainTex;
			float4 _MainTex_ST;
			half4 _Color;
			
			Texture2D _MetallicGlossMap;
			SamplerState sampler_MetallicGlossMap;
			
			Texture2D _BumpMap;
			SamplerState sampler_BumpMap;
			half _BumpScale;
			
			half _Glossiness;
			half _Metallic;
			half2 _GlossinessMinMax;
			half2 _MetallicMinMax;
			half _Occlusion;
			half _Reflectance;
			half _AlbedoSaturation;
			
			Texture2D _ParallaxMap;
			half _Parallax;
			float4 _ParallaxMap_TexelSize;
			
			struct SurfaceData
			{
				half3 albedo;
				half3 tangentNormal;
				half3 emission;
				half metallic;
				half perceptualRoughness;
				half occlusion;
				half reflectance;
				half alpha;
			};
			
			static SurfaceData surf;
			
			void InitializeDefaultSurfaceData()
			{
				surf.albedo = 1.0;
				surf.tangentNormal = half3(0,0,1);
				surf.emission = 0.0;
				surf.metallic = 0.0;
				surf.perceptualRoughness = 0.0;
				surf.occlusion = 1.0;
				surf.reflectance = 0.5;
				surf.alpha = 1.0;
			}
			struct ShaderData
			{
				// probably just gonna put here everything if needed
				float3 worldNormal;
				float3 worldNormalUnmodified;
				float3 bitangent;
				float3 tangent;
				half NoV;
				float3 viewDir;
				float2 parallaxUVOffset;
				float2 DFGLut;
				half3 f0;
				half3 energyCompensation;
				uint facing;
			};
			
			static ShaderData shaderData;
			
			// Partially taken from Google Filament, Xiexe, Catlike Coding and Unity
			// https://google.github.io/filament/Filament.html
			// https://github.com/Xiexe/Unity-Lit-Shader-Templates
			// https://catlikecoding.com/
			
			#define GRAYSCALE float3(0.2125, 0.7154, 0.0721)
			#define TAU float(6.28318530718)
			#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
			
			#ifndef BICUBIC_SAMPLING_INCLUDED
			#define BICUBIC_SAMPLING_INCLUDED
			#if defined(SHADER_API_MOBILE)
			#undef BICUBIC_LIGHTMAP
			#endif
			
			// https://ndotl.wordpress.com/2018/08/29/baking-artifact-free-lightmaps
			// bicubicw0, bicubicw1, bicubicw2, and bicubicw3 are the four cubic B-spline basis functions
			float bicubicw0(float a)
			{
				//    return (1.0f/6.0f)*(-a*a*a + 3.0f*a*a - 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);   // optimized
			}
			
			float bicubicw1(float a)
			{
				//    return (1.0f/6.0f)*(3.0f*a*a*a - 6.0f*a*a + 4.0f);
				return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
			}
			
			float bicubicw2(float a)
			{
				//    return (1.0f/6.0f)*(-3.0f*a*a*a + 3.0f*a*a + 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
			}
			
			float bicubicw3(float a)
			{
				return (1.0f/6.0f)*(a*a*a);
			}
			
			// bicubicg0 and bicubicg1 are the two amplitude functions
			float bicubicg0(float a)
			{
				return bicubicw0(a) + bicubicw1(a);
			}
			
			float bicubicg1(float a)
			{
				return bicubicw2(a) + bicubicw3(a);
			}
			
			// bicubich0 and bicubich1 are the two offset functions
			float bicubich0(float a)
			{
				// note +0.5 offset to compensate for CUDA linear filtering convention
				return -1.0f + bicubicw1(a) / (bicubicw0(a) + bicubicw1(a)) + 0.5f;
			}
			
			float bicubich1(float a)
			{
				return 1.0f + bicubicw3(a) / (bicubicw2(a) + bicubicw3(a)) + 0.5f;
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float2 widthHeight, float2 texelSize)
			{
				#if defined(SHADER_API_MOBILE) || !defined(BICUBIC_LIGHTMAP)
				return t.Sample(s, uv);
				#else
				
				float2 xy = uv * widthHeight - 0.5;
				float2 pxy = floor(xy);
				float2 fxy = xy - pxy;
				
				// note: we could store these functions in a lookup table texture, but maths is cheap
				float bicubicg0x = bicubicg0(fxy.x);
				float bicubicg1x = bicubicg1(fxy.x);
				float bicubich0x = bicubich0(fxy.x);
				float bicubich1x = bicubich1(fxy.x);
				float bicubich0y = bicubich0(fxy.y);
				float bicubich1y = bicubich1(fxy.y);
				
				float4 t0 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich0y) * texelSize);
				float4 t1 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich0y) * texelSize);
				float4 t2 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich1y) * texelSize);
				float4 t3 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich1y) * texelSize);
				
				return bicubicg0(fxy.y) * (t0 + t1) + bicubicg1(fxy.y) * (t2 + t3);
				#endif
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float4 texelSize)
			{
				return SampleBicubic(t, s, uv, texelSize.zw, texelSize.xy);
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv)
			{
				float2 widthHeight;
				t.GetDimensions(widthHeight.x, widthHeight.y);
				float2 texelSize = 1.0 / widthHeight;
				return SampleBicubic(t, s, uv, widthHeight, texelSize);
			}
			#endif
			
			#ifndef ENVIRONMENTBRDF_INCLUDED
			#define ENVIRONMENTBRDF_INCLUDED
			Texture2D _DFG;
			SamplerState sampler_DFG;
			
			half4 SampleDFG(half NoV, half perceptualRoughness)
			{
				return _DFG.Sample(sampler_DFG, float3(NoV, perceptualRoughness, 0));
			}
			
			half3 EnvBRDF(half2 dfg, half3 f0)
			{
				return f0 * dfg.x + dfg.y;
			}
			
			half3 EnvBRDFMultiscatter(half2 dfg, half3 f0)
			{
				return lerp(dfg.xxx, dfg.yyy, f0);
			}
			
			half3 EnvBRDFEnergyCompensation(half2 dfg, half3 f0)
			{
				return 1.0 + f0 * (1.0 / dfg.y - 1.0);
			}
			
			#endif
			
			#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			#define LIGHTMAP_ANY
			#endif
			
			#ifdef LIGHTMAP_ANY
			#if defined(BAKERY_RNM) || defined(BAKERY_SH) || defined(BAKERY_VERTEXLM)
			#define BAKERYLM_ENABLED
			#undef DIRLIGHTMAP_COMBINED
			#endif
			#else
			#undef BAKERY_SH
			#undef BAKERY_RNM
			#endif
			
			#ifndef SHADER_API_MOBILE
			#define VERTEXLIGHT_PS
			#endif
			
			half RemapMinMax(half value, half remapMin, half remapMax)
			{
				return value * (remapMax - remapMin) + remapMin;
			}
			
			float pow5(float x)
			{
				float x2 = x * x;
				return x2 * x2 * x;
			}
			
			float sq(float x)
			{
				return x * x;
			}
			
			half3 F_Schlick(half u, half3 f0)
			{
				return f0 + (1.0 - f0) * pow(1.0 - u, 5.0);
			}
			
			float F_Schlick(float f0, float f90, float VoH)
			{
				return f0 + (f90 - f0) * pow5(1.0 - VoH);
			}
			
			// Input [0, 1] and output [0, PI/2]
			// 9 VALU
			float FastACosPos(float inX)
			{
				float x = abs(inX);
				float res = (0.0468878 * x + -0.203471) * x + 1.570796; // p(x)
				res *= sqrt(1.0 - x);
				
				return res;
			}
			
			float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias)
			{
				float2 ddx_ = scale * uvdx;
				float2 ddy_ = scale * uvdy;
				float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));
				
				return max(0.5 * log2(d) - bias, 0.0);
			}
			
			float ComputeTextureLOD(float2 uv)
			{
				float2 ddx_ = ddx(uv);
				float2 ddy_ = ddy(uv);
				
				return ComputeTextureLOD(ddx_, ddy_, 1.0, 0.0);
			}
			
			half Fd_Burley(half roughness, half NoV, half NoL, half LoH)
			{
				// Burley 2012, "Physically-Based Shading at Disney"
				half f90 = 0.5 + 2.0 * roughness * LoH * LoH;
				float lightScatter = F_Schlick(1.0, f90, NoL);
				float viewScatter  = F_Schlick(1.0, f90, NoV);
				return lightScatter * viewScatter;
			}
			
			float3 getBoxProjection (float3 direction, float3 position, float4 cubemapPosition, float3 boxMin, float3 boxMax)
			{
				#if defined(UNITY_SPECCUBE_BOX_PROJECTION)
				if (cubemapPosition.w > 0.0)
				{
					float3 factors = ((direction > 0.0 ? boxMax : boxMin) - position) / direction;
					float scalar = min(min(factors.x, factors.y), factors.z);
					direction = direction * scalar + (position - cubemapPosition.xyz);
				}
				#endif
				
				return direction;
			}
			
			half computeSpecularAO(half NoV, half ao, half roughness)
			{
				return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
			}
			
			half D_GGX(half NoH, half roughness)
			{
				half a = NoH * roughness;
				half k = roughness / (1.0 - NoH * NoH + a * a);
				return k * k * (1.0 / UNITY_PI);
			}
			
			float V_SmithGGXCorrelatedFast(half NoV, half NoL, half roughness) {
				half a = roughness;
				float GGXV = NoL * (NoV * (1.0 - a) + a);
				float GGXL = NoV * (NoL * (1.0 - a) + a);
				return 0.5 / (GGXV + GGXL);
			}
			
			float V_SmithGGXCorrelated(half NoV, half NoL, half roughness)
			{
				#ifdef SHADER_API_MOBILE
				return V_SmithGGXCorrelatedFast(NoV, NoL, roughness);
				#else
				half a2 = roughness * roughness;
				float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
				float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
				return 0.5 / (GGXV + GGXL);
				#endif
			}
			
			half V_Kelemen(half LoH)
			{
				// Kelemen 2001, "A Microfacet Based Coupled Specular-Matte BRDF Model with Importance Sampling"
				return saturate(0.25 / (LoH * LoH));
			}
			
			half _specularAntiAliasingVariance;
			half _specularAntiAliasingThreshold;
			float GSAA_Filament(float3 worldNormal, float perceptualRoughness)
			{
				// Kaplanyan 2016, "Stable specular highlights"
				// Tokuyoshi 2017, "Error Reduction and Simplification for Shading Anti-Aliasing"
				// Tokuyoshi and Kaplanyan 2019, "Improved Geometric Specular Antialiasing"
				
				// This implementation is meant for deferred rendering in the original paper but
				// we use it in forward rendering as well (as discussed in Tokuyoshi and Kaplanyan
				// 2019). The main reason is that the forward version requires an expensive transform
				// of the half vector by the tangent frame for every light. This is therefore an
				// approximation but it works well enough for our needs and provides an improvement
				// over our original implementation based on Vlachos 2015, "Advanced VR Rendering".
				
				float3 du = ddx(worldNormal);
				float3 dv = ddy(worldNormal);
				
				float variance = _specularAntiAliasingVariance * (dot(du, du) + dot(dv, dv));
				
				float roughness = perceptualRoughness * perceptualRoughness;
				float kernelRoughness = min(2.0 * variance, _specularAntiAliasingThreshold);
				float squareRoughness = saturate(roughness * roughness + kernelRoughness);
				
				return sqrt(sqrt(squareRoughness));
			}
			
			float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
			{
				// average energy
				float R0 = L0;
				
				// avg direction of incoming light
				float3 R1 = 0.5f * L1;
				
				// directional brightness
				float lenR1 = length(R1);
				
				// linear angle between normal and direction 0-1
				//float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
				//float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
				float q = dot(normalize(R1), n) * 0.5 + 0.5;
				q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
				
				// power for q
				// lerps from 1 (linear) to 3 (cubic) based on directionality
				float p = 1.0f + 2.0f * lenR1 / R0;
				
				// dynamic range constant
				// should vary between 4 (highly directional) and 0 (ambient)
				float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
				
				return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
			}
			
			float3 Unity_NormalReconstructZ(float2 In)
			{
				float reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
				float3 normalVector = float3(In.x, In.y, reconstructZ);
				return normalize(normalVector);
			}
			
			#ifdef DYNAMICLIGHTMAP_ON
			float3 getRealtimeLightmap(float2 uv, float3 worldNormal)
			{
				half4 bakedCol = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, uv);
				float3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);
				
				#ifdef DIRLIGHTMAP_COMBINED
				half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
				realtimeLightmap += DecodeDirectionalLightmap (realtimeLightmap, realtimeDirTex, worldNormal);
				#endif
				
				return realtimeLightmap;
			}
			#endif
			
			half3 GetSpecularHighlights(float3 worldNormal, half3 lightColor, float3 lightDirection, half3 f0, float3 viewDir, half clampedRoughness, half NoV, half3 energyCompensation)
			{
				float3 halfVector = Unity_SafeNormalize(lightDirection + viewDir);
				
				half NoH = saturate(dot(worldNormal, halfVector));
				half NoL = saturate(dot(worldNormal, lightDirection));
				half LoH = saturate(dot(lightDirection, halfVector));
				
				half3 F = F_Schlick(LoH, f0);
				half D = D_GGX(NoH, clampedRoughness);
				half V = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
				
				#ifndef SHADER_API_MOBILE
				F *= energyCompensation;
				#endif
				
				return max(0, (D * V) * F) * lightColor * NoL;
			}
			
			float Unity_Dither(float In, float2 ScreenPosition)
			{
				float2 uv = ScreenPosition * _ScreenParams.xy;
				const half4 DITHER_THRESHOLDS[4] =
				{
					half4(1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0),
					half4(13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0),
					half4(4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0),
					half4(16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0)
				};
				
				return In - DITHER_THRESHOLDS[uint(uv.x) % 4][uint(uv.y) % 4];
			}
			
			void AACutout(inout half alpha, half cutoff)
			{
				alpha = (alpha - cutoff) / max(fwidth(alpha), 0.0001) + 0.5;
			}
			
			void FlipBTN(uint facing, inout float3 worldNormal, inout float3 bitangent, inout float3 tangent)
			{
				#if !defined(LIGHTMAP_ON)
				UNITY_FLATTEN
				if (!facing)
				{
					worldNormal *= -1.0;
					bitangent *= -1.0;
					tangent *= -1.0;
				}
				#endif
			}
			
			void TangentToWorldNormal(float3 normalTS, inout float3 normalWS, inout float3 tangent, inout float3 bitangent)
			{
				normalWS = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normalWS);
				tangent = normalize(cross(normalWS, bitangent));
				bitangent = normalize(cross(normalWS, tangent));
			}
			
			half NormalDotViewDir(float3 normalWS, float3 viewDir)
			{
				return abs(dot(normalWS, viewDir)) + 1e-5f;
			}
			
			half3 GetF0(half reflectance, half metallic, half3 albedo)
			{
				return 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo * metallic;
			}
			
			struct LightData
			{
				half3 Color;
				float3 Direction;
				half NoL;
				half LoH;
				half NoH;
				float3 HalfVector;
				half3 FinalColor;
				half3 Specular;
				half Attenuation;
			};
			static LightData lightData;
			
			half3 MainLightSpecular(LightData lightData, half NoV, half clampedRoughness, half3 f0)
			{
				half3 F = F_Schlick(lightData.LoH, f0) * shaderData.energyCompensation;
				half D = D_GGX(lightData.NoH, clampedRoughness);
				half V = V_SmithGGXCorrelated(NoV, lightData.NoL, clampedRoughness);
				
				return max(0.0, (D * V) * F) * lightData.FinalColor;
			}
			
			#if defined(UNITY_PASS_FORWARDBASE) && defined(DIRECTIONAL) && !(defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING))
			#define BRANCH_DIRECTIONAL
			
			#ifdef _SPECULAR_HIGHLIGHTS_OFF
			#undef BRANCH_DIRECTIONAL
			#endif
			#endif
			
			void InitializeLightData(inout LightData lightData, float3 normalWS, float3 viewDir, half NoV, half clampedRoughness, half perceptualRoughness, half3 f0)
			{
				#ifdef USING_LIGHT_MULTI_COMPILE
				#ifdef BRANCH_DIRECTIONAL
				UNITY_BRANCH
				if (any(_WorldSpaceLightPos0.xyz))
				{
					//printf("directional branch");
					#endif
					lightData.Direction = normalize(UnityWorldSpaceLightDir(vertexData.worldPos));
					lightData.HalfVector = Unity_SafeNormalize(lightData.Direction + viewDir);
					lightData.NoL = saturate(dot(normalWS, lightData.Direction));
					lightData.LoH = saturate(dot(lightData.Direction, lightData.HalfVector));
					lightData.NoH = saturate(dot(normalWS, lightData.HalfVector));
					
					UNITY_LIGHT_ATTENUATION(lightAttenuation, vertexData, vertexData.worldPos.xyz);
					lightData.Attenuation = lightAttenuation;
					lightData.Color = lightAttenuation * _LightColor0.rgb;
					lightData.FinalColor = (lightData.NoL * lightData.Color);
					
					#ifndef SHADER_API_MOBILE
					lightData.FinalColor *= Fd_Burley(perceptualRoughness, NoV, lightData.NoL, lightData.LoH);
					#endif
					
					#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
					float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
					lightData.FinalColor *= UnityComputeForwardShadows(lightmapUV, vertexData.worldPos, vertexData.screenPos);
					#endif
					
					lightData.Specular = MainLightSpecular(lightData, NoV, clampedRoughness, f0);
					#ifdef BRANCH_DIRECTIONAL
				}
				else
				{
					lightData = (LightData)0;
				}
				#endif
				#else
				lightData = (LightData)0;
				#endif
			}
			
			#if defined(VERTEXLIGHT_ON) && defined(UNITY_PASS_FORWARDBASE)
			void NonImportantLightsPerPixel(inout half3 lightColor, inout half3 directSpecular, float3 positionWS, float3 normalWS, float3 viewDir, half NoV, half3 f0, half clampedRoughness)
			{
				float4 toLightX = unity_4LightPosX0 - positionWS.x;
				float4 toLightY = unity_4LightPosY0 - positionWS.y;
				float4 toLightZ = unity_4LightPosZ0 - positionWS.z;
				
				float4 lengthSq = 0.0;
				lengthSq += toLightX * toLightX;
				lengthSq += toLightY * toLightY;
				lengthSq += toLightZ * toLightZ;
				
				float4 attenuation = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
				float4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25.0));
				attenuation = min(attenuation, atten2 * atten2);
				
				[unroll(4)]
				for(uint i = 0; i < 4; i++)
				{
					UNITY_BRANCH
					if (attenuation[i] > 0.0)
					{
						float3 direction = normalize(float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]) - positionWS);
						half NoL = saturate(dot(normalWS, direction));
						half3 color = NoL * attenuation[i] * unity_LightColor[i];
						lightColor += color;
						
						#ifndef _SPECULAR_HIGHLIGHTS_OFF
						float3 halfVector = Unity_SafeNormalize(direction + viewDir);
						half vNoH = saturate(dot(normalWS, halfVector));
						half vLoH = saturate(dot(direction, halfVector));
						
						half3 Fv = F_Schlick(vLoH, f0);
						half Dv = D_GGX(vNoH, clampedRoughness);
						half Vv = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
						directSpecular += max(0.0, (Dv * Vv) * Fv) * color;
						#endif
					}
				}
			}
			#endif
			
			half3 GetReflections(float3 normalWS, float3 positionWS, float3 viewDir, half3 f0, half roughness, half NoV, half3 indirectDiffuse)
			{
				half3 indirectSpecular = 0;
				#if defined(UNITY_PASS_FORWARDBASE)
				
				float3 reflDir = reflect(-viewDir, normalWS);
				reflDir = lerp(reflDir, normalWS, roughness * roughness);
				
				Unity_GlossyEnvironmentData envData;
				envData.roughness = surf.perceptualRoughness;
				envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
				
				half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
				indirectSpecular = probe0;
				
				#if defined(UNITY_SPECCUBE_BLENDING) && !defined(SHADER_API_MOBILE)
				UNITY_BRANCH
				if (unity_SpecCube0_BoxMin.w < 0.99999)
				{
					envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
					float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
					indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
				}
				#endif
				
				float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
				float2 dfg = shaderData.DFGLut;
				#ifdef LIGHTMAP_ANY
				dfg.x *= lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), 1.0);
				#endif
				indirectSpecular = indirectSpecular * horizon * horizon * shaderData.energyCompensation * EnvBRDFMultiscatter(dfg, f0);
				indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);
				
				#endif
				
				return indirectSpecular;
			}
			
			half3 GetLightProbes(float3 normalWS, float3 positionWS)
			{
				half3 indirectDiffuse = 0;
				#ifndef LIGHTMAP_ANY
				#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				UNITY_BRANCH
				if (unity_ProbeVolumeParams.x == 1.0)
				{
					indirectDiffuse = SHEvalLinearL0L1_SampleProbeVolume(float4(normalWS, 1.0), positionWS);
				}
				else
				{
					#endif
					#ifdef _NONLINEAR_LIGHTPROBESH
					float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					indirectDiffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normalWS);
					indirectDiffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normalWS);
					indirectDiffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normalWS);
					#else
					indirectDiffuse = ShadeSH9(float4(normalWS, 1.0));
					#endif
					#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				}
				#endif
				#endif
				return indirectDiffuse;
			}
			
			#ifndef BAKERY_INCLUDED
			#define BAKERY_INCLUDED
			
			Texture2D _RNM0, _RNM1, _RNM2;
			SamplerState sampler_RNM0, sampler_RNM1, sampler_RNM2;
			float4 _RNM0_TexelSize;
			
			#if !defined(SHADER_API_MOBILE)
			#define BAKERY_SHNONLINEAR
			#endif
			
			void BakeryRNMLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalTS, float3 viewDirTS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_RNM
				normalTS.g *= -1;
				float3 rnm0 = DecodeLightmap(_RNM0.Sample(sampler_RNM0, lightmapUV));
				float3 rnm1 = DecodeLightmap(_RNM1.Sample(sampler_RNM1, lightmapUV));
				float3 rnm2 = DecodeLightmap(_RNM2.Sample(sampler_RNM2, lightmapUV));
				
				const float3 rnmBasis0 = float3(0.816496580927726f, 0.0f, 0.5773502691896258f);
				const float3 rnmBasis1 = float3(-0.4082482904638631f, 0.7071067811865475f, 0.5773502691896258f);
				const float3 rnmBasis2 = float3(-0.4082482904638631f, -0.7071067811865475f, 0.5773502691896258f);
				
				lightMap =    saturate(dot(rnmBasis0, normalTS)) * rnm0
				+ saturate(dot(rnmBasis1, normalTS)) * rnm1
				+ saturate(dot(rnmBasis2, normalTS)) * rnm2;
				
				#ifdef _BAKED_SPECULAR
				float3 viewDirT = -normalize(viewDirTS);
				float3 dominantDirT = rnmBasis0 * dot(rnm0, GRAYSCALE) +
				rnmBasis1 * dot(rnm1, GRAYSCALE) +
				rnmBasis2 * dot(rnm2, GRAYSCALE);
				
				float3 dominantDirTN = normalize(dominantDirT);
				half3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
				saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
				saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;
				
				half3 halfDir = Unity_SafeNormalize(dominantDirTN - viewDirT);
				half NoH = saturate(dot(normalTS, halfDir));
				half spec = D_GGX(NoH, roughness);
				directSpecular += spec * specColor;
				#endif
				
				#endif
			}
			
			void BakerySHLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalWS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_SH
				
				half3 L0 = lightMap;
				float3 nL1x = _RNM0.Sample(sampler_RNM0, lightmapUV) * 2.0 - 1.0;
				float3 nL1y = _RNM1.Sample(sampler_RNM1, lightmapUV) * 2.0 - 1.0;
				float3 nL1z = _RNM2.Sample(sampler_RNM2, lightmapUV) * 2.0 - 1.0;
				float3 L1x = nL1x * L0 * 2.0;
				float3 L1y = nL1y * L0 * 2.0;
				float3 L1z = nL1z * L0 * 2.0;
				
				#ifdef BAKERY_SHNONLINEAR
				float lumaL0 = dot(L0, float(1));
				float lumaL1x = dot(L1x, float(1));
				float lumaL1y = dot(L1y, float(1));
				float lumaL1z = dot(L1z, float(1));
				float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWS);
				
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				float regularLumaSH = dot(lightMap, 1.0);
				lightMap *= lerp(1.0, lumaSH / regularLumaSH, saturate(regularLumaSH * 16.0));
				#else
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				#endif
				
				#ifdef _BAKED_SPECULAR
				float3 dominantDir = float3(dot(nL1x, GRAYSCALE), dot(nL1y, GRAYSCALE), dot(nL1z, GRAYSCALE));
				float3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + viewDir);
				half NoH = saturate(dot(normalWS, halfDir));
				half spec = D_GGX(NoH, roughness);
				half3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
				dominantDir = normalize(dominantDir);
				
				directSpecular += max(spec * sh, 0.0);
				#endif
				
				#endif
			}
			#endif
			
			void InitializeShaderData(uint facing)
			{
				shaderData.facing = facing;
				FlipBTN(facing, vertexData.worldNormal, vertexData.bitangent, vertexData.tangent);
				
				#ifdef _GEOMETRICSPECULAR_AA
				surf.perceptualRoughness = GSAA_Filament(vertexData.worldNormal, surf.perceptualRoughness);
				#endif
				shaderData.worldNormal = vertexData.worldNormal;
				shaderData.bitangent = vertexData.bitangent;
				shaderData.tangent = vertexData.tangent;
				
				surf.tangentNormal.g *= -1;
				TangentToWorldNormal(surf.tangentNormal, shaderData.worldNormal, shaderData.tangent, shaderData.bitangent);
				
				shaderData.viewDir = normalize(UnityWorldSpaceViewDir(vertexData.worldPos));
				shaderData.NoV = NormalDotViewDir(shaderData.worldNormal, shaderData.viewDir);
				shaderData.f0 = GetF0(surf.reflectance, surf.metallic, surf.albedo.rgb);
				shaderData.DFGLut = SampleDFG(shaderData.NoV, surf.perceptualRoughness).rg;
				shaderData.energyCompensation = EnvBRDFEnergyCompensation(shaderData.DFGLut, shaderData.f0);
			}
			
			#ifndef POM_INCLUDED
			#define POM_INCLUDED
			// com.unity.render-pipelines.core copyright © 2020 Unity Technologies ApS
			// Licensed under the Unity Companion License for Unity-dependent projects--see https://unity3d.com/legal/licenses/Unity_Companion_License.
			
			// This is implementation of parallax occlusion mapping (POM)
			// This function require that the caller define a callback for the height sampling name ComputePerPixelHeightDisplacement
			// A PerPixelHeightDisplacementParam is used to provide all data necessary to calculate the heights to ComputePerPixelHeightDisplacement it doesn't need to be
			// visible by the POM algorithm.
			// This function is compatible with tiled uv.
			// it return the offset to apply to the UVSet provide in PerPixelHeightDisplacementParam
			// viewDirTS is view vector in texture space matching the UVSet
			// ref: https://www.gamedev.net/resources/_/technical/graphics-programming-and-theory/a-closer-look-at-parallax-occlusion-mapping-r3262
			
			#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
			#define FLT_MIN         1.175494351e-38 // Minimum representable positive floating-point number
			#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number
			#define INV_HALF_PI     0.636619772367
			
			float _ParallaxFadingMip;
			float _MinSteps;
			float _MaxSteps;
			
			struct PerPixelHeightDisplacementParam
			{
				float2 uv;
				SamplerState sampl;
				Texture2D height;
			};
			
			float2 GetMinUvSize(float2 baseUV, float4 texelSize)
			{
				float2 minUvSize = float2(FLT_MAX, FLT_MAX);
				
				minUvSize = min(baseUV * texelSize.zw, minUvSize);
				
				return minUvSize;
			}
			
			float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
			{
				// Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
				// Tiling is automatically handled correctly here.
				return param.height.SampleLevel(param.sampl, param.uv + texOffsetCurrent, lod).r;
			}
			
			float2 ParallaxOcclusionMapping(float lod, float lodThreshold, uint numSteps, float uvSpaceScale, float3 viewDirTS, PerPixelHeightDisplacementParam ppdParam, out float outHeight)
			{
				// Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
				float stepSize = 1.0 / (float)numSteps;
				
				// View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
				// The length of viewDirTS vector determines the furthest amount of displacement:
				// float parallaxLimit = -length(viewDirTS.xy) / viewDirTS.z;
				// float2 parallaxDir = normalize(Out.viewDirTS.xy);
				// float2 parallaxMaxOffsetTS = parallaxDir * parallaxLimit;
				// Above code simplify to
				float2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z);
				float2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;
				
				// Do a first step before the loop to init all value correctly
				float2 texOffsetCurrent = float2(0.0, 0.0);
				float prevHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				texOffsetCurrent += texOffsetPerStep;
				float currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				float rayHeight = 1.0 - stepSize; // Start at top less one sample
				
				// Linear search
				for (uint stepIndex = 0; stepIndex < numSteps; ++stepIndex)
				{
					// Have we found a height below our ray height ? then we have an intersection
					if (currHeight > rayHeight)
					break; // end the loop
					
					prevHeight = currHeight;
					rayHeight -= stepSize;
					texOffsetCurrent += texOffsetPerStep;
					
					// Sample height map which in this case is stored in the alpha channel of the normal map:
					currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				}
				
				// Found below and above points, now perform line interesection (ray) with piecewise linear heightfield approximation
				
				// Refine the search with secant method
				#define POM_SECANT_METHOD 1
				#if POM_SECANT_METHOD
				
				float pt0 = rayHeight + stepSize;
				float pt1 = rayHeight;
				float delta0 = pt0 - prevHeight;
				float delta1 = pt1 - currHeight;
				
				float delta;
				float2 offset;
				
				// Secant method to affine the search
				// Ref: Faster Relief Mapping Using the Secant Method - Eric Risser
				[unroll]
				for (uint i = 0; i < 3; ++i)
				{
					// intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
					float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
					// Retrieve offset require to find this intersectionHeight
					offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
					
					currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
					
					delta = intersectionHeight - currHeight;
					
					if (abs(delta) <= 0.01)
					break;
					
					// intersectionHeight < currHeight => new lower bounds
					if (delta < 0.0)
					{
						delta1 = delta;
						pt1 = intersectionHeight;
					}
					else
					{
						delta0 = delta;
						pt0 = intersectionHeight;
					}
				}
				
				#else // regular POM intersection
				
				//float pt0 = rayHeight + stepSize;
				//float pt1 = rayHeight;
				//float delta0 = pt0 - prevHeight;
				//float delta1 = pt1 - currHeight;
				//float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
				//float2 offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
				
				// A bit more optimize
				float delta0 = currHeight - rayHeight;
				float delta1 = (rayHeight + stepSize) - prevHeight;
				float ratio = delta0 / (delta0 + delta1);
				float2 offset = texOffsetCurrent - ratio * texOffsetPerStep;
				
				currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
				
				#endif
				
				outHeight = currHeight;
				
				// Fade the effect with lod (allow to avoid pop when switching to a discrete LOD mesh)
				offset *= (1.0 - saturate(lod - lodThreshold));
				
				return offset;
			}
			
			float2 ParallaxOcclusionMappingUVOffset(float2 uv, float scale, float3 viewDirTS, Texture2D tex, SamplerState sampl, float4 texelSize)
			{
				float3 viewDirUV = normalize(float3(viewDirTS.xy * scale, viewDirTS.z));
				
				float unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);
				uint numSteps = (uint)lerp(_MinSteps, _MaxSteps, unitAngle);
				
				float2 minUvSize = GetMinUvSize(uv, texelSize);
				float lod = ComputeTextureLOD(minUvSize);
				
				PerPixelHeightDisplacementParam ppdParam;
				
				ppdParam.uv = uv;
				ppdParam.height = tex;
				ppdParam.sampl = sampl;
				
				float height = 0;
				float2 offset = ParallaxOcclusionMapping(lod, _ParallaxFadingMip, numSteps, scale, viewDirUV, ppdParam, height);
				
				return offset;
			}
			#endif
			void InitializeSurfaceData()
			{
				float2 mainUV = TRANSFORM_TEX(vertexData.uv[0], _MainTex);
				#ifdef _PARALLAX_MAP
				shaderData.parallaxUVOffset = ParallaxOcclusionMappingUVOffset(mainUV, _Parallax, vertexData.viewDirTS, _ParallaxMap, sampler_MainTex, _ParallaxMap_TexelSize);
				#endif
				mainUV += shaderData.parallaxUVOffset;
				
				half4 mainTex = _MainTex.Sample(sampler_MainTex, mainUV);
				mainTex.rgb = lerp(dot(mainTex.rgb, GRAYSCALE), mainTex.rgb, _AlbedoSaturation);
				mainTex *= _Color;
				surf.albedo = mainTex.rgb;
				surf.alpha = mainTex.a;
				
				#ifdef _NORMAL_MAP
				half4 normalMap = _BumpMap.Sample(sampler_BumpMap, mainUV);
				surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
				#endif
				
				#ifdef _MASK_MAP
				half4 maskMap = _MetallicGlossMap.Sample(sampler_MetallicGlossMap, mainUV);
				surf.perceptualRoughness = 1.0 - (RemapMinMax(maskMap.a, _GlossinessMinMax.x, _GlossinessMinMax.y));
				surf.metallic = RemapMinMax(maskMap.r, _MetallicMinMax.x, _MetallicMinMax.y);
				surf.occlusion = lerp(1.0, maskMap.g, _Occlusion);
				#else
				surf.perceptualRoughness = 1.0 - _Glossiness;
				surf.metallic = _Metallic;
				#endif
				
				surf.reflectance = _Reflectance;
			}
			
			VertexData Vertex(VertexInput v)
			{
				VertexData o;
				UNITY_INITIALIZE_OUTPUT(VertexData, o);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				#ifdef UNITY_PASS_META
				o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
				#else
				#if !defined(UNITY_PASS_SHADOWCASTER)
				o.pos = UnityObjectToClipPos(v.vertex);
				#endif
				#endif
				
				o.uv[0].xy = v.uv0.xy;
				o.uv[1].xy = v.uv1.xy;
				o.uv[2].xy = v.uv2.xy;
				o.uv[3].xy = v.uv3.xy;
				
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
				o.bitangent = cross(o.tangent, o.worldNormal) * v.tangent.w;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				o.vertexLight = Shade4PointLights
				(
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb,
				unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, o.worldPos,  o.worldNormal
				);
				#endif
				
				#if defined(REQUIRE_VIEWDIRTS)
				TANGENT_SPACE_ROTATION;
				o.viewDirTS = mul(rotation, ObjSpaceViewDir(v.vertex));
				#endif
				
				#ifdef UNITY_PASS_SHADOWCASTER
				o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
				o.pos = UnityApplyLinearShadowBias(o.pos);
				TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
				#else
				UNITY_TRANSFER_SHADOW(o, o.uv[1].xy);
				UNITY_TRANSFER_FOG(o,o.pos);
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				o.screenPos = ComputeScreenPos(o.pos);
				#endif
				
				return o;
			}
			
			half4 Fragment(VertexData input, uint facing : SV_IsFrontFace) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(vertexData)
				vertexData = input;
				
				#if defined(LOD_FADE_CROSSFADE)
				UnityApplyDitherCrossFade(vertexData.pos);
				#endif
				
				InitializeDefaultSurfaceData();
				InitializeSurfaceData();
				
				shaderData = (ShaderData)0;
				InitializeShaderData(facing);
				
				#if defined(UNITY_PASS_SHADOWCASTER)
				#if defined(_MODE_CUTOUT)
				if (surf.alpha < _Cutoff) discard;
				#endif
				
				#ifdef _ALPHAPREMULTIPLY_ON
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAPREMULTIPLY_ON) || defined(_MODE_FADE)
				half dither = Unity_Dither(surf.alpha, input.pos.xy);
				if (dither < 0.0) discard;
				#endif
				
				SHADOW_CASTER_FRAGMENT(vertexData);
				#else
				
				half3 indirectSpecular = 0.0;
				half3 directSpecular = 0.0;
				half3 otherSpecular = 0.0;
				
				half roughness = surf.perceptualRoughness * surf.perceptualRoughness;
				half clampedRoughness = max(roughness, 0.002);
				
				InitializeLightData(lightData, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, clampedRoughness, surf.perceptualRoughness, shaderData.f0);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				lightData.FinalColor += vertexData.vertexLight;
				#endif
				
				#if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
				NonImportantLightsPerPixel(lightData.FinalColor, directSpecular, vertexData.worldPos, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, shaderData.f0, clampedRoughness);
				#endif
				
				half3 indirectDiffuse;
				#if defined(LIGHTMAP_ANY)
				
				float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
				half4 bakedColorTex = SampleBicubic(unity_Lightmap, samplerunity_Lightmap, lightmapUV);
				half3 lightMap = DecodeLightmap(bakedColorTex);
				
				#ifdef BAKERY_RNM
				BakeryRNMLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, surf.tangentNormal, vertexData.viewDirTS, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#ifdef BAKERY_SH
				BakerySHLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, shaderData.worldNormal, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#if defined(DIRLIGHTMAP_COMBINED)
				float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
				lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, shaderData.worldNormal);
				#endif
				
				#if defined(DYNAMICLIGHTMAP_ON)
				float2 realtimeLightmapUV = vertexData.uv[2] * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				float3 realtimeLightMap = getRealtimeLightmap(realtimeLightmapUV, shaderData.worldNormal);
				lightMap += realtimeLightMap;
				#endif
				
				#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
				lightData.FinalColor = 0.0;
				lightData.Specular = 0.0;
				lightMap = SubtractMainLightWithRealtimeAttenuationFromLightmap (lightMap, lightData.Attenuation, bakedColorTex, shaderData.worldNormal);
				#endif
				
				indirectDiffuse = lightMap;
				#else
				indirectDiffuse = GetLightProbes(shaderData.worldNormal, vertexData.worldPos.xyz);
				#endif
				indirectDiffuse = max(0.0, indirectDiffuse);
				
				#if !defined(_SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
				directSpecular += lightData.Specular;
				#endif
				
				#if defined(_BAKED_SPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERYLM_ENABLED)
				{
					float3 bakedDominantDirection = 1.0;
					half3 bakedSpecularColor = 0.0;
					
					#if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
					bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
					bakedSpecularColor = indirectDiffuse;
					#endif
					
					#ifndef LIGHTMAP_ANY
					bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
					#endif
					
					bakedDominantDirection = normalize(bakedDominantDirection);
					directSpecular += GetSpecularHighlights(shaderData.worldNormal, bakedSpecularColor, bakedDominantDirection, shaderData.f0, shaderData.viewDir, clampedRoughness, shaderData.NoV, shaderData.energyCompensation);
				}
				#endif
				
				#if !defined(_REFLECTIONS_OFF) && defined(UNITY_PASS_FORWARDBASE)
				indirectSpecular += GetReflections(shaderData.worldNormal, vertexData.worldPos.xyz, shaderData.viewDir, shaderData.f0, roughness, shaderData.NoV, indirectDiffuse);
				#endif
				
				otherSpecular *= EnvBRDFMultiscatter(shaderData.DFGLut, shaderData.f0) * shaderData.energyCompensation;
				
				#if defined(_ALPHAPREMULTIPLY_ON)
				surf.albedo.rgb *= surf.alpha;
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAMODULATE_ON)
				surf.albedo.rgb = lerp(1.0, surf.albedo.rgb, surf.alpha);
				#endif
				
				#if defined(_MODE_CUTOUT)
				AACutout(surf.alpha, _Cutoff);
				#endif
				
				half4 finalColor = 0;
				//final color
				finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + (lightData.FinalColor))
				+ indirectSpecular + (directSpecular * UNITY_PI) + otherSpecular + surf.emission, surf.alpha);
				
				#ifdef UNITY_PASS_META
				UnityMetaInput metaInput;
				UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
				metaInput.Emission = surf.emission;
				metaInput.Albedo = surf.albedo;
				
				return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
				#endif
				
				UNITY_APPLY_FOG(vertexData.fogCoord, finalColor);
				
				return finalColor;
				#endif
			}
			
			ENDCG
		}
		Pass
		{
			Name "ShadowCaster"
			Tags
			{
				"LightMode"="ShadowCaster"
			}
			ZWrite On
			Cull [_Cull]
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 4.5
			#pragma vertex Vertex
			#pragma fragment Fragment
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _MODE_FADE
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				float4 tangent : TANGENT;
				float4 color : COLOR;
				uint vertexId : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
			#define REQUIRE_SCREENPOS
			#endif
			
			#if defined(BAKERY_RNM) && defined(_BAKED_SPECULAR)
			#define REQUIRE_VIEWDIRTS
			#endif
			
			#ifdef _PARALLAX_MAP
			#define REQUIRE_VIEWDIRTS
			#endif
			
			struct VertexData
			{
				float4 pos : SV_POSITION;
				float2 uv[4] : TEXCOORD0;
				
				float3 tangent : TEXCOORD4;
				float3 bitangent : TEXCOORD5;
				float3 worldNormal : TEXCOORD6;
				float4 worldPos : TEXCOORD7;
				
				#ifdef REQUIRE_VIEWDIRTS
				float3 viewDirTS : TEXCOORD8;
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				float4 screenPos : TEXCOORD9;
				#endif
				
				#if !defined(UNITY_PASS_SHADOWCASTER)
				UNITY_FOG_COORDS(10)
				UNITY_SHADOW_COORDS(11)
				#endif
				
				#if defined(VERTEXLIGHT_ON)
				half3 vertexLight : TEXCOORD12;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			static VertexData vertexData;
			
			half _Cutoff;
			
			Texture2D _MainTex;
			SamplerState sampler_MainTex;
			float4 _MainTex_ST;
			half4 _Color;
			
			Texture2D _MetallicGlossMap;
			SamplerState sampler_MetallicGlossMap;
			
			Texture2D _BumpMap;
			SamplerState sampler_BumpMap;
			half _BumpScale;
			
			half _Glossiness;
			half _Metallic;
			half2 _GlossinessMinMax;
			half2 _MetallicMinMax;
			half _Occlusion;
			half _Reflectance;
			half _AlbedoSaturation;
			
			Texture2D _ParallaxMap;
			half _Parallax;
			float4 _ParallaxMap_TexelSize;
			
			struct SurfaceData
			{
				half3 albedo;
				half3 tangentNormal;
				half3 emission;
				half metallic;
				half perceptualRoughness;
				half occlusion;
				half reflectance;
				half alpha;
			};
			
			static SurfaceData surf;
			
			void InitializeDefaultSurfaceData()
			{
				surf.albedo = 1.0;
				surf.tangentNormal = half3(0,0,1);
				surf.emission = 0.0;
				surf.metallic = 0.0;
				surf.perceptualRoughness = 0.0;
				surf.occlusion = 1.0;
				surf.reflectance = 0.5;
				surf.alpha = 1.0;
			}
			struct ShaderData
			{
				// probably just gonna put here everything if needed
				float3 worldNormal;
				float3 worldNormalUnmodified;
				float3 bitangent;
				float3 tangent;
				half NoV;
				float3 viewDir;
				float2 parallaxUVOffset;
				float2 DFGLut;
				half3 f0;
				half3 energyCompensation;
				uint facing;
			};
			
			static ShaderData shaderData;
			
			// Partially taken from Google Filament, Xiexe, Catlike Coding and Unity
			// https://google.github.io/filament/Filament.html
			// https://github.com/Xiexe/Unity-Lit-Shader-Templates
			// https://catlikecoding.com/
			
			#define GRAYSCALE float3(0.2125, 0.7154, 0.0721)
			#define TAU float(6.28318530718)
			#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
			
			#ifndef BICUBIC_SAMPLING_INCLUDED
			#define BICUBIC_SAMPLING_INCLUDED
			#if defined(SHADER_API_MOBILE)
			#undef BICUBIC_LIGHTMAP
			#endif
			
			// https://ndotl.wordpress.com/2018/08/29/baking-artifact-free-lightmaps
			// bicubicw0, bicubicw1, bicubicw2, and bicubicw3 are the four cubic B-spline basis functions
			float bicubicw0(float a)
			{
				//    return (1.0f/6.0f)*(-a*a*a + 3.0f*a*a - 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);   // optimized
			}
			
			float bicubicw1(float a)
			{
				//    return (1.0f/6.0f)*(3.0f*a*a*a - 6.0f*a*a + 4.0f);
				return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
			}
			
			float bicubicw2(float a)
			{
				//    return (1.0f/6.0f)*(-3.0f*a*a*a + 3.0f*a*a + 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
			}
			
			float bicubicw3(float a)
			{
				return (1.0f/6.0f)*(a*a*a);
			}
			
			// bicubicg0 and bicubicg1 are the two amplitude functions
			float bicubicg0(float a)
			{
				return bicubicw0(a) + bicubicw1(a);
			}
			
			float bicubicg1(float a)
			{
				return bicubicw2(a) + bicubicw3(a);
			}
			
			// bicubich0 and bicubich1 are the two offset functions
			float bicubich0(float a)
			{
				// note +0.5 offset to compensate for CUDA linear filtering convention
				return -1.0f + bicubicw1(a) / (bicubicw0(a) + bicubicw1(a)) + 0.5f;
			}
			
			float bicubich1(float a)
			{
				return 1.0f + bicubicw3(a) / (bicubicw2(a) + bicubicw3(a)) + 0.5f;
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float2 widthHeight, float2 texelSize)
			{
				#if defined(SHADER_API_MOBILE) || !defined(BICUBIC_LIGHTMAP)
				return t.Sample(s, uv);
				#else
				
				float2 xy = uv * widthHeight - 0.5;
				float2 pxy = floor(xy);
				float2 fxy = xy - pxy;
				
				// note: we could store these functions in a lookup table texture, but maths is cheap
				float bicubicg0x = bicubicg0(fxy.x);
				float bicubicg1x = bicubicg1(fxy.x);
				float bicubich0x = bicubich0(fxy.x);
				float bicubich1x = bicubich1(fxy.x);
				float bicubich0y = bicubich0(fxy.y);
				float bicubich1y = bicubich1(fxy.y);
				
				float4 t0 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich0y) * texelSize);
				float4 t1 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich0y) * texelSize);
				float4 t2 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich1y) * texelSize);
				float4 t3 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich1y) * texelSize);
				
				return bicubicg0(fxy.y) * (t0 + t1) + bicubicg1(fxy.y) * (t2 + t3);
				#endif
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float4 texelSize)
			{
				return SampleBicubic(t, s, uv, texelSize.zw, texelSize.xy);
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv)
			{
				float2 widthHeight;
				t.GetDimensions(widthHeight.x, widthHeight.y);
				float2 texelSize = 1.0 / widthHeight;
				return SampleBicubic(t, s, uv, widthHeight, texelSize);
			}
			#endif
			
			#ifndef ENVIRONMENTBRDF_INCLUDED
			#define ENVIRONMENTBRDF_INCLUDED
			Texture2D _DFG;
			SamplerState sampler_DFG;
			
			half4 SampleDFG(half NoV, half perceptualRoughness)
			{
				return _DFG.Sample(sampler_DFG, float3(NoV, perceptualRoughness, 0));
			}
			
			half3 EnvBRDF(half2 dfg, half3 f0)
			{
				return f0 * dfg.x + dfg.y;
			}
			
			half3 EnvBRDFMultiscatter(half2 dfg, half3 f0)
			{
				return lerp(dfg.xxx, dfg.yyy, f0);
			}
			
			half3 EnvBRDFEnergyCompensation(half2 dfg, half3 f0)
			{
				return 1.0 + f0 * (1.0 / dfg.y - 1.0);
			}
			
			#endif
			
			#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			#define LIGHTMAP_ANY
			#endif
			
			#ifdef LIGHTMAP_ANY
			#if defined(BAKERY_RNM) || defined(BAKERY_SH) || defined(BAKERY_VERTEXLM)
			#define BAKERYLM_ENABLED
			#undef DIRLIGHTMAP_COMBINED
			#endif
			#else
			#undef BAKERY_SH
			#undef BAKERY_RNM
			#endif
			
			#ifndef SHADER_API_MOBILE
			#define VERTEXLIGHT_PS
			#endif
			
			half RemapMinMax(half value, half remapMin, half remapMax)
			{
				return value * (remapMax - remapMin) + remapMin;
			}
			
			float pow5(float x)
			{
				float x2 = x * x;
				return x2 * x2 * x;
			}
			
			float sq(float x)
			{
				return x * x;
			}
			
			half3 F_Schlick(half u, half3 f0)
			{
				return f0 + (1.0 - f0) * pow(1.0 - u, 5.0);
			}
			
			float F_Schlick(float f0, float f90, float VoH)
			{
				return f0 + (f90 - f0) * pow5(1.0 - VoH);
			}
			
			// Input [0, 1] and output [0, PI/2]
			// 9 VALU
			float FastACosPos(float inX)
			{
				float x = abs(inX);
				float res = (0.0468878 * x + -0.203471) * x + 1.570796; // p(x)
				res *= sqrt(1.0 - x);
				
				return res;
			}
			
			float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias)
			{
				float2 ddx_ = scale * uvdx;
				float2 ddy_ = scale * uvdy;
				float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));
				
				return max(0.5 * log2(d) - bias, 0.0);
			}
			
			float ComputeTextureLOD(float2 uv)
			{
				float2 ddx_ = ddx(uv);
				float2 ddy_ = ddy(uv);
				
				return ComputeTextureLOD(ddx_, ddy_, 1.0, 0.0);
			}
			
			half Fd_Burley(half roughness, half NoV, half NoL, half LoH)
			{
				// Burley 2012, "Physically-Based Shading at Disney"
				half f90 = 0.5 + 2.0 * roughness * LoH * LoH;
				float lightScatter = F_Schlick(1.0, f90, NoL);
				float viewScatter  = F_Schlick(1.0, f90, NoV);
				return lightScatter * viewScatter;
			}
			
			float3 getBoxProjection (float3 direction, float3 position, float4 cubemapPosition, float3 boxMin, float3 boxMax)
			{
				#if defined(UNITY_SPECCUBE_BOX_PROJECTION)
				if (cubemapPosition.w > 0.0)
				{
					float3 factors = ((direction > 0.0 ? boxMax : boxMin) - position) / direction;
					float scalar = min(min(factors.x, factors.y), factors.z);
					direction = direction * scalar + (position - cubemapPosition.xyz);
				}
				#endif
				
				return direction;
			}
			
			half computeSpecularAO(half NoV, half ao, half roughness)
			{
				return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
			}
			
			half D_GGX(half NoH, half roughness)
			{
				half a = NoH * roughness;
				half k = roughness / (1.0 - NoH * NoH + a * a);
				return k * k * (1.0 / UNITY_PI);
			}
			
			float V_SmithGGXCorrelatedFast(half NoV, half NoL, half roughness) {
				half a = roughness;
				float GGXV = NoL * (NoV * (1.0 - a) + a);
				float GGXL = NoV * (NoL * (1.0 - a) + a);
				return 0.5 / (GGXV + GGXL);
			}
			
			float V_SmithGGXCorrelated(half NoV, half NoL, half roughness)
			{
				#ifdef SHADER_API_MOBILE
				return V_SmithGGXCorrelatedFast(NoV, NoL, roughness);
				#else
				half a2 = roughness * roughness;
				float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
				float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
				return 0.5 / (GGXV + GGXL);
				#endif
			}
			
			half V_Kelemen(half LoH)
			{
				// Kelemen 2001, "A Microfacet Based Coupled Specular-Matte BRDF Model with Importance Sampling"
				return saturate(0.25 / (LoH * LoH));
			}
			
			half _specularAntiAliasingVariance;
			half _specularAntiAliasingThreshold;
			float GSAA_Filament(float3 worldNormal, float perceptualRoughness)
			{
				// Kaplanyan 2016, "Stable specular highlights"
				// Tokuyoshi 2017, "Error Reduction and Simplification for Shading Anti-Aliasing"
				// Tokuyoshi and Kaplanyan 2019, "Improved Geometric Specular Antialiasing"
				
				// This implementation is meant for deferred rendering in the original paper but
				// we use it in forward rendering as well (as discussed in Tokuyoshi and Kaplanyan
				// 2019). The main reason is that the forward version requires an expensive transform
				// of the half vector by the tangent frame for every light. This is therefore an
				// approximation but it works well enough for our needs and provides an improvement
				// over our original implementation based on Vlachos 2015, "Advanced VR Rendering".
				
				float3 du = ddx(worldNormal);
				float3 dv = ddy(worldNormal);
				
				float variance = _specularAntiAliasingVariance * (dot(du, du) + dot(dv, dv));
				
				float roughness = perceptualRoughness * perceptualRoughness;
				float kernelRoughness = min(2.0 * variance, _specularAntiAliasingThreshold);
				float squareRoughness = saturate(roughness * roughness + kernelRoughness);
				
				return sqrt(sqrt(squareRoughness));
			}
			
			float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
			{
				// average energy
				float R0 = L0;
				
				// avg direction of incoming light
				float3 R1 = 0.5f * L1;
				
				// directional brightness
				float lenR1 = length(R1);
				
				// linear angle between normal and direction 0-1
				//float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
				//float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
				float q = dot(normalize(R1), n) * 0.5 + 0.5;
				q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
				
				// power for q
				// lerps from 1 (linear) to 3 (cubic) based on directionality
				float p = 1.0f + 2.0f * lenR1 / R0;
				
				// dynamic range constant
				// should vary between 4 (highly directional) and 0 (ambient)
				float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
				
				return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
			}
			
			float3 Unity_NormalReconstructZ(float2 In)
			{
				float reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
				float3 normalVector = float3(In.x, In.y, reconstructZ);
				return normalize(normalVector);
			}
			
			#ifdef DYNAMICLIGHTMAP_ON
			float3 getRealtimeLightmap(float2 uv, float3 worldNormal)
			{
				half4 bakedCol = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, uv);
				float3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);
				
				#ifdef DIRLIGHTMAP_COMBINED
				half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
				realtimeLightmap += DecodeDirectionalLightmap (realtimeLightmap, realtimeDirTex, worldNormal);
				#endif
				
				return realtimeLightmap;
			}
			#endif
			
			half3 GetSpecularHighlights(float3 worldNormal, half3 lightColor, float3 lightDirection, half3 f0, float3 viewDir, half clampedRoughness, half NoV, half3 energyCompensation)
			{
				float3 halfVector = Unity_SafeNormalize(lightDirection + viewDir);
				
				half NoH = saturate(dot(worldNormal, halfVector));
				half NoL = saturate(dot(worldNormal, lightDirection));
				half LoH = saturate(dot(lightDirection, halfVector));
				
				half3 F = F_Schlick(LoH, f0);
				half D = D_GGX(NoH, clampedRoughness);
				half V = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
				
				#ifndef SHADER_API_MOBILE
				F *= energyCompensation;
				#endif
				
				return max(0, (D * V) * F) * lightColor * NoL;
			}
			
			float Unity_Dither(float In, float2 ScreenPosition)
			{
				float2 uv = ScreenPosition * _ScreenParams.xy;
				const half4 DITHER_THRESHOLDS[4] =
				{
					half4(1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0),
					half4(13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0),
					half4(4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0),
					half4(16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0)
				};
				
				return In - DITHER_THRESHOLDS[uint(uv.x) % 4][uint(uv.y) % 4];
			}
			
			void AACutout(inout half alpha, half cutoff)
			{
				alpha = (alpha - cutoff) / max(fwidth(alpha), 0.0001) + 0.5;
			}
			
			void FlipBTN(uint facing, inout float3 worldNormal, inout float3 bitangent, inout float3 tangent)
			{
				#if !defined(LIGHTMAP_ON)
				UNITY_FLATTEN
				if (!facing)
				{
					worldNormal *= -1.0;
					bitangent *= -1.0;
					tangent *= -1.0;
				}
				#endif
			}
			
			void TangentToWorldNormal(float3 normalTS, inout float3 normalWS, inout float3 tangent, inout float3 bitangent)
			{
				normalWS = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normalWS);
				tangent = normalize(cross(normalWS, bitangent));
				bitangent = normalize(cross(normalWS, tangent));
			}
			
			half NormalDotViewDir(float3 normalWS, float3 viewDir)
			{
				return abs(dot(normalWS, viewDir)) + 1e-5f;
			}
			
			half3 GetF0(half reflectance, half metallic, half3 albedo)
			{
				return 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo * metallic;
			}
			
			struct LightData
			{
				half3 Color;
				float3 Direction;
				half NoL;
				half LoH;
				half NoH;
				float3 HalfVector;
				half3 FinalColor;
				half3 Specular;
				half Attenuation;
			};
			static LightData lightData;
			
			half3 MainLightSpecular(LightData lightData, half NoV, half clampedRoughness, half3 f0)
			{
				half3 F = F_Schlick(lightData.LoH, f0) * shaderData.energyCompensation;
				half D = D_GGX(lightData.NoH, clampedRoughness);
				half V = V_SmithGGXCorrelated(NoV, lightData.NoL, clampedRoughness);
				
				return max(0.0, (D * V) * F) * lightData.FinalColor;
			}
			
			#if defined(UNITY_PASS_FORWARDBASE) && defined(DIRECTIONAL) && !(defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING))
			#define BRANCH_DIRECTIONAL
			
			#ifdef _SPECULAR_HIGHLIGHTS_OFF
			#undef BRANCH_DIRECTIONAL
			#endif
			#endif
			
			void InitializeLightData(inout LightData lightData, float3 normalWS, float3 viewDir, half NoV, half clampedRoughness, half perceptualRoughness, half3 f0)
			{
				#ifdef USING_LIGHT_MULTI_COMPILE
				#ifdef BRANCH_DIRECTIONAL
				UNITY_BRANCH
				if (any(_WorldSpaceLightPos0.xyz))
				{
					//printf("directional branch");
					#endif
					lightData.Direction = normalize(UnityWorldSpaceLightDir(vertexData.worldPos));
					lightData.HalfVector = Unity_SafeNormalize(lightData.Direction + viewDir);
					lightData.NoL = saturate(dot(normalWS, lightData.Direction));
					lightData.LoH = saturate(dot(lightData.Direction, lightData.HalfVector));
					lightData.NoH = saturate(dot(normalWS, lightData.HalfVector));
					
					UNITY_LIGHT_ATTENUATION(lightAttenuation, vertexData, vertexData.worldPos.xyz);
					lightData.Attenuation = lightAttenuation;
					lightData.Color = lightAttenuation * _LightColor0.rgb;
					lightData.FinalColor = (lightData.NoL * lightData.Color);
					
					#ifndef SHADER_API_MOBILE
					lightData.FinalColor *= Fd_Burley(perceptualRoughness, NoV, lightData.NoL, lightData.LoH);
					#endif
					
					#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
					float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
					lightData.FinalColor *= UnityComputeForwardShadows(lightmapUV, vertexData.worldPos, vertexData.screenPos);
					#endif
					
					lightData.Specular = MainLightSpecular(lightData, NoV, clampedRoughness, f0);
					#ifdef BRANCH_DIRECTIONAL
				}
				else
				{
					lightData = (LightData)0;
				}
				#endif
				#else
				lightData = (LightData)0;
				#endif
			}
			
			#if defined(VERTEXLIGHT_ON) && defined(UNITY_PASS_FORWARDBASE)
			void NonImportantLightsPerPixel(inout half3 lightColor, inout half3 directSpecular, float3 positionWS, float3 normalWS, float3 viewDir, half NoV, half3 f0, half clampedRoughness)
			{
				float4 toLightX = unity_4LightPosX0 - positionWS.x;
				float4 toLightY = unity_4LightPosY0 - positionWS.y;
				float4 toLightZ = unity_4LightPosZ0 - positionWS.z;
				
				float4 lengthSq = 0.0;
				lengthSq += toLightX * toLightX;
				lengthSq += toLightY * toLightY;
				lengthSq += toLightZ * toLightZ;
				
				float4 attenuation = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
				float4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25.0));
				attenuation = min(attenuation, atten2 * atten2);
				
				[unroll(4)]
				for(uint i = 0; i < 4; i++)
				{
					UNITY_BRANCH
					if (attenuation[i] > 0.0)
					{
						float3 direction = normalize(float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]) - positionWS);
						half NoL = saturate(dot(normalWS, direction));
						half3 color = NoL * attenuation[i] * unity_LightColor[i];
						lightColor += color;
						
						#ifndef _SPECULAR_HIGHLIGHTS_OFF
						float3 halfVector = Unity_SafeNormalize(direction + viewDir);
						half vNoH = saturate(dot(normalWS, halfVector));
						half vLoH = saturate(dot(direction, halfVector));
						
						half3 Fv = F_Schlick(vLoH, f0);
						half Dv = D_GGX(vNoH, clampedRoughness);
						half Vv = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
						directSpecular += max(0.0, (Dv * Vv) * Fv) * color;
						#endif
					}
				}
			}
			#endif
			
			half3 GetReflections(float3 normalWS, float3 positionWS, float3 viewDir, half3 f0, half roughness, half NoV, half3 indirectDiffuse)
			{
				half3 indirectSpecular = 0;
				#if defined(UNITY_PASS_FORWARDBASE)
				
				float3 reflDir = reflect(-viewDir, normalWS);
				reflDir = lerp(reflDir, normalWS, roughness * roughness);
				
				Unity_GlossyEnvironmentData envData;
				envData.roughness = surf.perceptualRoughness;
				envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
				
				half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
				indirectSpecular = probe0;
				
				#if defined(UNITY_SPECCUBE_BLENDING) && !defined(SHADER_API_MOBILE)
				UNITY_BRANCH
				if (unity_SpecCube0_BoxMin.w < 0.99999)
				{
					envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
					float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
					indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
				}
				#endif
				
				float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
				float2 dfg = shaderData.DFGLut;
				#ifdef LIGHTMAP_ANY
				dfg.x *= lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), 1.0);
				#endif
				indirectSpecular = indirectSpecular * horizon * horizon * shaderData.energyCompensation * EnvBRDFMultiscatter(dfg, f0);
				indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);
				
				#endif
				
				return indirectSpecular;
			}
			
			half3 GetLightProbes(float3 normalWS, float3 positionWS)
			{
				half3 indirectDiffuse = 0;
				#ifndef LIGHTMAP_ANY
				#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				UNITY_BRANCH
				if (unity_ProbeVolumeParams.x == 1.0)
				{
					indirectDiffuse = SHEvalLinearL0L1_SampleProbeVolume(float4(normalWS, 1.0), positionWS);
				}
				else
				{
					#endif
					#ifdef _NONLINEAR_LIGHTPROBESH
					float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					indirectDiffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normalWS);
					indirectDiffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normalWS);
					indirectDiffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normalWS);
					#else
					indirectDiffuse = ShadeSH9(float4(normalWS, 1.0));
					#endif
					#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				}
				#endif
				#endif
				return indirectDiffuse;
			}
			
			#ifndef BAKERY_INCLUDED
			#define BAKERY_INCLUDED
			
			Texture2D _RNM0, _RNM1, _RNM2;
			SamplerState sampler_RNM0, sampler_RNM1, sampler_RNM2;
			float4 _RNM0_TexelSize;
			
			#if !defined(SHADER_API_MOBILE)
			#define BAKERY_SHNONLINEAR
			#endif
			
			void BakeryRNMLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalTS, float3 viewDirTS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_RNM
				normalTS.g *= -1;
				float3 rnm0 = DecodeLightmap(_RNM0.Sample(sampler_RNM0, lightmapUV));
				float3 rnm1 = DecodeLightmap(_RNM1.Sample(sampler_RNM1, lightmapUV));
				float3 rnm2 = DecodeLightmap(_RNM2.Sample(sampler_RNM2, lightmapUV));
				
				const float3 rnmBasis0 = float3(0.816496580927726f, 0.0f, 0.5773502691896258f);
				const float3 rnmBasis1 = float3(-0.4082482904638631f, 0.7071067811865475f, 0.5773502691896258f);
				const float3 rnmBasis2 = float3(-0.4082482904638631f, -0.7071067811865475f, 0.5773502691896258f);
				
				lightMap =    saturate(dot(rnmBasis0, normalTS)) * rnm0
				+ saturate(dot(rnmBasis1, normalTS)) * rnm1
				+ saturate(dot(rnmBasis2, normalTS)) * rnm2;
				
				#ifdef _BAKED_SPECULAR
				float3 viewDirT = -normalize(viewDirTS);
				float3 dominantDirT = rnmBasis0 * dot(rnm0, GRAYSCALE) +
				rnmBasis1 * dot(rnm1, GRAYSCALE) +
				rnmBasis2 * dot(rnm2, GRAYSCALE);
				
				float3 dominantDirTN = normalize(dominantDirT);
				half3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
				saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
				saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;
				
				half3 halfDir = Unity_SafeNormalize(dominantDirTN - viewDirT);
				half NoH = saturate(dot(normalTS, halfDir));
				half spec = D_GGX(NoH, roughness);
				directSpecular += spec * specColor;
				#endif
				
				#endif
			}
			
			void BakerySHLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalWS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_SH
				
				half3 L0 = lightMap;
				float3 nL1x = _RNM0.Sample(sampler_RNM0, lightmapUV) * 2.0 - 1.0;
				float3 nL1y = _RNM1.Sample(sampler_RNM1, lightmapUV) * 2.0 - 1.0;
				float3 nL1z = _RNM2.Sample(sampler_RNM2, lightmapUV) * 2.0 - 1.0;
				float3 L1x = nL1x * L0 * 2.0;
				float3 L1y = nL1y * L0 * 2.0;
				float3 L1z = nL1z * L0 * 2.0;
				
				#ifdef BAKERY_SHNONLINEAR
				float lumaL0 = dot(L0, float(1));
				float lumaL1x = dot(L1x, float(1));
				float lumaL1y = dot(L1y, float(1));
				float lumaL1z = dot(L1z, float(1));
				float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWS);
				
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				float regularLumaSH = dot(lightMap, 1.0);
				lightMap *= lerp(1.0, lumaSH / regularLumaSH, saturate(regularLumaSH * 16.0));
				#else
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				#endif
				
				#ifdef _BAKED_SPECULAR
				float3 dominantDir = float3(dot(nL1x, GRAYSCALE), dot(nL1y, GRAYSCALE), dot(nL1z, GRAYSCALE));
				float3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + viewDir);
				half NoH = saturate(dot(normalWS, halfDir));
				half spec = D_GGX(NoH, roughness);
				half3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
				dominantDir = normalize(dominantDir);
				
				directSpecular += max(spec * sh, 0.0);
				#endif
				
				#endif
			}
			#endif
			
			void InitializeShaderData(uint facing)
			{
				shaderData.facing = facing;
				FlipBTN(facing, vertexData.worldNormal, vertexData.bitangent, vertexData.tangent);
				
				#ifdef _GEOMETRICSPECULAR_AA
				surf.perceptualRoughness = GSAA_Filament(vertexData.worldNormal, surf.perceptualRoughness);
				#endif
				shaderData.worldNormal = vertexData.worldNormal;
				shaderData.bitangent = vertexData.bitangent;
				shaderData.tangent = vertexData.tangent;
				
				surf.tangentNormal.g *= -1;
				TangentToWorldNormal(surf.tangentNormal, shaderData.worldNormal, shaderData.tangent, shaderData.bitangent);
				
				shaderData.viewDir = normalize(UnityWorldSpaceViewDir(vertexData.worldPos));
				shaderData.NoV = NormalDotViewDir(shaderData.worldNormal, shaderData.viewDir);
				shaderData.f0 = GetF0(surf.reflectance, surf.metallic, surf.albedo.rgb);
				shaderData.DFGLut = SampleDFG(shaderData.NoV, surf.perceptualRoughness).rg;
				shaderData.energyCompensation = EnvBRDFEnergyCompensation(shaderData.DFGLut, shaderData.f0);
			}
			
			#ifndef POM_INCLUDED
			#define POM_INCLUDED
			// com.unity.render-pipelines.core copyright © 2020 Unity Technologies ApS
			// Licensed under the Unity Companion License for Unity-dependent projects--see https://unity3d.com/legal/licenses/Unity_Companion_License.
			
			// This is implementation of parallax occlusion mapping (POM)
			// This function require that the caller define a callback for the height sampling name ComputePerPixelHeightDisplacement
			// A PerPixelHeightDisplacementParam is used to provide all data necessary to calculate the heights to ComputePerPixelHeightDisplacement it doesn't need to be
			// visible by the POM algorithm.
			// This function is compatible with tiled uv.
			// it return the offset to apply to the UVSet provide in PerPixelHeightDisplacementParam
			// viewDirTS is view vector in texture space matching the UVSet
			// ref: https://www.gamedev.net/resources/_/technical/graphics-programming-and-theory/a-closer-look-at-parallax-occlusion-mapping-r3262
			
			#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
			#define FLT_MIN         1.175494351e-38 // Minimum representable positive floating-point number
			#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number
			#define INV_HALF_PI     0.636619772367
			
			float _ParallaxFadingMip;
			float _MinSteps;
			float _MaxSteps;
			
			struct PerPixelHeightDisplacementParam
			{
				float2 uv;
				SamplerState sampl;
				Texture2D height;
			};
			
			float2 GetMinUvSize(float2 baseUV, float4 texelSize)
			{
				float2 minUvSize = float2(FLT_MAX, FLT_MAX);
				
				minUvSize = min(baseUV * texelSize.zw, minUvSize);
				
				return minUvSize;
			}
			
			float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
			{
				// Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
				// Tiling is automatically handled correctly here.
				return param.height.SampleLevel(param.sampl, param.uv + texOffsetCurrent, lod).r;
			}
			
			float2 ParallaxOcclusionMapping(float lod, float lodThreshold, uint numSteps, float uvSpaceScale, float3 viewDirTS, PerPixelHeightDisplacementParam ppdParam, out float outHeight)
			{
				// Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
				float stepSize = 1.0 / (float)numSteps;
				
				// View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
				// The length of viewDirTS vector determines the furthest amount of displacement:
				// float parallaxLimit = -length(viewDirTS.xy) / viewDirTS.z;
				// float2 parallaxDir = normalize(Out.viewDirTS.xy);
				// float2 parallaxMaxOffsetTS = parallaxDir * parallaxLimit;
				// Above code simplify to
				float2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z);
				float2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;
				
				// Do a first step before the loop to init all value correctly
				float2 texOffsetCurrent = float2(0.0, 0.0);
				float prevHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				texOffsetCurrent += texOffsetPerStep;
				float currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				float rayHeight = 1.0 - stepSize; // Start at top less one sample
				
				// Linear search
				for (uint stepIndex = 0; stepIndex < numSteps; ++stepIndex)
				{
					// Have we found a height below our ray height ? then we have an intersection
					if (currHeight > rayHeight)
					break; // end the loop
					
					prevHeight = currHeight;
					rayHeight -= stepSize;
					texOffsetCurrent += texOffsetPerStep;
					
					// Sample height map which in this case is stored in the alpha channel of the normal map:
					currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				}
				
				// Found below and above points, now perform line interesection (ray) with piecewise linear heightfield approximation
				
				// Refine the search with secant method
				#define POM_SECANT_METHOD 1
				#if POM_SECANT_METHOD
				
				float pt0 = rayHeight + stepSize;
				float pt1 = rayHeight;
				float delta0 = pt0 - prevHeight;
				float delta1 = pt1 - currHeight;
				
				float delta;
				float2 offset;
				
				// Secant method to affine the search
				// Ref: Faster Relief Mapping Using the Secant Method - Eric Risser
				[unroll]
				for (uint i = 0; i < 3; ++i)
				{
					// intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
					float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
					// Retrieve offset require to find this intersectionHeight
					offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
					
					currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
					
					delta = intersectionHeight - currHeight;
					
					if (abs(delta) <= 0.01)
					break;
					
					// intersectionHeight < currHeight => new lower bounds
					if (delta < 0.0)
					{
						delta1 = delta;
						pt1 = intersectionHeight;
					}
					else
					{
						delta0 = delta;
						pt0 = intersectionHeight;
					}
				}
				
				#else // regular POM intersection
				
				//float pt0 = rayHeight + stepSize;
				//float pt1 = rayHeight;
				//float delta0 = pt0 - prevHeight;
				//float delta1 = pt1 - currHeight;
				//float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
				//float2 offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
				
				// A bit more optimize
				float delta0 = currHeight - rayHeight;
				float delta1 = (rayHeight + stepSize) - prevHeight;
				float ratio = delta0 / (delta0 + delta1);
				float2 offset = texOffsetCurrent - ratio * texOffsetPerStep;
				
				currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
				
				#endif
				
				outHeight = currHeight;
				
				// Fade the effect with lod (allow to avoid pop when switching to a discrete LOD mesh)
				offset *= (1.0 - saturate(lod - lodThreshold));
				
				return offset;
			}
			
			float2 ParallaxOcclusionMappingUVOffset(float2 uv, float scale, float3 viewDirTS, Texture2D tex, SamplerState sampl, float4 texelSize)
			{
				float3 viewDirUV = normalize(float3(viewDirTS.xy * scale, viewDirTS.z));
				
				float unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);
				uint numSteps = (uint)lerp(_MinSteps, _MaxSteps, unitAngle);
				
				float2 minUvSize = GetMinUvSize(uv, texelSize);
				float lod = ComputeTextureLOD(minUvSize);
				
				PerPixelHeightDisplacementParam ppdParam;
				
				ppdParam.uv = uv;
				ppdParam.height = tex;
				ppdParam.sampl = sampl;
				
				float height = 0;
				float2 offset = ParallaxOcclusionMapping(lod, _ParallaxFadingMip, numSteps, scale, viewDirUV, ppdParam, height);
				
				return offset;
			}
			#endif
			void InitializeSurfaceData()
			{
				float2 mainUV = TRANSFORM_TEX(vertexData.uv[0], _MainTex);
				#ifdef _PARALLAX_MAP
				shaderData.parallaxUVOffset = ParallaxOcclusionMappingUVOffset(mainUV, _Parallax, vertexData.viewDirTS, _ParallaxMap, sampler_MainTex, _ParallaxMap_TexelSize);
				#endif
				mainUV += shaderData.parallaxUVOffset;
				
				half4 mainTex = _MainTex.Sample(sampler_MainTex, mainUV);
				mainTex.rgb = lerp(dot(mainTex.rgb, GRAYSCALE), mainTex.rgb, _AlbedoSaturation);
				mainTex *= _Color;
				surf.albedo = mainTex.rgb;
				surf.alpha = mainTex.a;
				
				#ifdef _NORMAL_MAP
				half4 normalMap = _BumpMap.Sample(sampler_BumpMap, mainUV);
				surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
				#endif
				
				#ifdef _MASK_MAP
				half4 maskMap = _MetallicGlossMap.Sample(sampler_MetallicGlossMap, mainUV);
				surf.perceptualRoughness = 1.0 - (RemapMinMax(maskMap.a, _GlossinessMinMax.x, _GlossinessMinMax.y));
				surf.metallic = RemapMinMax(maskMap.r, _MetallicMinMax.x, _MetallicMinMax.y);
				surf.occlusion = lerp(1.0, maskMap.g, _Occlusion);
				#else
				surf.perceptualRoughness = 1.0 - _Glossiness;
				surf.metallic = _Metallic;
				#endif
				
				surf.reflectance = _Reflectance;
			}
			
			VertexData Vertex(VertexInput v)
			{
				VertexData o;
				UNITY_INITIALIZE_OUTPUT(VertexData, o);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				#ifdef UNITY_PASS_META
				o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
				#else
				#if !defined(UNITY_PASS_SHADOWCASTER)
				o.pos = UnityObjectToClipPos(v.vertex);
				#endif
				#endif
				
				o.uv[0].xy = v.uv0.xy;
				o.uv[1].xy = v.uv1.xy;
				o.uv[2].xy = v.uv2.xy;
				o.uv[3].xy = v.uv3.xy;
				
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
				o.bitangent = cross(o.tangent, o.worldNormal) * v.tangent.w;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				o.vertexLight = Shade4PointLights
				(
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb,
				unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, o.worldPos,  o.worldNormal
				);
				#endif
				
				#if defined(REQUIRE_VIEWDIRTS)
				TANGENT_SPACE_ROTATION;
				o.viewDirTS = mul(rotation, ObjSpaceViewDir(v.vertex));
				#endif
				
				#ifdef UNITY_PASS_SHADOWCASTER
				o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
				o.pos = UnityApplyLinearShadowBias(o.pos);
				TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
				#else
				UNITY_TRANSFER_SHADOW(o, o.uv[1].xy);
				UNITY_TRANSFER_FOG(o,o.pos);
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				o.screenPos = ComputeScreenPos(o.pos);
				#endif
				
				return o;
			}
			
			half4 Fragment(VertexData input, uint facing : SV_IsFrontFace) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(vertexData)
				vertexData = input;
				
				#if defined(LOD_FADE_CROSSFADE)
				UnityApplyDitherCrossFade(vertexData.pos);
				#endif
				
				InitializeDefaultSurfaceData();
				InitializeSurfaceData();
				
				shaderData = (ShaderData)0;
				InitializeShaderData(facing);
				
				#if defined(UNITY_PASS_SHADOWCASTER)
				#if defined(_MODE_CUTOUT)
				if (surf.alpha < _Cutoff) discard;
				#endif
				
				#ifdef _ALPHAPREMULTIPLY_ON
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAPREMULTIPLY_ON) || defined(_MODE_FADE)
				half dither = Unity_Dither(surf.alpha, input.pos.xy);
				if (dither < 0.0) discard;
				#endif
				
				SHADOW_CASTER_FRAGMENT(vertexData);
				#else
				
				half3 indirectSpecular = 0.0;
				half3 directSpecular = 0.0;
				half3 otherSpecular = 0.0;
				
				half roughness = surf.perceptualRoughness * surf.perceptualRoughness;
				half clampedRoughness = max(roughness, 0.002);
				
				InitializeLightData(lightData, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, clampedRoughness, surf.perceptualRoughness, shaderData.f0);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				lightData.FinalColor += vertexData.vertexLight;
				#endif
				
				#if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
				NonImportantLightsPerPixel(lightData.FinalColor, directSpecular, vertexData.worldPos, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, shaderData.f0, clampedRoughness);
				#endif
				
				half3 indirectDiffuse;
				#if defined(LIGHTMAP_ANY)
				
				float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
				half4 bakedColorTex = SampleBicubic(unity_Lightmap, samplerunity_Lightmap, lightmapUV);
				half3 lightMap = DecodeLightmap(bakedColorTex);
				
				#ifdef BAKERY_RNM
				BakeryRNMLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, surf.tangentNormal, vertexData.viewDirTS, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#ifdef BAKERY_SH
				BakerySHLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, shaderData.worldNormal, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#if defined(DIRLIGHTMAP_COMBINED)
				float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
				lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, shaderData.worldNormal);
				#endif
				
				#if defined(DYNAMICLIGHTMAP_ON)
				float2 realtimeLightmapUV = vertexData.uv[2] * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				float3 realtimeLightMap = getRealtimeLightmap(realtimeLightmapUV, shaderData.worldNormal);
				lightMap += realtimeLightMap;
				#endif
				
				#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
				lightData.FinalColor = 0.0;
				lightData.Specular = 0.0;
				lightMap = SubtractMainLightWithRealtimeAttenuationFromLightmap (lightMap, lightData.Attenuation, bakedColorTex, shaderData.worldNormal);
				#endif
				
				indirectDiffuse = lightMap;
				#else
				indirectDiffuse = GetLightProbes(shaderData.worldNormal, vertexData.worldPos.xyz);
				#endif
				indirectDiffuse = max(0.0, indirectDiffuse);
				
				#if !defined(_SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
				directSpecular += lightData.Specular;
				#endif
				
				#if defined(_BAKED_SPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERYLM_ENABLED)
				{
					float3 bakedDominantDirection = 1.0;
					half3 bakedSpecularColor = 0.0;
					
					#if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
					bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
					bakedSpecularColor = indirectDiffuse;
					#endif
					
					#ifndef LIGHTMAP_ANY
					bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
					#endif
					
					bakedDominantDirection = normalize(bakedDominantDirection);
					directSpecular += GetSpecularHighlights(shaderData.worldNormal, bakedSpecularColor, bakedDominantDirection, shaderData.f0, shaderData.viewDir, clampedRoughness, shaderData.NoV, shaderData.energyCompensation);
				}
				#endif
				
				#if !defined(_REFLECTIONS_OFF) && defined(UNITY_PASS_FORWARDBASE)
				indirectSpecular += GetReflections(shaderData.worldNormal, vertexData.worldPos.xyz, shaderData.viewDir, shaderData.f0, roughness, shaderData.NoV, indirectDiffuse);
				#endif
				
				otherSpecular *= EnvBRDFMultiscatter(shaderData.DFGLut, shaderData.f0) * shaderData.energyCompensation;
				
				#if defined(_ALPHAPREMULTIPLY_ON)
				surf.albedo.rgb *= surf.alpha;
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAMODULATE_ON)
				surf.albedo.rgb = lerp(1.0, surf.albedo.rgb, surf.alpha);
				#endif
				
				#if defined(_MODE_CUTOUT)
				AACutout(surf.alpha, _Cutoff);
				#endif
				
				half4 finalColor = 0;
				//final color
				finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + (lightData.FinalColor))
				+ indirectSpecular + (directSpecular * UNITY_PI) + otherSpecular + surf.emission, surf.alpha);
				
				#ifdef UNITY_PASS_META
				UnityMetaInput metaInput;
				UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
				metaInput.Emission = surf.emission;
				metaInput.Albedo = surf.albedo;
				
				return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
				#endif
				
				UNITY_APPLY_FOG(vertexData.fogCoord, finalColor);
				
				return finalColor;
				#endif
			}
			
			ENDCG
		}
		Pass
		{
			Name "Meta"
			Tags
			{
				"LightMode"="Meta"
			}
			Cull Off
			
			CGPROGRAM
			#pragma target 4.5
			#pragma vertex Vertex
			#pragma fragment Fragment
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#include "UnityMetaPass.cginc"
			//meta keywords
			
			#pragma shader_feature_local _ _MODE_CUTOUT _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				float4 tangent : TANGENT;
				float4 color : COLOR;
				uint vertexId : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
			#define REQUIRE_SCREENPOS
			#endif
			
			#if defined(BAKERY_RNM) && defined(_BAKED_SPECULAR)
			#define REQUIRE_VIEWDIRTS
			#endif
			
			#ifdef _PARALLAX_MAP
			#define REQUIRE_VIEWDIRTS
			#endif
			
			struct VertexData
			{
				float4 pos : SV_POSITION;
				float2 uv[4] : TEXCOORD0;
				
				float3 tangent : TEXCOORD4;
				float3 bitangent : TEXCOORD5;
				float3 worldNormal : TEXCOORD6;
				float4 worldPos : TEXCOORD7;
				
				#ifdef REQUIRE_VIEWDIRTS
				float3 viewDirTS : TEXCOORD8;
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				float4 screenPos : TEXCOORD9;
				#endif
				
				#if !defined(UNITY_PASS_SHADOWCASTER)
				UNITY_FOG_COORDS(10)
				UNITY_SHADOW_COORDS(11)
				#endif
				
				#if defined(VERTEXLIGHT_ON)
				half3 vertexLight : TEXCOORD12;
				#endif
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			static VertexData vertexData;
			
			half _Cutoff;
			
			Texture2D _MainTex;
			SamplerState sampler_MainTex;
			float4 _MainTex_ST;
			half4 _Color;
			
			Texture2D _MetallicGlossMap;
			SamplerState sampler_MetallicGlossMap;
			
			Texture2D _BumpMap;
			SamplerState sampler_BumpMap;
			half _BumpScale;
			
			half _Glossiness;
			half _Metallic;
			half2 _GlossinessMinMax;
			half2 _MetallicMinMax;
			half _Occlusion;
			half _Reflectance;
			half _AlbedoSaturation;
			
			Texture2D _ParallaxMap;
			half _Parallax;
			float4 _ParallaxMap_TexelSize;
			
			struct SurfaceData
			{
				half3 albedo;
				half3 tangentNormal;
				half3 emission;
				half metallic;
				half perceptualRoughness;
				half occlusion;
				half reflectance;
				half alpha;
			};
			
			static SurfaceData surf;
			
			void InitializeDefaultSurfaceData()
			{
				surf.albedo = 1.0;
				surf.tangentNormal = half3(0,0,1);
				surf.emission = 0.0;
				surf.metallic = 0.0;
				surf.perceptualRoughness = 0.0;
				surf.occlusion = 1.0;
				surf.reflectance = 0.5;
				surf.alpha = 1.0;
			}
			struct ShaderData
			{
				// probably just gonna put here everything if needed
				float3 worldNormal;
				float3 worldNormalUnmodified;
				float3 bitangent;
				float3 tangent;
				half NoV;
				float3 viewDir;
				float2 parallaxUVOffset;
				float2 DFGLut;
				half3 f0;
				half3 energyCompensation;
				uint facing;
			};
			
			static ShaderData shaderData;
			
			// Partially taken from Google Filament, Xiexe, Catlike Coding and Unity
			// https://google.github.io/filament/Filament.html
			// https://github.com/Xiexe/Unity-Lit-Shader-Templates
			// https://catlikecoding.com/
			
			#define GRAYSCALE float3(0.2125, 0.7154, 0.0721)
			#define TAU float(6.28318530718)
			#define glsl_mod(x,y) (((x)-(y)*floor((x)/(y))))
			
			#ifndef BICUBIC_SAMPLING_INCLUDED
			#define BICUBIC_SAMPLING_INCLUDED
			#if defined(SHADER_API_MOBILE)
			#undef BICUBIC_LIGHTMAP
			#endif
			
			// https://ndotl.wordpress.com/2018/08/29/baking-artifact-free-lightmaps
			// bicubicw0, bicubicw1, bicubicw2, and bicubicw3 are the four cubic B-spline basis functions
			float bicubicw0(float a)
			{
				//    return (1.0f/6.0f)*(-a*a*a + 3.0f*a*a - 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);   // optimized
			}
			
			float bicubicw1(float a)
			{
				//    return (1.0f/6.0f)*(3.0f*a*a*a - 6.0f*a*a + 4.0f);
				return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
			}
			
			float bicubicw2(float a)
			{
				//    return (1.0f/6.0f)*(-3.0f*a*a*a + 3.0f*a*a + 3.0f*a + 1.0f);
				return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
			}
			
			float bicubicw3(float a)
			{
				return (1.0f/6.0f)*(a*a*a);
			}
			
			// bicubicg0 and bicubicg1 are the two amplitude functions
			float bicubicg0(float a)
			{
				return bicubicw0(a) + bicubicw1(a);
			}
			
			float bicubicg1(float a)
			{
				return bicubicw2(a) + bicubicw3(a);
			}
			
			// bicubich0 and bicubich1 are the two offset functions
			float bicubich0(float a)
			{
				// note +0.5 offset to compensate for CUDA linear filtering convention
				return -1.0f + bicubicw1(a) / (bicubicw0(a) + bicubicw1(a)) + 0.5f;
			}
			
			float bicubich1(float a)
			{
				return 1.0f + bicubicw3(a) / (bicubicw2(a) + bicubicw3(a)) + 0.5f;
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float2 widthHeight, float2 texelSize)
			{
				#if defined(SHADER_API_MOBILE) || !defined(BICUBIC_LIGHTMAP)
				return t.Sample(s, uv);
				#else
				
				float2 xy = uv * widthHeight - 0.5;
				float2 pxy = floor(xy);
				float2 fxy = xy - pxy;
				
				// note: we could store these functions in a lookup table texture, but maths is cheap
				float bicubicg0x = bicubicg0(fxy.x);
				float bicubicg1x = bicubicg1(fxy.x);
				float bicubich0x = bicubich0(fxy.x);
				float bicubich1x = bicubich1(fxy.x);
				float bicubich0y = bicubich0(fxy.y);
				float bicubich1y = bicubich1(fxy.y);
				
				float4 t0 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich0y) * texelSize);
				float4 t1 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich0y) * texelSize);
				float4 t2 = bicubicg0x * t.Sample(s, float2(pxy.x + bicubich0x, pxy.y + bicubich1y) * texelSize);
				float4 t3 = bicubicg1x * t.Sample(s, float2(pxy.x + bicubich1x, pxy.y + bicubich1y) * texelSize);
				
				return bicubicg0(fxy.y) * (t0 + t1) + bicubicg1(fxy.y) * (t2 + t3);
				#endif
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float4 texelSize)
			{
				return SampleBicubic(t, s, uv, texelSize.zw, texelSize.xy);
			}
			
			half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv)
			{
				float2 widthHeight;
				t.GetDimensions(widthHeight.x, widthHeight.y);
				float2 texelSize = 1.0 / widthHeight;
				return SampleBicubic(t, s, uv, widthHeight, texelSize);
			}
			#endif
			
			#ifndef ENVIRONMENTBRDF_INCLUDED
			#define ENVIRONMENTBRDF_INCLUDED
			Texture2D _DFG;
			SamplerState sampler_DFG;
			
			half4 SampleDFG(half NoV, half perceptualRoughness)
			{
				return _DFG.Sample(sampler_DFG, float3(NoV, perceptualRoughness, 0));
			}
			
			half3 EnvBRDF(half2 dfg, half3 f0)
			{
				return f0 * dfg.x + dfg.y;
			}
			
			half3 EnvBRDFMultiscatter(half2 dfg, half3 f0)
			{
				return lerp(dfg.xxx, dfg.yyy, f0);
			}
			
			half3 EnvBRDFEnergyCompensation(half2 dfg, half3 f0)
			{
				return 1.0 + f0 * (1.0 / dfg.y - 1.0);
			}
			
			#endif
			
			#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			#define LIGHTMAP_ANY
			#endif
			
			#ifdef LIGHTMAP_ANY
			#if defined(BAKERY_RNM) || defined(BAKERY_SH) || defined(BAKERY_VERTEXLM)
			#define BAKERYLM_ENABLED
			#undef DIRLIGHTMAP_COMBINED
			#endif
			#else
			#undef BAKERY_SH
			#undef BAKERY_RNM
			#endif
			
			#ifndef SHADER_API_MOBILE
			#define VERTEXLIGHT_PS
			#endif
			
			half RemapMinMax(half value, half remapMin, half remapMax)
			{
				return value * (remapMax - remapMin) + remapMin;
			}
			
			float pow5(float x)
			{
				float x2 = x * x;
				return x2 * x2 * x;
			}
			
			float sq(float x)
			{
				return x * x;
			}
			
			half3 F_Schlick(half u, half3 f0)
			{
				return f0 + (1.0 - f0) * pow(1.0 - u, 5.0);
			}
			
			float F_Schlick(float f0, float f90, float VoH)
			{
				return f0 + (f90 - f0) * pow5(1.0 - VoH);
			}
			
			// Input [0, 1] and output [0, PI/2]
			// 9 VALU
			float FastACosPos(float inX)
			{
				float x = abs(inX);
				float res = (0.0468878 * x + -0.203471) * x + 1.570796; // p(x)
				res *= sqrt(1.0 - x);
				
				return res;
			}
			
			float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias)
			{
				float2 ddx_ = scale * uvdx;
				float2 ddy_ = scale * uvdy;
				float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));
				
				return max(0.5 * log2(d) - bias, 0.0);
			}
			
			float ComputeTextureLOD(float2 uv)
			{
				float2 ddx_ = ddx(uv);
				float2 ddy_ = ddy(uv);
				
				return ComputeTextureLOD(ddx_, ddy_, 1.0, 0.0);
			}
			
			half Fd_Burley(half roughness, half NoV, half NoL, half LoH)
			{
				// Burley 2012, "Physically-Based Shading at Disney"
				half f90 = 0.5 + 2.0 * roughness * LoH * LoH;
				float lightScatter = F_Schlick(1.0, f90, NoL);
				float viewScatter  = F_Schlick(1.0, f90, NoV);
				return lightScatter * viewScatter;
			}
			
			float3 getBoxProjection (float3 direction, float3 position, float4 cubemapPosition, float3 boxMin, float3 boxMax)
			{
				#if defined(UNITY_SPECCUBE_BOX_PROJECTION)
				if (cubemapPosition.w > 0.0)
				{
					float3 factors = ((direction > 0.0 ? boxMax : boxMin) - position) / direction;
					float scalar = min(min(factors.x, factors.y), factors.z);
					direction = direction * scalar + (position - cubemapPosition.xyz);
				}
				#endif
				
				return direction;
			}
			
			half computeSpecularAO(half NoV, half ao, half roughness)
			{
				return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
			}
			
			half D_GGX(half NoH, half roughness)
			{
				half a = NoH * roughness;
				half k = roughness / (1.0 - NoH * NoH + a * a);
				return k * k * (1.0 / UNITY_PI);
			}
			
			float V_SmithGGXCorrelatedFast(half NoV, half NoL, half roughness) {
				half a = roughness;
				float GGXV = NoL * (NoV * (1.0 - a) + a);
				float GGXL = NoV * (NoL * (1.0 - a) + a);
				return 0.5 / (GGXV + GGXL);
			}
			
			float V_SmithGGXCorrelated(half NoV, half NoL, half roughness)
			{
				#ifdef SHADER_API_MOBILE
				return V_SmithGGXCorrelatedFast(NoV, NoL, roughness);
				#else
				half a2 = roughness * roughness;
				float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
				float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
				return 0.5 / (GGXV + GGXL);
				#endif
			}
			
			half V_Kelemen(half LoH)
			{
				// Kelemen 2001, "A Microfacet Based Coupled Specular-Matte BRDF Model with Importance Sampling"
				return saturate(0.25 / (LoH * LoH));
			}
			
			half _specularAntiAliasingVariance;
			half _specularAntiAliasingThreshold;
			float GSAA_Filament(float3 worldNormal, float perceptualRoughness)
			{
				// Kaplanyan 2016, "Stable specular highlights"
				// Tokuyoshi 2017, "Error Reduction and Simplification for Shading Anti-Aliasing"
				// Tokuyoshi and Kaplanyan 2019, "Improved Geometric Specular Antialiasing"
				
				// This implementation is meant for deferred rendering in the original paper but
				// we use it in forward rendering as well (as discussed in Tokuyoshi and Kaplanyan
				// 2019). The main reason is that the forward version requires an expensive transform
				// of the half vector by the tangent frame for every light. This is therefore an
				// approximation but it works well enough for our needs and provides an improvement
				// over our original implementation based on Vlachos 2015, "Advanced VR Rendering".
				
				float3 du = ddx(worldNormal);
				float3 dv = ddy(worldNormal);
				
				float variance = _specularAntiAliasingVariance * (dot(du, du) + dot(dv, dv));
				
				float roughness = perceptualRoughness * perceptualRoughness;
				float kernelRoughness = min(2.0 * variance, _specularAntiAliasingThreshold);
				float squareRoughness = saturate(roughness * roughness + kernelRoughness);
				
				return sqrt(sqrt(squareRoughness));
			}
			
			float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
			{
				// average energy
				float R0 = L0;
				
				// avg direction of incoming light
				float3 R1 = 0.5f * L1;
				
				// directional brightness
				float lenR1 = length(R1);
				
				// linear angle between normal and direction 0-1
				//float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
				//float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
				float q = dot(normalize(R1), n) * 0.5 + 0.5;
				q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
				
				// power for q
				// lerps from 1 (linear) to 3 (cubic) based on directionality
				float p = 1.0f + 2.0f * lenR1 / R0;
				
				// dynamic range constant
				// should vary between 4 (highly directional) and 0 (ambient)
				float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
				
				return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
			}
			
			float3 Unity_NormalReconstructZ(float2 In)
			{
				float reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
				float3 normalVector = float3(In.x, In.y, reconstructZ);
				return normalize(normalVector);
			}
			
			#ifdef DYNAMICLIGHTMAP_ON
			float3 getRealtimeLightmap(float2 uv, float3 worldNormal)
			{
				half4 bakedCol = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, uv);
				float3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);
				
				#ifdef DIRLIGHTMAP_COMBINED
				half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
				realtimeLightmap += DecodeDirectionalLightmap (realtimeLightmap, realtimeDirTex, worldNormal);
				#endif
				
				return realtimeLightmap;
			}
			#endif
			
			half3 GetSpecularHighlights(float3 worldNormal, half3 lightColor, float3 lightDirection, half3 f0, float3 viewDir, half clampedRoughness, half NoV, half3 energyCompensation)
			{
				float3 halfVector = Unity_SafeNormalize(lightDirection + viewDir);
				
				half NoH = saturate(dot(worldNormal, halfVector));
				half NoL = saturate(dot(worldNormal, lightDirection));
				half LoH = saturate(dot(lightDirection, halfVector));
				
				half3 F = F_Schlick(LoH, f0);
				half D = D_GGX(NoH, clampedRoughness);
				half V = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
				
				#ifndef SHADER_API_MOBILE
				F *= energyCompensation;
				#endif
				
				return max(0, (D * V) * F) * lightColor * NoL;
			}
			
			float Unity_Dither(float In, float2 ScreenPosition)
			{
				float2 uv = ScreenPosition * _ScreenParams.xy;
				const half4 DITHER_THRESHOLDS[4] =
				{
					half4(1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0),
					half4(13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0),
					half4(4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0),
					half4(16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0)
				};
				
				return In - DITHER_THRESHOLDS[uint(uv.x) % 4][uint(uv.y) % 4];
			}
			
			void AACutout(inout half alpha, half cutoff)
			{
				alpha = (alpha - cutoff) / max(fwidth(alpha), 0.0001) + 0.5;
			}
			
			void FlipBTN(uint facing, inout float3 worldNormal, inout float3 bitangent, inout float3 tangent)
			{
				#if !defined(LIGHTMAP_ON)
				UNITY_FLATTEN
				if (!facing)
				{
					worldNormal *= -1.0;
					bitangent *= -1.0;
					tangent *= -1.0;
				}
				#endif
			}
			
			void TangentToWorldNormal(float3 normalTS, inout float3 normalWS, inout float3 tangent, inout float3 bitangent)
			{
				normalWS = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normalWS);
				tangent = normalize(cross(normalWS, bitangent));
				bitangent = normalize(cross(normalWS, tangent));
			}
			
			half NormalDotViewDir(float3 normalWS, float3 viewDir)
			{
				return abs(dot(normalWS, viewDir)) + 1e-5f;
			}
			
			half3 GetF0(half reflectance, half metallic, half3 albedo)
			{
				return 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo * metallic;
			}
			
			struct LightData
			{
				half3 Color;
				float3 Direction;
				half NoL;
				half LoH;
				half NoH;
				float3 HalfVector;
				half3 FinalColor;
				half3 Specular;
				half Attenuation;
			};
			static LightData lightData;
			
			half3 MainLightSpecular(LightData lightData, half NoV, half clampedRoughness, half3 f0)
			{
				half3 F = F_Schlick(lightData.LoH, f0) * shaderData.energyCompensation;
				half D = D_GGX(lightData.NoH, clampedRoughness);
				half V = V_SmithGGXCorrelated(NoV, lightData.NoL, clampedRoughness);
				
				return max(0.0, (D * V) * F) * lightData.FinalColor;
			}
			
			#if defined(UNITY_PASS_FORWARDBASE) && defined(DIRECTIONAL) && !(defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING))
			#define BRANCH_DIRECTIONAL
			
			#ifdef _SPECULAR_HIGHLIGHTS_OFF
			#undef BRANCH_DIRECTIONAL
			#endif
			#endif
			
			void InitializeLightData(inout LightData lightData, float3 normalWS, float3 viewDir, half NoV, half clampedRoughness, half perceptualRoughness, half3 f0)
			{
				#ifdef USING_LIGHT_MULTI_COMPILE
				#ifdef BRANCH_DIRECTIONAL
				UNITY_BRANCH
				if (any(_WorldSpaceLightPos0.xyz))
				{
					//printf("directional branch");
					#endif
					lightData.Direction = normalize(UnityWorldSpaceLightDir(vertexData.worldPos));
					lightData.HalfVector = Unity_SafeNormalize(lightData.Direction + viewDir);
					lightData.NoL = saturate(dot(normalWS, lightData.Direction));
					lightData.LoH = saturate(dot(lightData.Direction, lightData.HalfVector));
					lightData.NoH = saturate(dot(normalWS, lightData.HalfVector));
					
					UNITY_LIGHT_ATTENUATION(lightAttenuation, vertexData, vertexData.worldPos.xyz);
					lightData.Attenuation = lightAttenuation;
					lightData.Color = lightAttenuation * _LightColor0.rgb;
					lightData.FinalColor = (lightData.NoL * lightData.Color);
					
					#ifndef SHADER_API_MOBILE
					lightData.FinalColor *= Fd_Burley(perceptualRoughness, NoV, lightData.NoL, lightData.LoH);
					#endif
					
					#if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
					float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
					lightData.FinalColor *= UnityComputeForwardShadows(lightmapUV, vertexData.worldPos, vertexData.screenPos);
					#endif
					
					lightData.Specular = MainLightSpecular(lightData, NoV, clampedRoughness, f0);
					#ifdef BRANCH_DIRECTIONAL
				}
				else
				{
					lightData = (LightData)0;
				}
				#endif
				#else
				lightData = (LightData)0;
				#endif
			}
			
			#if defined(VERTEXLIGHT_ON) && defined(UNITY_PASS_FORWARDBASE)
			void NonImportantLightsPerPixel(inout half3 lightColor, inout half3 directSpecular, float3 positionWS, float3 normalWS, float3 viewDir, half NoV, half3 f0, half clampedRoughness)
			{
				float4 toLightX = unity_4LightPosX0 - positionWS.x;
				float4 toLightY = unity_4LightPosY0 - positionWS.y;
				float4 toLightZ = unity_4LightPosZ0 - positionWS.z;
				
				float4 lengthSq = 0.0;
				lengthSq += toLightX * toLightX;
				lengthSq += toLightY * toLightY;
				lengthSq += toLightZ * toLightZ;
				
				float4 attenuation = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
				float4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25.0));
				attenuation = min(attenuation, atten2 * atten2);
				
				[unroll(4)]
				for(uint i = 0; i < 4; i++)
				{
					UNITY_BRANCH
					if (attenuation[i] > 0.0)
					{
						float3 direction = normalize(float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]) - positionWS);
						half NoL = saturate(dot(normalWS, direction));
						half3 color = NoL * attenuation[i] * unity_LightColor[i];
						lightColor += color;
						
						#ifndef _SPECULAR_HIGHLIGHTS_OFF
						float3 halfVector = Unity_SafeNormalize(direction + viewDir);
						half vNoH = saturate(dot(normalWS, halfVector));
						half vLoH = saturate(dot(direction, halfVector));
						
						half3 Fv = F_Schlick(vLoH, f0);
						half Dv = D_GGX(vNoH, clampedRoughness);
						half Vv = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
						directSpecular += max(0.0, (Dv * Vv) * Fv) * color;
						#endif
					}
				}
			}
			#endif
			
			half3 GetReflections(float3 normalWS, float3 positionWS, float3 viewDir, half3 f0, half roughness, half NoV, half3 indirectDiffuse)
			{
				half3 indirectSpecular = 0;
				#if defined(UNITY_PASS_FORWARDBASE)
				
				float3 reflDir = reflect(-viewDir, normalWS);
				reflDir = lerp(reflDir, normalWS, roughness * roughness);
				
				Unity_GlossyEnvironmentData envData;
				envData.roughness = surf.perceptualRoughness;
				envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
				
				half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
				indirectSpecular = probe0;
				
				#if defined(UNITY_SPECCUBE_BLENDING) && !defined(SHADER_API_MOBILE)
				UNITY_BRANCH
				if (unity_SpecCube0_BoxMin.w < 0.99999)
				{
					envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
					float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
					indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
				}
				#endif
				
				float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
				float2 dfg = shaderData.DFGLut;
				#ifdef LIGHTMAP_ANY
				dfg.x *= lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), 1.0);
				#endif
				indirectSpecular = indirectSpecular * horizon * horizon * shaderData.energyCompensation * EnvBRDFMultiscatter(dfg, f0);
				indirectSpecular *= computeSpecularAO(NoV, surf.occlusion, surf.perceptualRoughness * surf.perceptualRoughness);
				
				#endif
				
				return indirectSpecular;
			}
			
			half3 GetLightProbes(float3 normalWS, float3 positionWS)
			{
				half3 indirectDiffuse = 0;
				#ifndef LIGHTMAP_ANY
				#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				UNITY_BRANCH
				if (unity_ProbeVolumeParams.x == 1.0)
				{
					indirectDiffuse = SHEvalLinearL0L1_SampleProbeVolume(float4(normalWS, 1.0), positionWS);
				}
				else
				{
					#endif
					#ifdef _NONLINEAR_LIGHTPROBESH
					float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					indirectDiffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normalWS);
					indirectDiffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normalWS);
					indirectDiffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normalWS);
					#else
					indirectDiffuse = ShadeSH9(float4(normalWS, 1.0));
					#endif
					#if UNITY_LIGHT_PROBE_PROXY_VOLUME
				}
				#endif
				#endif
				return indirectDiffuse;
			}
			
			#ifndef BAKERY_INCLUDED
			#define BAKERY_INCLUDED
			
			Texture2D _RNM0, _RNM1, _RNM2;
			SamplerState sampler_RNM0, sampler_RNM1, sampler_RNM2;
			float4 _RNM0_TexelSize;
			
			#if !defined(SHADER_API_MOBILE)
			#define BAKERY_SHNONLINEAR
			#endif
			
			void BakeryRNMLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalTS, float3 viewDirTS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_RNM
				normalTS.g *= -1;
				float3 rnm0 = DecodeLightmap(_RNM0.Sample(sampler_RNM0, lightmapUV));
				float3 rnm1 = DecodeLightmap(_RNM1.Sample(sampler_RNM1, lightmapUV));
				float3 rnm2 = DecodeLightmap(_RNM2.Sample(sampler_RNM2, lightmapUV));
				
				const float3 rnmBasis0 = float3(0.816496580927726f, 0.0f, 0.5773502691896258f);
				const float3 rnmBasis1 = float3(-0.4082482904638631f, 0.7071067811865475f, 0.5773502691896258f);
				const float3 rnmBasis2 = float3(-0.4082482904638631f, -0.7071067811865475f, 0.5773502691896258f);
				
				lightMap =    saturate(dot(rnmBasis0, normalTS)) * rnm0
				+ saturate(dot(rnmBasis1, normalTS)) * rnm1
				+ saturate(dot(rnmBasis2, normalTS)) * rnm2;
				
				#ifdef _BAKED_SPECULAR
				float3 viewDirT = -normalize(viewDirTS);
				float3 dominantDirT = rnmBasis0 * dot(rnm0, GRAYSCALE) +
				rnmBasis1 * dot(rnm1, GRAYSCALE) +
				rnmBasis2 * dot(rnm2, GRAYSCALE);
				
				float3 dominantDirTN = normalize(dominantDirT);
				half3 specColor = saturate(dot(rnmBasis0, dominantDirTN)) * rnm0 +
				saturate(dot(rnmBasis1, dominantDirTN)) * rnm1 +
				saturate(dot(rnmBasis2, dominantDirTN)) * rnm2;
				
				half3 halfDir = Unity_SafeNormalize(dominantDirTN - viewDirT);
				half NoH = saturate(dot(normalTS, halfDir));
				half spec = D_GGX(NoH, roughness);
				directSpecular += spec * specColor;
				#endif
				
				#endif
			}
			
			void BakerySHLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalWS, float3 viewDir, half roughness, half3 f0)
			{
				#ifdef BAKERY_SH
				
				half3 L0 = lightMap;
				float3 nL1x = _RNM0.Sample(sampler_RNM0, lightmapUV) * 2.0 - 1.0;
				float3 nL1y = _RNM1.Sample(sampler_RNM1, lightmapUV) * 2.0 - 1.0;
				float3 nL1z = _RNM2.Sample(sampler_RNM2, lightmapUV) * 2.0 - 1.0;
				float3 L1x = nL1x * L0 * 2.0;
				float3 L1y = nL1y * L0 * 2.0;
				float3 L1z = nL1z * L0 * 2.0;
				
				#ifdef BAKERY_SHNONLINEAR
				float lumaL0 = dot(L0, float(1));
				float lumaL1x = dot(L1x, float(1));
				float lumaL1y = dot(L1y, float(1));
				float lumaL1z = dot(L1z, float(1));
				float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWS);
				
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				float regularLumaSH = dot(lightMap, 1.0);
				lightMap *= lerp(1.0, lumaSH / regularLumaSH, saturate(regularLumaSH * 16.0));
				#else
				lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
				#endif
				
				#ifdef _BAKED_SPECULAR
				float3 dominantDir = float3(dot(nL1x, GRAYSCALE), dot(nL1y, GRAYSCALE), dot(nL1z, GRAYSCALE));
				float3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + viewDir);
				half NoH = saturate(dot(normalWS, halfDir));
				half spec = D_GGX(NoH, roughness);
				half3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
				dominantDir = normalize(dominantDir);
				
				directSpecular += max(spec * sh, 0.0);
				#endif
				
				#endif
			}
			#endif
			
			void InitializeShaderData(uint facing)
			{
				shaderData.facing = facing;
				FlipBTN(facing, vertexData.worldNormal, vertexData.bitangent, vertexData.tangent);
				
				#ifdef _GEOMETRICSPECULAR_AA
				surf.perceptualRoughness = GSAA_Filament(vertexData.worldNormal, surf.perceptualRoughness);
				#endif
				shaderData.worldNormal = vertexData.worldNormal;
				shaderData.bitangent = vertexData.bitangent;
				shaderData.tangent = vertexData.tangent;
				
				surf.tangentNormal.g *= -1;
				TangentToWorldNormal(surf.tangentNormal, shaderData.worldNormal, shaderData.tangent, shaderData.bitangent);
				
				shaderData.viewDir = normalize(UnityWorldSpaceViewDir(vertexData.worldPos));
				shaderData.NoV = NormalDotViewDir(shaderData.worldNormal, shaderData.viewDir);
				shaderData.f0 = GetF0(surf.reflectance, surf.metallic, surf.albedo.rgb);
				shaderData.DFGLut = SampleDFG(shaderData.NoV, surf.perceptualRoughness).rg;
				shaderData.energyCompensation = EnvBRDFEnergyCompensation(shaderData.DFGLut, shaderData.f0);
			}
			
			#ifndef POM_INCLUDED
			#define POM_INCLUDED
			// com.unity.render-pipelines.core copyright © 2020 Unity Technologies ApS
			// Licensed under the Unity Companion License for Unity-dependent projects--see https://unity3d.com/legal/licenses/Unity_Companion_License.
			
			// This is implementation of parallax occlusion mapping (POM)
			// This function require that the caller define a callback for the height sampling name ComputePerPixelHeightDisplacement
			// A PerPixelHeightDisplacementParam is used to provide all data necessary to calculate the heights to ComputePerPixelHeightDisplacement it doesn't need to be
			// visible by the POM algorithm.
			// This function is compatible with tiled uv.
			// it return the offset to apply to the UVSet provide in PerPixelHeightDisplacementParam
			// viewDirTS is view vector in texture space matching the UVSet
			// ref: https://www.gamedev.net/resources/_/technical/graphics-programming-and-theory/a-closer-look-at-parallax-occlusion-mapping-r3262
			
			#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
			#define FLT_MIN         1.175494351e-38 // Minimum representable positive floating-point number
			#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number
			#define INV_HALF_PI     0.636619772367
			
			float _ParallaxFadingMip;
			float _MinSteps;
			float _MaxSteps;
			
			struct PerPixelHeightDisplacementParam
			{
				float2 uv;
				SamplerState sampl;
				Texture2D height;
			};
			
			float2 GetMinUvSize(float2 baseUV, float4 texelSize)
			{
				float2 minUvSize = float2(FLT_MAX, FLT_MAX);
				
				minUvSize = min(baseUV * texelSize.zw, minUvSize);
				
				return minUvSize;
			}
			
			float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
			{
				// Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
				// Tiling is automatically handled correctly here.
				return param.height.SampleLevel(param.sampl, param.uv + texOffsetCurrent, lod).r;
			}
			
			float2 ParallaxOcclusionMapping(float lod, float lodThreshold, uint numSteps, float uvSpaceScale, float3 viewDirTS, PerPixelHeightDisplacementParam ppdParam, out float outHeight)
			{
				// Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
				float stepSize = 1.0 / (float)numSteps;
				
				// View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
				// The length of viewDirTS vector determines the furthest amount of displacement:
				// float parallaxLimit = -length(viewDirTS.xy) / viewDirTS.z;
				// float2 parallaxDir = normalize(Out.viewDirTS.xy);
				// float2 parallaxMaxOffsetTS = parallaxDir * parallaxLimit;
				// Above code simplify to
				float2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z);
				float2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;
				
				// Do a first step before the loop to init all value correctly
				float2 texOffsetCurrent = float2(0.0, 0.0);
				float prevHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				texOffsetCurrent += texOffsetPerStep;
				float currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				float rayHeight = 1.0 - stepSize; // Start at top less one sample
				
				// Linear search
				for (uint stepIndex = 0; stepIndex < numSteps; ++stepIndex)
				{
					// Have we found a height below our ray height ? then we have an intersection
					if (currHeight > rayHeight)
					break; // end the loop
					
					prevHeight = currHeight;
					rayHeight -= stepSize;
					texOffsetCurrent += texOffsetPerStep;
					
					// Sample height map which in this case is stored in the alpha channel of the normal map:
					currHeight = ComputePerPixelHeightDisplacement(texOffsetCurrent, lod, ppdParam);
				}
				
				// Found below and above points, now perform line interesection (ray) with piecewise linear heightfield approximation
				
				// Refine the search with secant method
				#define POM_SECANT_METHOD 1
				#if POM_SECANT_METHOD
				
				float pt0 = rayHeight + stepSize;
				float pt1 = rayHeight;
				float delta0 = pt0 - prevHeight;
				float delta1 = pt1 - currHeight;
				
				float delta;
				float2 offset;
				
				// Secant method to affine the search
				// Ref: Faster Relief Mapping Using the Secant Method - Eric Risser
				[unroll]
				for (uint i = 0; i < 3; ++i)
				{
					// intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
					float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
					// Retrieve offset require to find this intersectionHeight
					offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
					
					currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
					
					delta = intersectionHeight - currHeight;
					
					if (abs(delta) <= 0.01)
					break;
					
					// intersectionHeight < currHeight => new lower bounds
					if (delta < 0.0)
					{
						delta1 = delta;
						pt1 = intersectionHeight;
					}
					else
					{
						delta0 = delta;
						pt0 = intersectionHeight;
					}
				}
				
				#else // regular POM intersection
				
				//float pt0 = rayHeight + stepSize;
				//float pt1 = rayHeight;
				//float delta0 = pt0 - prevHeight;
				//float delta1 = pt1 - currHeight;
				//float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
				//float2 offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;
				
				// A bit more optimize
				float delta0 = currHeight - rayHeight;
				float delta1 = (rayHeight + stepSize) - prevHeight;
				float ratio = delta0 / (delta0 + delta1);
				float2 offset = texOffsetCurrent - ratio * texOffsetPerStep;
				
				currHeight = ComputePerPixelHeightDisplacement(offset, lod, ppdParam);
				
				#endif
				
				outHeight = currHeight;
				
				// Fade the effect with lod (allow to avoid pop when switching to a discrete LOD mesh)
				offset *= (1.0 - saturate(lod - lodThreshold));
				
				return offset;
			}
			
			float2 ParallaxOcclusionMappingUVOffset(float2 uv, float scale, float3 viewDirTS, Texture2D tex, SamplerState sampl, float4 texelSize)
			{
				float3 viewDirUV = normalize(float3(viewDirTS.xy * scale, viewDirTS.z));
				
				float unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);
				uint numSteps = (uint)lerp(_MinSteps, _MaxSteps, unitAngle);
				
				float2 minUvSize = GetMinUvSize(uv, texelSize);
				float lod = ComputeTextureLOD(minUvSize);
				
				PerPixelHeightDisplacementParam ppdParam;
				
				ppdParam.uv = uv;
				ppdParam.height = tex;
				ppdParam.sampl = sampl;
				
				float height = 0;
				float2 offset = ParallaxOcclusionMapping(lod, _ParallaxFadingMip, numSteps, scale, viewDirUV, ppdParam, height);
				
				return offset;
			}
			#endif
			void InitializeSurfaceData()
			{
				float2 mainUV = TRANSFORM_TEX(vertexData.uv[0], _MainTex);
				#ifdef _PARALLAX_MAP
				shaderData.parallaxUVOffset = ParallaxOcclusionMappingUVOffset(mainUV, _Parallax, vertexData.viewDirTS, _ParallaxMap, sampler_MainTex, _ParallaxMap_TexelSize);
				#endif
				mainUV += shaderData.parallaxUVOffset;
				
				half4 mainTex = _MainTex.Sample(sampler_MainTex, mainUV);
				mainTex.rgb = lerp(dot(mainTex.rgb, GRAYSCALE), mainTex.rgb, _AlbedoSaturation);
				mainTex *= _Color;
				surf.albedo = mainTex.rgb;
				surf.alpha = mainTex.a;
				
				#ifdef _NORMAL_MAP
				half4 normalMap = _BumpMap.Sample(sampler_BumpMap, mainUV);
				surf.tangentNormal = UnpackScaleNormal(normalMap, _BumpScale);
				#endif
				
				#ifdef _MASK_MAP
				half4 maskMap = _MetallicGlossMap.Sample(sampler_MetallicGlossMap, mainUV);
				surf.perceptualRoughness = 1.0 - (RemapMinMax(maskMap.a, _GlossinessMinMax.x, _GlossinessMinMax.y));
				surf.metallic = RemapMinMax(maskMap.r, _MetallicMinMax.x, _MetallicMinMax.y);
				surf.occlusion = lerp(1.0, maskMap.g, _Occlusion);
				#else
				surf.perceptualRoughness = 1.0 - _Glossiness;
				surf.metallic = _Metallic;
				#endif
				
				surf.reflectance = _Reflectance;
			}
			
			VertexData Vertex(VertexInput v)
			{
				VertexData o;
				UNITY_INITIALIZE_OUTPUT(VertexData, o);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				#ifdef UNITY_PASS_META
				o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
				#else
				#if !defined(UNITY_PASS_SHADOWCASTER)
				o.pos = UnityObjectToClipPos(v.vertex);
				#endif
				#endif
				
				o.uv[0].xy = v.uv0.xy;
				o.uv[1].xy = v.uv1.xy;
				o.uv[2].xy = v.uv2.xy;
				o.uv[3].xy = v.uv3.xy;
				
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
				o.bitangent = cross(o.tangent, o.worldNormal) * v.tangent.w;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				o.vertexLight = Shade4PointLights
				(
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb,
				unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, o.worldPos,  o.worldNormal
				);
				#endif
				
				#if defined(REQUIRE_VIEWDIRTS)
				TANGENT_SPACE_ROTATION;
				o.viewDirTS = mul(rotation, ObjSpaceViewDir(v.vertex));
				#endif
				
				#ifdef UNITY_PASS_SHADOWCASTER
				o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
				o.pos = UnityApplyLinearShadowBias(o.pos);
				TRANSFER_SHADOW_CASTER_NOPOS(o, o.pos);
				#else
				UNITY_TRANSFER_SHADOW(o, o.uv[1].xy);
				UNITY_TRANSFER_FOG(o,o.pos);
				#endif
				
				#ifdef REQUIRE_SCREENPOS
				o.screenPos = ComputeScreenPos(o.pos);
				#endif
				
				return o;
			}
			
			half4 Fragment(VertexData input, uint facing : SV_IsFrontFace) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(vertexData)
				vertexData = input;
				
				#if defined(LOD_FADE_CROSSFADE)
				UnityApplyDitherCrossFade(vertexData.pos);
				#endif
				
				InitializeDefaultSurfaceData();
				InitializeSurfaceData();
				
				shaderData = (ShaderData)0;
				InitializeShaderData(facing);
				
				#if defined(UNITY_PASS_SHADOWCASTER)
				#if defined(_MODE_CUTOUT)
				if (surf.alpha < _Cutoff) discard;
				#endif
				
				#ifdef _ALPHAPREMULTIPLY_ON
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAPREMULTIPLY_ON) || defined(_MODE_FADE)
				half dither = Unity_Dither(surf.alpha, input.pos.xy);
				if (dither < 0.0) discard;
				#endif
				
				SHADOW_CASTER_FRAGMENT(vertexData);
				#else
				
				half3 indirectSpecular = 0.0;
				half3 directSpecular = 0.0;
				half3 otherSpecular = 0.0;
				
				half roughness = surf.perceptualRoughness * surf.perceptualRoughness;
				half clampedRoughness = max(roughness, 0.002);
				
				InitializeLightData(lightData, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, clampedRoughness, surf.perceptualRoughness, shaderData.f0);
				
				#if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
				lightData.FinalColor += vertexData.vertexLight;
				#endif
				
				#if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
				NonImportantLightsPerPixel(lightData.FinalColor, directSpecular, vertexData.worldPos, shaderData.worldNormal, shaderData.viewDir, shaderData.NoV, shaderData.f0, clampedRoughness);
				#endif
				
				half3 indirectDiffuse;
				#if defined(LIGHTMAP_ANY)
				
				float2 lightmapUV = vertexData.uv[1] * unity_LightmapST.xy + unity_LightmapST.zw;
				half4 bakedColorTex = SampleBicubic(unity_Lightmap, samplerunity_Lightmap, lightmapUV);
				half3 lightMap = DecodeLightmap(bakedColorTex);
				
				#ifdef BAKERY_RNM
				BakeryRNMLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, surf.tangentNormal, vertexData.viewDirTS, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#ifdef BAKERY_SH
				BakerySHLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, shaderData.worldNormal, shaderData.viewDir, clampedRoughness, shaderData.f0);
				#endif
				
				#if defined(DIRLIGHTMAP_COMBINED)
				float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
				lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, shaderData.worldNormal);
				#endif
				
				#if defined(DYNAMICLIGHTMAP_ON)
				float2 realtimeLightmapUV = vertexData.uv[2] * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				float3 realtimeLightMap = getRealtimeLightmap(realtimeLightmapUV, shaderData.worldNormal);
				lightMap += realtimeLightMap;
				#endif
				
				#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
				lightData.FinalColor = 0.0;
				lightData.Specular = 0.0;
				lightMap = SubtractMainLightWithRealtimeAttenuationFromLightmap (lightMap, lightData.Attenuation, bakedColorTex, shaderData.worldNormal);
				#endif
				
				indirectDiffuse = lightMap;
				#else
				indirectDiffuse = GetLightProbes(shaderData.worldNormal, vertexData.worldPos.xyz);
				#endif
				indirectDiffuse = max(0.0, indirectDiffuse);
				
				#if !defined(_SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
				directSpecular += lightData.Specular;
				#endif
				
				#if defined(_BAKED_SPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERYLM_ENABLED)
				{
					float3 bakedDominantDirection = 1.0;
					half3 bakedSpecularColor = 0.0;
					
					#if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
					bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
					bakedSpecularColor = indirectDiffuse;
					#endif
					
					#ifndef LIGHTMAP_ANY
					bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
					#endif
					
					bakedDominantDirection = normalize(bakedDominantDirection);
					directSpecular += GetSpecularHighlights(shaderData.worldNormal, bakedSpecularColor, bakedDominantDirection, shaderData.f0, shaderData.viewDir, clampedRoughness, shaderData.NoV, shaderData.energyCompensation);
				}
				#endif
				
				#if !defined(_REFLECTIONS_OFF) && defined(UNITY_PASS_FORWARDBASE)
				indirectSpecular += GetReflections(shaderData.worldNormal, vertexData.worldPos.xyz, shaderData.viewDir, shaderData.f0, roughness, shaderData.NoV, indirectDiffuse);
				#endif
				
				otherSpecular *= EnvBRDFMultiscatter(shaderData.DFGLut, shaderData.f0) * shaderData.energyCompensation;
				
				#if defined(_ALPHAPREMULTIPLY_ON)
				surf.albedo.rgb *= surf.alpha;
				surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
				#endif
				
				#if defined(_ALPHAMODULATE_ON)
				surf.albedo.rgb = lerp(1.0, surf.albedo.rgb, surf.alpha);
				#endif
				
				#if defined(_MODE_CUTOUT)
				AACutout(surf.alpha, _Cutoff);
				#endif
				
				half4 finalColor = 0;
				//final color
				finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + (lightData.FinalColor))
				+ indirectSpecular + (directSpecular * UNITY_PI) + otherSpecular + surf.emission, surf.alpha);
				
				#ifdef UNITY_PASS_META
				UnityMetaInput metaInput;
				UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
				metaInput.Emission = surf.emission;
				metaInput.Albedo = surf.albedo;
				
				return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
				#endif
				
				UNITY_APPLY_FOG(vertexData.fogCoord, finalColor);
				
				return finalColor;
				#endif
			}
			
			ENDCG
		}
		
	}
	CustomEditor "z3y.Shaders.SmartGUI"
}
