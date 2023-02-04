using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_3_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine.UIElements;

namespace z3y.Shaders
{
    [CustomEditor(typeof(LitImporter))]
    internal class LitImporterEditor : ScriptedImporterEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.style.paddingTop = 10;


            var settings = serializedObject.FindProperty("settings");
            var settingsContainer = SettingsEditor.SettingsContainer(settings);
            root.Add(settingsContainer);


            var exportButton = new Button
            {
                text = "Copy Generated Shader"
            };
            exportButton.clicked += ExportShader;
            root.Add(exportButton);

            var revertGui = new IMGUIContainer(RevertGUI);
            root.Add(revertGui);
            return root;
        }

        public void ExportShader()
        {
            var importer = (LitImporter)serializedObject.targetObject;
            var settings = importer.settings;
            var assetPath = AssetDatabase.GetAssetPath(importer);
            var code = LitImporter.ProcessFileLines(settings, assetPath, EditorUserBuildSettings.activeBuildTarget);
            GUIUtility.systemCopyBuffer = code;
        }

        public void RevertGUI()
        {
            ApplyRevertGUI();
        }

    }
}
