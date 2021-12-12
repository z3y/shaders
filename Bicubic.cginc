// w0, w1, w2, and w3 are the four cubic B-spline basis functions
float w0(float a)
{
    //    return (1.0f/6.0f)*(-a*a*a + 3.0f*a*a - 3.0f*a + 1.0f);
    return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);   // optimized
}

float w1(float a)
{
    //    return (1.0f/6.0f)*(3.0f*a*a*a - 6.0f*a*a + 4.0f);
    return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
}

float w2(float a)
{
    //    return (1.0f/6.0f)*(-3.0f*a*a*a + 3.0f*a*a + 3.0f*a + 1.0f);
    return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
}

float w3(float a)
{
    return (1.0f/6.0f)*(a*a*a);
}

// g0 and g1 are the two amplitude functions
float g0(float a)
{
    return w0(a) + w1(a);
}

float g1(float a)
{
    return w2(a) + w3(a);
}

// h0 and h1 are the two offset functions
float h0(float a)
{
    // note +0.5 offset to compensate for CUDA linear filtering convention
    return -1.0f + w1(a) / (w0(a) + w1(a)) + 0.5f;
}

float h1(float a)
{
    return 1.0f + w3(a) / (w2(a) + w3(a)) + 0.5f;
}

//https://ndotl.wordpress.com/2018/08/29/baking-artifact-free-lightmaps
float3 tex2DFastBicubicLightmap(float2 uv, inout float4 bakedColorTex)
{
    #if defined(SHADER_API_D3D11) && defined(BICUBIC_LIGHTMAP)
    float width;
    float height;
    unity_Lightmap.GetDimensions(width, height);
    float x = uv.x * width;
    float y = uv.y * height;

    
    
    x -= 0.5f;
    y -= 0.5f;
    float px = floor(x);
    float py = floor(y);
    float fx = x - px;
    float fy = y - py;

    // note: we could store these functions in a lookup table texture, but maths is cheap
    float g0x = g0(fx);
    float g1x = g1(fx);
    float h0x = h0(fx);
    float h1x = h1(fx);
    float h0y = h0(fy);
    float h1y = h1(fy);

    float4 r = g0(fy) * ( g0x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h0x, py + h0y) * 1.0f/width)) +
                         g1x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h1x, py + h0y) * 1.0f/width))) +
                         g1(fy) * ( g0x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h0x, py + h1y) * 1.0f/width)) +
                         g1x * UNITY_SAMPLE_TEX2D(unity_Lightmap, (float2(px + h1x, py + h1y) * 1.0f/width)));
    bakedColorTex = r;
    return DecodeLightmap(r);
    #else
    bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, uv);
    return DecodeLightmap(bakedColorTex);
    #endif
}