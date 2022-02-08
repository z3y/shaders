#if BAKERY_INCLUDED
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace z3y.Shaders
{
    [InitializeOnLoad]
    public static class BakeryModeAutoSwitch
    {
        static BakeryModeAutoSwitch() => ftRenderLightmap.OnFinishedFullRender += OnBakeComplete;

        private static readonly int BakeryLightmapMode = Shader.PropertyToID("bakeryLightmapMode");
        private static readonly int Bakery = Shader.PropertyToID("Bakery");
        private static void OnBakeComplete(object sender, EventArgs e)
        {
            var storage = ftRenderLightmap.FindRenderSettingsStorage();
            var renderers = Object.FindObjectsOfType<Renderer>();

            for (var i = 0; i < renderers?.Length; i++)
            {
                var block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block);
                
                // bakery fails to remove property blocks on first bake after switching to none
                int bakeryMode = renderers[i].HasPropertyBlock() && storage.renderSettingsRenderDirMode != 0
                    ? block.GetInt(BakeryLightmapMode) : 0;
                
                for (var j = 0; j < renderers[i].sharedMaterials?.Length; j++)
                {
                    var a = renderers[i].sharedMaterials[j]?.shader;
                    if (a == null || !a.name.Equals(SimpleLitGUI.ShaderName) ) continue;
                    
                    var material = renderers[i].sharedMaterials[j];

                    switch (bakeryMode)
                    {
                        case 0: // none
                            material.SetFloat(Bakery, 0);
                            break;
                        case 2: // RNM
                            material.SetFloat(Bakery, 2);
                            break;
                        case 3: // SH
                            material.SetFloat(Bakery, 1);
                            break;
                    }
                    
                    material.ToggleKeyword("BAKERY_RNM", bakeryMode == 2);
                    material.ToggleKeyword("BAKERY_SH", bakeryMode == 3);
                }
            }
            
            Debug.Log("Bakery Mode Auto Switch Complete");
        }
        
    }
}
#endif