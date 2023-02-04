struct SurfaceData
{
    half3 albedo;
    half3 tangentNormal;
    half3 emission;
    half metallic;
    half perceptualRoughness;
    half occlusion;
    half reflectance;
    half alpha;
    half3 anisotropyTangent;
    half anisotropyLevel;
    half anisotropyDirection;
};

void InitializeDefaultSurfaceData(inout SurfaceData surf)
{
    surf.albedo = 1.0;
    surf.tangentNormal = half3(0,0,1);
    surf.emission = 0.0;
    surf.metallic = 0.0;
    surf.perceptualRoughness = 0.0;
    surf.occlusion = 1.0;
    surf.reflectance = 0.5;
    surf.alpha = 1.0;
    surf.anisotropyTangent = 0.0;
    surf.anisotropyLevel = 0.0;
    surf.anisotropyDirection = 0.0;
}