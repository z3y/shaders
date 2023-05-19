using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace z3y
{

    public class BuildPreprocessor : IPreprocessShaders
    {
        public int callbackOrder => 0;

        private const string PropertyName = "__LitShaderVariant";

        private readonly ShaderKeyword _lightmapOn;
        public BuildPreprocessor()
        {
            _lightmapOn = new ShaderKeyword("LIGHTMAP_ON");
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (!shader.GetPropertyName(0).Equals(PropertyName, System.StringComparison.Ordinal))
            {
                return;
            }

            for (int i = 0; i < data.Count; ++i)
            {
                var d = data[i];
                var keywordSet = d.shaderKeywordSet;
                bool lightmapEnabled = keywordSet.IsEnabled(_lightmapOn);

                if (!lightmapEnabled)
                {
                    var localBakeryMonoSH = new ShaderKeyword(shader, "BAKERY_MONOSH");
                    if (keywordSet.IsEnabled(localBakeryMonoSH))
                    {
                        Debug.Log("Removing keyword " + string.Join(" ", keywordSet.GetShaderKeywords().Select(x=>x.GetName()).ToArray()));
                        data.RemoveAt(i);
                        --i;
                    }
                }
            }
        }
    }
}
