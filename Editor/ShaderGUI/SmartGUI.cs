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


        private static Type[] _editors = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => typeof(SmartGUI).IsAssignableFrom(x) && x.IsClass).ToArray();
        public List<SmartGUI> _modulesInUse = new List<SmartGUI>();
        private const string ModulePrefix = "CustomEditor_";

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            var material = materialEditor.target as Material;

            if (!_initialized || propertyCount != materialProperties.Length)
            {
                if (GetType() == typeof(SmartGUI))
                {
                    ParseModules(materialProperties);
                }
                _materialEditor = materialEditor;
                Initialize(materialProperties);
                UpdateProperties(materialProperties);
                propertyCount = materialProperties.Length;
                OnValidate(material);
                _initialized = true;
            }

            if (GetType() == typeof(SmartGUI))
            {
                for (int i = 0; i < _modulesInUse.Count; i++)
                {
                    _modulesInUse[i].OnGUI(materialEditor, materialProperties);
                }
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

        private void ParseModules(MaterialProperty[] materialProperties)
        {
            _modulesInUse.Clear();
            for (int i = 0; i < materialProperties.Length; i++)
            {
                if (materialProperties[i].name.StartsWith(ModulePrefix, StringComparison.Ordinal))
                {
                    var name = materialProperties[i].name.Remove(0, ModulePrefix.Length);
                    int moduleIndex = Array.FindIndex(_editors, x => x.Name == name);
                    if (moduleIndex == -1)
                    {
                        continue;
                    }

                    var module = (SmartGUI)Activator.CreateInstance(_editors[moduleIndex]);
                    _modulesInUse.Add(module);
                }
            }
        }

        public void Draw(MaterialProperty property, string onHover, string nameOverride = null)
        {
            Draw(property, null, null, onHover, nameOverride);
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

        public virtual bool DrawAll() => false;

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
        

        private Rect GetControlRectForSingleLine()
        {
            return EditorGUILayout.GetControlRect(true, 20f, EditorStyles.layerMaskField);
        }
        private void ExtraPropertyAfterTexture(MaterialEditor materialEditor, Rect r, MaterialProperty property)
        {
            if ((property.type == MaterialProperty.PropType.Float || property.type == MaterialProperty.PropType.Color) && r.width > EditorGUIUtility.fieldWidth)
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = r.width - EditorGUIUtility.fieldWidth - 2f;

                var labelRect = new Rect(r.x, r.y, r.width - EditorGUIUtility.fieldWidth - 4f, r.height);
                var style = new GUIStyle("label");
                style.alignment = TextAnchor.MiddleRight;
                EditorGUI.LabelField(labelRect, property.displayName, style);
                materialEditor.ShaderProperty(r, property, " ");
                EditorGUIUtility.labelWidth = labelWidth;
            }
            else
            {
                materialEditor.ShaderProperty(r, property, string.Empty);
            }
        }

        public Rect TexturePropertySingleLine(MaterialEditor materialEditor, GUIContent label, MaterialProperty textureProp, MaterialProperty extraProperty1, MaterialProperty extraProperty2)
        {
            Rect controlRectForSingleLine = GetControlRectForSingleLine();
            materialEditor.TexturePropertyMiniThumbnail(controlRectForSingleLine, textureProp, label.text, label.tooltip);
            if (extraProperty1 == null && extraProperty2 == null)
            {
                return controlRectForSingleLine;
            }

            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (extraProperty1 == null || extraProperty2 == null)
            {
                MaterialProperty materialProperty = extraProperty1 ?? extraProperty2;
                if (materialProperty.type == MaterialProperty.PropType.Color)
                {
                    ExtraPropertyAfterTexture(materialEditor, MaterialEditor.GetLeftAlignedFieldRect(controlRectForSingleLine), materialProperty);
                }
                else
                {
                    ExtraPropertyAfterTexture(materialEditor, MaterialEditor.GetRectAfterLabelWidth(controlRectForSingleLine), materialProperty);
                }
            }
            else if (extraProperty1.type == MaterialProperty.PropType.Color)
            {
                ExtraPropertyAfterTexture(materialEditor, MaterialEditor.GetFlexibleRectBetweenFieldAndRightEdge(controlRectForSingleLine), extraProperty2);
                ExtraPropertyAfterTexture(materialEditor, MaterialEditor.GetLeftAlignedFieldRect(controlRectForSingleLine), extraProperty1);
            }
            else
            {
                ExtraPropertyAfterTexture(materialEditor, MaterialEditor.GetRightAlignedFieldRect(controlRectForSingleLine), extraProperty2);
                ExtraPropertyAfterTexture(materialEditor, MaterialEditor.GetFlexibleRectBetweenLabelAndField(controlRectForSingleLine), extraProperty1);
            }

            EditorGUI.indentLevel = indentLevel;
            return controlRectForSingleLine;
        }


        private bool Draw(MaterialEditor materialEditor, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string onHover = null, string nameOverride = null)
        {
            if (property.type == MaterialProperty.PropType.Texture)
            {
                TexturePropertySingleLine(materialEditor, new GUIContent(nameOverride ?? property.displayName, onHover), property, extraProperty, extraProperty2);

                return property.textureValue != null;
            }
            else
            {
                materialEditor.ShaderProperty(property, new GUIContent(nameOverride ?? property.displayName, onHover));
                return property.floatValue == 1;
            }
        }

        /// <summary> Draws a foldout and saves the state in the material property</summary>
        public bool Foldout(MaterialProperty foldout)
        {
            bool isOpen = foldout.floatValue == 1;
            DrawSplitter();
            isOpen = DrawHeaderFoldout( new GUIContent (foldout.displayName), isOpen);
            foldout.floatValue = isOpen ? 1 : 0;
            if (isOpen)
            {
                EditorGUILayout.Space();
                int currentIndex = Array.IndexOf(_materialProperties, foldout);
                DrawRest(currentIndex + 1);
            }
            return isOpen;
        }


        public void DrawMinMax(MaterialProperty min, MaterialProperty max, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null, string tooltip = null)
        {
            float currentMin = min.floatValue;
            float currentMax = max.floatValue;
            EditorGUILayout.BeginHorizontal();

            if (tex is null)
                EditorGUILayout.LabelField(new GUIContent(min.displayName, tooltip));
            else
                _materialEditor.TexturePropertySingleLine(new GUIContent(tex.displayName), tex);


            var rect = GUILayoutUtility.GetLastRect();
            rect = MaterialEditor.GetRectAfterLabelWidth(rect);
            float offset = 28f;
            rect.width += offset;
            rect.position = new Vector2(rect.x- offset, rect.y);

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(rect, ref currentMin, ref currentMax, minLimit, maxLimit);
            if (EditorGUI.EndChangeCheck())
            {
                min.floatValue = currentMin;
                max.floatValue = currentMax;
            }

            if (min.floatValue > max.floatValue)
            {
                min.floatValue = max.floatValue - 0.001f;
                if (min.floatValue < minLimit)
                {
                    min.floatValue = minLimit;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        public void DrawMinMax(MaterialProperty minMax, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null, string tooltip = null)
        {
            float currentMin = minMax.vectorValue.x;
            float currentMax = minMax.vectorValue.y;
            EditorGUILayout.BeginHorizontal();

            if (tex is null)
                EditorGUILayout.LabelField(new GUIContent(minMax.displayName, tooltip));
            else
                _materialEditor.TexturePropertySingleLine(new GUIContent(tex.displayName), tex);


            var rect = GUILayoutUtility.GetLastRect();
            rect = MaterialEditor.GetRectAfterLabelWidth(rect);
            float offset = 28f;
            rect.width += offset;
            rect.position = new Vector2(rect.x - offset, rect.y);

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(rect, ref currentMin, ref currentMax, minLimit, maxLimit);
            if (EditorGUI.EndChangeCheck())
            {
                minMax.vectorValue = new Vector2(currentMin, currentMax);
            }

            if (minMax.vectorValue.x > minMax.vectorValue.y)
            {
                minMax.vectorValue = new Vector2(minMax.vectorValue.y - 0.001f, minMax.vectorValue.y);
                if (minMax.vectorValue.x < minLimit)
                {
                    minMax.vectorValue = new Vector2(minLimit, minMax.vectorValue.y);
                }
            }

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
            bool isOpen = floatProperty.floatValue == 1;
            TextureFoldout(ref isOpen);
            floatProperty.floatValue = isOpen ? 1 : 0;
            if (floatProperty.floatValue != 1)
            {
                return false;
            }
            return true;
        }

        public static bool TextureFoldout(ref bool display)
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
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
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

        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false)
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


            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;
            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));


            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
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
    internal static class SmartGUIExtensions
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