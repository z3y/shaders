using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using z3y.Shaders;

namespace z3y.Shaders.SimpleLit
{
    public class InspectorData
    {
        public Dictionary<string, bool?> FoldoutValues = new Dictionary<string, bool?>();
    }

    [InitializeOnLoad]
    public class Helpers
    {
        public static Texture2D groupTex = (Texture2D)Resources.Load( EditorGUIUtility.isProSkin ? "lit_group" : "lit_group_light", typeof(Texture2D));

        public static bool TextureFoldout(bool display)
        {
            //var rect = GUILayoutUtility.GetRect(16f, -4);
            var lastRect = GUILayoutUtility.GetLastRect();
            var e = Event.current;
            var toggleRect = new Rect(lastRect.x, lastRect.y + 2f, 12f, 12f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }
            if (e.type == EventType.MouseDown && toggleRect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }
            return display;
        }

        public static bool Foldout(string title, bool display)
        {
            var rect = DrawFoldout(title, new Vector2(18f, 0f),18);
            var e = Event.current;
            var toggleRect = new Rect(rect.x + 12f, rect.y + 3f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }
            return display;
        }

        public static Rect DrawFoldout(string title, Vector2 contentOffset, int HeaderHeight)
        {
            var style = new GUIStyle("BoldLabel");
            style.font = new GUIStyle(EditorStyles.boldLabel).font;
            //style.font = EditorStyles.boldFont;
            //style.fontSize = GUI.skin.font.fontSize;
            style.fontSize = 12;
            //style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = HeaderHeight;
            style.contentOffset = contentOffset;
            var rect = GUILayoutUtility.GetRect(16f, HeaderHeight, style);
            var rect2 = new Rect(rect.x + -20f, rect.y, rect.width + 30f, rect.height+2);
            var rectText = new Rect(rect.x -8f, rect.y+1, rect.width, rect.height);

            GUI.DrawTexture(rect2, groupTex);
            GUI.Label(rectText, title, style);
            return rect2;
        }

       public static void PropertyGroup(Action action)
       {
            GUILayout.Space(1);
			using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(1);
                action();
                GUILayout.Space(1);
			}
			GUILayout.Space(1);
		}

        // Mimics the normal map import warning - written by Orels1
		static bool TextureImportWarningBox(string message)
        {
			GUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
			EditorGUILayout.LabelField(message, new GUIStyle(EditorStyles.label) {
				fontSize = 11, wordWrap = true
			});
			EditorGUILayout.BeginHorizontal(new GUIStyle() {
				alignment = TextAnchor.MiddleRight
			}, GUILayout.Height(24));
			EditorGUILayout.Space();
			bool buttonPress = GUILayout.Button("Fix Now", new GUIStyle("button") {
				stretchWidth = false,
				margin = new RectOffset(0, 0, 0, 0),
				padding = new RectOffset(8, 8, 0, 0)
			}, GUILayout.Height(22));
			EditorGUILayout.EndHorizontal();
			GUILayout.EndVertical();
			return buttonPress;
		}

		public static void sRGBWarning(MaterialProperty tex) {
            if(tex is null) return;
			if (tex.textureValue){
				string sRGBWarning = "This texture is marked as sRGB, but should be linear.";
				string texPath = AssetDatabase.GetAssetPath(tex.textureValue);
				TextureImporter texImporter;
				var importer = TextureImporter.GetAtPath(texPath) as TextureImporter;
				if (importer != null){
					texImporter = (TextureImporter)importer;
					if (texImporter.sRGBTexture){
						if (TextureImportWarningBox(sRGBWarning)){
							texImporter.sRGBTexture = false;
							texImporter.SaveAndReimport();
						}
					}
				}
			}
		}
        private const char hoverSplitSeparator = ':';

        public static void MaterialProp(MaterialProperty property, MaterialProperty extraProperty, MaterialEditor me)
        {
            // if(property is null) return;

            MaterialProperty animatedProp = null;

            if( property.type == MaterialProperty.PropType.Range ||
                property.type == MaterialProperty.PropType.Float ||
                property.type == MaterialProperty.PropType.Vector ||
                property.type == MaterialProperty.PropType.Color)
            {
                me.ShaderProperty(property, property.displayName);
                animatedProp = property;

            }

            if(property.type == MaterialProperty.PropType.Texture) 
            {
                string[] p = property.displayName.Split(hoverSplitSeparator);
                animatedProp = extraProperty != null ? extraProperty : null;
                p[0] = p[0];


                me.TexturePropertySingleLine(new GUIContent(p[0], p.Length == 2 ? p[1] : null), property, extraProperty);
            }
        }

        public static void DrawPropTileOffset(MaterialProperty property, MaterialEditor me)
        {
            me.TextureScaleOffsetProperty(property);
            EditorGUI.EndDisabledGroup();
        }

        public static bool Foldout(string foldoutText, bool foldoutName, Action action)
        {
            foldoutName = Foldout(foldoutText, foldoutName);
            if(foldoutName)
            {
                EditorGUILayout.Space();
			    action();
                EditorGUILayout.Space();
            }
            return foldoutName;
        }

        public static bool TriangleFoldout(bool foldoutName, Action action)
        {
            foldoutName = TextureFoldout(foldoutName);
            if(foldoutName)
            {
                PropertyGroup(() => {
                    action();
                });
            }
            return foldoutName;
        }

        public static void SetupGIFlags(float emissionEnabled, Material material)
        {
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (emissionEnabled != 1)
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }


    }
}

