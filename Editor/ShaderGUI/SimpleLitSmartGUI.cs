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

        private MaterialProperty _MainTex;
        private MaterialProperty _Color;

        private MaterialProperty _Metallic;
        private MaterialProperty _Glossiness;
        private MaterialProperty _MetallicMin;
        private MaterialProperty _GlossinessMin;
        private MaterialProperty _Occlusion;

        private MaterialProperty _MetallicGlossMap;

        private MaterialProperty _BumpMap;
        private MaterialProperty _BumpScale;
        private MaterialProperty _FlipNormal;
        private MaterialProperty _EnableEmission;
        private MaterialProperty _EmissionMap;
        private MaterialProperty _EmissionColor;
        private MaterialProperty _EmissionMultBase;
        private MaterialProperty _EmissionGIMultiplier;

        private MaterialProperty Bakery;
        private MaterialProperty _GlossyReflections;
        private MaterialProperty _SpecularHighlights;
        private MaterialProperty _GSAA;
        private MaterialProperty _specularAntiAliasingVariance;
        private MaterialProperty _specularAntiAliasingThreshold;
        private MaterialProperty _NonLinearLightProbeSH;
        private MaterialProperty _BakedSpecular;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private MaterialProperty _LTCGI;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private MaterialProperty _LTCGI_DIFFUSE_OFF;
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
            DrawRenderingOptions(material, materialEditor);
        }

        

        private void DrawSurfaceInputs(Material material, MaterialEditor me)
        {
            EditorGUILayout.Space();

            Draw(_MainTex, _Color);
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
            me.TextureScaleOffsetProperty(_MainTex);
            EditorGUILayout.Space();

        }

        private void DrawEmissionMaps(Material material, MaterialEditor me)
        {

            Draw(_EnableEmission);
            if (_EnableEmission.floatValue == 1)
            {
                Draw(_EmissionMap, _EmissionColor);
                EditorGUI.indentLevel += 2;
                Draw(_EmissionMultBase);
                me.LightmapEmissionProperty();
                Draw(_EmissionGIMultiplier, "Multiplies baked and realtime emission");
                EditorGUI.indentLevel -= 2;
            }
            EditorGUILayout.Space();
        }
        private void DrawRenderingOptions(Material material, MaterialEditor me)
        {
            Draw(_GlossyReflections);
            Draw(_SpecularHighlights);
            Draw(_GSAA, "Reduces specular shimmering");
            if (_GSAA.floatValue == 1)
            {
                EditorGUI.indentLevel += 1;
                Draw(_specularAntiAliasingVariance);
                Draw(_specularAntiAliasingThreshold);
                EditorGUI.indentLevel -= 1;
            }
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


        public const string ShaderName = "Lit/Simple";
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
            SetupMaterialWithBlendMode(m, 0);
            ApplyChanges(m);
        }

        public override void OnValidate(Material material)
        {
            ApplyChanges(material);
        }

        public static void ApplyChanges(Material m)
        {
            SetupGIFlags(m.GetFloat("_EnableEmission"), m);

            int bakeryMode = (int)m.GetFloat("Bakery");
            m.ToggleKeyword("BAKERY_RNM", bakeryMode == 2);
            m.ToggleKeyword("BAKERY_SH", bakeryMode == 1);

#if !LTCGI_INCLUDED
            m.SetFloat("_LTCGI", 0f);
            m.DisableKeyword("LTCGI");
#endif
        }
    }
}
