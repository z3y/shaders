using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static z3y.Shaders.Helpers;
using static z3y.Shaders.TexturePacking;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace z3y.Shaders
{

    public class SimpleLitGUI : ShaderGUI
    {

        protected MaterialProperty _MainTex = null;
        protected MaterialProperty _Mode = null;
        protected MaterialProperty _Cutoff = null;
        protected MaterialProperty _Texture = null;
        protected MaterialProperty _MainTexArray = null;
        protected MaterialProperty _Color = null;
        protected MaterialProperty _MetallicGlossMapArray = null;
        protected MaterialProperty _Metallic = null;
        protected MaterialProperty _Glossiness = null;
        protected MaterialProperty _MetallicMin = null;
        protected MaterialProperty _GlossinessMin = null;
        protected MaterialProperty _Occlusion = null;
        protected MaterialProperty _BumpMapArray = null;
        protected MaterialProperty _BumpScale = null;
        protected MaterialProperty _MetallicGlossMap = null;
        protected MaterialProperty _IsPackingMetallicGlossMap = null;
        protected MaterialProperty _MetallicMap = null;
        protected MaterialProperty _MetallicMapChannel = null;
        protected MaterialProperty _OcclusionMap = null;
        protected MaterialProperty _OcclusionMapChannel = null;
        protected MaterialProperty _DetailMaskMap = null;
        protected MaterialProperty _DetailMaskMapChannel = null;
        protected MaterialProperty _SmoothnessMap = null;
        protected MaterialProperty _SmoothnessMapChannel = null;
        protected MaterialProperty _SmoothnessMapInvert = null;
        protected MaterialProperty _BumpMap = null;
        protected MaterialProperty _EnableEmission = null;
        protected MaterialProperty _EmissionMap = null;
        protected MaterialProperty _EmissionColor = null;
        protected MaterialProperty _EmissionMultBase = null;
        protected MaterialProperty _EnableParallax = null;
        protected MaterialProperty _Parallax = null;
        protected MaterialProperty _ParallaxMap = null;
        protected MaterialProperty Bakery = null;
        protected MaterialProperty _GlossyReflections = null;
        protected MaterialProperty _SpecularHighlights = null;
        protected MaterialProperty _Reflectance = null;
        protected MaterialProperty _GSAA = null;
        protected MaterialProperty _specularAntiAliasingVariance = null;
        protected MaterialProperty _specularAntiAliasingThreshold = null;
        protected MaterialProperty _NonLinearLightProbeSH = null;
        protected MaterialProperty _BakedSpecular = null;
        protected MaterialProperty _AlbedoSaturation = null;
        protected MaterialProperty _RNM0 = null;
        protected MaterialProperty _RNM1 = null;
        protected MaterialProperty _RNM2 = null;
        protected MaterialProperty _Cull = null;
        protected MaterialProperty _ParallaxOffset = null;
        protected MaterialProperty _ParallaxSteps = null;
        protected MaterialProperty _DetailAlbedoMap = null;
        protected MaterialProperty _DetailNormalMap = null;
        protected MaterialProperty _DetailNormalScale = null;
        protected MaterialProperty _DetailMapUV = null;
        protected MaterialProperty _DetailAlbedoScale = null;
        protected MaterialProperty _DetailSmoothnessScale = null;
        protected MaterialProperty _SpecularOcclusion = null;



        private void ShaderPropertiesGUI(Material material, MaterialProperty[] props, MaterialEditor materialEditor)
        {

            EditorGUI.BeginChangeCheck();
            Prop(_Mode);
            if (EditorGUI.EndChangeCheck())
            {
                SetupBlendMode(materialEditor);
            }

            if (_Mode.floatValue == 1) Prop(_Cutoff);

            EditorGUILayout.Space();

            if(_Texture.floatValue == 1 || _Texture.floatValue == 2)
            {
                Prop(_MainTexArray, _Color);
                Prop(_MetallicGlossMapArray);

                EditorGUI.indentLevel+=2;
                if (_MetallicGlossMapArray.textureValue == null)
                {
                    Prop(_Metallic);
                    Prop(_Glossiness);
                }
                else
                {
                    RangedProp(_MetallicMin, _Metallic);
                    RangedProp(_GlossinessMin, _Glossiness);
                    Prop(_Occlusion);
                }
                EditorGUI.indentLevel-=2;
                Prop(_BumpMapArray, _BumpScale);
            }
            else
            {
                Prop(_MainTex, _Color);
                Prop(_MetallicGlossMap);
                sRGBWarning(_MetallicGlossMap);

                _IsPackingMetallicGlossMap.floatValue = TextureFoldout(_IsPackingMetallicGlossMap.floatValue == 1) ? 1 : 0;
                if(_IsPackingMetallicGlossMap.floatValue == 1)
                {
                    //texture packing
                    PropertyGroup(()=>
                    {
                        Prop(_MetallicMap, _MetallicMapChannel);
                        Prop(_OcclusionMap, _OcclusionMapChannel);
                        Prop(_DetailMaskMap, _DetailMaskMapChannel);
                        Prop(_SmoothnessMap, _SmoothnessMapChannel);
                        Prop(_SmoothnessMapInvert);
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Pack"))
                        {
                            if (PackMaskMap()) return;
                        }

                        if (GUILayout.Button("Close"))
                        {
                            _MetallicMap.textureValue = null;
                            _OcclusionMap.textureValue = null;
                            _DetailMaskMap.textureValue = null;
                            _SmoothnessMap.textureValue = null;
                            _IsPackingMetallicGlossMap.floatValue = 0;
                        }
                        EditorGUILayout.EndHorizontal();
                    });
                }

                EditorGUI.indentLevel+=2;
                if (_MetallicGlossMap.textureValue == null)
                {
                    Prop(_Metallic);
                    Prop(_Glossiness);
                }
                else
                {
                    RangedProp(_MetallicMin, _Metallic);
                    RangedProp(_GlossinessMin, _Glossiness);
                    Prop(_Occlusion);
                }
                EditorGUI.indentLevel-=2;

                
                Prop(_BumpMap, _BumpScale);
            }


            Prop(_EnableEmission);
            if (_EnableEmission.floatValue == 1)
            {
                Prop(_EmissionMap, _EmissionColor);
                EditorGUI.indentLevel+=2;
                Prop(_EmissionMultBase);
                materialEditor.LightmapEmissionProperty();
                EditorGUI.indentLevel-=2;
                EditorGUILayout.Space();
            }

            Prop(_EnableParallax);
            if (_EnableParallax.floatValue == 1)
            {
                Prop(_ParallaxMap, _Parallax);
                EditorGUI.indentLevel+=2;
                Prop(_ParallaxOffset);
                Prop(_ParallaxSteps);
                EditorGUI.indentLevel-=2;;
            }

            sRGBWarning(_ParallaxMap);
            
            EditorGUILayout.Space();
            materialEditor.TextureScaleOffsetProperty(_MainTex);



            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Detail Inputs", EditorStyles.boldLabel);
            if(_DetailAlbedoMap.textureValue)
            {
                Prop(_DetailAlbedoMap, _DetailAlbedoScale);
                EditorGUI.indentLevel+=2;
                Prop(_DetailSmoothnessScale);
                EditorGUI.indentLevel-=2;
            }
            else
            {
                Prop(_DetailAlbedoMap);
            }
            
            if(_DetailNormalMap.textureValue)
            {
                Prop(_DetailNormalMap, _DetailNormalScale);
            }
            else
            {
                Prop(_DetailNormalMap);
            }

            if(_DetailNormalMap.textureValue || _DetailAlbedoMap.textureValue)
            {
                Prop(_DetailMapUV);
                materialEditor.TextureScaleOffsetProperty(_DetailAlbedoMap);
            }



            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rendering Options", EditorStyles.boldLabel);
            Prop(_GlossyReflections);
            Prop(_SpecularHighlights);
            Prop(_Reflectance);
            Prop(_SpecularOcclusion);

            EditorGUILayout.Space();

            
            Prop(_GSAA);
            if (_GSAA.floatValue == 1)
            {
                EditorGUI.indentLevel += 1;
                Prop(_specularAntiAliasingVariance);
                Prop(_specularAntiAliasingThreshold);
                EditorGUI.indentLevel -= 1;
            }

            Prop(_NonLinearLightProbeSH);
            Prop(_BakedSpecular);
            Prop(_AlbedoSaturation);


#if BAKERY_INCLUDED
            EditorGUILayout.Space();
            Prop(Bakery);

            if (Bakery.floatValue != 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                Prop(_RNM0);
                Prop(_RNM1);
                Prop(_RNM2);
                EditorGUI.EndDisabledGroup();
            }
#endif
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
            Prop(_Texture);
            Prop(_Cull);
            materialEditor.DoubleSidedGIField();
            materialEditor.EnableInstancingField();
            materialEditor.RenderQueueField();
        }

        private bool PackMaskMap()
        {
            ChannelTexture redChannel = new ChannelTexture("Red", (int) _MetallicMapChannel.floatValue);
            ChannelTexture greenChannel = new ChannelTexture("Green", (int) _OcclusionMapChannel.floatValue);
            ChannelTexture blueChannel = new ChannelTexture("Blue", (int) _DetailMaskMapChannel.floatValue);
            ChannelTexture alphaChannel = new ChannelTexture("Alpha", (int) _SmoothnessMapChannel.floatValue);

            redChannel.texture = (Texture2D) _MetallicMap.textureValue;
            greenChannel.texture = (Texture2D) _OcclusionMap.textureValue;
            blueChannel.texture = (Texture2D) _DetailMaskMap.textureValue;
            alphaChannel.texture = (Texture2D) _SmoothnessMap.textureValue;
            alphaChannel.invert = _SmoothnessMapInvert.floatValue == 1;

            Texture2D reference = alphaChannel.texture ?? blueChannel.texture ?? greenChannel.texture ?? redChannel.texture;
            if (reference == null) return true;

            string path = AssetDatabase.GetAssetPath(reference);

            path = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path);

            ChannelTexture[] channelTextures = {redChannel, greenChannel, blueChannel, alphaChannel};
            string newTexturePath = ChannelTexture.PackTexture(channelTextures, path, reference.width, reference.height,
                ChannelTexture.TexEncoding.SaveAsPNG);

            TextureImporter tex = (TextureImporter) AssetImporter.GetAtPath(newTexturePath);
            tex.textureCompression = TextureImporterCompression.Compressed;
            tex.sRGBTexture = false;
            tex.SaveAndReimport();

            _MetallicGlossMap.textureValue = AssetDatabase.LoadAssetAtPath<Texture2D>(newTexturePath);
            return false;
        }

        private void SetupBlendMode(MaterialEditor materialEditor)
        {
            foreach (Material m in materialEditor.targets)
            {
                SetupMaterialWithBlendMode(m, (int) _Mode.floatValue);
            }
        }


        // On inspector change
        private void ApplyChanges(MaterialProperty[] props, MaterialEditor materialEditor, Material mat)
        {
            SetupGIFlags(_EnableEmission.floatValue, _material);
            SetupBlendMode(materialEditor);
            
            mat.DisableKeyword("BAKERY_NONE");
            mat.DisableKeyword("_MODE_OPAQUE");
            mat.DisableKeyword("_TEXTURE_DEFAULT");
            
            if(_Texture.floatValue == 1 || _Texture.floatValue == 2)
            {
                ToggleKeyword("_MASK_MAP", _MetallicGlossMapArray.textureValue, mat);
                ToggleKeyword("_NORMAL_MAP", _BumpMapArray.textureValue, mat);
            }
            else
            {
                ToggleKeyword("_MASK_MAP", _MetallicGlossMap.textureValue, mat);
                ToggleKeyword("_NORMAL_MAP", _BumpMap.textureValue, mat);
            }
            ToggleKeyword("_DETAILALBEDO_MAP", _DetailAlbedoMap.textureValue, mat);
            ToggleKeyword("_DETAILNORMAL_MAP", _DetailNormalMap.textureValue, mat);
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

        protected BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            _materialEditor = materialEditor;
            _material = materialEditor.target as Material;
            FindAllProperties(props);

            if (m_FirstTimeApply)
            {
                m_FirstTimeApply = false;
                SetupBlendMode(materialEditor);
                ApplyChanges(props, materialEditor, _material);
            }

           

            EditorGUI.BeginChangeCheck();

            ShaderPropertiesGUI(_material, props, materialEditor);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyChanges(props, materialEditor, _material);
            };
        }

        private void FindAllProperties(MaterialProperty[] props)
        {
            foreach (var property in GetType().GetFields(bindingFlags))
            {
                if (property.FieldType == typeof(MaterialProperty))
                {
                    try { property.SetValue(this, FindProperty(property.Name, props)); } catch { /*Is it really a problem if it doesn't exist?*/ }
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


        private void Prop(MaterialProperty property, MaterialProperty extraProperty = null) => MaterialProp(property, extraProperty, _materialEditor);

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


    }
}