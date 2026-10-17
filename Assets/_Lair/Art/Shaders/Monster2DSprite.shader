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
                return half4(rgb, alpha);
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
                return half4(FlashRgb(tex, IN.color) + EmissionRgb(IN.uv, alpha), alpha);
            }
            ENDHLSL
        }
    }
}
