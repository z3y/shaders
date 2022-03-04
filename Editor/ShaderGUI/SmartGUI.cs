using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace z3y.Shaders
{
    public class SmartGUI : ShaderGUI
    {
        private bool _initialized = false;
        private FieldInfo[] _fieldInfo;
        private int[] _index;
        private bool[] _isDrawn;

        private MaterialEditor _materialEditor;
        private MaterialProperty[] _materialProperties;
        private int propertyCount = 0;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            var material = materialEditor.target as Material;

            if (!_initialized || propertyCount != materialProperties.Length)
            {
                _materialEditor = materialEditor;
                Initialize(materialProperties);
                OnValidate(material);
                propertyCount = materialProperties.Length;
                _initialized = true;
            }

            UpdateProperties(materialProperties);
            _materialProperties = materialProperties;


            EditorGUI.BeginChangeCheck();
            OnGUIProperties(materialEditor, materialProperties, material);
            if (EditorGUI.EndChangeCheck())
            {
                OnValidate(material);
            };
        }

        public void Draw(MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string onHover = null, string nameOverride = null)
        {
            if (property is null) return;

            Draw(_materialEditor, property, extraProperty, extraProperty2, onHover, nameOverride);

            int currentIndex = Array.IndexOf(_materialProperties, property);
            currentIndex++;
            if (extraProperty != null) currentIndex++;
            if (extraProperty2 != null) currentIndex++;

            DrawRest(currentIndex);
        }

        public virtual bool DrawAll() => true;

        private void DrawRest(int currentIndex)
        {
            if (DrawAll())
            {
                for (int i = currentIndex; i < _isDrawn.Length; i++)
                {
                    if (_isDrawn[i])
                    {
                        break;
                    }
                    Draw(_materialEditor, _materialProperties[i]);
                }
            }
        }

        private void Draw(MaterialEditor materialEditor, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string onHover = null, string nameOverride = null)
        {
            if (property.type == MaterialProperty.PropType.Texture)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(nameOverride ?? property.displayName, onHover), property, extraProperty, extraProperty2);
            }
            else
            {
                materialEditor.ShaderProperty(property, new GUIContent(nameOverride ?? property.displayName, onHover));
            }
        }

        /// <summary> Draws a foldout and saves the state in the material property</summary>
        public bool Foldout(MaterialProperty foldout)
        {
            bool isOpen = foldout.floatValue == 1;
            DrawSplitter();
            isOpen = DrawHeaderFoldout(foldout.displayName, isOpen);
            foldout.floatValue = isOpen ? 1 : 0;
            if (isOpen)
            {
                EditorGUILayout.Space();
                int currentIndex = Array.IndexOf(_materialProperties, foldout);
                DrawRest(currentIndex + 1);
            }
            return isOpen;
        }

        public void DrawMinMax(MaterialProperty min, MaterialProperty max, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null)
        {
            float currentMin = min.floatValue;
            float currentMax = max.floatValue;
            EditorGUILayout.BeginHorizontal();

            if (tex is null)
                EditorGUILayout.LabelField(max.displayName);
            else
                _materialEditor.TexturePropertySingleLine(new GUIContent(tex.displayName), tex);


            EditorGUI.indentLevel -= 6;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref currentMin, ref currentMax, minLimit, maxLimit);
            if (EditorGUI.EndChangeCheck())
            {
                min.floatValue = currentMin;
                max.floatValue = currentMax;
            }
            EditorGUI.indentLevel += 6;
            EditorGUILayout.EndHorizontal();
        }

        public static void VerticalScopeBox(Action action)
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

        public void ResetProperty(Material material, MaterialProperty[] materialProperty)
        {
            for (int i = 0; i < materialProperty.Length; i++)
            {
                ResetProperty(material, materialProperty[i]);
            }
        }

        public void ResetProperty(Material material, MaterialProperty materialProperty)
        {
            int propIndex = material.shader.FindPropertyIndex(materialProperty.name);

            switch (materialProperty?.type)
            {
                case MaterialProperty.PropType.Vector:
                    materialProperty.vectorValue = material.shader.GetPropertyDefaultVectorValue(propIndex);
                    break;
                case MaterialProperty.PropType.Color:
                    materialProperty.colorValue = material.shader.GetPropertyDefaultVectorValue(propIndex);
                    break;
                case MaterialProperty.PropType.Texture:
                    materialProperty.textureValue = null;
                    break;
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    materialProperty.floatValue = material.shader.GetPropertyDefaultFloatValue(propIndex);
                    break;
            }
        }

        public static bool TextureFoldout(MaterialProperty floatProperty)
        {
            floatProperty.floatValue = TextureFoldout(floatProperty.floatValue == 1) ? 1 : 0;
            if (floatProperty.floatValue != 1)
            {
                return false;
            }
            return true;
        }

        public static bool TextureFoldout(bool display)
        {
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

        private void Initialize(MaterialProperty[] materialProperties)
        {
            _fieldInfo = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.FieldType == typeof(MaterialProperty)).ToArray();
            _index = new int[_fieldInfo.Length];
            
            for (int i = 0; i < _fieldInfo.Length; i++)
            {
                _index[i] = Array.FindIndex(materialProperties, x => x.name.Equals(_fieldInfo[i].Name, StringComparison.OrdinalIgnoreCase));
            }
            if (DrawAll())
            {
                _isDrawn = new bool[materialProperties.Length];

                for (int i = 0; i < materialProperties.Length; i++)
                {
                    _isDrawn[i] = Array.FindIndex(_fieldInfo, x => x.Name.Equals(materialProperties[i].name, StringComparison.OrdinalIgnoreCase)) != -1;
                    _isDrawn[i] |= materialProperties[i].flags.ToString().Contains("HideInInspector");
                }

            }
        }

        private void UpdateProperties(MaterialProperty[] materialProperties)
        {
            for (int i = 0; i < _fieldInfo.Length; i++)
            {
                if (_index[i] != -1)
                {
                    _fieldInfo[i].SetValue(this, materialProperties[_index[i]]);
                }
            }
        }


        public virtual void OnValidate(Material material)
        {

        }

        public virtual void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
        {
            for (int i = 0; i < materialProperties.Length; i++)
            {
                Draw(materialEditor, materialProperties[i]);
            }
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

        public void SetupBlendMode(MaterialEditor materialEditor, MaterialProperty mode)
        {
            foreach (var o in materialEditor.targets)
            {
                var m = (Material)o;
                SetupMaterialWithBlendMode(m, (int)mode.floatValue);
            }
        }

        public virtual void SetupMaterialWithBlendMode(Material material, int type)
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
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 3: // premultiply
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 4: // additive
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    break;
                case 5: // multiply
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;
            }
        }

        private static void SetupTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetInt("_ZWrite", 0);
            material.SetInt("_AlphaToMask", 0);
        }


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

    }
    public static class SmartGUIExtensions
    {
        public static void ToggleKeyword(this Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
                return;
            }
            material.DisableKeyword(keyword);
        }
    }
}