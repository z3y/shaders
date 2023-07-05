# Documentation

## [Texture Packing](/Documentation~/TexturePacking.md)

## [Included Variants](/Documentation~/CreatingVariants.md)

## [Creating Variants](/Documentation~/IncludedVariants.md)

## Shader Config

Create a default config `Tools > Lit > Create Config File` and open it. The options on the importer itself are not used. To apply it after editing reload (Ctrl R).

> Enabling Mono SH globally

```cpp
DEFINES_START
    #define BAKERY_MONOSH // force enable mono sh on all shader variants
DEFINES_END
```

> Global brightness slider

```cpp
CBUFFER_START
    half _UdonBrightness; // global property set with udon
CBUFFER_END

CODE_START
    // Unique function name
    void ModifyFinalColorGlobalBrightness(inout half4 finalColor, GIData giData, Varyings unpacked, ShaderData sd, SurfaceDescription surfaceDescription)
    {
        #ifdef USE_MODIFYFINALCOLOR
            // access the previous function and pass in all the same parameters if it exists
            ModifyFinalColor(finalColor, giData, unpacked, sd, surfaceDescription);
        #endif

        finalColor *= _UdonBrightness;
    }
    // override it
    #define USE_MODIFYFINALCOLOR
    #define ModifyFinalColor ModifyFinalColorGlobalBrightness
CODE_END
```
More info at [Creating Shader Variants](/Documentation~/CreatingVariants.md).

## Material Description

Add `SetupLitShader` to the model importer and switch the Material Creation Mode to MaterialDecription to setup materials with the default shader. The roughness, metallic, color and emission values, transparency, normal map and albedo map will be transferred properly from Blender materials.

![Image](/Documentation~/Images/MaterialDescription.png)

![Image](/Documentation~/Images/label.png)

## LTCGI and Area Lit

LTCGI and Area Lit toggles are automatically added if found in the project.