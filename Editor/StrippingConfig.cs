using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace z3y.Shaders
{
    public class StrippingConfig : IPreprocessShaders
    {
        static string[] ShaderNames =
        {
            "Complex Lit"
        };

        public int callbackOrder => 0;

        
        private ShaderKeyword dynamicLightmap = new ShaderKeyword("DYNAMICLIGHTMAP_ON");
        private ShaderKeyword lightmap = new ShaderKeyword("LIGHTMAP_ON");

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader is null || !Array.Exists(ShaderNames, x => x == shader.name))
            {
                return;
            }

            for (int i = 0; i < data.Count; i++)
            {
                ShaderCompilerData d = data[i];
                var keywords = d.shaderKeywordSet;

                if (keywords.IsEnabled(lightmap) || keywords.IsEnabled(dynamicLightmap))
                { // lightmap on
                    if (keywords.IsEnabled(new ShaderKeyword(shader, "NONLINEAR_LIGHTPROBESH")))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                }
                else
                { // lightmap off
                    
                    if (keywords.IsEnabled(new ShaderKeyword(shader, "BAKERY_SH")) || keywords.IsEnabled(new ShaderKeyword(shader, "BAKERY_RNM")))
                    {
                        data.RemoveAt(i);
                        continue;
                    }
                }

#if UNITY_ANDROID
                if (keywords.IsEnabled(new ShaderKeyword(shader, "PARALLAX")) ||
                    keywords.IsEnabled(new ShaderKeyword(shader, "NONLINEAR_LIGHTPROBESH")) ||
                    keywords.IsEnabled(new ShaderKeyword(shader, "LTCGI")) ||
                    keywords.IsEnabled(new ShaderKeyword(shader, "LTCGI_DIFFUSE_OFF"))
                    )
                {
                    data.RemoveAt(i);
                    continue;
                }
#endif

            }
        }
    }
}
