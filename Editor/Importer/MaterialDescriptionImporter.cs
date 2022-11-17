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
                material.SetFloat("_Glossiness", smoothness);
            }

            var textureProps = new List<string>();
            
            /*
            description.GetTexturePropertyNames(textureProps);
            
            if (mainTexUnity && description.TryGetProperty("ShininessExponent", out TexturePropertyDescription roughnessTexture))
            {
                Debug.Log(roughnessTexture.path);
                string roughnessPath = AssetDatabase.GetAssetPath(roughnessTexture.texture);
                string albedoPath = AssetDatabase.GetAssetPath(mainTexUnity);
                string newName = Path.GetFileNameWithoutExtension(albedoPath) + Path.GetFileNameWithoutExtension(roughnessPath) + "." + FreeImagePacking.PackingFormat.GetExtension();
                string newPath = "Assets/" + newName;
                Texture2D newTexture;
                newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
                
                if (!newTexture)
                {
                    FreeImagePacking.PackAlbedoAlpha(newPath, albedoPath, roughnessPath, FreeImagePacking.ChannelSource.Red, true);
                    AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);
                    _packedTextures.Add(new PackedTexture(material, "_MainTex", newPath));
                }
                else
                {
                    material.SetTexture("_MainTex", newTexture);
                }
                material.SetFloat("_SmoothnessAlbedoAlpha", 1f);
            }
            */

        


            LitGUI.ApplyChanges(material);
        }

        
        /*private static List<PackedTexture> _packedTextures = new List<PackedTexture>();
        private static void ApplyPackedTextures()
        {
            for (var i = 0; i < _packedTextures.Count; i++)
            {
                var applyTexture = _packedTextures[i];
                var packedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(applyTexture.Path);
                if (!packedTexture)
                {
                    continue;
                }
                applyTexture.Material.SetTexture(applyTexture.PropertyName, packedTexture);
            }
            
            _packedTextures.Clear();
        }

        private struct PackedTexture
        {
            public Material Material;
            public string PropertyName;
            public string Path;

            public PackedTexture(Material material, string propertyName, string path)
            {
                Material = material;
                PropertyName = propertyName;
                Path = path;
            }
        }*/
    }
}