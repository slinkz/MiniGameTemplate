// 弹幕弹丸着色器 — Additive Blend（发光层）
// Phase 3 增强：溶解效果 + 发光参数
Shader "MiniGameTemplate/Danmaku/BulletAdditive"
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
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha One

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

                // ── Dissolve（溶解效果） ──
                float dissolve = _DissolveAmount;
                if (dissolve > 0.001)
                {
                    float noise = tex2D(_DissolveTex, i.uv).r;
                    clip(noise - dissolve);
                    float edge = saturate(1.0 - (noise - dissolve) / max(_DissolveEdgeWidth, 0.001));
                    tex.rgb += _DissolveEdgeColor.rgb * edge * _DissolveEdgeColor.a;
                }

                // ── Glow（发光叠加） ──
                if (_GlowIntensity > 0.001)
                {
                    tex.rgb += _GlowColor.rgb * _GlowIntensity * tex.a;
                }

                return tex * i.color;
            }
            ENDCG
        }
    }

    Fallback Off
}
