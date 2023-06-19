using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class DefaultInspector : ShaderGUI
    {
        private bool _firstTime = true;
        private static bool _reset = false;
        public static void ReinitializeInspector() => _reset = true;

        private List<Property> _properties = new List<Property>();
        private Shader _curentShader = null;
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            if (_reset)
            {
                _firstTime = true;
                _reset = false;
            }
            var material = (Material)materialEditor.target;
            var shader = material.shader;
            if (_curentShader != shader)
            {
                _firstTime = true;
                _curentShader = shader;
            }

            if (_firstTime)
            {
                InitializeEditor(materialEditor, materialProperties);
                _firstTime = false;
                OnValidate(materialEditor, materialProperties);
            }

            EditorGUI.BeginChangeCheck();
            foreach (var property in _properties)
            {
                DrawPropertyRecursive(property, materialEditor, materialProperties);
            }

            //EditorGUILayout.Space();
            DrawSplitter();
            EditorGUILayout.Space();
            materialEditor.LightmapEmissionProperty();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();

            if (EditorGUI.EndChangeCheck())
            {
                OnValidate(materialEditor, materialProperties);
            }
        }

        private void OnValidate(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            foreach (Material mat in materialEditor.targets)
            {
                _onValidateAction(materialEditor, materialProperties, mat);
                if (mat.HasProperty("_EmissionToggle"))
                {
                    SetupGIFlags(mat.GetFloat("_EmissionToggle"), mat);
                }
            }
        }

        private Action<MaterialEditor, MaterialProperty[], Material> _onValidateAction;

        private void InitializeEditor(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            var material = (Material)materialEditor.target;
            var shader = material.shader;

            _onValidateAction = delegate { };

            Property parent = null;
            _properties.Clear();
            for (int i = 0; i < materialProperties.Length; i++)
            {
                MaterialProperty prop = materialProperties[i];
                var flags = prop.flags;
                var attributes = shader.GetPropertyAttributes(i);
                var type = prop.type;
                string displayName = prop.displayName;

                if ((flags & MaterialProperty.PropFlags.HideInInspector) == MaterialProperty.PropFlags.HideInInspector)
                {
                    continue;
                }

                bool propertyVisible = true;
                string tooltip = null;
                bool linear = false;
                bool toggleGroupStart = false;
                bool toggleGroupEnd = false;
                bool verticalScopeStart = false;
                bool verticalScopeEnd = false;
                bool helpBox = false;
                bool extraProperty = false;
                bool indentLevelAdd = false;
                bool indentLevelRemove = false;
                foreach (var attributeString in attributes)
                {
                    var attribute = attributeString.AsSpan();
                    int length = attribute.Length;

                    var tooltipAttribute = "Tooltip(".AsSpan();
                    if (attribute.StartsWith(tooltipAttribute) && attribute.EndsWith(")".AsSpan()))
                    {
                        tooltip = attribute.Slice(tooltipAttribute.Length, length - tooltipAttribute.Length - 1).ToString();
                    }

                    if (attribute.Equals("Linear".AsSpan(), StringComparison.Ordinal))
                    {
                        linear = true;
                    }

                    if (attribute.Equals("ExtraProperty".AsSpan(), StringComparison.Ordinal))
                    {
                        extraProperty = true;
                    }

                    if (attribute.Equals("Indent".AsSpan(), StringComparison.Ordinal))
                    {
                        indentLevelAdd = true;
                    }
                    if (attribute.Equals("UnIndent".AsSpan(), StringComparison.Ordinal))
                    {
                        indentLevelRemove = true;
                    }

                    if (attribute.Equals("ToggleGroupStart".AsSpan(), StringComparison.Ordinal))
                    {
                        toggleGroupStart = true;
                    }
                    else if (attribute.Equals("ToggleGroupEnd".AsSpan(), StringComparison.Ordinal))
                    {
                        toggleGroupEnd = true;
                    }

                    if (attribute.Equals("VerticalScopeStart".AsSpan(), StringComparison.Ordinal))
                    {
                        verticalScopeStart = true;
                    }
                    else if (attribute.Equals("VerticalScopeEnd".AsSpan(), StringComparison.Ordinal))
                    {
                        verticalScopeEnd = true;
                    }

                    if (prop.type == MaterialProperty.PropType.Texture)
                    {
                        var toggleAttribute = "Toggle(".AsSpan();
                        if (attribute.StartsWith(toggleAttribute) && attribute.EndsWith(")".AsSpan()))
                        {
                            string toggleName = attribute.Slice(toggleAttribute.Length, length - toggleAttribute.Length - 1).ToString();
                            int index = i;
                            _onValidateAction += (editor, props, mat) =>
                            {
                                ToggleKeyword(mat, toggleName, props[index].textureValue != null);
                            };
                        }
                    }

                    if (attribute.Equals("HelpBox".AsSpan(), StringComparison.Ordinal))
                    {
                        helpBox = true;
                    }
                }

                var p = new Property()
                {
                    index = i,
                    displayName = displayName,
                    tooltip = tooltip,
                };


                if (helpBox)
                {
                    p.drawAction = DrawHelpBox;
                }
                else if (prop.name == "_Cutoff" || prop.name == "_CutoutSharpness")
                {
                    p.drawAction = DrawCutoutShaderProperty;
                }
                else if (prop.name.EndsWith("_ScaleOffset"))
                {
                    var texturePropName = prop.name.Replace("_ScaleOffset", string.Empty);
                    p.index = Array.FindIndex(materialProperties, x => x.name.EndsWith(texturePropName));
                    p.drawAction = DrawShaderTextureScaleOffsetProperty;
                }
                else if (prop.name.Equals("_Mode"))
                {
                    p.drawAction = DrawTransparencyModeProperty;
                }
                else if (prop.name.StartsWith("StochasticPreprocessButton"))
                {
                    p.drawAction = DrawStochasticPreprocessButton;
                }
                else if (prop.name.StartsWith("FoldoutMainStart_"))
                {
                    p.drawAction = DrawFoldoutMain;
                    toggleGroupStart = true;
                }
                else if (prop.name.StartsWith("FoldoutMainEnd_"))
                {
                    p.drawAction = DrawSpace;
                    toggleGroupEnd = true;
                }
                else if (prop.name.StartsWith("FoldoutStart_"))
                {
                    p.drawAction = SmallFoldoutStart;
                    toggleGroupStart = true;
                }
                else if (prop.name.StartsWith("FoldoutEnd_"))
                {
                    p.drawAction = SmallFoldoutEnd;

                    propertyVisible = false;
                    toggleGroupEnd = true;
                }
                else if (type == MaterialProperty.PropType.Texture)
                {
                    p.drawAction = DrawShaderTextureProperty;
                    if (extraProperty)
                    {
                        p.drawAction += DrawShaderTexturePropertyExtra;
                    }
                    if (linear)
                    {
                        p.drawAction += DrawLinearWarning;
                    }
                }
                else if (type == MaterialProperty.PropType.Vector)
                {
                    p.drawAction = DrawShaderPropertyVector;
                }
                else
                {
                    p.drawAction = DrawShaderProperty;
                }

                if (prop.type == MaterialProperty.PropType.Texture && (flags & MaterialProperty.PropFlags.NoScaleOffset) != MaterialProperty.PropFlags.NoScaleOffset)
                {
                    p.drawAction += DrawShaderTextureScaleOffsetProperty;
                }

                if (indentLevelAdd)
                {
                    p.drawAction = IndentLevelAdd + p.drawAction;
                }
                if (indentLevelRemove)
                {
                    p.drawAction += IndentLevelRemove;
                }

                if (propertyVisible)
                {
                    if (parent == null)
                    {
                        _properties.Add(p);
                    }
                    else
                    {
                        parent.Add(p);
                    }
                }

                if (verticalScopeStart)
                {
                    p.drawAction = VerticalScopeStart + p.drawAction;
                }
                else if (verticalScopeEnd)
                {
                    p.drawAction += VerticalScopeEnd;
                }

                if (toggleGroupStart)
                {
                    parent = p;
                    if (type == MaterialProperty.PropType.Texture)
                    {
                        p.drawAction += ToggleGroupTexture;
                    }
                    else
                    {
                        p.drawAction += ToggleGroup;
                    }
                }

                if (toggleGroupEnd)
                {
                    parent = parent.Parent;
                }

                if (extraProperty)
                {
                    i++;
                }
            }
        }

        private static void ToggleKeyword(Material mat, string keyword, bool enabled)
        {
            if (enabled)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (material == null || newShader == null)
            {
                return;
            }

            int mode = (int)material.GetFloat("_Mode");
            SetupMaterialWithBlendMode(material, mode);
            SetupTransparencyKeywords(material, mode);
        }

        public static void SetupTransparencyKeywords(Material material, int mode)
        {
            ToggleKeyword(material, "_ALPHATEST_ON", mode == 1);
            ToggleKeyword(material, "_ALPHAFADE_ON", mode == 2);
            ToggleKeyword(material, "_ALPHAPREMULTIPLY_ON", mode == 3);
            ToggleKeyword(material, "_ALPHAMODULATE_ON", mode == 5);
        }

        private void DrawPropertyRecursive(Property property, MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            property.drawAction(property, materialEditor, materialProperties);

            if (property.childrenVisible)
            {
                //EditorGUI.indentLevel++;
                foreach (var child in property.children)
                {
                    DrawPropertyRecursive(child, materialEditor, materialProperties);
                }
                //EditorGUI.indentLevel--;
            }
        }

        public class Property
        {
            public Action<Property, MaterialEditor, MaterialProperty[]> drawAction;
            public int index;
            public bool childrenVisible;
            public List<Property> children;
            public string displayName;
            public string tooltip;

            public GUIContent guiContent => EditorGUIUtility.TrTextContent(displayName, tooltip);

            public Property Parent { get; private set; }

            public void Add(Property p)
            {
                if (p == this)
                {
                    return;
                }

                if (children == null)
                {
                    children = new List<Property>();
                }
                p.Parent = this;
                children.Add(p);
            }
        }



        public void DrawShaderProperty(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => editor.ShaderProperty(unityProperty[property.index], property.guiContent);
        public void DrawShaderPropertyVector(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            var prop = unityProperty[property.index];
            var vectorRect = EditorGUILayout.GetControlRect(true, MaterialEditor.GetDefaultPropertyHeight(prop) / 2, EditorStyles.layerMaskField);
            editor.VectorProperty(vectorRect, prop, property.displayName);
        }

        public void DrawShaderTextureProperty(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => editor.TexturePropertySingleLine(property.guiContent, unityProperty[property.index]);
        public void DrawShaderTexturePropertyExtra(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => TexturePropertySingleLineExtraProp(editor, property.guiContent, unityProperty[property.index+1]);
        public void DrawShaderTextureScaleOffsetProperty(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => editor.TextureScaleOffsetProperty(unityProperty[property.index]);
        public void ToggleGroup(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => property.childrenVisible = unityProperty[property.index].floatValue > 0f;
        public void ToggleGroupTexture(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => property.childrenVisible = unityProperty[property.index].textureValue != null;
        public void DrawHelpBox(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => EditorGUILayout.HelpBox(property.displayName, MessageType.Info);

        public void DrawStochasticPreprocessButton(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            if (editor.targets.Length > 1)
            {
                return;
            }

            if (GUILayout.Button("Stochastic Preprocess (Slow)"))
            {
                var stp = new StochasticTexturingPreprocess();
                stp.ApplyUserStochasticInputChoice((Material)editor.target);
                MaterialEditor.ApplyMaterialPropertyDrawers((Material)editor.target);
            }

            if (GUILayout.Button("Cleanup Unused Textures"))
            {
                var material = (Material)editor.target;
                material.SetTexture("_MainTex", null);
                material.SetTexture("_BumpMap", null);
                material.SetTexture("_MaskMap", null);
                material.SetTexture("_EmissionMap", null);
                MaterialEditor.ApplyMaterialPropertyDrawers((Material)editor.target);
            }
        }

        public void IndentLevelAdd(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => EditorGUI.indentLevel+=2;
        public void IndentLevelRemove(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => EditorGUI.indentLevel-=2;

        // just hard code this for now
        public void DrawCutoutShaderProperty(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            bool cutoutEnabled = Array.Find(unityProperty, x => x.name.Equals("_Mode", StringComparison.Ordinal)).floatValue == 1;
            if (!cutoutEnabled)
            {
                return;
            }
            editor.ShaderProperty(unityProperty[property.index], property.guiContent);
        }

        public void SmallFoldoutStart(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            bool enabled = unityProperty[property.index].floatValue > 0;
            enabled = EditorGUILayout.BeginFoldoutHeaderGroup(enabled, property.guiContent);
            unityProperty[property.index].floatValue = enabled ? 1f : 0f;
        }
        public void SmallFoldoutEnd(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => EditorGUILayout.EndFoldoutHeaderGroup();
        public void DrawSpace(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => EditorGUILayout.Space();

        public void VerticalScopeStart(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            GUILayout.Space(1);
            EditorGUILayout.BeginVertical("box");
        }

        public void VerticalScopeEnd(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(1);
        }

        public void DrawTransparencyModeProperty(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            EditorGUI.BeginChangeCheck();

            editor.ShaderProperty(unityProperty[property.index], property.guiContent);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material target in editor.targets)
                {
                    int mode = (int)target.GetFloat("_Mode");
                    SetupMaterialWithBlendMode(target, mode);
                    ToggleKeyword(target, "_ALPHATEST_ON", mode == 1);
                    ToggleKeyword(target, "_ALPHAFADE_ON", mode == 2);
                    ToggleKeyword(target, "_ALPHAPREMULTIPLY_ON", mode == 3);
                    ToggleKeyword(target, "_ALPHAMODULATE_ON", mode == 5);
                }
            }
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

        public void DrawLinearWarning(Property property, MaterialEditor editor, MaterialProperty[] unityProperty) => sRGBWarning(unityProperty[property.index]);
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
        public static void DrawFoldoutMain(Property property, MaterialEditor editor, MaterialProperty[] unityProperty)
        {
            bool isOpen = unityProperty[property.index].floatValue > 0;
            DrawSplitter();
            isOpen = DrawHeaderFoldout(property.guiContent, isOpen);
            unityProperty[property.index].floatValue = isOpen ? 1 : 0;
            if (isOpen)
            {
                EditorGUILayout.Space();
            }
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
        private static void SetupTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetInt("_ZWrite", 0);
            material.SetInt("_AlphaToMask", 0);
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

        public void TexturePropertySingleLineExtraProp(MaterialEditor editor, GUIContent label, MaterialProperty extraProperty1, MaterialProperty extraProperty2 = null)
        {
            Rect controlRectForSingleLine = GUILayoutUtility.GetLastRect();

            if (controlRectForSingleLine.height > 20)
            {
                // fix offset when there is a normal map fix button
                var pos = controlRectForSingleLine.position;
                controlRectForSingleLine.position = new Vector2(pos.x, pos.y - 42);
            }

            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (extraProperty1 == null || extraProperty2 == null)
            {
                MaterialProperty materialProperty = extraProperty1 ?? extraProperty2;
                if (materialProperty.type == MaterialProperty.PropType.Color)
                {
                    ExtraPropertyAfterTexture(editor, MaterialEditor.GetLeftAlignedFieldRect(controlRectForSingleLine), materialProperty);
                }
                else
                {
                    ExtraPropertyAfterTexture(editor, MaterialEditor.GetRectAfterLabelWidth(controlRectForSingleLine), materialProperty);
                }
            }
            else if (extraProperty1.type == MaterialProperty.PropType.Color)
            {
                ExtraPropertyAfterTexture(editor, MaterialEditor.GetFlexibleRectBetweenFieldAndRightEdge(controlRectForSingleLine), extraProperty2);
                ExtraPropertyAfterTexture(editor, MaterialEditor.GetLeftAlignedFieldRect(controlRectForSingleLine), extraProperty1);
            }
            else
            {
                ExtraPropertyAfterTexture(editor, MaterialEditor.GetRightAlignedFieldRect(controlRectForSingleLine), extraProperty2);
                ExtraPropertyAfterTexture(editor, MaterialEditor.GetFlexibleRectBetweenLabelAndField(controlRectForSingleLine), extraProperty1);
            }

            EditorGUI.indentLevel = indentLevel;
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
