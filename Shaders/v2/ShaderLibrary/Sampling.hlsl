#ifndef SAMPLING_INCLUDED
#define SAMPLING_INCLUDED

float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias)
{
    float2 ddx_ = scale * uvdx;
    float2 ddy_ = scale * uvdy;
    float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));

    return max(0.5 * log2(d) - bias, 0.0);
}

float ComputeTextureLOD(float2 uv)
{
    float2 ddx_ = ddx(uv);
    float2 ddy_ = ddy(uv);

    return ComputeTextureLOD(ddx_, ddy_, 1.0, 0.0);
}

float2 GetMinUvSize(float2 baseUV, float4 texelSize)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

    minUvSize = min(baseUV * texelSize.zw, minUvSize);

    return minUvSize;
}

float4 GetTexelSize(Texture2D t)
{
    float4 texelSize;
    t.GetDimensions(texelSize.x, texelSize.y);
    texelSize.zw = 1.0 / texelSize.xy;
    return texelSize;
}

// https://ndotl.wordpress.com/2018/08/29/baking-artifact-free-lightmaps
// bicubicw0, bicubicw1, bicubicw2, and bicubicw3 are the four cubic B-spline basis functions
float bicubicw0(float a)
{
    return (1.0f/6.0f)*(a*(a*(-a + 3.0f) - 3.0f) + 1.0f);   // optimized
}

float bicubicw1(float a)
{
    return (1.0f/6.0f)*(a*a*(3.0f*a - 6.0f) + 4.0f);
}

float bicubicw2(float a)
{
    return (1.0f/6.0f)*(a*(a*(-3.0f*a + 3.0f) + 3.0f) + 1.0f);
}

float bicubicw3(float a)
{
    return (1.0f/6.0f)*(a*a*a);
}

// bicubicg0 and bicubicg1 are the two amplitude functions
float bicubicg0(float a)
{
    return bicubicw0(a) + bicubicw1(a);
}

float bicubicg1(float a)
{
    return bicubicw2(a) + bicubicw3(a);
}

// bicubich0 and bicubich1 are the two offset functions
float bicubich0(float a)
{
    // note +0.5 offset to compensate for CUDA linear filtering convention
    return -1.0f + bicubicw1(a) / (bicubicw0(a) + bicubicw1(a)) + 0.5f;
}

float bicubich1(float a)
{
    return 1.0f + bicubicw3(a) / (bicubicw2(a) + bicubicw3(a)) + 0.5f;
}

half4 SampleBicubic(Texture2D t, SamplerState s, float2 uv, float4 texelSize, float lod = 0)
{
    #if defined(SHADER_API_MOBILE) || !defined(_BICUBICLIGHTMAP)
        return t.SampleLevel(s, uv, lod);
    #else
        float2 xy = uv * texelSize.xy - 0.5;
        float2 pxy = floor(xy);
        float2 fxy = xy - pxy;

        // note: we could store these functions in a lookup table texture, but maths is cheap
        float bicubicg0x = bicubicg0(fxy.x);
        float bicubicg1x = bicubicg1(fxy.x);
        float bicubich0x = bicubich0(fxy.x);
        float bicubich1x = bicubich1(fxy.x);
        float bicubich0y = bicubich0(fxy.y);
        float bicubich1y = bicubich1(fxy.y);

        //float lod = ComputeTextureLOD(uv);

        float4 t0 = bicubicg0x * t.SampleLevel(s, float2(pxy.x + bicubich0x, pxy.y + bicubich0y) * texelSize.zw, lod);
        float4 t1 = bicubicg1x * t.SampleLevel(s, float2(pxy.x + bicubich1x, pxy.y + bicubich0y) * texelSize.zw, lod);
        float4 t2 = bicubicg0x * t.SampleLevel(s, float2(pxy.x + bicubich0x, pxy.y + bicubich1y) * texelSize.zw, lod);
        float4 t3 = bicubicg1x * t.SampleLevel(s, float2(pxy.x + bicubich1x, pxy.y + bicubich1y) * texelSize.zw, lod);

        return bicubicg0(fxy.y) * (t0 + t1) + bicubicg1(fxy.y) * (t2 + t3);
    #endif
}
#endif