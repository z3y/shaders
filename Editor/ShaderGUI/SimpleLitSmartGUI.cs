using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class SimpleLitSmartGUI : SmartGUI
    {
        private MaterialProperty _mainTex;
        private MaterialProperty _color;
        private MaterialProperty _glossiness;
        private MaterialProperty _testFoldout;
        private MaterialProperty _testFoldout2;
        private MaterialProperty _Metallic;
        private MaterialProperty _TestFoldout3;
        private MaterialProperty _EnableEmission;
        private MaterialProperty _EmissionMultBase;
        public override void OnGUIProperties(MaterialEditor m, MaterialProperty[] materialProperties, Material material)
        {
            if (Foldout(_testFoldout))
            {

                Draw(_mainTex, _color, null, "On Hover");
                EditorGUI.indentLevel++;
                Draw(_glossiness, null, null, "On Hover");
                EditorGUI.indentLevel--;
            }

            if (Foldout(_testFoldout2))
            {
                Draw(_Metallic);
            }



            if (Foldout(_TestFoldout3))
            {
                Draw(_EnableEmission);
            }

            Draw(_EmissionMultBase);


        }
    }

    public class SimpleLitBetterGUI2 : SmartGUI
    {
        private MaterialProperty _Test;
        private MaterialProperty _Test23;
        private MaterialProperty _Color;

        public override void OnGUIProperties(MaterialEditor m, MaterialProperty[] materialProperties, Material material)
        {
            Draw(_Test);
            Draw(_Test23);
            Draw(_Color);
        }

        public override void OnValidate(Material material)
        {

        }
    }
}
