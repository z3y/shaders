using System.IO;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class ProjectSettings
    {
        public static Shader lit => Shader.Find("Lit");


        public const string ShaderVersion = "v2.2.1";
        public const string ShaderName = "Lit";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/Lit Shader", SettingsScope.Project)
            {
                label = "Lit Shader",
                guiHandler = (searchContext) =>
                {

                    EditorGUILayout.LabelField("  " + ShaderVersion, EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();
                    //EditorGUILayout.Space();
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
                        SettingsObject.ApplyModifiedProperties();
                        UpdateLitShaderFile.UpdateConfig();

                    }
                },
            };

            return provider;
        }

        const string SettingsPath = "Assets/Settings/LitShaderSettings.asset";

        private static LitShaderSettings _shaderSettings;
        public static LitShaderSettings ShaderSettings
        {
            get
            {   
                if (_shaderSettings is null)
                {
                    _shaderSettings = AssetDatabase.LoadAssetAtPath<LitShaderSettings>(SettingsPath);
                }
                if (_shaderSettings is null)
                {
                    _shaderSettings = CreateDefaultSettingsAsset();
                }
                return _shaderSettings;
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
