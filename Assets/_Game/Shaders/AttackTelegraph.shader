Shader "AbyssHunter/AttackTelegraph"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,.5,.1,1)
        _ChargeRadius("Charge front radius", Float) = -1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 positionXZ:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _ChargeRadius;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.color=input.color*_BaseColor;
                output.positionXZ=input.positionOS.xz;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float distanceToFront = abs(length(input.positionXZ) - _ChargeRadius);
                half arc = 1 - smoothstep(.045, .12, distanceToFront);
                half4 color = input.color;
                color.a = max(color.a, arc * _BaseColor.a);
                color.rgb = lerp(color.rgb, half3(1, .83, .42), arc * .7);
                return color;
            }
            ENDHLSL
        }
    }
}
