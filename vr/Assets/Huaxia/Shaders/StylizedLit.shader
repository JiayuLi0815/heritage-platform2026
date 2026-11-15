// 东方风格化数字遗产 —— 统一受光材质
// 特征：世界坐标 triplanar 细节与法线（避免缩放立方体 UV 拉伸）、粗粒度宏观斑驳、
//       柔和半兰伯特光阶、
//       低强度边缘光、极低金属度、可控自发光。禁止高反光塑料感与霓虹发光。
Shader "Huaxia/StylizedLit"
{
    Properties
    {
        _Color ("基础色", Color) = (1,1,1,1)
        _MainTex ("细节纹理 (Triplanar)", 2D) = "white" {}
        _DetailScale ("细节密度", Float) = 0.45
        _DetailStrength ("细节强度", Range(0,1)) = 0.42
        _BumpMap ("细节法线 (Triplanar)", 2D) = "bump" {}
        _BumpScale ("法线强度", Range(0,2)) = 0.65
        _MacroScale ("宏观斑驳密度", Float) = 0.035
        _MacroStrength ("宏观斑驳强度", Range(0,1)) = 0.3
        _ShadowTint ("暗部色", Color) = (0.42,0.47,0.55,1)
        _Wrap ("光照包裹", Range(0,1)) = 0.62
        _RampSoftness ("光阶柔和度", Range(0.02,1)) = 0.42
        _RimColor ("边缘光色", Color) = (0.84,0.88,0.9,1)
        _RimPower ("边缘光聚焦", Range(1,8)) = 4
        _RimStrength ("边缘光强度", Range(0,2)) = 0.16
        _Glossiness ("光滑度", Range(0,1)) = 0.14
        _Metallic ("金属度", Range(0,1)) = 0
        _EmissionColor ("自发光色", Color) = (0,0,0,1)
        _EmissionStrength ("自发光强度", Float) = 1
        _VertexColorInfluence ("顶点色影响", Range(0,1)) = 0
        _AmbientBoost ("环境光增益", Range(0,3)) = 1
        _BounceColor ("地面反弹光色", Color) = (0.13,0.115,0.09,1)
        _BounceStrength ("反弹光强度", Range(0,2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Huaxia fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        float _DetailScale;
        float _DetailStrength;
        float _BumpScale;
        float _MacroScale;
        float _MacroStrength;
        fixed4 _ShadowTint;
        float _Wrap;
        float _RampSoftness;
        fixed4 _RimColor;
        float _RimPower;
        float _RimStrength;
        half _Glossiness;
        half _Metallic;
        fixed4 _EmissionColor;
        float _EmissionStrength;
        float _VertexColorInfluence;
        float _AmbientBoost;
        fixed4 _BounceColor;
        float _BounceStrength;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
            float4 vertColor : COLOR;
        };

        struct SurfaceOutputHuaxia
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

        // 三向投影权重：主轴占主导，避免斜面上出现「三张图叠在一起」的重影
        float3 TriWeights (float3 n)
        {
            float3 w = abs(normalize(n));
            w = pow(w, 8.0);
            return w / max(w.x + w.y + w.z, 1e-4);
        }

        fixed4 TriplanarScale (float3 p, float3 n, float scale)
        {
            float3 w = TriWeights(n);
            fixed4 cx = tex2D(_MainTex, p.zy * scale);
            fixed4 cy = tex2D(_MainTex, p.xz * scale);
            fixed4 cz = tex2D(_MainTex, p.xy * scale);
            return cx * w.x + cy * w.y + cz * w.z;
        }

        // 把切空间法线图按三个投影面转到世界空间，再按同一组权重混合
        float3 TriplanarNormal (float3 p, float3 n)
        {
            float3 w = TriWeights(n);

            float3 nx = tex2D(_BumpMap, p.zy * _DetailScale).xyz * 2.0 - 1.0;
            float3 ny = tex2D(_BumpMap, p.xz * _DetailScale).xyz * 2.0 - 1.0;
            float3 nz = tex2D(_BumpMap, p.xy * _DetailScale).xyz * 2.0 - 1.0;

            // 各面切线基不同：X 面 (x,y,z)->(z,y,x)、Y 面 ->(x,z,y)、Z 面保持原序
            float3 wx = normalize(float3(nx.z, nx.y, max(nx.x, 0.06)));
            float3 wy = normalize(float3(ny.x, max(ny.z, 0.06), ny.y));
            float3 wz = normalize(float3(nz.x, nz.y, max(nz.z, 0.06)));

            return normalize(wx * w.x + wy * w.y + wz * w.z);
        }

        void surf (Input IN, inout SurfaceOutputHuaxia o)
        {
            fixed3 detail = TriplanarScale(IN.worldPos, IN.worldNormal, _DetailScale).rgb;
            // 细节纹理均值约 0.78，乘 1.28 还原到 1.0，只保留起伏不改变整体明度
            fixed3 detailMul = lerp(fixed3(1, 1, 1), detail * 1.28, _DetailStrength);

            // 第二层粗粒度斑驳：打破小尺度纹理的平铺重复，让大墙面有脏与旧
            fixed3 macro = TriplanarScale(IN.worldPos, IN.worldNormal, _MacroScale).rgb;
            fixed3 macroMul = lerp(fixed3(1, 1, 1), macro * 1.28, _MacroStrength);

            fixed3 vc = lerp(fixed3(1, 1, 1), IN.vertColor.rgb, saturate(_VertexColorInfluence));
            o.Albedo = _Color.rgb * detailMul * macroMul * vc;

            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normalize(IN.worldNormal)));
            o.Rim = pow(rim, _RimPower) * _RimStrength;

            // 法线扰动写进世界法线：自定义光照函数只认 s.WorldNormal
            float3 baseN = normalize(IN.worldNormal);
            float3 bumpN = TriplanarNormal(IN.worldPos, IN.worldNormal);
            o.WorldNormal = normalize(lerp(baseN, bumpN, saturate(_BumpScale)));
            // 不写 o.Normal：一旦写它，surface shader 生成代码会强制走
            // WorldNormalVector（切空间→世界）通道，与我们直接给世界法线的做法冲突。

            o.Emission = _EmissionColor.rgb * _EmissionStrength;
            o.Specular = 0.0;
            o.Gloss = _Glossiness;
            o.Alpha = 1.0;
        }

        half4 LightingHuaxia (SurfaceOutputHuaxia s, half3 viewDir, UnityGI gi)
        {
            half3 nrm = normalize(s.WorldNormal);
            half ndl = dot(nrm, gi.light.dir);
            half wrap = saturate((ndl + _Wrap) / (1.0 + _Wrap));

            // 柔和两阶：暗部偏冷、亮部还原固有色，避免硬切卡通感
            half soft = _RampSoftness * 0.5;
            half lit = smoothstep(0.5 - soft, 0.5 + soft, wrap);
            half3 ramp = lerp(_ShadowTint.rgb, fixed3(1, 1, 1), lit);

            half3 diffuse = s.Albedo * gi.light.color * ramp;

            half3 h = normalize(gi.light.dir + viewDir);
            half spec = pow(saturate(dot(nrm, h)), 48.0) * s.Gloss * 0.35;
            half3 specular = gi.light.color * spec;

            half3 rim = _RimColor.rgb * s.Rim * gi.light.color * 0.5;

            // 自定义光照函数不会自动叠加环境光，需显式取 SH 间接光
            half3 ambient = s.Albedo * gi.indirect.diffuse * _AmbientBoost;

            // 风格化地面反弹光：朝下的面（檐底、台基阴面）不再死黑
            half bounce = saturate(-nrm.y) * _BounceStrength;

            half4 c;
            c.rgb = diffuse + specular + rim + s.Emission + ambient + _BounceColor.rgb * bounce;
            c.a = s.Alpha;
            return c;
        }

        inline void LightingHuaxia_GI (SurfaceOutputHuaxia s, UnityGIInput data, inout UnityGI gi)
        {
            gi = UnityGlobalIllumination(data, 1.0, normalize(s.WorldNormal));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
