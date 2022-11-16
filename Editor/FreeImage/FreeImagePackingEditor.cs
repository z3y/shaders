using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using static z3y.FreeImagePacking;
using Color = System.Drawing.Color;

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
            window.minSize = new Vector2(400, 300);
        }

        public static PackingField ChannelR;
        public static PackingField ChannelG;
        public static PackingField ChannelB;
        public static PackingField ChannelA;

        public static int Width = 1024;
        public static int Height = 1024;

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
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear"))
            {
                
            }
            if (GUILayout.Button("Pack"))
            {
                GetTexturePath(ref ChannelR);
                GetTexturePath(ref ChannelG);
                GetTexturePath(ref ChannelB);
                GetTexturePath(ref ChannelA);

                PackCustom(@"d:\packed", ChannelR.Channel, ChannelB.Channel, ChannelG.Channel, ChannelA.Channel, (Width, Height), PackingFormat);
            }
            EditorGUILayout.EndHorizontal();

        }
        
        private static TexturePackingFormat PackingFormat = TexturePackingFormat.tga;


        private void OnEnable()
        {
            ChannelR.DisplayName = "Red";
            ChannelG.DisplayName = "Green";
            ChannelB.DisplayName = "Blue";
            ChannelA.DisplayName = "Alpha";
        }

        private static void GetTexturePath(ref PackingField field)
        {
            if (field.UnityTexture is null) return;
            
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
            
            GUILayout.Label("Fallback", GUILayout.Width(50));

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