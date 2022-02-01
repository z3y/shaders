using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class ShaderConfigWindow : EditorWindow
    {
        [MenuItem("z3y/Shader Config")]
        public static void ShowWindow() => GetWindow<ShaderConfigWindow>("Shader Config");

        bool firstTime = true;

        internal class ShaderConfig
        {
            public static bool VERTEXLIGHT;
            public static bool VERTEXLIGHT_PS = true;
            public static bool NEED_CENTROID_NORMAL;
            public static bool NONLINEAR_LIGHTPROBESH = true;
            public static bool BAKERY_RNM;
            public static bool BAKERY_SH;
            public static bool BICUBIC_LIGHTMAP = true;
            public static bool UNITY_LIGHT_PROBE_PROXY_VOLUME = false;
            public static bool UNITY_SPECCUBE_BLENDING = true;
        }


        FieldInfo[] configFields = typeof(ShaderConfig).GetFields(BindingFlags.Public | BindingFlags.Static);
        private void OnGUI()
        {
            if (firstTime)
            {
                firstTime = false;
                HandleConfigFields((bool value, FieldInfo field) => {
                    field.SetValue(typeof(bool), ShaderConfigData.Load(field.Name, value));
                });
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {

            }
            if (GUILayout.Button("Reset"))
            {
                HandleConfigFields((bool value, FieldInfo field) => {
                    var defaultValue = (bool)field.GetValue(new ShaderConfig());
                    field.SetValue(typeof(bool), defaultValue);
                    Debug.Log(defaultValue);
                    ShaderConfigData.Save(field.Name, defaultValue);
                });
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Global Toggles", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            DrawToggle(ref ShaderConfig.NONLINEAR_LIGHTPROBESH, "Non-Linear Light Probe SH");
            DrawToggle(ref ShaderConfig.VERTEXLIGHT, "Allow Non-Important Lights");
            DrawToggle(ref ShaderConfig.VERTEXLIGHT_PS, "Non-Important Lights per Pixel");
            DrawToggle(ref ShaderConfig.NEED_CENTROID_NORMAL, "Centroid Normal Sampling");
            DrawToggle(ref ShaderConfig.BAKERY_RNM, "Bakery RNM");
            DrawToggle(ref ShaderConfig.BAKERY_SH, "Bakery SH");
            DrawToggle(ref ShaderConfig.BICUBIC_LIGHTMAP, "Bicubic Lightmap");
            DrawToggle(ref ShaderConfig.UNITY_LIGHT_PROBE_PROXY_VOLUME, "Allow Light Probe Proxy Volumes");
            DrawToggle(ref ShaderConfig.UNITY_SPECCUBE_BLENDING, "Allow Reflection Probe Blending");



            if (EditorGUI.EndChangeCheck())
            {
                HandleConfigFields((bool value, FieldInfo field) => {
                    ShaderConfigData.Save(field.Name, value);
                });
            }
        }

        private void HandleConfigFields(Action<bool, FieldInfo> action)
        {
            for (int i = 0; i < configFields.Length; i++)
            {
                var field = configFields[i];
                var value = (bool)field.GetValue(null);
                action.Invoke(value, field);
            }
        }

        private void DrawToggle(ref bool toggle, string display) => toggle = GUILayout.Toggle(toggle, display);
    }
    

    internal static class ShaderConfigData
    {
        const string PrefsPrefix = "z3yGlobalShaderConfig";
        internal static void Save(string keyword, bool toggle)
        {
            EditorPrefs.SetBool(PrefsPrefix + keyword, toggle);
        }

        internal static bool Load(string keyword, bool defaultValue = false)
        {
            return EditorPrefs.GetBool(PrefsPrefix + keyword, defaultValue);
        }
    }
}