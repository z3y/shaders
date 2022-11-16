using System;
using System.Diagnostics;
using JetBrains.Annotations;
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
        }

        public static ImageFormat PackingFormat = ImageFormat.TIFF;
        public static FREE_IMAGE_FILTER ImageFilter = FREE_IMAGE_FILTER.Bilinear;

        /*
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

            SetChannel(albedoTex, alphaTex, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

            FreeImage_Save(PackingFormat, albedoTex, destinationPath);

            FreeImage_Unload(albedoTex);
            FreeImage_Unload(alphaTex);
        }
        */
        
        public struct TextureChannel
        {
            [CanBeNull] public string Path;
            public bool Invert;
            public ChannelSource Source;
            public DefaultColor DefaultColor;
        }
        
        public enum DefaultColor
        {
            White,
            Black
        }

        private static void HandleTextureChannel(TextureChannel textureChannel, (int, int) widthHeight, IntPtr newImage, FREE_IMAGE_COLOR_CHANNEL newChannel)
        {
            if (textureChannel.Path is null)
            {
                if (textureChannel.DefaultColor == DefaultColor.White)
                {
                    var ch = GetChannel(newImage, ChannelSourceToFreeImage(textureChannel.Source));
                    FillBackground(ch, Color.White);
                    SetChannel(newImage, ch, newChannel);
                }
                return;
            }
            
            var ptr = FreeImage_Load(textureChannel.Path);
            var size = GetWithAndHeight(ptr);

            uint bpp = GetBPP(ptr);
            Debug.Log(bpp);
            if (bpp > 8)
            {
                ptr = GetChannel(ptr, ChannelSourceToFreeImage(textureChannel.Source));
            }
            
            if (size.Item1 != widthHeight.Item1 || size.Item2 != widthHeight.Item2)
            {
                ptr = Rescale(ptr, widthHeight.Item1, widthHeight.Item2, ImageFilter);
            }
            
            if (textureChannel.Invert)
            {
                Invert(ptr);
            }

            SetChannel(newImage, ptr, newChannel);
            
            FreeImage_Unload(ptr);
        }
        
        public static void PackCustom(string destinationPath, TextureChannel textureChannelR, TextureChannel textureChannelG, TextureChannel textureChannelB, TextureChannel textureChannelA, (int, int) widthHeight, TexturePackingFormat format)
        {
            var sw = new Stopwatch();
            sw.Start();

            IntPtr newTexture = Allocate(widthHeight.Item1, widthHeight.Item2, 32);

            HandleTextureChannel(textureChannelR, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
            HandleTextureChannel(textureChannelG, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_GREEN);
            HandleTextureChannel(textureChannelB, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
            HandleTextureChannel(textureChannelA, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

            FreeImage_Save((ImageFormat)format, newTexture, destinationPath + "." + Enum.GetName(typeof(TexturePackingFormat), format));

            FreeImage_Unload(newTexture);
            
            
            sw.Stop();
            Debug.Log($"Packed in {sw.ElapsedMilliseconds} ms");
        }
        
        public enum TexturePackingFormat
        {
            tga = 17,
            psd = 20,
            tiff = 18,
            exr = 29,
            jpg = 2,
            png = 13,
        }
        
        public enum ChannelSource
        {
            Red,
            Green,
            Blue,
            Alpha
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