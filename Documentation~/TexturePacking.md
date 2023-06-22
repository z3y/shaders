# Texture Packing

## Difference from other packing tools

This tool does not reduce the image quality while packing. It reads textures from the source file using the native image library (FreeImage) that Unity uses for importing textures. This avoids using the already compressed imported texture and unity methods for packing. It can cause issues with certain image formats, but most are supported. The preview might also not be accurate if the importer has already modified it (Applied sRGB, imported as Normal Map etc.)

> By default the packed texture will be saved in the same folder as source textures.

## Packing Fields

Each packing field represents a destination channel in the packed texture.

| Property | Description |
| - | - |
|Channel Selection (Red, Green, Blue, Alpha) | Specifies the texture channel that will be used from the selected texture. |
| Invert | Invert the color (1-color). |
| Fallback (Black, White) | If no texture is selected use this color instead. |

## Packing Options

| Property | Description |
| - | - |
|Format|Encode format for the packed texture. The default .tga option is selected for the fastest import time|
|Rescale Filter| Filter used for all source textures if they require resizing|
|Size|Default size will use the imported texture size of one of the properties. The order of importance is GARB.
|Linear | Toggles off sRGB on the imported texture. Used for data textures (Smoothness, Metallic, Occlusion etc.) In most cases this should be enabled, unless you are packing an albedo map and alpha. Disabling sRGB manually after packing on the texture importer does the same thing

## Packing Window

MenuItem `Tools > Lit > Texture Packing`

![image](https://i.imgur.com/Rc1e8qM.png)

## Packing in the material

Some material properties have a button for packing (P). This will open the packing window with a preset and set destination channel names accordingly for easier packing.

![image](https://i.imgur.com/uGTb1Io.png)



