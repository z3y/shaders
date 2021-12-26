#if defined(VERTEXLIGHT_ON) && defined(UNITY_PASS_FORWARDBASE)
struct VertexLightInformation
{
    float3 Direction[4];
    float3 ColorFalloff[4];
    float Attenuation[4];
};


void get4VertexLightsColFalloff(inout VertexLightInformation vLight, float3 worldPos, float3 normal)
{
    float3 lightColor = 0;
    float4 toLightX = unity_4LightPosX0 - worldPos.x;
    float4 toLightY = unity_4LightPosY0 - worldPos.y;
    float4 toLightZ = unity_4LightPosZ0 - worldPos.z;

    float4 lengthSq = 0;
    lengthSq += toLightX * toLightX;
    lengthSq += toLightY * toLightY;
    lengthSq += toLightZ * toLightZ;

    float4 atten = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
    float4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25));
    atten = min(atten, atten2 * atten2);

    vLight.ColorFalloff[0] = unity_LightColor[0] * atten.x;
    vLight.ColorFalloff[1] = unity_LightColor[1] * atten.y;
    vLight.ColorFalloff[2] = unity_LightColor[2] * atten.z;
    vLight.ColorFalloff[3] = unity_LightColor[3] * atten.w;

    vLight.Attenuation[0] = atten.x;
    vLight.Attenuation[1] = atten.y;
    vLight.Attenuation[2] = atten.z;
    vLight.Attenuation[3] = atten.w;
}

void getVertexLightsDir(inout VertexLightInformation vLights, float3 worldPos)
{
    float3 toLightX = float3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x);
    float3 toLightY = float3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y);
    float3 toLightZ = float3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z);
    float3 toLightW = float3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w);

    float3 dirX = toLightX - worldPos;
    float3 dirY = toLightY - worldPos;
    float3 dirZ = toLightZ - worldPos;
    float3 dirW = toLightW - worldPos;

    vLights.Direction[0] = normalize(dirX);
    vLights.Direction[1] = normalize(dirY);
    vLights.Direction[2] = normalize(dirZ);
    vLights.Direction[3] = normalize(dirW);
}

void InitVertexLightData(float3 worldPos, float3 worldNormal, inout VertexLightInformation vLights)
{
    get4VertexLightsColFalloff(vLights, worldPos, worldNormal);
    getVertexLightsDir(vLights, worldPos);
}
#endif


// Original code by Xiexe
// only returns the vertex light information struct and normalized direction, the rest is handled in the fragment shader
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