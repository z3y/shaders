using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class LitGUI : SmartGUI
    {
        #region Properties
        private MaterialProperty Foldout_RenderingOptions;
        private MaterialProperty _Mode;
        private MaterialProperty _Cutoff;
        private MaterialProperty _CutoutSharpness;
        private MaterialProperty ResetFix;
        private MaterialProperty _ZWrite;
        private MaterialProperty _DstBlend;
        private MaterialProperty _SrcBlend;
        private MaterialProperty _Cull;

        private MaterialProperty Foldout_SurfaceInputs;
        private MaterialProperty _MainTex;
        private MaterialProperty _Color;
        private MaterialProperty _AlbedoSaturation;
        private MaterialProperty _MetallicGlossMap;
        private MaterialProperty _Metallic;
        private MaterialProperty _Glossiness;
        private MaterialProperty _GlossinessRange;
        private MaterialProperty _GlossinessRemapping;
        private MaterialProperty _MetallicRemapping;
        private MaterialProperty _OcclusionStrength;
        private MaterialProperty _Reflectance;
        private MaterialProperty _BumpMap;
        private MaterialProperty _BumpScale;
        private MaterialProperty _SpecularOcclusion;
        private MaterialProperty _SmoothnessAlbedoAlpha;

        private MaterialProperty _ParallaxMap;
        private MaterialProperty _Parallax;
        private MaterialProperty _ParallaxOffset;
        private MaterialProperty _ParallaxSteps;

        private MaterialProperty Foldout_Emission;
        private MaterialProperty _EmissionMap;
        private MaterialProperty _EmissionColor;
        private MaterialProperty _EmissionMap_UV;
        private MaterialProperty _EmissionMultBase;
        private MaterialProperty _EmissionGIMultiplier;
        private MaterialProperty _AudioLinkEmissionBand;
        private MaterialProperty _EmissionIntensity;
        private MaterialProperty _EmissionColorLDR;
        private MaterialProperty _UseEmissionIntensity;

        private MaterialProperty Foldout_DetailFoldout;
        private MaterialProperty _DetailAlbedoMap;
        private MaterialProperty _DetailMask;
        private MaterialProperty _DetailBlendMode;
        private MaterialProperty _DetailMask_UV;
        private MaterialProperty _DetailNormalMap;
        private MaterialProperty _DetailColor;
        private MaterialProperty _DetailNormalScale;
        private MaterialProperty _DetailMetallic;
        private MaterialProperty _DetailGlossiness;
        private MaterialProperty _DetailMap_UV;
        private MaterialProperty _DetailHeightBlend;
        private MaterialProperty _HeightBlend;
        private MaterialProperty _HeightBlendInvert;

        private MaterialProperty Foldout_WindFoldout;
        private MaterialProperty _WindNoise;
        private MaterialProperty _WindScale;
        private MaterialProperty _WindSpeed;
        private MaterialProperty _WindIntensity;

        private MaterialProperty Foldout_AvancedSettings;
        private MaterialProperty _specularAntiAliasingVariance;
        private MaterialProperty _specularAntiAliasingThreshold;
        private MaterialProperty Bakery;
        private MaterialProperty _AudioLinkEmissionToggle;
        private MaterialProperty _EmissionToggle;
        private MaterialProperty _IsDecal;
        private MaterialProperty _WindToggle;
        private MaterialProperty _BAKERY_SHNONLINEAR;
        private MaterialProperty _NonLinearLightProbeSH;
        private MaterialProperty _LightmappedSpecular;
        private MaterialProperty _BicubicLightmap;
        private MaterialProperty _LTCGI;
        private MaterialProperty _LTCGI_DIFFUSE_OFF;
        private MaterialProperty _SpecularHighlights;
        private MaterialProperty _GlossyReflections;
        private MaterialProperty _ForceBoxProjection;
        private MaterialProperty _BlendReflectionProbes;
        private MaterialProperty _GSAA;
        #endregion

        public override void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            if (ResetFix.floatValue == 0f)
            {
                foreach (var keyword in material.shaderKeywords)
                {
                    material.DisableKeyword(keyword);
                }
                var preset = ProjectSettings.ShaderSettings.defaultPreset;
                if (preset != null)
                {
                    ApplyPresetPartially(preset, material, material.shader, 1);
                }
                MaterialEditor.ApplyMaterialPropertyDrawers(material);
                OnValidate(material);

                ResetFix.floatValue = 1f;
            }

            EditorGUI.BeginChangeCheck();
            Draw(_Mode);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material target in materialEditor.targets)
                {
                    SetupMaterialWithBlendMode(target, (int)_Mode.floatValue);
                }
            }
            if (_Mode.floatValue == 1)
            {
                Draw(_Cutoff);
                Draw(_CutoutSharpness);
            }

            Space();

            if (Foldout(Foldout_RenderingOptions))
            {
                DrawRenderingOptions(materialEditor, materialProperties, material);
            }

            if (Foldout(Foldout_SurfaceInputs))
            {
                DrawSurfaceInputs(materialEditor, materialProperties, material);
            }

            if (Foldout(Foldout_Emission))
            {
                DrawEmission(materialEditor, materialProperties, material);
            }

            if (Foldout(Foldout_DetailFoldout))
            {
                DrawDetail(materialEditor, materialProperties, material);
            }

            if (Foldout(Foldout_WindFoldout))
            {
                DrawWind(materialEditor, materialProperties, material);
            }

            if (Foldout(Foldout_AvancedSettings))
            {
                DrawAvancedSettings(materialEditor, materialProperties, material);
            }

            Space();
            DrawSplitter();
            Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();

/*            Space();
            GUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Keywords");
            if (material.shaderKeywords.Length > 0) Space();
            foreach (var keyword in material.shaderKeywords)
            {
                EditorGUILayout.LabelField(keyword);
            }
            GUILayout.EndVertical();*/

        }

        private void DrawAvancedSettings(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_SpecularHighlights);
            Draw(_GlossyReflections);
            Draw(_ForceBoxProjection, "Force Box Projection on Quest");
            Draw(_BlendReflectionProbes);

            Draw(_GSAA);
            if (_GSAA.floatValue == 1)
            {
                Draw(_specularAntiAliasingVariance);
                Draw(_specularAntiAliasingThreshold);
            }

            if (ProjectSettings.ShaderSettings.allowLTCGI)
            {
                Space();
                Draw(_LTCGI);
                Draw(_LTCGI_DIFFUSE_OFF);
            }

            Space();
            Draw(_LightmappedSpecular);
            if (ProjectSettings.ShaderSettings.bicubicLightmap == LitShaderSettings.BicubicLightmap.PerMaterial)
            {
                Draw(_BicubicLightmap);
            }

            Space();
            if (ProjectSettings.ShaderSettings.bakeryMode == LitShaderSettings.BakeryMode.PerMaterial)
            {
                Draw(Bakery);
            }

            if (ProjectSettings.ShaderSettings.nonLinearLightmapSH == LitShaderSettings.NonLinearLightmapSH.PerMaterial)
            {
                Draw(_BAKERY_SHNONLINEAR);
            }
            if (ProjectSettings.ShaderSettings.nonLinearLightprobeSH == LitShaderSettings.NonLinearLightprobeSH.PerMaterial)
            {
                Draw(_NonLinearLightProbeSH);
            }
            Space();
        }

        private void DrawWind(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_WindToggle);
            Draw(_WindNoise);
            Draw(_WindScale);
            Draw(_WindSpeed);
            Draw(_WindIntensity);

            EditorGUILayout.HelpBox("Vertex Colors RGB used for wind intensity XYZ mask", MessageType.Info);
            Space();
        }

        private void DrawDetail(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_DetailBlendMode);
            Draw(_DetailMask);
            materialEditor.TextureScaleOffsetProperty(_DetailBlendMode);
            Draw(_DetailMask_UV);

            Space();


            Draw(_DetailAlbedoMap, _DetailColor, null, "RGB: Albedo\nA: Blend Mask");
            if (TexturePackingButton())
            {
                TexturePacking window = GetPackingWindow(material);
                window.packingProperty = _DetailAlbedoMap;
            }
            Draw(_DetailNormalMap, _DetailNormalScale);

            if (_DetailBlendMode.floatValue == 3)
            {
                Draw(_DetailMetallic);
                Draw(_DetailGlossiness);
            }
            materialEditor.TextureScaleOffsetProperty(_DetailAlbedoMap);
            Draw(_DetailMap_UV);
            Draw(_IsDecal, "Use the Detail textures as Decal, only sampling within the UV range");

            Space();
            Draw(_DetailHeightBlend, _HeightBlend);
            Draw(_HeightBlendInvert);
        }

        private void DrawEmission(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_EmissionToggle);
            Space();


            Draw(_UseEmissionIntensity);
            if (_UseEmissionIntensity.floatValue == 1)
            {
                Draw(_EmissionMap, _EmissionColorLDR, _EmissionIntensity);
                _EmissionIntensity.floatValue = Mathf.Clamp(_EmissionIntensity.floatValue, 0, float.MaxValue);
                _EmissionColor.colorValue = _EmissionColorLDR.colorValue * _EmissionIntensity.floatValue;
            }
            else
            {
                Draw(_EmissionMap, _EmissionColor);
            }


            materialEditor.TextureScaleOffsetProperty(_EmissionMap);
            Draw(_EmissionMap_UV);
            materialEditor.LightmapEmissionProperty();

            Space();

            Draw(_EmissionMultBase, "Multiply emission with base color");
            Draw(_EmissionGIMultiplier, "Emission multiplier for the Meta Pass, used for realtime or baked GI");

            Space();
            Draw(_AudioLinkEmissionToggle);
            if (_AudioLinkEmissionToggle.floatValue == 1)
            {
                Draw(_AudioLinkEmissionBand);
            }

            Space();
        }

        private void DrawSurfaceInputs(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_MainTex, _Color, _AlbedoSaturation);
            if (TexturePackingButton())
            {
                TexturePacking window = GetPackingWindow(material);
                window.packingProperty = _MainTex;
            }

            Draw(_BumpMap, _BumpScale);
            Draw(_MetallicGlossMap, _SmoothnessAlbedoAlpha, null, "R: Metallic\nG: Occlusion\nA: Smoothness");
            if (TexturePackingButton())
            {
                TexturePacking window = GetPackingWindow(material);
                window.packingProperty = _MetallicGlossMap;
                window.dataR.displayName = "Metallic";
                window.dataG.displayName = "Occlusion";
                window.dataG.isWhite = true;
                window.dataB.displayName = "";
                window.dataA.displayName = "Smoothness";
                window.disableSrgb = true;
            }
            sRGBWarning(_MetallicGlossMap);
            EditorGUI.indentLevel+=2;
            if (_MetallicGlossMap.textureValue || _SmoothnessAlbedoAlpha.floatValue == 1)
            {
                DrawMinMax(_MetallicRemapping);
                DrawMinMax(_GlossinessRange);
                DrawMinMax(_GlossinessRemapping);
                Draw(_OcclusionStrength);
            }
            else
            {
                Draw(_Metallic);
                Draw(_Glossiness);
            }
            EditorGUI.indentLevel -= 2;

            if (_ParallaxMap.textureValue)
            {
                Draw(_ParallaxMap, _Parallax);
                Draw(_ParallaxOffset);
                Draw(_ParallaxSteps);
            }
            else
            {
                Draw(_ParallaxMap);
            }
            sRGBWarning(_ParallaxMap);
            Space();
            materialEditor.TextureScaleOffsetProperty(_MainTex);
            Draw(_Reflectance);
            Draw(_SpecularOcclusion, "Use lightmap or lightprobe intensity for occlusion");

            Space();
        }

        private static TexturePacking GetPackingWindow(Material material)
        {
            TexturePacking.Init();
            TexturePacking window = (TexturePacking)EditorWindow.GetWindow(typeof(TexturePacking));
            window.Close();
            TexturePacking.Init();
            window = (TexturePacking)EditorWindow.GetWindow(typeof(TexturePacking));
            window.packingMaterial = material;
            window.minSize = new Vector2(450, 350);
            return window;
        }

        private void DrawRenderingOptions(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            
            
            Draw(_SrcBlend);
            Draw(_DstBlend);
            Draw(_ZWrite);
            Draw(_Cull);

            Space();
        }

        private Color defaultEmission = new Color(0f, 0f, 0f, 1f);
        public override void AssignNewShaderToMaterial(Material m, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(m, oldShader, newShader);
            if (m == null || newShader == null)
            {
                return;
            }

            var localKeywords = (string[])_getShaderLocalKeywords.Invoke(null, new object[] { m.shader });

            foreach (var keyword in m.shaderKeywords)
            {
                if (!Array.Exists(localKeywords, x => x.Equals(keyword, StringComparison.Ordinal)))
                {
                    m.DisableKeyword(keyword);
                }
            }

            MaterialEditor.ApplyMaterialPropertyDrawers(m);
            SetupMaterialWithBlendMode(m, (int)m.GetFloat("_Mode"));
            if (m.GetColor("_EmissionColor") != defaultEmission || m.GetTexture("_EmissionMap") != null)
            {
                m.SetFloat("_EmissionToggle", 1f);
                m.EnableKeyword("_EMISSION");
                m.SetFloat("Foldout_Emission", 1f);
            }


            ApplyChanges(m);
        }

        public override void OnValidate(Material material)
        {
            ApplyChanges(material);
        }

        public static void ApplyChanges(Material m)
        {
            SetupGIFlags(m.GetFloat("_EmissionToggle"), m);

            int mode = (int)m.GetFloat("_Mode");
            m.ToggleKeyword("_ALPHATEST_ON", mode == 1);
            m.ToggleKeyword("_ALPHAFADE_ON", mode == 2);
            m.ToggleKeyword("_ALPHAPREMULTIPLY_ON", mode == 3);
            m.ToggleKeyword("_ALPHAMODULATE_ON", mode == 5);

            m.ToggleKeyword("_MASKMAP", m.GetTexture("_MetallicGlossMap"));
            m.ToggleKeyword("_NORMALMAP", m.GetTexture("_BumpMap"));
            m.ToggleKeyword("_PARALLAXMAP", m.GetTexture("_ParallaxMap"));

            int detailBlend = (int)m.GetFloat("_DetailBlendMode");
            m.ToggleKeyword("_DETAILBLEND_SCREEN", detailBlend == 1);
            m.ToggleKeyword("_DETAILBLEND_MULX2", detailBlend == 2);
            m.ToggleKeyword("_DETAILBLEND_LERP", detailBlend == 3);
            m.ToggleKeyword("_DETAIL_BLENDMASK", m.GetTexture("_DetailMask"));
            m.ToggleKeyword("_DETAIL_ALBEDOMAP", m.GetTexture("_DetailAlbedoMap"));
            m.ToggleKeyword("_DETAIL_NORMALMAP", m.GetTexture("_DetailNormalMap"));
            m.ToggleKeyword("_DETAIL_HEIGHTBLEND", m.GetTexture("_DetailHeightBlend"));

            int bakeryMode = (int)m.GetFloat("Bakery");
            m.ToggleKeyword("BAKERY_MONOSH", bakeryMode == 3);
            m.ToggleKeyword("BAKERY_RNM", bakeryMode == 2);
            m.ToggleKeyword("BAKERY_SH", bakeryMode == 1);

        }
    }
}
