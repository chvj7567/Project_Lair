#ifndef LAIR_DOT_LIGHT_QUANTIZE_INCLUDED
#define LAIR_DOT_LIGHT_QUANTIZE_INCLUDED

//# scene-2d-conversion §1.5 — 2D 라이트 텍스처를 도트 중심에서 1회 샘플해 6단계 + 4×4 Bayer 디더로 양자화.
//# 포함 전에 Core.hlsl · LightingUtility.hlsl · ShapeLightVariables.hlsl(_HDREmulationScale) 과 SHAPE_LIGHT(0) 선언이 필요하다.

#define DOT_PPU 48.0
#define DOT_LIGHT_STEPS 6.0

//# 화면 중앙의 배경 도트(배틀 (614,352), 마을·로딩 (240,135)) — DotLightingSettings 가 전역으로 설정
float4 _DotGridOrigin;

static const float DOT_BAYER[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

//# 뷰 공간 위치 → 도트 좌표(좌상단 원점, 아래 +y)
float2 DotCoordFromView(float3 positionVS)
{
    return floor(float2(positionVS.x * DOT_PPU + _DotGridOrigin.x, -positionVS.y * DOT_PPU + _DotGridOrigin.y));
}

//# 도트 중심의 화면 UV — 라이트 텍스처 샘플 좌표
float2 DotCenterScreenUV(float2 dotCoord, float viewZ)
{
    float2 centerVS = float2((dotCoord.x + 0.5 - _DotGridOrigin.x) / DOT_PPU, -(dotCoord.y + 0.5 - _DotGridOrigin.y) / DOT_PPU);
    float4 cs = mul(UNITY_MATRIX_P, float4(centerVS, viewZ, 1.0));
    float4 ndc = cs / cs.w;
    return ComputeScreenPos(ndc).xy;
}

half3 QuantizeDotLight(half3 light, float2 dotCoord)
{
    int ix = ((int)dotCoord.x) & 3;
    int iy = ((int)dotCoord.y) & 3;
    float b = (DOT_BAYER[iy * 4 + ix] + 0.5) / 16.0;
    return floor(light * DOT_LIGHT_STEPS + b) / DOT_LIGHT_STEPS;
}

//# 조명 곱 계수(양자화 후). 2D 라이트가 없는 카메라면 1(무조명).
half3 DotLight(float3 positionVS)
{
#if USE_SHAPE_LIGHT_TYPE_0
    float2 dotCoord = DotCoordFromView(positionVS);
    float2 uv = DotCenterScreenUV(dotCoord, positionVS.z);
    half3 light = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, uv).rgb * _ShapeLightBlendFactors0.x * _HDREmulationScale;
    return QuantizeDotLight(light, dotCoord);
#else
    return half3(1, 1, 1);
#endif
}

#endif
