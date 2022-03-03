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

        private MaterialProperty _testFoldout;
        private MaterialProperty _testFoldout2;
        private MaterialProperty _testFoldout3;
        private MaterialProperty _EnableEmission;
        private MaterialProperty _EmissionMap;

        public override void OnGUIProperties(MaterialEditor m, MaterialProperty[] materialProperties, Material material)
        {
            if (Foldout(_testFoldout))
            {

                Draw(_mainTex, _color);

            }

            if (Foldout(_testFoldout2))
            {

            }



            if (Foldout(_testFoldout3))
            {
                Draw(_EnableEmission);
                if (_EnableEmission.floatValue == 1)

                {
                    Draw(_EmissionMap);
                }

            }


        }
    }
}
