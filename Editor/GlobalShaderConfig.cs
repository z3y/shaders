using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace z3y.Shaders
{
    public class OnBuildShaderPreprocessor : IPreprocessShaders
    {
        public int callbackOrder => 512;

        private readonly ShaderKeyword _directional;
        private readonly ShaderKeyword _lightmapOn;
        private readonly ShaderKeyword _shadowsScreen;
        private readonly ShaderKeyword _shadowMask;
        private readonly ShaderKeyword _shadowMixing;

        private static bool configLoaded = false;

        public OnBuildShaderPreprocessor()
        {
            _directional = new ShaderKeyword("DIRECTIONAL");
            _lightmapOn = new ShaderKeyword("LIGHTMAP_ON");
            _shadowsScreen = new ShaderKeyword("SHADOWS_SCREEN");
            _shadowMask = new ShaderKeyword("SHADOWS_SHADOWMASK");
            _shadowMixing = new ShaderKeyword("LIGHTMAP_SHADOW_MIXING");
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader.name != ComplexLitSmartGUI.ShaderName)
            {
                return;
            }

            if (!configLoaded)
            {
                ShaderConfigData.LoadAll();
                configLoaded = true;
            }

            for (int i = data.Count - 1; i >= 0; --i)
            {
                if (ShaderConfig.INJECT_DISABLED_DIRECTIONAL)
                {
                    bool directionalEnabled = data[i].shaderKeywordSet.IsEnabled(_directional);
                    bool _shadowsScreenEnabled = data[i].shaderKeywordSet.IsEnabled(_shadowsScreen);
                    bool _shadowMaskEnabled = data[i].shaderKeywordSet.IsEnabled(_shadowMask);
                    bool _shadowMixingEnabled = data[i].shaderKeywordSet.IsEnabled(_shadowMixing);
                    
                    if (directionalEnabled && !_shadowsScreenEnabled && !_shadowMaskEnabled && !_shadowMixingEnabled)
                    {
                        var shaderData = data[i];
                        shaderData.shaderKeywordSet.Disable(_directional);
                        data.Add(shaderData);

                    }
                }
            }
        }
    }
    public class ShaderConfigWindow
    {
        // [MenuItem("z3y/Shader Config")]
        // public static void ShowWindow()
        // {
        //     GetWindow<ShaderConfigWindow>("Shader Config");
        // }

        private static readonly FieldInfo[] ConfigFields = typeof(ShaderConfig).GetFields(BindingFlags.Public | BindingFlags.Static);
        private static readonly ConstructorInfo ShaderConfigConsturctor = typeof(ShaderConfig).GetConstructor(BindingFlags.Static | BindingFlags.NonPublic, null, new Type[0], null);

        private static bool firstTime = true;
        public static void DrawGlobalConfig()
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
            DrawToggle(ref ShaderConfig.BICUBIC_LIGHTMAP, "Bicubic Lightmap");
            DrawToggle(ref ShaderConfig.ACES_TONEMAPPING, "ACES Tone Mapping");

            EditorGUILayout.Space();
            DrawToggle(ref ShaderConfig.BAKERY_NONE, "Bakery Disabled");
            DrawToggle(ref ShaderConfig.BAKERY_RNM, "Bakery RNM");
            DrawToggle(ref ShaderConfig.BAKERY_SH, "Bakery SH");
            DrawToggle(ref ShaderConfig.BAKERY_SHNONLINEAR, "Bakery Lightmap SH Non-Linear ");
            DrawToggle(ref ShaderConfig.NONLINEAR_LIGHTPROBESH, "Non-Linear Light Probe SH");

            EditorGUILayout.Space();
            DrawToggle(ref ShaderConfig.ALLOW_LTCGI, "Allow LTCGI");
            DrawToggle(ref ShaderConfig.UNITY_SPECCUBE_BLENDING, "Allow Reflection Probe Blending");
            DrawToggle(ref ShaderConfig.UNITY_LIGHT_PROBE_PROXY_VOLUME, "Allow Light Probe Proxy Volumes");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Multi Compiles", EditorStyles.boldLabel);
            DrawToggle(ref ShaderConfig.VERTEXLIGHT_ON, "Allow Non-Important Lights");
            DrawToggle(ref ShaderConfig.LOD_FADE_CROSSFADE, "Allow Dithered Lod Cross-Fade");
            DrawToggle(ref ShaderConfig.INJECT_DISABLED_DIRECTIONAL, "Compile Disabled Directional Variants");

            if (EditorGUI.EndChangeCheck())
            {
                ShaderConfigData.SaveAll();
            }
            EditorGUILayout.Space();
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

        private static void DrawToggle(ref bool toggle, string display) => toggle = GUILayout.Toggle(toggle, display);
    }
    internal static class ShaderConfig
    {
        public static bool VERTEXLIGHT_ON = true;
        public static bool VERTEXLIGHT_PS = true;
        public static bool NONLINEAR_LIGHTPROBESH;
        public static bool BAKERY_RNM;
        public static bool BAKERY_SH;
        public static bool BAKERY_NONE;
        public static bool BAKERY_SHNONLINEAR = true;
        public static bool BICUBIC_LIGHTMAP = true;
        public static bool LOD_FADE_CROSSFADE;
        public static bool UNITY_SPECCUBE_BLENDING = true;
        public static bool UNITY_LIGHT_PROBE_PROXY_VOLUME;
        public static bool ACES_TONEMAPPING;
        public static bool ALLOW_LTCGI;
        public static bool INJECT_DISABLED_DIRECTIONAL = false;
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
            sb.AppendLine(ShaderConfig.BAKERY_NONE ? $"{SkipVariant}BAKERY_SH{NewLine}{SkipVariant}BAKERY_RNM" : "");
            sb.AppendLine(ShaderConfig.BAKERY_RNM ? $"{Define}BAKERY_RNM{NewLine}{SkipVariant}BAKERY_RNM" : "");
            sb.AppendLine(ShaderConfig.BAKERY_SH ? $"{Define}BAKERY_SH{NewLine}{SkipVariant}BAKERY_SH" : "");
            sb.AppendLine(ShaderConfig.BAKERY_SHNONLINEAR ? $"{Define}BAKERY_SHNONLINEAR" : "");
            sb.AppendLine(ShaderConfig.BICUBIC_LIGHTMAP ? Define + "BICUBIC_LIGHTMAP" : "");
            sb.AppendLine(ShaderConfig.ACES_TONEMAPPING ? Define + "ACES_TONEMAPPING" : "");
            sb.AppendLine(ShaderConfig.ALLOW_LTCGI ? Define + "ALLOW_LTCGI" : "");
            sb.AppendLine(ShaderConfig.LOD_FADE_CROSSFADE ? "" : SkipVariant + "LOD_FADE_CROSSFADE");
            sb.AppendLine(ShaderConfig.UNITY_SPECCUBE_BLENDING ? "" : Undef + "UNITY_SPECCUBE_BLENDING");
            sb.AppendLine(ShaderConfig.UNITY_LIGHT_PROBE_PROXY_VOLUME ? "" : Define + "UNITY_LIGHT_PROBE_PROXY_VOLUME 0");
            ApplyShaderConfig(sb, ShaderPath);
            AssetDatabase.Refresh();
            Debug.Log("Updated Shader File");
        }

        private static void ApplyShaderConfig(StringBuilder sb, string shaderPath)
        {
            var lines = File.ReadAllLines(shaderPath).ToList();
            var begin = lines.FindIndex(x => x.StartsWith("//ShaderConfigBegin", StringComparison.Ordinal)) + 1;
            var end = lines.FindIndex(x => x.StartsWith("//ShaderConfigEnd", StringComparison.Ordinal)) - 1;
            var count = end - begin;
            if (count > 0) lines.RemoveRange(begin, count);
            lines.Insert(begin, sb.ToString());


            File.WriteAllLines(ShaderPath, lines);
        }
    }
}