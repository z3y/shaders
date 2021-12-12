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
};

void InitializeDefaultSurfaceData(inout SurfaceData surf)
{
    surf.albedo = 1;
    surf.tangentNormal = half3(0,0,1);
    surf.emission = 0;
    surf.metallic = 0;
    surf.perceptualRoughness = 0;
    surf.occlusion = 1;
    surf.reflectance = 0.5;
    surf.alpha = 1;
}