#ifdef _WIND

Texture2D _WindNoise; SamplerState sampler_WindNoise;

half _WindSpeed;
half _WindScale;
half3 _WindIntensity;

// very simple but enough to make things less static
float3 GetWindOffset(float3 position, half3 vertexColor)
{
    half3 windNoise = _WindNoise.SampleLevel(sampler_WindNoise, (position.xz * _WindScale) + (_Time.y * _WindSpeed), 0);
    windNoise = windNoise * 2 - 1;

    return windNoise * _WindIntensity * vertexColor;
}



#endif