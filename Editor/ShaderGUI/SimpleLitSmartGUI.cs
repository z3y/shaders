using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class SimpleLitSmartGUI : SmartGUI
    {
        #region Material Properties

        private MaterialProperty _Mode;
        private MaterialProperty _Cutoff;
        private MaterialProperty Foldout_SurfaceInputs;
        private MaterialProperty _MainTex;
        private MaterialProperty _Color;
        private MaterialProperty _AlbedoSaturation;
        private MaterialProperty _Texture;
        private MaterialProperty _MainTexArray;
        private MaterialProperty _MetallicGlossMapArray;
        private MaterialProperty _Metallic;
        private MaterialProperty _Glossiness;
        private MaterialProperty _MetallicMin;
        private MaterialProperty _GlossinessMin;
        private MaterialProperty _Occlusion;

        private MaterialProperty _MetallicGlossMap;

        private MaterialProperty _IsPackingMetallicGlossMap;
        private MaterialProperty _MetallicMap;
        private MaterialProperty _MetallicMapChannel;
        private MaterialProperty _MetallicMapInvert;
        private MaterialProperty _OcclusionMap;
        private MaterialProperty _OcclusionMapChannel;
        private MaterialProperty _OcclusionMapInvert;
        private MaterialProperty _DetailMaskMap;
        private MaterialProperty _DetailMaskMapChannel;
        private MaterialProperty _DetailMaskMapInvert;
        private MaterialProperty _SmoothnessMap;
        private MaterialProperty _SmoothnessMapChannel;
        private MaterialProperty _SmoothnessMapInvert;

        private MaterialProperty _BumpMap;
        private MaterialProperty _BumpScale;
        private MaterialProperty _BumpMapArray;

        private MaterialProperty Foldout_EmissionInputs;
        private MaterialProperty _EnableEmission;
        private MaterialProperty _EmissionMap;
        private MaterialProperty _EmissionColor;
        private MaterialProperty _EmissionDepth;
        private MaterialProperty _EmissionMultBase;
        private MaterialProperty _EmissionGIMultiplier;

        private MaterialProperty _Parallax;
        private MaterialProperty _ParallaxMap;
        private MaterialProperty _ParallaxOffset;
        private MaterialProperty _ParallaxSteps;

        private MaterialProperty _TextureIndex;

        private MaterialProperty Foldout_DetailInputs;
        private MaterialProperty _DetailAlbedoMap;
        private MaterialProperty _DetailNormalMap;
        private MaterialProperty _DetailNormalScale;
        private MaterialProperty _DetailMapUV;
        private MaterialProperty _DetailAlbedoScale;
        private MaterialProperty _DetailSmoothnessScale;

        private MaterialProperty _IsPackingDetailAlbedo;
        private MaterialProperty _DetailAlbedoPacking;
        private MaterialProperty _DetailSmoothnessPacking;
        private MaterialProperty _DetailSmoothnessPackingChannel;
        private MaterialProperty _DetailSmoothnessPackingInvert;
        private MaterialProperty _DetailAlbedoAlpha;
        private MaterialProperty _DetailBlendMode;


        private MaterialProperty Foldout_RenderingOptions;
        private MaterialProperty _RNM0;
        private MaterialProperty _RNM1;
        private MaterialProperty _RNM2;
        private MaterialProperty _Cull;
        private MaterialProperty _SpecularOcclusion;
        private MaterialProperty Bakery;
        private MaterialProperty _GlossyReflections;
        private MaterialProperty _SpecularHighlights;
        private MaterialProperty _Reflectance;
        private MaterialProperty _GSAA;
        private MaterialProperty _specularAntiAliasingVariance;
        private MaterialProperty _specularAntiAliasingThreshold;
        private MaterialProperty _NonLinearLightProbeSH;
        private MaterialProperty _BakedSpecular;
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private MaterialProperty _LTCGI;
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private MaterialProperty _LTCGI_DIFFUSE_OFF;
        private MaterialProperty _DetailDepth;
        #endregion


        public override void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            DrawSurfaceInputs(material, materialEditor);
            DrawEmissionMaps(material, materialEditor);
            DrawDetailInputs(material, materialEditor);
            DrawRenderingOptions(material, materialEditor);
        }

        

        private void DrawSurfaceInputs(Material material, MaterialEditor me)
        {
            EditorGUI.BeginChangeCheck();
            Draw(_Mode);
            if (EditorGUI.EndChangeCheck())
            {
                SetupBlendMode(me, _Mode);
            }
            if (_Mode.floatValue == 1) Draw(_Cutoff);


            EditorGUILayout.Space();

            if (!Foldout(Foldout_SurfaceInputs))
            {
                return;
            }

            if (_Texture.floatValue == 1 || _Texture.floatValue == 2)
            {
                Draw(_MainTexArray, _Color, _AlbedoSaturation);
                Draw(_MetallicGlossMapArray, null, null, "Metallic (R) | Occlusion (G) | Detail Mask (B) | Smoothness (A)");

                EditorGUI.indentLevel += 2;
                if (_MetallicGlossMapArray.textureValue == null)
                {
                    Draw(_Metallic);
                    Draw(_Glossiness);
                }
                else
                {
                    DrawMinMax(_MetallicMin, _Metallic);
                    DrawMinMax(_GlossinessMin, _Glossiness);
                    Draw(_Occlusion);
                }
                EditorGUI.indentLevel -= 2;
                Draw(_BumpMapArray, _BumpScale);
            }
            else
            {
                Draw(_MainTex, _Color, _AlbedoSaturation);
                Draw(_MetallicGlossMap, null, null, "Metallic (R) | Occlusion (G) | Detail Mask (B) | Smoothness (A)");
                sRGBWarning(_MetallicGlossMap);

                DrawMaskMapPacking(material);

                EditorGUI.indentLevel += 2;
                if (_MetallicGlossMap.textureValue == null)
                {
                    Draw(_Metallic);
                    Draw(_Glossiness);
                }
                else
                {
                    DrawMinMax(_MetallicMin, _Metallic);
                    DrawMinMax(_GlossinessMin, _Glossiness);
                    Draw(_Occlusion);
                }
                EditorGUI.indentLevel -= 2;


                Draw(_BumpMap, _BumpScale);
            }

            if (_ParallaxMap.textureValue)
            {
                Draw(_ParallaxMap, _Parallax);
                EditorGUI.indentLevel += 2;
                Draw(_ParallaxOffset);
                Draw(_ParallaxSteps);
                EditorGUI.indentLevel -= 2; ;
            }
            else
            {
                Draw(_ParallaxMap);
            }
            sRGBWarning(_ParallaxMap);

            EditorGUILayout.Space();
            me.TextureScaleOffsetProperty(_MainTex);
            Draw(_Texture);
            if (_Texture.floatValue == 2)
            {
                Draw(_TextureIndex);
            }
            EditorGUILayout.Space();

        }

        private void DrawEmissionMaps(Material material, MaterialEditor me)
        {
            if (!Foldout(Foldout_EmissionInputs))
            {
                return;
            }

            Draw(_EnableEmission);
            EditorGUILayout.Space();
            Draw(_EmissionMap, _EmissionColor, _EmissionDepth);
            Draw(_EmissionMultBase);

            EditorGUILayout.Space();
            me.LightmapEmissionProperty();

            if (material.globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack)
            {
                Draw(_EmissionGIMultiplier);
            }
            EditorGUILayout.Space();
        }

        private void DrawDetailInputs(Material material, MaterialEditor me)
        {
            if (!Foldout(Foldout_DetailInputs))
            {
                return;
            }

            Draw(_DetailBlendMode);
            Draw(_DetailAlbedoAlpha);
            Draw(_DetailAlbedoMap, _DetailAlbedoScale, null, _DetailAlbedoAlpha.floatValue == 1 ? "Albedo & Mask" : null);
            DrawDetailAlbedoPacking(material);
            if (_DetailAlbedoAlpha.floatValue == 0)
            {
                EditorGUI.indentLevel += 2;
                Draw(_DetailSmoothnessScale);
                EditorGUI.indentLevel -= 2;
            }
            else
            {
                _DetailSmoothnessScale.floatValue = 0f;
            }

            Draw(_DetailNormalMap, _DetailNormalScale);
            me.TextureScaleOffsetProperty(_DetailAlbedoMap);
            Draw(_DetailMapUV);
            Draw(_DetailDepth);
            EditorGUILayout.Space();
        }

        private void DrawRenderingOptions(Material material, MaterialEditor me)
        {
            if (!Foldout(Foldout_RenderingOptions))
            {
                return;
            }
            Draw(_GlossyReflections);
            Draw(_SpecularHighlights);
            Draw(_GSAA);
            if (_GSAA.floatValue == 1)
            {
                EditorGUI.indentLevel += 1;
                Draw(_specularAntiAliasingVariance);
                Draw(_specularAntiAliasingThreshold);
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.Space();
            }
            Draw(_Reflectance);
            Draw(_SpecularOcclusion);
            EditorGUILayout.Space();


#if LTCGI_INCLUDED
            Draw(_LTCGI);
            Draw(_LTCGI_DIFFUSE_OFF);
            EditorGUILayout.Space();
#endif



#if BAKERY_INCLUDED
            Draw(Bakery);
            if (Bakery.floatValue != 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                Draw(_RNM0);
                Draw(_RNM1);
                Draw(_RNM2);
                EditorGUI.EndDisabledGroup();
            }
#endif

            Draw(_BakedSpecular);
            Draw(_NonLinearLightProbeSH);
            EditorGUILayout.Space();

            Draw(_Cull);
            me.DoubleSidedGIField();
            me.EnableInstancingField();
            me.RenderQueueField();
            EditorGUILayout.Space();
        }
        private void DrawMaskMapPacking(Material material)
        {
            if (!TextureFoldout(_IsPackingMetallicGlossMap))
            {
                return;
            }

            VerticalScopeBox(() =>
            {
                Draw(_MetallicMap, _MetallicMapInvert, _MetallicMapChannel);
                Draw(_OcclusionMap, _OcclusionMapInvert, _OcclusionMapChannel);
                Draw(_DetailMaskMap, _DetailMaskMapInvert, _DetailMaskMapChannel);
                Draw(_SmoothnessMap, _SmoothnessMapInvert, _SmoothnessMapChannel, null, _SmoothnessMapInvert.floatValue == 1 ? "Roughness Map" : null);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pack"))
                {
                    if (PackMaskMap()) return;
                }

                if (GUILayout.Button("Close"))
                {
                    ResetProperty(material, new[] { _MetallicMap, _OcclusionMap, _DetailMaskMap, _IsPackingMetallicGlossMap });
                }
                EditorGUILayout.EndHorizontal();
            });
        }
        private bool PackMaskMap()
        {
            var rTex = (Texture2D)_MetallicMap.textureValue;
            var gTex = (Texture2D)_OcclusionMap.textureValue;
            var bTex = (Texture2D)_DetailMaskMap.textureValue;
            var aTex = (Texture2D)_SmoothnessMap.textureValue;

            var reference = aTex ?? gTex ?? rTex ?? bTex;
            if (reference == null) return true;

            var rChannel = new TexturePacking.Channel()
            {
                Tex = rTex,
                ID = (int)_MetallicMapChannel.floatValue,
                Invert = _MetallicMapInvert.floatValue == 1,
                DefaultWhite = false
            };

            var gChannel = new TexturePacking.Channel()
            {
                Tex = gTex,
                ID = (int)_OcclusionMapChannel.floatValue,
                Invert = _OcclusionMapInvert.floatValue == 1
            };

            var bChannel = new TexturePacking.Channel()
            {
                Tex = bTex,
                ID = (int)_DetailMaskMapChannel.floatValue,
                Invert = _DetailMaskMapInvert.floatValue == 1
            };

            var aChannel = new TexturePacking.Channel()
            {
                Tex = aTex,
                ID = (int)_SmoothnessMapChannel.floatValue,
                Invert = _SmoothnessMapInvert.floatValue == 1
            };

            var path = AssetDatabase.GetAssetPath(reference);
            var newPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_Packed";

            TexturePacking.Pack(new[] { rChannel, gChannel, bChannel, aChannel }, newPath, reference.width, reference.height);
            var packedTexture = TexturePacking.GetPackedTexture(newPath);
            TexturePacking.DisableSrgb(packedTexture);
            _MetallicGlossMap.textureValue = packedTexture;
            return false;
        }

        private void DrawDetailAlbedoPacking(Material material)
        {
            if (!TextureFoldout(_IsPackingDetailAlbedo))
            {
                return;
            }

            VerticalScopeBox(() =>
            {
                Draw(_DetailAlbedoPacking);
                Draw(_DetailSmoothnessPacking, _DetailSmoothnessPackingChannel, _DetailSmoothnessPackingInvert, null, 
                    _DetailAlbedoAlpha.floatValue == 1 ? "Mask Map" : _DetailSmoothnessPackingInvert.floatValue == 1 ? "Roughness Map" : null);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pack"))
                {
                    PackDetailAlbedoMap();
                }

                if (GUILayout.Button("Close"))
                {
                    ResetProperty(material, new [] { _DetailAlbedoPacking, _DetailSmoothnessPacking, _IsPackingDetailAlbedo });
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        private bool PackDetailAlbedoMap()
        {
            var detailAlbedo = (Texture2D)_DetailAlbedoPacking.textureValue;
            var detailSmoothness = (Texture2D)_DetailSmoothnessPacking.textureValue;

            var reference = detailAlbedo ?? detailSmoothness;
            if (reference == null) return true;

            var rChannel = new TexturePacking.Channel()
            {
                Tex = detailAlbedo,
                ID = 0,
            };

            var gChannel = new TexturePacking.Channel()
            {
                Tex = detailAlbedo,
                ID = 1,
            };

            var bChannel = new TexturePacking.Channel()
            {
                Tex = detailAlbedo,
                ID = 2,
            };

            var aChannel = new TexturePacking.Channel()
            {
                Tex = detailSmoothness,
                ID = (int)_DetailSmoothnessPackingChannel.floatValue,
                Invert = _DetailSmoothnessPackingInvert.floatValue == 1
            };

            var path = AssetDatabase.GetAssetPath(reference);
            var newPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_Packed";

            TexturePacking.Pack(new[] { rChannel, gChannel, bChannel, aChannel }, newPath, reference.width, reference.height);
            var packedTexture = TexturePacking.GetPackedTexture(newPath);
            _DetailAlbedoMap.textureValue = packedTexture;
            return false;
        }

        public const string ShaderName = "Simple Lit";
        public override void AssignNewShaderToMaterial(Material m, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(m, oldShader, newShader);
            if (m == null || newShader == null || newShader.name != ShaderName)
            {
                return;
            }

            foreach (var keyword in m.shaderKeywords)
            {
                m.DisableKeyword(keyword);
            }

            MaterialEditor.ApplyMaterialPropertyDrawers(m);
            SetupMaterialWithBlendMode(m, (int)m.GetFloat("_Mode"));
            ApplyChanges(m);
        }

        public override void OnValidate(Material material)
        {
            ApplyChanges(material);
        }

        public static void ApplyChanges(Material m)
        {
            SetupGIFlags(m.GetFloat("_EnableEmission"), m);

            int mode = (int)m.GetFloat("_Mode");
            m.ToggleKeyword("_MODE_CUTOUT", mode == 1);
            m.ToggleKeyword("_MODE_FADE", mode == 2);
            m.ToggleKeyword("_ALPHAPREMULTIPLY_ON", mode == 3);
            m.ToggleKeyword("_ALPHAMODULATE_ON", mode == 5);

            m.ToggleKeyword("AUDIOLINK", m.GetFloat("_AudioLinkEmission") != 1000);

            var samplingMode = (int)m.GetFloat("_Texture");
            m.ToggleKeyword("_TEXTURE_ARRAY", samplingMode == 1 || samplingMode == 2);

            if (samplingMode == 1 || samplingMode == 2)
            {
                m.ToggleKeyword("_MASK_MAP", m.GetTexture("_MetallicGlossMapArray"));
                m.ToggleKeyword("_NORMAL_MAP", m.GetTexture("_BumpMapArray"));
            }
            else
            {
                m.ToggleKeyword("_MASK_MAP", m.GetTexture("_MetallicGlossMap"));
                m.ToggleKeyword("_NORMAL_MAP", m.GetTexture("_BumpMap"));
            }

            int bakeryMode = (int)m.GetFloat("Bakery");
            m.ToggleKeyword("BAKERY_RNM", bakeryMode == 2);
            m.ToggleKeyword("BAKERY_SH", bakeryMode == 1);

            var detailBlend = (int)m.GetFloat("_DetailBlendMode");
            m.ToggleKeyword("_DETAILALBEDO_MAP", m.GetTexture("_DetailAlbedoMap"));
            m.ToggleKeyword("_DETAILNORMAL_MAP", m.GetTexture("_DetailNormalMap"));
            m.ToggleKeyword("_DETAILBLEND_SCREEN", detailBlend == 1);
            m.ToggleKeyword("_DETAILBLEND_MULX2", detailBlend == 2);
            m.ToggleKeyword("_DETAILBLEND_LERP", detailBlend == 3);

            m.ToggleKeyword("PARALLAX", m.GetTexture("_ParallaxMap"));

#if !LTCGI_INCLUDED
            m.SetFloat("_LTCGI", 0f);
            m.DisableKeyword("LTCGI");
#endif
        }
    }
}
