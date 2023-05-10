﻿using System;
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

        private const string LTCGIIncludePath = "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc";
        private static bool _ltcgiIncluded = File.Exists(LTCGIIncludePath);


        [SerializeField] public ShaderSettings settings;

        private static readonly List<string> SourceDependencies = new List<string>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
            if (oldShader != null) ShaderUtil.ClearShaderMessages(oldShader);

            SourceDependencies.Clear();

            var code = GetShaderLabCode(settings, ctx.assetPath, ctx.selectedBuildTarget);
            var shader = ShaderUtil.CreateShaderAsset(code, false);

            EditorMaterialUtility.SetShaderNonModifiableDefaults(shader, new[] { "_DFG", "BlueNoise" }, new Texture[] { DFGLut(), BlueNoise() });
            
            ctx.DependsOnSourceAsset("Assets/com.z3y.shaders/Editor/Importer/LitImporter.cs");

            foreach (var dependency in SourceDependencies)
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
        internal static string GetShaderLabCode(ShaderSettings settings, string assetPath, BuildTarget buildTarget)
        {
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
                        if (_ltcgiIncluded) sb.AppendLine("\"LTCGI\" = \"_LTCGI\"");
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

                            if (!isAndroid && _ltcgiIncluded)
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

                        sb.AppendLine("// DEFINES_START");
                        sb.AppendLine(definesSbString);
                        sb.AppendLine("// DEFINES_END");
                        
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
                            if (!isAndroid)
                            {
                                sb.AppendLine(GetDefineTypeDeclaration(settings.bicubicLightmap, ShaderSettings.BicubicLightmapKeyword));
                                sb.AppendLine(GetDefineTypeDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword));          
                            }

                            sb.AppendLine(GetDefineTypeDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword));

                            sb.AppendLine("#pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");

                            sb.AppendLine("// DEFINES_START");
                            sb.AppendLine(definesSbString);
                            sb.AppendLine("// DEFINES_END");

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
                            if (!isAndroid)
                            {
                                sb.AppendLine(GetDefineTypeDeclaration(settings.bicubicLightmap, ShaderSettings.BicubicLightmapKeyword));
                                sb.AppendLine(GetDefineTypeDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword));          
                            }

                            sb.AppendLine(GetDefineTypeDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword));

                            sb.AppendLine("#pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");

                            sb.AppendLine("// DEFINES_START");
                            sb.AppendLine(definesSbString);
                            sb.AppendLine("// DEFINES_END");

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
                return prefix + settings.shaderName;
            }
        }

        private static string GetShaderInspectorLine(ShaderSettings settings)
        {
            string name;
            if (string.IsNullOrEmpty(settings.customEditorGUI))
            {
                if (!string.IsNullOrEmpty(defaultShaderEditor))
                {
                    name = $"CustomEditor \"{defaultShaderEditor}\"";
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
        
        internal static string ProcessFileLinesOld(ShaderSettings settings, string assetPath, BuildTarget buildTarget)
        {
            if (settings is null)
            {
                settings = new ShaderSettings();
            }


            bool isAndroid = buildTarget == BuildTarget.Android;
            lastFolderPath = Path.GetDirectoryName(assetPath);
            var fileLines = File.ReadLines(assetPath);

            string defaultProps = GetDefaultPropertiesInclude(settings, isAndroid);

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

            var shaderData = new ShaderBlocks();


            GetShaderBlocksRecursive(fileLines, shaderData);
            AppendAdditionalDataToBlocks(isAndroid, shaderData);
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
                    _currentPass = CurrentPass.ForwardAdd;
                }
                else if (trimmed.Equals("Name \"SHADOWCASTER\""))
                {
                    _currentPass = CurrentPass.ShadowCaster;
                }
                else if (trimmed.Equals("Name \"META_BAKERY\""))
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
                    template[i] = defaultProps;
                }

                else if (trimmed.StartsWith("$Feature_RenderQueue"))
                {
                    template[i] = settings.grabPass ? "\"Queue\"=\"Transparent+100\"" : "\"Queue\"=\"Geometry+0\"";
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

            if (isAndroid || !_ltcgiIncluded)
            {
                shaderData.definesSb.AppendLine("#pragma skip_variants LTCGI");
                shaderData.definesSb.AppendLine("#pragma skip_variants LTCGI_DIFFUSE_OFF");
            }
            else if (_ltcgiIncluded)
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

            if (!isAndroid && _ltcgiIncluded)
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
                        includePath = lastFolderPath + "/" + includeFile.ToString();
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
        public static Texture2D BlueNoise()
        {
            const string path = "Packages/com.z3y.shaders/ShaderLibrary/LDR_LLL1_0.png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        
    }
}
