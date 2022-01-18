#if UDON
using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Configuration;
using UdonSharpEditor;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace z3y.InstancedPropertyBlocks
{
    #if UDON
    public class InstancedArrayProperty : UdonSharpBehaviour
    {
        public MeshRenderer[] renderers;
        public int[] arrayIndex;

        void Start()
        {
            #if UNITY_EDITOR
            return;
            #endif
            for (int i = 0; i < renderers.Length; i++)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                
                if (renderers[i].HasPropertyBlock())
                {
                    renderers[i].GetPropertyBlock(propertyBlock);
                }
                propertyBlock.SetFloat("_TextureIndex", arrayIndex[i]);
                renderers[i].SetPropertyBlock(propertyBlock);
            }

            gameObject.SetActive(false);
        }
    }
    #endif

    #if !COMPILER_UDONSHARP && UNITY_EDITOR
    public class InstancedArrayPropertyEditor : Editor
    {

        // [MenuItem("Tools/Set Instanced Array Properties")]
        public static void SetProperties()
        {
            GameObject obj = GameObject.Find("InstancedArrayProperty");
            if (obj == null) return;
            if(PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab) PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            InstancedArrayProperty uba = obj.GetUdonSharpComponent<InstancedArrayProperty>();

            MeshRenderer[] renderersEditor = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            List<MeshRenderer> renderersEditorClean = new List<MeshRenderer>();


            List<int> arrayIndex = new List<int>();


            for (int i = 0; i < renderersEditor.Length; i++)
            {
                MaterialPropertyBlock b = new MaterialPropertyBlock();
                renderersEditor[i].GetPropertyBlock(b);

                int idx = (int) b.GetFloat("_TextureIndex");

                if (idx != 0)
                {
                    for (int j = 0; j < renderersEditor[i].sharedMaterials.Length; j++)
                    {
                        Material mat = renderersEditor[i].sharedMaterials[j];
                        if (mat == null) continue;
                        renderersEditorClean.Add(renderersEditor[i]);
                        arrayIndex.Add(idx);
                    }
                }

            }

            MeshRenderer[] r = renderersEditorClean.ToArray();

            uba.UpdateProxy();
            uba.renderers = r;
            uba.arrayIndex = arrayIndex.ToArray();
            uba.ApplyProxyModifications();
        }
    }

    public class SetInstancedArrayProperties : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 60;

        bool IVRCSDKBuildRequestedCallback.OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            InstancedArrayPropertyEditor.SetProperties();
            return true;
        }
    }
    #endif
}
#endif