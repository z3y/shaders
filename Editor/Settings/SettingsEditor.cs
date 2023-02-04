using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace z3y.Shaders
{
    internal static class SettingsEditor
    {
        public static VisualElement SettingsContainer(SerializedProperty settings)
        {
            var root = new VisualElement();


            foreach (SerializedProperty sp in settings)
            {
                if (sp.depth != 1)
                {
                    continue;
                }

                var p = new PropertyField();
                p.BindProperty(sp);
                root.Add(p);
            }

            


            return root;
        }
    }
}
