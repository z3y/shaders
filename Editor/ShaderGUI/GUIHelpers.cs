using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

namespace z3y.Shaders
{
    public static class GUIHelpers
    {
		public static Texture2D groupTex = (Texture2D)Resources.Load(EditorGUIUtility.isProSkin ? "z3y_shaders_foldout_group" : "z3y_shaders_foldout_group_light", typeof(Texture2D));
        public static bool DrawGroupFoldout(Material material, string title, bool defaultValue)
        {
            var tagName = $"z3y_group_foldout_{title.Replace(' ', '_')}";
            bool isOpen = material.HasProperty(tagName) ? material.GetFloat(tagName) == 1 : defaultValue;
            var HeaderHeight = 18;
            var style = new GUIStyle("BoldLabel")
            {
                font = new GUIStyle(EditorStyles.boldLabel).font,
                fontSize = 12,
                fixedHeight = HeaderHeight,
                contentOffset = new Vector2(18f, 0f)
            };
            var rect = GUILayoutUtility.GetRect(16f, HeaderHeight, style);
            var rect2 = new Rect(rect.x + -20f, rect.y, rect.width + 30f, rect.height + 2);
            var rectText = new Rect(rect.x - 8f, rect.y + 1, rect.width, rect.height);
            GUI.DrawTexture(rect2, groupTex);
            GUI.Label(rectText, title, style);
            if (isOpen) EditorGUILayout.Space();

            var e = Event.current;
            var toggleRect = new Rect(rect2.x + 12f, rect2.y + 3f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, isOpen, false);
            }
            if (e.type == EventType.MouseDown && rect2.Contains(e.mousePosition))
            {
                isOpen = !isOpen;
                e.Use();
            }
            material.SetFloat(tagName, isOpen ? 1 : 0);
            return isOpen;
        }

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

        public static bool BoolValue(this MaterialProperty p) => p.type == MaterialProperty.PropType.Texture ? p.textureValue : p.floatValue == 1;

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
        

        public static void InitializeAllProperties(FieldInfo[] infos,MaterialProperty[] props, object obj, Func<string, MaterialProperty[], bool, MaterialProperty> findProperty)
		{
			foreach (var property in infos)
			{
				if (property.FieldType != typeof(MaterialProperty))
					continue;
				
				property.SetValue(obj, findProperty(property.Name, props, false));
			}
		}

        public static void DrawMaterialProperty(this MaterialEditor me, MaterialProperty property, MaterialProperty extraProperty = null, MaterialProperty extraProperty2 = null, string nameOverride = null)
        {
            if (property is null) return;
            if (property.type == MaterialProperty.PropType.Texture) 
            {
                if (extraProperty2 != null)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                string[] p = property.displayName.Split(':');
                me.TexturePropertySingleLine(new GUIContent(nameOverride ?? p[0], p.Length == 2 ? p[1] : null), property, extraProperty);

                if (extraProperty2 == null)
                {
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
                case 1: // cutout
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetInt("_AlphaToMask", 1);
                    break;
                case 2: // alpha fade
                    material.SetupTransparentMaterial();
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 3: // premultiply
                    material.SetupTransparentMaterial();
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 4: // additive
	                material.SetupTransparentMaterial();
	                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
	                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
	                break;
                case 5: // multiply
	                material.SetupTransparentMaterial();
	                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
	                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
	                break;
            }
        }

        private static void SetupTransparentMaterial(this Material material)
        {
	        material.SetOverrideTag("RenderType", "Transparent");
	        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
	        material.SetInt("_ZWrite", 0);
	        material.SetInt("_AlphaToMask", 0);
        }
    }
}

