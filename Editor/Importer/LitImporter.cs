using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
#if UNITY_2020_3_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace z3y.Shaders
{
    [ScriptedImporter(1, EXT, 0)]
    public class LitImporter : ScriptedImporter
    {
        public const string EXT = "litshader";

        public enum RenderPipeline
        {
            BuiltIn,
            URP
        }

        private const string TemplateLitBuiltIn = "Packages/com.z3y.shaders/Editor/Importer/Templates/T_LitBuiltIn.template";
        private const string TemplateLitURP = "Packages/com.z3y.shaders/Editor/Importer/Templates/T_LitURP.template";


        private const string TemplateUnlitBuiltIn = "Packages/com.z3y.shaders/Editor/Importer/Templates/T_UnlitBuiltIn.template";



        private const string DefaultShaderPath = "Packages/com.z3y.shaders/Shaders/Default.litshader";

        private const string DefaultPropertiesInclude = "Packages/com.z3y.shaders/Editor/Importer/Templates/Properties.txt";


        private const string LTCGIIncludePath = "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc";
        private static bool _ltcgiIncluded = File.Exists(LTCGIIncludePath);


        [SerializeField] public ShaderSettings settings;

        private static List<string> _sourceDependencies = new List<string>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
            if (oldShader != null) ShaderUtil.ClearShaderMessages(oldShader);

            _sourceDependencies.Clear();

            var code = ProcessFileLines(settings, ctx.assetPath, ctx.selectedBuildTarget);
            var shader = ShaderUtil.CreateShaderAsset(code, false);

            EditorMaterialUtility.SetShaderNonModifiableDefaults(shader, new[] { "_DFG", "BlueNoise" }, new Texture[] { DFGLut(), BlueNoise() });
            
            ctx.DependsOnSourceAsset("Assets/com.z3y.shaders/Editor/Importer/LitImporter.cs");

            foreach (var dependency in _sourceDependencies)
            {
                ctx.DependsOnSourceAsset(dependency);
            }

            if (_ltcgiIncluded)
            {
                ctx.DependsOnSourceAsset(LTCGIIncludePath);
            }


            ctx.AddObjectToAsset("MainAsset", shader);
            ctx.SetMainObject(shader);
        }

        [MenuItem("Assets/Create/Shader/Lit Shader Variant")]
        public static void CreateVariantFile()
        {
            var defaultContent = File.ReadAllText(DefaultShaderPath);
            ProjectWindowUtil.CreateAssetWithContent($"Lit Shader Variant.{EXT}", defaultContent);
        }

        const string defaultShaderEditor = "z3y.Shaders.DefaultInspector";


        private static string lastFolderPath = string.Empty;

        private static CurrentPass _currentPass = CurrentPass.ForwardBase;
        
        private enum CurrentPass
        {
            ForwardBase,
            ForwardAdd,
            ShadowCaster,
            Meta
        }

        private class ShaderData
        {
            public StringBuilder propertiesSb = new StringBuilder();
            public StringBuilder definesSb = new StringBuilder();
            public StringBuilder codeSb = new StringBuilder();
            public StringBuilder cbufferSb = new StringBuilder();

            public StringBuilder definesForwardBaseSb = new StringBuilder();
            public StringBuilder definesForwardAddSb = new StringBuilder();
            public StringBuilder definesShadowcasterSb = new StringBuilder();
            public StringBuilder definesMetaSb = new StringBuilder();
        }


        internal static string ProcessFileLines(ShaderSettings settings, string assetPath, BuildTarget buildTarget)
        {
            if (settings is null)
            {
                settings = new ShaderSettings();
            }


            bool isAndroid = buildTarget == BuildTarget.Android;
            lastFolderPath = Path.GetDirectoryName(assetPath);
            var fileLines = File.ReadLines(assetPath);

            var defaultProps = new StringBuilder(File.ReadAllText(DefaultPropertiesInclude));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.bakeryMonoSH, ShaderSettings.MonoShKeyword, "Mono SH"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.bicubicLightmap, ShaderSettings.BicubicLightmapKeyword, "Bicubic Lightmap"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword, "Geometric Specular AA"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword, "Anisotropy"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.lightmappedSpecular, ShaderSettings.LightmappedSpecular, "Lightmapped Specular"));

            if (!isAndroid && _ltcgiIncluded)
            {
                defaultProps.AppendLine(GetPropertyDeclaration(ShaderSettings.DefineType.LocalKeyword, "LTCGI", "Enable LTCGI"));
                defaultProps.AppendLine(GetPropertyDeclaration(ShaderSettings.DefineType.LocalKeyword, "LTCGI_DIFFUSE_OFF", "Disable LTCGI Diffuse"));
            }

            if (settings.grabPass) defaultProps.Append("[HideInInspector][ToggleUI]_GrabPass(\"GrabPass\", Float) = 1");

            RenderPipeline rp = QualitySettings.renderPipeline == null ? RenderPipeline.BuiltIn : RenderPipeline.URP;

            string[] template = null;

            if (settings.materialType == ShaderSettings.MaterialType.Lit)
            {
                if (rp == RenderPipeline.BuiltIn) template = File.ReadAllLines(TemplateLitBuiltIn);
                if (rp == RenderPipeline.URP) template = File.ReadAllLines(TemplateLitURP);
            }
            else if (settings.materialType == ShaderSettings.MaterialType.Unlit)
            {
                template = File.ReadAllLines(TemplateUnlitBuiltIn);
            }

            var shaderData = new ShaderData();


            Parse(fileLines, shaderData);
#if VRCHAT_SDK
            shaderData.definesSb.AppendLine("#define VRCHAT_SDK");
#endif

            if (isAndroid || !_ltcgiIncluded)
            {
                shaderData.definesSb.AppendLine("#pragma skip_variants LTCGI");
                shaderData.definesSb.AppendLine("#pragma skip_variants LTCGI_DIFFUSE_OFF");
            }
            else if (_ltcgiIncluded)
            {
                shaderData.definesSb.AppendLine("#define LTCGI_EXISTS");
            }

            for (int i = 0; i < template.Length; i++)
            {
                var trimmed = template[i].Trim();
                
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                if (trimmed.Equals("Name \"FORWARDBASE\""))
                {
                    _currentPass = CurrentPass.ForwardBase;
                }
                else if (trimmed.Equals("Name \"FORWARD_DELTA\""))
                {
                    _currentPass = CurrentPass.ForwardBase;
                }
                else if (trimmed.Equals("Name \"SHADOWCASTER\""))
                {
                    _currentPass = CurrentPass.ShadowCaster;
                }
                else if (trimmed.Equals("Name \"META\""))
                {
                    _currentPass = CurrentPass.Meta;
                }


                if (trimmed.StartsWith("$Feature_GrabPass"))
                {
                    if (settings.grabPass && !isAndroid)
                    {
                        shaderData.definesSb.AppendLine("#define REQUIRE_OPAQUE_TEXTURE");
                        template[i] = "GrabPass\n{\n Name \"GrabPass\" \n \"_CameraOpaqueTexture\" \n}";
                    }
                    else
                    {
                        template[i] = string.Empty;
                    }
                    
                }

                if (trimmed.StartsWith("$Feature_a2c"))
                {
                    if (settings.alphaToCoverage)
                    {
                        shaderData.definesSb.AppendLine("#define ALPHATOCOVERAGE_ON");
                        template[i] = "AlphaToMask [_AlphaToMask]";
                    }
                    else
                    {
                        template[i] = string.Empty;
                    }
                }

                if (trimmed.StartsWith("$DefaultPropertiesInclude"))
                {
                    template[i] = defaultProps.ToString();
                }

                else if (trimmed.StartsWith("$PropertiesInclude"))
                {
                    template[i] = shaderData.propertiesSb.ToString();
                }

                else if (trimmed.StartsWith("$Defines"))
                {
                    template[i] = shaderData.definesSb.ToString();
                    if (_currentPass == CurrentPass.ForwardBase)
                    {
                        template[i] += '\n' + shaderData.definesForwardBaseSb.ToString();
                    }
                    else if (_currentPass == CurrentPass.ForwardAdd)
                    {
                        template[i] += '\n' + shaderData.definesForwardAddSb.ToString();
                    }
                    else if (_currentPass == CurrentPass.ShadowCaster)
                    {
                        template[i] += '\n' + shaderData.definesShadowcasterSb.ToString();
                    }
                    else if (_currentPass == CurrentPass.Meta)
                    {
                        template[i] += '\n' + shaderData.definesMetaSb.ToString();
                    }
                }

                else if (trimmed.StartsWith("$ShaderDescription"))
                {
                    template[i] = shaderData.codeSb.ToString();
                }
                
                else if (trimmed.StartsWith("$Cbuffer"))
                {
                    template[i] = shaderData.cbufferSb.ToString();
                }
                
                else if (trimmed.StartsWith("$Feature_MonoSH"))
                {
                    template[i] = GetDefineTypeDeclaration(settings.bakeryMonoSH, ShaderSettings.MonoShKeyword);
                }
                
                else if (trimmed.StartsWith("$Feature_BicubicLightmap"))
                {
                    template[i] = GetDefineTypeDeclaration(settings.bicubicLightmap, ShaderSettings.BicubicLightmapKeyword);
                }
                else if (trimmed.StartsWith("$Feature_GSAA"))
                {
                    template[i] = GetDefineTypeDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword);
                }
                else if (trimmed.StartsWith("$Feature_Anisotropy"))
                {
                    template[i] = GetDefineTypeDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword);
                }
                else if (trimmed.StartsWith("$Feature_LightmappedSpecular"))
                {
                    template[i] = GetDefineTypeDeclaration(settings.lightmappedSpecular, ShaderSettings.LightmappedSpecular);
                }

                else if (trimmed.Equals("$ShaderEditor"))
                {
                    if (string.IsNullOrEmpty(settings.customEditorGUI))
                    {
                        if (!string.IsNullOrEmpty(defaultShaderEditor))
                        {
                            template[i] = $"CustomEditor \"{defaultShaderEditor}\"";
                        }
                        else
                        {
                            template[i] = string.Empty;
                        }
                    }
                    else
                    {
                        template[i] = $"CustomEditor \"{settings.customEditorGUI}\"";
                    }
                }
            }

            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrEmpty(settings.shaderName))
            {
                template[0] = $"Shader \"Lit Variants/{fileName}\"";
            }
            else
            {
                template[0] = $"Shader \"{settings.shaderName}\"";
            }

            return string.Join(Environment.NewLine, template);
        }

        private static void Parse(IEnumerable<string> fileLines, ShaderData shaderData)
        {
            var ienum = fileLines.GetEnumerator();
            while (ienum.MoveNext())
            {
                var variantSpan = ienum.Current.AsSpan();
                var trimmed = variantSpan.TrimStart();

                if (trimmed.StartsWith("//".AsSpan()))
                {
                    continue;
                }

                if (trimmed.StartsWith("PROPERTIES_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.propertiesSb, "PROPERTIES_END".AsSpan());
                }

                else if (trimmed.StartsWith("DEFINES_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.definesSb, "DEFINES_END".AsSpan());
                }

                else if (trimmed.StartsWith("DEFINES_FORWARDBASE_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.definesForwardBaseSb, "DEFINES_FORWARDBASE_END".AsSpan());
                }

                else if (trimmed.StartsWith("DEFINES_FORWARDADD_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.definesForwardAddSb, "DEFINES_FORWARDADD_END".AsSpan());
                }

                else if (trimmed.StartsWith("DEFINES_SHADOWCASTER_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.definesShadowcasterSb, "DEFINES_SHADOWCASTER_END".AsSpan());
                }

                else if (trimmed.StartsWith("DEFINES_META_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.definesMetaSb, "DEFINES_META_END".AsSpan());
                }


                else if (trimmed.StartsWith("CODE_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.codeSb, "CODE_END".AsSpan());
                }

                else if (trimmed.StartsWith("CBUFFER_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.cbufferSb, "CBUFFER_END".AsSpan());
                }

                else if (trimmed.StartsWith("#include ".AsSpan()))
                {
                    var includeFile = trimmed.Slice(9).TrimEnd('"').TrimStart('"');
                    string includePath;
                    if (includeFile.StartsWith("Assets/".AsSpan()) || includeFile.StartsWith("Packages/".AsSpan()))
                    {
                        includePath = includeFile.ToString();
                    }
                    else
                    {
                        includePath = lastFolderPath + "/" + includeFile.ToString();
                    }

                    if (includeFile.EndsWith(".litshader".AsSpan(), StringComparison.Ordinal))
                    {
                        var includeFileLines = File.ReadLines(includePath);
                        _sourceDependencies.Add(includePath);
                        Parse(includeFileLines, shaderData);
                    }

                }
            }
            ienum.Dispose();
        }

        private static void AppendLineBlockSpan(IEnumerator<string> ienum, StringBuilder sb, ReadOnlySpan<char> breakName)
        {
            while (ienum.MoveNext())
            {
                var line = ienum.Current.AsSpan();
                var trimmed = line.TrimStart();

                if (trimmed.IsEmpty)
                {
                    continue;
                }

                if (trimmed.StartsWith("//".AsSpan()))
                {
                    continue;
                }

                if (trimmed.StartsWith(breakName))
                {
                    break;
                }

                sb.AppendLine(ienum.Current);
            }
        }
        
        private static string GetDefineTypeDeclaration(ShaderSettings.DefineType defineType, string keyword)
        {
            switch (defineType)
            {
                case ShaderSettings.DefineType.Disabled:
                    return string.Empty;
                case ShaderSettings.DefineType.Enabled:
                    return $"#define {keyword}";
                case ShaderSettings.DefineType.LocalKeyword:
                    return $"#pragma shader_feature_local {keyword}";
                case ShaderSettings.DefineType.GlobalKeyword:
                    return $"#pragma shader_feature {keyword}";
            }
            
            return string.Empty;
        }
        
        internal static string GetPropertyDeclaration(ShaderSettings.DefineType defineType, string keyword, string displayName, bool toggleOff = false)
        {
            if (defineType == ShaderSettings.DefineType.LocalKeyword || defineType == ShaderSettings.DefineType.GlobalKeyword)
            {
                string value = toggleOff ? "1" : "0";
                string off = toggleOff ? "Off" : string.Empty;
                return $"[Toggle{off}({keyword})]_{keyword}(\"{displayName}\", Float) = {value}";
            }
            
            return string.Empty;
        }

        public static Texture2D DFGLut()
        {
            const string path = "Packages/com.z3y.shaders/ShaderLibrary/dfg-multiscatter.exr"; //TODO: replace the package path with const string
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static Texture2D BlueNoise()
        {
            const string path = "Packages/com.z3y.shaders/ShaderLibrary/LDR_LLL1_0.png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        
    }
}
