using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using z3y.Shaders;

public class ModuleTest : SmartGUI
{
    private MaterialProperty _Test;
    public override void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
    {
        Draw(_Test);
    }
}

public class ModuleTest2 : SmartGUI
{
    private MaterialProperty _Test2;
    public override void OnGUIProperties(MaterialEditor materialEditor, MaterialProperty[] materialProperties, Material material)
    {
        Draw(_Test2);
    }


    public override void OnValidate(Material material)
    {
        Debug.Log("Validating " + _Test2.floatValue);
    }
}