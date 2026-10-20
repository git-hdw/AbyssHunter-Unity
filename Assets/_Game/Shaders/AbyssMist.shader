Shader "AbyssHunter/DepthMist"
{
    Properties { _BaseColor("雾色", Color) = (.12,.2,.28,.22) _Speed("漂移速度", Float) = .025 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-50" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _Speed;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
            Varyings vert(Attributes i) { Varyings o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.uv=i.uv; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p) { float2 i=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y); }
            half4 frag(Varyings i) : SV_Target
            {
                float2 p=i.world.xz*.14+_Time.y*_Speed;
                float n=noise(p)*.65+noise(p*2.3-_Time.y*_Speed)*.35;
                float radial=1-smoothstep(.2,.7,length(i.uv-.5));
                return half4(_BaseColor.rgb,_BaseColor.a*radial*smoothstep(.15,.8,n));
            }
            ENDHLSL
        }
    }
}
