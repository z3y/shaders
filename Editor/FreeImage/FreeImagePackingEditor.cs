using System;
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

            TexturePacking window = (TexturePacking)GetWindow(typeof(TexturePacking));
            window.Show();
            window.minSize = new Vector2(400, 300);
        }

        public static PackingField ChannelR;
        public static PackingField ChannelG;
        public static PackingField ChannelB;
        public static PackingField ChannelA;

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

        }
        
        private static void TexturePackingField(ref PackingField field, bool showOptions = true)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(1);

                GUILayout.BeginHorizontal();
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    richText = true,
                    
                };
                field.UnityTexture = (Texture2D)EditorGUILayout.ObjectField(field.UnityTexture, typeof(Texture2D), false, GUILayout.Width(40), GUILayout.Height(40));
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                if (field.InvertDisplayName != null && field.Channel.Invert)
                {
                    GUILayout.Label($"<b>{field.InvertDisplayName}</b>", style, GUILayout.Width(85));
                }
                else
                {
                    GUILayout.Label($"<b>{field.DisplayName}</b>", style, GUILayout.Width(85));
                }
                GUILayout.Label(field.UnityTexture ? field.UnityTexture.name :  " ", style);
                GUILayout.EndHorizontal();

                if (showOptions)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Source: ", GUILayout.Width(50));
                    field.Channel.Source = (ChannelSource)EditorGUILayout.EnumPopup(field.Channel.Source, GUILayout.Width(70));
                    GUILayout.Space(20);
                    field.Channel.Invert = GUILayout.Toggle(field.Channel.Invert, "Invert", GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.Space(1);
            }

        }
    }
}