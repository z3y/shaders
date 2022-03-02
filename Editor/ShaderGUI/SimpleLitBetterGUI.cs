using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    public class SimpleLitBetterGUI : BetterGUI
    {
        private MaterialProperty _MainTex;
        private MaterialProperty _Color;

        public override void OnGUIProperties(MaterialEditor m, MaterialProperty[] materialProperties, Material material)
        {
            m.Draw(_MainTex, _Color);
        }
    }

    public class SimpleLitBetterGUI2 : BetterGUI
    {
        private MaterialProperty _Test;
        private MaterialProperty _Test23;
        private MaterialProperty _Color;

        public override void OnGUIProperties(MaterialEditor m, MaterialProperty[] materialProperties, Material material)
        {
            m.Draw(_Test);
            m.Draw(_Test23);
            m.Draw(_Color);
        }

        public override void OnValidate(Material material)
        {

        }
    }
}
