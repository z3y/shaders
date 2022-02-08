#if !VRC_SDK_VRCSDK3 || UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class TextureArraySelector : MonoBehaviour
{
    public List<int> index = new List<int>();

    private void OnValidate()
    {
        var mesh = GetMesh(gameObject);
        for (int i = 0; i < mesh?.subMeshCount; i++)
        {
            if(index.Count != mesh.subMeshCount) index.Add(0);
            index[i] = Mathf.Clamp(index[i], 0, int.MaxValue);
            SetUVW(mesh, i, index[i]);
        }
    }


    public void SetUVW (Mesh mesh, int submeshIndex, int index)
    {
        if(mesh is null) return;
        List<Vector3> uvs = new List<Vector3>();
        mesh.GetUVs(0, uvs);

        HashSet<int> subMeshTris = new HashSet<int>(mesh.GetTriangles(submeshIndex));

        foreach (var a in subMeshTris)
        {
            uvs[a] = new Vector3(uvs[a].x, uvs[a].y, index);
        }
        mesh.SetUVs(0, uvs);
        
    }

    public Mesh GetMesh(GameObject go)
    {
        go.TryGetComponent<SkinnedMeshRenderer>(out var meshRenderer);
        go.TryGetComponent<MeshFilter>(out var meshFilter);
        var mesh = meshRenderer?.sharedMesh ?? meshFilter?.sharedMesh;
        return mesh;
    }
}

[CustomEditor(typeof(TextureArraySelector))]
public class TextureArraySelectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TextureArraySelector textureArraySelector = (TextureArraySelector)target;
        var mesh = textureArraySelector.GetMesh(textureArraySelector.gameObject);
        if (mesh is null) return;   
        
        // base.OnInspectorGUI();
        var serializedObject = new SerializedObject(textureArraySelector);
        SerializedProperty idx = serializedObject.FindProperty("index");

        serializedObject.Update();
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var value = idx.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(value);
            if(GUILayout.Button("-")) value.intValue --;
            if(GUILayout.Button("+")) value.intValue ++;
            GUILayout.EndHorizontal();
        }
        
        serializedObject.ApplyModifiedProperties();

    }
}
#endif