using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace z3y
{
    public class BuildPreprocessor : IPreprocessShaders
    {
        public int callbackOrder => 69;

        private readonly ShaderKeyword _directional;
        private readonly ShaderKeyword _shadowsScreen;
        private readonly ShaderKeyword _shadowMask;
        private readonly ShaderKeyword _shadowMixing;

        private const string ShaderName = "Lit";

        public BuildPreprocessor()
        {
            _directional = new ShaderKeyword("DIRECTIONAL");
            _shadowsScreen = new ShaderKeyword("SHADOWS_SCREEN");
            _shadowMask = new ShaderKeyword("SHADOWS_SHADOWMASK");
            _shadowMixing = new ShaderKeyword("LIGHTMAP_SHADOW_MIXING");
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader.name != ShaderName)
            {
                return;
            }

            for (int i = data.Count - 1; i >= 0; --i)
            {
                bool directionalEnabled = data[i].shaderKeywordSet.IsEnabled(_directional);
                bool _shadowsScreenEnabled = data[i].shaderKeywordSet.IsEnabled(_shadowsScreen);
                bool _shadowMaskEnabled = data[i].shaderKeywordSet.IsEnabled(_shadowMask);
                bool _shadowMixingEnabled = data[i].shaderKeywordSet.IsEnabled(_shadowMixing);

                if (directionalEnabled && !_shadowsScreenEnabled && !_shadowMaskEnabled && !_shadowMixingEnabled)
                {
                    var shaderData = data[i];
                    shaderData.shaderKeywordSet.Disable(_directional);
                    data.Add(shaderData);

                }
            }
        }
    }
}
