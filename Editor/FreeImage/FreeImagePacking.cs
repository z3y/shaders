using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
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

            //Pack();


        }
        
        [MenuItem("Test/Pack")]
        public static void Pack()
        {
            var sw = new Stopwatch();
            sw.Start();
            //PackAlbedoAlpha(@"d:\packed.tga", @"d:\Bricks043_1K_Color.png", @"d:\shader.png", ChannelSource.Green, true);

            var r = new TextureChannel()
            {
                DefaultBlack = false,
                Invert = false,
                Path = null,
                Source = ChannelSource.Red
            };
            
            var g = new TextureChannel()
            {
                DefaultBlack = true,
                Invert = false,
                Path = null,
                Source = ChannelSource.Red
            };
            
            var b = new TextureChannel()
            {
                DefaultBlack = false,
                Invert = false,
                Path = null,
                Source = ChannelSource.Red
            };
            
            var a = new TextureChannel()
            {
                DefaultBlack = false,
                Invert = true,
                Path = @"d:\Bricks043_1K_Roughness.png",
                Source = ChannelSource.Grayscale
            };
            
            PackCustom(@"d:\packed2.tiff", r, g, b, a, (512,512));
            
            
            sw.Stop();
            Debug.Log("Packed " + sw.ElapsedMilliseconds);
        }

        public static ImageFormat PackingFormat = ImageFormat.FIF_TIFF;
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

            SetChannel(albedoTex, alphaTex, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

            FreeImage_Save(PackingFormat, albedoTex, destinationPath);

            FreeImage_Unload(albedoTex);
            FreeImage_Unload(alphaTex);
        }
        
        public struct TextureChannel
        {
            [CanBeNull] public string Path;
            public bool Invert;
            public ChannelSource Source;
            public bool DefaultBlack;
        }

        private static void HandleTextureChannel(TextureChannel textureChannel, (int, int) widthHeight, IntPtr newImage, FREE_IMAGE_COLOR_CHANNEL newChannel)
        {
            if (textureChannel.Path is null)
            {
                if (!textureChannel.DefaultBlack)
                {
                    var ch = GetChannel(newImage, ChannelSourceToFreeImage(textureChannel.Source));
                    FillBackground(ch, Color.White);
                    SetChannel(newImage, ch, newChannel);
                }
                return;
            }
            
            var ptr = FreeImage_Load(textureChannel.Path);
            var size = GetWithAndHeight(ptr);

            if (textureChannel.Source != ChannelSource.Grayscale)
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
        
        public static void PackCustom(string destinationPath, TextureChannel textureChannelR, TextureChannel textureChannelG, TextureChannel textureChannelB, TextureChannel textureChannelA, (int, int) widthHeight)
        {
            IntPtr newTexture = Allocate(widthHeight.Item1, widthHeight.Item2, 32);

            HandleTextureChannel(textureChannelR, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
            HandleTextureChannel(textureChannelG, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_GREEN);
            HandleTextureChannel(textureChannelB, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
            HandleTextureChannel(textureChannelA, widthHeight, newTexture, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

            
            FreeImage_Save(PackingFormat, newTexture, destinationPath);

            FreeImage_Unload(newTexture);
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