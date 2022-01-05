
Texture2D _DFG;
SamplerState sampler_DFG;

half4 SampleDFG(half NoV, half perceptualRoughness)
{
    return _DFG.Sample(sampler_DFG, float3(NoV, perceptualRoughness, 0));
}

half3 EnvBRDF(half2 dfg, half3 f0)
{
    return f0 * dfg.x + dfg.y;
}

half3 EnvBRDFMultiscatter(half2 dfg, half3 f0)
{
    return lerp(dfg.xxx, dfg.yyy, f0);
}

half3 EnvBRDFEnergyCompensation(half2 dfg, half3 f0)
{
    return 1.0 + f0 * (1.0 / dfg.y - 1.0);
}

half3 EnvBRDFApprox(half perceptualRoughness, half NoV, half3 f0)
{
    half g = 1 - perceptualRoughness;
    //https://blog.selfshadow.com/publications/s2013-shading-course/lazarov/s2013_pbs_black_ops_2_notes.pdf
    half4 t = half4(1 / 0.96, 0.475, (0.0275 - 0.25 * 0.04) / 0.96, 0.25);
    t *= half4(g, g, g, g);
    t += half4(0, 0, (0.015 - 0.75 * 0.04) / 0.96, 0.75);
    half a0 = t.x * min(t.y, exp2(-9.28 * NoV)) + t.z;
    half a1 = t.w;
    return saturate(lerp(a0, a1, f0));
}