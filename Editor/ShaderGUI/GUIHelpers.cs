using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

namespace z3y.Shaders2
{
    public static class GUIHelpers
    {
        #region CoreEditorUtils.cs
        /// <summary>Draw a header</summary>
        /// <param name="title">Title of the header</param>
        public static void DrawHeader(string title) => DrawHeader(EditorGUIUtility.TrTextContent(title));

        /// <summary>Draw a header</summary>
        /// <param name="title">Title of the header</param>
        public static void DrawHeader(GUIContent title)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOption"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderFoldout(string title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOption = null)
            => DrawHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed, hasMoreOptions, toggleMoreOption);

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);
            float xMin = backgroundRect.xMin;

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;
            foldoutRect.x = labelRect.xMin + 15 * (EditorGUI.indentLevel - 1); //fix for presset

            // More options 1/2
            var moreOptionsRect = new Rect();
            if (hasMoreOptions != null)
            {
                moreOptionsRect = backgroundRect;
                moreOptionsRect.x += moreOptionsRect.width - 16 - 1;
                moreOptionsRect.height = 15;
                moreOptionsRect.width = 16;
            }

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
                backgroundRect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                backgroundRect.width -= 1;
            }

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // More options 2/2
            if (hasMoreOptions != null)
            {
                EditorGUI.BeginChangeCheck();
                Styles.DrawMoreOptions(moreOptionsRect, hasMoreOptions());
                if (EditorGUI.EndChangeCheck() && toggleMoreOptions != null)
                {
                    toggleMoreOptions();
                }
            }

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && !moreOptionsRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }
        class Styles
        {
            static readonly Color k_Normal_AllTheme = new Color32(0, 0, 0, 0);
            //static readonly Color k_Hover_Dark = new Color32(70, 70, 70, 255);
            //static readonly Color k_Hover = new Color32(193, 193, 193, 255);
            static readonly Color k_Active_Dark = new Color32(80, 80, 80, 255);
            static readonly Color k_Active = new Color32(216, 216, 216, 255);

            static readonly int s_MoreOptionsHash = "MoreOptions".GetHashCode();

            static public GUIContent moreOptionsLabel { get; private set; }
            static public GUIStyle moreOptionsStyle { get; private set; }
            static public GUIStyle moreOptionsLabelStyle { get; private set; }

            static Styles()
            {
                moreOptionsLabel = EditorGUIUtility.TrIconContent("MoreOptions", "More Options");

                moreOptionsStyle = new GUIStyle(GUI.skin.toggle);
                Texture2D normalColor = new Texture2D(1, 1);
                normalColor.SetPixel(1, 1, k_Normal_AllTheme);
                moreOptionsStyle.normal.background = normalColor;
                moreOptionsStyle.onActive.background = normalColor;
                moreOptionsStyle.onFocused.background = normalColor;
                moreOptionsStyle.onNormal.background = normalColor;
                moreOptionsStyle.onHover.background = normalColor;
                moreOptionsStyle.active.background = normalColor;
                moreOptionsStyle.focused.background = normalColor;
                moreOptionsStyle.hover.background = normalColor;

                moreOptionsLabelStyle = new GUIStyle(GUI.skin.label);
                moreOptionsLabelStyle.padding = new RectOffset(0, 0, 0, -1);
            }

            //Note:
            // - GUIStyle seams to be broken: all states have same state than normal light theme
            // - Hover with event will not be updated right when we enter the rect
            //-> Removing hover for now. Keep theme color for refactoring with UIElement later
            static public bool DrawMoreOptions(Rect rect, bool active)
            {
                int id = GUIUtility.GetControlID(s_MoreOptionsHash, FocusType.Passive, rect);
                var evt = Event.current;
                switch (evt.type)
                {
                    case EventType.Repaint:
                        Color background = k_Normal_AllTheme;
                        if (active)
                            background = EditorGUIUtility.isProSkin ? k_Active_Dark : k_Active;
                        EditorGUI.DrawRect(rect, background);
                        GUI.Label(rect, moreOptionsLabel, moreOptionsLabelStyle);
                        break;
                    case EventType.KeyDown:
                        bool anyModifiers = (evt.alt || evt.shift || evt.command || evt.control);
                        if ((evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !anyModifiers && GUIUtility.keyboardControl == id)
                        {
                            evt.Use();
                            GUI.changed = true;
                            return !active;
                        }
                        break;
                    case EventType.MouseDown:
                        if (rect.Contains(evt.mousePosition))
                        {
                            GrabMouseControl(id);
                            evt.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (HasMouseControl(id))
                        {
                            ReleaseMouseControl();
                            evt.Use();
                            if (rect.Contains(evt.mousePosition))
                            {
                                GUI.changed = true;
                                return !active;
                            }
                        }
                        break;
                    case EventType.MouseDrag:
                        if (HasMouseControl(id))
                            evt.Use();
                        break;
                }

                return active;
            }

            static int s_GrabbedID = -1;
            static void GrabMouseControl(int id) => s_GrabbedID = id;
            static void ReleaseMouseControl() => s_GrabbedID = -1;
            static bool HasMouseControl(int id) => s_GrabbedID == id;
        }

        /// <summary>Draw a splitter separator</summary>
        /// <param name="isBoxed">[Optional] add margin if the splitter is boxed</param>
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }
        #endregion

        public static bool DrawGroupFoldout(Material material, string title, bool defaultValue)
        {
            var materialPropertyName = $"z3y_group_foldout_{title.Replace(' ', '_')}";
            bool isOpen = material.HasProperty(materialPropertyName) ? material.GetFloat(materialPropertyName) == 1 : defaultValue;
            DrawSplitter();
            isOpen = DrawHeaderFoldout(title, isOpen);
            material.SetFloat(materialPropertyName, isOpen ? 1 : 0);
            if (isOpen) EditorGUILayout.Space();
            return isOpen;
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

        public static bool BoolValue(this MaterialProperty p) => p.type == MaterialProperty.PropType.Texture ? p.textureValue : p.floatValue == 1;

        public static bool TextureFoldout(bool display)
       {
	       //var rect = GUILayoutUtility.GetRect(16f, -4);
	       var lastRect = GUILayoutUtility.GetLastRect();
	       var e = Event.current;
	       var toggleRect = new Rect(lastRect.x - 15f, lastRect.y + 2f, 12f, 12f);
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

        // Mimics the normal map import warning - written by Orels1
		private static bool TextureImportWarningBox(string message)
        {
			GUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
			EditorGUILayout.LabelField(message, new GUIStyle(EditorStyles.label) { fontSize = 11, wordWrap = true });
			EditorGUILayout.BeginHorizontal(new GUIStyle() { alignment = TextAnchor.MiddleRight }, GUILayout.Height(24));
			EditorGUILayout.Space();
			var buttonPress = GUILayout.Button("Fix Now", new GUIStyle("button")
			{
				stretchWidth = false,
				margin = new RectOffset(0, 0, 0, 0),
				padding = new RectOffset(8, 8, 0, 0)
			}, GUILayout.Height(22));
			EditorGUILayout.EndHorizontal();
			GUILayout.EndVertical();
			return buttonPress;
		}

		public static void sRGBWarning(MaterialProperty tex)
		{
            if (!tex?.textureValue) return;
			var texPath = AssetDatabase.GetAssetPath(tex.textureValue);
			var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
			if (importer == null) return;
            const string text = "This texture is marked as sRGB, but should be linear.";
			if (!importer.sRGBTexture || !TextureImportWarningBox(text)) return;
			importer.sRGBTexture = false;
			importer.SaveAndReimport();
		}
        

        public static void InitializeAllProperties(FieldInfo[] infos,MaterialProperty[] props, object obj, Func<string, MaterialProperty[], bool, MaterialProperty> findProperty)
		{
			foreach (var property in infos)
			{
				if (property.FieldType != typeof(MaterialProperty))
					continue;
				
				property.SetValue(obj, findProperty(property.Name, props, false));
			}
		}

        public static void DrawMaterialProperty(this MaterialEditor me, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string nameOverride = null)
        {
            if (property is null) return;
            if (property.type == MaterialProperty.PropType.Texture) 
            {
                if (extraProperty2 != null)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                string[] p = property.displayName.Split(':');
                me.TexturePropertySingleLine(new GUIContent(nameOverride ?? p[0], p.Length == 2 ? p[1] : null), property, extraProperty);

                if (extraProperty2 == null)
                {
                    return;
                }
				
                EditorGUILayout.LabelField("");
                var lastRect = GUILayoutUtility.GetLastRect();
                var rect = new Rect(new Vector2(Screen.width/2f+5, lastRect.y), new Vector2(lastRect.size.x-15, lastRect.size.y));
                me.ShaderProperty(rect, extraProperty2, extraProperty2.displayName,4);
                EditorGUILayout.EndHorizontal();
                return;
            }

			me.ShaderProperty(property, property.displayName);
		}
        
        public static void DrawRangedProperty(this MaterialEditor me, MaterialProperty min, MaterialProperty max, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null)
        {
	        float currentMin = min.floatValue;
	        float currentMax = max.floatValue;
	        EditorGUILayout.BeginHorizontal();

	        if(tex is null)
		        EditorGUILayout.LabelField(max.displayName);
	        else
		        me.TexturePropertySingleLine(new GUIContent(tex.displayName), tex);


	        EditorGUI.indentLevel -= 6;
	        EditorGUI.BeginChangeCheck();
	        EditorGUILayout.MinMaxSlider(ref currentMin,ref currentMax, minLimit, maxLimit);
	        if(EditorGUI.EndChangeCheck())
	        {
		        min.floatValue = currentMin;
		        max.floatValue = currentMax;
	        }
	        EditorGUI.indentLevel += 6;
	        EditorGUILayout.EndHorizontal();
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
        
        public static void ToggleKeyword(this Material mat, string keyword, bool toggle)
        {
	        if (toggle)
	        {
		        mat.EnableKeyword(keyword);
		        return;
	        }
		    mat.DisableKeyword(keyword);
        }
        
        public static void SetupMaterialWithBlendMode(Material material, int type)
        {
            switch (type)
            {
                case 0:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_AlphaToMask", 0);
                    material.renderQueue = -1;
                    break;
                case 1: // cutout
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetInt("_AlphaToMask", 1);
                    break;
                case 2: // alpha fade
                    material.SetupTransparentMaterial();
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 3: // premultiply
                    material.SetupTransparentMaterial();
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 4: // additive
	                material.SetupTransparentMaterial();
	                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
	                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
	                break;
                case 5: // multiply
	                material.SetupTransparentMaterial();
	                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
	                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
	                break;
            }
        }

        private static void SetupTransparentMaterial(this Material material)
        {
	        material.SetOverrideTag("RenderType", "Transparent");
	        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
	        material.SetInt("_ZWrite", 0);
	        material.SetInt("_AlphaToMask", 0);
        }
    }
}

