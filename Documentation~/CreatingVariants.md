# Creating Shaders Variants

To create a new shader variant use `Create > Shader > Lit Shader Variant`. The shader gets created from a template default shader. Some shader features are exposed in the importer UI and the rest can be defined in code.

## Improvements over Unity Surface Shaders

- More Features
- Custom Interpolators
- Uses CoreRP library
- Not a Surface Shader

## Syntax

The code syntax is very simple. It contains a few blocks that get copied over to the ShaderLab code.
To preview the generated ShaderLab code use `Copy Generated Shader` on the importer

| Block | Description |
| - | - |
|PROPERTIES| Properties get copied over into the Properties {} of the ShaderLab code. Some default properties are added before and after|
|DEFINES|Shader Features and Defines. Included after the CoreRP shader library and before all the code|
|CBUFFER| Declare all Material properties excluding textures. Copied after importing the CoreRP library and code used for lighting |
|CODE| Contains texture declarations and override functions for parts of the vertex and fragment shader. All existing override functions are included in the template|

## Configuring VSCode

To have proper hlsl syntax highlighting you can set a language mode to be associated with this file extension. Bottom right click on "Plain Text" and "Set file association for .litshader" and select hlsl.

## Importer Shader Defines

The importer sets some additional useful defines.

> To update defines reimport all shaders `Tools > Lit > Reimport Shaders`

| Define | Description |
| - | - |
|VRCHAT_SDK|Defined if the VRChat SDK is imported in the project.|
|LTCGI_EXISTS|Defined if LTCGI is imported in the project. Used internally for disabling LTCGI.|
BUILD_TARGET_PC | Defined if the current platform is PC/Windows
BUILD_TARGET_ANDROID | Defined if the current platform is Android/Quest
BAKERY_INCLUDED | Same as the C# define, defined if bakery is imported in the project

## Including Other Shaders

It is possible to stack other shaders by including a `.litshader` outside of any code blocks. To stack an output of a previous function you can redefine it and access the previous function inside it. [Example](/Shaders/Samples/Stacked.litshader)

## Optional Includes

Using `#include_optional ""` instead of `#include ""` will only include the file if it exists so it doesnt cause errors. This also works on .litshader file types. With this it is possible to make a global include with code that will be included in all shaders, it works as stacked shader. The default template will include file at path `Assets/Settings/LitShaderConfig.litshader`.

If the include didnt exist at first, the shader will still register it as a dependency and automatically re-import it.

## Custom Interpolators

You can set fully custom varyings to access in the vertex and fragment shaders.
[Example](/Shaders/Samples/ShaderData.litshader#10)

```cpp
#define CUSTOM_VARYING0 float2 uvData : VARYING0;
...

void ModifyVaryings(Attributes attributes, VertexDescription description, inout Varyings varyings)
{
    varyings.uvData.xy = TRANSFORM_TEX(attributes.uv0.xy, _MainTex);
}

...

void SurfaceDescriptionFunction(Varyings IN, inout SurfaceDescription surface)
{
    float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
}
```
