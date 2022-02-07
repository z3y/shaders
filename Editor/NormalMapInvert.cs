using UnityEngine;
using UnityEditor;
using System.IO;

namespace z3y.Shaders
{
    public static class NormalMapInvert
    {
        [MenuItem("Assets/Invert Normal Map")]
        public static void InvertMenuItem()
        {
            var selection = Selection.activeObject;
            if (selection.GetType() != typeof(Texture2D)) return;
            var texture = (Texture2D)selection;
            InvertNormal(texture);
        }

        private static bool InvertNormal(Texture2D normal)
        {
            var reference = normal;
            if (reference == null) return true;

            var rChannel = new TexturePacking.Channel()
            {
                Tex = normal,
                ID = 0
            };

            var gChannel = new TexturePacking.Channel()
            {
                Tex = normal,
                ID = 1,
                Invert = true
            };

            var bChannel = new TexturePacking.Channel()
            {
                Tex = normal,
                ID = 2
            };

            var aChannel = new TexturePacking.Channel()
            {
                Tex = normal,
                ID = 3
            };

            var path = AssetDatabase.GetAssetPath(reference);
            var newPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_Inverted";

            TexturePacking.Pack(new[] { rChannel, gChannel, bChannel, aChannel }, newPath, reference.width, reference.height);
            var packedTexture = TexturePacking.GetPackedTexture(newPath);
            TexturePacking.CopyImportSettings(normal, packedTexture);
            return false;
        }


        [MenuItem("Assets/Invert Normal Map", true)]
        private static bool InvertMenuItemValidation()
        {
            var isTexture = Selection.activeObject.GetType() == typeof(Texture2D);
            if (!isTexture) return false;

            var tex = (Texture2D)Selection.activeObject;
            if (tex == null) return false;

            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            return importer.textureType == TextureImporterType.NormalMap;
        }


    }
}