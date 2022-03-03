using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class SimpleLitBetterGUI : BetterGUI
    {
        private MaterialProperty _mainTex;
        private MaterialProperty _color;
        private MaterialProperty _glossiness;
        private MaterialProperty _testFoldout;

        public override void OnGUIProperties(MaterialEditor m, MaterialProperty[] materialProperties, Material material)
        {
            if (Foldout(_testFoldout))
            {

                Draw(_mainTex, _color, null, "On Hover");
            }

            Draw(_glossiness, null, null, "On Hover");
            
        }
    }

    public class SimpleLitBetterGUI2 : BetterGUI
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
