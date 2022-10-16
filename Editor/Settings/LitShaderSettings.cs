using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace z3y.Shaders
{
    public class LitShaderSettings : ScriptableObject
    {
        public bool defaultShader = false;
        public enum BakeryMode
        {
            PerMaterial,
            ForceRNM,
            ForceSH,
            ForceMonoSH
        }
        public BakeryMode bakeryMode = BakeryMode.PerMaterial;
        public enum BicubicLightmap
        {
            PerMaterial,
            ForceDisabled,
            ForceEnabled
        }
        public BicubicLightmap bicubicLightmap = BicubicLightmap.PerMaterial;

        public enum NonLinearLightprobeSH
        {
            PerMaterial,
            ForceDisabled,
            ForceEnabled
        }
        public NonLinearLightprobeSH nonLinearLightprobeSH = NonLinearLightprobeSH.PerMaterial;

        public enum NonLinearLightmapSH
        {
            PerMaterial,
            ForceDisabled,
            ForceEnabled
        }
        public NonLinearLightmapSH nonLinearLightmapSH = NonLinearLightmapSH.PerMaterial;

        public bool allowLPPV = false;
        public bool allowLTCGI = false;

        // global keywords
        public bool dithering = false;
        public bool aces = false;

        // variants
        public bool compileVariantsWithoutDirectionalLight = true;
        public bool compileVertexLights = true;
        public bool compileLODCrossfade = false;
    }

    
}
