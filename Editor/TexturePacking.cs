using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


namespace z3y.Shaders
{
    public static class TexturePacking
    {
        public class Channel
        {
            public Texture2D Tex;
            public bool Invert;
            public int ID;
            public bool DefaultWhite = true;
        }

        private static readonly Shader TextureUtilityShader = Shader.Find("Hidden/z3y/TextureUtility");

        public static void Pack(Channel[] channels, string newTexturePath, int newWidth, int newHeight = 0)
        {
            if (newHeight == 0)
            {
                newHeight = newWidth;
            }
            
            var mat = new Material(TextureUtilityShader);

            var textures = new Texture2D[4];
            for (int i = 0; i < 4; i++)
            {
                if (channels[i].Tex != null)
                {
                    textures[i] = GetTempUncompressedTexture(channels[i].Tex);
                }
                else
                {
                    textures[i] = channels[i].DefaultWhite ? Texture2D.whiteTexture : Texture2D.blackTexture;
                }
                mat.SetTexture($"_Texture{i}", textures[i]);
                mat.SetInt($"_Texture{i}Channel", channels[i].ID);
                mat.SetInt($"_Texture{i}Invert", channels[i].Invert ? 1 : 0);
            }

            var rt = new RenderTexture(newWidth, newHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                useMipMap = false,
                anisoLevel = 0,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();
            
            Graphics.Blit(null, rt, mat, 0);
            
            var newTexture = new Texture2D(newWidth, newHeight, TextureFormat.ARGB32, 0, true);
            newTexture.ReadPixels( new Rect( 0, 0, rt.width, rt.height ), 0, 0, true );
            newTexture.Apply();
            var bytes = newTexture.EncodeToTGA();

            RenderTexture.active = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(newTexture);
            UnityEngine.Object.DestroyImmediate(rt);

            File.WriteAllBytes(newTexturePath + ".tga", bytes);
            AssetDatabase.ImportAsset(newTexturePath + ".tga");

            ClearTempTextures();
        }

        public static void DisableSrgb(Texture tex)
        {
            var importer = (TextureImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        public static Texture2D GetPackedTexture(string path) => (Texture2D)AssetDatabase.LoadAssetAtPath(path + ".tga", typeof(Texture2D));
        
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
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;
            }
        }
    }
}
