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

    public class OnBuildCleanup
    {
        private static readonly int MetallicMap = Shader.PropertyToID("_MetallicMap");
        private static readonly int OcclusionMap = Shader.PropertyToID("_OcclusionMap");
        private static readonly int DetailMaskMap = Shader.PropertyToID("_DetailMaskMap");
        private static readonly int SmoothnessMap = Shader.PropertyToID("_SmoothnessMap");
        private static readonly int DetailSmoothnessPacking = Shader.PropertyToID("_DetailSmoothnessPacking");
        private static readonly int DetailAlbedoPacking = Shader.PropertyToID("_DetailAlbedoPacking");

        //[MenuItem("z3y/CleanUpTexturePacking")]
        public static void CleanUpTexturePacking()
        {
            foreach (var m in Helpers.FindMaterialsUsingShader("Simple Lit"))
            {
                m.SetTexture(MetallicMap, null);
                m.SetTexture(OcclusionMap, null);
                m.SetTexture(DetailMaskMap, null);
                m.SetTexture(SmoothnessMap, null);
                m.SetTexture(DetailAlbedoPacking, null);
                m.SetTexture(DetailSmoothnessPacking, null);
            }
        }

        
    }
}
