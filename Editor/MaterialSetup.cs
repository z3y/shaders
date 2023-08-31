using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace z3y
{
    public class MaterialSetup
    {
        private static Shader _defaultShader = Shader.Find("Lit Variants/Default");

        [MenuItem("Assets/Create/Material with PBR Setup (Lit)", priority = 301)]
        public static void MenuItem()
        {
            var selectedAsset = GetCurrentAssetDirectory();
            string selectedDirectory = Directory.Exists(selectedAsset) ?
                selectedAsset :
                Path.GetDirectoryName(selectedAsset);

            if (selectedDirectory is null)
            {
                return;
            }

            var setupInstance = new MaterialSetup();
            setupInstance.Setup(selectedDirectory, _defaultShader);
        }

        public static string GetCurrentAssetDirectory()
        {
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (Directory.Exists(path))
                {
                    return path;
                }
                
                if (File.Exists(path))
                {
                    return Path.GetDirectoryName(path);
                }
            }

            return null;
        }

        private Dictionary<PBRTextureType, string> _matchedTextures = new Dictionary<PBRTextureType, string>();
        private List<string> _possibleMaterialNames = new List<string>();

        public void Setup(string directoryPath, Shader shader)
        {
            var files = Directory.GetFiles(directoryPath);

            if (files.Length == 0)
            {
                return;
            }

            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);
                var fileNameNoExt = Path.GetFileNameWithoutExtension(file);
                var searchName = fileNameNoExt.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ');

                var splitSearchName = searchName.Split(' ');
                if (splitSearchName[0].Equals("TexturesCom"))
                {
                    _possibleMaterialNames.Add(splitSearchName[1]);
                }
                else
                {
                    _possibleMaterialNames.Add(splitSearchName[0]);
                }

                CheckMatch(file, splitSearchName);
            }


            if (_matchedTextures.Count == 0)
            {
                return;
            }

            string mostCommon = _possibleMaterialNames.GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;

            var material = new Material(shader)
            {
                name = mostCommon
            };
            string materialPath = Path.Combine(directoryPath, material.name) + ".mat";

            // Debug.Log(string.Join("\n", _matchedTextures));

            FreeImagePackingEditor.ResetFields();
            bool needsPacking = false;
            foreach (var texture in _matchedTextures)
            {
                var textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texture.Value);
                switch (texture.Key)
                {
                    case PBRTextureType.Albedo: material.SetTexture("_MainTex", textureAsset); continue;
                    case PBRTextureType.Normal: material.SetTexture("_BumpMap", textureAsset); continue;
                    case PBRTextureType.Emission:
                        material.SetTexture("_EmissionMap", textureAsset);
                        material.EnableKeyword("_EMISSION");
                        material.SetFloat("_EmissionToggle", 1.0f);
                        material.SetColor("_EmissionColor", Color.white);
                        continue;
                    case PBRTextureType.Smoothness:
                        FreeImagePackingEditor.ChannelG.UnityTexture = textureAsset;
                        FreeImagePackingEditor.ChannelG.Channel.Invert = true;
                        needsPacking = true;
                        continue;
                    case PBRTextureType.Roughness:
                        FreeImagePackingEditor.ChannelG.UnityTexture = textureAsset;
                        needsPacking = true;
                        continue;
                    case PBRTextureType.AO:
                            FreeImagePackingEditor.ChannelR.UnityTexture = textureAsset;
                            needsPacking = true;
                        continue;
                    case PBRTextureType.Metallic:
                        FreeImagePackingEditor.ChannelB.UnityTexture = textureAsset;
                        needsPacking = true;
                        continue;
                }
            }

            AssetDatabase.CreateAsset(material, materialPath);

            if (needsPacking)
            {
                FreeImagePackingEditor.Init(false);
                FreeImagePackingEditor.ChannelR.DisplayName = "AO";
                FreeImagePackingEditor.ChannelG.DisplayName = "Roughness";
                FreeImagePackingEditor.ChannelB.DisplayName = "Metallic";
                FreeImagePackingEditor.ChannelB.Channel.DefaultColor = FreeImagePacking.DefaultColor.Black;
                FreeImagePackingEditor.ChannelA.DisplayName = "";
                FreeImagePackingEditor.Linear = true;
                FreeImagePackingEditor.AddPackingMaterial(material, "_MaskMap");
                FreeImagePackingEditor.onPackingFinished = () =>
                {
                    material.SetFloat("_Glossiness", 1.0f);
                    material.SetFloat("_Roughness", 1.0f);
                    material.SetFloat("_Metallic", 1.0f);
                    FreeImagePackingEditor.onPackingFinished = delegate { };
                };
            }

            EditorGUIUtility.PingObject(material);
            Selection.activeObject = material;
        }

        private void CheckMatch(string filePath, string[] splitSearchName)
        {
            bool roughnessFound = false;
            for (int i = 1; i < splitSearchName.Length; i++)
            {
                string currentSubstring = splitSearchName[i];

                TryAddMatch(filePath, currentSubstring, _albedoMatch);
                TryAddMatch(filePath, currentSubstring, _normalMatch);
                if (TryAddMatch(filePath, currentSubstring, _roughnessMatch))
                {
                    roughnessFound = true;
                }
                else if (!roughnessFound)
                {
                    TryAddMatch(filePath, currentSubstring, _smoothnessMatch);
                }
                TryAddMatch(filePath, currentSubstring, _metallicMatch);
                TryAddMatch(filePath, currentSubstring, _aoMatch);
                TryAddMatch(filePath, currentSubstring, _emissionMatch);
            }
        }

        private bool TryAddMatch(string filePath, string currentSubstring, TextureMatch match)
        {
            bool found = Array.Exists(match.names, x => x.Equals(currentSubstring, StringComparison.OrdinalIgnoreCase));

            if (!found || MatchExists(filePath, match.type))
            {
                return false;
            }

            _matchedTextures.Add(match.type, filePath);
            return true;
        }

        private bool MatchExists(string filePath, PBRTextureType textureType)
        {
            if (_matchedTextures.ContainsKey(textureType))
            {
                return true;
            }
            if (_matchedTextures.Any(x => x.Value.Equals(filePath)))
            {
                return true;
            }

            return false;
        }

        private enum PBRTextureType
        {
            Albedo,
            Normal,
            Roughness,
            Smoothness,
            Metallic,
            AO,
            Emission,
        }

        private struct TextureMatch
        {
            public PBRTextureType type;
            public string[] names;
        }

        private TextureMatch _albedoMatch = new TextureMatch()
        {
            type = PBRTextureType.Albedo,
            names = new string[]
            {
                "albedo",
                "basecolor",
                "color",
                "diffuse",
                "maintex",
                "base",
                "col",
                "diff",
                "dif"
            }
        };

        private TextureMatch _normalMatch = new TextureMatch()
        {
            type = PBRTextureType.Normal,
            names = new string[]
            {
                "normal",
                "normalgl",
                "normalmap",
                "bump",
                "bumpmap",
                "normaldx",
                "nrm",
                "nrlm",
                "nor" 
            }
        };

        private TextureMatch _metallicMatch = new TextureMatch()
        {
            type = PBRTextureType.Metallic,
            names = new string[]
            {
                "metallic",
                "metal",
                "metalness",
                "mtl"
            }
        };

        private TextureMatch _roughnessMatch = new TextureMatch()
        {
            type = PBRTextureType.Roughness,
            names = new string[]
            {
                "roughness",
                "rough",
                "rgh",
            }
        };

        private TextureMatch _smoothnessMatch = new TextureMatch()
        {
            type = PBRTextureType.Smoothness,
            names = new string[]
            {
                "smoothness",
                "glossiness",
                "smooth",
                "gloss"
            }
        };

        private TextureMatch _aoMatch = new TextureMatch()
        {
            type = PBRTextureType.AO,
            names = new string[]
            {
                "ambientocclusion",
                "occlusion",
                "ao"
            }
        };

        private TextureMatch _emissionMatch = new TextureMatch()
        {
            type = PBRTextureType.Emission,
            names = new string[]
            {
                "emissive",
                "emission",
                "emit"
            }
        };
    }
}

