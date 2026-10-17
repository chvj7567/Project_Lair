Shader "Lair/Dot2DLit"
{
    //# scene-2d-conversion §1.6 Mat_Dot2DLit — 배경·제단 본체·어두운 링·그림자. 2D 렌더러에서 도트 양자화 조명, 기본 렌더러에선 무조명.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
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
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

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
            half4 color : COLOR;
            float3 positionVS : TEXCOORD1;
        };

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
        CBUFFER_END

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.positionHCS = TransformWorldToHClip(positionWS);
            OUT.positionVS = TransformWorldToView(positionWS);
            OUT.uv = IN.uv;
            OUT.color = IN.color * _Color * unity_SpriteColor;
            return OUT;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragLit
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightVariables.hlsl"
            TEXTURE2D(_ShapeLightTexture0); SAMPLER(sampler_ShapeLightTexture0);
            half2 _ShapeLightBlendFactors0;
            #include "Assets/_Lair/Art/Shaders/DotLightQuantize.hlsl"

            half4 FragLit(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                if (c.a <= 0.0)
                    discard;
                c.rgb *= DotLight(IN.positionVS);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            //# 기본(Universal) 렌더러 카메라용 무조명 폴백 — 2D 렌더러는 이 패스를 그리지 않는다
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUnlit

            half4 FragUnlit(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
            }
            ENDHLSL
        }
    }
}
