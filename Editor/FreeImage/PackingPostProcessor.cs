using System;
using UnityEditor;
using UnityEngine;

namespace z3y
{
    public class PackingPostProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var path = importer.assetPath;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith("_linear"))
            {
                importer.sRGBTexture = false;
            }
        }
    }
}