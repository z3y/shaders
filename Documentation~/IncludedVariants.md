# Included Shader Variants

## Lit

## Triplanar

## Stochastic

## Pixel AA

Antialiased replacement for point filtering mode. Requires the texture to be set as bilinear

### With Pixel AA & Bilinear Filter:
![image](/Documentation~//Images/pixelaa.png)

### With Point Filter:
![image](/Documentation~//Images/no%20aa.png)

## Screen

![image](/Documentation~/Images/screen.png)

## Flipbook mode
### Creating a texture array from a gif
- Install the [UnityTexture2DArrayImportPipeline](https://github.com/pschraut/UnityTexture2DArrayImportPipeline).
    - This is very important if you want your texture array to be imported properly on Android (Quest), most other tools will not re-import the proper format for Android and cause more VRAM usage.
- Use ffmpeg to create PNGs from a gif `ffmpeg -i your_file.gif -vsync 0 frame%d.png`.
- Import the created PNGs in Unity and add them to a new texture array `Create > Texture2DArray` (You can lock the inspector and drag them all in at once).

## Default Shader