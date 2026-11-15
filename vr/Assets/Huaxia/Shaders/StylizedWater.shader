// 东方风格化数字遗产 —— 静态水景
// 半透明、程序化微波、菲涅尔提亮；不使用环境反射探针，保持含蓄的墨青水面。
Shader "Huaxia/StylizedWater"
{
    Properties
    {
        _DeepColor ("深水色", Color) = (0.13,0.21,0.23,0.92)
        _ShallowColor ("浅水色", Color) = (0.26,0.36,0.34,0.85)
        _SkyTint ("天光色", Color) = (0.72,0.78,0.78,0.55)
        _FresnelPower ("菲涅尔聚焦", Range(0.5,8)) = 3.2
        _WaveScale ("微波尺度", Float) = 0.8
        _WaveSpeed ("微波速度", Float) = 0.22
        _WaveStrength ("微波起伏", Range(0,1)) = 0.3
        _Glossiness ("光滑度", Range(0,1)) = 0.5
        _AlphaBase ("基础不透明度", Range(0,1)) = 0.86
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        ZWrite Off

        CGPROGRAM
        #pragma surface surf HuaxiaWater alpha:fade
        #pragma target 3.0

        fixed4 _DeepColor;
        fixed4 _ShallowColor;
        fixed4 _SkyTint;
        float _FresnelPower;
        float _WaveScale;
        float _WaveSpeed;
        float _WaveStrength;
        half _Glossiness;
        float _AlphaBase;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
            float3 worldNormal;
        };

        float WaveHeight (float2 p, float t)
        {
            float h = sin(p.x * 0.9 + t * 1.1) * 0.45;
            h += sin(p.y * 1.3 - t * 0.85) * 0.4;
            h += sin((p.x + p.y) * 0.55 + t * 1.6) * 0.5;
            h += sin((p.x - p.y) * 1.7 - t * 2.1) * 0.18;
            return h;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            float t = _Time.y * _WaveSpeed * 6.0;
            float2 p = IN.worldPos.xz * _WaveScale;
            float e = 0.35;

            float hC = WaveHeight(p, t);
            float hX = WaveHeight(p + float2(e, 0), t);
            float hZ = WaveHeight(p + float2(0, e), t);

            float3 n = normalize(float3(-(hX - hC) / e * _WaveStrength * 0.35, 1.0,
                                        -(hZ - hC) / e * _WaveStrength * 0.35));

            half fres = pow(1.0 - saturate(dot(normalize(IN.viewDir), n)), _FresnelPower);

            fixed3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(hC * 0.25 + 0.5));
            o.Albedo = col;
            o.Normal = n;
            o.Emission = _SkyTint.rgb * (0.10 + fres * 0.85) * _SkyTint.a;
            o.Gloss = _Glossiness;
            o.Specular = 1.0;
            o.Alpha = saturate(lerp(_AlphaBase, 1.0, fres) * _DeepColor.a);
        }

        half4 LightingHuaxiaWater (SurfaceOutput s, half3 viewDir, UnityGI gi)
        {
            half ndl = saturate(dot(s.Normal, gi.light.dir) * 0.5 + 0.5);
            half3 ramp = lerp(fixed3(0.72, 0.78, 0.8), fixed3(1, 1, 1), ndl);
            half3 diffuse = s.Albedo * gi.light.color * ramp;

            half3 h = normalize(gi.light.dir + viewDir);
            half spec = pow(saturate(dot(s.Normal, h)), 72.0) * 0.22;
            half3 specular = gi.light.color * spec;

            half3 ambient = s.Albedo * gi.indirect.diffuse * 0.85;
            half4 c;
            c.rgb = diffuse + specular + s.Emission + ambient;
            c.a = s.Alpha;
            return c;
        }

        inline void LightingHuaxiaWater_GI (SurfaceOutput s, UnityGIInput data, inout UnityGI gi)
        {
            gi = UnityGlobalIllumination(data, 0.6, s.Normal);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
