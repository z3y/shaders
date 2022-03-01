#define ParallaxSteps 20
float _ParallaxOffset;
float _Parallax;
Texture2D _ParallaxMap;

float3 CalculateTangentViewDir(float3 tangentViewDir)
{
    tangentViewDir = Unity_SafeNormalize(tangentViewDir);
    tangentViewDir.xy /= (tangentViewDir.z + 0.42);
	return tangentViewDir;
}

float2 ParallaxOffsetMultiStep(float surfaceHeight, float strength, float2 uv, float3 tangentViewDir)
{
	float stepSize = 1.0 / ParallaxSteps;
    float3 uvDelta_stepSize = float3(tangentViewDir.xy * (stepSize * strength), stepSize);
    float3 uvOffset_stepHeight = float3(float2(0, 0), 1.0);

    [unroll(ParallaxSteps)]
    for (int j = 0; j < ParallaxSteps; j++)
    {
        UNITY_BRANCH
        if (uvOffset_stepHeight.z > surfaceHeight)
        {
            uvOffset_stepHeight -= uvDelta_stepSize;
            surfaceHeight = _ParallaxMap.Sample(sampler_MainTex, (uv + uvOffset_stepHeight.xy)) + _ParallaxOffset;
        }
    }

    [unroll(3)]
    for (int k = 0; k < 3; k++)
    {
        uvDelta_stepSize *= 0.5;
        uvOffset_stepHeight += uvDelta_stepSize * ((uvOffset_stepHeight.z < surfaceHeight) * 2.0 - 1.0);
        surfaceHeight = _ParallaxMap.Sample(sampler_MainTex, (uv + uvOffset_stepHeight.xy)) + _ParallaxOffset;
    }

    return uvOffset_stepHeight.xy;
}

float2 ParallaxOffset (float3 viewDirForParallax, float2 parallaxUV)
{
    viewDirForParallax = CalculateTangentViewDir(viewDirForParallax);
    float h = _ParallaxMap.Sample(sampler_MainTex, parallaxUV);
    h = clamp(h, 0.0, 0.999);
    float2 offset = ParallaxOffsetMultiStep(h, _Parallax, parallaxUV, viewDirForParallax);

	return offset;
}


float2 ParallaxOffsetOneStep(half depth, float3 tangentViewDir)
{
    float3 uvDelta_stepSize = float3(tangentViewDir.xy * depth, 1);
    float3 uvOffset_stepHeight = float3(float2(0, 0), 1.0);
    uvOffset_stepHeight -= uvDelta_stepSize;
        
    return uvOffset_stepHeight.xy;
}
float2 ParallaxOffsetUV (half depth, float3 viewDirForParallax)
{
    viewDirForParallax = CalculateTangentViewDir(viewDirForParallax);
    float2 offset = ParallaxOffsetOneStep(depth, viewDirForParallax);

	return offset;
}



// parallax from mochie
// https://github.com/MochiesCode/Mochies-Unity-Shaders/blob/7d48f101d04dac11bd4702586ee838ca669f426b/Mochie/Standard%20Shader/MochieStandardParallax.cginc#L13
// MIT License

// Copyright (c) 2020 MochiesCode

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.