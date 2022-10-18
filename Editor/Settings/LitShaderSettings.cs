using System.Collections;
using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;

namespace z3y.Shaders
{
    public class LitShaderSettings : ScriptableObject
    {
        public bool defaultShader = false;
        //public Preset defaultPreset;
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
        public bool fixBlackLevel = true;

        // variants
        public enum CompileDirectional
        {
            OnlyEnabled,
            OnlyDisabled,
            CompileBoth,
        }
        public CompileDirectional directionalLightVariants = CompileDirectional.CompileBoth;
        public bool compileVertexLights = true;
        public bool compileLODCrossfade = false;

        // quest
        public bool q_DisableForwardAdd = true;
        public bool q_DisableShadowCaster = true;
    }

    
}
