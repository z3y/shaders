#if !VRC_SDK_VRCSDK3 || UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    [ExecuteInEditMode, AddComponentMenu("z3y/Instanced Properties"), HelpURL("https://github.com/z3y/shaders/wiki/Avanced-Features#instanced-properties")]
    public class InstancedPropertyBlocks : MonoBehaviour
    {
        public int Index = 0;
        public Color BaseColor;
        [ColorUsage(false, true)] public Color EmissionColor;
        public Vector2 Tiling;
        public Vector2 Offset;

        [SerializeField, HideInInspector] private bool _hasInitialized;

        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private static readonly int TextureIndex = Shader.PropertyToID("_TextureIndex");
        private static readonly int EmissionColor1 = Shader.PropertyToID("_EmissionColor");
        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");

        private void OnValidate()
        {
            Index = Mathf.Clamp(Index, 0, int.MaxValue);
            var renderer = gameObject.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            if (renderer.HasPropertyBlock())
            {
                renderer.GetPropertyBlock(block);
            }

            block.SetFloat(TextureIndex, Index);
            block.SetColor(Color1, BaseColor);
            block.SetColor(EmissionColor1, EmissionColor);
            block.SetVector(MainTexSt, new Vector4(Tiling.x, Tiling.y, Offset.x, Offset.y));

            renderer.SetPropertyBlock(block);
        }

        private void Awake()
        {
            if (_hasInitialized)
            {
                return;
            }

            var material = gameObject.GetComponent<Renderer>().sharedMaterial;
            BaseColor = material.GetColor(Color1);
            EmissionColor = material.GetColor(EmissionColor1);

            var tileOffset = material.GetVector(MainTexSt);
            Tiling = new Vector2(tileOffset.x, tileOffset.y);
            Offset = new Vector2(tileOffset.z, tileOffset.w);

            _hasInitialized = true;
        }
    }

    [CustomEditor(typeof(InstancedPropertyBlocks)), CanEditMultipleObjects]
    public class InstancedPropertyBlocksEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var index = serializedObject.FindProperty("Index");
            var baseColor = serializedObject.FindProperty("BaseColor");
            var emissionColor = serializedObject.FindProperty("EmissionColor");
            var tiling = serializedObject.FindProperty("Tiling");
            var offset = serializedObject.FindProperty("Offset");

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(index, new GUIContent("Array Index"));
            if (GUILayout.Button("-")) index.intValue--;
            if (GUILayout.Button("+")) index.intValue++;
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(baseColor);
            EditorGUILayout.PropertyField(tiling);
            EditorGUILayout.PropertyField(offset);
            EditorGUILayout.PropertyField(emissionColor);

            serializedObject.ApplyModifiedProperties();

        }
    }
}
#endif