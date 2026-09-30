Shader "Lair/Monster2DSprite"
{
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

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _EMISSION

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
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 rgb = tex.rgb * IN.color.rgb;
                rgb = lerp(rgb, 1.0 - rgb, _FlashInvert);
                rgb = lerp(rgb, 1.0, _FlashWhite);
                half alpha = tex.a * IN.color.a;

                #if defined(_EMISSION)
                half emissionMask = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, IN.uv).r;
                rgb += _EmissionColor.rgb * emissionMask * alpha;
                #endif

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
