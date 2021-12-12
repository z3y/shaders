#if defined(VERTEXLIGHT_ON) && defined(UNITY_PASS_FORWARDBASE)
struct VertexLightInformation
{
    float3 Direction[4];
    half3 ColorFalloff[4];
    half Attenuation[4];
};
static VertexLightInformation vertexLightInformation;

//Returns the average color of all lights and writes to a struct contraining individual colors
half3 get4VertexLightsColFalloff(inout VertexLightInformation vLight, half3 worldPos, half3 normal, inout half4 vertexLightAtten)
{
    half3 lightColor = 0;
    #if defined(VERTEXLIGHT_ON)
        half4 toLightX = unity_4LightPosX0 - worldPos.x;
        half4 toLightY = unity_4LightPosY0 - worldPos.y;
        half4 toLightZ = unity_4LightPosZ0 - worldPos.z;

        half4 lengthSq = 0;
        lengthSq += toLightX * toLightX;
        lengthSq += toLightY * toLightY;
        lengthSq += toLightZ * toLightZ;

        half4 atten = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
        half4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25));
        atten = min(atten, atten2 * atten2);
        // Cleaner, nicer looking falloff. Also prevents the "Snapping in" effect that Unity's normal integration of vertex lights has.
        vertexLightAtten = atten;

        lightColor.rgb += unity_LightColor[0] * atten.x;
        lightColor.rgb += unity_LightColor[1] * atten.y;
        lightColor.rgb += unity_LightColor[2] * atten.z;
        lightColor.rgb += unity_LightColor[3] * atten.w;

        vLight.ColorFalloff[0] = unity_LightColor[0] * atten.x;
        vLight.ColorFalloff[1] = unity_LightColor[1] * atten.y;
        vLight.ColorFalloff[2] = unity_LightColor[2] * atten.z;
        vLight.ColorFalloff[3] = unity_LightColor[3] * atten.w;

        vLight.Attenuation[0] = atten.x;
        vLight.Attenuation[1] = atten.y;
        vLight.Attenuation[2] = atten.z;
        vLight.Attenuation[3] = atten.w;
    #endif
    return lightColor;
}

//Returns the average direction of all lights and writes to a struct contraining individual directions
half3 getVertexLightsDir(inout VertexLightInformation vLights, half3 worldPos, half4 vertexLightAtten)
{
    half3 dir = half3(0,0,0);
    half3 toLightX = half3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x);
    half3 toLightY = half3(unity_4LightPosX0.y, unity_4LightPosY0.y, unity_4LightPosZ0.y);
    half3 toLightZ = half3(unity_4LightPosX0.z, unity_4LightPosY0.z, unity_4LightPosZ0.z);
    half3 toLightW = half3(unity_4LightPosX0.w, unity_4LightPosY0.w, unity_4LightPosZ0.w);

    half3 dirX = toLightX - worldPos;
    half3 dirY = toLightY - worldPos;
    half3 dirZ = toLightZ - worldPos;
    half3 dirW = toLightW - worldPos;

    dirX *= length(toLightX) * vertexLightAtten.x;
    dirY *= length(toLightY) * vertexLightAtten.y;
    dirZ *= length(toLightZ) * vertexLightAtten.z;
    dirW *= length(toLightW) * vertexLightAtten.w;

    vLights.Direction[0] = dirX;
    vLights.Direction[1] = dirY;
    vLights.Direction[2] = dirZ;
    vLights.Direction[3] = dirW;

    dir = (dirX + dirY + dirZ + dirW) / 4;
    return dir;
}

void initVertexLights(half3 worldPos, half3 worldNormal, inout half3 vLight, inout half3 vertexLightColor)
{
    half3 vertexLightData = 0;
    half4 vertexLightAtten = half4(0,0,0,0);
    vertexLightColor = get4VertexLightsColFalloff(vertexLightInformation, worldPos, worldNormal, vertexLightAtten);
    half3 vertexLightDir = getVertexLightsDir(vertexLightInformation, worldPos, vertexLightAtten);
    [unroll(4)]
    for(int i = 0; i < 4; i++)
    {
        vertexLightData += saturate(dot(vertexLightInformation.Direction[i], worldNormal)) * vertexLightInformation.ColorFalloff[i];
    }
    vLight = vertexLightData;
}
#endif


//Lifted vertex light support from XSToon: https://github.com/Xiexe/Xiexes-Unity-Shaders

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