using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace z3y.Shaders
{
    public static class Helpers
    {
        public static IEnumerable<Material> FindMaterialsUsingShader(string shaderName)
        {
            var foundMaterials = new List<Material>();
            var renderers = Object.FindObjectsOfType<Renderer>();

            for (var i = 0; i < renderers?.Length; i++)
            {
                for (var j = 0; j < renderers[i].sharedMaterials?.Length; j++)
                {
                    var a = renderers[i].sharedMaterials[j]?.shader;
                    if (a == null || a.name != shaderName) continue;
                    
                    foundMaterials.Add(renderers[i].sharedMaterials[j]);
                }
            }
            return foundMaterials.Distinct();
        }
        
    }
}