using System;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace z3y.Editor
{
    public static class TexturePacking
    {
        public class TextureChannel
        {
            public Texture2D Tex;
            public bool Invert;
            public int Channel;
            public bool DefaultWhite = true;
        }

        private static readonly Shader TextureUtilityShader = Shader.Find("Hidden/z3y/TextureUtility");
        private static readonly int Texture0 = Shader.PropertyToID("_Texture0");
        private static readonly int Texture1 = Shader.PropertyToID("_Texture1");
        private static readonly int Texture2 = Shader.PropertyToID("_Texture2");
        private static readonly int Texture3 = Shader.PropertyToID("_Texture3");
        private static readonly int Texture0Channel = Shader.PropertyToID("_Texture0Channel");
        private static readonly int Texture1Channel = Shader.PropertyToID("_Texture1Channel");
        private static readonly int Texture2Channel = Shader.PropertyToID("_Texture2Channel");
        private static readonly int Texture3Channel = Shader.PropertyToID("_Texture3Channel");
        private static readonly int Texture0Invert = Shader.PropertyToID("_Texture0Invert");
        private static readonly int Texture1Invert = Shader.PropertyToID("_Texture1Invert");
        private static readonly int Texture2Invert = Shader.PropertyToID("_Texture2Invert");
        private static readonly int Texture3Invert = Shader.PropertyToID("_Texture3Invert");


        public static void Pack(TextureChannel r, TextureChannel g, TextureChannel b, TextureChannel a, string newTexturePath, int newWidth, int newHeight = 0)
        {
            if (newHeight == 0)
            {
                newHeight = newWidth;
            }
            
            var mat = new Material(TextureUtilityShader);
            
            var rTex = r.DefaultWhite ? Texture2D.whiteTexture : Texture2D.blackTexture;
            var gTex = g.DefaultWhite ? Texture2D.whiteTexture : Texture2D.blackTexture;
            var bTex = b.DefaultWhite ? Texture2D.whiteTexture : Texture2D.blackTexture;
            var aTex = a.DefaultWhite ? Texture2D.whiteTexture : Texture2D.blackTexture;

            if (r.Tex != null) rTex = GetTempUncompressedTexture(r.Tex);
            if (g.Tex != null) gTex = GetTempUncompressedTexture(g.Tex);
            if (b.Tex != null) bTex = GetTempUncompressedTexture(b.Tex);
            if (a.Tex != null) aTex = GetTempUncompressedTexture(a.Tex);
            
            mat.SetTexture(Texture0, rTex);
            mat.SetTexture(Texture1, gTex);
            mat.SetTexture(Texture2, bTex);
            mat.SetTexture(Texture3, aTex);
            
            mat.SetInt(Texture0Channel, r.Channel);
            mat.SetInt(Texture1Channel, g.Channel);
            mat.SetInt(Texture2Channel, b.Channel);
            mat.SetInt(Texture3Channel, a.Channel);
            
            mat.SetInt(Texture0Invert, r.Invert ? 1 : 0);
            mat.SetInt(Texture1Invert, g.Invert ? 1 : 0);
            mat.SetInt(Texture2Invert, b.Invert ? 1 : 0);
            mat.SetInt(Texture3Invert, a.Invert ? 1 : 0);
        
        
            var rt = new RenderTexture(newWidth, newHeight, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                useMipMap = false,
                anisoLevel = 0,
                wrapMode = TextureWrapMode.Clamp,
            };
            rt.Create();
            
            Graphics.Blit(null, rt, mat, 0);
            
            var newTexture = new Texture2D(newWidth, newHeight);
            newTexture.ReadPixels( new Rect( 0, 0, rt.width, rt.height ), 0, 0, true );
            newTexture.Apply();

            var bytes = newTexture.EncodeToPNG();
            
            RenderTexture.active = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(newTexture);
            UnityEngine.Object.DestroyImmediate(rt);
            
            File.WriteAllBytes(newTexturePath + ".png", bytes);
            AssetDatabase.ImportAsset(newTexturePath + ".png");

            ClearTempTextures();
        }

        public static void DisableSrgb(Texture tex)
        {
            var importer = (TextureImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        public static Texture2D GetPackedTexture(string path) => (Texture2D)AssetDatabase.LoadAssetAtPath(path + ".png", typeof(Texture2D));

        private const string TempTextureFolder = "Assets/z3y/TexturePackingUncompressedTemp/";

        private static Texture2D GetTempUncompressedTexture(Texture2D tex)
        {
            var path = AssetDatabase.GetAssetPath(tex);
            var guid = AssetDatabase.AssetPathToGUID(path);
            var fileName = Path.GetFileName(path);

            var tempFolder = Path.Combine(TempTextureFolder, guid);
            Directory.CreateDirectory(tempFolder);
            var tempPath = Path.Combine(tempFolder, fileName);
                    
            if (!File.Exists(tempPath))
            {
                File.Copy(path, tempPath);
            }
            AssetDatabase.ImportAsset(tempPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(tempPath);
        }

        private static void ClearTempTextures()
        {
            if (!Directory.Exists(TempTextureFolder)) return;
            Directory.Delete(TempTextureFolder, true);
            File.Delete(TempTextureFolder.Remove(TempTextureFolder.Length-1) + ".meta");
        }

        public class FixImportSettings : AssetPostprocessor
        {
            private void OnPreprocessTexture()
            {
                var importer = assetImporter as TextureImporter;
                if (importer == null || !importer.assetPath.StartsWith(TempTextureFolder, StringComparison.Ordinal)) return;

                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.anisoLevel = 0;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
            }
        }
        
       
    }
}
