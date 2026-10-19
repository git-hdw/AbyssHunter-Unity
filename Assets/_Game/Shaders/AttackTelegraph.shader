Shader "AbyssHunter/AttackTelegraph"
{
    Properties { _BaseColor("Color", Color) = (1,.5,.1,1) }
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
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.color=input.color*_BaseColor;
                return output;
            }
            half4 Frag(Varyings input):SV_Target {return input.color;}
            ENDHLSL
        }
    }
}
