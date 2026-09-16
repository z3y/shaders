# Shader Config

Shader config is applied to all shader variants in the project created with this system. Same rules apply as for [Creating Variants](./creating-variants)

## Creating a config
- MenuItem `Tools > Lit > Create Config File`
- Created file will be selected in the project window
- Open and edit the file in a text editor

## Examples

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