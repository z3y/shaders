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

        public bool drawAll = true;

        private MaterialEditor _materialEditor;
        private MaterialProperty[] _materialProperties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            var material = materialEditor.target as Material;

            if (!_initialized)
            {
                _materialEditor = materialEditor;
                Initialize(materialProperties);
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

        public void Draw(MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string onHover = null, string nameOverride = null)
        {
            if (property is null) return;

            Draw(_materialEditor, property, extraProperty, extraProperty2, onHover, nameOverride);

            if (drawAll)
            {
                int currentIndex = Array.IndexOf(_materialProperties, property);
                currentIndex++;
                if (extraProperty != null) currentIndex++;
                if (extraProperty2 != null) currentIndex++;

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
        public static bool Foldout(MaterialProperty foldout)
        {
            bool isOpen = foldout.floatValue == 1;
            GUIHelpers.DrawSplitter();
            isOpen = GUIHelpers.DrawHeaderFoldout(foldout.displayName, isOpen);
            foldout.floatValue = isOpen ? 1 : 0;
            if (isOpen) EditorGUILayout.Space();
            return isOpen;
        }

        private void Initialize(MaterialProperty[] materialProperties)
        {
            _fieldInfo = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.FieldType == typeof(MaterialProperty)).ToArray();
            _index = new int[_fieldInfo.Length];
            
            for (int i = 0; i < _fieldInfo.Length; i++)
            {
                _index[i] = Array.FindIndex(materialProperties, x => x.name.Equals(_fieldInfo[i].Name, StringComparison.OrdinalIgnoreCase));
            }
            if (drawAll)
            {
                _isDrawn = new bool[materialProperties.Length];

                for (int i = 0; i < materialProperties.Length; i++)
                {
                    _isDrawn[i] = Array.FindIndex(_fieldInfo, x => x.Name.Equals(materialProperties[i].name, StringComparison.OrdinalIgnoreCase)) != -1;
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
    }
}