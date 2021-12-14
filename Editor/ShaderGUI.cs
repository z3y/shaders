using UnityEditor;
using UnityEngine;
using static z3y.Shaders.SimpleLit.Helpers;

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

            Prop("_MetallicGlossMap");
            sRGBWarning(GetProperty("_MetallicGlossMap"));
            EditorGUI.indentLevel+=2;
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
            EditorGUI.indentLevel-=2;

            


            Prop("_BumpMap", "_BumpScale");




            Prop("_EnableEmission");
            if (IfProp("_EnableEmission"))
            {
                Prop("_EmissionMap", "_EmissionColor");
                EditorGUI.indentLevel+=2;
                Prop("_EmissionMultBase");
                materialEditor.LightmapEmissionProperty();
                EditorGUI.indentLevel-=2;
                EditorGUILayout.Space();
            }

            Prop("_EnableParallax");
            if (IfProp("_EnableParallax"))
            {
                Prop("_ParallaxMap", "_Parallax");
                EditorGUI.indentLevel+=2;
                Prop("_ParallaxOffset");
                Prop("_ParallaxSteps");
                EditorGUI.indentLevel-=2;;
            }

            sRGBWarning(GetProperty("_ParallaxMap"));
            
            EditorGUILayout.Space();
            PropTileOffset("_MainTex");



            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Detail Inputs", EditorStyles.boldLabel);
            if(IfProp("_DetailAlbedoMap"))
            {
                Prop("_DetailAlbedoMap", "_DetailAlbedoScale");
                EditorGUI.indentLevel+=2;
                Prop("_DetailSmoothnessScale");
                EditorGUI.indentLevel-=2;
            }
            else
            {
                Prop("_DetailAlbedoMap");
            }
            if(IfProp("_DetailNormalMap"))
            {
                Prop("_DetailNormalMap","_DetailNormalScale");
            }
            else
            {
                Prop("_DetailNormalMap");

            }
            if(IfProp("_DetailNormalMap") || IfProp("_DetailAlbedoMap"))
            {
                Prop("_DetailMap_UV");
                PropTileOffset("_DetailAlbedoMap");
            }



            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rendering Options", EditorStyles.boldLabel);
            Prop("_GlossyReflections");
            Prop("_SpecularHighlights");
            EditorGUILayout.Space();


            Prop("_GSAA");
            if (IfProp("_GSAA"))
            {
                EditorGUI.indentLevel += 1;
                Prop("_specularAntiAliasingVariance");
                Prop("_specularAntiAliasingThreshold");
                EditorGUI.indentLevel -= 1;
            }

            Prop("_NonLinearLightProbeSH");
            Prop("_BakedSpecular");
            Prop("_SpecularOcclusion");


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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
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
        private void ApplyChanges(MaterialProperty[] props, MaterialEditor materialEditor, Material mat)
        {
            SetupGIFlags(GetProperty("_EnableEmission").floatValue, _material);
            SetupBlendMode(materialEditor);
            
            mat.DisableKeyword("BAKERY_NONE");
            mat.DisableKeyword("_MODE_OPAQUE");
            
            ToggleKeyword("_MASK_MAP", IfProp("_MetallicGlossMap"), mat);
            ToggleKeyword("_NORMAL_MAP", IfProp("_BumpMap"), mat);
            ToggleKeyword("_DETAILALBEDO_MAP", IfProp("_DetailAlbedoMap"), mat);
            ToggleKeyword("_DETAILNORMAL_MAP", IfProp("_DetailNormalMap"), mat);
        }

        private void ToggleKeyword(string keyword, bool toggle, Material mat)
        {
            if(toggle)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
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
                SetupBlendMode(materialEditor);
                ApplyChanges(props, materialEditor, _material);
            }

            EditorGUI.BeginChangeCheck();

            ShaderPropertiesGUI(_material, props, materialEditor);

            if (EditorGUI.EndChangeCheck()) {
                ApplyChanges(props, materialEditor, _material);
            };
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            if (newShader.name == "Simple Lit")
            {
                foreach (var keyword in material.shaderKeywords)
                {
                    material.DisableKeyword(keyword);
                    MaterialEditor.ApplyMaterialPropertyDrawers(material);
                }
            }
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

        private void Prop(string property, string extraProperty = null) => MaterialProp(GetProperty(property), extraProperty is null ? null : GetProperty(extraProperty), _materialEditor);
        private void PropTileOffset(string property) => DrawPropTileOffset(GetProperty(property), _materialEditor);
        private float GetFloatValue(string name) => (float)GetProperty(name)?.floatValue;
        private bool IfProp(string name)
        {
            MaterialProperty property = GetProperty(name);
            if (property.textureValue != null) return true;
            return property.floatValue == 1;
        }

        private void RangedProp(MaterialProperty min, MaterialProperty max, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null)
        {
            float currentMin = min.floatValue;
            float currentMax = max.floatValue;
            EditorGUILayout.BeginHorizontal();

            if(tex is null)
                EditorGUILayout.LabelField(max.displayName);
            else
                _materialEditor.TexturePropertySingleLine(new GUIContent(tex.displayName), tex);


            EditorGUI.indentLevel -= 6;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref currentMin,ref currentMax, minLimit, maxLimit);
            if(EditorGUI.EndChangeCheck())
            {
                min.floatValue = currentMin;
                max.floatValue = currentMax;
            }
            EditorGUI.indentLevel += 6;
            EditorGUILayout.EndHorizontal();
        }

        private MaterialProperty GetProperty(string name)
        {
            MaterialProperty p = System.Array.Find(_allProps, x => x.name == name);
            return p;
        }
    }
}