using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using z3y.ShaderEditorFunctions;
using z3y.Shaders;
using static z3y.ShaderEditorFunctions.Functions;


namespace z3y.Shaders.SimpleLit
{

    public class ShaderEditor : ShaderGUI
    {
        private void ShaderPropertiesGUI(Material material, MaterialProperty[] props, MaterialEditor materialEditor)
        {

            EditorGUI.BeginChangeCheck();
            Prop("_Mode");
            if (EditorGUI.EndChangeCheck())
            {
                SetupBlendMode(materialEditor);
            }

            if (GetFloatValue("_Mode") == 1) Prop("_Cutoff");

            EditorGUILayout.Space();

            Prop("_MainTex", "_Color");
            EditorGUILayout.Space();

            if (GetProperty("_MetallicGlossMap").textureValue is null)
            {
                Prop("_Metallic");
                Prop("_Glossiness");
            }
            else
            {
                RangedProp(GetProperty("_MetallicMin"), GetProperty("_Metallic"));
                RangedProp(GetProperty("_GlossinessMin"), GetProperty("_Glossiness"));
                Prop("_Occlusion");
            }

            Prop("_MetallicGlossMap");
            sRGBWarning(GetProperty("_MetallicGlossMap"));


            Prop("_BumpMap", "_BumpScale");





            Prop("_EnableEmission");
            if (IfProp("_EnableEmission"))
            {
                Prop("_EmissionMap", "_EmissionColor");
                materialEditor.LightmapEmissionProperty();
                Prop("_EmissionMultBase");
            }

            Prop("_EnableParallax");
            if (IfProp("_EnableParallax"))
            {
                PropertyGroup(() =>
                {
                    Prop("_ParallaxMap", "_Parallax");
                    Prop("_ParallaxOffset");
                    Prop("_ParallaxSteps");
                });
            }

            sRGBWarning(GetProperty("_ParallaxMap"));




            Prop("_DetailAlbedoMap");
            Prop("_DetailNormalMap");

            EditorGUILayout.Space();

            Prop("_DetailAlbedoScale");
            Prop("_DetailNormalScale");
            Prop("_DetailSmoothnessScale");

            EditorGUILayout.Space();
            Prop("_DetailMap_UV");
            PropTileOffset("_DetailAlbedoMap");




            Prop("_GlossyReflections");
            Prop("_SpecularHighlights");

            Prop("_SpecularOcclusion");
            EditorGUILayout.Space();


            Prop("_GSAA");
            if (IfProp("_GSAA"))
            {
                Prop("_specularAntiAliasingVariance");
                Prop("_specularAntiAliasingThreshold");
            }

            ;


            Prop("_NonLinearLightProbeSH");
            Prop("_BakedSpecular");

#if BAKERY_INCLUDED
            EditorGUILayout.Space();
            Prop("Bakery");

            if (GetProperty("Bakery").floatValue != 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                Prop("_RNM0");
                Prop("_RNM1");
                Prop("_RNM2");
                EditorGUI.EndDisabledGroup();
            }
#endif


            EditorGUILayout.LabelField("Rendering Options", EditorStyles.boldLabel);
            materialEditor.DoubleSidedGIField();
            materialEditor.EnableInstancingField();
            materialEditor.RenderQueueField();
            Prop("_Cull");
        }

        private void SetupBlendMode(MaterialEditor materialEditor)
        {
            foreach (Material m in materialEditor.targets)
            {
                SetupMaterialWithBlendMode(m, (int) GetProperty("_Mode").floatValue);
            }
        }


        // On inspector change
        private void ApplyChanges(MaterialProperty[] props, MaterialEditor materialEditor)
        {
            SetupGIFlags(GetProperty("_EnableEmission").floatValue, _material);
            SetupBlendMode(materialEditor);
        }

        MaterialEditor _materialEditor;
        private bool m_FirstTimeApply = true;

        Material _material = null;

        MaterialProperty[] _allProps = null;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            _materialEditor = materialEditor;
            _material = materialEditor.target as Material;
            _allProps = props;
            

            if (m_FirstTimeApply)
            {
                m_FirstTimeApply = false;
            }

            EditorGUI.BeginChangeCheck();

            ShaderPropertiesGUI(_material, props, materialEditor);

            if (EditorGUI.EndChangeCheck()) {
                ApplyChanges(props, materialEditor);
            };
        }

        private static void SetupMaterialWithBlendMode(Material material, int type)
        {
            switch (type)
            {
                case 0:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_AlphaToMask", 0);
                    material.renderQueue = -1;
                    break;
                case 1:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetInt("_AlphaToMask", 1);
                    break;
                case 2:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetInt("_AlphaToMask", 0);
                    break;
                case 3:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetInt("_AlphaToMask", 0);
                    break;
            }
        }

        private void Prop(string property, string extraProperty = null) => MaterialProp(GetProperty(property), extraProperty is null ? null : GetProperty(extraProperty), _materialEditor, false, _material);
        private void PropTileOffset(string property) => DrawPropTileOffset(GetProperty(property), false, _materialEditor, _material);
        private float GetFloatValue(string name) => (float)GetProperty(name)?.floatValue;
        private bool IfProp(string name) => GetProperty(name)?.floatValue == 1;

        private void RangedProp(MaterialProperty min, MaterialProperty max, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null)
        {
            float currentMin = min.floatValue;
            float currentMax = max.floatValue;
            EditorGUILayout.BeginHorizontal();

            if(tex is null)
                EditorGUILayout.LabelField(max.displayName);
            else
                _materialEditor.TexturePropertySingleLine(new GUIContent(tex.displayName), tex);


            EditorGUI.indentLevel -= 4;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref currentMin,ref currentMax, minLimit, maxLimit);
            if(EditorGUI.EndChangeCheck())
            {
                min.floatValue = currentMin;
                max.floatValue = currentMax;
            }
            EditorGUI.indentLevel += 4;
            EditorGUILayout.EndHorizontal();
            HandleMouseEvents(max, _material, min.name);
        }

        private MaterialProperty GetProperty(string name)
        {
            MaterialProperty p = System.Array.Find(_allProps, x => x.name == name);
            return p;
        }
    }
}