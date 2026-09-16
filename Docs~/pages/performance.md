# Performance

## Android Features

Lit shaders were optimized for android first, using half precision wherever possible.
Certain features are automatically disabled when using the Android build target, for example the Height Map, LTCGI etc.
These features can also have a heavy impact on PC, due to high resolutions in VR, and all the calculations being done per pixel.
The number of steps of the Height Map increases the number of time the Height Map will get sampled.
The more pixels on a screen the material covers, the higher the performance impact will be.

## Directional Light

Unity assumes that one directional light is always active in the scene and doesn't have any keywords to disable and skip calculations when its not used.
This can have a noticeable impact on Android (mostly the Specular Highlights math), so in certain cases, like when using a lightmap, the directional light is disabled.
This is also slightly wasteful on PC, so a dynamic branch is used to skip directional lights when they're disabled.
Without a dynamic branch, both sides are evaluated, and the result is selected conditionally.

### Compiled Code Example

#### Line 50

If directional light is disabled this huge chunk of instructions is skipped until line 103.

#### Line 108

Light Probe Proxy Volumes use a dynamic branch on all materials without lightmaps enabled. To save space disable them in the config file `Tools > Lit > Create Config File`.

#### Line 168

This branch is used for reflection probes, if the second probe needs be sampled and blended with the first one.
Some shaders also have a branch for the box projection, but this shader assumes box projection on average is enabled, to skip the overhead of a branch.

```asm
   0: sample_indexable(texture2d)(float,float,float,float) r0.xyz, v4.xyxx, t3.xyzw, s2
   1: mul r0.xyz, r0.xyzx, cb0[5].xyzx
   2: add r1.xy, -cb0[7].xyxx, l(1.000000, 1.000000, 0.000000, 0.000000)
   3: add r0.w, -r1.x, l(1.000000)
   4: deriv_rtx_coarse r1.xzw, v2.xxyz
   5: deriv_rty_coarse r2.xyz, v2.xyzx
   6: dp3 r1.x, r1.xzwx, r1.xzwx
   7: dp3 r1.z, r2.xyzx, r2.xyzx
   8: add r1.x, r1.z, r1.x
   9: mul r0.w, r0.w, r0.w
  10: dp2 r1.x, cb0[15].wwww, r1.xxxx
  11: min r1.x, r1.x, cb0[16].x
  12: mad_sat r0.w, r0.w, r0.w, r1.x
  13: sqrt r0.w, r0.w
  14: sqrt r0.w, r0.w
  15: add r0.w, -r0.w, l(1.000000)
  16: dp3 r1.x, v2.xyzx, v2.xyzx
  17: max r1.x, r1.x, l(0.000000)
  18: rsq r1.x, r1.x
  19: mul r2.xyz, r1.xxxx, v2.xyzx
  20: mov r1.z, v5.y
  21: add r3.xyz, -v1.xyzx, cb2[r1.z + 64].xyzx
  22: dp3 r1.z, r3.xyzx, r3.xyzx
  23: rsq r1.z, r1.z
  24: mul r3.xyz, r1.zzzz, r3.xyzx
  25: dp3 r1.z, r2.xyzx, r3.xyzx
  26: add r4.x, |r1.z|, l(0.000010)
  27: add r4.y, -r0.w, l(1.000000)
  28: mul r0.w, cb0[16].z, cb0[16].z
  29: mul r0.w, r0.w, l(0.160000)
  30: mul r5.xyz, r0.xyzx, cb0[7].yyyy
  31: mad r5.xyz, r0.wwww, r1.yyyy, r5.xyzx
  32: sample_l_indexable(texture2d)(float,float,float,float) r1.zw, r4.xyxx, t4.zwxy, s3, l(0.000000)
  33: add r0.w, -r1.z, r1.w
  34: mad r6.xyz, r5.xyzx, r0.wwww, r1.zzzz
  35: div r0.w, l(1.000000, 1.000000, 1.000000, 1.000000), r1.w
  36: add r0.w, r0.w, l(-1.000000)
  37: mad r7.xyz, r5.xyzx, r0.wwww, l(1.000000, 1.000000, 1.000000, 0.000000)
  38: dp3 r0.w, -r3.xyzx, r2.xyzx
  39: add r0.w, r0.w, r0.w
  40: mad r8.xyz, r2.xyzx, -r0.wwww, -r3.xyzx
  41: mul r0.w, r4.y, r4.y
  42: mad r1.xzw, v2.xxyz, r1.xxxx, -r8.xxyz
  43: mad r1.xzw, r0.wwww, r1.xxzw, r8.xxyz
  44: dp3 r3.w, cb1[0].xyzx, cb1[0].xyzx
  45: max r3.w, r3.w, l(0.000000)
  46: rsq r3.w, r3.w
  47: mul r8.xyz, r3.wwww, cb1[0].xyzx
  48: dp3 r4.z, r8.xyzx, r8.xyzx
  49: ne r4.z, r4.z, l(0.000000)
  50: if_nz r4.z
  51:   mad r3.xyz, cb1[0].xyzx, r3.wwww, r3.xyzx
  52:   dp3 r3.w, r3.xyzx, r3.xyzx
  53:   rsq r3.w, r3.w
  54:   mul r3.xyz, r3.wwww, r3.xyzx
  55:   dp3_sat r3.w, r2.xyzx, r8.xyzx
  56:   dp3_sat r4.z, r8.xyzx, r3.xyzx
  57:   dp3_sat r3.x, r2.xyzx, r3.xyzx
  58:   mul r8.xyz, r3.wwww, cb0[1].xyzx
  59:   mul r3.y, r4.z, r4.z
  60:   dp2 r3.y, r3.yyyy, r4.yyyy
  61:   add r3.y, r3.y, l(-0.500000)
  62:   add r3.z, -r3.w, l(1.000000)
  63:   mul r4.w, r3.z, r3.z
  64:   mul r4.w, r4.w, r4.w
  65:   mul r3.z, r3.z, r4.w
  66:   mad r3.z, r3.y, r3.z, l(1.000000)
  67:   add r4.w, -r4.x, l(1.000000)
  68:   mul r5.w, r4.w, r4.w
  69:   mul r5.w, r5.w, r5.w
  70:   mul r4.w, r4.w, r5.w
  71:   mad r3.y, r3.y, r4.w, l(1.000000)
  72:   mul r3.y, r3.y, r3.z
  73:   mul r8.xyz, r3.yyyy, r8.xyzx
  74:   max r0.w, r0.w, l(0.002000)
  75:   add r9.xyz, -r5.xyzx, l(1.000000, 1.000000, 1.000000, 0.000000)
  76:   add r3.y, -r4.z, l(1.000000)
  77:   mul r3.z, r3.y, r3.y
  78:   mul r3.z, r3.z, r3.z
  79:   mul r3.y, r3.z, r3.y
  80:   mad r5.xyz, r9.xyzx, r3.yyyy, r5.xyzx
  81:   mul r5.xyz, r7.xyzx, r5.xyzx
  82:   mul r3.y, r0.w, r3.x
  83:   mad r3.x, -r3.x, r3.x, l(1.000000)
  84:   mad r3.x, r3.y, r3.y, r3.x
  85:   div r3.x, r0.w, r3.x
  86:   mul r3.x, r3.x, r3.x
  87:   mul r3.x, r3.x, l(0.318310)
  88:   mul r3.y, r0.w, r0.w
  89:   mul r3.z, r4.x, r4.x
  90:   mad r0.w, -r0.w, r0.w, l(1.000000)
  91:   mad r3.z, r3.z, r0.w, r3.y
  92:   sqrt r3.z, r3.z
  93:   mul r4.z, r3.w, r3.w
  94:   mad r0.w, r4.z, r0.w, r3.y
  95:   sqrt r0.w, r0.w
  96:   mul r0.w, r0.w, r4.x
  97:   mad r0.w, r3.w, r3.z, r0.w
  98:   div r0.w, l(0.500000), r0.w
  99:   mul r0.w, r0.w, r3.x
 100:   mul r3.xyz, r5.xyzx, r0.wwww
 101:   max r3.xyz, r3.xyzx, l(0.000000, 0.000000, 0.000000, 0.000000)
 102:   mul r3.xyz, r8.xyzx, r3.xyzx
 103: else
 104:   mov r8.xyz, l(0,0,0,0)
 105:   mov r3.xyz, l(0,0,0,0)
 106: endif
 107: eq r0.w, cb4[0].x, l(1.000000)
 108: if_nz r0.w
 109:   eq r0.w, cb4[0].y, l(1.000000)
 110:   mul r4.xzw, v1.yyyy, cb4[2].xxyz
 111:   mad r4.xzw, cb4[1].xxyz, v1.xxxx, r4.xxzw
 112:   mad r4.xzw, cb4[3].xxyz, v1.zzzz, r4.xxzw
 113:   add r4.xzw, r4.xxzw, cb4[4].xxyz
 114:   movc r4.xzw, r0.wwww, r4.xxzw, v1.xxyz
 115:   add r4.xzw, r4.xxzw, -cb4[6].xxyz
 116:   mul r5.yzw, r4.xxzw, cb4[5].xxyz
 117:   mul r0.w, r5.y, l(0.250000)
 118:   mul r3.w, cb4[0].z, l(0.500000)
 119:   mad r4.x, -cb4[0].z, l(0.500000), l(0.250000)
 120:   max r0.w, r0.w, r3.w
 121:   min r5.x, r4.x, r0.w
 122:   sample_indexable(texture3d)(float,float,float,float) r9.xyzw, r5.xzwx, t2.xyzw, s1
 123:   add r4.xzw, r5.xxzw, l(0.250000, 0.000000, 0.000000, 0.000000)
 124:   sample_indexable(texture3d)(float,float,float,float) r10.xyzw, r4.xzwx, t2.xyzw, s1
 125:   add r4.xzw, r5.xxzw, l(0.500000, 0.000000, 0.000000, 0.000000)
 126:   sample_indexable(texture3d)(float,float,float,float) r5.xyzw, r4.xzwx, t2.xyzw, s1
 127:   mov r2.w, l(1.000000)
 128:   dp4 r9.x, r9.xyzw, r2.xyzw
 129:   dp4 r9.y, r10.xyzw, r2.xyzw
 130:   dp4 r9.z, r5.xyzw, r2.xyzw
 131: else
 132:   mov r2.w, l(1.000000)
 133:   dp4 r9.x, cb1[39].xyzw, r2.xyzw
 134:   dp4 r9.y, cb1[40].xyzw, r2.xyzw
 135:   dp4 r9.z, cb1[41].xyzw, r2.xyzw
 136: endif
 137: mul r5.xyzw, r2.yzzx, r2.xyzz
 138: dp4 r10.x, cb1[42].xyzw, r5.xyzw
 139: dp4 r10.y, cb1[43].xyzw, r5.xyzw
 140: dp4 r10.z, cb1[44].xyzw, r5.xyzw
 141: mul r0.w, r2.y, r2.y
 142: mad r0.w, r2.x, r2.x, -r0.w
 143: mad r4.xzw, cb1[45].xxyz, r0.wwww, r10.xxyz
 144: add r4.xzw, r4.xxzw, r9.xxyz
 145: max r4.xzw, r4.xxzw, l(0.000000, 0.000000, 0.000000, 0.000000)
 146: lt r0.w, l(0.000000), cb3[2].w
 147: lt r5.xyz, l(0.000000, 0.000000, 0.000000, 0.000000), r1.xzwx
 148: movc r9.xyz, r5.xyzx, cb3[0].xyzx, cb3[1].xyzx
 149: add r9.xyz, r9.xyzx, -v1.xyzx
 150: div r9.xyz, r9.xyzx, r1.xzwx
 151: min r2.w, r9.y, r9.x
 152: min r2.w, r9.z, r2.w
 153: add r9.xyz, v1.xyzx, -cb3[2].xyzx
 154: mad r9.xyz, r1.xzwx, r2.wwww, r9.xyzx
 155: movc r9.xyz, r0.wwww, r9.xyzx, r1.xzwx
 156: mad r0.w, -r4.y, l(0.700000), l(1.700000)
 157: mul r0.w, r0.w, r4.y
 158: mul r0.w, r0.w, l(6.000000)
 159: sample_l_indexable(texturecube)(float,float,float,float) r9.xyzw, r9.xyzx, t0.xyzw, s0, r0.w
 160: add r2.w, r9.w, l(-1.000000)
 161: mad r2.w, cb3[3].w, r2.w, l(1.000000)
 162: log r2.w, r2.w
 163: mul r2.w, r2.w, cb3[3].y
 164: exp r2.w, r2.w
 165: mul r2.w, r2.w, cb3[3].x
 166: mul r10.xyz, r9.xyzx, r2.wwww
 167: lt r3.w, cb3[1].w, l(0.999990)
 168: if_nz r3.w
 169:   lt r3.w, l(0.000000), cb3[6].w
 170:   movc r5.xyz, r5.xyzx, cb3[4].xyzx, cb3[5].xyzx
 171:   add r5.xyz, r5.xyzx, -v1.xyzx
 172:   div r5.xyz, r5.xyzx, r1.xzwx
 173:   min r4.y, r5.y, r5.x
 174:   min r4.y, r5.z, r4.y
 175:   add r5.xyz, v1.xyzx, -cb3[6].xyzx
 176:   mad r5.xyz, r1.xzwx, r4.yyyy, r5.xyzx
 177:   movc r5.xyz, r3.wwww, r5.xyzx, r1.xzwx
 178:   sample_l_indexable(texturecube)(float,float,float,float) r5.xyzw, r5.xyzx, t1.xyzw, s0, r0.w
 179:   add r0.w, r5.w, l(-1.000000)
 180:   mad r0.w, cb3[7].w, r0.w, l(1.000000)
 181:   log r0.w, r0.w
 182:   mul r0.w, r0.w, cb3[7].y
 183:   exp r0.w, r0.w
 184:   mul r0.w, r0.w, cb3[7].x
 185:   mul r5.xyz, r5.xyzx, r0.wwww
 186:   mad r9.xyz, r2.wwww, r9.xyzx, -r5.xyzx
 187:   mad r10.xyz, cb3[1].wwww, r9.xyzx, r5.xyzx
 188: endif
 189: dp3 r0.w, r1.xzwx, r2.xyzx
 190: add r0.w, r0.w, l(1.000000)
 191: min r0.w, r0.w, l(1.000000)
 192: mul r0.w, r0.w, r0.w
 193: mul r1.xzw, r0.wwww, r10.xxyz
 194: add r2.xyz, r8.xyzx, r4.xzwx
 195: dp3 r0.w, r2.xyzx, l(1.000000, 1.000000, 1.000000, 0.000000)
 196: sqrt r0.w, r0.w
 197: add r0.w, r0.w, l(-1.000000)
 198: mad_sat r0.w, cb0[9].w, r0.w, l(1.000000)
 199: mul r1.xzw, r0.wwww, r1.xxzw
 200: mul r4.xyz, r6.xyzx, r7.xyzx
 201: mul r1.xzw, r1.xxzw, r4.xxyz
 202: mul r0.xyz, r0.xyzx, r1.yyyy
 203: mad r0.xyz, r0.xyzx, r2.xyzx, r1.xzwx
 204: mad o0.xyz, r3.xyzx, l(3.141593, 3.141593, 3.141593, 0.000000), r0.xyzx
 205: mov o0.w, l(1.000000)
 206: ret
```
