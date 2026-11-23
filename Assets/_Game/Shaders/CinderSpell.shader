Shader "AbyssHunter/CinderSpell"
{
    Properties
    {
        [Enum(Flame,0,Impact,1,Warning,2,Ember,3)] _Effect("Effect", Float) = 0
        _Age("Impact age", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Effect, _Age;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                if (_Effect > 2.5)
                {
                    float ember = 1 - smoothstep(.15, 1, abs(p.x)*1.45 + abs(p.y)*.8);
                    return half4(i.color.rgb, i.color.a * ember);
                }
                if (_Effect > 1.5)
                {
                    // Repeating ember marks leave the floor and silhouettes visible between them.
                    float dash = abs(frac(i.uv.x * 1.5 - _Time.y * 1.7) - .5) * 2;
                    float alpha = (1 - smoothstep(.38, .78, dash)) * (1 - smoothstep(.25, 1, abs(p.y)));
                    return half4(1, .31, .045, alpha * .82);
                }
                if (_Effect > .5)
                {
                    float angle = atan2(p.y, p.x);
                    float r = length(p);
                    float ray = pow(saturate(.5 + .5 * cos(angle * 9 + 2 * sin(angle * 3))), 7);
                    float front = .26 + _Age * (.45 + .25 * ray);
                    float shell = 1 - smoothstep(.02, .14, abs(r - front));
                    float flash = saturate(1 - r / max(.01, .52 * (1 - _Age))) * (1 - _Age);
                    float alpha = (shell * (.18 + .82 * ray) + flash) * (1 - _Age);
                    return half4(lerp(float3(1,.16,.012), float3(1,.89,.35), flash + .35 * ray), saturate(alpha));
                }

                // Three tapered tongues rise from the same coal. Each curls in a different phase.
                float t = _Time.y * 8;
                float h = saturate((p.y + .32) / 1.22);
                float sway = h * (.14 * sin(9*h - t) + .09 * sin(17*h - 1.6*t));
                float x = p.x - sway;
                float coal = 1 - length(float2(p.x / .64, (p.y + .34) / .50));
                float tongue = 1 - length(float2((x + .08*h) / max(.045, .54 - .4*h), (p.y + .03) / .91));
                float fork = 1 - length(float2((x - .30 + h*.13) / .19, (p.y + .08) / .69));
                float lip = 1 - length(float2((x + .32 + h*.06) / .15, (p.y + .17) / .58));
                float heat = max(coal, max(tongue, max(lip, fork)));
                float glow = exp(-4.5 * length(float2(p.x, p.y + .24))) * .33;
                float alpha = max(glow, smoothstep(-.025, .045, heat));
                float3 color = lerp(float3(.85,.065,.006), float3(1,.42,.014), smoothstep(.035,.26,heat));
                color = lerp(color, float3(1,.88,.26), smoothstep(.28,.56,heat));
                color = lerp(color, float3(1,.97,.72), smoothstep(.57,.86,coal));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
