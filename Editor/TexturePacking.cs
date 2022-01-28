using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class TexturePacking : Editor
    {
        [System.Serializable]
        public class ChannelTexture
        {
            public string name;
            public Texture2D texture;
            public bool invert;
            public ColorMode mode = ColorMode.Red;
            public enum ColorMode
            {
                Red,
                Green,
                Blue,
                Alpha
            }

            public void SetMode(int i, bool ignoreSave = false)
            {
                switch (i)
                {
                    case 0:
                        mode = ColorMode.Red;
                        break;
                    case 1:
                        mode = ColorMode.Green;
                        break;
                    case 2:
                        mode = ColorMode.Blue;
                        break;
                    case 3:
                        mode = ColorMode.Alpha;
                        break;
                }
                if (!ignoreSave)
                {
                    EditorPrefs.SetInt("TextureUtilityChannel" + name, i);
                }
            }

            public ChannelTexture(string n, int mode)
            {
                name = n;
                SetMode(mode, true);
            }

            public static string PackTexture(ChannelTexture[] channels, string destination,int width, int height, TexEncoding encodingType, bool refresh=true,bool overwrite=true)
            {
                int firstIndex = -1;
                for (int i = 3; i >= 0; i--)
                {
                    if (channels[i].texture)
                        firstIndex = i;
                }
                if (firstIndex < 0)
                    return string.Empty;

                ChannelTexture firstChannel = channels[firstIndex];

                
                Texture2D newTexture = new Texture2D(width, height);
                channels[0].GetChannelColors(width, height, out float[] reds, true);
                channels[1].GetChannelColors(width, height, out float[] greens, true);
                channels[2].GetChannelColors(width, height, out float[] blues, true);
                channels[3].GetChannelColors(width, height, out float[] alphas, true);
                Color[] finalColors = new Color[width*height];

                for (int i=0;i< finalColors.Length;i++)
                {
                    finalColors[i].r = (reds!=null) ? reds[i] : 0;
                    finalColors[i].g = (greens != null) ? greens[i] : 1;
                    finalColors[i].b = (blues != null) ? blues[i] : 1;
                    finalColors[i].a = (alphas != null) ? alphas[i] : 1;
                }
                newTexture.SetPixels(finalColors);
                newTexture.Apply();

                GetEncoding(newTexture, encodingType, out byte[] data, out string ext);

                string newTexturePath = destination+"_Packed"+ext;
                if (!overwrite)
                    newTexturePath = AssetDatabase.GenerateUniqueAssetPath(newTexturePath);
                SaveTexture(data, newTexturePath);
                DestroyImmediate(newTexture);
                if (refresh)
                    AssetDatabase.Refresh();

                ClearTempTextures();
                
                return newTexturePath;
            }

            private static void SaveTexture(byte[] textureEncoding, string path)
            {
                System.IO.FileStream stream = System.IO.File.Create(path);
                stream.Write(textureEncoding, 0, textureEncoding.Length);
                stream.Dispose();
            }

            private static string GetDestinationFolder(string path)
            {
                return path.Substring(0, path.LastIndexOf('/'));
            }

            // private static TexEncoding encoding = TexEncoding.SaveAsPNG;
            public enum TexEncoding
            {
                SaveAsPNG,
                SaveAsJPG,
                SaveAsTGA
            }
            private static void GetEncoding(Texture2D texture, TexEncoding encodingType, out byte[] data, out string ext)
            {
                switch ((int)encodingType)
                {
                    default:
                        ext = ".png";
                        data = texture.EncodeToPNG();
                        break;
                    case 1:
                        ext = ".jpg";
                        data = texture.EncodeToJPG(75);
                        break;
                    case 2:
                        ext = ".tga";
                        data = texture.EncodeToTGA();
                        break;
                }
            }

            
            public Texture2D GetChannelColors(int width, int height, out float[] colors, bool unloadTempTexture)
            {
                if (!texture)
                {
                    colors = null;
                    return null;
                }
                else
                {
                    texture = GetTempUncompressedTexture(texture);
                    Texture2D newTexture = GetColors(texture, width, height, out Color[] myColors, unloadTempTexture);
                    colors = myColors.Select(c =>
                    {
                        if (mode == ColorMode.Red)
                            return c.r;
                        if (mode == ColorMode.Green)
                            return c.g;
                        if (mode == ColorMode.Blue)
                            return c.b;

                        return c.a;
                    }).ToArray();
                    if (invert)
                    {
                        for (int i = 0; i < colors.Length; i++)
                        {
                            colors[i] = 1 - colors[i];
                        }
                    }
                    return newTexture;
                }
            }

            public static Texture2D GetColors(Texture2D texture, int width, int height, out Color[] Colors,bool unloadTempTexture = false)
            {
                //Thanks to
                //https://gamedev.stackexchange.com/questions/92285/unity3d-resize-texture-without-corruption
                texture.filterMode = FilterMode.Point;
                RenderTexture rt = RenderTexture.GetTemporary(width, height);
                
                rt.filterMode = FilterMode.Point;
                RenderTexture.active = rt;
                Graphics.Blit(texture, rt);
                Texture2D newTexture = new Texture2D(width, height);
                newTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                Color[] myColors = newTexture.GetPixels();
                RenderTexture.active = null;
                /////////////////////
                Colors = myColors;
                if (unloadTempTexture)
                {
                    DestroyImmediate(newTexture);
                    return null;
                }
                return newTexture;
            }

            private const string TempTextureFolder = "Assets/z3y/TexturePackingUncompressedTemp/";
            public static Texture2D GetTempUncompressedTexture(Texture2D tex)
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

            public static void ClearTempTextures()
            {
                if (Directory.Exists(TempTextureFolder))
                {
                    Directory.Delete(TempTextureFolder, true);
                }
            }

            public class FixImportSettings : AssetPostprocessor
            {
                private void OnPreprocessTexture()
                {
                    var importer = assetImporter as TextureImporter;
                    if (importer == null || !importer.assetPath.StartsWith(TempTextureFolder, StringComparison.Ordinal)) return;

                    importer.filterMode = FilterMode.Point;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.anisoLevel = 0;
                    importer.sRGBTexture = true;
                    importer.mipmapEnabled = false;
                }
            }
        }
    }

}

// parts for packing textures taken from Dreadrith
// https://github.com/Dreadrith/DreadScripts/tree/main/Texture%20Utility
// MIT License

// Copyright (c) 2020 Dreadrith

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
