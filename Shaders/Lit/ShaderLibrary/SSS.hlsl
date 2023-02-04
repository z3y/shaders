#ifndef SSS_INCLUDED
#define SSS_INCLUDED

void ApplySSS(inout LightData lightData, float3 normalWS, float3 viewDir)
{
    half3 vLTLight = normalWS + lightData.Direction;
    half fLTDot = pow(saturate(dot(viewDir, -lightData.Direction)), 5) * 5;
    half3 fLT = lightData.Attenuation * fLTDot * 1;
    lightData.FinalColor += fLT;
}
#endif