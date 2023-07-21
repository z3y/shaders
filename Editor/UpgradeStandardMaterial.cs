using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{

    public class UpgradeStandardMaterial
    {

        public static void Upgrade()
        {
            var selection = Selection.GetFiltered<Object>(SelectionMode.Assets);
            foreach (var obj in selection)
            {
                if (!(obj is Material material))
                {
                    continue;
                }
                
                var nma = material.name;

                var metallicSmoothness = material.GetTexture("_MetallicGlossMap");
                var occlusionTexture = material.GetTexture("_OcclusionMap");
                var roughness = 1.0f - material.GetFloat("_Glossiness");
                
            }
        }
    }
}