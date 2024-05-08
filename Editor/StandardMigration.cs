using UnityEditor;
using UnityEngine;

namespace z3y
{
    public static class StandardMigration
    {
        [MenuItem("Tools/Lit/Migrate Material Selection")]
        public static void MigrateSelection()
        {
            foreach (Material obj in Selection.GetFiltered(typeof(Material), SelectionMode.Assets))
            {
                Migrate(obj);
            }
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
            Debug.Log($"Migrating: {material.name}");

            var smoothness = material.GetFloat("_Glossiness");
            var metallic = material.GetFloat("_Metallic");
            var metallicGlossMap = material.GetTexture("_MetallicGlossMap");
            var detailAlbedo = material.GetTexture("_DetailAlbedoMap");
            var detailTiling = material.GetVector("_DetailAlbedoMap_ST");
            var detailNormalScale = material.GetFloat("_DetailNormalMapScale");
            var detailNormal = material.GetTexture("_DetailNormalMap");
            var detailuv = material.GetFloat("_UVSec");

            var emission = material.GetColor("_EmissionColor");

            var emissionEnabled = material.IsKeywordEnabled("_EMISSION");

            material.shader = Shader.Find("Lit");

            if (metallicGlossMap)
            {
                material.SetFloat("_Roughness", 1.0f);
            }
            else
            {
                material.SetFloat("_Roughness", 1.0f - smoothness);
            }

            material.SetFloat("_Metallic", Mathf.Pow(metallic, 2.2f));
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


        }

    }
}