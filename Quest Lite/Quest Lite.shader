Shader "Mobile/Quest Lite"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "FORWARDBASE"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma target 4.5

            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRECTIONAL

            //#define _ACES

            #include "UnityCG.cginc"
            //#include "AutoLight.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 lightmapOrSH : TEXCOORD1;
                #ifdef DIRECTIONAL
                float3 worldNormal : TEXCOORD2;
                float3 lightDir : TEXCOORD3;
                #endif

                UNITY_FOG_COORDS(3)

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float4 _MainTex_ST;
            half3 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                #ifdef LIGHTMAP_ON
                    o.lightmapOrSH.xy = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                #else
                    o.lightmapOrSH = ShadeSH9(float4(worldNormal, 1.0f));
                #endif

                #ifdef DIRECTIONAL
                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                    o.lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
                    // half NoL = saturate(dot(worldNormal, lightDirection));
                    // o.light = _LightColor0.rgb * NoL;
                    o.worldNormal = worldNormal;
                #endif

                return o;
            }

            #ifdef _ACES
            static const half3x3 ACESInputMat =
            {
                {0.59719, 0.35458, 0.04823},
                {0.07600, 0.90834, 0.01566},
                {0.02840, 0.13383, 0.83777}
            };

            // ODT_SAT => XYZ => D60_2_D65 => sRGB
            static const half3x3 ACESOutputMat =
            {
                { 1.60475, -0.53108, -0.07367},
                {-0.10208,  1.10813, -0.00605},
                {-0.00327, -0.07276,  1.07602}
            };

            half3 RRTAndODTFit(half3 v)
            {
                half3 a = v * (v + 0.0245786f) - 0.000090537f;
                half3 b = v * (0.983729f * v + 0.4329510f) + 0.238081f;
                return a / b;
            }

            half3 ACESFitted(half3 color)
            {
                color = mul(ACESInputMat, color);

                // Apply RRT and ODT
                color = RRTAndODTFit(color);

                color = mul(ACESOutputMat, color);

                // Clamp to [0, 1]
                // color = saturate(color);
                return color;
            }

            #endif

            half4 frag (v2f i) : SV_Target
            {
                half3 indirectDiffuse = 0;
                half3 light = 0;

                #ifdef DIRECTIONAL
                    half NoL = saturate(dot(i.worldNormal, i.lightDir));
                    light = _LightColor0.rgb * NoL;
                #endif

                #ifdef LIGHTMAP_ON
                    half4 lightmapData = unity_Lightmap.Sample(samplerunity_Lightmap, i.lightmapOrSH.xy);
                    indirectDiffuse = DecodeLightmap(lightmapData);
                #else
                    indirectDiffuse = i.lightmapOrSH;
                #endif


                half3 mainTex = _MainTex.Sample(sampler_MainTex, i.uv) * _Color;

                half3 finalColor = mainTex * (indirectDiffuse + light);
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                #ifdef _ACES
                    finalColor.rgb = ACESFitted(finalColor.rgb);
                #endif

                return finalColor.xyzz;
            }
            ENDCG
        }
    }
}
