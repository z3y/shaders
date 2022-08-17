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

        private MaterialProperty Foldout_Emission;
        private MaterialProperty _EmissionMap;
        private MaterialProperty _EmissionColor;
        private MaterialProperty _EmissionMap_UV;
        private MaterialProperty _EmissionMultBase;
        private MaterialProperty _EmissionGIMultiplier;
        private MaterialProperty _AudioLinkEmissionBand;

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

        #endregion

        public override void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            if (ResetFix.floatValue == 0f)
            {
                foreach (var keyword in material.shaderKeywords)
                {
                    material.DisableKeyword(keyword);
                }
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


            /*        GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Keywords");
                    if (material.shaderKeywords.Length > 0) Space();
                    foreach (var keyword in material.shaderKeywords)
                    {
                        EditorGUILayout.LabelField(keyword);
                    }
                    GUILayout.EndVertical();*/

        }

        private void DrawWind(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            KeywordToggle("_WIND", material, new GUIContent("Enable Wind"));
            Draw(_WindNoise);
            Draw(_WindScale);
            Draw(_WindSpeed);
            Draw(_WindIntensity);

            Space();
        }

        private void DrawDetail(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_DetailBlendMode);
            Draw(_DetailMask);
            materialEditor.TextureScaleOffsetProperty(_DetailBlendMode);
            Draw(_DetailMask_UV);

            Space();


            Draw(_DetailAlbedoMap, _DetailColor);
            Draw(_DetailNormalMap, _DetailNormalScale);

            if (_DetailBlendMode.floatValue == 3)
            {
                Draw(_DetailMetallic);
                Draw(_DetailGlossiness);
            }
            materialEditor.TextureScaleOffsetProperty(_DetailAlbedoMap);
            Draw(_DetailMap_UV);
            KeywordToggle("_DECAL", material, new GUIContent("Use as Decal", "Use the Detail textures as Decal, only sampling within the UV range"));

            Space();
            Draw(_DetailHeightBlend, _HeightBlend);
            Draw(_HeightBlendInvert);
        }

        private void DrawEmission(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            KeywordToggle("_EMISSION", material, new GUIContent("Enable Emission"));
            Space();

            Draw(_EmissionMap, _EmissionColor);
            materialEditor.TextureScaleOffsetProperty(_EmissionMap);
            Draw(_EmissionMap_UV);

            Space();

            Draw(_EmissionMultBase);
            Draw(_EmissionGIMultiplier);

            Space();

            if (KeywordToggle("_AUDIOLINK_EMISSION", material, new GUIContent("Audio Link")))
            {
                Draw(_AudioLinkEmissionBand);
            }

            Space();
        }

        private void DrawSurfaceInputs(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_MainTex, _Color, _AlbedoSaturation);

            Draw(_BumpMap, _BumpScale);
            Draw(_MetallicGlossMap, _SmoothnessAlbedoAlpha);
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
            Space();
            //Draw(_Reflectance);
            materialEditor.TextureScaleOffsetProperty(_MainTex);
            Draw(_SpecularOcclusion, "Removes reflections based on lightmap or lightprobe intensity");

            Space();
        }
        
        private void DrawRenderingOptions(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            
            
            Draw(_SrcBlend);
            Draw(_DstBlend);
            Draw(_ZWrite);
            Draw(_Cull);

            Space();
        }

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

            //MaterialEditor.ApplyMaterialPropertyDrawers(m);
            SetupMaterialWithBlendMode(m, (int)m.GetFloat("_Mode"));
            ApplyChanges(m);
        }

        public override void OnValidate(Material material)
        {
            ApplyChanges(material);
        }

        private static void ApplyChanges(Material m)
        {
            //SetupGIFlags(m.GetFloat("_EmissionToggle"), m);

            int mode = (int)m.GetFloat("_Mode");
            m.ToggleKeyword("_ALPHATEST_ON", mode == 1);
            m.ToggleKeyword("_ALPHAFADE_ON", mode == 2);
            m.ToggleKeyword("_ALPHAPREMULTIPLY_ON", mode == 3);
            m.ToggleKeyword("_ALPHAMODULATE_ON", mode == 5);

            m.ToggleKeyword("_MASKMAP", m.GetTexture("_MetallicGlossMap"));
            m.ToggleKeyword("_NORMALMAP", m.GetTexture("_BumpMap"));

            int detailBlend = (int)m.GetFloat("_DetailBlendMode");
            m.ToggleKeyword("_DETAILBLEND_SCREEN", detailBlend == 1);
            m.ToggleKeyword("_DETAILBLEND_MULX2", detailBlend == 2);
            m.ToggleKeyword("_DETAILBLEND_LERP", detailBlend == 3);
            m.ToggleKeyword("_DETAIL_BLENDMASK", m.GetTexture("_DetailMask"));
            m.ToggleKeyword("_DETAIL_ALBEDOMAP", m.GetTexture("_DetailAlbedoMap"));
            m.ToggleKeyword("_DETAIL_NORMALMAP", m.GetTexture("_DetailNormalMap"));
            m.ToggleKeyword("_DETAIL_HEIGHTBLEND", m.GetTexture("_DetailHeightBlend"));


        }
    }
}
