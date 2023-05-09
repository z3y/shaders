# Scripted Surface Shaders
To create a new shader variant use `Create > Shader > Lit Shader Variant`. The shader gets created from a template default shader. Some shader features are exposed in the importer UI and the rest can be defined in code.


## Syntax
The code syntax is very simple. It contains a few blocks that get copied over to the ShaderLab code.
To preview the generated ShaderLab code use `Copy Generated Shader` on the importer

| Block | Description |
| - | - |
|PROPERTIES| Properties get copied over into the Properties {} of the ShaderLab code. Some default properties are added before and after|
|DEFINES|Shader Features and Defines. Included after the CoreRP shader library and before all the code|
|CBUFFER| Declare all Material properties excluding textures. Copied over after importing the CoreRP library and code used for ligting |
|CODE| Contains texture declarations and override functions for parts of the vertex and fragment shader. All existing override functions are included in the template|


## Importer Shader Defines

The importer sets some additional useful defines

| Define | Description |
| - | - |
|VRCHAT_SDK|Defined if the VRChat SDK is imported in the project.|
|LTCGI_EXISTS|Defined if LTCGI is imported in the project. Used internally for disabling LTCGI.|


## Custom Interpolators