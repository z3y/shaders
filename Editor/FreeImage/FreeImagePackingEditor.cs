using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using z3y.Shaders;
using static z3y.FreeImagePacking;

namespace z3y
{
    public class FreeImagePackingEditor : EditorWindow
    {
        [MenuItem("Window/z3y/Texture Packing")]
        public static void Init()
        {
            var window = (FreeImagePackingEditor)GetWindow(typeof(FreeImagePackingEditor));
            window.titleContent = new GUIContent("Texture Packing");
            window.Show();
            window.minSize = new Vector2(400, 500);
            ResetFields();
        }

        public static void ResetFields()
        {
            _packingMaterial = null;
            _packingProperty = null;
            ChannelR = new PackingField();
            ChannelG = new PackingField();
            ChannelB = new PackingField();
            ChannelA = new PackingField();
            Linear = false;
            
            ChannelR.DisplayName = "Red";
            ChannelG.DisplayName = "Green";
            ChannelB.DisplayName = "Blue";
            ChannelA.DisplayName = "Alpha";

            ChannelG.Channel.Source = ChannelSource.Green;
            ChannelB.Channel.Source = ChannelSource.Blue;
            ChannelA.Channel.Source = ChannelSource.Alpha;

            ChannelR.Channel.DefaultColor = DefaultColor.Black;
            ChannelG.Channel.DefaultColor = DefaultColor.Black;
            ChannelB.Channel.DefaultColor = DefaultColor.Black;
        }

        public static PackingField ChannelR;
        public static PackingField ChannelG;
        public static PackingField ChannelB;
        public static PackingField ChannelA;

        public static Texture2D PackedTexture = null;
        private static Vector2Int _customSize = new Vector2Int(1024, 1024);
        public static bool Linear = false;
        public static TextureSize Size = FreeImagePacking.TextureSize.Default;

        private static Material _packingMaterial = null;
        private static MaterialProperty _packingProperty = null;
        
        public struct PackingField
        {
            public Texture2D UnityTexture;
            public TextureChannel Channel;
            public string DisplayName;
            [CanBeNull] public string InvertDisplayName;
        }

        private const string _guiStyle = "helpBox";
        public void OnGUI()
        {
            if (_packingMaterial)
            {
                EditorGUILayout.BeginVertical(_guiStyle);

                EditorGUILayout.LabelField(new GUIContent($"Material - {_packingMaterial.name}"));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent($"Texture - {_packingProperty.displayName}"));
                if (_packingProperty.textureValue && GUILayout.Button("Modify"))
                {
                    var texture = _packingProperty.textureValue;
                    if (texture is Texture2D texture2D)
                    {
                        ChannelR.UnityTexture = texture2D;
                        ChannelG.UnityTexture = texture2D;
                        ChannelB.UnityTexture = texture2D;
                        ChannelA.UnityTexture = texture2D;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            
            
            TexturePackingField(ref ChannelR, Color.red);
            TexturePackingField(ref ChannelG, Color.green);
            TexturePackingField(ref ChannelB, Color.blue);
            TexturePackingField(ref ChannelA, Color.white);
            
            
            EditorGUILayout.BeginVertical(_guiStyle);
            PackingFormat = (TexturePackingFormat)EditorGUILayout.EnumPopup("Format",PackingFormat);
            ImageFilter = (FreeImage.FREE_IMAGE_FILTER)EditorGUILayout.EnumPopup(new GUIContent("Rescale Filter", "Filter that will be used for rescaling textures to match them to the target size if needed"),ImageFilter);
            Size = (TextureSize)EditorGUILayout.EnumPopup(new GUIContent("Size", "Specify the size of the packed texture"), Size);
            if (Size == TextureSize.Custom)
            {
                EditorGUI.indentLevel++;
                _customSize = EditorGUILayout.Vector2IntField(new GUIContent(), _customSize);
                EditorGUI.indentLevel--;
            }
            Linear = EditorGUILayout.Toggle(new GUIContent("Linear", "Disable sRGB on texture import, for mask and data textures (Roughness, Occlusion, Metallic etc)"), Linear);
            
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset"))
            {
                ResetFields();
            }
            
            if (GUILayout.Button("Pack"))
            {
                GetTexturePath(ref ChannelR);
                GetTexturePath(ref ChannelG);
                GetTexturePath(ref ChannelB);
                GetTexturePath(ref ChannelA);

                
                var referenceTexture = ChannelG.UnityTexture ?? ChannelA.UnityTexture ?? ChannelR.UnityTexture ?? ChannelB.UnityTexture;
                if (referenceTexture is null) return;
                
                var path = AssetDatabase.GetAssetPath(referenceTexture);
                var fullPath = Path.GetFullPath(path);
                
                var absolutePath = GetPackedTexturePath(fullPath);
                var unityPath = GetPackedTexturePath(path);
                
                int width = (int)Size;
                int height = (int)Size;

                if (Size == TextureSize.Default)
                {
                    width = referenceTexture.width;
                    height = referenceTexture.height;
                }
                else if (Size == TextureSize.Custom)
                {
                    width = _customSize.x;
                    height = _customSize.y;
                }

                PackCustom(absolutePath, ChannelR.Channel, ChannelG.Channel, ChannelB.Channel, ChannelA.Channel, (width, height), PackingFormat);
                AssetDatabase.ImportAsset(unityPath, ImportAssetOptions.ForceUpdate);

                if (_packingMaterial)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(unityPath);
                    _packingMaterial.SetTexture(_packingProperty.name, texture);
                    LitGUI.ApplyChanges(_packingMaterial);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            
            if (LastPackingTime > 0)
            {
                EditorGUILayout.LabelField($"Packed in {LastPackingTime}ms");
            }

        }

        public static void AddPackingMaterial(Material material, MaterialProperty property)
        {
            _packingProperty = property;
            _packingMaterial = material;
        }

        private static string GetPackedTexturePath(string referencePath)
        {
            var directory = Path.GetDirectoryName(referencePath);
            var fileName = Path.GetFileNameWithoutExtension(referencePath);
            
            var newPath = directory + @"\" + fileName + "_packed";
            if (Linear)
            {
                newPath += "_linear";
            }
            var extension = PackingFormat.GetExtension();

            newPath = newPath + "." + extension;

            return newPath;
        }
        
        

        public static int LastPackingTime = 0;

        private void OnEnable()
        {
            LastPackingTime = 0;
        }

        private static void GetTexturePath(ref PackingField field)
        {
            if (field.UnityTexture is null) field.Channel.Path = null;
            
            var path = AssetDatabase.GetAssetPath(field.UnityTexture);
            field.Channel.Path = path;
        }

        private static void TexturePackingField(ref PackingField field, Color color)
        {
            EditorGUILayout.BeginVertical(_guiStyle);

            //GUILayout.Space(1);

            
            
            GUILayout.BeginHorizontal();
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                richText = true,
                
            };
            field.UnityTexture = (Texture2D)EditorGUILayout.ObjectField(field.UnityTexture, typeof(Texture2D), false, GUILayout.Width(60), GUILayout.Height(60));
            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(rect.x-2, rect.y, 2, 60), color);
            GUILayout.Space(5);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            
            field.Channel.Source = (ChannelSource)EditorGUILayout.EnumPopup(field.Channel.Source, GUILayout.Width(80));
    


            GUILayout.Label("➜", GUILayout.Width(15));

            if (field.InvertDisplayName != null && field.Channel.Invert)
            {
                GUILayout.Label($"<b>{field.InvertDisplayName}</b>", style, GUILayout.Width(85));
            }
            else
            {
                GUILayout.Label($"<b>{field.DisplayName}</b>", style, GUILayout.Width(85));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            
            field.Channel.Invert = GUILayout.Toggle(field.Channel.Invert, "Invert", GUILayout.Width(70));
            
            GUILayout.Label("Fallback", GUILayout.Width(55));

            field.Channel.DefaultColor = (DefaultColor)EditorGUILayout.EnumPopup(field.Channel.DefaultColor, GUILayout.Width(60));

            GUILayout.EndHorizontal();
                
                
            GUILayout.Label(field.UnityTexture ? field.UnityTexture.name :  " ", style);

            GUILayout.EndVertical();
            

            GUILayout.EndHorizontal();

            //GUILayout.Space(1);
            
            EditorGUILayout.EndVertical();
            
           

        }
    }
}