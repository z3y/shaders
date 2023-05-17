using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    internal class LitVariantPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(LitImporter.Ext))
                {
                    continue;
                }

                var mainObject = AssetDatabase.LoadMainAssetAtPath(path);
                if (mainObject is Shader shader)
                {
                    ShaderUtil.RegisterShader(shader);
                    ShaderUtil.ClearShaderMessages(shader);
                }

                DefaultInspector.ReinitializeInspector();
            }
        }
    }
}
