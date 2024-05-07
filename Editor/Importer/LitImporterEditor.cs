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


            var importer = (LitImporter)serializedObject.targetObject;
            var assetPath = AssetDatabase.GetAssetPath(importer);


            if (!assetPath.EndsWith("LitShaderConfig." + LitImporter.Ext))
            {

                var settings = serializedObject.FindProperty("settings");
                var settingsContainer = SettingsEditor.SettingsContainer(settings);
                root.Add(settingsContainer);
                bool isPackage = assetPath.StartsWith("Packages/");
                if (isPackage)
                {
                    root.Add(new HelpBox("Editing shader settings in packages folder, changes will not be saved.", HelpBoxMessageType.Warning));
                }


                var exportButton = new Button
                {
                    text = "Copy Generated Shader"
                };
                exportButton.clicked += ExportShader;
                root.Add(exportButton);

            }


            var revertGui = new IMGUIContainer(RevertGUI);
            root.Add(revertGui);
            return root;
        }

        private void ExportShader()
        {
            var importer = (LitImporter)serializedObject.targetObject;
            var settings = importer.settings;
            var assetPath = AssetDatabase.GetAssetPath(importer);
            var code = LitImporter.RequestGeneratedShader(assetPath);
            GUIUtility.systemCopyBuffer = code;
        }

        private void RevertGUI()
        {
            ApplyRevertGUI();
        }

    }
}
