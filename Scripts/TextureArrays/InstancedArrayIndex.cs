#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class InstancedArrayIndex : MonoBehaviour
{
    [SerializeField] private int index = 0;

    private void OnValidate()
    {
        index = Mathf.Clamp(index, 0, int.MaxValue);
        Renderer r = gameObject.GetComponent<Renderer>();
        MaterialPropertyBlock b = new MaterialPropertyBlock();
        b.SetFloat("_TextureIndex", index);
        r.SetPropertyBlock(b);
    }
}

[CustomEditor(typeof(InstancedArrayIndex))]
public class InstancedArrayIndexEditor : Editor
{
    public override void OnInspectorGUI()
    {
        InstancedArrayIndex i = (InstancedArrayIndex)target;
        
        var s = new SerializedObject(i);
        SerializedProperty idx = serializedObject.FindProperty("index");

        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(idx);
        if(GUILayout.Button("-")) idx.intValue --;
        if(GUILayout.Button("+")) idx.intValue ++;
        GUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();

    }
}
#endif