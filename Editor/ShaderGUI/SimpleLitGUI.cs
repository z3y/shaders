using System.IO;
using UnityEditor;
using UnityEngine;
using static z3y.Shaders.GUIHelpers;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace z3y.Shaders
{

    public class SimpleLitGUI : ShaderGUI
    {

        private MaterialProperty _MainTex = null;
        private MaterialProperty _Mode = null;
        private MaterialProperty _Cutoff = null;
        private MaterialProperty _Texture = null;
        private MaterialProperty _MainTexArray = null;
        private MaterialProperty _Color = null;
        private MaterialProperty _MetallicGlossMapArray = null;
        private MaterialProperty _Metallic = null;
        private MaterialProperty _Glossiness = null;
        private MaterialProperty _MetallicMin = null;
        private MaterialProperty _GlossinessMin = null;
        private MaterialProperty _Occlusion = null;
        private MaterialProperty _BumpMapArray = null;
        private MaterialProperty _BumpScale = null;
        private MaterialProperty _MetallicGlossMap = null;
        private MaterialProperty _IsPackingMetallicGlossMap = null;
        private MaterialProperty _MetallicMap = null;
        private MaterialProperty _MetallicMapChannel = null;
        private MaterialProperty _OcclusionMap = null;
        private MaterialProperty _OcclusionMapChannel = null;
        private MaterialProperty _DetailMaskMap = null;
        private MaterialProperty _DetailMaskMapChannel = null;
        private MaterialProperty _SmoothnessMap = null;
        private MaterialProperty _SmoothnessMapChannel = null;
        private MaterialProperty _SmoothnessMapInvert = null;
        private MaterialProperty _BumpMap = null;
        private MaterialProperty _EnableEmission = null;
        private MaterialProperty _EmissionMap = null;
        private MaterialProperty _EmissionColor = null;
        private MaterialProperty _EmissionMultBase = null;
        private MaterialProperty _EnableParallax = null;
        private MaterialProperty _Parallax = null;
        private MaterialProperty _ParallaxMap = null;
        private MaterialProperty Bakery = null;
        private MaterialProperty _GlossyReflections = null;
        private MaterialProperty _SpecularHighlights = null;
        private MaterialProperty _Reflectance = null;
        private MaterialProperty _GSAA = null;
        private MaterialProperty _specularAntiAliasingVariance = null;
        private MaterialProperty _specularAntiAliasingThreshold = null;
        private MaterialProperty _NonLinearLightProbeSH = null;
        private MaterialProperty _BakedSpecular = null;
        private MaterialProperty _AlbedoSaturation = null;
        private MaterialProperty _RNM0 = null;
        private MaterialProperty _RNM1 = null;
        private MaterialProperty _RNM2 = null;
        private MaterialProperty _Cull = null;
        private MaterialProperty _ParallaxOffset = null;
        private MaterialProperty _ParallaxSteps = null;
        private MaterialProperty _DetailAlbedoMap = null;
        private MaterialProperty _DetailNormalMap = null;
        private MaterialProperty _DetailNormalScale = null;
        private MaterialProperty _DetailMapUV = null;
        private MaterialProperty _DetailAlbedoScale = null;
        private MaterialProperty _DetailSmoothnessScale = null;
        private MaterialProperty _SpecularOcclusion = null;
        private MaterialProperty _DetailMaskMapInvert = null;
        private MaterialProperty _OcclusionMapInvert = null;
        private MaterialProperty _MetallicMapInvert = null;
        private MaterialProperty _QueueOffset = null;
        private MaterialProperty _AudioLinkEmission = null;



        private void DrawProperties(Material material, MaterialProperty[] props, MaterialEditor me)
        {

            EditorGUI.BeginChangeCheck();
            Prop(_Mode);
            if (EditorGUI.EndChangeCheck())
            {
                SetupBlendMode(me);
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
                    me.DrawRangedProperty(_MetallicMin, _Metallic);
                    me.DrawRangedProperty(_GlossinessMin, _Glossiness);
                    Prop(_Occlusion);
                }
                EditorGUI.indentLevel-=2;
                Prop(_BumpMapArray, _BumpScale);
            }
            else
            {
                Prop(_MainTex, _Color, _AlbedoSaturation);
                Prop(_MetallicGlossMap);
                sRGBWarning(_MetallicGlossMap);

                _IsPackingMetallicGlossMap.floatValue = TextureFoldout(_IsPackingMetallicGlossMap.floatValue == 1) ? 1 : 0;
                if(_IsPackingMetallicGlossMap.floatValue == 1)
                {
                    //texture packing
                    PropertyGroup(()=>
                    {
                        Prop(_MetallicMap, _MetallicMapChannel, _MetallicMapInvert);
                        Prop(_OcclusionMap, _OcclusionMapChannel, _OcclusionMapInvert);
                        Prop(_DetailMaskMap, _DetailMaskMapChannel, _DetailMaskMapInvert);
                        Prop(_SmoothnessMap, _SmoothnessMapChannel, _SmoothnessMapInvert, _SmoothnessMapInvert.floatValue == 1 ? "Roughness Map" : null);
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Pack"))
                        {
                            if (PackMaskMap()) return;
                        }
                        
                        if (GUILayout.Button("Modify"))
                        {
                            var t = _MetallicGlossMap.textureValue;
                            if (!_MetallicMap.textureValue)
                            {
                                material.SetTexture("_MetallicMap", t);
                                material.SetInt("_MetallicMapChannel", 0);
                                material.SetInt("_MetallicMapInvert", 0);
                            }

                            if (!_OcclusionMap.textureValue)
                            {
                                material.SetTexture("_OcclusionMap", t);
                                material.SetInt("_OcclusionMapChannel", 1);
                                material.SetInt("_OcclusionMapInvert", 0);
                            }

                            if (!_DetailMaskMap.textureValue)
                            {
                                material.SetTexture("_DetailMaskMap", t);
                                material.SetInt("_DetailMaskMapChannel", 2);
                                material.SetInt("_DetailMaskMapInvert", 0);
                            }

                            if (!_SmoothnessMap.textureValue)
                            {
                                material.SetTexture("_SmoothnessMap", t);
                                material.SetInt("_SmoothnessMapChannel", 3);
                                material.SetInt("_SmoothnessMapInvert", 0);
                            }
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
                    me.DrawRangedProperty(_MetallicMin, _Metallic);
                    me.DrawRangedProperty(_GlossinessMin, _Glossiness);
                    Prop(_Occlusion);
                }
                EditorGUI.indentLevel-=2;

                
                Prop(_BumpMap, _BumpScale);
            }


            Prop(_EnableEmission);
            if (_EnableEmission.boolValue())
            {
                Prop(_EmissionMap, _EmissionColor, _EmissionMultBase);
                EditorGUI.indentLevel+=2;
                #if UDON
                Prop(_AudioLinkEmission);
                #endif
                me.LightmapEmissionProperty();
                EditorGUI.indentLevel-=2;
                EditorGUILayout.Space();
            }

            Prop(_EnableParallax);
            if (_EnableParallax.boolValue())
            {
                Prop(_ParallaxMap, _Parallax);
                EditorGUI.indentLevel+=2;
                Prop(_ParallaxOffset);
                Prop(_ParallaxSteps);
                EditorGUI.indentLevel-=2;;
            }

            sRGBWarning(_ParallaxMap);
            
            EditorGUILayout.Space();
            me.TextureScaleOffsetProperty(_MainTex);



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
                me.TextureScaleOffsetProperty(_DetailAlbedoMap);
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
            me.DoubleSidedGIField();
            me.EnableInstancingField();
            me.RenderQueueField();
            if (_Mode.floatValue > 1)
            {
                Prop(_QueueOffset);
            }
        }

        private bool PackMaskMap()
        {
            var rTex = (Texture2D)_MetallicMap.textureValue;
            var gTex = (Texture2D)_OcclusionMap.textureValue;
            var bTex = (Texture2D)_DetailMaskMap.textureValue;
            var aTex = (Texture2D)_SmoothnessMap.textureValue;
            
            var reference = aTex ?? gTex ?? rTex ?? bTex;
            if (reference == null) return true;
            
            var rChannel = new TexturePacking.Channel()
            {
                Tex = rTex,
                ID = (int)_MetallicMapChannel.floatValue,
                Invert = _MetallicMapInvert.floatValue == 1,
                DefaultWhite = false
            };
            
            var gChannel = new TexturePacking.Channel()
            {
                Tex = gTex,
                ID = (int)_OcclusionMapChannel.floatValue,
                Invert = _OcclusionMapInvert.floatValue == 1
            };
            
            var bChannel = new TexturePacking.Channel()
            {
                Tex = bTex,
                ID = (int)_DetailMaskMapChannel.floatValue,
                Invert = _DetailMaskMapInvert.floatValue == 1
            };
            
            var aChannel = new TexturePacking.Channel()
            {
                Tex = aTex,
                ID = (int)_SmoothnessMapChannel.floatValue,
                Invert = _SmoothnessMapInvert.floatValue == 1
            };

            var path = AssetDatabase.GetAssetPath(reference);
            var newPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_Packed";

            TexturePacking.Pack(new []{rChannel, gChannel, bChannel, aChannel}, newPath, reference.width, reference.height);
            var packedTexture = TexturePacking.GetPackedTexture(newPath);
            TexturePacking.DisableSrgb(packedTexture);
            _MetallicGlossMap.textureValue = packedTexture;
            return false;
        }

        private void SetupBlendMode(MaterialEditor materialEditor)
        {
            foreach (var o in materialEditor.targets)
            {
                var m = (Material) o;
                SetupMaterialWithBlendMode(m, (int) _Mode.floatValue);
            }
        }


        // On inspector change
        private void ApplyChanges(MaterialEditor materialEditor, Material mat)
        {
            SetupGIFlags(_EnableEmission.floatValue, _material);
            
            SetupBlendMode(materialEditor);
            mat.ToggleKeyword("_MODE_CUTOUT", _Mode.floatValue == 1);
            mat.ToggleKeyword("_MODE_FADE", _Mode.floatValue == 2);
            mat.ToggleKeyword("_ALPHAPREMULTIPLY_ON", _Mode.floatValue == 3);
            mat.ToggleKeyword("_ALPHAMODULATE_ON", _Mode.floatValue == 5);

            var samplingMode = _Texture.floatValue;
            mat.ToggleKeyword("_TEXTURE_ARRAY", samplingMode == 1 || samplingMode == 2);
            
            
            mat.ToggleKeyword("AUDIOLINK", _AudioLinkEmission.floatValue != 1000);
            
            
            
            if(_Texture.floatValue == 1 || _Texture.floatValue == 2)
            {
                mat.ToggleKeyword("_MASK_MAP", _MetallicGlossMapArray.textureValue);
                mat.ToggleKeyword("_NORMAL_MAP", _BumpMapArray.textureValue);
            }
            else
            {
                mat.ToggleKeyword("_MASK_MAP", _MetallicGlossMap.textureValue);
                mat.ToggleKeyword("_NORMAL_MAP", _BumpMap.textureValue);
            }
            mat.ToggleKeyword("_DETAILALBEDO_MAP", _DetailAlbedoMap.textureValue);
            mat.ToggleKeyword("_DETAILNORMAL_MAP", _DetailNormalMap.textureValue);
        }
        
        private MaterialEditor _materialEditor;
        private bool _firstTimeApply = true;
        private Material _material = null;
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            _materialEditor = materialEditor;
            _material = materialEditor.target as Material;
            InitializeAllProperties(props, this, FindProperty);

            if (_firstTimeApply)
            {
                _firstTimeApply = false;
                SetupBlendMode(materialEditor);
                ApplyChanges(materialEditor, _material);
            }


            EditorGUI.BeginChangeCheck();

            DrawProperties(_material, props, materialEditor);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyChanges(materialEditor, _material);
            };
        }

        private void Prop(MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string nameOverride = null) => _materialEditor.DrawMaterialProperty(property, extraProperty, extraProperty2, nameOverride);
    }
}