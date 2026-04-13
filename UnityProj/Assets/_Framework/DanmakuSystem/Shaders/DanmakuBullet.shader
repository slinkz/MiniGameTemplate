// 弹幕弹丸着色器 — Alpha Blend（Normal 层）
// Phase 3 增强：溶解效果 + 发光参数
Shader "MiniGameTemplate/Danmaku/Bullet"
{
    Properties
    {
        _MainTex ("Bullet Atlas", 2D) = "white" {}
        [Header(Dissolve)]
        _DissolveTex ("Dissolve Noise", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.15)) = 0.05
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.5, 0, 1)
        [Header(Glow)]
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowWidth ("Glow Width", Range(0.02, 0.6)) = 0.2

    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _DissolveTex;
            float _DissolveAmount;
            float _DissolveEdgeWidth;
            fixed4 _DissolveEdgeColor;
            float _GlowIntensity;
            fixed4 _GlowColor;
            float _GlowWidth;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 color = tex * i.color;

                // ── Dissolve（溶解效果，_DissolveAmount > 0 时启用） ──
                float dissolve = _DissolveAmount;
                if (dissolve > 0.001)
                {
                    float noise = tex2D(_DissolveTex, i.uv).r;
                    clip(noise - dissolve);
                    float edge = saturate(1.0 - (noise - dissolve) / max(_DissolveEdgeWidth, 0.001));
                    color.rgb += _DissolveEdgeColor.rgb * edge * _DissolveEdgeColor.a;
                }

                // ── Glow（覆盖型 rim glow：边缘向 GlowColor 插值，而非加法混色） ──
                if (_GlowIntensity > 0.001)
                {
                    float alpha = color.a;
                    float width = max(_GlowWidth, 0.02);
                    float innerStart = saturate(0.5 - width);
                    float innerEnd = saturate(0.5);
                    float outerStart = saturate(0.5);
                    float outerEnd = saturate(0.5 + width);
                    float glowMask = smoothstep(innerStart, innerEnd, alpha) * (1.0 - smoothstep(outerStart, outerEnd, alpha));
                    float glowBlend = saturate(_GlowIntensity * glowMask);

                    color.rgb = lerp(color.rgb, _GlowColor.rgb, glowBlend);
                }



                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
