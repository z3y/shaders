using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] materialAnimation)
        {
            if (!ProjectSettings.ShaderSettings.defaultShader)
            {
                return;
            }

            material.shader = ProjectSettings.lit;


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

            if (description.TryGetProperty("EmissiveColor", out Vector4 emissiveColor))
            {
                if (description.TryGetProperty("EmissiveColor", out TexturePropertyDescription emissionTex))
                {
                    material.SetFloat("_EmissionToggle", 1f);
                    material.SetFloat("Foldout_Emission", 1f);
                    material.EnableKeyword("_EMISSION");
                }

                if (description.TryGetProperty("EmissiveFactor", out float emissiveFactor) && emissiveFactor > 0 && (emissiveColor.x + emissiveColor.y + emissiveColor.z) > 0)
                {
                    material.SetFloat("_EmissionToggle", 1f);
                    material.SetFloat("Foldout_Emission", 1f);
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emissiveColor * emissiveFactor);
                }
            }

            if (description.TryGetProperty("ReflectionFactor", out float metallic))
            {
                material.SetFloat("_Metallic", metallic);
            }

            /*
            if (description.TryGetProperty("SpecularFactor", out float reflectance))
            {
                material.SetFloat("_Reflectance", reflectance * 2);
            }
            */

            if (description.TryGetProperty("Shininess", out float shininessFactor))
            {
                var smoothness = Mathf.Sqrt(shininessFactor * 0.01f);
                material.SetFloat("_Glossiness", smoothness);
            }
            

            SmartGUI.SetupMaterialWithBlendMode(material, (int)material.GetFloat("_Mode"));
            LitGUI.ApplyChanges(material);
        }
    }
}