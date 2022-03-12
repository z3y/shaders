#ifndef NONIMPORTANTLIGHTS_PERPIXEL_INCLUDED
#define NONIMPORTANTLIGHTS_PERPIXEL_INCLUDED
#if defined(VERTEXLIGHT_ON) && defined(UNITY_PASS_FORWARDBASE)

void NonImportantLightsPerPixel(inout half3 lightColor, inout half3 directSpecular, float3 positionWS, float3 normalWS, float3 viewDir, half NoV, half3 f0, half clampedRoughness)
{
    float4 toLightX = unity_4LightPosX0 - positionWS.x;
    float4 toLightY = unity_4LightPosY0 - positionWS.y;
    float4 toLightZ = unity_4LightPosZ0 - positionWS.z;

    float4 lengthSq = 0.0;
    lengthSq += toLightX * toLightX;
    lengthSq += toLightY * toLightY;
    lengthSq += toLightZ * toLightZ;

    float4 attenuation = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
    float4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25.0));
    attenuation = min(attenuation, atten2 * atten2);

    [unroll(4)]
    for(uint i = 0; i < 4; i++)
    {
        UNITY_BRANCH
        if (attenuation[i] > 0.0)
        {
            float3 direction = normalize(float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]) - positionWS);
            half NoL = saturate(dot(normalWS, direction));
            half3 color = NoL * attenuation[i] * unity_LightColor[i];
            lightColor += color;

            #ifndef SPECULAR_HIGHLIGHTS_OFF
                float3 halfVector = Unity_SafeNormalize(direction + viewDir);
                half vNoH = saturate(dot(normalWS, halfVector));
                half vLoH = saturate(dot(direction, halfVector));

                half3 Fv = F_Schlick(vLoH, f0);
                half Dv = D_GGX(vNoH, clampedRoughness);
                half Vv = V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
                directSpecular += max(0.0, (Dv * Vv) * Fv) * color * UNITY_PI;
            #endif
        }
    }
}



// Original code by Xiexe
// https://github.com/Xiexe/Xiexes-Unity-Shaders

// MIT License

// Copyright (c) 2019 Xiexe

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
#endif
#endif