using System;
using System.IO;
using UnityEditor;
using UnityEditor.WindowsStandalone;
using UnityEngine;

namespace z3y.Shaders
{
    public class ProjectSettings
    {
        public const string ShaderName = "Lit Variants/Legacy/Lit v2";

        public static Shader lit => Shader.Find(ShaderName);

        // remove this in new projects to avoid confusion without breaking older projects
        public static bool SettingsDisabled => !File.Exists(ReferenceSettingsPath);

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            if (SettingsDisabled)
            {
                return null;
            }
            var provider = new SettingsProvider("Project/Lit Shader", SettingsScope.Project)
            {
                label = "Lit Shader",
                guiHandler = (searchContext) =>
                {

                    EditorGUI.BeginChangeCheck();
                    _shaderSettingsEditorWindow = (LitShaderSettings)EditorGUILayout.ObjectField(_shaderSettingsEditorWindow, typeof(LitShaderSettings), false);
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.defaultShader)), new GUIContent("MaterialDescription Shader", "Use this shader on models with MaterialDescription creation mode for improved Blender workflow"));
                    //EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.defaultPreset)), new GUIContent("Default Preset"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Lightmap", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.bakeryMode)), new GUIContent("Bakery Mode"));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.nonLinearLightmapSH)), new GUIContent("Non Linear Lightmap SH"));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.bicubicLightmap)), new GUIContent("Bicubic Lightmap"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Lightprobes", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.allowLPPV)), new GUIContent("Allow Lightprobe Proxy Volumes"));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.nonLinearLightprobeSH)), new GUIContent("Non Linear Lightprobe SH"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Features", EditorStyles.boldLabel);
                    //EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.fixBlackLevel)), new GUIContent("Black Level Fix"));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.dithering)), new GUIContent("Dithering"));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.aces)), new GUIContent("ACES"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Compile Variants", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.directionalLightVariants)), new GUIContent("Directional Light",
                        "Only Enabled: Default Unity behaviour. Every variant in the ForwardBase pass is calculating directional lights even if it doesnt exist.\n" +
                        "Only Disabled: Completely disable directional light, mostly for improving performance on Quest.\n" +
                        "Compile Both: Compile both Disabled and Enabled variants. This almost doubles the compiled variants."));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.compileVertexLights)), new GUIContent("Vertex Lights"));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.compileLODCrossfade)), new GUIContent("LOD Crossfade"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Quest", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.q_DisableForwardAdd)), new GUIContent("Disable AddPass"));
                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.q_DisableShadowCaster)), new GUIContent("Disable ShadowCaster"));


                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Third Party", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(SettingsObject.FindProperty(nameof(LitShaderSettings.allowLTCGI)), new GUIContent("Allow LTCGI"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveSettingsReference();
                        SettingsObject.ApplyModifiedProperties();
                        UpdateLitShaderFile.UpdateConfig();
                    }
                },
            };
            
            return provider;
        }

        const string SettingsPath = "Assets/Settings/LitShaderSettings.asset";

        private static string ReferenceSettingsPath = Environment.CurrentDirectory + @"\ProjectSettings\LitShaderSettings.txt";

        private static string _lastGUID = string.Empty;

        private static void SaveSettingsReference()
        {
            ShaderSettings = _shaderSettingsEditorWindow;
            var path = AssetDatabase.GetAssetPath(ShaderSettings);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (!_lastGUID.Equals(guid))
            {
                File.WriteAllText(ReferenceSettingsPath, guid);
                _settingsObject = null;
            }
            _lastGUID = guid;
        }

        private static string LoadSettingsReference()
        {
            if (File.Exists(ReferenceSettingsPath))
            {
                return File.ReadAllText(ReferenceSettingsPath);
            }
            return null;
        }

        private static LitShaderSettings _shaderSettingsEditorWindow;

        private static LitShaderSettings _shaderSettings;
        public static LitShaderSettings ShaderSettings
        {
            get
            {
                if (_shaderSettings is null)
                {
                    var reference = LoadSettingsReference();
                    if (!string.IsNullOrEmpty(reference))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(LoadSettingsReference());
                        if (!string.IsNullOrEmpty(path)) _shaderSettings = AssetDatabase.LoadAssetAtPath<LitShaderSettings>(path);
                    }
                }
                if (_shaderSettings is null)
                {
                    _shaderSettings = AssetDatabase.LoadAssetAtPath<LitShaderSettings>(SettingsPath);
                }
                if (_shaderSettings is null)
                {
                    _shaderSettings = CreateDefaultSettingsAsset();
                    SaveSettingsReference();
                }
                _shaderSettingsEditorWindow = _shaderSettings;
                return _shaderSettings;
            }
            set
            {
                _shaderSettings = value;
            }

        }

        private static SerializedObject _settingsObject;
        internal static SerializedObject SettingsObject
        {
            get
            {
                if (_settingsObject is null)
                {
                    _settingsObject = new SerializedObject(ShaderSettings);
                }
                return _settingsObject;
            }
        }


        private static LitShaderSettings CreateDefaultSettingsAsset()
        {
            var settingsAsset = ScriptableObject.CreateInstance<LitShaderSettings>();
            if (!Directory.Exists("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            AssetDatabase.CreateAsset(settingsAsset, SettingsPath);
            AssetDatabase.Refresh();

            return settingsAsset;
        }
    }
}
