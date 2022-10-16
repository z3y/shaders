using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class SetDefaultShader : AssetPostprocessor
    {
        void OnPostprocessMaterial(Material material)
        {
            if (!ProjectSettings.litShaderSettings.defaultShader)
            {
                return;
            }

            material.shader = ProjectSettings.lit;
        }
    }
}