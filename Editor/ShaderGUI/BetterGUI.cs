using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public static class BetterGUIExtensions
    {
        public static void Draw(this MaterialEditor materialEditor, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string nameOverride = null, string onHover = null)
        {

            if (property is null) return;

            if (property.type == MaterialProperty.PropType.Texture)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(nameOverride ?? property.displayName, onHover), property, extraProperty, extraProperty2);
            }
            else
            {
                materialEditor.ShaderProperty(property, new GUIContent(nameOverride ?? property.displayName, onHover));
            }

        }
    }


    public class BetterGUI : ShaderGUI
    {
        private bool _initialized = false;
        private FieldInfo[] _fieldInfo;
        private int[] _index;
        private bool[] _isDrawn;

        public bool drawAll = true;



        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            var material = materialEditor.target as Material;

            if (!_initialized)
            {
                Initialize(materialProperties);
                OnValidate(material);
                _initialized = true;
            }
            UpdateProperties(materialProperties);


            EditorGUI.BeginChangeCheck();
            OnGUIProperties(materialEditor, materialProperties, material);
            if (EditorGUI.EndChangeCheck())
            {
                OnValidate(material);
            };
        }

        private void Initialize(MaterialProperty[] materialProperties)
        {
            _fieldInfo = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.FieldType == typeof(MaterialProperty)).ToArray();
            _index = new int[_fieldInfo.Length];
            
            for (int i = 0; i < _fieldInfo.Length; i++)
            {
                _index[i] = Array.FindIndex(materialProperties, x => x.name == _fieldInfo[i].Name);
            }
            if (drawAll)
            {
                _isDrawn = new bool[materialProperties.Length];

                for (int i = 0; i < materialProperties.Length; i++)
                {
                    _isDrawn[i] = Array.FindIndex(_fieldInfo, x => x.Name == materialProperties[i].name) != -1;
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