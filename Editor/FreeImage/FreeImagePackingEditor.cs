using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using static z3y.FreeImagePacking;

namespace z3y
{
    public class FreeImagePackingEditor : EditorWindow
    {
        [MenuItem("Window/z3y/Texture Packing new")]
        public static void Init()
        {

            var window = (FreeImagePackingEditor)GetWindow(typeof(FreeImagePackingEditor));
            window.titleContent = new GUIContent("Texture Packing");
            window.Show();
            window.minSize = new Vector2(400, 500);
        }

        public static PackingField ChannelR;
        public static PackingField ChannelG;
        public static PackingField ChannelB;
        public static PackingField ChannelA;

        public static Texture2D packedTexture = null;

        public static bool Linear = false;

        public static TextureSize Size = FreeImagePacking.TextureSize.Default;

        public struct PackingField
        {
            public Texture2D UnityTexture;
            public TextureChannel Channel;
            public string DisplayName;
            [CanBeNull] public string InvertDisplayName;
        }

        public void OnGUI()
        {
            TexturePackingField(ref ChannelR);
            TexturePackingField(ref ChannelG);
            TexturePackingField(ref ChannelB);
            TexturePackingField(ref ChannelA);

            
            EditorGUILayout.Space(20);
            
            PackingFormat = (TexturePackingFormat)EditorGUILayout.EnumPopup("Format",PackingFormat);
            ImageFilter = (FreeImage.FREE_IMAGE_FILTER)EditorGUILayout.EnumPopup("Rescale Filter",ImageFilter);
            Size = (TextureSize)EditorGUILayout.EnumPopup("Size", Size);
            Linear = EditorGUILayout.Toggle("Linear", Linear);

            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear"))
            {
                ChannelR.UnityTexture = null;
                ChannelB.UnityTexture = null;
                ChannelG.UnityTexture = null;
                ChannelA.UnityTexture = null;
            }
            if (GUILayout.Button("Pack"))
            {
                GetTexturePath(ref ChannelR);
                GetTexturePath(ref ChannelG);
                GetTexturePath(ref ChannelB);
                GetTexturePath(ref ChannelA);

                
                var referenceTexture = ChannelG.UnityTexture ?? ChannelA.UnityTexture ?? ChannelR.UnityTexture ?? ChannelB.UnityTexture;
                if (referenceTexture is null) return;
                
                var unityPath = AssetDatabase.GetAssetPath(referenceTexture);
                var fileName = Path.GetFileNameWithoutExtension(unityPath);
                var path = Path.GetDirectoryName(Path.GetFullPath(unityPath));

                var newPath = path + "/" + fileName + "_packed";
                if (Linear)
                {
                    newPath += "_linear";
                }


                int width = (int)Size;
                int height = (int)Size;

                if (Size == TextureSize.Default)
                {
                    width = referenceTexture.width;
                    height = referenceTexture.height;
                }

                PackCustom(newPath, ChannelR.Channel, ChannelG.Channel, ChannelB.Channel, ChannelA.Channel, (width, height), PackingFormat);
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();
            
            
            EditorGUILayout.Space(20);
            if (LastPackingTime > 0)
            {
                EditorGUILayout.LabelField($"Packed in {LastPackingTime}ms");
            }

        }
        
        private static TexturePackingFormat PackingFormat = TexturePackingFormat.tga;

        public static int LastPackingTime = 0;

        private void OnEnable()
        {
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

            LastPackingTime = 0;

        }

        private static void GetTexturePath(ref PackingField field)
        {
            if (field.UnityTexture is null) field.Channel.Path = null;
            
            var path = AssetDatabase.GetAssetPath(field.UnityTexture);
            field.Channel.Path = path;
        }

        private static void TexturePackingField(ref PackingField field)
        {
            EditorGUILayout.BeginVertical("box");

            //GUILayout.Space(1);

            GUILayout.BeginHorizontal();
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                richText = true,
                
            };
            field.UnityTexture = (Texture2D)EditorGUILayout.ObjectField(field.UnityTexture, typeof(Texture2D), false, GUILayout.Width(60), GUILayout.Height(60));
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