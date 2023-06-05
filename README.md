# Shaders
A Standard Shader and Surface Shaders replacement for Unity for Built-In pipeline and forward rendering (VRChat)

##  Installation
### Unity Package Manager

- Have [git](https://git-scm.com/downloads) installed
- Open the Package Manager window `Window > Package Manager`
- Press + in the top left and package from git url
```
https://github.com/z3y/shaders.git
```

> To install a specific version add `#v0.0.0` to the end of the url with the version number  from the release labels

## Shader Features

| Feature | Description |
| - | - |
|Accurate PBR Shading | More accurate fresnel calculations and updated lighting functions from [Filament](https://github.com/google/filament) |
|Bakery Features| Mono SH, Lightmapped Specular, Non-Linear SH and Non-Linear Light Probe SH supported |
|Geometric Specular AA| Reduced specular shimmering |
|Antialiased Cutout | Alpha to Coverage |
|Lightmap Specular Occlusion| Lightmap intensity used for occluding reflection probes for Reduced reflections in dark areas|
|Bicubic Lightmap| Smoother lightmap at the small cost of sampling it multiple times and performing bilinear filering in the shader|
|Improved Parallax | Inreased Parallax steps count |
|Emission Multiply Base | Multiply Emission with Albedo|
|Emission GI Multiplier| Adjusts emission intensity used for baking lightmaps in the Meta pass |
|Transparency Modes | Cutout, Fade, Premultiply, Additive, Multiply|
|Non-Important lights per pixel| Cheaper realtime lights done in one pass. Does not work with lightmapped object |
|[LTCGI](https://github.com/PiMaker/ltcgi)||
|Box Projection on Quest||
|Anisotropy||

## [Scripted Surface Shaders](/Documentation~/ScriptedSurfaceShaders.md)

**This is now the recommended workflow**

You can create surface-like shaders `Create > Shader > Lit Shader Variant`. Having just one bloated uber shader will not fit everyone's workflow. With some basic shader knowledge you can create exactly what you need while still having all of the advanced lighting and shader features. Supports PC and Quest. 

Some additional features are included:

| Feature | Description |
| - | - |
|Screen-Space Reflections| |
|Bakery Alpha Meta| Surface inputs used in the shader are properly passed to the meta pass for baking instead of just reading the main texture and main color|
|Bakery Alpha Dither|Semi-Transparent baked shadows. Available only in Bakery L1 mode|
|Baked Area Light Specular Approximation| Reduced smoothness in areas where there is less directionality for more accurate lightmapped specular|
|Centroid Lightmap UVs|Fix for lightmap bleeds with very tight packing|

## Shader Graph
Most of the features supported in the modified shader graph version:

https://github.com/z3y/ShaderGraph 


## Included Tools 
### [Texture Packing](/Documentation~/TexturePacking.md)


#

### [Discord Support](https://discord.gg/bw46tKgRFT)
### [Bug Reports](https://github.com/z3y/shaders/issues)

### License
Core RP Licensed under the [Unity Companion License](/ShaderLibrary//CoreRP/LICENSE.md)

Some additional code from Google Filament Licensed under [Apache License 2.0](/ShaderLibrary//FilamentLicense.md)

The rest under MIT
