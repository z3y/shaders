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

        private MaterialProperty _BakeryAlphaDither;
        private MaterialProperty _GlossyReflections;
        private MaterialProperty _SpecularHighlights;
        private MaterialProperty _BAKERY_MONOSH;
        private MaterialProperty _BICUBIC_LIGHTMAP;
        private MaterialProperty _GEOMETRICSPECULAR_AA;
        private MaterialProperty _LIGHTMAPPED_SPECULAR;
        private MaterialProperty _ANISOTROPY;

        private static bool _foldout0 = true;
        private static bool _foldout1 = true;
        private static bool _foldout2 = true;

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            foreach (var keyword in material.shaderKeywords)
            {
                material.DisableKeyword(keyword);
            }
            MaterialEditor.ApplyMaterialPropertyDrawers(material);
            SetupMaterialWithBlendMode(material, (int)material.GetFloat("_Mode"));
            ApplyChanges(material);
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
        }

        private bool _firstTime = true;
        private Shader _shader;

        public override void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            _shader = material.shader;

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
                    if (materialProperties[i].flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                    {
                        continue;
                    }

                    if (materialProperties[i].type == MaterialProperty.PropType.Texture)
                    {
                        bool isSrgb = materialProperties[i].displayName.Contains("sRGB");
                        string displayName = materialProperties[i].displayName;
                        if (isSrgb)
                        {
                            displayName = displayName.Replace("sRGB", string.Empty);
                        }
                        float fieldWidth = EditorGUIUtility.fieldWidth;
                        float labelWidth = EditorGUIUtility.labelWidth;
                        materialEditor.SetDefaultGUIWidths();
                        materialEditor.TextureProperty(materialProperties[i], displayName);
                        EditorGUIUtility.fieldWidth = fieldWidth;
                        EditorGUIUtility.labelWidth = labelWidth;
                        if (isSrgb)
                        {
                            sRGBWarning(materialProperties[i]);
                        }
                        
                        continue;
                    }
                    Draw(materialProperties[i]);
                }
            }
            
            if (Foldout(ref _foldout2, "Additional Settings"))
            {
                Draw(_BAKERY_MONOSH);
                Draw(_BICUBIC_LIGHTMAP);
                Draw(_GEOMETRICSPECULAR_AA);
                Draw(_LIGHTMAPPED_SPECULAR);
                Draw(_ANISOTROPY);
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
