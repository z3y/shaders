using UnityEditor;
using UnityEngine;

namespace z3y
{
    // currently disabled because unity 2022 reimports everything 

    /*public class PackingPostProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!FreeImagePackingEditor.settingsNeedApply)
            {
                return;
            }

            var textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null)
            {
                return;
            }

            textureImporter.sRGBTexture = !FreeImagePackingEditor.Linear;

            textureImporter.alphaSource = FreeImagePackingEditor.ChannelA.UnityTexture == null
                ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;

            FreeImagePackingEditor.settingsNeedApply = false;
        }
    }*/
}