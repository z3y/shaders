using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Windows;
using Color = System.Drawing.Color;

namespace z3y
{
  public class FreeImage
  {
    public enum ImageFormat
    {
      FIF_UNKNOWN = -1,
      BMP = 0,
      ICO = 1,
      JPEG = 2,
      FIF_JNG = 3,
      FIF_KOALA = 4,
      FIF_LBM = 5,
      FIF_IFF = FIF_LBM,
      FIF_MNG = 6,
      FIF_PBM = 7,
      FIF_PBMRAW = 8,
      FIF_PCD = 9,
      FIF_PCX = 10,
      FIF_PGM = 11,
      FIF_PGMRAW = 12,
      FIF_PNG = 13,
      FIF_PPM = 14,
      FIF_PPMRAW = 15,
      FIF_RAS = 16,
      FIF_TARGA = 17,
      FIF_TIFF = 18,
      FIF_WBMP = 19,
      FIF_PSD = 20,
      FIF_CUT = 21,
      FIF_XBM = 22,
      FIF_XPM = 23,
      FIF_DDS = 24,
      FIF_GIF = 25,
      FIF_HDR = 26,
      FIF_FAXG3 = 27,
      FIF_SGI = 28,
      FIF_EXR = 29,
      FIF_J2K = 30,
      FIF_JP2 = 31,
      FIF_PFM = 32,
      FIF_PICT = 33,
      FIF_RAW = 34,
      FIF_WEBP = 35,
      FIF_JXR = 36
    }

    internal enum ImageType
    {
      FIT_UNKNOWN = 0,
      FIT_BITMAP = 1,
      FIT_UINT16 = 2,
      FIT_INT16 = 3,
      FIT_UINT32 = 4,
      FIT_INT32 = 5,
      FIT_FLOAT = 6,
      FIT_DOUBLE = 7,
      FIT_COMPLEX = 8,
      FIT_RGB16 = 9,
      FIT_RGBA16 = 10,
      FIT_RGBF = 11,
      FIT_RGBAF = 12
    }

    internal enum ColorType
    {
      FIC_MINISWHITE = 0,
      FIC_MINISBLACK = 1,
      FIC_RGB = 2,
      FIC_PALETTE = 3,
      FIC_RGBALPHA = 4,
      FIC_CMYK = 5
    }

    // https://freeimage.sourceforge.io/fnet/html/FA33955C.htm
    public enum FREE_IMAGE_COLOR_CHANNEL
    {
      FICC_RGB,
      FICC_RED,
      FICC_GREEN,
      FICC_BLUE,
      FICC_ALPHA,
      FICC_BLACK,
      FICC_REAL,
      FICC_IMAG,
      FICC_MAG,
      FICC_PHASE
    }

    // https://freeimage.sourceforge.io/fnet/html/A732273F.htm
    public enum FREE_IMAGE_COLOR_TYPE
    {
      FIC_MINISWHITE,
      FIC_MINISBLACK,
      FIC_RGB,
      FIC_PALETTE,
      FIC_RGBALPHA,
      FIC_CMYK,
    }

    public enum FREE_IMAGE_FILTER
    {
      FILTER_BOX,
      FILTER_BICUBIC,
      FILTER_BILINEAR,
      FILTER_BSPLINE,
      FILTER_CATMULLROM,
      FILTER_LANCZOS3
    }

    [Flags]
    public enum FREE_IMAGE_COLOR_DEPTH
    {
      FICD_UNKNOWN,
      FICD_AUTO,
      FICD_01_BPP,
      FICD_01_BPP_DITHER,
      FICD_01_BPP_THRESHOLD,
      FICD_04_BPP,
      FICD_08_BPP,
      FICD_16_BPP_555,
      FICD_16_BPP,
      FICD_24_BPP,
      FICD_32_BPP,
      FICD_REORDER_PALETTE,
      FICD_FORCE_GREYSCALE,
      FICD_COLOR_MASK
    }

    const string FreeImageLibrary = "FreeImage";

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_GetFIFFromFormat")]
    private static extern ImageFormat GetFIFFromFormat(string format);

    public static ImageFormat GetImageFormat(string extension)
    {
      if (extension.Equals("tga")) return ImageFormat.FIF_TARGA;
      
      return GetFIFFromFormat(extension);
    }

    public static ImageFormat GetImageFormatAtPath(string path)
    {
      var extension = System.IO.Path.GetExtension(path).Remove(0,1);
      return GetImageFormat(extension);
    }
    
    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_Load")]
    public static extern IntPtr FreeImage_Load(ImageFormat format, string filename, int flags = 0);

    public static IntPtr FreeImage_Load(string path, int flags = 0)
    {
      return FreeImage_Load(GetImageFormatAtPath(path), path, flags);
    }
         
    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_Unload")]
    public static extern void FreeImage_Unload(IntPtr handle);
    
    [DllImport(FreeImageLibrary,  EntryPoint = "FreeImage_FlipHorizontal")]
    public static extern bool FlipHorizontal(IntPtr dib);

    [DllImport(FreeImageLibrary,  EntryPoint = "FreeImage_AdjustContrast")]
    public static extern bool AdjustContrast(IntPtr dib, double percentage);

    [DllImport(FreeImageLibrary,  EntryPoint = "FreeImage_GetChannel")]
    public static extern IntPtr GetChannel(IntPtr dib, FREE_IMAGE_COLOR_CHANNEL channel);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_SetChannel")]
    public static extern bool SetChannel(IntPtr dib, IntPtr dib8, FREE_IMAGE_COLOR_CHANNEL channel);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_Save")]
    public static extern bool FreeImage_Save(ImageFormat format, IntPtr handle, string filename, int flags = 0);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_FillBackground")]
    public static extern bool FillBackground(IntPtr dib, Color color, int options = 0);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_Rescale")]
    public static extern IntPtr Rescale(IntPtr dib, int dst_width, int dst_height, FREE_IMAGE_FILTER filter);

    public delegate void OutputMessageFunction(ImageFormat fif, string message);
    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_SetOutputMessage")]
    public static extern void SetOutputMessage(OutputMessageFunction messageFunction);
    
    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_GetWidth")]
    internal static extern uint GetWidth(IntPtr handle);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_GetHeight")]
    internal static extern uint GetHeight(IntPtr handle);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_GetImageType")]
    internal static extern ImageType GetImageType(IntPtr dib);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_SetTransparent")]
    public static extern void SetTransparent(IntPtr dib, bool enabled = true);

    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_SetTransparentIndex")]
    public static extern void SetTransparentIndex(IntPtr dib, int index = 3);
    
    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_IsTransparent")]
    public static extern bool IsTransparent(IntPtr dib);
    
    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_ConvertTo32Bits")]
    public static extern IntPtr ConvertTo32Bits(IntPtr dib);
    
    [DllImport(FreeImageLibrary, EntryPoint = "FreeImage_Invert")]
    public static extern bool Invert(IntPtr dib);


    public static (uint, uint) GetWithAndHeight(IntPtr dib)
    {
      uint width = GetWidth(dib);
      uint height = GetHeight(dib);

      return (width, height);
    }
  }
}