// 东方风格化数字遗产 —— 《河山册》书页材质
// 一张纸的物理状态就是这个作品的进度条：湿 -> 干。
// 湿的时候纸色暗、发凉、水渍重、墨迹晕开且淡；干的时候纸色回暖、水渍退成淡圈、墨迹收紧变黑。
// 墨迹烘在一张带 alpha 的贴图里（Editor 端用楷书渲染出来），运行时只改两个数字：_Wetness / _Ink。
Shader "Huaxia/BookPage"
{
    Properties
    {
        _Color ("纸色 · 干", Color) = (0.92,0.89,0.80,1)
        _WetColor ("纸色 · 湿", Color) = (0.63,0.59,0.48,1)
        _Wetness ("湿度", Range(0,1)) = 1

        _MainTex ("纸纤维", 2D) = "white" {}
        _FiberScale ("纤维密度", Float) = 2.4
        _FiberStrength ("纤维强度", Range(0,1)) = 0.26

        _StainTex ("水渍与湿痕", 2D) = "black" {}
        _StainStrength ("水渍强度", Range(0,1)) = 1
        _StainTint ("水渍色", Color) = (0.55,0.50,0.38,1)

        _InkTex ("墨迹 · 卷名地名 (alpha)", 2D) = "black" {}
        _Ink ("显墨 · 卷名地名", Range(0,1)) = 0
        _NoteTex ("墨迹 · 页面字手记 (alpha)", 2D) = "black" {}
        _Note ("显墨 · 页面字手记", Range(0,1)) = 0
        _FootTex ("墨迹 · 页边注 (alpha)", 2D) = "black" {}
        _Foot ("显墨 · 页边注", Range(0,1)) = 0
        _InkColor ("墨色", Color) = (0.10,0.10,0.11,1)
        _InkFade ("淡墨色", Color) = (0.30,0.28,0.27,1)

        _GutterDark ("订口暗角", Range(0,1)) = 0.50
        _EdgeDark ("页边暗角", Range(0,1)) = 0.16
        _SealMask ("朱印 (alpha)", 2D) = "black" {}
        _SealStrength ("朱印强度", Range(0,1)) = 1
        _SealColor ("朱砂", Color) = (0.65,0.22,0.17,1)

        _ShadowTint ("暗部色", Color) = (0.42,0.47,0.55,1)
        _Wrap ("光照包裹", Range(0,1)) = 0.72
        _RampSoftness ("光阶柔和度", Range(0.02,1)) = 0.5
        _Glossiness ("光滑度", Range(0,1)) = 0.06
        _WetGloss ("湿面光泽", Range(0,2)) = 2.2
        _RimColor ("边缘光色", Color) = (1,0.95,0.86,1)
        _RimPower ("边缘光聚焦", Range(1,8)) = 5
        _RimStrength ("边缘光强度", Range(0,2)) = 0.05
        _EmissionColor ("自发光色", Color) = (0,0,0,1)
        _EmissionStrength ("自发光强度", Float) = 0
        _AmbientBoost ("环境光增益", Range(0,3)) = 1
        _BounceColor ("反弹光色", Color) = (0.13,0.115,0.09,1)
        _BounceStrength ("反弹光强度", Range(0,2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf HuaxiaBook fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _StainTex;
        sampler2D _InkTex;
        sampler2D _NoteTex;
        sampler2D _FootTex;
        sampler2D _SealMask;

        fixed4 _Color;
        fixed4 _WetColor;
        float _Wetness;
        float _FiberScale;
        float _FiberStrength;
        float _StainStrength;
        fixed4 _StainTint;
        float _Ink;
        float _Note;
        float _Foot;
        fixed4 _InkColor;
        fixed4 _InkFade;
        float _GutterDark;
        float _EdgeDark;
        float _SealStrength;
        fixed4 _SealColor;

        fixed4 _ShadowTint;
        float _Wrap;
        float _RampSoftness;
        half _Glossiness;
        float _WetGloss;
        fixed4 _RimColor;
        float _RimPower;
        float _RimStrength;
        fixed4 _EmissionColor;
        float _EmissionStrength;
        float _AmbientBoost;
        fixed4 _BounceColor;
        float _BounceStrength;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 viewDir;
        };

        struct SurfaceOutputBook
        {
            fixed3 Albedo;
            fixed3 Normal;
            fixed3 Emission;
            half Specular;
            fixed Gloss;
            fixed Alpha;
            half Rim;
            fixed3 WorldNormal;
        };

        // 墨迹在湿纸上会晕：用 4 次小偏移采样模拟扩散，同时整体减淡
        half InkMask (sampler2D tex, float2 uv, float wet, float reveal)
        {
            float b = lerp(0.00035, 0.0022, wet);
            half m = tex2D(tex, uv).a;
            m += tex2D(tex, uv + float2(b, b * 0.6)).a;
            m += tex2D(tex, uv + float2(-b, b * 0.4)).a;
            m += tex2D(tex, uv + float2(b * 0.3, -b)).a;
            m *= 0.25;
            // 晕开的墨：边缘抬高一点，主体略降，读起来「湿」
            m = lerp(m, saturate(m * 0.82 + 0.10), wet * 0.8);
            return saturate(m) * saturate(reveal);
        }

        void surf (Input IN, inout SurfaceOutputBook o)
        {
            float2 uv = IN.uv_MainTex;
            float wet = saturate(_Wetness);

            fixed3 fiber = tex2D(_MainTex, uv * _FiberScale).rgb;
            fixed3 fiberMul = lerp(fixed3(1, 1, 1), fiber * 1.26, _FiberStrength);

            fixed4 stain = tex2D(_StainTex, uv);

            // 1. 纸色：湿纸暗、冷、偏黄褐；干纸回暖
            fixed3 paper = lerp(_Color.rgb, _WetColor.rgb, wet * 0.52f);
            paper *= fiberMul;

            // 2. 水渍：湿时是深色湿痕，干了留下一圈淡黄
            float stainAmt = saturate(stain.r * _StainStrength);
            float dryRing = saturate(stain.b * _StainStrength);
            // 湿痕在湿的时候偏冷（潮气），干了才转成暖黄的水渍圈
            fixed3 stainTone = lerp(_StainTint.rgb, fixed3(0.82, 0.83, 0.83), wet * 0.55f);
            paper = lerp(paper, paper * stainTone, stainAmt * lerp(0.14, 0.46, wet));
            paper *= 1.0 - dryRing * (1.0 - wet) * 0.16;

            // 3. 潮斑：细碎的深浅，只在湿的时候出现
            paper *= lerp(1.0, 0.90 + 0.22 * stain.g, wet * 0.55);

            // 4. 订口与页边：这两个暗角让平铺的纸读成「一本书」
            float gutter = 1.0 - smoothstep(0.0, 0.075, uv.x);
            float edge = smoothstep(0.88, 1.0, uv.x) + smoothstep(0.06, 0.0, uv.y) + smoothstep(0.94, 1.0, uv.y);
            paper *= 1.0 - _GutterDark * gutter * 0.85;
            paper *= 1.0 - _EdgeDark * saturate(edge);

            // 5. 墨迹：三层分开显影——卷名地名 / 页面字与手记 / 页边注
            //    湿时晕、淡、发灰；干时收紧、变黑
            fixed3 inkCol = lerp(_InkFade.rgb, _InkColor.rgb, saturate(wet < 0.35f ? 0.95f : 0.55f));
            paper = lerp(paper, inkCol, InkMask(_InkTex, uv, wet, _Ink));
            paper = lerp(paper, inkCol * 1.02f, InkMask(_NoteTex, uv, wet, _Note));
            paper = lerp(paper, inkCol * 0.92f, InkMask(_FootTex, uv, wet, _Foot) * 0.85f);

            // 6. 朱印：始终在墨之上。印面是「朱地白文」——红色糊满、字口留黑透出纸，
            //    所以用「红多于绿」判印地，黑字自然成为镂空，不需要额外的遮罩通道。
            half4 sealTex = tex2D(_SealMask, uv);
            half seal = saturate((sealTex.r - sealTex.g) * 3.0) * sealTex.a * _SealStrength;
            // 印泥有厚薄：借纸纤维做出一点点不均匀，免得像贴纸
            fixed3 sealTone = 0.86 + 0.22 * fiberMul;
            paper = lerp(paper, _SealColor.rgb * sealTone, seal * 0.90);

            o.Albedo = paper;

            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normalize(IN.worldNormal)));
            o.Rim = pow(rim, _RimPower) * _RimStrength;
            o.WorldNormal = normalize(IN.worldNormal);

            o.Emission = _EmissionColor.rgb * _EmissionStrength;
            o.Specular = 0.0;
            o.Gloss = saturate(_Glossiness * lerp(1.0, _WetGloss, wet));
            o.Alpha = 1.0;
        }

        half4 LightingHuaxiaBook (SurfaceOutputBook s, half3 viewDir, UnityGI gi)
        {
            half3 nrm = normalize(s.WorldNormal);
            half ndl = dot(nrm, gi.light.dir);
            half wrap = saturate((ndl + _Wrap) / (1.0 + _Wrap));

            half soft = _RampSoftness * 0.5;
            half lit = smoothstep(0.5 - soft, 0.5 + soft, wrap);
            half3 ramp = lerp(_ShadowTint.rgb, fixed3(1, 1, 1), lit);

            half3 diffuse = s.Albedo * gi.light.color * ramp;

            half3 h = normalize(gi.light.dir + viewDir);
            half spec = pow(saturate(dot(nrm, h)), 40.0) * s.Gloss * 0.30;
            half3 specular = gi.light.color * spec;

            half3 rim = _RimColor.rgb * s.Rim * gi.light.color * 0.5;
            half3 ambient = s.Albedo * gi.indirect.diffuse * _AmbientBoost;

            half bounce = saturate(-nrm.y) * _BounceStrength;

            half4 c;
            c.rgb = diffuse + specular + rim + s.Emission + ambient + _BounceColor.rgb * bounce;
            c.a = s.Alpha;
            return c;
        }

        inline void LightingHuaxiaBook_GI (SurfaceOutputBook s, UnityGIInput data, inout UnityGI gi)
        {
            gi = UnityGlobalIllumination(data, 1.0, normalize(s.WorldNormal));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
