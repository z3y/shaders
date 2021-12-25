using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace z3y.Shaders.SimpleLit
{
    
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
    public class LockAllMaterialsOnVRCWorldUpload : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 1;

        bool IVRCSDKBuildRequestedCallback.OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            OnBuildCleanup.CleanUpTexturePacking();
            return true;
        }
    }
#endif

    public static class OnBuildCleanup
    {
        private static readonly int MetallicMap = Shader.PropertyToID("_MetallicMap");
        private static readonly int OcclusionMap = Shader.PropertyToID("_OcclusionMap");
        private static readonly int DetailMaskMap = Shader.PropertyToID("_DetailMaskMap");
        private static readonly int SmoothnessMap = Shader.PropertyToID("_SmoothnessMap");

        // [MenuItem("OnBuildCleanup/CleanUpTexturePacking")]
        public static void CleanUpTexturePacking()
        {
            foreach (var m in FindMaterialsUsingShader("Simple Lit"))
            {
                m.SetTexture(MetallicMap, null);
                m.SetTexture(OcclusionMap, null);
                m.SetTexture(DetailMaskMap, null);
                m.SetTexture(SmoothnessMap, null);
            }
        }

        private static Material[] FindMaterialsUsingShader(string shaderName)
        {
            List<Material> foundMaterials = new List<Material>();

            var renderers = Object.FindObjectsOfType<Renderer>();

            for (int i = 0; i < renderers?.Length; i++)
            {
                for (int j = 0; j < renderers[i].sharedMaterials?.Length; j++)
                {
                    var a = renderers[i].sharedMaterials[j]?.shader;
                    if (a != null &&
                        a.name == shaderName)
                        foundMaterials.Add(renderers[i].sharedMaterials[j]);
                }
            }

            return foundMaterials.Distinct().ToArray();
        }
    }
}
