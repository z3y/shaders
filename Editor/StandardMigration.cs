using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static z3y.FreeImagePacking;
namespace z3y
{
    public static class StandardMigration
    {
        [MenuItem("Tools/Lit/Migrate Material Selection")]
        public static void MigrateSelection()
        {
            var objs = Selection.GetFiltered(typeof(Material), SelectionMode.Assets);
            var len = objs.Length;
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i] as Material;
                if (obj == null)
                {
                    continue;
                }
                EditorUtility.DisplayProgressBar("Migrating", "Doing some work...", i / len);
                Migrate(obj);
            }

            EditorUtility.ClearProgressBar();
        }

        public static void Migrate(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.shader.name != "Standard")
            {
                return;
            }

            Undo.RecordObject(material, "Migrating Material");


            var smoothness = material.GetFloat("_Glossiness");
            var metallic = material.GetFloat("_Metallic");
            var metallicGlossMap = material.GetTexture("_MetallicGlossMap");
            var detailAlbedo = material.GetTexture("_DetailAlbedoMap");
            var detailTiling = material.GetVector("_DetailAlbedoMap_ST");
            var detailNormalScale = material.GetFloat("_DetailNormalMapScale");
            var detailNormal = material.GetTexture("_DetailNormalMap");
            var detailuv = material.GetFloat("_UVSec");
            var occlusionMap = material.GetTexture("_OcclusionMap");

            var emission = material.GetColor("_EmissionColor");

            var emissionEnabled = material.IsKeywordEnabled("_EMISSION");

            material.shader = Shader.Find("Lit");


            material.SetFloat("_Roughness", 1.0f - smoothness);

            if (metallicGlossMap)
            {
                material.SetFloat("_Metallic", 1.0f);
            }
            else
            {
                material.SetFloat("_Metallic", Mathf.Pow(metallic, 2.2f));
            }
            material.SetColor("_EmissionColor", new Color(Mathf.Pow(emission.r, 2.2f), Mathf.Pow(emission.g, 2.2f), Mathf.Pow(emission.b, 2.2f)));

            if (emissionEnabled)
            {
                material.SetFloat("_EmissionToggle", 1.0f);
                material.EnableKeyword("_EMISSION");
            }

            material.SetTexture("_DetailAlbedo", detailAlbedo);
            material.SetTexture("_DetailBumpMap", detailNormal);
            material.SetFloat("_DetailBumpScale", detailNormalScale);
            material.SetFloat("_Detail_UV", detailuv);
            material.SetVector("_DetailAlbedo_ST", detailTiling);
            if (detailNormal)
            {
                material.EnableKeyword("_DETAIL_NORMAL");
            }
            if (detailAlbedo)
            {
                material.EnableKeyword("_DETAIL_ALBEDO");
            }

            bool needsPacking = occlusionMap || metallicGlossMap;
            if (needsPacking)
            {
                var refTex = metallicGlossMap == null ? occlusionMap : metallicGlossMap;


                var path = AssetDatabase.GetAssetPath(refTex);
                var fullPath = Path.GetFullPath(path);

                var absolutePath = FreeImagePackingEditor.GetPackedTexturePath(fullPath);
                var unityPath = FreeImagePackingEditor.GetPackedTexturePath(path);


                // occlusion
                var r = new TextureChannel();
                r.DefaultColor = DefaultColor.White;
                r.Source = ChannelSource.Green;
                if (occlusionMap)
                {
                    r.Path = AssetDatabase.GetAssetPath(occlusionMap);
                }

                // roughness
                var g = new TextureChannel();
                g.DefaultColor = DefaultColor.White;
                g.Source = ChannelSource.Alpha;

                if (metallicGlossMap)
                {
                    var hasAlpha = GraphicsFormatUtility.HasAlphaChannel(metallicGlossMap.graphicsFormat);
                    if (hasAlpha)
                    {
                        Debug.Log("true");
                        g.Invert = true;
                        g.Path = AssetDatabase.GetAssetPath(metallicGlossMap);
                    }
                }

                // metallic
                var b = new TextureChannel();
                b.DefaultColor = DefaultColor.White;
                b.Source = ChannelSource.Red;
                if (metallicGlossMap)
                {
                    b.Path = AssetDatabase.GetAssetPath(metallicGlossMap);
                }

                var a = new TextureChannel();


                PackCustom(absolutePath, r,g,b,a, (refTex.width, refTex.height), PackingFormat);
                AssetDatabase.ImportAsset(unityPath);

                // metallic gloss map alpha is always linear, but red and occlusion map can differ, if its not setup correctly
                // this assumes it should be linear so srgb is turned off
                var importer = AssetImporter.GetAtPath(unityPath);
                (importer as TextureImporter).sRGBTexture = false;
                importer.SaveAndReimport();

                var maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>(unityPath);

                material.SetTexture("_MaskMap", maskMap);
                material.EnableKeyword("_MASKMAP");

            }

        }

    }
}