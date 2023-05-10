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
    [ScriptedImporter(1, Ext, 0)]
    public class LitImporter : ScriptedImporter
    {
        public const string Ext = "litshader";
        private const string DefaultShaderPath = "Packages/com.z3y.shaders/Shaders/Default.litshader";
        private const string LtcgiIncludePath = "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc";
        private static readonly bool LtcgiIncluded = File.Exists(LtcgiIncludePath);
        private const string DefaultShaderEditor = "z3y.Shaders.DefaultInspector";
        
        [SerializeField] public ShaderSettings settings = new ShaderSettings();

        private static readonly List<string> SourceDependencies = new List<string>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
            if (oldShader != null) ShaderUtil.ClearShaderMessages(oldShader);

            SourceDependencies.Clear();

            var code = GetShaderLabCode(settings, ctx.assetPath, ctx.selectedBuildTarget);
            var shader = ShaderUtil.CreateShaderAsset(code, false);

            EditorMaterialUtility.SetShaderNonModifiableDefaults(shader, new[] { "_DFG" }, new Texture[] { DFGLut()});
            
            ctx.DependsOnSourceAsset("Assets/com.z3y.shaders/Editor/Importer/LitImporter.cs");

            foreach (var dependency in SourceDependencies)
            {
                ctx.DependsOnSourceAsset(dependency);
            }

            if (LtcgiIncluded)
            {
                ctx.DependsOnSourceAsset(LtcgiIncludePath);
            }


            ctx.AddObjectToAsset("MainAsset", shader);
            ctx.SetMainObject(shader);
        }

        [MenuItem("Assets/Create/Shader/Lit Shader Variant")]
        public static void CreateVariantFile()
        {
            var defaultContent = File.ReadAllText(DefaultShaderPath);
            ProjectWindowUtil.CreateAssetWithContent($"Lit Shader Variant.{Ext}", defaultContent);
        }

        private class ShaderBlocks
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
        
        private static string _lastFolderPath = string.Empty;
        internal static string GetShaderLabCode(ShaderSettings settings, string assetPath, BuildTarget buildTarget)
        {
            _lastFolderPath = Path.GetDirectoryName(assetPath);
            var shaderBlocks = new ShaderBlocks();
            var fileLines = File.ReadLines(assetPath);
            GetShaderBlocksRecursive(fileLines, shaderBlocks);
            string definesSbString = shaderBlocks.definesSb.ToString();
            string codeSbSbString = shaderBlocks.codeSb.ToString();
            string cbufferSbSbString = shaderBlocks.cbufferSb.ToString();

            bool isAndroid = buildTarget == BuildTarget.Android;
            AppendAdditionalDataToBlocks(isAndroid, shaderBlocks);

            var sb = new StringBuilder();

            sb.AppendLine($"Shader \"{GetShaderName(settings, assetPath)}\" ");
            sb.AppendLine("{");
            {
                sb.AppendLine("Properties");
                sb.AppendLine("{");
                {
                    sb.AppendLine("[HideInInspector]__LitShaderVariant(\"\", Float) = 0");
                    sb.AppendLine(GetDefaultPropertiesInclude(settings, isAndroid));
                    sb.AppendLine(shaderBlocks.propertiesSb.ToString());
                }
                sb.AppendLine("}");

                sb.AppendLine("SubShader");
                sb.AppendLine("{");
                {
                    sb.AppendLine("Tags");
                    sb.AppendLine("{");
                    {
                        //sb.AppendLine("\"RenderPipeline\"=\"\""); // Always built-in
                        sb.AppendLine("\"RenderType\"=\"Opaque\"");
                        sb.AppendLine(settings.grabPass ? "\"Queue\"=\"Transparent+100\"" : "\"Queue\"=\"Geometry+0\"");
                        if (LtcgiIncluded) sb.AppendLine("\"LTCGI\" = \"_LTCGI\"");
                    }
                    sb.AppendLine("}");
                    
                    if (settings.grabPass && !isAndroid)
                    {
                        sb.AppendLine("GrabPass\n{\n Name \"GrabPass\" \n \"_CameraOpaqueTexture\" \n}");
                    }
                    
                    sb.AppendLine("Pass"); // FwdBase
                    sb.AppendLine("{");
                    {
                        sb.AppendLine("Name \"FORWARDBASE\"");
                        
                        sb.AppendLine("Tags { \"LightMode\" = \"ForwardBase\"}");

                        sb.AppendLine("Blend [_SrcBlend] [_DstBlend]");
                        sb.AppendLine("Cull [_Cull]");
                        sb.AppendLine("// ZTest: <None>");
                        sb.AppendLine("ZWrite [_ZWrite]");
                        if (settings.alphaToCoverage) sb.AppendLine("AlphaToMask [_AlphaToMask]");

                        sb.AppendLine("HLSLPROGRAM");
                        sb.AppendLine("#define PIPELINE_BUILTIN");
                        sb.AppendLine("#define GENERATION_CODE");
                        sb.AppendLine("#pragma vertex vert");
                        sb.AppendLine("#pragma fragment frag");

                        sb.AppendLine("#pragma target 4.5");
                        sb.AppendLine("#pragma multi_compile_fog");
                        sb.AppendLine("#pragma multi_compile_instancing");
                        if (settings.materialType == ShaderSettings.MaterialType.Lit)
                        {
                            sb.AppendLine("#pragma multi_compile_fwdbase");
                            sb.AppendLine("#pragma multi_compile _ VERTEXLIGHT_ON");
                            sb.AppendLine("#pragma skip_variants LIGHTPROBE_SH");
                            sb.AppendLine("#pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF");
                            sb.AppendLine("#pragma shader_feature_local _GLOSSYREFLECTIONS_OFF");
                            sb.AppendLine("#pragma shader_feature_local _EMISSION");

                            if (settings.grabPass && !isAndroid)
                            {
                                sb.AppendLine("#define REQUIRE_OPAQUE_TEXTURE");
                            }

                            if (!isAndroid && LtcgiIncluded)
                            {
                                sb.AppendLine("#pragma shader_feature_local_fragment LTCGI");
                                sb.AppendLine("#pragma shader_feature_local_fragment LTCGI_DIFFUSE_OFF");
                            }

                            sb.AppendLine(GetDefineTypeDeclaration(settings.bakeryMonoSH, ShaderSettings.MonoShKeyword));
                            if (!isAndroid) sb.AppendLine(GetDefineTypeDeclaration(settings.bicubicLightmap, ShaderSettings.BicubicLightmapKeyword));
                            if (!isAndroid) sb.AppendLine(GetDefineTypeDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword));
                            sb.AppendLine(GetDefineTypeDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword));
                            sb.AppendLine(GetDefineTypeDeclaration(settings.lightmappedSpecular, ShaderSettings.LightmappedSpecular));
                        }
                        sb.AppendLine("#pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");
                        
                        if (settings.alphaToCoverage)
                        {
                            sb.AppendLine("#define ALPHATOCOVERAGE_ON");
                        }
                        
                        sb.AppendLine("// DEFINES_START");
                        sb.AppendLine(definesSbString);
                        sb.AppendLine("// DEFINES_END");

                        sb.AppendLine("// DEFINES_FORWARDBASE_START");
                        sb.AppendLine(shaderBlocks.definesForwardBaseSb.ToString());
                        sb.AppendLine("// DEFINES_FORWARDBASE_END");

                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/ShaderPass.hlsl\"");
                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Structs.hlsl\"");
                        
                        sb.AppendLine("// CBUFFER_START");
                        sb.AppendLine(cbufferSbSbString);
                        sb.AppendLine("// CBUFFER_END");
                        
                        sb.AppendLine("// CODE_START");
                        sb.AppendLine(codeSbSbString);
                        sb.AppendLine("// CODE_END");

                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Vertex.hlsl\"");
                        sb.AppendLine(settings.materialType == ShaderSettings.MaterialType.Lit
                            ? "#include \"Packages/com.z3y.shaders/ShaderLibrary/Fragment.hlsl\""
                            : "#include \"Packages/com.z3y.shaders/ShaderLibrary/FragmentUnlit.hlsl\"");
                        sb.AppendLine("ENDHLSL");
                    }
                    sb.AppendLine("}");

                    if (settings.materialType == ShaderSettings.MaterialType.Lit)
                    {
                        sb.AppendLine("Pass"); // FwdAdd
                        sb.AppendLine("{");
                        {
                            sb.AppendLine("Name \"FORWARD_DELTA\"");
                            sb.AppendLine("Tags { \"LightMode\" = \"ForwardAdd\"}");
                            sb.AppendLine("Fog { Color (0,0,0,0) }");
                            sb.AppendLine("Blend [_SrcBlend] One");
                            sb.AppendLine("Cull [_Cull]");
                            sb.AppendLine("// ZTest: <None>");
                            sb.AppendLine("ZWrite Off");
                            sb.AppendLine("ZTest LEqual");
                            if (settings.alphaToCoverage) sb.AppendLine("AlphaToMask [_AlphaToMask]");

                            sb.AppendLine("HLSLPROGRAM");
                            sb.AppendLine("#define PIPELINE_BUILTIN");
                            sb.AppendLine("#define GENERATION_CODE");
                            sb.AppendLine("#pragma vertex vert");
                            sb.AppendLine("#pragma fragment frag");

                            sb.AppendLine("#pragma target 4.5");
                            sb.AppendLine("#pragma multi_compile_fog");
                            sb.AppendLine("#pragma multi_compile_instancing");
                            sb.AppendLine("#pragma multi_compile_fwdadd_fullshadows");

                            sb.AppendLine("#pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF");
                            if (!isAndroid)
                            {
                                sb.AppendLine(GetDefineTypeDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword));
                            }

                            sb.AppendLine(GetDefineTypeDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword));

                            sb.AppendLine("#pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");

                            if (settings.alphaToCoverage)
                            {
                                sb.AppendLine("#define ALPHATOCOVERAGE_ON");
                            }
                            
                            sb.AppendLine("// DEFINES_START");
                            sb.AppendLine(definesSbString);
                            sb.AppendLine("// DEFINES_END");
                            
                            sb.AppendLine("// DEFINES_FORWARDADD_START");
                            sb.AppendLine(shaderBlocks.definesForwardAddSb.ToString());
                            sb.AppendLine("// DEFINES_FORWARDADD_END");

                            sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/ShaderPass.hlsl\"");
                            sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Structs.hlsl\"");

                            sb.AppendLine("// CBUFFER_START");
                            sb.AppendLine(cbufferSbSbString);
                            sb.AppendLine("// CBUFFER_END");

                            sb.AppendLine("// CODE_START");
                            sb.AppendLine(codeSbSbString);
                            sb.AppendLine("// CODE_END");


                            sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Vertex.hlsl\"");
                            sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Fragment.hlsl\"");
                            sb.AppendLine("ENDHLSL");
                        }
                        sb.AppendLine("}");
                    }
                    
                    sb.AppendLine("Pass"); // ShadowCaster
                    sb.AppendLine("{");
                    {
                        sb.AppendLine("Name \"SHADOWCASTER\"");
                        sb.AppendLine("Tags { \"LightMode\" = \"ShadowCaster\"}");
                        sb.AppendLine("ZWrite On");
                        sb.AppendLine("Cull [_Cull]");
                        sb.AppendLine("ZTest LEqual");
                        
                        sb.AppendLine("HLSLPROGRAM");
                        sb.AppendLine("#define PIPELINE_BUILTIN");
                        sb.AppendLine("#define GENERATION_CODE");
                        sb.AppendLine("#pragma vertex vert");
                        sb.AppendLine("#pragma fragment frag");

                        sb.AppendLine("#pragma target 4.5");
                        sb.AppendLine("#pragma multi_compile_fog");
                        sb.AppendLine("#pragma multi_compile_instancing");
                        sb.AppendLine("#pragma multi_compile_shadowcaster");
                        
                        sb.AppendLine("#pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAFADE_ON");

                        sb.AppendLine("// DEFINES_START");
                        sb.AppendLine(definesSbString);
                        sb.AppendLine("// DEFINES_END");
                        
                        sb.AppendLine("// DEFINES_SHADOWCASTER_START");
                        sb.AppendLine(shaderBlocks.definesShadowcasterSb.ToString());
                        sb.AppendLine("// DEFINES_SHADOWCASTER_END");

                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/ShaderPass.hlsl\"");
                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Structs.hlsl\"");

                        sb.AppendLine("// CBUFFER_START");
                        sb.AppendLine(cbufferSbSbString);
                        sb.AppendLine("// CBUFFER_END");

                        sb.AppendLine("// CODE_START");
                        sb.AppendLine(codeSbSbString);
                        sb.AppendLine("// CODE_END");


                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Vertex.hlsl\"");
                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/FragmentShadowCaster.hlsl\"");
                        sb.AppendLine("ENDHLSL");
                    }
                    sb.AppendLine("}");
                    
                    
                    sb.AppendLine("Pass"); // Meta
                    sb.AppendLine("{");
                    {
                        sb.AppendLine("Name \"META_BAKERY\"");
                        sb.AppendLine("Tags { \"LightMode\" = \"Meta\"}");
                        sb.AppendLine("Cull Off");

                        sb.AppendLine("HLSLPROGRAM");
                        sb.AppendLine("#define PIPELINE_BUILTIN");
                        sb.AppendLine("#define GENERATION_CODE");
                        sb.AppendLine("#pragma vertex vert");
                        sb.AppendLine("#pragma fragment frag");

                        sb.AppendLine("#pragma target 4.5");
                        sb.AppendLine("#pragma shader_feature EDITOR_VISUALIZATION");
                        
                        sb.AppendLine("#pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");
                        sb.AppendLine("#pragma shader_feature_local _EMISSION");

                        sb.AppendLine("// DEFINES_START");
                        sb.AppendLine(definesSbString);
                        sb.AppendLine("// DEFINES_END");

                        sb.AppendLine("// DEFINES_META_START");
                        sb.AppendLine(shaderBlocks.definesMetaSb.ToString());
                        sb.AppendLine("// DEFINES_META_END");
                        
                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/ShaderPass.hlsl\"");
                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Structs.hlsl\"");

                        sb.AppendLine("// CBUFFER_START");
                        sb.AppendLine(cbufferSbSbString);
                        sb.AppendLine("// CBUFFER_END");

                        sb.AppendLine("// CODE_START");
                        sb.AppendLine(codeSbSbString);
                        sb.AppendLine("// CODE_END");


                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/Vertex.hlsl\"");
                        sb.AppendLine("#include \"Packages/com.z3y.shaders/ShaderLibrary/FragmentMeta.hlsl\"");
                        sb.AppendLine("ENDHLSL");
                    }
                    sb.AppendLine("}");
                }
                sb.AppendLine("}");
                
                sb.AppendLine(GetShaderInspectorLine(settings));
                // sb.AppendLine("Fallback");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GetShaderName(ShaderSettings settings, string assetPath)
        {
            const string prefix = "Lit Variants/";
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrEmpty(settings.shaderName))
            {
                return prefix + fileName;
            }
            else
            {
                return settings.shaderName;
            }
        }

        private static string GetShaderInspectorLine(ShaderSettings settings)
        {
            string name;
            if (string.IsNullOrEmpty(settings.customEditorGUI))
            {
                if (!string.IsNullOrEmpty(DefaultShaderEditor))
                {
                    name = $"CustomEditor \"{DefaultShaderEditor}\"";
                }
                else
                {
                    name = string.Empty;
                }
            }
            else
            {
                name = $"CustomEditor \"{settings.customEditorGUI}\"";
            }

            return name;
        }
        
        private static void AppendAdditionalDataToBlocks(bool isAndroid, ShaderBlocks shaderData)
        {
#if VRCHAT_SDK
            shaderData.definesSb.AppendLine("#define VRCHAT_SDK");
#endif

            if (isAndroid)
            {
                shaderData.definesSb.AppendLine("#define BUILD_TARGET_ANDROID");
                shaderData.definesSb.AppendLine("#pragma skip_variants " + ShaderSettings.GsaaKeyword);
                shaderData.definesSb.AppendLine("#pragma skip_variants " + ShaderSettings.BicubicLightmapKeyword);
                shaderData.definesSb.AppendLine("#pragma skip_variants SHADOWS_DEPTH");
                shaderData.definesSb.AppendLine("#pragma skip_variants SHADOWS_SCREEN");
                shaderData.definesSb.AppendLine("#pragma skip_variants SHADOWS_CUBE");
                shaderData.definesSb.AppendLine("#pragma skip_variants SHADOWS_SOFT");

                shaderData.definesSb.AppendLine("#pragma skip_variants DIRECTIONAL_COOKIE");
                shaderData.definesSb.AppendLine("#pragma skip_variants SPOT_COOKIE");
                shaderData.definesSb.AppendLine("#pragma skip_variants POINT_COOKIE");
                shaderData.definesSb.AppendLine("#pragma skip_variants DYNAMICLIGHTMAP_ON");

            }
            else
            {
                shaderData.definesSb.AppendLine("#define BUILD_TARGET_PC");
            }

            if (isAndroid || !LtcgiIncluded)
            {
                shaderData.definesSb.AppendLine("#pragma skip_variants LTCGI");
                shaderData.definesSb.AppendLine("#pragma skip_variants LTCGI_DIFFUSE_OFF");
            }
            else if (LtcgiIncluded)
            {
                shaderData.definesSb.AppendLine("#define LTCGI_EXISTS");
            }

#if BAKERY_INCLUDED
            shaderData.definesSb.AppendLine("#define BAKERY_INCLUDED");
#endif
        }

        private static string GetDefaultPropertiesInclude(ShaderSettings settings, bool isAndroid)
        {
            var defaultProps = new StringBuilder();
            defaultProps.AppendLine(GetPropertyDeclaration(settings.bakeryMonoSH, ShaderSettings.MonoShKeyword, "Mono SH"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.bicubicLightmap, ShaderSettings.BicubicLightmapKeyword, "Bicubic Lightmap"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword, "Geometric Specular AA"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword, "Anisotropy"));
            defaultProps.AppendLine(GetPropertyDeclaration(settings.lightmappedSpecular, ShaderSettings.LightmappedSpecular, "Lightmapped Specular"));

            if (!isAndroid && LtcgiIncluded)
            {
                defaultProps.AppendLine(GetPropertyDeclaration(ShaderSettings.DefineType.LocalKeyword, "LTCGI", "Enable LTCGI"));
                defaultProps.AppendLine(GetPropertyDeclaration(ShaderSettings.DefineType.LocalKeyword, "LTCGI_DIFFUSE_OFF", "Disable LTCGI Diffuse"));
            }

            if (settings.grabPass) defaultProps.Append("[HideInInspector][ToggleUI]_GrabPass(\"GrabPass\", Float) = 1"); // just a property to detect if there is a grabpass

            defaultProps.AppendLine(LitImporterConstants.DefaultPropertiesInclude);
            return defaultProps.ToString();
        }

        private static void GetShaderBlocksRecursive(IEnumerable<string> fileLines, ShaderBlocks shaderData)
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
                        includePath = _lastFolderPath + "/" + includeFile.ToString();
                    }

                    if (includeFile.EndsWith(".litshader".AsSpan(), StringComparison.Ordinal))
                    {
                        var includeFileLines = File.ReadLines(includePath);
                        SourceDependencies.Add(includePath);
                        GetShaderBlocksRecursive(includeFileLines, shaderData);
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
                string propName = keyword.StartsWith("_", StringComparison.Ordinal) ? keyword :  "_" + keyword;
                return $"[Toggle{off}({keyword})]{propName}(\"{displayName}\", Float) = {value}";
            }
            
            return "// Keyword Disabled " + keyword;
        }

        public static Texture2D DFGLut()
        {
            const string path = "Packages/com.z3y.shaders/ShaderLibrary/dfg-multiscatter.exr"; //TODO: replace the package path with const string
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

    }
}
