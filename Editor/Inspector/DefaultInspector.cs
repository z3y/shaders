using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class DefaultInspector : BaseShaderGUI
    {
        private MaterialProperty _Mode;
        private MaterialProperty _ZWrite;
        private MaterialProperty _DstBlend;
        private MaterialProperty _SrcBlend;
        private MaterialProperty _Cull;

        private MaterialProperty _LightmappedSpecular;
        private MaterialProperty _NonLinearLightProbeSH;
        private MaterialProperty _BakeryAlphaDither;
        private MaterialProperty _GlossyReflections;
        private MaterialProperty _SpecularHighlights;


        private static bool _foldout0 = true;
        private static bool _foldout1 = true;
        private static bool _foldout2 = true;

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            foreach (var keyword in material.shaderKeywords)
            {
                material.DisableKeyword(keyword);
                MaterialEditor.ApplyMaterialPropertyDrawers(material);
                ApplyChanges(material);
            }
        }

        public static void ApplyChanges(Material m)
        {
            if (m.HasProperty("_EmissionToggle"))
            {
                SetupGIFlags(m.GetFloat("_EmissionToggle"), m);
            }

            int mode = (int)m.GetFloat("_Mode");
            m.ToggleKeyword("_ALPHATEST_ON", mode == 1);
            m.ToggleKeyword("_ALPHAFADE_ON", mode == 2);
            m.ToggleKeyword("_ALPHAPREMULTIPLY_ON", mode == 3);
            m.ToggleKeyword("_ALPHAMODULATE_ON", mode == 5);

            if (m.FindPass("GrabPass") != -1 && m.renderQueue <= 3000)
            {
                m.renderQueue = 3001;
            }
        }

        public override void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            int startIndex = Array.FindIndex(materialProperties, x => x.name.Equals("_DFG", StringComparison.Ordinal)) + 1;
            if (Foldout(ref _foldout0, "Rendering Options"))
            {
                EditorGUI.BeginChangeCheck();
                Draw(_Mode);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (Material target in materialEditor.targets)
                    {
                        SetupMaterialWithBlendMode(target, (int)_Mode.floatValue);
                        ApplyChanges(target);
                    }
                }
                Draw(_ZWrite);
                Draw(_DstBlend);
                Draw(_SrcBlend);
                Draw(_Cull);

            }

            EditorGUI.BeginChangeCheck();

            if (Foldout(ref _foldout1, "Properties"))
            {
                for (int i = startIndex; i < materialProperties.Length; i++)
                {
                    if (materialProperties[i].type == MaterialProperty.PropType.Texture)
                    {
                        float fieldWidth = EditorGUIUtility.fieldWidth;
                        float labelWidth = EditorGUIUtility.labelWidth;
                        materialEditor.SetDefaultGUIWidths();
                        materialEditor.TextureProperty(materialProperties[i], materialProperties[i].displayName);
                        EditorGUIUtility.fieldWidth = fieldWidth;
                        EditorGUIUtility.labelWidth = labelWidth;
                        continue;
                    }
                    Draw(materialProperties[i]);
                }
            }
            
            if (Foldout(ref _foldout2, "Additional Settings"))
            {
                Draw(_LightmappedSpecular);
                Draw(_NonLinearLightProbeSH);
                Draw(_BakeryAlphaDither);
                Draw(_GlossyReflections);
                Draw(_SpecularHighlights);
            }



            Space();
            DrawSplitter();
            Space();
            materialEditor.LightmapEmissionProperty();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();

            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material target in materialEditor.targets)
                {
                    ApplyChanges(target);
                }
            }
        }
    }
}
