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
        public static void CopyImportSettings(Texture refTex, Texture toTex)
        {
            var refImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(refTex));
            var toImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(toTex));
            var textureSettings = new TextureImporterSettings();
            refImporter.ReadTextureSettings(textureSettings);
            toImporter.SetTextureSettings(textureSettings);
            toImporter.SaveAndReimport();
        }


        public static Texture2D GetPackedTexture(string path) => (Texture2D)AssetDatabase.LoadAssetAtPath(path + ".tga", typeof(Texture2D));
        
        private const string TempTextureFolder = "Assets/_TexturePackingUncompressedTemp/";
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
            AssetDatabase.Refresh();
        }

        public enum ChannelSelect
        {
            Red,
            Green,
            Blue,
            Alpha
        }

        

        public static void PackButton(Action onPack, Action onReset)
        {
            GUILayout.Space(1);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(1);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pack"))
                {
                    onPack();
                }

                if (GUILayout.Button("Clear"))
                {
                    onReset();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(1);
            }
            GUILayout.Space(1);
        }

        public static void ResetPackingField(ref FieldData d1, ref FieldData d2)
        {
            d1 = new FieldData();
            d2 = new FieldData();
        }

        public static void ResetPackingField(ref FieldData d1, ref FieldData d2, ref FieldData d3, ref FieldData d4)
        {
            d1 = new FieldData();
            d2 = new FieldData();
            d3 = new FieldData();
            d4 = new FieldData();
        }

        public static bool Pack(MaterialProperty setTexture, FieldData albedo, FieldData alpha, bool disableSrgb = false)
        {
            FieldData r = albedo;
            FieldData g = albedo;
            FieldData b = albedo;
            r.channelSelect = ChannelSelect.Red;
            g.channelSelect = ChannelSelect.Green;
            b.channelSelect = ChannelSelect.Blue;

            return Pack(setTexture, r,g,b, alpha, disableSrgb);
        }

        public static bool Pack(MaterialProperty setTexture, FieldData red, FieldData green, FieldData blue, FieldData alpha, bool disableSrgb = false)
        {
            var reference = green.texture ?? red.texture ?? alpha.texture ?? blue.texture;
            if (reference == null)
            {
                return true;
            }

            var rChannel = new Channel()
            {
                Tex = red.texture,
                ID = (int)red.channelSelect,
                Invert = red.invert,
                DefaultWhite = red.isWhite
            };

            var gChannel = new Channel()
            {
                Tex = green.texture,
                ID = (int) green.channelSelect,
                Invert = green.invert,
                DefaultWhite = green.isWhite
            };

            var bChannel = new Channel()
            {
                Tex = blue.texture,
                ID = (int)blue.channelSelect,
                Invert = blue.invert,
                DefaultWhite = blue.isWhite
            };

            var aChannel = new Channel()
            {
                Tex = alpha.texture,
                ID = (int)alpha.channelSelect,
                Invert = alpha.invert,
                DefaultWhite = alpha.isWhite
            };

            var path = AssetDatabase.GetAssetPath(reference);
            var newPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_Packed";

            Pack(new[] { rChannel, gChannel, bChannel, aChannel }, newPath, reference.width, reference.height);
            var packedTexture = GetPackedTexture(newPath);
            if (disableSrgb)
            {
                DisableSrgb(packedTexture);
            }
            setTexture.textureValue = packedTexture;
            return false;
        }

        public static void TexturePackingField(ref FieldData data, string name, string invertName = null, bool showOptions = true)
        {
            TexturePackingField(ref data.texture, ref data.channelSelect, ref data.invert, name, invertName, showOptions);
        }

        private static void TexturePackingField(ref Texture2D texture, ref ChannelSelect channelSelect, ref bool invert, string name, string invertName = null, bool showOptions = true)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(1);

                GUILayout.BeginHorizontal();
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    richText = true,
                    
                };
                texture = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(40), GUILayout.Height(40));
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                if (invertName != null && invert)
                {
                    GUILayout.Label($"<b>{invertName}</b>", style, GUILayout.Width(85));
                }
                else
                {
                    GUILayout.Label($"<b>{name}</b>", style, GUILayout.Width(85));
                }
                GUILayout.Label(texture ? texture.name :  " ", style);
                GUILayout.EndHorizontal();

                if (showOptions)
                {
                    GUILayout.BeginHorizontal();
                    channelSelect = (ChannelSelect)EditorGUILayout.EnumPopup(channelSelect, GUILayout.Width(70));
                    GUILayout.Space(20);
                    invert = GUILayout.Toggle(invert, "Invert", GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.Space(1);
            }

        }

        public struct FieldData
        {
            public Texture2D texture;
            public bool invert;
            public bool isWhite;
            public ChannelSelect channelSelect;
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
