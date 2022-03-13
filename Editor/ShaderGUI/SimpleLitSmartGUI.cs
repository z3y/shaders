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

        private static bool _detailPacking;
        private static TexturePacking.FieldData _detailPackingAlbedo;
        private static TexturePacking.FieldData _detailPackingSmoothness;

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
                Draw(_MetallicGlossMapArray, null, null, "R: Metallic\nG: Occlusion\nB: Detail Mask\nA: Smoothness");

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
                Draw(_MetallicGlossMap, null, null, "R: Metallic\nG: Occlusion\nB: Detail Mask\nA: Smoothness");
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

            Draw(_DetailMaskSelect);
            if (_DetailMaskSelect.floatValue == 1)
            {
                Draw(_DetailMask);
                me.TextureScaleOffsetProperty(_DetailMask);
                Draw(_DetailMaskUV);

            }
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
            TexturePacking.TexturePackingField(ref _maskPackingDetailMask, "Detail Mask");
            TexturePacking.TexturePackingField(ref _maskPackingOcclusion, "Occlusion");
            TexturePacking.TexturePackingField(ref _maskPackingSmoothness, "Smoothness", "Roughness");

            TexturePacking.PackButton( ()=> {
                TexturePacking.Pack(_MetallicGlossMap, _maskPackingMetallic, _maskPackingDetailMask, _maskPackingOcclusion, _maskPackingSmoothness, true);
            }, () => {
                TexturePacking.ResetPackingField(ref _maskPackingMetallic,ref _maskPackingDetailMask,ref _maskPackingOcclusion,ref _maskPackingSmoothness);
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

            m.ToggleKeyword("_DETAILMASK_MAP", m.GetFloat("_DetailMaskSelect") == 1 && m.GetTexture("_DetailMask"));

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
