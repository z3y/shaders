using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    
    public class ShaderConfigWindow : EditorWindow
    {
        [MenuItem("z3y/Shader Config")]
        public static void ShowWindow()
        {
            GetWindow<ShaderConfigWindow>("Shader Config");
        }

        private static readonly FieldInfo[] ConfigFields = typeof(ShaderConfig).GetFields(BindingFlags.Public | BindingFlags.Static);
        private static readonly ConstructorInfo ShaderConfigConsturctor = typeof(ShaderConfig).GetConstructor(BindingFlags.Static | BindingFlags.NonPublic, null, new Type[0], null);

        static bool firstTime = true;
        private void OnGUI()
        {
            if (firstTime)
            {
                firstTime = false;
                ShaderConfigData.LoadAll();
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                ShaderConfigData.Generate();
            }
            if (GUILayout.Button("Reset"))
            {
                HandleConfigFields((bool value, FieldInfo field) =>
                {
                    field.SetValue(null, false);
                });
                ShaderConfigConsturctor.Invoke(null, null);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Global Toggles", EditorStyles.boldLabel);
            DrawToggle(ref ShaderConfig.VERTEXLIGHT_PS, "Non-Important Lights per Pixel");
            DrawToggle(ref ShaderConfig.NEED_CENTROID_NORMAL, "Centroid Normal Sampling");
            DrawToggle(ref ShaderConfig.BICUBIC_LIGHTMAP, "Bicubic Lightmap");

            EditorGUILayout.Space();
            DrawToggle(ref ShaderConfig.BAKERY_RNM, "Bakery RNM");
            DrawToggle(ref ShaderConfig.BAKERY_SH, "Bakery SH");
            DrawToggle(ref ShaderConfig.BAKERY_SHNONLINEAR, "Bakery Lightmap SH Non-Linear ");
            DrawToggle(ref ShaderConfig.NONLINEAR_LIGHTPROBESH, "Non-Linear Light Probe SH");

            EditorGUILayout.Space();
            DrawToggle(ref ShaderConfig.UNITY_SPECCUBE_BLENDING, "Allow Reflection Probe Blending");
            DrawToggle(ref ShaderConfig.UNITY_LIGHT_PROBE_PROXY_VOLUME, "Allow Light Probe Proxy Volumes");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Multi Compiles", EditorStyles.boldLabel);
            DrawToggle(ref ShaderConfig.VERTEXLIGHT_ON, "Allow Non-Important Lights");
            DrawToggle(ref ShaderConfig.LOD_FADE_CROSSFADE, "Allow Dithered Lod Cross-Fade");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Changing settings above will cause the shader to recompile at build", MessageType.Info);



            if (EditorGUI.EndChangeCheck())
            {
                ShaderConfigData.SaveAll();
            }
        }

        internal static void HandleConfigFields(Action<bool, FieldInfo> action)
        {
            for (int i = 0; i < ConfigFields.Length; i++)
            {
                var field = ConfigFields[i];
                var value = (bool)field.GetValue(null);
                action.Invoke(value, field);
            }
        }

        private void DrawToggle(ref bool toggle, string display) => toggle = GUILayout.Toggle(toggle, display);
    }
    internal static class ShaderConfig
    {
        public static bool VERTEXLIGHT_ON = true;
        public static bool VERTEXLIGHT_PS = true;
        public static bool NEED_CENTROID_NORMAL;
        public static bool NONLINEAR_LIGHTPROBESH;
        public static bool BAKERY_RNM;
        public static bool BAKERY_SH;
        public static bool BAKERY_SHNONLINEAR = true;
        public static bool BICUBIC_LIGHTMAP = true;
        public static bool LOD_FADE_CROSSFADE;
        public static bool UNITY_SPECCUBE_BLENDING = true;
        public static bool UNITY_LIGHT_PROBE_PROXY_VOLUME;
    }

    internal static class ShaderConfigData
    {
        private static readonly string ConfigPath = Path.Combine(Application.dataPath, "../") + "ProjectSettings/z3yGlobalShaderConfig.txt";
        internal static void SaveAll()
        {
            var sb = new StringBuilder();
            ShaderConfigWindow.HandleConfigFields((bool value, FieldInfo field) => {
                sb.AppendLine(field.Name + " " + (value ? 'T' : 'F'));
            });
            File.WriteAllText(ConfigPath, sb.ToString());
        }
        internal static void LoadAll()
        {
            if (!File.Exists(ConfigPath))
            {
                SaveAll();
            }
            var config = File.ReadAllLines(ConfigPath);
            ShaderConfigWindow.HandleConfigFields((bool value, FieldInfo field) => {
                foreach(var line in config)
                {
                    if (line.StartsWith(field.Name, StringComparison.Ordinal))
                    {
                        field.SetValue(null, line[line.Length-1] == 'T');
                        break;
                    }
                }
            });
        }

        static readonly string ShaderPath = AssetDatabase.GetAssetPath(Shader.Find(ComplexLitSmartGUI.ShaderName));
        static readonly string ShaderPath2 = AssetDatabase.GetAssetPath(Shader.Find(SimpleLitSmartGUI.ShaderName));
        private static readonly string NewLine = Environment.NewLine;
        private const string SkipVariant = "#pragma skip_variants ";
        private const string Define = "#define ";
        private const string Undef = "#undef ";

        internal static void Generate()
        {
            var sb = new StringBuilder().AppendLine();

    
            sb.AppendLine(ShaderConfig.NONLINEAR_LIGHTPROBESH ? $"{Define}NONLINEAR_LIGHTPROBESH{NewLine}{SkipVariant}NONLINEAR_LIGHTPROBESH" : "");
            sb.AppendLine(ShaderConfig.VERTEXLIGHT_ON ? "" : SkipVariant + "VERTEXLIGHT_ON");
            sb.AppendLine(ShaderConfig.VERTEXLIGHT_PS ? Define + "VERTEXLIGHT_PS" : "");
            sb.AppendLine(ShaderConfig.NEED_CENTROID_NORMAL ? Define + "NEED_CENTROID_NORMAL" : "");
            sb.AppendLine(ShaderConfig.BAKERY_RNM ? $"{Define}BAKERY_RNM{NewLine}{SkipVariant}BAKERY_RNM" : "");
            sb.AppendLine(ShaderConfig.BAKERY_SH ? $"{Define}BAKERY_SH{NewLine}{SkipVariant}BAKERY_SH" : "");
            sb.AppendLine(ShaderConfig.BAKERY_SHNONLINEAR ? $"{Define}BAKERY_SHNONLINEAR" : "");
            sb.AppendLine(ShaderConfig.BICUBIC_LIGHTMAP ? Define + "BICUBIC_LIGHTMAP" : "");
            sb.AppendLine(ShaderConfig.LOD_FADE_CROSSFADE ? "" : SkipVariant + "LOD_FADE_CROSSFADE");
            sb.AppendLine(ShaderConfig.UNITY_SPECCUBE_BLENDING ? "" : Undef + "UNITY_SPECCUBE_BLENDING");
            sb.AppendLine(ShaderConfig.UNITY_LIGHT_PROBE_PROXY_VOLUME ? "" : Define + "UNITY_LIGHT_PROBE_PROXY_VOLUME 0");

            var lines = File.ReadAllLines(ShaderPath).ToList();
            var begin = lines.FindIndex(x => x.StartsWith("//ShaderConfigBegin", StringComparison.Ordinal)) + 1;
            var end = lines.FindIndex(x => x.StartsWith("//ShaderConfigEnd", StringComparison.Ordinal)) - 1;
            var count = end - begin;
            if (count > 0) lines.RemoveRange(begin, count);
            lines.Insert(begin, sb.ToString());


            File.WriteAllLines(ShaderPath, lines);
            AssetDatabase.Refresh();
            Debug.Log("Updated Shader File");
        }
    }
}