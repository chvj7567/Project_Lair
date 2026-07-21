Shader "Lair/HeroOutline"
{
    //# 스킨드 대응 인버티드-헐 아웃라인 (hero-stage-variant plan Task 3, spec §3-B).
    //# SkinnedMeshRenderer 의 두 번째 서브머터리얼로 부착. Unity 가 스키닝 후 넘긴 오브젝트공간 버텍스를
    //# 노멀 방향으로 팽창 → 뼈 애니메이션(idle/walk)을 그대로 따라간다. Cull Front 로 뒷면만 렌더해 실루엣만 남긴다.
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                //# 스킨드 버텍스(이미 스키닝된 오브젝트공간)를 노멀 방향으로 팽창.
                float3 inflated = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(inflated);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
