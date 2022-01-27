using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace z3y.Shaders
{
    public static class GUIHelpers
    {
       public static void PropertyGroup(Action action)
       {
            GUILayout.Space(1);
			using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(1);
                action();
                GUILayout.Space(1);
			}
			GUILayout.Space(1);
		}

		public static bool boolValue(this MaterialProperty p)
        {
			if (p.type == MaterialProperty.PropType.Texture)
				return p.textureValue;

			return p.floatValue == 1;
        }
       
       public static bool TextureFoldout(bool display)
       {
	       //var rect = GUILayoutUtility.GetRect(16f, -4);
	       var lastRect = GUILayoutUtility.GetLastRect();
	       var e = Event.current;
	       var toggleRect = new Rect(lastRect.x - 15f, lastRect.y + 2f, 12f, 12f);
	       if (e.type == EventType.Repaint)
	       {
		       EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
	       }
	       if (e.type == EventType.MouseDown && toggleRect.Contains(e.mousePosition))
	       {
		       display = !display;
		       e.Use();
	       }
	       return display;
       }

        // Mimics the normal map import warning - written by Orels1
		private static bool TextureImportWarningBox(string message)
        {
			GUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
			EditorGUILayout.LabelField(message, new GUIStyle(EditorStyles.label) { fontSize = 11, wordWrap = true });
			EditorGUILayout.BeginHorizontal(new GUIStyle() { alignment = TextAnchor.MiddleRight }, GUILayout.Height(24));
			EditorGUILayout.Space();
			var buttonPress = GUILayout.Button("Fix Now", new GUIStyle("button")
			{
				stretchWidth = false,
				margin = new RectOffset(0, 0, 0, 0),
				padding = new RectOffset(8, 8, 0, 0)
			}, GUILayout.Height(22));
			EditorGUILayout.EndHorizontal();
			GUILayout.EndVertical();
			return buttonPress;
		}


		public static void sRGBWarning(MaterialProperty tex)
		{
            if (!tex?.textureValue) return;
			var texPath = AssetDatabase.GetAssetPath(tex.textureValue);
			var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
			if (importer == null) return;
            const string text = "This texture is marked as sRGB, but should be linear.";
			if (!importer.sRGBTexture || !TextureImportWarningBox(text)) return;
			importer.sRGBTexture = false;
			importer.SaveAndReimport();
		}

		public static void InitializeAllProperties(MaterialProperty[] props, object obj, Func<string, MaterialProperty[], bool, MaterialProperty> findProperty)
		{
			foreach (var property in obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (property.FieldType != typeof(MaterialProperty))
					continue;
				
				property.SetValue(obj, findProperty(property.Name, props, false));
			}
		}

        public static void DrawMaterialProperty(this MaterialEditor me, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null)
        {
            if (property is null) return;
            if (property.type == MaterialProperty.PropType.Texture) 
            {
                string[] p = property.displayName.Split(':');
                EditorGUILayout.BeginHorizontal();
                me.TexturePropertySingleLine(new GUIContent(p[0], p.Length == 2 ? p[1] : null), property, extraProperty);

                if (extraProperty2 == null)
                {
                    EditorGUILayout.EndHorizontal();
                    return;
                }
				
                EditorGUILayout.LabelField("");
                var lastRect = GUILayoutUtility.GetLastRect();
                var rect = new Rect(new Vector2(Screen.width/2f+5, lastRect.y), new Vector2(lastRect.size.x-15, lastRect.size.y));
                me.ShaderProperty(rect, extraProperty2, extraProperty2.displayName,4);
                EditorGUILayout.EndHorizontal();
                return;
            }

			me.ShaderProperty(property, property.displayName);
		}
        
        public static void DrawRangedProperty(this MaterialEditor me, MaterialProperty min, MaterialProperty max, float minLimit = 0, float maxLimit = 1, MaterialProperty tex = null)
        {
	        float currentMin = min.floatValue;
	        float currentMax = max.floatValue;
	        EditorGUILayout.BeginHorizontal();

	        if(tex is null)
		        EditorGUILayout.LabelField(max.displayName);
	        else
		        me.TexturePropertySingleLine(new GUIContent(tex.displayName), tex);


	        EditorGUI.indentLevel -= 6;
	        EditorGUI.BeginChangeCheck();
	        EditorGUILayout.MinMaxSlider(ref currentMin,ref currentMax, minLimit, maxLimit);
	        if(EditorGUI.EndChangeCheck())
	        {
		        min.floatValue = currentMin;
		        max.floatValue = currentMax;
	        }
	        EditorGUI.indentLevel += 6;
	        EditorGUILayout.EndHorizontal();
        }


        public static void SetupGIFlags(float emissionEnabled, Material material)
        {
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (emissionEnabled != 1)
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }
        
        public static void ToggleKeyword(this Material mat, string keyword, bool toggle)
        {
	        if (toggle)
	        {
		        mat.EnableKeyword(keyword);
		        return;
	        }
		    mat.DisableKeyword(keyword);
        }
        
        public static void SetupMaterialWithBlendMode(Material material, int type)
        {
            switch (type)
            {
                case 0:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_AlphaToMask", 0);
                    material.renderQueue = -1;
                    break;
                case 1:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetInt("_AlphaToMask", 1);
                    break;
                case 2:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetInt("_AlphaToMask", 0);
                    break;
                case 3:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetInt("_AlphaToMask", 0);
                    break;
            }
        }

    }

}

