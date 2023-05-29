using UnityEditor;
using UnityEngine;

namespace z3y
{
    public class PackingPostProcessor : AssetPostprocessor
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
            Debug.Log("Applied");
        }
    }
}