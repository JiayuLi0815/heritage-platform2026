// 地上的引导圈专用：一圈投在石墁上的柔光，不是实体环。
// 用加法混合而不是不透明色块，圈才能"透"出底下的石板；
// 内外沿按 uv.y 软化（PlateRing 的 uv.y 就是从内径 0 到外径 1），
// 免得出现硬边的塑料圈。
Shader "Huaxia/GuideGlow"
{
    Properties
    {
        _Tint ("光色", Color) = (1,0.78,0.35,1)
        _Alpha ("不透明度", Range(0,1)) = 0.34
        _Intensity ("亮度", Range(0,4)) = 1.25
        _EdgeSoft ("边缘柔化", Range(0.02,0.5)) = 0.3
        _HotEdge ("外沿提亮", Range(0,2)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            fixed4 _Tint;
            float _Alpha;
            float _Intensity;
            float _EdgeSoft;
            float _HotEdge;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float y = saturate(i.uv.y);
                float soft = max(_EdgeSoft, 0.001);
                float edge = smoothstep(0.0, soft, y) * smoothstep(0.0, soft, 1.0 - y);
                // 外沿那一线亮一点，形状才读成一圈光，而不是一片染色的地面
                float hot = lerp(1.0, 1.0 + _HotEdge, smoothstep(0.5, 0.92, y));
                return fixed4(_Tint.rgb * (_Intensity * hot), _Alpha * edge);
            }
            ENDCG
        }
    }
}
