Shader "Lair/Tests/FlashPropertyFixture"
{
    //# test-engineer 테스트 전용 픽스처 — HitFlash 의 _FlashInvert/_FlashWhite 프로퍼티 분기(monster-2d-conversion.md §6.2)를
    //# 검증하기 위한 최소 셰이더. 실제 Monster2DSprite.shadergraph 는 아트 소스 확정 후 별도 제작(기획서 §7.1).
    //# 프로덕션 렌더링에 쓰이지 않음 — Assets/_Lair/Tests 하위 테스트 더블.
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FlashInvert ("Flash Invert", Float) = 0
        _FlashWhite ("Flash White", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _FlashInvert;
                float _FlashWhite;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 rgb = _Color.rgb;
                rgb = lerp(rgb, 1 - rgb, _FlashInvert);
                rgb = lerp(rgb, 1, _FlashWhite);
                return half4(rgb, _Color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
