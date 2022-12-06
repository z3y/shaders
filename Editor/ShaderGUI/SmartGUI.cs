using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;


namespace z3y.Shaders
{
    public class SmartGUI : ShaderGUI
    {
        private bool _initialized = false;
        private FieldInfo[] _fieldInfo;
        private int[] _index;

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
                UpdateProperties(materialProperties);
                propertyCount = materialProperties.Length;
                OnValidate(material);
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

        public void Draw(MaterialProperty property, string onHover, string nameOverride = null)
        {
            Draw(property, null, null, onHover, nameOverride);
        }

        public void Draw(MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string onHover = null, string nameOverride = null)
        {
            if (property is null) return;

            Draw(_materialEditor, property, extraProperty, extraProperty2, onHover, nameOverride);
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

        public bool TexturePackingButton()
        {
            var lastRect = GUILayoutUtility.GetLastRect();

            var buttonRect = new Rect(lastRect.x - 16f, lastRect.y +1.5f, 14f, 14f);
            var textRect = new Rect(lastRect.x - 14f, lastRect.y + 0.5f, 17f, 17f);

            if (GUI.Button(buttonRect, new GUIContent("", "Texture Packing")))
            {
                return true;
            }
            EditorGUI.LabelField(textRect, "P");

            return false;
        }


        private bool Draw(MaterialEditor materialEditor, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string onHover = null, string nameOverride = null)
        {
            if (property.type == MaterialProperty.PropType.Texture)
            {
                TexturePropertySingleLine(materialEditor, new GUIContent(nameOverride ?? property.displayName, onHover), property, extraProperty, extraProperty2);

                return property.textureValue != null;
            }
            else if (property.type == MaterialProperty.PropType.Vector)
            {
                var vectorRect = EditorGUILayout.GetControlRect(true, MaterialEditor.GetDefaultPropertyHeight(property) / 2, EditorStyles.layerMaskField);
                materialEditor.VectorProperty(vectorRect, property, property.displayName);
                return false;
            }
            else
            {
                materialEditor.ShaderProperty(property, new GUIContent(nameOverride ?? property.displayName, onHover));
                return property.floatValue == 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void Space() => EditorGUILayout.Space();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void Space(float width) => EditorGUILayout.Space(width);

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
            }
            return isOpen;
        }

        public static void ApplyPresetPartially(Preset preset, Material material, Shader shader, int startIndex = 0)
        {
            var tempMaterial = new Material(shader);
            var defaultProperties = MaterialEditor.GetMaterialProperties(new Material[] { tempMaterial } );
            preset.ApplyTo(tempMaterial);
            var presetProperties = MaterialEditor.GetMaterialProperties(new Material[] { tempMaterial });

            Undo.RecordObject(material, "ApplyingMaterialPropertiesPreset");
            for (int i = startIndex; i < defaultProperties.Length; i++)
            {
                switch (defaultProperties[i].type)
                {
                    case MaterialProperty.PropType.Color:
                        if (defaultProperties[i].colorValue != presetProperties[i].colorValue && material.GetColor(defaultProperties[i].name) == defaultProperties[i].colorValue)
                        {
                            material.SetColor(defaultProperties[i].name, presetProperties[i].colorValue);
                        }
                        break;
                    case MaterialProperty.PropType.Vector:
                        if (defaultProperties[i].vectorValue != presetProperties[i].vectorValue && material.GetVector(defaultProperties[i].name) == defaultProperties[i].vectorValue)
                        {
                            material.SetVector(defaultProperties[i].name, presetProperties[i].vectorValue);
                        }
                        break;
                    case MaterialProperty.PropType.Float:
                    case MaterialProperty.PropType.Range:
                        if (defaultProperties[i].floatValue != presetProperties[i].floatValue && material.GetFloat(defaultProperties[i].name) == defaultProperties[i].floatValue)
                        {
                            material.SetFloat(defaultProperties[i].name, presetProperties[i].floatValue);
                        }
                        break;
                    case MaterialProperty.PropType.Texture:
                        if (presetProperties[i].textureValue && defaultProperties[i].textureValue != presetProperties[i].textureValue && material.GetTexture(defaultProperties[i].name) == defaultProperties[i].textureValue)
                        {
                            material.SetTexture(defaultProperties[i].name, presetProperties[i].textureValue);
                        }
                        break;
                }
            }


            UnityEngine.Object.DestroyImmediate(tempMaterial);

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

        public void DrawMinMax(MaterialProperty minMax, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null)
        {
            float currentMin = minMax.vectorValue.x;
            float currentMax = minMax.vectorValue.y;
            EditorGUILayout.BeginHorizontal();

            if (tex is null)
                EditorGUILayout.LabelField(minMax.displayName);
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

        public static MethodInfo _getShaderLocalKeywords = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic);

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

        public const byte k_MaxByteForOverexposedColor = 191;
        public static void DecomposeHdrColor(Color linearColorHdr, out Color32 baseLinearColor, out float exposure)
        {
            baseLinearColor = linearColorHdr;
            var maxColorComponent = linearColorHdr.maxColorComponent;
            // replicate Photoshops's decomposition behaviour
            if (maxColorComponent == 0f || maxColorComponent <= 1f && maxColorComponent >= 1 / 255f)
            {
                exposure = 0f;
                baseLinearColor.r = (byte)Mathf.RoundToInt(linearColorHdr.r * 255f);
                baseLinearColor.g = (byte)Mathf.RoundToInt(linearColorHdr.g * 255f);
                baseLinearColor.b = (byte)Mathf.RoundToInt(linearColorHdr.b * 255f);
            }
            else
            {
                // calibrate exposure to the max float color component
                var scaleFactor = k_MaxByteForOverexposedColor / maxColorComponent;
                exposure = Mathf.Log(255f / scaleFactor) / Mathf.Log(2f);
                // maintain maximal integrity of byte values to prevent off-by-one errors when scaling up a color one component at a time
                baseLinearColor.r = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.r));
                baseLinearColor.g = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.g));
                baseLinearColor.b = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.b));
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
                case 1: // cutout a2c
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