using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace z3y
{
    // A script for toggling local keywords globally because of the issues with Unity global keywords limit and VRChat
    public class GlobalKeywords : EditorWindow
    {
        [MenuItem("Window/z3y/Global Keywords")]
        private static void Init()
        {
            var window = (GlobalKeywords)GetWindow(typeof(GlobalKeywords));
            window.Show();
        }

        private static MethodInfo _getShaderGlobalKeywords = typeof(ShaderUtil).GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
        private static MethodInfo _getShaderLocalKeywords = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic);

        private static Shader shader;
        private static string[] keywords;
        private static int keywordIndex;

        private static string[] unityKeywords =
        {
        "SPOT",
        "DIRECTIONAL",
        "DIRECTIONAL_COOKIE",
        "POINT",
        "POINT_COOKIE",
        "SHADOWS_DEPTH",
        "SHADOWS_SCREEN",
        "SHADOWS_CUBE",
        "SHADOWS_SOFT",
        "LIGHTMAP_ON",
        "DIRLIGHTMAP_COMBINED",
        "DYNAMICLIGHTMAP_ON",
        "LIGHTMAP_SHADOW_MIXING",
        "SHADOWS_SHADOWMASK",
        "FOG_LINEAR",
        "FOG_EXP",
        "FOG_EXP2",
        "VERTEXLIGHT_ON",
        "INSTANCING_ON",
        "UNITY_HDR_ON",
        "EDITOR_VISUALIZATION",
        "LIGHTPROBE_SH",
        "LOD_FADE_CROSSFADE"
    };

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            shader = EditorGUILayout.ObjectField(shader, typeof(Shader), true) as Shader;

            if (EditorGUI.EndChangeCheck())
            {

                var globalKeywords = (string[])_getShaderGlobalKeywords.Invoke(null, new object[] { shader });
                var localKeywords = (string[])_getShaderLocalKeywords.Invoke(null, new object[] { shader });

                keywords = globalKeywords.Concat(localKeywords).Where(x => !unityKeywords.Contains(x)).ToArray();
            }



            if (shader is null || keywords.Length < 1)
            {
                //  EditorGUILayout.HelpBox("Select a shader", MessageType.Info);
                return;
            }


            keywordIndex = EditorGUILayout.Popup("Keyword", keywordIndex, keywords);



            if (GUILayout.Button("Enable"))
            {
                ToggleKeyword(keywords[keywordIndex], true);
            }
            if (GUILayout.Button("Disable"))
            {
                ToggleKeyword(keywords[keywordIndex], false);
            }

            EditorGUILayout.HelpBox("Currently not possible to revert back to different per material values after enabling or disabling keywords. Only use with keywords that can be global", MessageType.Warning);
        }

        private static void ToggleKeyword(string keyword, bool enabled)
        {

            var renderers = FindObjectsOfType<Renderer>().ToList();
            var materials = renderers.SelectMany(x => x.sharedMaterials).Distinct().Where(x => x?.shader == shader).ToArray();

            Undo.RecordObjects(materials, "GlobalKeywordToggle");
            for (int i = 0; i < materials.Length && enabled; i++)
            {
                materials[i].EnableKeyword(keyword);
            }

            for (int i = 0; i < materials.Length && !enabled; i++)
            {
                materials[i].DisableKeyword(keyword);
            }



        }
    }
}
