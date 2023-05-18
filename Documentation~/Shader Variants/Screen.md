# Screen

## Creating a texture array from a gif
- Install the [UnityTexture2DArrayImportPipeline](https://github.com/pschraut/UnityTexture2DArrayImportPipeline). This is very important if you want your textue array to be imported properly on Quest, most other tools will not re-import the proper format for Android and cause more VRAM usage
- Use ffmpeg to create PNGs from a gif `ffmpeg -i your_file.gif -vsync 0 frame%d.png`
- Import the created PNGs in Unity and add them to a new texture array `Create > Texture2DArray`. You can lock the inspector and drag them all in at once


![image](/Documentation~/Images/screen.png)