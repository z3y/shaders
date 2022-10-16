using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    [InitializeOnLoad]
    public static class UpdateLitShaderFile
    {
        const string shaderName = "Lit";
        private static Shader _lit = Shader.Find(shaderName);

        static UpdateLitShaderFile() => UpdateConfig();

        public static void UpdateConfig()
        {
            if (_lit == null)
            {
                Debug.Log($"Shader {shaderName} not found");
                return;
            }

            #if !LTCGI_INCLUDED
            ProjectSettings.litShaderSettings.allowLTCGI = false;
            #endif

            var path = AssetDatabase.GetAssetPath(_lit);
            var lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Equals("//ConfigStart"))
                {
                    for (int j = i; j < lines.Length; j++)
                    {
                        if (lines[j].Equals("//ConfigEnd"))
                        {
                            break;
                        }

                        lines[j] = string.Empty;
                    }

                    lines[i] += Environment.NewLine;
                    lines[i] += GetConfig();
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



            return sb.ToString();
        }
    }
}

