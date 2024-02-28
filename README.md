# Shaders

A Standard Shader and Surface Shaders replacement for Unity's Built-In pipeline and forward rendering in linear color space, created for use in VRChat.

Now you can also create more customizable shaders with similar features using my own node shader editor https://github.com/z3y/ShaderGraphZ

## Installation

### Using VRChat Creator Companion
-  VCC https://z3y.github.io/vpm-package-listing/

### Using git:

```
https://github.com/z3y/shaders.git
```
## Features

| Feature | Description |
| - | - |
|Accurate PBR Shading | More accurate Fresnel calculations and updated lighting functions from [Filament](https://github.com/google/filament) |
|Bakery Features| Mono SH, Lightmapped Specular, Non-Linear SH and Non-Linear Light Probe SH supported |
|Geometric Specular AA| Reduced specular shimmering |
|Antialiased Cutout | Alpha to Coverage |
|Lightmap Specular Occlusion| Lightmap intensity used for occluding reflection probes for reduced reflections in dark areas|
|Bicubic Lightmap| Smoother lightmap at the small cost of sampling it multiple times and performing bicubic filtering in the shader|
|Improved Parallax | Increased Parallax steps count |
|Emission Multiply Base | Multiply Emission with Albedo|
|Emission GI Multiplier| Adjusts emission intensity used for baking lightmaps in the Meta pass |
|Transparency Modes | Cutout, Fade, Premultiply, Additive, Multiply|
|Non-Important lights per pixel| Cheaper real-time lights done in one pass. Does not work with lightmapped object |
|Bakery Alpha Dither|Semi-Transparent baked shadows. Available only in Bakery L1 mode|
|Baked Area Light Specular Approximation| Reduced smoothness in areas where there is less directionality for more accurate lightmapped specular|
|Centroid Lightmap UVs|Fix for lightmap bleeding with very tight packing|
|[LTCGI](https://github.com/PiMaker/ltcgi), [Area Lit](https://booth.pm/en/items/3661829)|Realtime Area lights|
|Box Projection on Quest| Force enables box projection even if it's disabled in Unity|
|Anisotropy| Supports Tangent maps|
|[Shader Config](/Documentation~/ScriptedSurfaceShaders.md#config-example)| A very customizable way of enabling/disabling existing features and adding new ones to all included shaders|
|Texture Packing| A tool for texture packing included: `Lit > Texture Packing` and in the material inspector for packed textures |
|Screen-Space Reflections| |

## [Scripted Surface Shaders](/Documentation~/CreatingVariants.md)

You can create surface-like shaders `Create > Shader > Lit Shader Variant`. Having just one shader will not fit everyone's workflow. With some basic shader knowledge you can create exactly what you need while still having all the advanced lighting and shader features.

The default shader created with this system is `Lit v3` and other variants included at `Lit Variants/`

Supports PC and Quest.

## [Documentation](/Documentation~/Documentation.md)

## License

[MIT](/LICENSE.md)

Some additional code from Google Filament Licensed under [Apache License 2.0](/ShaderLibrary/FilamentLicense.md)



[Patreon](https://www.patreon.com/z3y) |
[Bug Reports](https://github.com/z3y/shaders/issues) |
[Discord Support](https://discord.gg/bw46tKgRFT)
