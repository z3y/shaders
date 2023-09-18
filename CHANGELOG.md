# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
## [3.2.9] - 2023-09-18
- Removed bakery meta pass because of baking performance issues
- Fixed bump scale wrong property name in the layered shader
- Fixed shadowmask, subtractive, directional shadow fade issues
- Fixed some macro redefinition warnings

## [3.2.8] - 2023-08-31
## Added
- Support for using custom drawers in the inspector
- Refactored Importer Code

## [3.2.7] - 2023-07-20
## Added
- New automatic PBR material setup with texture packing
    - Right-click on a texture or a folder `Create > Material with PBR Setup (Lit)`
    - Matches PBR textures based on their suffix
    - Applies them to a material and opens the packing window to confirm packing
    - Folder should only contain PBR textures for one material
- New directional specular occlusion
    - Occludes areas based on the baked light intensity coming from the reflected direction
    - Better occlusion, similar to bent normal maps, by using data already stored in SH
    - Surfaces can have varying occlusion based on the view direction and light intensity coming from that direction
    - Supports Mono-SH and light probes
    - Currently Disabled by default - create a config to enable
- Increased specular occlusion range
- Specular occlusion is now also affected by the real-time directional light and light probes
- Created a Terrain shader variant for the Unity Terrain
    - Supports Mask Map
    - Adjustable height blending (Mask Map Blue channel)
    - Doesn't work with more than 4 layers currently
    - Possible bugs since I avoid using Unity terrain
- Ported over `SurfaceDescriptionInputs` and `VertexDescriptionInputs` functions from Shader Graph for getting shader data when creating new shaders in [Structs](/ShaderLibrary/Structs.hlsl)

## [3.2.6] - 2023-07-03
### Added
- Added package listing for [VCC](https://z3y.github.io/vpm-package-listing/)
    - Requires VCC v2.1.2

## [3.2.5] - 2023-07-01
### Added
- Added a [Changelog](/CHANGELOG.md).
- Area Lit and LTCGI are now automatically detected when imported, without needing to manually reimport all shaders.

## [3.2.4] - 2023-06-30
### Fixed
- Removed "Legacy" from the v2 shader name to stop VRChat from replacing it in game.

## [3.2.3] - 2023-06-26
### Fixed
- Fixed an error in logs when using the new pack button
- Inspector now updates the keyword after using the pack button.
### Added
- Added more options in the default config file.

## [3.2.2] - 2023-06-24
### Added
- Added a texture packing button in the new inspector.
### Changed
- Clean up renamed and moved the old shader version to `Lit Variants/Legacy/Lit v2` and disabled creating project settings for this shader in new projects to avoid confusion as its not being used and it was not as well implemented as the new config. This way projects using the old version will continue working.

## [3.2.1] - 2023-06-20
### Added
- Added a Stochastic shader variant at Lit Variants/Stochastic with 2 options:
    - Simple one with no pre-processing required, less contrasty looking than without stochastic.
    - Advanced one from https://github.com/UnityLabs/procedural-stochastic-texturing that requires texture preprocessing, with improved contrast and more accurate representation of the original textures.

## [3.2.0] - 2023-06-18
### Added
- Added support for Area Lit
- To redetect if Area Lit is included `Tools > Lit > Reimport Shaders`
- Added a [relative include path](https://github.com/z3y/shaders/commit/86db5baaea2a598609a573b4db9cbb6012c322b7#diff-258aff6eecb91a9b210767e5ad30e1b66ca0a909c759610e6754dd3bd101e328) to the ShaderLibrary with <> instead of full path with ""

## [3.1.8] - 2023-06-14
### Fixed
- Implemented proper branching for triplanar

## [3.1.5] - 2023-06-12
### Added
- Added a new way to set completely custom varyings: [Example](https://github.com/z3y/shaders/blob/main/Documentation~/ScriptedSurfaceShaders.md#custom-interpolators-1)
- Added hlsl syntax highlighting on github for the custom file type
### Fixed
- Fixed LTCGI lightmap uv that affected ltcgi shadow maps - with the optimizations of the new system the keyword now needs to affect the vertex shader too in order to pass the raw uv2 varying

## [3.1.4] - 2023-06-05
### Added
- Triplanar shader (UDN and RNM blending)
- Support for outputting normals in tangent space, world space or object space
- Implemented hlsl line directive for proper error handling. Errors in custom shaders will now show the correct line and file name
- Added a menu item for reimporting all shaders and creating the global config file
- Included an example shader for getting different shader data
### Changed
- Included CommonMaterial.hlsl by default
### Fixed
- Fixed a bug in the meta pass

## [3.1.3] - 2023-05-29
### Added
- Added the wind feature in world space so it doesnt break with static batching
- Added a label `SetupLitShader` for setting up blender materials in the model importer. This is a Replacement for the MaterialDescription shader toggle in the global config - [Material Description](https://github.com/z3y/shaders/blob/main/Documentation~/ScriptedSurfaceShaders.md#material-description)
### Changed
- Renamed the new version of the main shader to `Lit v3`

## [3.1.2] - 2023-05-19
### Added
- Completely new inspector with attributes to control the appearance

- Added some variants included by default (work in progress):
    - Lit (replacement for the main shader created with the new system)
    - Layered (2 layers blended with a mask)
    - Pixel AA
    - Screen (no subpixel layout patterns included yet)
### Changed
- Use SH per vertex on Android and full per pixel on PC

## [3.1.1] - 2023-05-15
### Added
- Added [Documentation](https://github.com/z3y/shaders/tree/main/Documentation~)
- Added Approximate Area Light Lightmapped Specular
### Changed
- Updated default template shader to use different packing. This will be used on all new variants created in the future because its more efficient (Occlusion, Roughness, Metallic). While this is not a standard in unity it is better the alpha channel is not used and the texture size could be halved (DXT1 instead of DXT5). It only affects newly created shaders
- Lightmap UVs are now using centroid interpolation
- Reworked the code for importer shader generation
- Exclude LTCGI and SSR from getting lightmap specular occlusion
- Reduced a lot of variants that will most likely not be used on Quest
- Set default texture packing alpha source to red
### Fixed
- Fixed box projection on Android/Quest

## [3.1.0] - 2023-03-07
### Added
- Added screen-space reflections
- Added lod crossfade
### Fixed
- Fixed ltcgi lightmap uv coordinates

## [3.0.0] - 2023-02-04
### Added
- You can now create surface like shaders `Create > Shader > Lit Shader Variant` using the same lighting as the main shader
    - Supports custom varyings, centroid and nointerp varyings, stacking
    - Examples included in the Tests folder