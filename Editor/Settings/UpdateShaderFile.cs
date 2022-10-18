using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    // TODO: update the config less often 
    [InitializeOnLoad]
    public static class UpdateLitShaderFile
    {

        static UpdateLitShaderFile() => UpdateConfig();

        public static void UpdateConfig()
        {
            if (ProjectSettings.lit is null)
            {
                Debug.Log($"Shader {ProjectSettings.shaderName} not found");
                return;
            }

            #if !LTCGI_INCLUDED
            ProjectSettings.litShaderSettings.allowLTCGI = false;
            #endif

            var path = AssetDatabase.GetAssetPath(ProjectSettings.lit);
            var lines = File.ReadAllLines(path).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("//ConfigStart", System.StringComparison.Ordinal))
                {
                    lines[i] += "\n";
                    lines[i] += GetConfig();

                    for (int j = i+1; j < lines.Count; j++)
                    {
                        if (lines[j].StartsWith("//ConfigEnd", System.StringComparison.Ordinal))
                        {
                            break;
                        }

                        lines.RemoveAt(j);
                        j--;
                    }

                    break;
                }
            }

            File.WriteAllLines(path, lines);

            AssetDatabase.ImportAsset(path);
        }

        private static string GetConfig()
        {
            var sb = new StringBuilder();

            // bakeryMode
            if (ProjectSettings.litShaderSettings.bakeryMode != LitShaderSettings.BakeryMode.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants BAKERY_SH");
                sb.AppendLine("#pragma skip_variants BAKERY_RNM");
                sb.AppendLine("#pragma skip_variants BAKERY_MONOSH");
            }
            switch (ProjectSettings.litShaderSettings.bakeryMode)
            {
                case LitShaderSettings.BakeryMode.ForceRNM:
                    sb.AppendLine("#define BAKERY_RNM");
                    break;
                case LitShaderSettings.BakeryMode.ForceSH:
                    sb.AppendLine("#define BAKERY_SH");
                    break;
                case LitShaderSettings.BakeryMode.ForceMonoSH:
                    sb.AppendLine("#define BAKERY_MONOSH");
                    break;
            }

            // nonLinearLightmapSH
            if (ProjectSettings.litShaderSettings.nonLinearLightmapSH != LitShaderSettings.NonLinearLightmapSH.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants BAKERY_SHNONLINEAR_OFF");
            }
            switch (ProjectSettings.litShaderSettings.nonLinearLightmapSH)
            {
                case LitShaderSettings.NonLinearLightmapSH.ForceDisabled:
                    sb.AppendLine("#define BAKERY_SHNONLINEAR_OFF");
                    break;
                case LitShaderSettings.NonLinearLightmapSH.ForceEnabled:
                    // default
                    break;
            }

            // bicubicLightmap
            if (ProjectSettings.litShaderSettings.bicubicLightmap != LitShaderSettings.BicubicLightmap.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants _BICUBICLIGHTMAP");
            }
            switch (ProjectSettings.litShaderSettings.bicubicLightmap)
            {
                case LitShaderSettings.BicubicLightmap.ForceDisabled:
                    // default
                    break;
                case LitShaderSettings.BicubicLightmap.ForceEnabled:
                    sb.AppendLine("#define _BICUBICLIGHTMAP");
                    break;
            }

            // allowLPPV
            if (ProjectSettings.litShaderSettings.allowLPPV)
            {
                sb.AppendLine("#define _ALLOW_LPPV");
            }

            // nonLinearLightprobeSH
            if (ProjectSettings.litShaderSettings.nonLinearLightprobeSH != LitShaderSettings.NonLinearLightprobeSH.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants NONLINEAR_LIGHTPROBESH");
            }
            switch (ProjectSettings.litShaderSettings.nonLinearLightprobeSH)
            {
                case LitShaderSettings.NonLinearLightprobeSH.ForceDisabled:
                    // default
                    break;
                case LitShaderSettings.NonLinearLightprobeSH.ForceEnabled:
                    sb.AppendLine("#define NONLINEAR_LIGHTPROBESH");
                    break;
            }

            // dithering
            if (ProjectSettings.litShaderSettings.dithering)
            {
                sb.AppendLine("#define DITHERING");
            }

            // aces
            if (ProjectSettings.litShaderSettings.aces)
            {
                sb.AppendLine("#define ACES_TONEMAPPING");
            }

            // compileVertexLights
            if (!ProjectSettings.litShaderSettings.compileVertexLights)
            {
                sb.AppendLine("#pragma skip_variants VERTEXLIGHT_ON");
            }

            // compileLODCrossfade
            if (!ProjectSettings.litShaderSettings.compileLODCrossfade)
            {
                sb.AppendLine("#pragma skip_variants LOD_FADE_CROSSFADE");
            }

            // allowLTCGI
            if (!ProjectSettings.litShaderSettings.allowLTCGI)
            {
                sb.AppendLine("#pragma skip_variants LTCGI");
                sb.AppendLine("#pragma skip_variants LTCGI_DIFFUSE_OFF");
            }
            else
            {
                sb.AppendLine("#ifdef LTCGI\n#include \"Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc\"\n#endif");
            }



            return sb.ToString();
        }
    }
}

