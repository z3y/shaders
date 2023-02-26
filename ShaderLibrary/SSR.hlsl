#ifndef SSR_INCLUDED
#define SSR_INCLUDED

//-----------------------------------------------------------------------------------
// SCREEN SPACE REFLECTIONS
// 
// Original made by error.mdl, Toocanzs, and Xiexe.
// Reworked and updated by Mochie
// Modified by z3y
//-----------------------------------------------------------------------------------

#ifdef _SSR
#define _EdgeFade 0.1

inline float4 ComputeGrabScreenPos(float4 pos)
{
	#if UNITY_UV_STARTS_AT_TOP
	float scale = -1.0;
	#else
	float scale = 1.0;
	#endif
	float4 o = pos * 0.5f;
	o.xy = float2(o.x, o.y*scale) + o.w;
#ifdef UNITY_SINGLE_PASS_STEREO
	o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
	o.zw = pos.zw;
	return o;
}

// from slz modified urp
float3 CameraToScreenPosCheap(const float3 pos)
{
	return float3(pos.x * UNITY_MATRIX_P._m00 + pos.z * UNITY_MATRIX_P._m02, pos.y * UNITY_MATRIX_P._m11 + pos.z * UNITY_MATRIX_P._m12, -pos.z);
}

float3 GetBlurredGP(Texture2D ssrg, SamplerState ssrg_sampler, const float2 texelSize, const float2 uvs, const float dim){
	float2 pixSize = 2/texelSize;
	float center = floor(dim*0.5);
	float3 refTotal = float3(0,0,0);
	for (int i = 0; i < floor(dim); i++){
		for (int j = 0; j < floor(dim); j++){
			float4 refl = SAMPLE_TEXTURE2D_LOD(ssrg, ssrg_sampler, float2(uvs.x + pixSize.x*(i-center), uvs.y + pixSize.y*(j-center)), 0);
			refTotal += refl.rgb;
		}
	}
	return refTotal/(floor(dim)*floor(dim));
}

float4 ReflectRay(float3 reflectedRay, float3 rayDir, float _LRad, float _SRad, float _Step, float noise, const int maxIterations){

	#if UNITY_SINGLE_PASS_STEREO || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		half x_min = 0.5*unity_StereoEyeIndex;
		half x_max = 0.5 + 0.5*unity_StereoEyeIndex;
	#else
		half x_min = 0.0;
		half x_max = 1.0;
	#endif
	
	reflectedRay = mul(UNITY_MATRIX_V, float4(reflectedRay, 1));
	rayDir = mul(UNITY_MATRIX_V, float4(rayDir, 0));
	int totalIterations = 0;
	int direction = 1;
	float3 finalPos = 0;
	float step = _Step;
	float lRad = _LRad;
	float sRad = _SRad;

	for (int i = 0; i < maxIterations; i++){
		totalIterations = i;
		float4 spos = ComputeGrabScreenPos(CameraToScreenPosCheap(reflectedRay).xyzz);
		float2 uvDepth = spos.xy / spos.w;
		UNITY_BRANCH
		if (uvDepth.x > x_max || uvDepth.x < x_min || uvDepth.y > 1 || uvDepth.y < 0){
			break;
		}

		float rawDepth = DecodeFloatRG(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture,float4(uvDepth,0,0)));
		float linearDepth = Linear01Depth(rawDepth);
		float sampleDepth = -reflectedRay.z;
		float realDepth = linearDepth * _ProjectionParams.z;
		float depthDifference = abs(sampleDepth - realDepth);

		if (depthDifference < lRad){ 
			if (direction == 1){
				if(sampleDepth > (realDepth - sRad)){
					if(sampleDepth < (realDepth + sRad)){
						finalPos = reflectedRay;
						break;
					}
					direction = -1;
					step = step*0.1;
				}
			}
			else {
				if(sampleDepth < (realDepth + sRad)){
					direction = 1;
					step = step*0.1;
				}
			}
		}
		reflectedRay = reflectedRay + direction*step*rayDir;
		step += step*(0.025 + 0.005*noise);
		lRad += lRad*(0.025 + 0.005*noise);
		sRad += sRad*(0.025 + 0.005*noise);
	}
	return float4(finalPos, totalIterations);
}

float4 GetSSR(float3 wPos, float3 viewDir, float3 rayDir, half3 faceNormal, float smoothness, float3 albedo, float metallic, float2 screenUVs, float4 screenPos){
	
	float FdotR = dot(faceNormal, rayDir.xyz);

	UNITY_BRANCH
	if (IsInMirror() || FdotR < 0)
    {
		return 0;
	}
	
    float4 noiseUvs = screenPos;
    noiseUvs.xy = (noiseUvs.xy * _CameraOpaqueTexture_TexelSize.zw) / (_NoiseTexSSR_TexelSize.zw * noiseUvs.w);	
    float4 noiseRGBA = SAMPLE_TEXTURE2D_LOD(_NoiseTexSSR, sampler_NoiseTexSSR, noiseUvs.xy, 0);
    float noise = noiseRGBA.r;
    
    float3 reflectedRay = wPos + (0.2*0.09/FdotR + noise*0.09)*rayDir;
    float4 finalPos = ReflectRay(reflectedRay, rayDir, 0.2, 0.02, 0.09, noise, 50);
    float totalSteps = finalPos.w;
    finalPos.w = 1;
    
    if (!any(finalPos.xyz)){
        return 0;
    }
    
    // float4 uvs = UNITY_PROJ_COORD(ComputeGrabScreenPos(mul(UNITY_MATRIX_P, finalPos)));
    float4 uvs = UNITY_PROJ_COORD(ComputeGrabScreenPos(CameraToScreenPosCheap(finalPos).xyzz));
    uvs.xy = uvs.xy / uvs.w;

    #if UNITY_SINGLE_PASS_STEREO || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        float xfade = 1;
    #else
        float xfade = smoothstep(0, _EdgeFade, uvs.x) * smoothstep(1, 1-_EdgeFade, uvs.x); //Fade x uvs out towards the edges
    #endif
    float yfade = smoothstep(0, _EdgeFade, uvs.y)*smoothstep(1, 1-_EdgeFade, uvs.y); //Same for y
    float lengthFade = smoothstep(1, 0, 2*(totalSteps / 50)-1);

    float blurFac = max(1,min(12, 12 * (-2)*(smoothness-1)));
    float4 reflection = float4(GetBlurredGP(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, _CameraOpaqueTexture_TexelSize.zw, uvs.xy, blurFac*1.5),1);

    //reflection.rgb = lerp(reflection.rgb, reflection.rgb*albedo.rgb,smoothstep(0, 1.75, metallic));

    reflection.a = FdotR * xfade * yfade * lengthFade;
    return max(0,reflection);
}

#endif

#endif

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