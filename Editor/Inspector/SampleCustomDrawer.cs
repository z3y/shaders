using UnityEditor;
using UnityEngine;

namespace z3y.Shaders
{
    // Usage: [CustomDrawer(SampleCustomDrawer)] 
    public class SampleCustomDrawer : ICustomPropertyDrawer
    {

        public void OnInitializeEditor(DefaultInspector.Property property, MaterialEditor editor, MaterialProperty[] materialProperties)
        {
            // executes once before the first frame
            var attributes = property.attributes;
        }

        public void OnInspectorGUI(DefaultInspector.Property property, MaterialEditor editor, MaterialProperty[] materialProperties)
        {
            // executes every frame
            EditorGUILayout.LabelField("This is a SampleCustomDrawer");
            var materialProperty = materialProperties[property.index];
            editor.ShaderProperty(materialProperty, property.displayName);
        }
    }
}
