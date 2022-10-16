using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class ProjectSettings
    {
        public const string shaderName = "Lit";
        internal static Shader lit = Shader.Find(shaderName);

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/Lit Shader", SettingsScope.Project)
            {
                label = "Lit Shader",
                guiHandler = (searchContext) =>
                {


                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.defaultShader)), new GUIContent("Default Shader", "Use this shader on materials as default instead of Standard. Only affects the Model Importer materials"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Lightmap", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.bakeryMode)), new GUIContent("Bakery Mode"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.nonLinearLightmapSH)), new GUIContent("Non Linear Lightmap SH"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.bicubicLightmap)), new GUIContent("Bicubic Lightmap"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Lightprobes", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.allowLPPV)), new GUIContent("Allow Lightprobe Proxy Volumes"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.nonLinearLightprobeSH)), new GUIContent("Non Linear Lightprobe SH"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.dithering)), new GUIContent("Dithering"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.aces)), new GUIContent("ACES"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Compile Variants", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.compileVariantsWithoutDirectionalLight)), new GUIContent("Directional Light Off"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.compileVertexLights)), new GUIContent("Vertex Lights"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.compileLODCrossfade)), new GUIContent("LOD Crossfade"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Third Party", EditorStyles.boldLabel);

                    EditorGUI.BeginDisabledGroup(!ltcgiIncluded);
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(litShaderSettings.allowLTCGI)), new GUIContent("Allow LTCGI"));
                    EditorGUI.EndDisabledGroup();

                    if (EditorGUI.EndChangeCheck())
                    {
                        settingsObject.ApplyModifiedProperties();
                        UpdateLitShaderFile.UpdateConfig();

                    }
                },
            };

            return provider;
        }

#if LTCGI_INCLUDED
        const bool ltcgiIncluded = true;
#else
        const bool ltcgiIncluded = false;
#endif

        const string SettingsPath = "Assets/Settings/LitShaderSettings.asset";
        private static LitShaderSettings _litShaderSettings;
        public static LitShaderSettings litShaderSettings
        {
            get
            {   
                if (_litShaderSettings is null)
                {
                    _litShaderSettings = AssetDatabase.LoadAssetAtPath<LitShaderSettings>(SettingsPath);
                }
                if (_litShaderSettings is null)
                {
                    _litShaderSettings = CreateDefaultSettingsAsset();
                }
                return _litShaderSettings;
            }

        }

        private static SerializedObject _settingsObject;
        internal static SerializedObject settingsObject
        {
            get
            {
                if (_settingsObject is null)
                {
                    _settingsObject = new SerializedObject(litShaderSettings);
                }
                return _settingsObject;
            }
        }


        private static LitShaderSettings CreateDefaultSettingsAsset()
        {
            var settingsAsset = ScriptableObject.CreateInstance<LitShaderSettings>();
            AssetDatabase.CreateAsset(settingsAsset, SettingsPath);
            AssetDatabase.Refresh();

            return settingsAsset;
        }
    }
}
