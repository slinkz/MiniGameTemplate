// 激光着色器 — 叠加发光 + 中心高亮渐变
// 顶点 UV.x 控制横向渐变（0→边缘, 0.5→中心, 1→边缘）——始终 [0,1]
// 顶点 UV.y 控制纵向衰减 / Atlas 模式下为 Atlas 子区域 UV.y
//
// _ATLASMODE_ON 时：UV.x 仅用于程序化渐变，跳过 tex2D 采样（避免跨 Atlas 子区域）
// _ATLASMODE_ON 关闭时：UV 同时用于纹理采样和渐变（保持原行为）
Shader "MiniGameTemplate/Danmaku/Laser"
{
    Properties
    {
        _MainTex ("Laser Texture", 2D) = "white" {}
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.3, 0.5, 1, 0.5)
        _CoreWidth ("Core Width", Range(0, 0.5)) = 0.15
        // _AtlasMode 由代码通过 EnableKeyword("_ATLASMODE_ON") 控制，不在 Inspector 暴露。
        [HideInInspector] [Toggle] _AtlasMode ("Atlas Mode", Float) = 0
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
            #pragma multi_compile_local __ _ATLASMODE_ON

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
                // Atlas 模式：UV.x = 渐变参数 [0,1]，跳过纹理采样（避免采样到 Atlas 其他子区域）。
                // 激光视觉完全由程序化渐变（CoreColor + GlowColor + smoothstep）驱动。
                // 非 Atlas 模式：UV 同时用于纹理采样和渐变（保持原行为）
                #ifdef _ATLASMODE_ON
                fixed4 tex = fixed4(1, 1, 1, 1);
                #else
                fixed4 tex = tex2D(_MainTex, i.uv);
                #endif

                // 横向渐变：中心白芯 + 外围辉光（两种模式下 UV.x 都是 [0,1]）
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
