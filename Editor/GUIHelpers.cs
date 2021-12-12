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
        public static Texture2D animatedTex = (Texture2D)Resources.Load( "lit_animated", typeof(Texture2D));
        public static Texture2D xTex = (Texture2D)Resources.Load( "lit_x", typeof(Texture2D));


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

		public static void sRGBWarning(MaterialProperty tex){
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
        // public static void Prop(string property, string extraProperty = null) => MaterialProp(GetProperty(property), extraProperty is null ? null : GetProperty(extraProperty), me, isLocked, material);

        public static void MaterialProp(MaterialProperty property, MaterialProperty extraProperty, MaterialEditor me, bool isLocked, Material material)
        {

            EditorGUI.BeginDisabledGroup(isLocked);
            if(property is null) return;

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

            HandleMouseEvents(animatedProp, material);

            EditorGUI.EndDisabledGroup();
 
        }
        const string AnimatedPropertySuffix = "Animated";

        public struct ResetPropertyData
        {
            public MaterialProperty p;
            public float defaultFloatValue;
            public Vector4 defaultVectorValue;
            public string[] attributes;
        }

        static ResetPropertyData resetProperty;

        public static void HandleMouseEvents (MaterialProperty p, Material material, string extraPropertyName = null)
        {
            if(p is null) return;

            string k = p.name;
            string animatedName = k + AnimatedPropertySuffix;
            bool isAnimated = material.GetTag(animatedName, false) == "" ? false : true;
            var e = Event.current;

            if (e.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(e.mousePosition) && e.button == 1)
            {
                int propIndex = material.shader.FindPropertyIndex(p.name);
                if(p.type == MaterialProperty.PropType.Float || p.type == MaterialProperty.PropType.Range || p.type == MaterialProperty.PropType.Vector || p.type == MaterialProperty.PropType.Color)
                {
                    resetProperty.p = p;

                    

                    if(p.type == MaterialProperty.PropType.Vector || p.type == MaterialProperty.PropType.Color)
                        resetProperty.defaultVectorValue = material.shader.GetPropertyDefaultVectorValue(propIndex);
                    else
                        resetProperty.defaultFloatValue = material.shader.GetPropertyDefaultFloatValue(propIndex);

                    resetProperty.attributes = material.shader.GetPropertyAttributes(propIndex);
                    bool isKeywordToggle = false;
                    foreach (var s in resetProperty.attributes)
                    {
                        if(s.StartsWith("Toggle(")) isKeywordToggle = true;
                        else if(s.StartsWith("ToggleOff(")) isKeywordToggle = true;
                        else if(s.StartsWith("KeywordEnum(")) isKeywordToggle = true;
                    }
                    if(isKeywordToggle) return;

                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Reset"), false, ResetProperty);
                    menu.ShowAsContext();

                }
                
                
            }

            if (e.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(e.mousePosition) && e.button == 2)
            {
                e.Use();
                material.SetOverrideTag(animatedName, isAnimated ? "" : "1");
                if(extraPropertyName != null) material.SetOverrideTag(extraPropertyName + AnimatedPropertySuffix, isAnimated ? "" : "1");

            }
            if(isAnimated)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                Rect stopWatch = new Rect(lastRect.x - 16f, lastRect.y + 4f, 11f, 11f);

                GUI.DrawTexture(stopWatch, animatedTex);

            }
        }

        public static void ResetProperty()
        {
            

            if(resetProperty.p.type == MaterialProperty.PropType.Range || resetProperty.p.type == MaterialProperty.PropType.Float)
            {
                resetProperty.p.floatValue = resetProperty.defaultFloatValue;
            }
            else if(resetProperty.p.type == MaterialProperty.PropType.Vector)
            {
                resetProperty.p.vectorValue = resetProperty.defaultVectorValue;
            }
            else
            {
                resetProperty.p.colorValue = resetProperty.defaultVectorValue;
            }
        }

        public static void DrawAnimatedPropertiesList(bool isLocked, MaterialProperty[] allProps, Material material)
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.HelpBox("Middle click a property to keep it unlocked", MessageType.Info);
            EditorGUI.indentLevel++;

            EditorGUI.BeginDisabledGroup(isLocked);
            foreach(MaterialProperty property in allProps){
                string animatedName = property.name + AnimatedPropertySuffix;
                bool isAnimated = material.GetTag(animatedName, false) == "" ? false : true;
                if (isAnimated)
                { 
                    EditorGUILayout.LabelField(property.displayName);
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    Rect x = new Rect(lastRect.x, lastRect.y + 4f, 15f, 12f);
                    GUI.DrawTexture(x, xTex);

                    var e = Event.current;
                    if (e.type == EventType.MouseDown && x.Contains(e.mousePosition) && e.button == 0)
                    {
                        e.Use();
                        material.SetOverrideTag(animatedName, "");
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        public static void DrawPropTileOffset(MaterialProperty property, bool isLocked, MaterialEditor me, Material material)
        {
            EditorGUI.BeginDisabledGroup(isLocked);
            me.TextureScaleOffsetProperty(property);
            HandleMouseEvents(property, material, property.name + "_ST");
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

        public static void ShaderOptimizerButton(MaterialProperty shaderOptimizer, MaterialEditor materialEditor, Material mat)
        {
            if (materialEditor.targets.Length == 1)
            {
               
                EditorGUI.BeginChangeCheck();
                if (shaderOptimizer.floatValue == 0)
                {
                    GUILayout.Button("Lock");
                }
                else GUILayout.Button("Unlock");
                if (EditorGUI.EndChangeCheck())
                {
                    shaderOptimizer.floatValue = shaderOptimizer.floatValue == 1 ? 0 : 1;
                    if (shaderOptimizer.floatValue == 1)
                    {
                        Shaders.Optimizer.LockMaterial(mat);
                    }
                    else
                    {
                        Shaders.Optimizer.Unlock(mat);
                    }
                }
                EditorGUILayout.Space(4);
            }
        }

        // public static Dictionary<string, MaterialProperty> MaterialProperties = new Dictionary<string, MaterialProperty>();
        // public static void SetupPropertiesDictionary(MaterialProperty[] props)
        // {
        //     for (int i = 0; i < props.Length; i++)
        //     {
        //         MaterialProperty p = props[i];
        //         MaterialProperties[p.name] = p;
        //     }
        // }

        // public static MaterialProperty GetProperty(string name)
        // {
        //     MaterialProperties.TryGetValue(name, out MaterialProperty p);
        //     return p;
        // }



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

