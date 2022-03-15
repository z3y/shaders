using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class ComplexLitSmartGUI : SmartGUI
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

        private MaterialProperty _BumpMap;
        private MaterialProperty _BumpScale;
        private MaterialProperty _FlipNormal;
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
        private MaterialProperty _DetailDepth;
        private MaterialProperty _DetailMaskSelect;
        private MaterialProperty _DetailMask;
        private MaterialProperty _DetailMaskUV;
        private MaterialProperty _DetailAlbedoMap;
        private MaterialProperty _DetailNormalMap;
        private MaterialProperty _DetailNormalScale;
        private MaterialProperty _DetailMapUV;
        private MaterialProperty _DetailAlbedoScale;
        private MaterialProperty _DetailSmoothnessScale;
        private MaterialProperty _DetailBlendMode;
        private MaterialProperty _Layers;

        private MaterialProperty _DetailMapUV2;
        private MaterialProperty _DetailAlbedoMap2;
        private MaterialProperty _DetailNormalMap2;
        private MaterialProperty _DetailDepth2;
        private MaterialProperty _DetailAlbedoScale2;
        private MaterialProperty _DetailNormalScale2;
        private MaterialProperty _DetailSmoothnessScale2;
        private MaterialProperty _DetailMapUV3;
        private MaterialProperty _DetailAlbedoMap3;
        private MaterialProperty _DetailNormalMap3;
        private MaterialProperty _DetailDepth3;
        private MaterialProperty _DetailAlbedoScale3;
        private MaterialProperty _DetailNormalScale3;
        private MaterialProperty _DetailSmoothnessScale3;


        private MaterialProperty Foldout_RenderingOptions;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private MaterialProperty _LTCGI;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private MaterialProperty _LTCGI_DIFFUSE_OFF;
        private MaterialProperty _EmissionPulseIntensity;
        private MaterialProperty _EmissionPulseSpeed;
        private MaterialProperty _AudioLinkEmission;
        private MaterialProperty _AudioTexture;

        #endregion



        private static bool _maskPacking;
        private static TexturePacking.FieldData _maskPackingMetallic;
        private static TexturePacking.FieldData _maskPackingDetailMask;
        private static TexturePacking.FieldData _maskPackingOcclusion;
        private static TexturePacking.FieldData _maskPackingSmoothness;

        private static bool _layerMaskPacking;
        private static TexturePacking.FieldData _layerMaskPacking1;
        private static TexturePacking.FieldData _layerMaskPacking2;
        private static TexturePacking.FieldData _layerMaskPacking3;
        private static TexturePacking.FieldData _layerMaskPacking4;

        private static bool _detailPacking;
        private static TexturePacking.FieldData _detailPackingAlbedo;
        private static TexturePacking.FieldData _detailPackingSmoothness;

        private static bool _detailPacking2;
        private static TexturePacking.FieldData _detailPackingAlbedo2;
        private static TexturePacking.FieldData _detailPackingSmoothness2;

        private static bool _detailPacking3;
        private static TexturePacking.FieldData _detailPackingAlbedo3;
        private static TexturePacking.FieldData _detailPackingSmoothness3;

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
                Draw(_MetallicGlossMapArray, null, null, "R: Metallic\nG: Occlusion\nA: Smoothness");

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
                Draw(_MetallicGlossMap, null, null, "R: Metallic\nG: Occlusion\nA: Smoothness");
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


                Draw(_BumpMap, _BumpScale, null, "Normal Map (OpenGL)" );
                EditorGUI.indentLevel += 2;
                Draw(_FlipNormal, "Flip (DirectX Mode)");
                EditorGUI.indentLevel -= 2;
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
            me.LightmapEmissionProperty();
            Draw(_EmissionGIMultiplier, "Multiplies baked and realtime emission");
            EditorGUILayout.Space();

            Draw(_EmissionPulseIntensity);
            Draw(_EmissionPulseSpeed);
            EditorGUILayout.Space();
            Draw(_AudioLinkEmission);
            if (_AudioLinkEmission.floatValue != 1000)
            {
                Draw(_AudioTexture);
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
            Draw(_Layers);
            EditorGUILayout.Space();

            Draw(_DetailMask, "R: Layer 1\nG: Layer 2\nB: Layer 3");
            sRGBWarning(_DetailMask);
            DrawLayerMaskMapPacking(material);
            me.TextureScaleOffsetProperty(_DetailMask);
            Draw(_DetailMaskUV);
            EditorGUILayout.Space();

            int layers = (int)_Layers.floatValue;
            if (layers >= 1)
            {
                VerticalScopeBox(() => {
                    EditorGUILayout.LabelField("Layer 1", EditorStyles.boldLabel);
                    Draw(_DetailAlbedoMap, _DetailAlbedoScale, null, "RGB: Albedo\nA: Smoothness");
                    DrawDetailAlbedoPacking(material);
                    EditorGUI.indentLevel += 2;
                    Draw(_DetailSmoothnessScale);
                    EditorGUI.indentLevel -= 2;

                    Draw(_DetailNormalMap, _DetailNormalScale);
                    me.TextureScaleOffsetProperty(_DetailAlbedoMap);
                    Draw(_DetailMapUV);
                    Draw(_DetailDepth);
                    EditorGUILayout.Space();
                });
            }

            if (layers >= 2)
            {
                VerticalScopeBox(() => {
                    EditorGUILayout.LabelField("Layer 2", EditorStyles.boldLabel);
                    Draw(_DetailAlbedoMap2, _DetailAlbedoScale2, null, "RGB: Albedo\nA: Smoothness");
                    DrawDetailAlbedoPacking2(material);
                    EditorGUI.indentLevel += 2;
                    Draw(_DetailSmoothnessScale2);
                    EditorGUI.indentLevel -= 2;

                    Draw(_DetailNormalMap2, _DetailNormalScale2);
                    me.TextureScaleOffsetProperty(_DetailAlbedoMap2);
                    Draw(_DetailMapUV2);
                    Draw(_DetailDepth2);
                    EditorGUILayout.Space();
                });
            }

            if (layers >= 3)
            {
                VerticalScopeBox(() => {
                    EditorGUILayout.LabelField("Layer 3", EditorStyles.boldLabel);
                    Draw(_DetailAlbedoMap3, _DetailAlbedoScale3, null, "RGB: Albedo\nA: Smoothness");
                    DrawDetailAlbedoPacking3(material);
                    EditorGUI.indentLevel += 2;
                    Draw(_DetailSmoothnessScale3);
                    EditorGUI.indentLevel -= 2;

                    Draw(_DetailNormalMap3, _DetailNormalScale3);
                    me.TextureScaleOffsetProperty(_DetailAlbedoMap3);
                    Draw(_DetailMapUV3);
                    Draw(_DetailDepth3);
                    EditorGUILayout.Space();
                    });
            }

            
        }

        private void DrawRenderingOptions(Material material, MaterialEditor me)
        {
            if (!Foldout(Foldout_RenderingOptions))
            {
                return;
            }
            Draw(_GlossyReflections);
            Draw(_SpecularHighlights);
            Draw(_GSAA, "Reduces specular shimmering");
            if (_GSAA.floatValue == 1)
            {
                EditorGUI.indentLevel += 1;
                Draw(_specularAntiAliasingVariance);
                Draw(_specularAntiAliasingThreshold);
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.Space();
            }
            Draw(_Reflectance);
            Draw(_SpecularOcclusion, "Removes fresnel from dark parts of lightmap");
            EditorGUILayout.Space();


#if LTCGI_INCLUDED
            Draw(_LTCGI);
            Draw(_LTCGI_DIFFUSE_OFF);
            EditorGUILayout.Space();
#endif


            Draw(Bakery);
            Draw(_BakedSpecular, "Specular Highlights from Directional, SH or RNM Lightmaps or Light Probes");
            Draw(_NonLinearLightProbeSH, "Reduces ringing on Light Probes. Recommended to use with Bakery L1");
            EditorGUILayout.Space();

            Draw(_Cull);
            me.DoubleSidedGIField();
            me.EnableInstancingField();
            me.RenderQueueField();
            EditorGUILayout.Space();
        }
        private void DrawMaskMapPacking(Material material)
        {
            if (!TextureFoldout(ref _maskPacking))
            {
                return;
            }

            _maskPackingDetailMask.isWhite = true;
            _maskPackingOcclusion.isWhite = true;
            _maskPackingSmoothness.isWhite = true;
            TexturePacking.TexturePackingField(ref _maskPackingMetallic, "Metallic");
            //TexturePacking.TexturePackingField(ref _maskPackingDetailMask, "Detail Mask");
            TexturePacking.TexturePackingField(ref _maskPackingOcclusion, "Occlusion");
            TexturePacking.TexturePackingField(ref _maskPackingSmoothness, "Smoothness", "Roughness");

            TexturePacking.PackButton( ()=> {
                TexturePacking.Pack(_MetallicGlossMap, _maskPackingMetallic, _maskPackingDetailMask, _maskPackingOcclusion, _maskPackingSmoothness, true);
            }, () => {
                TexturePacking.ResetPackingField(ref _maskPackingMetallic,ref _maskPackingDetailMask,ref _maskPackingOcclusion,ref _maskPackingSmoothness);
            });
        }

        private void DrawLayerMaskMapPacking(Material material)
        {
            if (!TextureFoldout(ref _layerMaskPacking))
            {
                return;
            }

            _layerMaskPacking1.isWhite = true;
            _layerMaskPacking2.isWhite = true;
            _layerMaskPacking3.isWhite = true;

            TexturePacking.TexturePackingField(ref _layerMaskPacking1, "Layer 1");
            TexturePacking.TexturePackingField(ref _layerMaskPacking2, "Layer 2");
            TexturePacking.TexturePackingField(ref _layerMaskPacking3, "Layer 3");

            TexturePacking.PackButton( ()=> {
                TexturePacking.Pack(_DetailMask, _layerMaskPacking1, _layerMaskPacking2, _layerMaskPacking3, _layerMaskPacking4, true);
            }, () => {
                TexturePacking.ResetPackingField(ref _layerMaskPacking1,ref _layerMaskPacking2,ref _layerMaskPacking3,ref _layerMaskPacking4);
            });
        }

        private void DrawDetailAlbedoPacking(Material material)
        {
            if (!TextureFoldout(ref _detailPacking))
            {
                return;
            }

            _detailPackingAlbedo.isWhite = true;
            TexturePacking.TexturePackingField(ref _detailPackingAlbedo, "Albedo", null, false);
            TexturePacking.TexturePackingField(ref _detailPackingSmoothness, "Smoothness", "Roughness");

            TexturePacking.PackButton(() => {
                TexturePacking.Pack(_DetailAlbedoMap, _detailPackingAlbedo, _detailPackingSmoothness);
            }, () => {
                TexturePacking.ResetPackingField(ref _detailPackingAlbedo,ref _detailPackingSmoothness);
            });
        }

         private void DrawDetailAlbedoPacking2(Material material)
        {
            if (!TextureFoldout(ref _detailPacking2))
            {
                return;
            }

            _detailPackingAlbedo2.isWhite = true;
            TexturePacking.TexturePackingField(ref _detailPackingAlbedo2, "Albedo", null, false);
            TexturePacking.TexturePackingField(ref _detailPackingSmoothness2, "Smoothness", "Roughness");

            TexturePacking.PackButton(() => {
                TexturePacking.Pack(_DetailAlbedoMap2, _detailPackingAlbedo2, _detailPackingSmoothness2);
            }, () => {
                TexturePacking.ResetPackingField(ref _detailPackingAlbedo2,ref _detailPackingSmoothness2);
            });
        }

        private void DrawDetailAlbedoPacking3(Material material)
        {
            if (!TextureFoldout(ref _detailPacking3))
            {
                return;
            }

            _detailPackingAlbedo3.isWhite = true;
            TexturePacking.TexturePackingField(ref _detailPackingAlbedo3, "Albedo", null, false);
            TexturePacking.TexturePackingField(ref _detailPackingSmoothness3, "Smoothness", "Roughness");

            TexturePacking.PackButton(() => {
                TexturePacking.Pack(_DetailAlbedoMap3, _detailPackingAlbedo3, _detailPackingSmoothness3);
            }, () => {
                TexturePacking.ResetPackingField(ref _detailPackingAlbedo3,ref _detailPackingSmoothness3);
            });
        }

        

        public const string ShaderName = "Lit/Complex";
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
            m.ToggleKeyword("_DETAILBLEND_SCREEN", detailBlend == 1);
            m.ToggleKeyword("_DETAILBLEND_MULX2", detailBlend == 2);
            m.ToggleKeyword("_DETAILBLEND_LERP", detailBlend == 3);

            var layers = m.GetFloat("_Layers");

            m.ToggleKeyword("_LAYER1ALBEDO", m.GetTexture("_DetailAlbedoMap") && layers >= 1);
            m.ToggleKeyword("_LAYER2ALBEDO", m.GetTexture("_DetailAlbedoMap2") && layers >= 2);
            m.ToggleKeyword("_LAYER3ALBEDO", m.GetTexture("_DetailAlbedoMap3") && layers >= 3);

            m.ToggleKeyword("_LAYER1NORMAL", m.GetTexture("_DetailNormalMap") && layers >= 1);
            m.ToggleKeyword("_LAYER2NORMAL", m.GetTexture("_DetailNormalMap2") && layers >= 2);
            m.ToggleKeyword("_LAYER3NORMAL", m.GetTexture("_DetailNormalMap3") && layers >= 3);


            m.ToggleKeyword("PARALLAX", m.GetTexture("_ParallaxMap"));

#if !LTCGI_INCLUDED
            m.SetFloat("_LTCGI", 0f);
            m.DisableKeyword("LTCGI");
#endif
        }
    }
}
