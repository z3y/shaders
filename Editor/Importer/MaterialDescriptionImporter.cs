using System;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace z3y.Shaders
{
    public class MaterialDescriptionImporter : AssetPostprocessor
    {
        public override int GetPostprocessOrder()
        {
            return 2;
        }

        public static Shader _defaultShader;
        private static Shader DefaultShader
        {
            get
            {
                if (_defaultShader == null)
                {
                    _defaultShader = Shader.Find("Lit Variants/Default");
                }
                return _defaultShader;
            }
        }

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] materialAnimation)
        {
            var labels = AssetDatabase.GetLabels(assetImporter);
            bool useScriptedImporterShader = false;
            if (Array.Exists(labels, x => x.Equals("SetupLitShader", StringComparison.OrdinalIgnoreCase)))
            {
                useScriptedImporterShader = true;
            }

            if (!ProjectSettings.ShaderSettings.defaultShader && !useScriptedImporterShader)
            {
                return;
            }

            material.shader = useScriptedImporterShader ? DefaultShader : ProjectSettings.lit;


            if (description.TryGetProperty("DiffuseColor", out Vector4 color))
            {
                if (description.TryGetProperty("DiffuseFactor", out float diffuseFactor))
                {
                    color *= diffuseFactor;
                }

                float gValue = Mathf.LinearToGammaSpace(color.y);
                float rValue = Mathf.LinearToGammaSpace(color.x);
                float bValue = Mathf.LinearToGammaSpace(color.z);

                color.x = rValue;
                color.y = gValue;
                color.z = bValue;

                material.SetColor("_Color", color);
            }

            Texture mainTexUnity = null;
            if (description.TryGetProperty("DiffuseColor", out TexturePropertyDescription mainTex))
            {
                material.SetTexture("_MainTex", mainTex.texture);
                material.SetColor("_Color", Color.white);
                mainTexUnity = mainTex.texture;
            }

            if (description.TryGetProperty("TransparencyFactor", out float transparencyFactor))
            {
                var albedoColor = material.GetVector("_Color");
                var newColor = new Vector4(albedoColor.x, albedoColor.y, albedoColor.z, 1 - transparencyFactor);
                material.SetColor("_Color", newColor);
            }
            else
            {
                material.SetFloat("_Mode", 0);
            }

            if (description.TryGetProperty("EmissiveColor", out Vector4 emissiveColor))
            {
                if (description.TryGetProperty("EmissiveFactor", out float emissiveFactor) && emissiveFactor > 0 && (emissiveColor.x + emissiveColor.y + emissiveColor.z) > 0)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetFloat("_EmissionToggle", 1f);
                    material.SetFloat("Foldout_Emission", 1f);
                    material.SetColor("_EmissionColor", emissiveColor * emissiveFactor);
                }
            }

            if (description.TryGetProperty("ReflectionFactor", out float metallic))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (description.TryGetProperty("SpecularFactor", out float reflectance))
            {
                material.SetFloat("_Reflectance", reflectance * 2);
            }

            if (description.TryGetProperty("Shininess", out float shininessFactor))
            {
                var smoothness = Mathf.Sqrt(shininessFactor * 0.01f);
                if (useScriptedImporterShader)
                {
                    material.SetFloat("_Roughness", 1.0f - smoothness);
                }
                else
                {
                    material.SetFloat("_Glossiness", smoothness);
                }
            }

            int mode = (int)material.GetFloat("_Mode");
            if (useScriptedImporterShader)
            {
                DefaultInspector.SetupMaterialWithBlendMode(material, mode);
                DefaultInspector.SetupTransparencyKeywords(material, mode);

                if (material.GetTexture("_BumpMap") != null)
                {
                    material.EnableKeyword("_NORMALMAP");
                }
            }
            else
            {
                BaseShaderGUI.SetupMaterialWithBlendMode(material, mode);
                LitGUI.ApplyChanges(material);
            }
        }

    }
}