using System;
using System.Diagnostics;
using UnityEditor;
using Color = System.Drawing.Color;
using Debug = UnityEngine.Debug;
using static z3y.FreeImage;

namespace z3y
{
    [InitializeOnLoad]
    public static class FreeImagePacking
    {
        static FreeImagePacking()
        {
            SetOutputMessage(LogMessage);

            Pack();


        }
        
        [MenuItem("Test/Pack")]
        public static void Pack()
        {
            var sw = new Stopwatch();
            sw.Start();
            PackAlbedoAlpha(@"d:\packed.tga", @"d:\Bricks043_1K_Color.png", @"d:\shader.png", ChannelSource.Green, true);
            sw.Stop();
            Debug.Log("Packed " + sw.ElapsedMilliseconds);
        }

        public static ImageFormat PackingFormat = ImageFormat.FIF_TARGA;
        public static FREE_IMAGE_FILTER ImageFilter = FREE_IMAGE_FILTER.FILTER_BILINEAR;

        public static void PackAlbedoAlpha(string destinationPath, string albedoPath, string alphaPath, ChannelSource alphaSource = ChannelSource.Grayscale, bool invertAlpha = false)
        {
            
            var albedoTex = FreeImage_Load(albedoPath);
            var alphaTex = FreeImage_Load(alphaPath);
            var albedoWh = GetWithAndHeight(albedoTex);
            var alphaWh = GetWithAndHeight(alphaTex);

            if (alphaWh.Item1 != albedoWh.Item1 || alphaWh.Item2 != albedoWh.Item2)
            {
                alphaTex = Rescale(alphaTex, (int)albedoWh.Item1, (int)albedoWh.Item2, ImageFilter);
            }
            
            albedoTex = ConvertTo32Bits(albedoTex);

            if (alphaSource != ChannelSource.Grayscale)
            {
                var source = ChannelSourceToFreeImage(alphaSource);
                alphaTex = GetChannel(alphaTex, source);
            }

            if (invertAlpha)
            {
                Invert(alphaTex);
            }

            var success = SetChannel(albedoTex, alphaTex, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
            
            Debug.Log(success);

            FreeImage_Save(PackingFormat, albedoTex, destinationPath);

            FreeImage_Unload(albedoTex);
            FreeImage_Unload(alphaTex);
        }
        
        
        
        public enum ChannelSource
        {
            Red,
            Green,
            Blue,
            Alpha,
            Grayscale
        }
        
        public static FREE_IMAGE_COLOR_CHANNEL ChannelSourceToFreeImage(ChannelSource channelSource)
        {
            switch (channelSource)
            {
                case ChannelSource.Red:
                    return FREE_IMAGE_COLOR_CHANNEL.FICC_RED;
                case ChannelSource.Green:
                    return FREE_IMAGE_COLOR_CHANNEL.FICC_GREEN;
                case ChannelSource.Blue:
                    return FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE;
                case ChannelSource.Alpha:
                    return FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA;
                case ChannelSource.Grayscale:
                    return FREE_IMAGE_COLOR_CHANNEL.FICC_RGB;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channelSource), channelSource, null);
            }
        }

        private static void LogMessage(FreeImage.ImageFormat fif, string message)
        {
            Debug.Log(message);
        }
    }
}