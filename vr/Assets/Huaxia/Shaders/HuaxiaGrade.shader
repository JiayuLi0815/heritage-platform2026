// 全屏调色 / 胶片曲线 —— 内置管线 OnRenderImage 用
// 顺序：曝光 → ACES 胶片曲线 → 对比度 → lift/gamma/gain → 分离色调 → 饱和 → 暗角
// 目的不是加滤镜，而是把「线性直出」的平画面压出明暗结构与冷暖分离。
Shader "Hidden/Huaxia/Grade"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float  _Exposure;
            float  _Contrast;
            float  _Saturation;
            float  _Vignette;
            float4 _LiftColor;
            float4 _GammaColor;
            float4 _GainColor;
            float4 _ShadowsTint;
            float4 _HighlightsTint;

            float3 AcesFilm (float3 x)
            {
                // Narkowicz 2015 拟合，比 Reinhard 更接近胶片肩部
                return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;

                c = AcesFilm(c * _Exposure);

                // 对比度在 gamma 空间做：线性空间里 0.5 不是中灰
                float3 g = pow(saturate(c), 1.0 / 2.2);
                g = (g - 0.5) * _Contrast + 0.5;
                g = saturate(g);

                // lift / gamma / gain
                g = saturate(g * _GainColor.rgb + _LiftColor.rgb);
                g = pow(g, 1.0 / max(_GammaColor.rgb, 0.01));

                // 分离色调：暗部冷、亮部暖。alpha 是强度。
                float lum = dot(g, float3(0.299, 0.587, 0.114));
                float shadowMask = 1.0 - smoothstep(0.0, 0.55, lum);
                float highMask   = smoothstep(0.45, 1.0, lum);
                g = lerp(g, g * _ShadowsTint.rgb, shadowMask * _ShadowsTint.a);
                g = lerp(g, g * _HighlightsTint.rgb, highMask * _HighlightsTint.a);

                // 饱和度
                float lum2 = dot(g, float3(0.299, 0.587, 0.114));
                g = lerp(lum2.xxx, g, _Saturation);

                // 暗角：把视觉重心压回画面中下部
                float2 d = i.uv - 0.5;
                float vig = 1.0 - _Vignette * smoothstep(0.18, 0.72, length(d));
                g *= vig;

                return fixed4(pow(saturate(g), 2.2), 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
