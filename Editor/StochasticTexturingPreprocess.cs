using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Lightmapping;

// Unity didnt even try to make the methods public
// Its all private in the standard shader inspector
// Copied over the entire thing here
// There is no license but ill assume
// Unity Companion License https://github.com/UnityLabs/procedural-stochastic-texturing

namespace z3y
{
    public class StochasticTexturingPreprocess
    {
        /*********************************************************************/
        /*********************************************************************/
        /*************Procedural Stochastic Texturing Pre-process*************/
        /*********************************************************************/
        /*********************************************************************/
        const float GAUSSIAN_AVERAGE = 0.5f;    // Expectation of the Gaussian distribution
        const float GAUSSIAN_STD = 0.16666f;    // Std of the Gaussian distribution
        const int LUT_WIDTH = 128;              // Size of the look-up table

        struct TextureData
        {
            public Color[] data;
            public int width;
            public int height;

            public TextureData(int w, int h)
            {
                width = w;
                height = h;
                data = new Color[w * h];
            }
            public TextureData(TextureData td)
            {
                width = td.width;
                height = td.height;
                data = new Color[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        data[y * width + x] = td.data[y * width + x];
            }

            public Color GetColor(int w, int h)
            {
                return data[h * width + w];
            }
            public ref Color GetColorRef(int w, int h)
            {
                return ref data[h * width + w];
            }
            public void SetColorAt(int w, int h, Color value)
            {
                data[h * width + w] = value;
            }
        };

        public void ApplyUserStochasticInputChoice(Material material, string mainTexName = "_MainTex", string bumpMapName = "_BumpMap", string maskMapName = "_MaskMap", string emissionMapName = "_EmissionMap")
        {
            Vector3 colorSpaceVector1 = new Vector3();
            Vector3 colorSpaceVector2 = new Vector3();
            Vector3 colorSpaceVector3 = new Vector3();
            Vector3 colorSpaceOrigin = new Vector3();
            Vector3 dxtScalers = new Vector3();
            Texture2D texT = new Texture2D(1, 1);
            Texture2D texInvT = new Texture2D(1, 1);
            TextureFormat inputFormat = TextureFormat.RGB24;

            #region ALBEDO MAP
            if (material.HasProperty(mainTexName) && material.GetTexture(mainTexName) != null)
            {
                var texName = mainTexName;
                var albedoMap = material.GetTexture(mainTexName);
                int stepCounter = 0;
                int totalSteps = 14;
                string inputName = "Albedo Map";
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

                // Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
                TextureData albedoData = TextureToTextureData((Texture2D)albedoMap, ref inputFormat);
                TextureData decorrelated = new TextureData(albedoData);
                DecorrelateColorSpace(ref albedoData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
                ComputeDXTCompressionScalers((Texture2D)albedoMap, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

                // Perform precomputations if precomputed textures don't already exist
                if (LoadPrecomputedTexturesIfExist((Texture2D)albedoMap, ref texT, ref texInvT) == false)
                {
                    TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
                    TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
                    Precomputations(ref decorrelated, new List<int> { 0, 1, 2, 3 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
                    EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
                    RescaleForDXTCompression(ref Tinput, ref dxtScalers);
                    EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

                    // Serialize precomputed data and setup material
                    SerializePrecomputedTextures((Texture2D)albedoMap, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
                }
                EditorUtility.ClearProgressBar();

                // Apply to shader properties
                material.SetTexture(texName + "T", texT);
                material.SetTexture(texName + "InvT", texInvT);
                material.SetVector(texName + "ColorSpaceOrigin", colorSpaceOrigin);
                material.SetVector(texName + "ColorSpaceVector1", colorSpaceVector1);
                material.SetVector(texName + "ColorSpaceVector2", colorSpaceVector2);
                material.SetVector(texName + "ColorSpaceVector3", colorSpaceVector3);
                material.SetVector(texName + "DXTScalers", dxtScalers);
            }
            #endregion

            #region MASKMAP

            if (material.HasProperty(maskMapName) && material.GetTexture(maskMapName) != null)
            {
                var texName = maskMapName;
                var maskMap = material.GetTexture(maskMapName);
                int stepCounter = 0;
                int totalSteps = 6;
                string inputName = "Mask Map";
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

                // Perform precomputations if precomputed textures don't already exist
                if (LoadPrecomputedTexturesIfExist((Texture2D)maskMap, ref texT, ref texInvT) == false)
                {
                    TextureData metallicData = TextureToTextureData((Texture2D)maskMap, ref inputFormat);

                    TextureData Tinput = new TextureData(metallicData.width, metallicData.height);
                    TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
                    Precomputations(ref metallicData, new List<int> { 0,1,2 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);

                    // Serialize precomputed data and setup material
                    SerializePrecomputedTextures((Texture2D)maskMap, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
                }
                EditorUtility.ClearProgressBar();

                // Apply to shader properties
                material.SetTexture(texName + "T", texT);
                material.SetTexture(texName + "InvT", texInvT);
            }
            #endregion

            #region NORMAL MAP
            if (material.HasProperty(bumpMapName) && material.GetTexture(bumpMapName) != null)
            {
                var texName = bumpMapName;
                var bumpMap = material.GetTexture(bumpMapName);
                int stepCounter = 0;
                int totalSteps = 11;
                string inputName = "Normal Map";
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

                // Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
                TextureData normalData = TextureToTextureData((Texture2D)bumpMap, ref inputFormat);
                TextureData decorrelated = new TextureData(normalData);
                DecorrelateColorSpace(ref normalData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
                ComputeDXTCompressionScalers((Texture2D)bumpMap, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

                // Perform precomputations if precomputed textures don't already exist
                if (LoadPrecomputedTexturesIfExist((Texture2D)bumpMap, ref texT, ref texInvT) == false)
                {
                    TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
                    TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
                    Precomputations(ref decorrelated, new List<int> { 0, 1, 2 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
                    EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
                    RescaleForDXTCompression(ref Tinput, ref dxtScalers);
                    EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

                    // Serialize precomputed data and setup material
                    SerializePrecomputedTextures((Texture2D)bumpMap, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
                }
                EditorUtility.ClearProgressBar();

                // Apply to shader properties
                material.SetTexture(texName + "T", texT);
                material.SetTexture(texName + "InvT", texInvT);
                material.SetVector(texName + "ColorSpaceOrigin", colorSpaceOrigin);
                material.SetVector(texName + "ColorSpaceVector1", colorSpaceVector1);
                material.SetVector(texName + "ColorSpaceVector2", colorSpaceVector2);
                material.SetVector(texName + "ColorSpaceVector3", colorSpaceVector3);
                material.SetVector(texName + "DXTScalers", dxtScalers);
            }
            #endregion

            #region EMISSION MAP
            if (material.HasProperty(emissionMapName) && material.GetTexture(emissionMapName) != null)
            {
                var texName = emissionMapName;
                var emissionMap = material.GetTexture(emissionMapName);
                int stepCounter = 0;
                int totalSteps = 11;
                string inputName = "Emission Map";
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

                // Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
                TextureData emissionData = TextureToTextureData((Texture2D)emissionMap, ref inputFormat);
                TextureData decorrelated = new TextureData(emissionData);
                DecorrelateColorSpace(ref emissionData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
                ComputeDXTCompressionScalers((Texture2D)emissionMap, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

                // Perform precomputations if precomputed textures don't already exist
                if (LoadPrecomputedTexturesIfExist((Texture2D)emissionMap, ref texT, ref texInvT) == false)
                {
                    TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
                    TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
                    Precomputations(ref decorrelated, new List<int> { 0, 1, 2 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
                    EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
                    RescaleForDXTCompression(ref Tinput, ref dxtScalers);
                    EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

                    // Serialize precomputed data and setup material
                    SerializePrecomputedTextures((Texture2D)emissionMap, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
                }
                EditorUtility.ClearProgressBar();

                // Apply to shader properties
                material.SetTexture(texName + "T", texT);
                material.SetTexture(texName + "InvT", texInvT);
                material.SetVector(texName + "ColorSpaceOrigin", colorSpaceOrigin);
                material.SetVector(texName + "ColorSpaceVector1", colorSpaceVector1);
                material.SetVector(texName + "ColorSpaceVector2", colorSpaceVector2);
                material.SetVector(texName + "ColorSpaceVector3", colorSpaceVector3);
                material.SetVector(texName + "DXTScalers", dxtScalers);
            }
            #endregion
        }
        bool LoadPrecomputedTexturesIfExist(Texture2D input, ref Texture2D Tinput, ref Texture2D invT)
        {
            Tinput = null;
            invT = null;

            string localInputPath = AssetDatabase.GetAssetPath(input);
            int fileExtPos = localInputPath.LastIndexOf(".");
            if (fileExtPos >= 0)
                localInputPath = localInputPath.Substring(0, fileExtPos);

            Tinput = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_T.png", typeof(Texture2D));
            invT = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_invT.png", typeof(Texture2D));

            if (Tinput != null && invT != null)
                return true;
            else
                return false;
        }

        TextureData TextureToTextureData(Texture2D input, ref TextureFormat inputFormat)
        {
            // Modify input texture import settings temporarily
            string texpath = AssetDatabase.GetAssetPath(input);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(texpath);
            TextureImporterCompression prev = importer.textureCompression;
            TextureImporterType prevType = importer.textureType;
            bool linearInput = importer.sRGBTexture == false || importer.textureType == TextureImporterType.NormalMap;
            bool prevReadable = importer.isReadable;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
                inputFormat = input.format;
            }

            // Copy input texture pixel data
            Color[] colors = input.GetPixels();
            TextureData res = new TextureData(input.width, input.height);
            for (int x = 0; x < res.width; x++)
            {
                for (int y = 0; y < res.height; y++)
                {
                    res.SetColorAt(x, y, linearInput || PlayerSettings.colorSpace == ColorSpace.Gamma ?
                        colors[y * res.width + x] : colors[y * res.width + x].linear);
                }
            }

            // Revert input texture settings
            if (importer != null)
            {
                importer.textureType = prevType;
                importer.isReadable = prevReadable;
                importer.textureCompression = prev;
                AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
            }
            return res;
        }

        void SerializePrecomputedTextures(Texture2D input, ref TextureFormat inputFormat, ref TextureData Tinput, ref TextureData invT, ref Texture2D output, ref Texture2D outputLUT)
        {
            string path = AssetDatabase.GetAssetPath(input);
            TextureImporter inputImporter = (TextureImporter)TextureImporter.GetAtPath(path);

            // Copy generated data into new textures
            output = new Texture2D(Tinput.width, Tinput.height, inputFormat, true, true);
            output.SetPixels(Tinput.data);
            output.Apply();

            outputLUT = new Texture2D(invT.width, invT.height, inputFormat, false, true);
            outputLUT.SetPixels(invT.data);
            outputLUT.Apply();

            // Create output path at input texture location
            string assetsPath = Application.dataPath;
            assetsPath = assetsPath.Substring(0, assetsPath.Length - "Assets".Length);

            string localInputPath = AssetDatabase.GetAssetPath(input);
            int fileExtPos = localInputPath.LastIndexOf(".");
            if (fileExtPos >= 0)
                localInputPath = localInputPath.Substring(0, fileExtPos);

            // Write output textures
            System.IO.File.WriteAllBytes(assetsPath + localInputPath + "_T.png", output.EncodeToPNG());
            System.IO.File.WriteAllBytes(assetsPath + localInputPath + "_invT.png", outputLUT.EncodeToPNG());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            output = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_T.png", typeof(Texture2D));
            outputLUT = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_invT.png", typeof(Texture2D));

            // Change import settings
            path = AssetDatabase.GetAssetPath(output);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = input.filterMode;
            importer.anisoLevel = input.anisoLevel;
            importer.mipmapEnabled = inputImporter.mipmapEnabled;
            importer.sRGBTexture = false;
            importer.textureCompression = inputImporter.textureCompression;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            path = AssetDatabase.GetAssetPath(outputLUT);
            importer = (TextureImporter)TextureImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private void Precomputations(
            ref TextureData input,      // input: example image
                List<int> channels,     // input: channels to process
            ref TextureData Tinput,     // output: T(input) image
            ref TextureData invT,       // output: T^{-1} look-up table
            string inputName,
            ref int stepCounter,
            int totalSteps)
        {
            // Section 1.3.2 Applying the histogram transformation T on the input
            foreach (int channel in channels)
            {
                ComputeTinput(ref input, ref Tinput, channel);
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
            }

            // Section 1.3.3 Precomputing the inverse histogram transformation T^{-1}
            foreach (int channel in channels)
            {
                ComputeinvT(ref input, ref invT, channel);
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
            }

            // Section 1.5 Improvement: prefiltering the look-up table
            foreach (int channel in channels)
            {
                PrefilterLUT(ref Tinput, ref invT, channel);
                EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
            }
        }

        private void ComputeDXTCompressionScalers(Texture2D input, ref Vector3 DXTScalers, Vector3 colorSpaceVector1, Vector3 colorSpaceVector2, Vector3 colorSpaceVector3)
        {
            string path = AssetDatabase.GetAssetPath(input);
            TextureImporter inputImporter = (TextureImporter)TextureImporter.GetAtPath(path);
            if (inputImporter.textureCompression != TextureImporterCompression.Uncompressed)
            {

                DXTScalers.x = 1.0f / Mathf.Sqrt(colorSpaceVector1.x * colorSpaceVector1.x + colorSpaceVector1.y * colorSpaceVector1.y + colorSpaceVector1.z * colorSpaceVector1.z);
                DXTScalers.y = 1.0f / Mathf.Sqrt(colorSpaceVector2.x * colorSpaceVector2.x + colorSpaceVector2.y * colorSpaceVector2.y + colorSpaceVector2.z * colorSpaceVector2.z);
                DXTScalers.z = 1.0f / Mathf.Sqrt(colorSpaceVector3.x * colorSpaceVector3.x + colorSpaceVector3.y * colorSpaceVector3.y + colorSpaceVector3.z * colorSpaceVector3.z);
            }
            else
            {
                DXTScalers.x = -1.0f;
                DXTScalers.y = -1.0f;
                DXTScalers.z = -1.0f;
            }
        }

        private void RescaleForDXTCompression(ref TextureData Tinput, ref Vector3 DXTScalers)
        {
            // If we use DXT compression
            // we need to rescale the Gaussian channels (see Section 1.6)
            if (DXTScalers.x >= 0.0f)
            {
                for (int y = 0; y < Tinput.height; y++)
                    for (int x = 0; x < Tinput.width; x++)
                        for (int i = 0; i < 3; i++)
                        {
                            float v = Tinput.GetColor(x, y)[i];
                            v = (v - 0.5f) / DXTScalers[i] + 0.5f;
                            Tinput.GetColorRef(x, y)[i] = v;
                        }
            }
        }

        /*****************************************************************************/
        /**************** Section 1.3.1 Target Gaussian distribution *****************/
        /*****************************************************************************/

        private float Erf(float x)
        {
            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Mathf.Abs(x);

            // A&S formula 7.1.26
            float t = 1.0f / (1.0f + 0.3275911f * x);
            float y = 1.0f - (((((1.061405429f * t + -1.453152027f) * t) + 1.421413741f)
                * t + -0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-x * x);

            return sign * y;
        }

        private float ErfInv(float x)
        {
            float w, p;
            w = -Mathf.Log((1.0f - x) * (1.0f + x));
            if (w < 5.000000f)
            {
                w = w - 2.500000f;
                p = 2.81022636e-08f;
                p = 3.43273939e-07f + p * w;
                p = -3.5233877e-06f + p * w;
                p = -4.39150654e-06f + p * w;
                p = 0.00021858087f + p * w;
                p = -0.00125372503f + p * w;
                p = -0.00417768164f + p * w;
                p = 0.246640727f + p * w;
                p = 1.50140941f + p * w;
            }
            else
            {
                w = Mathf.Sqrt(w) - 3.000000f;
                p = -0.000200214257f;
                p = 0.000100950558f + p * w;
                p = 0.00134934322f + p * w;
                p = -0.00367342844f + p * w;
                p = 0.00573950773f + p * w;
                p = -0.0076224613f + p * w;
                p = 0.00943887047f + p * w;
                p = 1.00167406f + p * w;
                p = 2.83297682f + p * w;
            }
            return p * x;
        }

        private float CDF(float x, float mu, float sigma)
        {
            float U = 0.5f * (1 + Erf((x - mu) / (sigma * Mathf.Sqrt(2.0f))));
            return U;
        }

        private float invCDF(float U, float mu, float sigma)
        {
            float x = sigma * Mathf.Sqrt(2.0f) * ErfInv(2.0f * U - 1.0f) + mu;
            return x;
        }

        /*****************************************************************************/
        /**** Section 1.3.2 Applying the histogram transformation T on the input *****/
        /*****************************************************************************/
        private struct PixelSortStruct
        {
            public int x;
            public int y;
            public float value;
        };

        private void ComputeTinput(ref TextureData input, ref TextureData T_input, int channel)
        {
            // Sort pixels of example image
            PixelSortStruct[] sortedInputValues = new PixelSortStruct[input.width * input.height];
            for (int y = 0; y < input.height; y++)
                for (int x = 0; x < input.width; x++)
                {
                    sortedInputValues[y * input.width + x].x = x;
                    sortedInputValues[y * input.width + x].y = y;
                    sortedInputValues[y * input.width + x].value = input.GetColor(x, y)[channel];
                }
            Array.Sort(sortedInputValues, (x, y) => x.value.CompareTo(y.value));

            // Assign Gaussian value to each pixel
            for (uint i = 0; i < sortedInputValues.Length; i++)
            {
                // Pixel coordinates
                int x = sortedInputValues[i].x;
                int y = sortedInputValues[i].y;
                // Input quantile (given by its order in the sorting)
                float U = (i + 0.5f) / (sortedInputValues.Length);
                // Gaussian quantile
                float G = invCDF(U, GAUSSIAN_AVERAGE, GAUSSIAN_STD);
                // Store
                T_input.GetColorRef(x, y)[channel] = G;
            }
        }

        /*****************************************************************************/
        /** Section 1.3.3 Precomputing the inverse histogram transformation T^{-1} ***/
        /*****************************************************************************/

        private void ComputeinvT(ref TextureData input, ref TextureData Tinv, int channel)
        {
            // Sort pixels of example image
            float[] sortedInputValues = new float[input.width * input.height];
            for (int y = 0; y < input.height; y++)
                for (int x = 0; x < input.width; x++)
                {
                    sortedInputValues[y * input.width + x] = input.GetColor(x, y)[channel];
                }
            Array.Sort(sortedInputValues);

            // Generate Tinv look-up table 
            for (int i = 0; i < Tinv.width; i++)
            {
                // Gaussian value in [0, 1]
                float G = (i + 0.5f) / (Tinv.width);
                // Quantile value 
                float U = CDF(G, GAUSSIAN_AVERAGE, GAUSSIAN_STD);
                // Find quantile in sorted pixel values
                int index = (int)Mathf.Floor(U * sortedInputValues.Length);
                // Get input value 
                float I = sortedInputValues[index];
                // Store in LUT
                Tinv.GetColorRef(i, 0)[channel] = I;
            }
        }

        /*****************************************************************************/
        /******** Section 1.4 Improvement: using a decorrelated color space **********/
        /*****************************************************************************/

        // Compute the eigen vectors of the histogram of the input
        private void ComputeEigenVectors(ref TextureData input, Vector3[] eigenVectors)
        {
            // First and second order moments
            float R = 0, G = 0, B = 0, RR = 0, GG = 0, BB = 0, RG = 0, RB = 0, GB = 0;
            for (int y = 0; y < input.height; y++)
            {
                for (int x = 0; x < input.width; x++)
                {
                    Color col = input.GetColor(x, y);
                    R += col.r;
                    G += col.g;
                    B += col.b;
                    RR += col.r * col.r;
                    GG += col.g * col.g;
                    BB += col.b * col.b;
                    RG += col.r * col.g;
                    RB += col.r * col.b;
                    GB += col.g * col.b;
                }
            }

            R /= (float)(input.width * input.height);
            G /= (float)(input.width * input.height);
            B /= (float)(input.width * input.height);
            RR /= (float)(input.width * input.height);
            GG /= (float)(input.width * input.height);
            BB /= (float)(input.width * input.height);
            RG /= (float)(input.width * input.height);
            RB /= (float)(input.width * input.height);
            GB /= (float)(input.width * input.height);

            // Covariance matrix
            double[][] covarMat = new double[3][];
            for (int i = 0; i < 3; i++)
                covarMat[i] = new double[3];
            covarMat[0][0] = RR - R * R;
            covarMat[0][1] = RG - R * G;
            covarMat[0][2] = RB - R * B;
            covarMat[1][0] = RG - R * G;
            covarMat[1][1] = GG - G * G;
            covarMat[1][2] = GB - G * B;
            covarMat[2][0] = RB - R * B;
            covarMat[2][1] = GB - G * B;
            covarMat[2][2] = BB - B * B;

            // Find eigen values and vectors using Jacobi algorithm
            double[][] eigenVectorsTemp = new double[3][];
            for (int i = 0; i < 3; i++)
                eigenVectorsTemp[i] = new double[3];
            double[] eigenValuesTemp = new double[3];
            ComputeEigenValuesAndVectors(covarMat, eigenVectorsTemp, eigenValuesTemp);

            // Set return values
            eigenVectors[0] = new Vector3((float)eigenVectorsTemp[0][0], (float)eigenVectorsTemp[1][0], (float)eigenVectorsTemp[2][0]);
            eigenVectors[1] = new Vector3((float)eigenVectorsTemp[0][1], (float)eigenVectorsTemp[1][1], (float)eigenVectorsTemp[2][1]);
            eigenVectors[2] = new Vector3((float)eigenVectorsTemp[0][2], (float)eigenVectorsTemp[1][2], (float)eigenVectorsTemp[2][2]);
        }

        // ----------------------------------------------------------------------------
        // Numerical diagonalization of 3x3 matrcies
        // Copyright (C) 2006  Joachim Kopp
        // ----------------------------------------------------------------------------
        // This library is free software; you can redistribute it and/or
        // modify it under the terms of the GNU Lesser General Public
        // License as published by the Free Software Foundation; either
        // version 2.1 of the License, or (at your option) any later version.
        //
        // This library is distributed in the hope that it will be useful,
        // but WITHOUT ANY WARRANTY; without even the implied warranty of
        // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
        // Lesser General Public License for more details.
        //
        // You should have received a copy of the GNU Lesser General Public
        // License along with this library; if not, write to the Free Software
        // Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
        // ----------------------------------------------------------------------------
        // Calculates the eigenvalues and normalized eigenvectors of a symmetric 3x3
        // matrix A using the Jacobi algorithm.
        // The upper triangular part of A is destroyed during the calculation,
        // the diagonal elements are read but not destroyed, and the lower
        // triangular elements are not referenced at all.
        // ----------------------------------------------------------------------------
        // Parameters:
        //		A: The symmetric input matrix
        //		Q: Storage buffer for eigenvectors
        //		w: Storage buffer for eigenvalues
        // ----------------------------------------------------------------------------
        // Return value:
        //		0: Success
        //		-1: Error (no convergence)
        private int ComputeEigenValuesAndVectors(double[][] A, double[][] Q, double[] w)
        {
            const int n = 3;
            double sd, so;                  // Sums of diagonal resp. off-diagonal elements
            double s, c, t;                 // sin(phi), cos(phi), tan(phi) and temporary storage
            double g, h, z, theta;          // More temporary storage
            double thresh;

            // Initialize Q to the identitity matrix
            for (int i = 0; i < n; i++)
            {
                Q[i][i] = 1.0;
                for (int j = 0; j < i; j++)
                    Q[i][j] = Q[j][i] = 0.0;
            }

            // Initialize w to diag(A)
            for (int i = 0; i < n; i++)
                w[i] = A[i][i];

            // Calculate SQR(tr(A))  
            sd = 0.0;
            for (int i = 0; i < n; i++)
                sd += System.Math.Abs(w[i]);
            sd = sd * sd;

            // Main iteration loop
            for (int nIter = 0; nIter < 50; nIter++)
            {
                // Test for convergence 
                so = 0.0;
                for (int p = 0; p < n; p++)
                    for (int q = p + 1; q < n; q++)
                        so += System.Math.Abs(A[p][q]);
                if (so == 0.0)
                    return 0;

                if (nIter < 4)
                    thresh = 0.2 * so / (n * n);
                else
                    thresh = 0.0;

                // Do sweep
                for (int p = 0; p < n; p++)
                {
                    for (int q = p + 1; q < n; q++)
                    {
                        g = 100.0 * System.Math.Abs(A[p][q]);
                        if (nIter > 4 && System.Math.Abs(w[p]) + g == System.Math.Abs(w[p])
                            && System.Math.Abs(w[q]) + g == System.Math.Abs(w[q]))
                        {
                            A[p][q] = 0.0;
                        }
                        else if (System.Math.Abs(A[p][q]) > thresh)
                        {
                            // Calculate Jacobi transformation
                            h = w[q] - w[p];
                            if (System.Math.Abs(h) + g == System.Math.Abs(h))
                            {
                                t = A[p][q] / h;
                            }
                            else
                            {
                                theta = 0.5 * h / A[p][q];
                                if (theta < 0.0)
                                    t = -1.0 / (System.Math.Sqrt(1.0 + (theta * theta)) - theta);
                                else
                                    t = 1.0 / (System.Math.Sqrt(1.0 + (theta * theta)) + theta);
                            }
                            c = 1.0 / System.Math.Sqrt(1.0 + (t * t));
                            s = t * c;
                            z = t * A[p][q];

                            // Apply Jacobi transformation
                            A[p][q] = 0.0;
                            w[p] -= z;
                            w[q] += z;
                            for (int r = 0; r < p; r++)
                            {
                                t = A[r][p];
                                A[r][p] = c * t - s * A[r][q];
                                A[r][q] = s * t + c * A[r][q];
                            }
                            for (int r = p + 1; r < q; r++)
                            {
                                t = A[p][r];
                                A[p][r] = c * t - s * A[r][q];
                                A[r][q] = s * t + c * A[r][q];
                            }
                            for (int r = q + 1; r < n; r++)
                            {
                                t = A[p][r];
                                A[p][r] = c * t - s * A[q][r];
                                A[q][r] = s * t + c * A[q][r];
                            }

                            // Update eigenvectors
                            for (int r = 0; r < n; r++)
                            {
                                t = Q[r][p];
                                Q[r][p] = c * t - s * Q[r][q];
                                Q[r][q] = s * t + c * Q[r][q];
                            }
                        }
                    }
                }
            }

            return -1;
        }

        // Main function of Section 1.4
        private void DecorrelateColorSpace(
            ref TextureData input,                  // input: example image
            ref TextureData input_decorrelated,     // output: decorrelated input 
            ref Vector3 colorSpaceVector1,          // output: color space vector1 
            ref Vector3 colorSpaceVector2,          // output: color space vector2
            ref Vector3 colorSpaceVector3,          // output: color space vector3
            ref Vector3 colorSpaceOrigin)           // output: color space origin
        {
            // Compute the eigenvectors of the histogram
            Vector3[] eigenvectors = new Vector3[3];
            ComputeEigenVectors(ref input, eigenvectors);

            // Rotate to eigenvector space
            for (int y = 0; y < input.height; y++)
                for (int x = 0; x < input.width; x++)
                    for (int channel = 0; channel < 3; ++channel)
                    {
                        // Get current color
                        Color color = input.GetColor(x, y);
                        Vector3 vec = new Vector3(color.r, color.g, color.b);
                        // Project on eigenvector 
                        float new_channel_value = Vector3.Dot(vec, eigenvectors[channel]);
                        // Store
                        input_decorrelated.GetColorRef(x, y)[channel] = new_channel_value;
                    }

            // Compute ranges of the new color space
            Vector2[] colorSpaceRanges = new Vector2[3]{
                new Vector2(float.MaxValue, float.MinValue),
                new Vector2(float.MaxValue, float.MinValue),
                new Vector2(float.MaxValue, float.MinValue) };
            for (int y = 0; y < input.height; y++)
                for (int x = 0; x < input.width; x++)
                    for (int channel = 0; channel < 3; ++channel)
                    {
                        colorSpaceRanges[channel].x = Mathf.Min(colorSpaceRanges[channel].x, input_decorrelated.GetColor(x, y)[channel]);
                        colorSpaceRanges[channel].y = Mathf.Max(colorSpaceRanges[channel].y, input_decorrelated.GetColor(x, y)[channel]);
                    }

            // Remap range to [0, 1]
            for (int y = 0; y < input.height; y++)
                for (int x = 0; x < input.width; x++)
                    for (int channel = 0; channel < 3; ++channel)
                    {
                        // Get current value
                        float value = input_decorrelated.GetColor(x, y)[channel];
                        // Remap in [0, 1]
                        float remapped_value = (value - colorSpaceRanges[channel].x) / (colorSpaceRanges[channel].y - colorSpaceRanges[channel].x);
                        // Store
                        input_decorrelated.GetColorRef(x, y)[channel] = remapped_value;
                    }

            // Compute color space origin and vectors scaled for the normalized range
            colorSpaceOrigin.x = colorSpaceRanges[0].x * eigenvectors[0].x + colorSpaceRanges[1].x * eigenvectors[1].x + colorSpaceRanges[2].x * eigenvectors[2].x;
            colorSpaceOrigin.y = colorSpaceRanges[0].x * eigenvectors[0].y + colorSpaceRanges[1].x * eigenvectors[1].y + colorSpaceRanges[2].x * eigenvectors[2].y;
            colorSpaceOrigin.z = colorSpaceRanges[0].x * eigenvectors[0].z + colorSpaceRanges[1].x * eigenvectors[1].z + colorSpaceRanges[2].x * eigenvectors[2].z;
            colorSpaceVector1.x = eigenvectors[0].x * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
            colorSpaceVector1.y = eigenvectors[0].y * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
            colorSpaceVector1.z = eigenvectors[0].z * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
            colorSpaceVector2.x = eigenvectors[1].x * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
            colorSpaceVector2.y = eigenvectors[1].y * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
            colorSpaceVector2.z = eigenvectors[1].z * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
            colorSpaceVector3.x = eigenvectors[2].x * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
            colorSpaceVector3.y = eigenvectors[2].y * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
            colorSpaceVector3.z = eigenvectors[2].z * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
        }

        /*****************************************************************************/
        /* ===== Section 1.5 Improvement: prefiltering the look-up table =========== */
        /*****************************************************************************/
        // Compute average subpixel variance at a given LOD
        private float ComputeLODAverageSubpixelVariance(ref TextureData image, int LOD, int channel)
        {
            // Window width associated with
            int windowWidth = 1 << LOD;

            // Compute average variance in all the windows
            float average_window_variance = 0.0f;

            // Loop over al the windows
            for (int window_y = 0; window_y < image.height; window_y += windowWidth)
                for (int window_x = 0; window_x < image.width; window_x += windowWidth)
                {
                    // Estimate variance of current window
                    float v = 0.0f;
                    float v2 = 0.0f;
                    for (int y = 0; y < windowWidth; y++)
                        for (int x = 0; x < windowWidth; x++)
                        {
                            float value = image.GetColor(window_x + x, window_y + y)[channel];
                            v += value;
                            v2 += value * value;
                        }
                    v /= (float)(windowWidth * windowWidth);
                    v2 /= (float)(windowWidth * windowWidth);
                    float window_variance = Mathf.Max(0.0f, v2 - v * v);

                    // Update average
                    average_window_variance += window_variance / (image.width * image.height / windowWidth / windowWidth);
                }

            return average_window_variance;
        }

        // Filter LUT by sampling a Gaussian N(mu, std²)
        private float FilterLUTValueAtx(ref TextureData LUT, float x, float std, int channel)
        {
            // Number of samples for filtering (heuristic: twice the LUT resolution)
            const int numberOfSamples = 2 * LUT_WIDTH;

            // Filter
            float filtered_value = 0.0f;
            for (int sample = 0; sample < numberOfSamples; sample++)
            {
                // Quantile used to sample the Gaussian
                float U = (sample + 0.5f) / numberOfSamples;
                // Sample the Gaussian 
                float sample_x = invCDF(U, x, std);
                // Find sample texel in LUT (the LUT covers the domain [0, 1])
                int sample_texel = Mathf.Max(0, Mathf.Min(LUT_WIDTH - 1, (int)Mathf.Floor(sample_x * LUT_WIDTH)));
                // Fetch LUT at level 0
                float sample_value = LUT.GetColor(sample_texel, 0)[channel];
                // Accumulate
                filtered_value += sample_value;
            }
            // Normalize and return
            filtered_value /= (float)numberOfSamples;
            return filtered_value;
        }

        // Main function of section 1.5
        private void PrefilterLUT(ref TextureData image_T_Input, ref TextureData LUT_Tinv, int channel)
        {
            // Prefilter 
            for (int LOD = 1; LOD < LUT_Tinv.height; LOD++)
            {
                // Compute subpixel variance at LOD 
                float window_variance = ComputeLODAverageSubpixelVariance(ref image_T_Input, LOD, channel);
                float window_std = Mathf.Sqrt(window_variance);

                // Prefilter LUT with Gaussian kernel of this variance
                for (int i = 0; i < LUT_Tinv.width; i++)
                {
                    // Texel position in [0, 1]
                    float x_texel = (i + 0.5f) / LUT_Tinv.width;
                    // Filter look-up table around this position with Gaussian kernel
                    float filteredValue = FilterLUTValueAtx(ref LUT_Tinv, x_texel, window_std, channel);
                    // Store filtered value
                    LUT_Tinv.GetColorRef(i, LOD)[channel] = filteredValue;
                }
            }
        }
    }
}