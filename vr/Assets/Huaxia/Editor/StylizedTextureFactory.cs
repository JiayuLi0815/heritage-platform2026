using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 程序化生成可平铺的灰阶细节纹理（均值统一为 0.78），供 Huaxia/StylizedLit 的
    /// 世界坐标 triplanar 采样使用。避免引入外部贴图资源，同时消除缩放立方体的 UV 拉伸。
    /// </summary>
    public static class StylizedTextureFactory
    {
        public const string Dir = "Assets/Huaxia/Textures";
        const int Size = 256;
        const float TargetMean = 0.78f;

        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> normalCache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string kind)
        {
            if (string.IsNullOrEmpty(kind)) kind = "paper";

            Texture2D cached;
            if (cache.TryGetValue(kind, out cached) && cached != null) return cached;

            if (!AssetDatabase.IsValidFolder(Dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Huaxia")) AssetDatabase.CreateFolder("Assets", "Huaxia");
                AssetDatabase.CreateFolder("Assets/Huaxia", "Textures");
            }

            string path = Dir + "/hx_" + kind + ".png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                tex = Build(kind);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Configure(path);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            cache[kind] = tex;
            return tex;
        }

        /// <summary>生成经度-纬度（latlong）天空渐变，供 Skybox/Panoramic 使用。cloudAmount 控制云量。</summary>
        public static Texture2D SkyGradient(string key, Color zenith, Color mid, Color horizon, Color ground,
                                           float cloudAmount = 1f)
        {
            if (!AssetDatabase.IsValidFolder(Dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Huaxia")) AssetDatabase.CreateFolder("Assets", "Huaxia");
                AssetDatabase.CreateFolder("Assets/Huaxia", "Textures");
            }

            // v4：云带加宽、加厚。改版本号让旧缓存自动重建。
            string path = Dir + "/hx_sky5_" + key + ".png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return tex;

            const int w = 512, h = 256;
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h;

                Color c;
                // 分段必须首尾相接，否则天空会出现一条硬边
                if (v > 0.60f)
                    c = Color.Lerp(mid, zenith, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 1f, v)));
                else if (v > 0.515f)
                    c = Color.Lerp(horizon, mid, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.515f, 0.60f, v)));
                else if (v > 0.500f)
                    c = Color.Lerp(horizon * 0.97f, horizon, Mathf.InverseLerp(0.500f, 0.515f, v));
                else
                    c = Color.Lerp(ground * 0.72f, ground, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 0.50f, v)));

                // 地平线附近压一条很淡的暖光带，避免天空过于平铺
                float band = Mathf.Exp(-Mathf.Pow((v - 0.535f) * 46f, 2f));
                c = Color.Lerp(c, c * new Color(1.06f, 1.0f, 0.94f), band * 0.8f);

                // 0.50 是地平线，0.5–0.75 对应仰角 0–45°：平视镜头能拍到的天空全在这一段，
                // 云再高就只剩天顶一块空地。低空部分被山挡住，所以云的重点放在 0.53–0.68。
                float cloudBand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.505f, 0.535f, v))
                                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.660f, 0.790f, v)));
                // 云的受光面要明显亮过天，背光面明显冷过天，否则云和天空是一个色，等于没有云
                Color cloudLit = Color.Lerp(Color.Lerp(mid, horizon, 0.30f), new Color(0.985f, 0.97f, 0.93f), 0.90f);
                Color cloudDark = Color.Lerp(cloudLit * 0.62f, new Color(0.44f, 0.51f, 0.63f), 0.46f);

                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    float drift = (Mathf.PerlinNoise(u * 6f + 3.1f, v * 9f + 7.7f) - 0.5f) * 0.016f;
                    Color p = new Color(c.r + drift, c.g + drift, c.b + drift, 1f);

                    if (cloudBand > 0.001f)
                    {
                        // 横向拉长：u 低频（几个大云团绕一圈），v 稍高（层层叠高）
                        float n1 = Fbm2(u, v, 2.6f, 9.0f, 211, 4);
                        float n2 = Fbm2(u, v, 7.0f, 17.0f, 223, 3);
                        float field = n1 * 0.72f + n2 * 0.28f;
                        float cover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.395f, 0.585f, field)) * cloudBand;
                        float shade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.62f, field));
                        Color cloud = Color.Lerp(cloudDark, cloudLit, shade);
                        p = Color.Lerp(p, cloud, cover * 0.94f * Mathf.Clamp01(cloudAmount));
                    }

                    px[y * w + x] = p;
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureSky(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void ConfigureSky(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void Configure(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = Size;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// 取与 Get(kind) 严格对应的切空间法线图（RGB 编码、关闭 sRGB）。
        /// 由同一张高度场做 Sobel 得到，因此起伏与 albedo 的纹理完全咬合。
        /// </summary>
        public static Texture2D GetNormal(string kind)
        {
            if (string.IsNullOrEmpty(kind)) kind = "paper";

            Texture2D cached;
            if (normalCache.TryGetValue(kind, out cached) && cached != null) return cached;

            EnsureFolder();

            string path = Dir + "/hx_" + kind + "_n.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                tex = BuildNormal(kind);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ConfigureNormal(path);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            normalCache[kind] = tex;
            return tex;
        }

        /// <summary>
        /// 只重建纹理、不重建场景。调完 Sample() 之后跑这个，几十秒就能看到新图，
        /// 不必等完整的 BuildAll。
        /// </summary>
        public static void RebuildAll()
        {
            EnsureFolder();
            var kinds = new List<string>
            {
                "paper", "plaster", "tile", "fabric", "wood", "metal", "stone", "earth",
                "foliage", "daub", "rammed", "brick", "thatch", "bark", "bronze", "pottery", "jade", "ridge"
            };

            foreach (var k in kinds)
            {
                foreach (var suffix in new[] { "", "_n" })
                {
                    string p = Dir + "/hx_" + k + suffix + ".png";
                    if (File.Exists(p)) File.Delete(p);
                    if (File.Exists(p + ".meta")) File.Delete(p + ".meta");
                }
            }

            AssetDatabase.Refresh();
            cache.Clear();
            normalCache.Clear();

            foreach (var k in kinds)
            {
                Get(k);
                GetNormal(k);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Huaxia] 已重建 " + kinds.Count + " 种细节纹理（albedo + normal）");
        }

        static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(Dir)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Huaxia")) AssetDatabase.CreateFolder("Assets", "Huaxia");
            AssetDatabase.CreateFolder("Assets/Huaxia", "Textures");
        }

        static void ConfigureNormal(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            // 法线是数据不是颜色：必须 Default + 关 sRGB，否则 gamma 会把起伏压平
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = Size;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static float[] BlurOnce(float[] src)
        {
            var dst = new float[src.Length];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float sum = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = (y + dy + Size) % Size;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = (x + dx + Size) % Size;
                            sum += src[yy * Size + xx];
                        }
                    }
                    dst[y * Size + x] = sum / 9f;
                }
            }
            return dst;
        }

        static Texture2D BuildNormal(string kind)
        {
            var height = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    height[y * Size + x] = Sample(kind, (x + 0.5f) / Size, (y + 0.5f) / Size);
            }

            // 先抹掉逐像素的砂粒噪点：法线要读的是「夯层 / 砖缝 / 草木」这种结构，
            // 不是 256 象素级的白噪声。两层 3x3 模糊刚好保住 20 象素以上的构造。
            for (int pass = 0; pass < 2; pass++) height = BlurOnce(height);

            // 归一化高度再求梯度，保证不同 kind 的法线强度可比
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < height.Length; i++)
            {
                if (height[i] < min) min = height[i];
                if (height[i] > max) max = height[i];
            }
            float span = Mathf.Max(max - min, 1e-4f);

            // k 越大起伏越陡；contrast 高的 kind（瓦、茅草）自动更明显
            float k = 6f * ContrastOf(kind);

            var px = new Color32[height.Length];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // 环绕取样，保证法线图与 albedo 一样可无缝平铺
                    int xl = (x - 1 + Size) % Size, xr = (x + 1) % Size;
                    int yd = (y - 1 + Size) % Size, yu = (y + 1) % Size;

                    float hl = (height[y * Size + xl] - min) / span;
                    float hr = (height[y * Size + xr] - min) / span;
                    float hd = (height[yd * Size + x] - min) / span;
                    float hu = (height[yu * Size + x] - min) / span;

                    Vector3 n = new Vector3(-(hr - hl) * k, -(hu - hd) * k, 1f).normalized;
                    px[y * Size + x] = new Color32(
                        (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f),
                        255);
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true);
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply(true, false);
            return tex;
        }

        static Texture2D Build(string kind)
        {
            var values = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size;
                    float v = (y + 0.5f) / Size;
                    values[y * Size + x] = Sample(kind, u, v);
                }
            }

            // 归一化：以 0.78 为中心，只保留纹理起伏，不改变整体明度
            float mean = 0f;
            for (int i = 0; i < values.Length; i++) mean += values[i];
            mean /= values.Length;

            var pixels = new Color32[values.Length];
            float contrast = ContrastOf(kind);
            for (int i = 0; i < values.Length; i++)
            {
                float g = Mathf.Clamp01(TargetMean + (values[i] - mean) * contrast);
                byte b = (byte)Mathf.RoundToInt(g * 255f);
                pixels[i] = new Color32(b, b, b, 255);
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false);
            tex.SetPixels32(pixels);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply(true, false);
            return tex;
        }

        static float ContrastOf(string kind)
        {
            switch (kind)
            {
                case "paper": return 0.55f;
                case "plaster": return 0.95f;
                case "tile": return 1.25f;
                case "fabric": return 0.75f;
                case "wood": return 1.0f;
                case "metal": return 0.8f;
                case "stone": return 1.15f;
                case "earth": return 1.05f;
                case "daub": return 1.1f;
                case "foliage": return 1.25f;
                case "rammed": return 1.0f;
                case "brick": return 1.1f;
                case "thatch": return 1.35f;
                case "bark": return 1.3f;
                case "bronze": return 0.95f;
                case "pottery": return 0.95f;
                case "jade": return 0.6f;
                case "ridge": return 0.7f;
                default: return 0.9f;
            }
        }

        static float Sample(string kind, float u, float v)
        {
            switch (kind)
            {
                case "wood":
                {
                    // 顺纹条带 + 细木理，纵向为主
                    float warp = Fbm(u, v, 2.6f, 3, 4) - 0.5f;
                    float stripes = Mathf.Sin((u * 44f + warp * 2.2f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    float fine = Fbm2(u, v, 90f, 14f, 1, 3);
                    return 0.5f + (stripes - 0.5f) * 0.16f + (fine - 0.5f) * 0.13f;
                }
                case "stone":
                {
                    // 花岗岩是细粒的：低频只留极弱的一层，主靠高频颗粒读质感。
                    // 低频一强就会在近景变成迷彩斑块，这是之前最刺眼的问题。
                    float baseN = Fbm(u, v, 40f, 4, 2);
                    float grit = Fbm(u, v, 96f, 9, 2);
                    float speck = Fbm(u, v, 150f, 23, 1);
                    float crack = 1f - Mathf.Abs(Fbm(u, v, 5f, 17, 3) - 0.5f) * 2f;
                    crack = Mathf.Pow(Mathf.Clamp01(crack), 26f);
                    return 0.5f + (baseN - 0.5f) * 0.08f + (grit - 0.5f) * 0.09f
                         + (speck - 0.5f) * 0.055f - crack * 0.05f;
                }
                case "tile":
                {
                    // 瓦垄：u 方向密排半圆凸起，v 方向细密搭接线
                    float rib = Mathf.Repeat(u * 9f, 1f);
                    float crown = Mathf.Sin(rib * Mathf.PI);
                    float seam = Mathf.Sin(v * Mathf.PI * 2f * 13f) * 0.5f + 0.5f;
                    float grit = Fbm(u, v, 42f, 13, 2);
                    return 0.32f + crown * 0.44f + seam * 0.05f + (grit - 0.5f) * 0.10f;
                }
                case "earth":
                {
                    float baseN = Fbm(u, v, 7f, 5, 4);
                    float grit = Fbm(u, v, 48f, 11, 2);
                    float clod = Fbm(u, v, 18f, 13, 3);
                    return 0.5f + (baseN - 0.5f) * 0.42f + (grit - 0.5f) * 0.20f
                         + (clod - 0.5f) * 0.16f;
                }
                case "plaster":
                {
                    // 抹面：大块深浅不匀 + 抹子留下的细长刮痕 + 砂粒
                    float baseN = Fbm(u, v, 3.2f, 7, 4);
                    float stain = Fbm(u, v, 6f, 23, 3);
                    float grit = Fbm(u, v, 44f, 19, 2);
                    float trowel = Fbm2(u, v, 22f, 3.5f, 29, 2);
                    return 0.5f + (baseN - 0.5f) * 0.30f + (stain - 0.5f) * 0.22f
                         + (grit - 0.5f) * 0.12f + (trowel - 0.5f) * 0.10f;
                }
                case "daub":
                {
                    // 草拌泥：土体斑驳 + 短草筋（沿 u 拉长）+ 抹子压出的浅弧
                    float soil = Fbm(u, v, 5f, 43, 4);
                    float straw = Fbm2(u, v, 46f, 9f, 47, 3);
                    float smear = Fbm2(u, v, 4f, 11f, 49, 2);
                    return 0.5f + (soil - 0.5f) * 0.34f + (straw - 0.5f) * 0.26f
                         + (smear - 0.5f) * 0.14f;
                }
                case "fabric":
                {
                    float weave = (Mathf.Sin(u * 84f * Mathf.PI * 2f) + Mathf.Sin(v * 84f * Mathf.PI * 2f)) * 0.25f;
                    float baseN = Fbm(u, v, 4.5f, 13, 3);
                    return 0.5f + (baseN - 0.5f) * 0.12f + weave * 0.05f;
                }
                case "foliage":
                {
                    float clump = Fbm(u, v, 7f, 29, 4);
                    float leaf = Fbm(u, v, 30f, 31, 3);
                    return 0.5f + (clump - 0.5f) * 0.5f + (leaf - 0.5f) * 0.3f;
                }
                case "metal":
                {
                    float brush = Fbm(u, v, 60f, 37, 2);
                    float patina = Fbm(u, v, 9f, 41, 3);
                    return 0.5f + (brush - 0.5f) * 0.1f + (patina - 0.5f) * 0.16f;
                }
                case "rammed":
                {
                    // 夯土不是木头：连续等高的横线一到墙面上就是木挂板或木纹贴皮。
                    // 只留颗粒、斑块和一点横向拉长的低幅压实痕，层理由几何体去交代。
                    float mott = Fbm(u, v, 6f, 55, 3);
                    float grit = Fbm(u, v, 42f, 53, 2);
                    float streak = Fbm(u, v * 0.25f, 5f, 51, 2);
                    return 0.5f + (mott - 0.5f) * 0.10f + (grit - 0.5f) * 0.08f
                                 + (streak - 0.5f) * 0.05f;
                }
                case "brick":
                {
                    // 错缝砖砌：13 皮 / 6.5 砖，砖缝凹下去
                    float rows = 13f;
                    float rv = Mathf.Repeat(v * rows, 1f);
                    float cu = Mathf.Repeat(u * 6.5f + Mathf.Floor(v * rows) * 0.5f, 1f);
                    float joint = Mathf.Min(
                        Ramp(rv, 0.07f) * Ramp(1f - rv, 0.07f),
                        Ramp(cu, 0.05f) * Ramp(1f - cu, 0.05f));
                    float grit = Fbm(u, v, 50f, 61, 2);
                    return Mathf.Lerp(0.36f, 0.63f, joint) + (grit - 0.5f) * 0.10f;
                }
                case "thatch":
                {
                    // 茅草/稻草：沿 u 拉长的草茎，叠成一束一束
                    float streak = Fbm2(u, v, 58f, 5f, 71, 3);
                    float bundle = Mathf.Sin(v * Mathf.PI * 2f * 9f + (Fbm(u, v, 4f, 73, 2) - 0.5f) * 5f) * 0.5f + 0.5f;
                    float clump = Fbm(u, v, 6f, 75, 3);
                    return 0.46f + (streak - 0.5f) * 0.5f + (bundle - 0.5f) * 0.16f + (clump - 0.5f) * 0.2f;
                }
                case "bark":
                {
                    // 树皮：沿 v 的深纵裂 + 细横纹
                    float fis = 1f - Mathf.Abs(Fbm2(u, v, 13f, 2.2f, 81, 3) - 0.5f) * 2f;
                    fis = Mathf.Pow(Mathf.Clamp01(fis), 6f);
                    float grain = Fbm2(u, v, 28f, 3f, 83, 2);
                    return 0.5f + (grain - 0.5f) * 0.3f - fis * 0.22f;
                }
                case "bronze":
                {
                    // 青铜：铜锈斑 + 细颗粒 + 少量锈蚀凹坑
                    float patina = Fbm(u, v, 6f, 91, 4);
                    float pit = Fbm(u, v, 34f, 93, 2);
                    float edge = 1f - Mathf.Abs(Fbm(u, v, 11f, 95, 3) - 0.5f) * 2f;
                    return 0.5f + (patina - 0.5f) * 0.34f + (pit - 0.5f) * 0.16f
                         - Mathf.Pow(Mathf.Clamp01(edge), 8f) * 0.12f;
                }
                case "pottery":
                {
                    // 陶：细泥颗粒 + 轮制旋纹 + 气孔
                    float grit = Fbm(u, v, 55f, 101, 2);
                    float ring = Mathf.Sin(v * Mathf.PI * 2f * 22f) * 0.5f + 0.5f;
                    float hole = Fbm(u, v, 18f, 103, 3);
                    return 0.5f + (grit - 0.5f) * 0.18f + (ring - 0.5f) * 0.05f + (hole - 0.5f) * 0.12f;
                }
                case "jade":
                {
                    // 玉：云雾状色带 + 极淡的石筋，几乎不做高频
                    float cloud = Fbm(u, v, 3.5f, 111, 4);
                    float vein = 1f - Mathf.Abs(Fbm(u, v, 7f, 113, 3) - 0.5f) * 2f;
                    vein = Mathf.Pow(Mathf.Clamp01(vein), 10f);
                    return 0.5f + (cloud - 0.5f) * 0.18f - vein * 0.07f;
                }
                case "ridge":
                {
                    // 远山：只留极粗的软起伏，避免世界坐标纹理在远山上拉出条带
                    float big = Fbm(u, v, 2.2f, 121, 3);
                    float mid = Fbm(u, v, 5f, 123, 2);
                    return 0.5f + (big - 0.5f) * 0.5f + (mid - 0.5f) * 0.2f;
                }
                default: // paper 宣纸：细纤维 + 极浅色斑
                {
                    float fiber = Mathf.PerlinNoise(u * 150f, v * 8f) * 0.5f + Mathf.PerlinNoise(u * 12f, v * 170f) * 0.5f;
                    float baseN = Fbm(u, v, 3.4f, 43, 4);
                    return 0.5f + (baseN - 0.5f) * 0.12f + (fiber - 0.5f) * 0.12f;
                }
            }
        }

        /// <summary>各向异性的无缝分形噪声：u/v 方向频率可不同，用于草茎、树皮等条状纹理。</summary>
        // 0 在边缘、1 在 e 之外，边缘内平滑过渡
        static float Ramp(float t, float e) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, e, t));

        static float Fbm2(float u, float v, float fu, float fv, int seed, int octaves)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            float a = fu, b = fv;
            for (int i = 0; i < octaves; i++)
            {
                sum += Tileable2(u, v, a, b, seed + i * 7) * amp;
                norm += amp;
                amp *= 0.5f;
                a *= 2f;
                b *= 2f;
            }
            return sum / Mathf.Max(norm, 1e-4f);
        }

        static float Tileable2(float u, float v, float fu, float fv, int seed)
        {
            float ox = seed * 13.37f;
            float oy = seed * 7.71f;
            float su = u * fu;
            float sv = v * fv;

            float a = Mathf.PerlinNoise(ox + su, oy + sv);
            float b = Mathf.PerlinNoise(ox + su - fu, oy + sv);
            float c = Mathf.PerlinNoise(ox + su, oy + sv - fv);
            float d = Mathf.PerlinNoise(ox + su - fu, oy + sv - fv);

            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>无缝分形噪声：四角位移加权混合，保证上下左右可平铺。</summary>
        static float Fbm(float u, float v, float freq, int seed, int octaves)
        {
            float sum = 0f, amp = 1f, norm = 0f, f = freq;
            for (int i = 0; i < octaves; i++)
            {
                sum += Tileable(u, v, f, seed + i * 7) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2f;
            }
            return sum / Mathf.Max(norm, 1e-4f);
        }

        static float Tileable(float u, float v, float freq, int seed)
        {
            float ox = seed * 13.37f;
            float oy = seed * 7.71f;
            float su = u * freq;
            float sv = v * freq;

            float a = Mathf.PerlinNoise(ox + su, oy + sv);
            float b = Mathf.PerlinNoise(ox + su - freq, oy + sv);
            float c = Mathf.PerlinNoise(ox + su, oy + sv - freq);
            float d = Mathf.PerlinNoise(ox + su - freq, oy + sv - freq);

            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }
    }
}
