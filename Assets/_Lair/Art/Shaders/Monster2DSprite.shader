Shader "Lair/Monster2DSprite"
{
    //# scene-2d-conversion §1.6 — 2D 렌더러: flash(원화 × 정점색) × 도트 양자화 조명 + 발광. 기본 렌더러: 기존 무조명 그대로.
    //# 속성·키워드 계약(_FlashWhite·_FlashInvert·_EMISSION·_EmissionColor·_EmissionMask) 불변.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _EmissionMask ("Emission Mask", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        [Toggle(_EMISSION)] _EmissionToggle ("Emission Enabled", Float) = 0
        _FlashWhite ("Flash White", Range(0,1)) = 0
        _FlashInvert ("Flash Invert", Range(0,1)) = 0
        //# 등급 연출(monster-grades §2.2) — 기본값(폭 0·밝기/채도 1)이면 출력 불변. 폭은 텍스처 픽셀 단위.
        _GlowColor ("Grade Glow Color", Color) = (1,1,1,1)
        _GlowWidth ("Grade Glow Width (texel)", Float) = 0
        _GlowInner ("Grade Glow Inner Ratio", Range(0,1)) = 0.5
        _Brightness ("Body Brightness", Float) = 1
        _Saturation ("Body Saturation", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest [_ZTest]
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
            float3 positionVS : TEXCOORD1;
        };

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_EmissionMask); SAMPLER(sampler_EmissionMask);
        float4 _MainTex_ST;
        half4 _EmissionColor;
        half _FlashWhite;
        half _FlashInvert;
        half4 _GlowColor;
        half _GlowWidth;
        half _GlowInner;
        half _Brightness;
        half _Saturation;

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.positionHCS = TransformWorldToHClip(positionWS);
            OUT.positionVS = TransformWorldToView(positionWS);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.color = IN.color;
            return OUT;
        }

        half3 FlashRgb(half4 tex, float4 color)
        {
            half3 rgb = tex.rgb * color.rgb;
            rgb = lerp(rgb, 1.0 - rgb, _FlashInvert);
            return lerp(rgb, 1.0, _FlashWhite);
        }

        half3 EmissionRgb(float2 uv, half alpha)
        {
            #if defined(_EMISSION)
            half emissionMask = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, uv).r;
            return _EmissionColor.rgb * emissionMask * alpha;
            #else
            return half3(0, 0, 0);
            #endif
        }

        //# 등급 연출 — 몸 밝기·채도 배율 + 외곽 후광(알파를 8방향 2겹 샘플해 바깥으로 번지게). 폭 0 이고 밝기/채도 1 이면 입력 그대로.
        half4 GradeLook(float2 uv, half3 rgb, half alpha, half colorA)
        {
            half luma = dot(rgb, half3(0.299, 0.587, 0.114));
            rgb = lerp(half3(luma, luma, luma), rgb, _Saturation) * _Brightness;
            if (_GlowWidth > 0.001)
            {
                uint texW, texH;
                _MainTex.GetDimensions(texW, texH);
                float2 texel = 1.0 / float2(texW, texH);
                half outer = 0;
                half inner = 0;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 0.785398;
                    float2 dir = float2(cos(angle), sin(angle)) * texel;
                    outer += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + dir * _GlowWidth, 0).a;
                    inner += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + dir * _GlowWidth * _GlowInner, 0).a;
                }
                half glow = saturate(outer * 0.075 + inner * 0.125) * colorA * _GlowColor.a;
                half glowAlpha = glow * (1.0 - alpha);
                half total = alpha + glowAlpha;
                rgb = (rgb * alpha + _GlowColor.rgb * glowAlpha) / max(total, 0.0001);
                alpha = total;
            }
            return half4(rgb, alpha);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragLit
            #pragma shader_feature_local _EMISSION
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightVariables.hlsl"
            TEXTURE2D(_ShapeLightTexture0); SAMPLER(sampler_ShapeLightTexture0);
            half2 _ShapeLightBlendFactors0;
            #include "Assets/_Lair/Art/Shaders/DotLightQuantize.hlsl"

            half4 FragLit(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half alpha = tex.a * IN.color.a;
                half3 rgb = FlashRgb(tex, IN.color) * DotLight(IN.positionVS) + EmissionRgb(IN.uv, alpha);
                return GradeLook(IN.uv, rgb, alpha, IN.color.a);
            }
            ENDHLSL
        }

        Pass
        {
            //# 기본(Universal) 렌더러 카메라용 — 전환 전 셰이더와 같은 무조명 출력
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUnlit
            #pragma shader_feature_local _EMISSION

            half4 FragUnlit(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half alpha = tex.a * IN.color.a;
                return GradeLook(IN.uv, FlashRgb(tex, IN.color) + EmissionRgb(IN.uv, alpha), alpha, IN.color.a);
            }
            ENDHLSL
        }
    }
}
