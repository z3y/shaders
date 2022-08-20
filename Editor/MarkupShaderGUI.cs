using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace z3y
{
    public class MarkupShaderGUI : ShaderGUI
    {
        private bool _firstTime = true;
        private int _cachedPropertyLength = 0;

        private List<Action<MaterialEditor, MaterialProperty[]>> _propertyDrawers = new List<Action<MaterialEditor, MaterialProperty[]>>();
        private List<MaterialProperty> _skipProperty = new List<MaterialProperty>();
        
        private enum PropertyType
        {
            DefaultProperty,
            Foldout,
            TilingOffset,
            MinMax,
            OverrideAction,
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == newShader) return;

            foreach (var materialShaderKeyword in material.shaderKeywords)
            {
                material.DisableKeyword(materialShaderKeyword);
            }
        }

        public void CustomDrawerExample(MaterialEditor materialEditor, MaterialProperty[] properties, MaterialProperty property)
        {
            EditorGUILayout.LabelField("Custom Drawer");
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            
            var e = Event.current;
            EditorGUI.indentLevel++;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
            {
                _firstTime = true;
            }

            if (_cachedPropertyLength != properties.Length)
            {
                _firstTime = true;
            }
            if (_firstTime)
            {
                OnInitialize(materialEditor, properties);
                _cachedPropertyLength = properties.Length;
                _firstTime = false;
            }
            

            foreach (var propertyDrawer in _propertyDrawers)
            {
                propertyDrawer.Invoke(materialEditor, properties);
            }

            EditorGUI.indentLevel = 0;
            EditorGUILayout.Space();
            DrawSplitter();
            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
            materialEditor.LightmapEmissionProperty();

        }

        private static void DrawDebugKeywords(Material material)
        {
            foreach (var materialShaderKeyword in material.shaderKeywords)
            {
                EditorGUILayout.LabelField(materialShaderKeyword);
            }
        }


        private static void ToggleKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
                return;
            }
            material.DisableKeyword(keyword);
        }

        private static TextAnchor ParseAnchor(string input)
        {
            var anchor = TextAnchor.MiddleLeft;
                input = input.Trim(' ');
                
            if (input.Equals("MiddleRight", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.MiddleRight;
            else if (input.Equals("MiddleCenter", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.MiddleCenter;
            else if (input.Equals("MiddleLeft", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.MiddleLeft;
            else if (input.Equals("UpperCenter", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.UpperCenter;
            else if (input.Equals("UpperLeft", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.UpperLeft;
            else if (input.Equals("UpperRight", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.UpperRight;
            else if (input.Equals("LowerCenter", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.LowerCenter;
            else if (input.Equals("LowerRight", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.LowerRight;
            else if (input.Equals("LowerLeft", StringComparison.OrdinalIgnoreCase)) anchor = TextAnchor.LowerLeft;

            return anchor;
        }

        private static string TrimAttribute(string input, string trim)
        {
            var trimmed = input.Remove(0, trim.Length);
            return trimmed.Remove(trimmed.Length - 1, 1);
        }
        private void InitializePropertyDrawers(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _propertyDrawers.Clear();
            _skipProperty.Clear();
            var material = materialEditor.target as Material;
            var targets = materialEditor.targets;
            var materials = targets.Select(target => target as Material).ToArray();

            var shader = material.shader;

            int currentFoldout = -1;
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var propertyIndex = i;
                PropertyType type = 0;
                var onValidate = new List<Action<MaterialEditor, MaterialProperty[]>>();
                var onValidateOnce = new List<Action<MaterialEditor, MaterialProperty[]>>();
                var additionalActions = new List<Action<MaterialEditor, MaterialProperty[]>>();
                var additionalActionsEnd = new List<Action<MaterialEditor, MaterialProperty[]>>();
                var conditionalHide = new List<Func<MaterialEditor, MaterialProperty[], bool>>();
                var overrideAction = new List<Action<MaterialEditor, MaterialProperty[]>>();

                bool isLinear = false;



                if ((property.flags & MaterialProperty.PropFlags.HideInInspector) != 0) continue;
                if (_skipProperty.Contains(property)) continue;

#if !LTCGI_INCLUDED
                foreach(var mat in materials)
                {
                    mat.DisableKeyword("LTCGI");
                    mat.SetFloat("_LTCGI", 0);
                }
                if (property.name.StartsWith("_LTCGI", StringComparison.Ordinal)) continue;
#else

#endif

                string tooltip = string.Empty;
                int extraPropertyIndex = -1;
                int extraProperty2Index = -1;

                int indentLevel = 0;

                var attributes = shader.GetPropertyAttributes(i);


                if (currentFoldout >= 0)
                {
                    var foldout = currentFoldout;
                    Func<MaterialEditor, MaterialProperty[], bool> func = (_, props) =>
                    {
                        if (attributes.Contains("Foldout") && props[foldout].floatValue == 1) EditorGUILayout.Space(10);
                        return props[foldout].floatValue == 0 && !attributes.Contains("Foldout") && !attributes.Contains("EndFoldout");
                    };
                    conditionalHide.Add(func);
                }

                foreach (var attribute in attributes)
                {
                    if (attribute.Equals("EndFoldout", StringComparison.Ordinal))
                    {
                        currentFoldout = -1;
                    }
                    else if (attribute.StartsWith("Tooltip(", StringComparison.Ordinal))
                    {
                        tooltip = TrimAttribute(attribute, "Tooltip(");
                    }
                    else if (attribute.StartsWith("Indent(", StringComparison.Ordinal))
                    {
                        var indent = TrimAttribute(attribute, "Indent(");
                        indentLevel = int.Parse(indent);
                    }
                    else if (attribute.StartsWith("ExtraProperty(", StringComparison.Ordinal))
                    {
                        var name = TrimAttribute(attribute, "ExtraProperty(").Split(',');
                        if (name.Length >= 1)
                        {
                            extraPropertyIndex = Array.FindIndex(properties, x => x.name.Equals(name[0].Trim(' '), StringComparison.Ordinal));
                            _skipProperty.Add(properties[extraPropertyIndex]);
                        }
                        if (name.Length >= 2)
                        {
                            extraProperty2Index = Array.FindIndex(properties, x => x.name.Equals(name[1].Trim(' '), StringComparison.Ordinal));
                            _skipProperty.Add(properties[extraProperty2Index]);
                        }
                    }
                    
                    else if (property.type == MaterialProperty.PropType.Texture && attribute.StartsWith("Toggle(", StringComparison.Ordinal))
                    {
                        var keyword = TrimAttribute(attribute, "Toggle(");
                        var i1 = i;
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            foreach (var material1 in materials)
                            {
                                ToggleKeyword(material1, keyword, propertiesLocal[i1].textureValue != null);
                            }

                        };
                        onValidate.Add(action);
                    }
                    else if (property.type == MaterialProperty.PropType.Texture && attribute.StartsWith("ToggleOff(", StringComparison.Ordinal))
                    {
                        var keyword = TrimAttribute(attribute, "ToggleOff(");
                        var i1 = i;
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            foreach (var material1 in materials)
                            {
                                ToggleKeyword(material1, keyword,propertiesLocal[i1].textureValue == null);
                            }
                        };
                        onValidate.Add(action);
                    }
                    
                    else if (attribute.Equals("MinMax", StringComparison.Ordinal))
                    {
                        type = PropertyType.MinMax;
                    }
                    
                    else if (attribute.Equals("TilingOffset", StringComparison.Ordinal))
                    {
                        type = PropertyType.TilingOffset;
                        extraPropertyIndex = Array.FindIndex(properties, x => x.name.Equals(property.displayName));
                        

                    }
                    
                    else if (attribute.Equals("Foldout", StringComparison.Ordinal))
                    {
                        currentFoldout = i;
                        type = PropertyType.Foldout;
                    }
                    
                    else if (attribute.StartsWith("HideIf(", StringComparison.Ordinal))
                    {
                        var conditionProp = TrimAttribute(attribute, "HideIf(");
                        var arr = conditionProp.Split(new [] { "is" }, StringSplitOptions.None);
                        conditionProp = arr[0].Trim(' ');
                        int idx = Array.FindIndex(properties, x => x.name.Equals(conditionProp));
                        if (idx == -1) continue;
                        
                        Func<MaterialEditor, MaterialProperty[], bool> func;
                        if (properties[idx].type == MaterialProperty.PropType.Texture)
                        {
                            func = (_, props) => props[idx].textureValue;
                        }
                        else
                        {
                            var cdValue = float.Parse(arr[1].Trim(' '));
                            func = (_, props) => props[idx].floatValue == cdValue;
                        }
                        conditionalHide.Add(func);
                    }
                    else if (attribute.StartsWith("ShowIf(", StringComparison.Ordinal))
                    {
                        var conditionProp = TrimAttribute(attribute, "ShowIf(");
                        var arr = conditionProp.Split(new [] { "is" }, StringSplitOptions.None);
                        conditionProp = arr[0].Trim(' ');
                        int idx = Array.FindIndex(properties, x => x.name.Equals(conditionProp));
                        if (idx == -1) continue;
                        Func<MaterialEditor, MaterialProperty[], bool> func;
                        if (properties[idx].type == MaterialProperty.PropType.Texture)
                        {
                            func = (_, props) => !props[idx].textureValue;
                        }
                        else
                        {
                            var cdValue = float.Parse(arr[1].Trim(' '));
                            func = (_, props) => props[idx].floatValue != cdValue;
                        }

                        conditionalHide.Add(func);
                    }
                    
                    else if (attribute.StartsWith("AdvancedKeywordEnum(", StringComparison.Ordinal))
                    {
                        var args = TrimAttribute(attribute, "AdvancedKeywordEnum(").Trim(' ').Split(',');

                        for (int j = 0; j < args.Length; j+=2)
                        {
                            string keyword = args[j].Trim(' ');
                            int enumValue = int.Parse(args[j + 1].Trim(' '));
                            if (keyword.Equals("_")) continue;
                            
                            var i1 = i;
                            Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                            {
                                foreach (var material1 in materials)
                                {
                                    ToggleKeyword(material1, keyword, propertiesLocal[i1].floatValue == enumValue);
                                }
                            };
                            onValidate.Add(action);
                        }
                    }

                    else if (attribute.Equals("Linear", StringComparison.Ordinal))
                    {
                        isLinear = true;
                    }
                    
                    else if (attribute.StartsWith("VerticalScopeBegin(", StringComparison.Ordinal))
                    {
                        var style = TrimAttribute(attribute, "VerticalScopeBegin(");
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            GUILayout.BeginVertical(new GUIStyle(style));
                            EditorGUILayout.Space(1);
                        };
                        additionalActions.Add(action);
                    }
                    
                    else if (attribute.Equals("VerticalScopeEnd", StringComparison.Ordinal))
                    {
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            EditorGUILayout.Space(1);
                            GUILayout.EndVertical();
                        };
                        
                        additionalActionsEnd.Add(action);
                    }
                    
                    else if (attribute.StartsWith("HorizontalScopeBegin(", StringComparison.Ordinal))
                    {
                        var style = TrimAttribute(attribute, "HorizontalScopeBegin(");
                        var guiStyle = new GUIStyle(style);
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            GUILayout.BeginHorizontal(guiStyle);
                            EditorGUILayout.Space(1);
                        };
                        additionalActions.Add(action);
                    }
                    
                    else if (attribute.Equals("HorizontalScopeEnd", StringComparison.Ordinal))
                    {
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            EditorGUILayout.Space(1);
                            GUILayout.EndHorizontal();
                        };
                        
                        additionalActionsEnd.Add(action);
                    }
                    
                    else if (attribute.StartsWith("SetValue(", StringComparison.Ordinal))
                    {
                        var args = TrimAttribute(attribute, "SetValue(").Split(',');

                        var propertyName = args[0].Trim(' ');
                        var setProperty = Array.FindIndex(properties,x => x.name.Equals(propertyName, StringComparison.Ordinal));
                        
                        if (setProperty != -1)
                        {
                            Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                            {
                                foreach (var m in materials)
                                {
                                    if (float.TryParse(
                                            args[(int)propertiesLocal[propertyIndex].floatValue + 1].Trim(' '),
                                            out var value))
                                        m.SetFloat(propertiesLocal[setProperty].name, value);
                                }
                            };
                            onValidateOnce.Add(action);
                        }
                        else if (propertyName.Equals("renderQueue", StringComparison.Ordinal))
                        {
                            Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                            {
                                foreach (var m in materials)
                                {
                                    if (int.TryParse(args[(int)propertiesLocal[propertyIndex].floatValue + 1].Trim(' '),
                                            out var value))
                                        m.renderQueue = value;
                                }

                            };
                            onValidateOnce.Add(action);
                        }
                    }
                    
                    else if (attribute.StartsWith("SetTag(", StringComparison.Ordinal))
                    {
                        var args = TrimAttribute(attribute, "SetTag(").Split(',');
                        var tagName = args[0].Trim(' ');

                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            foreach (var m in materials)
                            {
                                m.SetOverrideTag(tagName,
                                    args[(int)propertiesLocal[propertyIndex].floatValue + 1].Trim(' '));
                            }
                        };
                        onValidateOnce.Add(action);
                    }
                    
                    else if (attribute.StartsWith("Label(", StringComparison.Ordinal))
                    {
                        var args = TrimAttribute(attribute, "Label(").Split(',');
                        var style = args[0].Trim(' ');
                        type = PropertyType.OverrideAction;
                        var anchor = TextAnchor.MiddleLeft;
                        if (args.Length == 2)
                        {
                            anchor = ParseAnchor(args[1]);
                        }
                        
                        var data = properties[propertyIndex].displayName.Split(new [] { @"\n" }, StringSplitOptions.None);
                        var labelText = string.Join("\n", data);


                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            EditorGUILayout.LabelField(labelText, new GUIStyle(style)
                            {
                                fontSize = 12, wordWrap = true, richText = true, alignment = anchor
                            });
                        };
                        overrideAction.Add(action);
                    }
                    
                    else if (attribute.Equals("Empty", StringComparison.Ordinal))
                    {
                        type = PropertyType.OverrideAction;
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
  
                        };
                        overrideAction.Add(action);
                    }
                    
                    else if (attribute.StartsWith("Image(", StringComparison.Ordinal))
                    {
                        type = PropertyType.OverrideAction;
                        var args = TrimAttribute(attribute, "Image(").Trim(' ');
                        var anchor = ParseAnchor(args);

                        var path = property.displayName;
                        var tex = Resources.Load(path) as Texture;
                        var style = new GUIStyle("label")
                        {
                            alignment = anchor
                        };
                        
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            GUILayout.Box(tex, style);
                        };
                        overrideAction.Add(action);
                    }
                    
                    else if (attribute.StartsWith("MarkupSpace(", StringComparison.Ordinal))
                    {
                        var args = TrimAttribute(attribute, "MarkupSpace(").Trim(' ');

                        int width = int.Parse(args);

                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            EditorGUILayout.Space(width);
                        };
                        additionalActions.Add(action);
                    }

                    else if (attribute.StartsWith("TexturePacking(", StringComparison.Ordinal))
                    {
                        var args = TrimAttribute(attribute, "TexturePacking(").Trim(' ').Split(',');

                        var params0 = args[0].Trim(' ').Split('#');
                        var params1 = args[1].Trim(' ').Split('#');
                        var params2 = args[2].Trim(' ').Split('#');
                        var params3 = args[3].Trim(' ').Split('#');

                        var textureToSet = property.name;
                        var textureToSetProperty = property;

                        bool isOpen = false;
                        if (_foldoutOpen.ContainsKey(textureToSet))
                        {
                            _foldoutOpen.TryGetValue(textureToSet, out isOpen);
                        }
                        else
                        {
                            _foldoutOpen.Add(textureToSet, isOpen);
                        }

                        int offset = EditorGUI.indentLevel;
                        
                        _foldoutOpen[textureToSet] = isOpen;

                        var packingTextureNames = new string[4];
                        packingTextureNames[0] = params0[0];
                        packingTextureNames[1] = params1[0];
                        packingTextureNames[2] = params2[0];
                        packingTextureNames[3] = params3[0];

                        var defaultWhite = new bool[4];
                        defaultWhite[0] = params0[1].Equals("white", StringComparison.OrdinalIgnoreCase);
                        defaultWhite[1] = params1[1].Equals("white", StringComparison.OrdinalIgnoreCase);
                        defaultWhite[2] = params2[1].Equals("white", StringComparison.OrdinalIgnoreCase);
                        defaultWhite[3] = params3[1].Equals("white", StringComparison.OrdinalIgnoreCase);


                        var packingField = new PackingField[4];
                        if (_packingFields.ContainsKey(textureToSet))
                        {
                            _packingFields.TryGetValue(textureToSet, out packingField);
                        }
                        else
                        {
                            packingField[1].channelSelect = (TexturePacking.ChannelSelect)1;
                            packingField[2].channelSelect = (TexturePacking.ChannelSelect)2;
                            _packingFields.Add(textureToSet, packingField);
                        }
                        Action<MaterialEditor, MaterialProperty[]> action = (materialEditorLocal, propertiesLocal) =>
                        {
                            if (!TriangleFoldout(ref isOpen, offset))
                            {
                                _foldoutOpen[textureToSet] = isOpen;
                                return;
                            }

                            GUILayout.BeginVertical("Helpbox");
                            for (int j = 0; j < packingField.Length; j++)
                            {
                                packingField[j].isWhite = defaultWhite[j];

                                if (packingTextureNames[j].Equals("none", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                TexturePackingField(ref packingField[j].texture, ref packingField[j].channelSelect, ref packingField[j].invert, packingTextureNames[j]);
                            }

                            GUILayout.Space(1);
                            using (new EditorGUILayout.VerticalScope("box"))
                            {
                                GUILayout.Space(1);
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Pack"))
                                {
                                    Pack(textureToSetProperty, packingField[0], packingField[1], packingField[2], packingField[3], isLinear);
                                }

                                if (GUILayout.Button("Clear"))
                                {
                                    for (int j = 0; j < packingField.Length; j++)
                                    {
                                        packingField[j] = new PackingField();
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                                GUILayout.Space(1);
                            }
                            GUILayout.Space(1);
                            GUILayout.EndVertical();
                            GUILayout.Space(10);
                        };

                        additionalActionsEnd.Add(action);

                    }
                    




                }

                bool firstTime = true;
                Action<MaterialEditor, MaterialProperty[]> drawer = (materialEditorLocal, propertiesLocal) =>
                {
                    EditorGUI.indentLevel = indentLevel;
                    EditorGUI.BeginChangeCheck();
                    MaterialProperty propertyLocal = propertiesLocal[propertyIndex];

                    bool hide = false;
                    foreach (Func<MaterialEditor,MaterialProperty[],bool> func in conditionalHide)
                    {
                        hide = func(materialEditorLocal, propertiesLocal);
                        if (hide) break;
                    }
                    if (!hide)
                    {

                        foreach (var action in additionalActions)
                        {
                            action.Invoke(materialEditorLocal, propertiesLocal);
                        }

                        switch (type)
                        {
                            case PropertyType.DefaultProperty when property.type == MaterialProperty.PropType.Texture:
                                {
                                    MaterialProperty extraProperty = null;
                                    MaterialProperty extraProperty2 = null;
                                    if (extraPropertyIndex >= 0) extraProperty = propertiesLocal[extraPropertyIndex];
                                    if (extraProperty2Index >= 0) extraProperty2 = propertiesLocal[extraProperty2Index];

                                    /*if (property.textureValue && firstTime || end)
                                    {
                                        if (!string.IsNullOrEmpty(tooltip)) tooltip += '\n';

                                        tooltip += propertyLocal.textureValue.name;

                                    }
        */
                                    TexturePropertySingleLine(materialEditorLocal, new GUIContent(propertyLocal.displayName, tooltip), propertyLocal, extraProperty, extraProperty2);
                                    if (isLinear) sRGBWarning(propertyLocal);


                                    break;
                                }
                            case PropertyType.DefaultProperty:
                                materialEditorLocal.ShaderProperty(propertyLocal, new GUIContent(propertyLocal.displayName, tooltip));
                                break;
                            case PropertyType.TilingOffset:
                                materialEditorLocal.TextureScaleOffsetProperty(propertiesLocal[extraPropertyIndex]);
                                break;
                            case PropertyType.Foldout:
                                Foldout(propertyLocal);
                                break;
                            case PropertyType.MinMax:
                                {
                                    if (propertyLocal.type == MaterialProperty.PropType.Vector)
                                    {
                                        DrawMinMax(propertyLocal, 0, 1, tooltip);
                                    }

                                    break;
                                }
                            case PropertyType.OverrideAction:
                                {
                                    foreach (var action in overrideAction)
                                    {
                                        action.Invoke(materialEditorLocal, propertiesLocal);
                                    }

                                    break;
                                }
                        }

                        foreach (var action in additionalActionsEnd)
                        {
                            action.Invoke(materialEditorLocal, propertiesLocal);
                        }

                        /*          if (property.textureValue)
                                  {
                                      EditorGUI.indentLevel += 2;
                                      EditorGUILayout.BeginHorizontal();
                                      EditorGUILayout.LabelField(property.textureValue.name);
                                      EditorGUILayout.LabelField(property.textureValue.width.ToString());
                                      EditorGUILayout.EndHorizontal();
                                      EditorGUI.indentLevel -= 2;
                                  }

              */
                    }

                    bool end = EditorGUI.EndChangeCheck();
                    if (end || firstTime)
                    {
                        foreach (var validate in onValidate)
                        {
                            validate.Invoke(materialEditorLocal, propertiesLocal);
                        }
                        firstTime = false;
                    }



                    if (end)
                    {
                        foreach (var validate in onValidateOnce)
                        {
                            validate.Invoke(materialEditorLocal, propertiesLocal);
                        }
                    }
                };
                
                _propertyDrawers.Add(drawer);
            }
        }

        private void OnInitialize(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            InitializePropertyDrawers(materialEditor, properties);
        }
        
        private static void Draw(MaterialEditor materialEditor, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string tooltip = null, string nameOverride = null)
        {
            if (property.type == MaterialProperty.PropType.Texture)
            {
                TexturePropertySingleLine(materialEditor, new GUIContent(property.displayName, tooltip), property, extraProperty, extraProperty2);
            }
            else
            {
                materialEditor.ShaderProperty(property, new GUIContent(property.displayName, tooltip));
            }
        }
        private static Rect GetControlRectForSingleLine()
        {
            return EditorGUILayout.GetControlRect(true, 20f, EditorStyles.layerMaskField);
        }
        private static void ExtraPropertyAfterTexture(MaterialEditor materialEditor, Rect r, MaterialProperty property)
        {
            if ((property.type == MaterialProperty.PropType.Float || property.type == MaterialProperty.PropType.Color) && r.width > EditorGUIUtility.fieldWidth)
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = r.width - EditorGUIUtility.fieldWidth - 2f;

                var labelRect = new Rect(r.x, r.y, r.width - EditorGUIUtility.fieldWidth - 4f, r.height);
                var style = new GUIStyle("label")
                {
                    alignment = TextAnchor.MiddleRight,
                    //richText = true
                };
                EditorGUI.LabelField(labelRect, property.displayName, style);
                materialEditor.ShaderProperty(r, property, " ");
                EditorGUIUtility.labelWidth = labelWidth;
            }
            else
            {
                materialEditor.ShaderProperty(r, property, string.Empty);
            }
        }

        public struct PackingField
        {
            public Texture2D texture;
            public bool isWhite;
            public bool invert;
            public TexturePacking.ChannelSelect channelSelect;
        }

        private static Dictionary<string, bool> _foldoutOpen = new Dictionary<string, bool>();
        private static Dictionary<string, PackingField[]> _packingFields = new Dictionary<string, PackingField[]>();

        private static void TexturePackingField(ref Texture2D texture, ref TexturePacking.ChannelSelect channelSelect, ref bool invert, string name, string invertName = null, bool showOptions = true)
        {
            int previousIntent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
   
            GUILayout.BeginVertical("Box");
            GUILayout.Space(1);

            GUILayout.BeginHorizontal();
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                richText = true,

            };
            texture = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(40), GUILayout.Height(40));
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (invertName != null && invert)
            {
                GUILayout.Label($"<b>{invertName}</b>", style, GUILayout.Width(85));
            }
            else
            {
                GUILayout.Label($"<b>{name}</b>", style, GUILayout.Width(85));
            }
            GUILayout.Label(texture ? texture.name : " ", style);
            GUILayout.EndHorizontal();

            if (showOptions)
            {
                GUILayout.BeginHorizontal();
                channelSelect = (TexturePacking.ChannelSelect)EditorGUILayout.EnumPopup(channelSelect, GUILayout.Width(70));
                GUILayout.Space(20);
                invert = GUILayout.Toggle(invert, "Invert", GUILayout.Width(70));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(1);

            GUILayout.EndVertical();

            EditorGUI.indentLevel = previousIntent;
        }

        public static bool Pack(MaterialProperty setTexture, PackingField red, PackingField green, PackingField blue, PackingField alpha, bool disableSrgb = false)
        {
            var reference = green.texture ?? red.texture ?? alpha.texture ?? blue.texture;
            if (reference == null)
            {
                return true;
            }

            var rChannel = new TexturePacking.Channel()
            {
                Tex = red.texture,
                ID = (int)red.channelSelect,
                Invert = red.invert,
                DefaultWhite = red.isWhite
            };

            var gChannel = new TexturePacking.Channel()
            {
                Tex = green.texture,
                ID = (int)green.channelSelect,
                Invert = green.invert,
                DefaultWhite = green.isWhite
            };

            var bChannel = new TexturePacking.Channel()
            {
                Tex = blue.texture,
                ID = (int)blue.channelSelect,
                Invert = blue.invert,
                DefaultWhite = blue.isWhite
            };

            var aChannel = new TexturePacking.Channel()
            {
                Tex = alpha.texture,
                ID = (int)alpha.channelSelect,
                Invert = alpha.invert,
                DefaultWhite = alpha.isWhite
            };

            var path = AssetDatabase.GetAssetPath(reference);

            var newPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_Packed";

            TexturePacking.Pack(new[] { rChannel, gChannel, bChannel, aChannel }, newPath, reference.width, reference.height);
            var packedTexture = TexturePacking.GetPackedTexture(newPath);
            if (disableSrgb)
            {
                TexturePacking.DisableSrgb(packedTexture);
            }
            setTexture.textureValue = packedTexture;
            return false;
        }

        private static Rect TexturePropertySingleLine(MaterialEditor materialEditor, GUIContent label, MaterialProperty textureProp, MaterialProperty extraProperty1, MaterialProperty extraProperty2)
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

        private static void sRGBWarning(MaterialProperty tex)
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

        private static void DrawMinMax(MaterialProperty min, MaterialProperty max, float minLimit = 0, float maxLimit = 1, string tooltip = null)
        {
            float currentMin = min.floatValue;
            float currentMax = max.floatValue;
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent(min.displayName, tooltip));


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

        private static bool TriangleFoldout(ref bool display, int indent = 0)
        {
            //indent *= 10;
            indent = EditorGUI.indentLevel * 15;
            var lastRect = GUILayoutUtility.GetLastRect();
            var e = Event.current;
            var toggleRect = new Rect(lastRect.x - 15f + indent, lastRect.y + 2f, 12f, 12f);
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

        public static void DrawMinMax(MaterialProperty minMax, float minLimit = 0, float maxLimit = 1, string tooltip = null)
        {
            float currentMin = minMax.vectorValue.x;
            float currentMax = minMax.vectorValue.y;
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent(minMax.displayName, tooltip));


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

        private void Foldout(MaterialProperty foldout)
        {
            bool isOpen = foldout.floatValue == 1;
            DrawSplitter();
            isOpen = DrawHeaderFoldout( new GUIContent (foldout.displayName), isOpen);
            foldout.floatValue = isOpen ? 1 : 0;
            if (isOpen) EditorGUILayout.Space(10);
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

        private static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false)
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
        private static void DrawSplitter(bool isBoxed = false)
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
}
