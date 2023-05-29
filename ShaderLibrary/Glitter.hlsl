float lilNsqDistance(float2 a, float2 b)
{
    return dot(a-b,a-b);
}

// this is wip and the fucntion will change
half3 Glitter(float2 uv, float3 normalWS, float3 viewDirectionWS, float3 lightDirection, float4 glitterParams1, float4 glitterParams2, float glitterPostContrast, bool AntiAliasing, bool randomColor = false)
{
    // glitterParams1
    // x: Scale, y: Scale, z: Size, w: Contrast
    // glitterParams2
    // x: Speed, y: Angle, z: Light Direction, w: 
    float2 pos = abs(uv * glitterParams1.xy + glitterParams1.xy);

    #if defined(SHADER_API_D3D9) || defined(SHADER_API_D3D11_9X)
        #define M1 46203.4357
        #define M2 21091.5327
        #define M3 35771.1966
        float2 q = trunc(pos);
        float4 q2 = float4(q.x, q.y, q.x+1, q.y+1);
        float3 noise0 = frac(sin(dot(q2.xy,float2(12.9898,78.233))) * float3(M1, M2, M3));
        float3 noise1 = frac(sin(dot(q2.zy,float2(12.9898,78.233))) * float3(M1, M2, M3));
        float3 noise2 = frac(sin(dot(q2.xw,float2(12.9898,78.233))) * float3(M1, M2, M3));
        float3 noise3 = frac(sin(dot(q2.zw,float2(12.9898,78.233))) * float3(M1, M2, M3));
    #else
        // Hash
        // https://www.shadertoy.com/view/MdcfDj
        #define M1 1597334677U
        #define M2 3812015801U
        #define M3 2912667907U
        uint2 q = (uint2)pos;
        uint4 q2 = uint4(q.x, q.y, q.x+1, q.y+1) * uint4(M1, M2, M1, M2);
        uint3 n0 = (q2.x ^ q2.y) * uint3(M1, M2, M3);
        uint3 n1 = (q2.z ^ q2.y) * uint3(M1, M2, M3);
        uint3 n2 = (q2.x ^ q2.w) * uint3(M1, M2, M3);
        uint3 n3 = (q2.z ^ q2.w) * uint3(M1, M2, M3);
        float3 noise0 = float3(n0) * (1.0/float(0xffffffffU));
        float3 noise1 = float3(n1) * (1.0/float(0xffffffffU));
        float3 noise2 = float3(n2) * (1.0/float(0xffffffffU));
        float3 noise3 = float3(n3) * (1.0/float(0xffffffffU));
    #endif

    // Get the nearest position
    float4 fracpos = frac(pos).xyxy + float4(0.5,0.5,-0.5,-0.5);
    float4 dist4 = float4(lilNsqDistance(fracpos.xy,noise0.xy), lilNsqDistance(fracpos.zy,noise1.xy), lilNsqDistance(fracpos.xw,noise2.xy), lilNsqDistance(fracpos.zw,noise3.xy));
    float4 near0 = dist4.x < dist4.y ? float4(noise0,dist4.x) : float4(noise1,dist4.y);
    float4 near1 = dist4.z < dist4.w ? float4(noise2,dist4.z) : float4(noise3,dist4.w);
    float4 near = near0.w < near1.w ? near0 : near1;

    #define GLITTER_DEBUG_MODE 0
    #define GLITTER_ANTIALIAS 1

    #if GLITTER_DEBUG_MODE == 1
        // Voronoi
        return near.x;
    #else
        // Glitter
        float3 glitterNormal = abs(frac(near.xyz*14.274 + _Time.x * glitterParams2.x) * 2.0 - 1.0);
        glitterNormal = normalize(glitterNormal * 2.0 - 1.0);
        float glitter = dot(glitterNormal, viewDirectionWS);
        glitter = saturate(1.0 - (glitter * glitterParams1.w + glitterParams1.w));
        glitter = pow(glitter, glitterPostContrast);
        // Circle
        if (AntiAliasing)
            glitter *= saturate((glitterParams1.z-near.w) / fwidth(near.w));
        else
            glitter = near.w < glitterParams1.z ? glitter : 0.0;

        // Angle
        float3 halfDirection = normalize(viewDirectionWS + lightDirection * glitterParams2.z);
        float nh = saturate(dot(normalWS, halfDirection));
        glitter = saturate(glitter * saturate(nh * glitterParams2.y + 1.0 - glitterParams2.y));
        // Random Color
        half3 glitterColor;
        if (randomColor)
        {
            glitterColor = glitter - glitter * frac(near.xyz*278.436) * glitterParams2.w;
        }
        else
        {
            glitterColor = glitter * glitterParams2.w;
        }
        return glitterColor;
    #endif
}

/*
MIT License

Copyright (c) 2020-2021 lilxyzw

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/