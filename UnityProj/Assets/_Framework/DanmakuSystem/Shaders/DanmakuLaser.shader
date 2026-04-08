// 激光着色器 — 叠加发光 + 中心高亮渐变
// 顶点 UV.x 控制横向渐变（0→边缘, 0.5→中心, 1→边缘）
// 顶点 UV.y 控制纵向衰减
Shader "MiniGameTemplate/Danmaku/Laser"
{
    Properties
    {
        _MainTex ("Laser Texture", 2D) = "white" {}
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.3, 0.5, 1, 0.5)
        _CoreWidth ("Core Width", Range(0, 0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+2"
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

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _CoreColor;
            fixed4 _GlowColor;
            float _CoreWidth;

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

                // 横向渐变：中心白芯 + 外围辉光
                float distFromCenter = abs(i.uv.x - 0.5) * 2.0;  // 0(中心) → 1(边缘)
                float coreMask = 1.0 - smoothstep(0, _CoreWidth, distFromCenter);
                float glowMask = 1.0 - smoothstep(_CoreWidth, 1.0, distFromCenter);

                fixed4 color = _CoreColor * coreMask + _GlowColor * glowMask;
                color *= tex;
                color *= i.color;

                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
