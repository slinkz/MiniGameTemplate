Shader "Hidden/RuntimeAtlasBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                // 全屏 quad 顶点已经是 NDC 坐标 (-1,-1) 到 (1,1)，
                // 直接 passthrough 即可。不能使用 UnityObjectToClipPos，
                // 因为 CommandBuffer 上下文中 VP 矩阵不可控，
                // 会导致 quad 变换到错误位置，Blit 结果为空。
                o.vertex = float4(v.vertex.xy, 0, 1);
                o.uv = v.uv;
                // 写入 RenderTexture 时需要翻转源纹理 Y，
                // 否则 SpriteSheet 在 RuntimeAtlas 子区域内会呈现上下颠倒。
                o.uv.y = 1.0 - o.uv.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
