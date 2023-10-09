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

namespace z3y.Shaders
{
    [ScriptedImporter(5, Ext, 0)]
    public class LitImporter : ScriptedImporter
    {
        public const string Ext = "litshader";
        private const string DefaultShaderPath = "Packages/com.z3y.shaders/Shaders/Default.litshader";
        private const string LtcgiIncludePath = "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc";
        private static bool LtcgiIncluded => File.Exists(LtcgiIncludePath);
        private const string DefaultShaderEditor = "z3y.Shaders.DefaultInspector";

        private static Texture2D Thumbnail => AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.z3y.shaders/Editor/lit.png");

        private const string AreaLitIncludePath = "Assets/AreaLit/Shader/Lighting.hlsl";
        private static bool AreaLitIncluded => File.Exists(AreaLitIncludePath);

        [SerializeField] public ShaderSettings settings = new ShaderSettings();
        
        private readonly HashSet<string> _sourceDependencies = new HashSet<string>();

        private static string _lastImportedShader = string.Empty;
        private static bool _requestGeneratedShader;
        public static string RequestGeneratedShader(string path)
        {
            _requestGeneratedShader = true;
            
            AssetDatabase.ImportAsset(path);
            
            _requestGeneratedShader = false;
            var code = _lastImportedShader;
            _lastImportedShader = string.Empty;

            return code;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
            if (oldShader != null) ShaderUtil.ClearShaderMessages(oldShader);

            _sourceDependencies.Clear();

            if (ctx.assetPath.EndsWith("LitShaderConfig." + Ext))
            {
                var text = new TextAsset(File.ReadAllText(ctx.assetPath));
                ctx.AddObjectToAsset("MainAsset", text);
                return;
            }

            var code = GetShaderLabCode(ctx);
            
            if (_requestGeneratedShader)
            {
                _lastImportedShader = code;
            }
            
            var shader = ShaderUtil.CreateShaderAsset(code, false);

            SetDefaultTextures(shader);

            ctx.DependsOnSourceAsset("Assets/com.z3y.shaders/Editor/Importer/LitImporter.cs");

            // this will make it reimport even if the file didnt exist
            ctx.DependsOnSourceAsset(LtcgiIncludePath);
            ctx.DependsOnSourceAsset(AreaLitIncludePath);

            foreach (var dependency in _sourceDependencies)
            {
                ctx.DependsOnSourceAsset(dependency);
            }

            ctx.AddObjectToAsset("MainAsset", shader, Thumbnail);
            ctx.SetMainObject(shader);
        }

        private static void SetDefaultTextures(Shader shader)
        {
            EditorMaterialUtility.SetShaderNonModifiableDefaults(shader, new[] { "_DFG", "BlueNoise" }, new Texture[] { DfgLut(), BlueNoise() });
        }

        [MenuItem("Assets/Create/Shader/Lit Shader Variant")]
        public static void CreateVariantFile()
        {
            var defaultContent = File.ReadAllText(DefaultShaderPath);
            ProjectWindowUtil.CreateAssetWithContent($"Lit Shader Variant.{Ext}", defaultContent);
        }

        [MenuItem("Tools/Lit/Create Config File")]
        public static void CreateConfigFile()
        {
            const string folder = "Assets/Settings/";
            const string fileName = "LitShaderConfig.litshader";
            const string fullPath = folder + fileName;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (File.Exists(fullPath))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath));
                return;
            }

            using (StreamWriter sw = File.CreateText(fullPath))
            {
                sw.WriteLine(LitImporterConstants.DefaultConfigFile);
            }

            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath));
        }
        [MenuItem("Tools/Lit/Reimport Shaders")]
        public static void ReimportShaders()
        {
            var guids = AssetDatabase.FindAssets("t:shader");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.EndsWith("." + Ext))
                {
                    continue;
                }

                AssetDatabase.ImportAsset(path);
            }
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
            public StringBuilder dependenciesSb = new StringBuilder();
        }

        private class EnumeratorWrapper : IDisposable
        {
            private IEnumerator<string> Enumerator { get; set; }
            public string FileName { get; private set; }
            public string FilePath { get; private set; }
            public int Index { get; private set; }

            public EnumeratorWrapper(IEnumerable<string> lines, string fileName, string filePath)
            {
                Enumerator = lines.GetEnumerator();
                FileName = fileName;
                Index = -1;
                FilePath = filePath;
            }

            ~EnumeratorWrapper()
            {
                if (Enumerator != null)
                {
                    Enumerator.Dispose();
                }
            }
            
            public bool MoveNext()
            {
                Index++;
                return Enumerator.MoveNext();
            }
            public string Current => Enumerator.Current;

            public void Reset()
            {
                Enumerator.Reset();
                Index = -1;
            }

            public void Dispose()
            {
                Enumerator.Dispose();
                Enumerator = null;
            }
        }
        
        private string _lastFolderPath = string.Empty;

        private string GetShaderLabCode(AssetImportContext ctx)
        {
            _lastFolderPath = Path.GetDirectoryName(assetPath);
            var shaderBlocks = new ShaderBlocks();
            var fileLines = File.ReadLines(assetPath);
            string fileName = Path.GetFileName(assetPath);
            var enumeratorWrapper = new EnumeratorWrapper(fileLines, fileName, assetPath);
            GetShaderBlocksRecursive(enumeratorWrapper, shaderBlocks, assetPath);


            bool isAndroid = ctx.selectedBuildTarget == BuildTarget.Android;
            bool ltcgiAllowed = !isAndroid && LtcgiIncluded;
            bool areaLitAllowed = !isAndroid && AreaLitIncluded;

            AppendAdditionalDataToBlocks(isAndroid, shaderBlocks);

            _sourceDependencies.Add("Packages/com.z3y.shaders/ShaderLibrary/ShaderPass.hlsl");
            _sourceDependencies.Add("Packages/com.z3y.shaders/ShaderLibrary/Vertex.hlsl");
            _sourceDependencies.Add("Packages/com.z3y.shaders/ShaderLibrary/Fragment.hlsl");
            _sourceDependencies.Add("Packages/com.z3y.shaders/ShaderLibrary/FragmentShadowCaster.hlsl");
            _sourceDependencies.Add("Packages/com.z3y.shaders/ShaderLibrary/FragmentMeta.hlsl");
            _sourceDependencies.Add("Packages/com.z3y.shaders/ShaderLibrary/Structs.hlsl");
            _sourceDependencies.Add("Packages/com.z3y.shaders/ShaderLibrary/ForwardLighting.hlsl");

            string definesSbString = shaderBlocks.definesSb.ToString();
            string codeSbSbString = shaderBlocks.codeSb.ToString();
            string cbufferSbSbString = shaderBlocks.cbufferSb.ToString();

            var shaderName = GetShaderName();
            bool isTerrain = shaderName.Contains("Terrain"); // overlooked while making the importer
            bool isTerrainAdd = shaderName.Contains("Terrain-Add");

            var sb = new StringBuilder();

            sb.AppendLine($"Shader \"{shaderName}\" ");
            sb.AppendLine("{");
            {
                sb.AppendLine("Properties");
                sb.AppendLine("{");
                {
                    sb.AppendLine("[HideInInspector]__LitShaderVariant(\"\", Float) = 0");
                    sb.AppendLine("FoldoutMainStart_RenderingOptions (\"Rendering Options\", Float) = 1");
                    sb.AppendLine(LitImporterConstants.DefaultPropertiesInclude);
                    sb.AppendLine("FoldoutMainEnd_RenderingOptions (\"\", Float) = 0");

                    sb.AppendLine("FoldoutMainStart_Properties (\"Properties\", Float) = 1");
                    sb.AppendLine(shaderBlocks.propertiesSb.ToString());
                    sb.AppendLine("FoldoutMainEnd_Properties (\"\", Float) = 0");

                    sb.AppendLine("FoldoutMainStart_AdditionalSettings (\"Additional Settings\", Float) = 1");
                    sb.AppendLine(GetDefaultPropertiesIncludeAfter(isAndroid));
                    sb.AppendLine(LitImporterConstants.DefaultPropertiesIncludeAfter);
                    if (areaLitAllowed) sb.AppendLine(LitImporterConstants.AreaLitProperties);
                    sb.AppendLine("FoldoutMainEnd_AdditionalSettings (\"\", Float) = 0");
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
                        if (isTerrain)
                        {
                            sb.AppendLine("\"Queue\" = \"Geometry-100\"");
                        }
                        else if (isTerrainAdd)
                        {
                            sb.AppendLine("\"Queue\" = \"Geometry-99\"");
                        }
                        else
                        {
                            sb.AppendLine(settings.grabPass ? "\"Queue\"=\"Transparent+100\"" : "\"Queue\"=\"Geometry+0\"");
                        }

                        if (ltcgiAllowed) sb.AppendLine("\"LTCGI\" = \"_LTCGI\"");
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
                        if (isTerrainAdd)
                        {
                            sb.AppendLine("Blend SrcAlpha One");
                        }
                        else
                        {
                            sb.AppendLine("Blend [_SrcBlend] [_DstBlend]");
                        }
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

                            if (ltcgiAllowed)
                            {
                                sb.AppendLine("#pragma shader_feature_local LTCGI");
                                sb.AppendLine("#pragma shader_feature_local LTCGI_DIFFUSE_OFF");
                            }
                            if (areaLitAllowed)
                            {
                                sb.AppendLine("#pragma shader_feature_local _AREALIT");
                                sb.AppendLine("#pragma shader_feature_local _OPAQUELIGHTS_OFF");
                            }

                            sb.AppendLine(GetDefineTypeDeclaration(settings.bakeryMonoSH, ShaderSettings.MonoShKeyword));
                            if (!isAndroid) sb.AppendLine(GetDefineTypeDeclaration(settings.bicubicLightmap, ShaderSettings.BicubicLightmapKeyword));
                            if (!isAndroid) sb.AppendLine(GetDefineTypeDeclaration(settings.gsaa, ShaderSettings.GsaaKeyword));
                            sb.AppendLine(GetDefineTypeDeclaration(settings.anisotropy, ShaderSettings.AnisotropyKeyword));
                            sb.AppendLine(GetDefineTypeDeclaration(settings.lightmappedSpecular, ShaderSettings.LightmappedSpecular));
                        }
                        sb.AppendLine("#pragma shader_feature_local _ _ALPHAFADE_ON _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");
                        
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

                            sb.AppendLine("#pragma shader_feature_local _ _ALPHAFADE_ON _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");

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
                        
                        sb.AppendLine("#pragma shader_feature_local _ _ALPHAFADE_ON _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");

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
                        // sb.AppendLine("Name \"META_BAKERY\"");
                        sb.AppendLine("Name \"META\"");

                        sb.AppendLine("Tags { \"LightMode\" = \"Meta\"}");
                        sb.AppendLine("Cull Off");

                        sb.AppendLine("HLSLPROGRAM");
                        sb.AppendLine("#define PIPELINE_BUILTIN");
                        sb.AppendLine("#define GENERATION_CODE");
                        sb.AppendLine("#pragma vertex vert");
                        sb.AppendLine("#pragma fragment frag");

                        sb.AppendLine("#pragma target 4.5");
                        sb.AppendLine("#pragma shader_feature EDITOR_VISUALIZATION");
                        
                        sb.AppendLine("#pragma shader_feature_local _ _ALPHAFADE_ON _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON");
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
                
                sb.AppendLine(GetShaderInspectorLine());
                sb.AppendLine("Fallback \"Mobile/Quest Lite\"");
                sb.AppendLine(shaderBlocks.dependenciesSb.ToString());
                // sb.AppendLine("Fallback");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GetShaderName()
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

        private string GetShaderInspectorLine()
        {
            string line;
            if (string.IsNullOrEmpty(settings.customEditorGUI))
            {
                if (!string.IsNullOrEmpty(DefaultShaderEditor))
                {
                    line = $"CustomEditor \"{DefaultShaderEditor}\"";
                }
                else
                {
                    line = string.Empty;
                }
            }
            else
            {
                line = $"CustomEditor \"{settings.customEditorGUI}\"";
            }

            return line;
        }
        
        private void AppendAdditionalDataToBlocks(bool isAndroid, ShaderBlocks shaderData)
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
                shaderData.definesSb.AppendLine("#pragma skip_variants _AREALIT");
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

        private string GetDefaultPropertiesIncludeAfter(bool isAndroid)
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
            return defaultProps.ToString();
        }

        private void GetShaderBlocksRecursive(EnumeratorWrapper ienum, ShaderBlocks shaderData, string currentPath)
        {
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
                    AppendLineBlockSpan(ienum, shaderData.propertiesSb, "PROPERTIES_END".AsSpan(), false);
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

                else if (trimmed.StartsWith("DEPENDENCY_START".AsSpan()))
                {
                    AppendLineBlockSpan(ienum, shaderData.dependenciesSb, "DEPENDENCY_END".AsSpan(), false);
                }

                else if (trimmed.StartsWith("#include_optional ".AsSpan()))
                {
                    var includeFile = trimmed.Slice("#include_optional ".Length).TrimEnd('"').TrimStart('"');
                    var includePath = GetFullIncludePath(includeFile);
                                        
                    if (includePath.Equals(currentPath))
                    {
                        continue;
                    }
                    
                    _sourceDependencies.Add(includePath);

                    if (!File.Exists(includePath))
                    {
                        continue;
                    }

                    if (includeFile.EndsWith(".litshader".AsSpan(), StringComparison.Ordinal))
                    {
                        if (includePath.Equals(ienum.FilePath, StringComparison.Ordinal))
                        {
                            Debug.LogError($"File {includePath} already included at line {ienum.Index} in {ienum.FileName}");
                            continue;
                        }
                        var includeFileLines = File.ReadLines(includePath);

                        string fileName = Path.GetFileName(includePath);
                        var enumeratorWrapper = new EnumeratorWrapper(includeFileLines, fileName, includePath);
                        GetShaderBlocksRecursive(enumeratorWrapper, shaderData, currentPath);
                    }
                }

                else if (trimmed.StartsWith("#include ".AsSpan()))
                {
                    var includeFile = trimmed.Slice("#include ".Length).TrimEnd('"').TrimStart('"');
                    var includePath = GetFullIncludePath(includeFile);

                    if (includePath.Equals(currentPath))
                    {
                        continue;
                    }
                    
                    if (includeFile.EndsWith(".litshader".AsSpan(), StringComparison.Ordinal))
                    {
                        var includeFileLines = File.ReadLines(includePath);
                        _sourceDependencies.Add(includePath);
                        string fileName = Path.GetFileName(includePath);
                        var enumeratorWrapper = new EnumeratorWrapper(includeFileLines, fileName, includePath);
                        GetShaderBlocksRecursive(enumeratorWrapper, shaderData, currentPath);
                    }

                }
            }
            ienum.Dispose();
        }

        private string GetFullIncludePath(ReadOnlySpan<char> includeFile)
        {
            string includePath;
            if (includeFile.StartsWith("Assets/".AsSpan()) || includeFile.StartsWith("Packages/".AsSpan()))
            {
                includePath = includeFile.ToString();
            }
            else
            {
                includePath = _lastFolderPath + "/" + includeFile.ToString();
            }

            return includePath;
        }

        private void AppendLineBlockSpan(EnumeratorWrapper ienum, StringBuilder sb, ReadOnlySpan<char> breakName, bool appendFileLine = true)
        {
            if (appendFileLine)
            {
                //sb.AppendLine("#line " + (ienum.Index + 2));
                string lineDirective = "#line " + (ienum.Index + 2) + " \"" + ienum.FileName + "\"";
                sb.AppendLine(lineDirective);
            }

            while (ienum.MoveNext())
            {

                var line = ienum.Current.AsSpan();
                var trimmed = line.TrimStart();

             /*   if (trimmed.IsEmpty)
                {
                    continue;
                }

                if (trimmed.StartsWith("//".AsSpan()))
                {
                    continue;
                }*/

                if (trimmed.StartsWith(breakName))
                {
                    break;
                }

                if (trimmed.StartsWith("#include_optional \"".AsSpan()))
                {
                    var includeFile = trimmed.Slice("#include_optional ".Length).TrimEnd('"').TrimStart('"');
                    var includePath = GetFullIncludePath(includeFile);
                    _sourceDependencies.Add(includePath);
                    if (!File.Exists(includePath))
                    {
                        sb.AppendLine(string.Empty); // for the line directive
                        continue;
                    }

                    sb.AppendLine("#include \"" + includePath + "\"");
                    continue;
                }

                else if (trimmed.StartsWith("#include <".AsSpan()))
                {
                    var includeFile = trimmed.Slice("#include ".Length).TrimStart('<').TrimEnd('>');
                    var includePath = "Packages/com.z3y.shaders/ShaderLibrary/" + includeFile.ToString();

                    _sourceDependencies.Add(includePath);
                    sb.AppendLine("#include \"" + includePath + "\"");
                    continue;
                }

                else if (trimmed.StartsWith("#include \"".AsSpan()))
                {
                    var includePath = trimmed.Slice("#include ".Length).TrimEnd('\"').TrimStart('\"');
                    _sourceDependencies.Add(includePath.ToString());
                    sb.AppendLine(ienum.Current);
                    continue;
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

        private static string GetPropertyDeclaration(ShaderSettings.DefineType defineType, string keyword, string displayName, bool toggleOff = false)
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

        private static Texture2D DfgLut()
        {
            const string path = "Packages/com.z3y.shaders/ShaderLibrary/dfg-multiscatter.exr"; //TODO: replace the package path with const string
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D BlueNoise()
        {
            const string path = "Packages/com.z3y.shaders/ShaderLibrary/LDR_LLL1_0.png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
