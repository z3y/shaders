using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace z3y.Shaders
{
    [InitializeOnLoad]
    public static class UpdateLitShaderFile
    {
        public static string SessionKey
        {
            get
            {
                return "UpdateLitShaderFile" + ProjectSettings.ShaderVersion;
            }
        }

        static UpdateLitShaderFile()
        {
            var isApplied = SessionState.GetBool(SessionKey, false);

            if (isApplied)
            {
                return;
            }
            EditorApplication.update += UpdateConfig;
            EditorApplication.update += RemoveUpdateConfigActionAfterUpdate;

        }

        private static void RemoveUpdateConfigActionAfterUpdate()
        {
            var isApplied = SessionState.GetBool(SessionKey, false);

            if (!isApplied || ProjectSettings.lit is null)
            {
                return;
            }
            EditorApplication.update -= UpdateConfig;
            EditorApplication.update -= RemoveUpdateConfigActionAfterUpdate;
        }

        public static void UpdateConfig()
        {
            if (ProjectSettings.lit is null)
            {
                return;
            }

            #if !LTCGI_INCLUDED
            ProjectSettings.ShaderSettings.allowLTCGI = false;
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
            SessionState.SetBool(SessionKey, true);
        }

        private static string GetConfig()
        {
            var sb = new StringBuilder();

            // bakeryMode
            if (ProjectSettings.ShaderSettings.bakeryMode != LitShaderSettings.BakeryMode.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants BAKERY_SH");
                sb.AppendLine("#pragma skip_variants BAKERY_RNM");
                sb.AppendLine("#pragma skip_variants BAKERY_MONOSH");
            }
            switch (ProjectSettings.ShaderSettings.bakeryMode)
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
            if (ProjectSettings.ShaderSettings.nonLinearLightmapSH != LitShaderSettings.NonLinearLightmapSH.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants BAKERY_SHNONLINEAR_OFF");
            }
            switch (ProjectSettings.ShaderSettings.nonLinearLightmapSH)
            {
                case LitShaderSettings.NonLinearLightmapSH.ForceDisabled:
                    sb.AppendLine("#define BAKERY_SHNONLINEAR_OFF");
                    break;
                case LitShaderSettings.NonLinearLightmapSH.ForceEnabled:
                    // default
                    break;
            }

            // bicubicLightmap
            if (ProjectSettings.ShaderSettings.bicubicLightmap != LitShaderSettings.BicubicLightmap.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants _BICUBICLIGHTMAP");
            }
            switch (ProjectSettings.ShaderSettings.bicubicLightmap)
            {
                case LitShaderSettings.BicubicLightmap.ForceDisabled:
                    // default
                    break;
                case LitShaderSettings.BicubicLightmap.ForceEnabled:
                    sb.AppendLine("#define _BICUBICLIGHTMAP");
                    break;
            }

            // allowLPPV
            if (ProjectSettings.ShaderSettings.allowLPPV)
            {
                sb.AppendLine("#define _ALLOW_LPPV");
            }

            // nonLinearLightprobeSH
            if (ProjectSettings.ShaderSettings.nonLinearLightprobeSH != LitShaderSettings.NonLinearLightprobeSH.PerMaterial)
            {
                sb.AppendLine("#pragma skip_variants NONLINEAR_LIGHTPROBESH");
            }
            switch (ProjectSettings.ShaderSettings.nonLinearLightprobeSH)
            {
                case LitShaderSettings.NonLinearLightprobeSH.ForceDisabled:
                    // default
                    break;
                case LitShaderSettings.NonLinearLightprobeSH.ForceEnabled:
                    sb.AppendLine("#define NONLINEAR_LIGHTPROBESH");
                    break;
            }

            // dithering
            if (ProjectSettings.ShaderSettings.dithering)
            {
                sb.AppendLine("#define DITHERING");
            }

            // aces
            if (ProjectSettings.ShaderSettings.aces)
            {
                sb.AppendLine("#define ACES_TONEMAPPING");
            }

            // fixBlackLevel
            if (ProjectSettings.ShaderSettings.fixBlackLevel)
            {
                sb.AppendLine("#define FIX_BLACK_LEVEL");
            }

            // compileVertexLights
            if (!ProjectSettings.ShaderSettings.compileVertexLights)
            {
                sb.AppendLine("#pragma skip_variants VERTEXLIGHT_ON");
            }

            // compileLODCrossfade
            if (!ProjectSettings.ShaderSettings.compileLODCrossfade)
            {
                sb.AppendLine("#pragma skip_variants LOD_FADE_CROSSFADE");
            }

            // allowLTCGI
            if (!ProjectSettings.ShaderSettings.allowLTCGI)
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

