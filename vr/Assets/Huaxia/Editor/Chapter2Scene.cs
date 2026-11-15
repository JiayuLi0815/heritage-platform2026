using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 第二章 · 秦陵（骊山北麓陵园）。
    /// 空间序列：外城阙门 → 神道 → 夯土城垣 → 陪葬坑（棚木 / 隔墙 / 陶俑）→ 封土。
    /// 严格取舍：不做地宫奇观、不做机关宝藏，只复原可验证的地面遗存。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        // ---------------------------------------------------------------- 布局常量

        const float QnInnerX = 46f;
        const float QnInnerZ0 = -22f;
        const float QnInnerZ1 = 76f;
        const float QnOuterX = 74f;
        const float QnOuterZ0 = -42f;
        const float QnOuterZ1 = 94f;

        const float QnMoundZ = 34f;
        const float QnMoundTop = 21.0f;
        // 陪葬坑在陵园外城以东，与封土拉开距离：符合"坑在园外"的实际情况
        const float QnPitX = 124f;
        const float QnPitZ = 4f;
        const float QnBronzeX = 60f;
        const float QnBronzeZ = 6f;

        // ---------------------------------------------------------------- 地形

        /// <summary>矩形掩膜：内部为 1，边界 feather 宽度内平滑过渡。</summary>
        static float QnRect(float x, float z, float x0, float x1, float z0, float z1, float feather)
        {
            float dx = Mathf.Min(x - x0, x1 - x);
            float dz = Mathf.Min(z - z0, z1 - z);
            float d = Mathf.Min(dx, dz);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-feather, feather, d));
        }

        /// <summary>
        /// 山体：各向异性的宽丘，脊线用低频噪声起伏。
        /// 原来的圆丘只有 15–30 m 高，站在陵园里几乎看不到，标题里那个「秦岭」就没了。
        /// 现在主脊 150 m 起，山脚在 z≈-150，一道山墙把南边封住。
        /// </summary>
        static float QnMassif(float x, float z, float cx, float cz, float rx, float rz, float h, float seed)
        {
            float dx = (x - cx) / rx, dz = (z - cz) / rz;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d >= 1f) return 0f;
            float wob = 0.78f + 0.40f * Mathf.PerlinNoise(x * 0.011f + seed, z * 0.011f + seed * 1.7f);
            return h * wob * Mathf.Pow(Mathf.Cos(d * Mathf.PI * 0.5f), 1.9f);
        }

        /// <summary>小丘。泉州章节还在用它。</summary>
        static float QnHill(float x, float z, float cx, float cz, float rx, float rz, float h)
        {
            float dx = (x - cx) / rx, dz = (z - cz) / rz;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d >= 1f) return 0f;
            return h * Mathf.Pow(Mathf.Cos(d * Mathf.PI * 0.5f), 2.2f);
        }

        static float QinlingHeight(float x, float z)
        {
            float h = 0f;

            h += (Mathf.Sin(x * 0.041f + 1.3f) + Mathf.Cos(z * 0.036f - 0.7f)) * 0.34f;
            h += Mathf.Sin(x * 0.11f + z * 0.07f) * 0.12f;

            // 陵园台地：内城略高于外城，外城略高于原野
            h += 0.55f * QnRect(x, z, -QnOuterX, QnOuterX, QnOuterZ0, QnOuterZ1, 22f);
            h += 0.75f * QnRect(x, z, -QnInnerX, QnInnerX, QnInnerZ0, QnInnerZ1, 14f);

            // 封土基座隆起
            h += 1.6f * QnRect(x, z, -38f, 38f, QnMoundZ - 38f, QnMoundZ + 38f, 16f);

            // 兵马俑坑与铜车马坑挖方
            // 一号坑挖方：76 × 33 m 的坑，深 5 m（原坑 230 × 62 m、深约 5 m）。
            // 直接压平到 -5 m，不再叠加底噪——否则坑底起伏 ±0.5 m，
            // 陶俑站上去要么陷进地砖、要么悬空，整座军阵都在"飘"。
            float qPitMask = QnRect(x, z, QnPitX - 38.8f, QnPitX + 38.8f, QnPitZ - 16.9f, QnPitZ + 16.9f, 1.2f);
            h = Mathf.Lerp(h, -5.0f, qPitMask);
            h -= 1.10f * QnRect(x, z, QnBronzeX - 8f, QnBronzeX + 8f, QnBronzeZ - 6f, QnBronzeZ + 6f, 1.2f);

            // 骊山主脊（-Z 为南）：山脚从 z≈-150 起，峰顶 150–210 m。
            // 三段错开的山体叠出前后层次，不至于是一条均匀的圆弧。
            h += QnMassif(x, z, 0f, -268f, 332f, 118f, 150f, 3.1f);
            h += QnMassif(x, z, -150f, -300f, 170f, 130f, 176f, 7.7f);
            h += QnMassif(x, z, 150f, -300f, 170f, 130f, 168f, 11.3f);
            h += QnMassif(x, z, 0f, -420f, 420f, 160f, 210f, 5.3f);
            // 东西两翼把山谷合起来
            h += QnMassif(x, z, -292f, -10f, 140f, 280f, 118f, 13.9f);
            h += QnMassif(x, z, 292f, -20f, 140f, 280f, 110f, 17.1f);
            // 北面低丘：收住地平线，但不抢南面的主景
            h += QnMassif(x, z, 0f, 336f, 360f, 110f, 66f, 19.7f);
            h += QnMassif(x, z, -260f, 250f, 150f, 130f, 58f, 23.3f);
            h += QnMassif(x, z, 260f, 250f, 150f, 130f, 62f, 29.1f);
            // 前排两个小峰，把"两个圆包"的天际线打散
            h += QnMassif(x, z, -74f, -232f, 86f, 64f, 46f, 41.3f);
            h += QnMassif(x, z, 96f, -244f, 78f, 58f, 40f, 43.1f);

            // 山体的碎褶：余弦丘太圆，像沙丘。把 Perlin 噪声取绝对值后翻成尖脊，
            // 再叠一层细的冲沟，山才有了秦岭那种一道道梁和沟的走向。
            if (h > 4f)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 32f, h));
                // 三个尺度的尖脊噪声：大山梁（~125 m）、支脊（~50 m）、碎石沟（~20 m）
                float r1 = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x * 0.008f + 27.3f, z * 0.008f + 8.9f) - 1f);
                float r2 = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x * 0.020f + 5.1f, z * 0.020f + 19.7f) - 1f);
                float r3 = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x * 0.050f + 61.7f, z * 0.050f + 43.3f) - 1f);
                // 第四层 6 m 级的碎褶：没有它，坡面在长焦下就是一整块抹平的绿，
                // 像苔藓不是树林。地形网格 1.6 m 一格，这一层刚好咬得住。
                float r4 = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x * 0.16f + 33.7f, z * 0.16f + 12.9f) - 1f);
                h += k * (r1 * r1 * 44f + r2 * r2 * 16f + r3 * r3 * 5f + r4 * r4 * 3.2f - 30f);
            }

            return h;
        }

        static Color QinlingColor(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.055f + 13.7f, z * 0.055f + 6.1f);
            float n2 = Mathf.PerlinNoise(x * 0.17f + 41.3f, z * 0.17f + 9.9f);
            float n3 = Mathf.PerlinNoise(x * 0.021f + 71.3f, z * 0.021f + 3.7f);

            // 塬上不是沙漠。原来整片原野都是土黄，隔着一百米看就是一片沙滩，
            // 这是"看不出秦岭"最直接的原因：秦岭北麓的谷地里长的是草和树。
            Color loess = Color.Lerp(new Color(0.44f, 0.37f, 0.24f), new Color(0.50f, 0.43f, 0.29f), n);
            Color dryGrass = new Color(0.34f, 0.36f, 0.20f);
            Color meadow = new Color(0.29f, 0.35f, 0.18f);
            Color c = Color.Lerp(loess, dryGrass, Mathf.SmoothStep(0.36f, 0.72f, n2) * 0.80f);
            c = Color.Lerp(c, meadow, Mathf.SmoothStep(0.24f, 0.62f, n) * 0.52f);

            // 陵园内：踩踏成路的黄土更亮
            float yard = QnRect(x, z, -QnInnerX, QnInnerX, QnInnerZ0, QnInnerZ1, 10f);
            c = Color.Lerp(c, new Color(0.55f, 0.48f, 0.33f), yard * 0.30f);

            // 神道
            float road = QnRect(x, z, -5.2f, 5.2f, QnOuterZ0 - 30f, QnMoundZ - 26f, 2.4f);
            c = Color.Lerp(c, new Color(0.58f, 0.51f, 0.36f), road * 0.50f);

            // 封土坡面长草
            float mound = QnRect(x, z, -32f, 32f, QnMoundZ - 32f, QnMoundZ + 32f, 8f);
            c = Color.Lerp(c, new Color(0.33f, 0.36f, 0.23f), mound * 0.55f);

            // 几十米尺度的明暗：田野、撂荒地、灌木斑交错，别让平原是一块纯色板。
            // 光靠乘法调明暗，远看还是一整块绿；掺进秸秆色的田块，
            // 才有北方塬上那种"收割过的麦田 / 冬小麦 / 裸土"拼出来的花田。
            float broad = Mathf.PerlinNoise(x * 0.012f + 5.7f, z * 0.012f + 2.3f);
            c *= Mathf.Lerp(0.80f, 1.10f, broad);
            float field = Mathf.PerlinNoise(x * 0.034f + 31.3f, z * 0.034f + 17.9f);
            c = Color.Lerp(c, new Color(0.47f, 0.42f, 0.24f),
                Mathf.SmoothStep(0.56f, 0.80f, field) * 0.46f);

            // 陵园外的针叶林带：原野在这里先转暗绿，再交给山体
            float r = Mathf.Sqrt(x * x + (z - 8f) * (z - 8f));
            float belt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(96f, 230f, r));
            c = Color.Lerp(c, new Color(0.24f, 0.31f, 0.19f), belt * 0.74f);

            // 骊山北坡：缓坡长满松栎，陡坎露赭石与灰岩。
            // 光靠海拔分色带会让整座山一个色，得看坡度：岩壁和沟底是灰的，
            // 山梁两肩是绿的——这也是秦岭照片里最抓眼的一层结构。
            float hgt = QinlingHeight(x, z);
            float mtn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 26f, hgt));
            if (mtn > 0f)
            {
                float dhx = QinlingHeight(x + 2.5f, z) - QinlingHeight(x - 2.5f, z);
                float dhz = QinlingHeight(x, z + 2.5f) - QinlingHeight(x, z - 2.5f);
                float slope = Mathf.Sqrt(dhx * dhx + dhz * dhz) / 5f;
                // 秦岭北坡几乎整个坡面都长树，岩壁只在近垂直的断崖上露一条。
                // 之前 1.9–3.0 的阈值仍然太低，整座山是灰的，读成水泥堆。
                float steep = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.8f, 4.3f,
                    slope + (n2 - 0.5f) * 0.30f));

                // 林冠不是一整块绿：十几米一块的明暗斑，远看才有"树"的颗粒感
                float patch = Mathf.PerlinNoise(x * 0.09f + 3.3f, z * 0.09f + 27.1f);
                // 林冠要明暗咬得开：同一种绿里差三个层次，远看才是树的颗粒，不是苔藓。
                // 底值不能压到 0.1——工程是线性色彩空间，0.1 的 sRGB 只有 0.01 的反射率，
                // 背光的北坡就成了一块黑布。这里按真实树叶的反射率（sRGB 0.2–0.5）取。
                float canopy = Mathf.PerlinNoise(x * 0.23f + 57.1f, z * 0.23f + 61.7f);
                Color forest = Color.Lerp(new Color(0.20f, 0.32f, 0.15f), new Color(0.38f, 0.52f, 0.24f), patch);
                forest *= Mathf.Lerp(0.62f, 1.32f, canopy);
                // 松栎混交的黄绿斑：秦岭秋天的山坡上总有几片开始转色的阔叶
                forest = Color.Lerp(forest, new Color(0.46f, 0.45f, 0.20f),
                    Mathf.SmoothStep(0.66f, 0.90f, n2) * 0.26f);
                // 空气透视往冷绿偏，不是往灰偏——灰化会把山推回水泥
                forest = Color.Lerp(forest, new Color(0.42f, 0.52f, 0.46f),
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(90f, 200f, hgt)) * 0.24f);

                Color rock = Color.Lerp(new Color(0.48f, 0.45f, 0.38f), new Color(0.60f, 0.56f, 0.48f), n3);
                float bare = Mathf.Max(steep * 0.55f, Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(150f, 200f, hgt + (n3 - 0.5f) * 26f)) * 0.80f);
                Color mtnCol = Color.Lerp(forest, rock, bare);
                c = Color.Lerp(c, mtnCol, mtn);
            }

            return c;
        }

        // ---------------------------------------------------------------- 环境

        static void SetupQinlingEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(new Color(0.62f, 0.68f, 0.72f));
            RenderSettings.ambientEquatorColor = Lin(new Color(0.54f, 0.51f, 0.40f));
            RenderSettings.ambientGroundColor = Lin(new Color(0.34f, 0.30f, 0.22f));
            RenderSettings.ambientIntensity = 1.02f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // 山现在离陵园 300 m 左右（远景压缩过的尺度），要按"两三公里外的山"
            // 来读，雾得比 0.0016 重一档，山才有空气透视，而不是一块贴在天边的色板。
            RenderSettings.fogDensity = 0.0026f;
            RenderSettings.fogColor = Lin(new Color(0.70f, 0.72f, 0.71f));

            // 关中深秋：高而薄的灰蓝天，地面反光是土黄
            RenderSettings.skybox = SkyMat("Sky_Qinling",
                new Color(0.50f, 0.62f, 0.74f), new Color(0.76f, 0.73f, 0.64f), 0.85f, 0.74f, 0.74f);
            DynamicGI.UpdateEnvironment();

            QualitySettings.shadowDistance = 150f;
        }

        // ---------------------------------------------------------------- 主构建

        public static void BuildQinlingScene()
        {
            LoadFonts();
            EnsureFolder(MatDir);
            EnsureFolder(SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupQinlingEnvironment();
            // 换成 Kevin MacLeod 的《Eastern Thought》（CC BY 4.0，可商用）。
            // 之前程序合成的低频埙/木梆被听成"可怕"，不再用合成器做这一章的音乐。
            AddSceneAmbient("amb_qinling_music", 0.38f);

            var sunGO = new GameObject("AutumnSun", typeof(Light));
            sunGO.transform.rotation = Quaternion.Euler(34f, 38f, 0f);
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Lin(new Color(1f, 0.94f, 0.84f));
            sun.intensity = 1.70f;
            sun.shadows = LightShadows.Soft;
            // 阴影压到 0.60 时，背光的北坡和封土阴面全糊成一块深色，
            // 山和土的形体都读不出来。压到 0.42，让天光把暗部托起来。
            sun.shadowStrength = 0.80f;
            sun.shadowBias = 0.045f;
            RenderSettings.sun = sun;

            var fillGO = new GameObject("SkyFill", typeof(Light));
            fillGO.transform.rotation = Quaternion.Euler(26f, 218f, 0f);
            var fill = fillGO.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Lin(new Color(0.68f, 0.76f, 0.86f));
            // 太阳在西南，骊山北坡全在背光里。真实照片里背光坡靠天光补亮，
            // 这里把北边的冷补光提起来，山才不是一块黑布。
            fill.intensity = 0.70f;
            fill.shadows = LightShadows.None;

            var root = Empty("Qinling", null, Vector3.zero).transform;

            // ------------------------------------------------------------ 地形
            // 800 m 见方：原来 320 m 的地块装不下骊山，山只能靠远处的切纸板撑场面。
            // 500 段 ≈ 1.6 m 一格：近处走路够平，20 m 一级的山脊噪声也咬得住。
            // 单独一份地形材质：默认的 _DetailScale 0.1 在这么大片地上等于没纹理，
            // 一眼看过去就是一块抹平的沙地。抬到 0.34，脚下才有土和草的颗粒。
            var qnTerrainMat = TerrainMat("QinlingTerrain");
            qnTerrainMat.SetFloat("_DetailScale", 0.34f);
            qnTerrainMat.SetFloat("_DetailStrength", 0.52f);
            qnTerrainMat.SetFloat("_BumpScale", 0.34f);
            qnTerrainMat.SetFloat("_MacroStrength", 0.30f);
            EditorUtility.SetDirty(qnTerrainMat);
            MeshObject("Terrain", root, Vector3.zero,
                MeshFactory.Terrain(800f, 500, QinlingHeight, QinlingColor), qnTerrainMat, true, false);

            // ------------------------------------------------------------ 材质
            var loessMat = MatAs("QnLoess", new Color(0.50f, 0.43f, 0.30f), "earth", 0f, 0.12f);
            var earthMat = MatAs("QnEarth", new Color(0.40f, 0.34f, 0.24f), "earth", 0f, 0.10f);
            var rammedMat = MatAs("QnRammed", new Color(0.47f, 0.45f, 0.38f), "earth", 0f, 0.14f);
            var rammedDarkMat = MatAs("QnRammedDark", new Color(0.38f, 0.36f, 0.30f), "earth", 0f, 0.10f);
            var rammedPaleMat = MatAs("QnRammedPale", new Color(0.53f, 0.50f, 0.43f), "earth", 0f, 0.16f);
            // 夯土纹理的横纹太密会读成木纹，压掉细节，只留一点颗粒。
            // bump 0.30 会在斜光下点出一层白色高光斑，远看像水磨石，压到 0.10。
            Calm("QnRammed", 0.10f, 0.34f, 0.10f);
            Calm("QnRammedDark", 0.10f, 0.34f, 0.10f);
            Calm("QnRammedPale", 0.10f, 0.34f, 0.10f);
            var timberMat = MatAs("QnTimber", new Color(0.33f, 0.26f, 0.18f), "wood", 0f, 0.16f);
            var timberPaleMat = MatAs("QnTimberPale", new Color(0.43f, 0.34f, 0.23f), "wood", 0f, 0.20f);

            // 一号坑的过洞只有 3.2 m 高、3.5 m 宽，太阳在 34° 上，整个过洞底都在阴影里。
            // 这几个是坑内主要构件，环境光提 1.35：暗部才不会掉成纯黑，
            // 隔着坑沿往下看也才分得清隔墙、椽子和陶俑。
            foreach (var m in new[] { rammedMat, rammedDarkMat, rammedPaleMat, earthMat, timberMat, timberPaleMat })
            {
                m.SetFloat("_AmbientBoost", 1.35f);
                EditorUtility.SetDirty(m);
            }
            var potteryMat = MatAs("QnPottery", new Color(0.47f, 0.39f, 0.30f), "plaster", 0f, 0.16f);
            var potteryDarkMat = MatAs("QnPotteryDark", new Color(0.36f, 0.30f, 0.23f), "plaster", 0f, 0.12f);
            var potteryRedMat = MatAs("QnPotteryRed", new Color(0.42f, 0.30f, 0.22f), "plaster", 0f, 0.14f);
            var bronzeMat = MatAs("QnBronze", new Color(0.56f, 0.47f, 0.29f), "metal", 0.42f, 0.40f);
            var bronzeDarkMat = MatAs("QnBronzeDark", new Color(0.34f, 0.33f, 0.24f), "metal", 0.30f, 0.30f);
            var patinaMat = MatAs("QnPatina", new Color(0.44f, 0.47f, 0.37f), "metal", 0.08f, 0.28f);
            var stoneMat = MatAs("QnStone", new Color(0.46f, 0.45f, 0.41f), "stone", 0.02f, 0.22f);
            // 石纹细节过强会变水磨石，压到一半
            Calm("QnStone", 0.34f, 0.26f, 0.50f);
            Calm("QnStoneDark", 0.34f, 0.26f, 0.50f);
            var stoneDarkMat = MatAs("QnStoneDark", new Color(0.35f, 0.34f, 0.31f), "stone", 0.02f, 0.18f);
            var charcoalMat = MatAs("QnCharcoal", new Color(0.17f, 0.15f, 0.13f), "stone", 0f, 0.08f);
            var grassMat = MatAs("QnGrass", new Color(0.43f, 0.44f, 0.26f), "foliage", 0f, 0.24f);
            var dryGrassMat = MatAs("QnDryGrass", new Color(0.55f, 0.50f, 0.30f), "foliage", 0f, 0.20f);
            // 太阳在西南，站在神道上往封土看，树全是背光的。原来的固有色到了背光面
            // 就是一排黑钉子，提亮一档才留得住"树林"的绿。
            var pineMat = MatAs("QnPine", new Color(0.42f, 0.53f, 0.35f), "foliage", 0f, 0.26f);
            var cypressMat = MatAs("QnCypress", new Color(0.36f, 0.47f, 0.30f), "foliage", 0f, 0.24f);
            // 封土表面：今天是一座被草木盖住的土山，不是裸露的夯土台
            // 名字里得带 grass，DetailKindOf 才认得出是草。原来叫 QnTurf，
            // 识别不出来就落回石纹，30 m 见方的封土坡面看起来像一块水磨石。
            var turfMat = MatAs("QnTurfGrass", new Color(0.30f, 0.35f, 0.19f), "grass", 0f, 0.22f);
            // 深秋的落叶乔木：塬上和陵园外缘的暖黄、赭红，和深绿的松柏拉开层次
            var autumnMat = MatAs("QnAutumn", new Color(0.54f, 0.41f, 0.19f), "foliage", 0f, 0.20f);
            var russetMat = MatAs("QnRusset", new Color(0.40f, 0.27f, 0.16f), "foliage", 0f, 0.18f);
            var shrubMat = MatAs("QnShrub", new Color(0.30f, 0.37f, 0.24f), "foliage", 0f, 0.20f);
            // 逆光的树冠：太阳在西南，站在神道上往南看，树全是背光面，只吃环境光。
            // 环境光增益拉高，再加一点绿叶透光的暖绿边光，才不是一排黑钉子。
            // 树冠是几米见方的大面，0.26 的细节强度铺上去就是一块迷彩色。
            // 压到 0.12，只留方向性的颗粒，绿色才不至于糊成迷彩布。
            Calm("QnPine", 0.12f, 0.22f, 0.18f);
            Calm("QnCypress", 0.12f, 0.22f, 0.18f);
            // 柏/松是全场景最亮、最饱和的一块：树冠朝上、边缘光 0.34，
            // 满太阳下一整排从画面里"跳"出来，读成塑料冰激凌。
            // 压暗一档、收掉边缘光，树才退到环境里，封土和夯土台基才是主角。
            Tone("QnPine", new Color(0.31f, 0.40f, 0.25f), 0.13f);
            Tone("QnCypress", new Color(0.28f, 0.36f, 0.23f), 0.11f);
            foreach (var n in new[] { "QnPine", "QnCypress", "QnShrub" })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + n + ".mat");
                if (mat == null) continue;
                mat.SetFloat("_AmbientBoost", 1.55f);
                mat.SetFloat("_RimStrength", 0.42f);
                mat.SetFloat("_RimPower", 2.6f);
                mat.SetColor("_RimColor", Lin(new Color(0.55f, 0.72f, 0.36f)));
                EditorUtility.SetDirty(mat);
            }

            // 统一走一遍表面质感：原来土、草、树皮的高光都在 0.10–0.26 之间，
            // 斜阳一压，整片场景读成一层抛光的水磨石。这里把高光压到 0.02–0.04，
            // 同时把三平面细节的密度提上去——土要有颗粒，树冠要有针叶的碎纹。
            SurfaceDetailPass();
            // ------------------------------------------------------------ 远山
            // 原来的远山是一圈竖着的切纸板：法线全朝相机，光打上去一片死平，
            // 侧面还露着纸边。现在山体直接长在地形里（QinlingHeight 里的 QnMassif），
            // 有坡、有明暗、有林线，雾也能按距离自然地把山推远。

            // ------------------------------------------------------------ 阙门与神道
            QinGate(root, new Vector3(0f, QinlingHeight(0f, -70f), -70f),
                rammedMat, rammedDarkMat, rammedPaleMat, timberMat, timberPaleMat, stoneMat);

            // 神道分两段：阙门前（-102 → -74，入口走到门前）与阙门内（-66 → -44）。
            // 原来只铺了门内一段，第一眼看见的秦陵是一块空荡荡的黄地，
            // 大门像是悬浮在荒野里。入口段铺上夯土路面、压出车辙，
            // 两侧列柏，视线就有了一条从脚下一直走到门里的纵深。
            void RoadSegment(float z0, float z1, string tag)
            {
                for (float z = z0; z < z1; z += 3f)
                {
                    float y = QinlingHeight(0f, z);
                    Box("SpiritRoad_" + tag, root, new Vector3(0f, y + 0.06f, z + 1.5f),
                        new Vector3(10.4f, 0.14f, 3.05f), loessMat, false);
                }
                for (float z = z0; z < z1; z += 6f)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float x = s * 5.6f;
                        float y = QinlingHeight(x, z);
                        Box("RoadCurb", root, new Vector3(x, y + 0.16f, z), new Vector3(0.7f, 0.30f, 6.05f),
                            rammedDarkMat, true);
                        if (Mathf.Abs(z % 18f) < 3.1f)
                            Box("RoadPost", root, new Vector3(s * 6.5f, y + 0.62f, z), new Vector3(0.22f, 1.25f, 0.22f),
                                timberMat, false);
                    }
                }
            }
            RoadSegment(-102f, -74f, "S");
            RoadSegment(-66f, -44f, "N");

            // 入口段柏列：退到路肩外 11.5 m。贴着路边种（7.6 m）时，
            // 第一人称镜头里两排柏树直接占满左右半幅，阙门只剩一条缝，
            // 这不是"夹出纵深"，是把画面夹死了。
            for (int i = 0; i < 9; i++)
            {
                float z = -93f + i * 2.8f;
                for (int s = -1; s <= 1; s += 2)
                {
                    float x = s * (11.5f + ((i + (s > 0 ? 1 : 0)) % 2) * 0.7f);
                    Conifer(root, new Vector3(x, QinlingHeight(x, z), z), 610 + i * 5 + s,
                        (i % 2 == 0) ? cypressMat : pineMat, timberMat, 0.62f + (i % 3) * 0.07f);
                }
            }

            // 内城甬道：由南门直抵封土基部
            for (float z = QnInnerZ0 + 3f; z < QnMoundZ - 31f; z += 2.4f)
            {
                float y = QinlingHeight(0f, z);
                Box("InnerAvenue", root, new Vector3(0f, y + 0.07f, z + 1.2f),
                    new Vector3(9.6f, 0.16f, 2.45f), rammedPaleMat, false);
            }

            // 神道两侧柏列
            for (int i = 0; i < 7; i++)
            {
                float z = -66f + i * 3.6f;
                for (int s = -1; s <= 1; s += 2)
                {
                    float x = s * (8.0f + (i % 2) * 0.7f);
                    Conifer(root, new Vector3(x, QinlingHeight(x, z), z), 120 + i * 3 + s,
                        (i % 2 == 0) ? cypressMat : pineMat, timberMat, 0.86f);
                }
            }

            // ------------------------------------------------------------ 外城垣
            float wallH = 4.4f, wallT = 7.6f;
            QnWall(root, "OuterWall_S_W", new Vector3(-(QnOuterX + 5f) * 0.5f - 2f, 0f, QnOuterZ0),
                QnOuterX - 5f, wallT, wallH, 0f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            QnWall(root, "OuterWall_S_E", new Vector3((QnOuterX + 5f) * 0.5f + 2f, 0f, QnOuterZ0),
                QnOuterX - 5f, wallT, wallH, 0f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            QnWall(root, "OuterWall_W", new Vector3(-QnOuterX, 0f, (QnOuterZ0 + QnOuterZ1) * 0.5f),
                QnOuterZ1 - QnOuterZ0, wallT, wallH, 90f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            // 外城东墙留一道门：一号坑在陵园东边，去陪葬坑的路要穿出去，
            // 原来四面封死，走到墙根就没路了。门洞开在 z = 0…9。
            QnWall(root, "OuterWall_E_S", new Vector3(QnOuterX, 0f, -21f),
                42f, wallT, wallH, 90f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            QnWall(root, "OuterWall_E_N", new Vector3(QnOuterX, 0f, 51.5f),
                85f, wallT, wallH, 90f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            for (int s2 = -1; s2 <= 1; s2 += 2)
                Box("OuterEastGatePier", root,
                    new Vector3(QnOuterX, QinlingHeight(QnOuterX, 4.5f) + wallH * 0.5f, 4.5f + s2 * 5.6f),
                    new Vector3(wallT + 2.2f, wallH * 1.05f, 2.2f), rammedDarkMat, true);

            for (int s = -1; s <= 1; s += 2)
                Box("OuterGateTower", root,
                    new Vector3(s * 12.6f, QinlingHeight(s * 12.6f, QnOuterZ0) + wallH * 0.62f, QnOuterZ0),
                    new Vector3(11.4f, wallH * 1.24f, wallT + 2.4f), rammedMat, true);

            // 城墙剖面（外城南垣西段的断口）
            QnWallSection(root, new Vector3(-34.5f, QinlingHeight(-34.5f, QnOuterZ0), QnOuterZ0 - wallT * 0.5f - 0.25f),
                wallT, wallH, rammedMat, rammedDarkMat, rammedPaleMat, stoneMat);

            // ------------------------------------------------------------ 内城垣
            float inH = 5.6f, inT = 8.4f;
            QnWall(root, "InnerWall_S_W", new Vector3(-(QnInnerX + 6f) * 0.5f, 0f, QnInnerZ0),
                QnInnerX - 6f, inT, inH, 0f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            QnWall(root, "InnerWall_S_E", new Vector3((QnInnerX + 6f) * 0.5f, 0f, QnInnerZ0),
                QnInnerX - 6f, inT, inH, 0f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            QnWall(root, "InnerWall_W", new Vector3(-QnInnerX, 0f, (QnInnerZ0 + QnInnerZ1) * 0.5f),
                QnInnerZ1 - QnInnerZ0, inT, inH, 90f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            QnWall(root, "InnerWall_E_S", new Vector3(QnInnerX, 0f, -11f),
                22f, inT, inH, 90f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            QnWall(root, "InnerWall_E_N", new Vector3(QnInnerX, 0f, 42.5f),
                67f, inT, inH, 90f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);
            for (int s2 = -1; s2 <= 1; s2 += 2)
                Box("InnerEastGatePier", root,
                    new Vector3(QnInnerX, QinlingHeight(QnInnerX, 4.5f) + inH * 0.5f, 4.5f + s2 * 5.6f),
                    new Vector3(inT + 2.2f, inH * 1.05f, 2.2f), rammedDarkMat, true);
            QnWall(root, "InnerWall_N", new Vector3(0f, 0f, QnInnerZ1),
                QnInnerX * 2f, inT, inH, 0f, rammedMat, rammedDarkMat, rammedPaleMat, grassMat);

            // 内城门阙：原来是一整块 12.8 m 见方的素盒子，站在神道上看就是两个纸箱。
            // 阙是上下两层、逐层收分的夯土台，顶上覆瓦——收两道、加一圈檐口就认得出是阙了。
            for (int s = -1; s <= 1; s += 2)
            {
                float gy = QinlingHeight(s * 13.4f, QnInnerZ0);
                float tx = s * 13.4f, tz = QnInnerZ0;
                float h0 = inH * 1.30f;
                Box("InnerGateTower", root, new Vector3(tx, gy + h0 * 0.5f, tz),
                    new Vector3(9.6f, h0, inT + 2.0f), rammedDarkMat, true);
                Box("InnerGateTier", root, new Vector3(tx, gy + h0 + inH * 0.26f, tz),
                    new Vector3(7.6f, inH * 0.52f, inT + 1.2f), rammedMat, true);
                Box("InnerGateEave", root, new Vector3(tx, gy + h0 + 0.10f, tz),
                    new Vector3(10.4f, 0.26f, inT + 2.8f), stoneDarkMat, false);
                // 瓦顶：四坡收成一个很小的脊，别做成尖塔
                Box("InnerGateRoof", root, new Vector3(tx, gy + h0 + inH * 0.56f, tz),
                    new Vector3(8.8f, 0.34f, inT + 2.4f), charcoalMat, false);
                Box("InnerGateRidge", root, new Vector3(tx, gy + h0 + inH * 0.66f, tz),
                    new Vector3(4.2f, 0.42f, inT + 0.9f), charcoalMat, false);
                // 夯土台基与散水
                Box("InnerGatePlinth", root, new Vector3(tx, gy + 0.24f, tz),
                    new Vector3(11.4f, 0.48f, inT + 3.6f), rammedPaleMat, true);
            }

            // ------------------------------------------------------------ 兵马俑坑
            // 陶土色比原来那套"陶器"材质更闷、更灰：秦俑是模制成型后入窑烧的灰陶，
            // 不是土黄釉陶。vertex color 里带一点每尊的色差。
            var terracotta = MatAs("QnTerracotta", new Color(0.47f, 0.42f, 0.37f), "earth", 0f, 0.10f);
            terracotta.SetFloat("_VertexColorInfluence", 1f);
            terracotta.SetFloat("_DetailStrength", 0.30f);
            terracotta.SetFloat("_BumpScale", 0.14f);
            terracotta.SetFloat("_AmbientBoost", 1.45f);
            EditorUtility.SetDirty(terracotta);

            // 二号色调：同一批陶土烧出来也有偏灰偏黄的差别，混着用军阵才不像复制粘贴
            var terracottaWarm = MatAs("QnTerracottaWarm", new Color(0.43f, 0.36f, 0.30f), "earth", 0f, 0.08f);
            terracottaWarm.SetFloat("_VertexColorInfluence", 1f);
            terracottaWarm.SetFloat("_DetailStrength", 0.34f);
            terracottaWarm.SetFloat("_BumpScale", 0.18f);
            terracottaWarm.SetFloat("_AmbientBoost", 1.45f);
            EditorUtility.SetDirty(terracottaWarm);

            var pitBrick = MatAs("QnPitBrick", new Color(0.44f, 0.43f, 0.41f), "stone", 0f, 0.14f);
            pitBrick.SetFloat("_DetailScale", 1.8f);
            pitBrick.SetFloat("_DetailStrength", 0.44f);
            pitBrick.SetFloat("_AmbientBoost", 1.7f);
            EditorUtility.SetDirty(pitBrick);

            QinPit(root, new Vector3(QnPitX, 0f, QnPitZ),
                rammedMat, rammedDarkMat, rammedPaleMat, earthMat,
                pitBrick, timberMat, timberPaleMat, terracotta, terracottaWarm);


            // 铜车马坑
            QinBronzePit(root, new Vector3(QnBronzeX, 0f, QnBronzeZ), 16f, 12f, 1.1f,
                rammedMat, rammedDarkMat, earthMat, timberMat, bronzeMat, bronzeDarkMat, patinaMat);

            // ------------------------------------------------------------ 封土
            QinTumulus(root, new Vector3(0f, 0f, QnMoundZ), rammedMat, rammedDarkMat, rammedPaleMat,
                turfMat, dryGrassMat, stoneMat, pineMat, cypressMat, timberMat, grassMat, shrubMat);

            // 陵园内的建筑基址：只做夯土台基与柱础，不臆造上部结构
            QinPlatform(root, new Vector3(-36f, 0f, 14f), 9f, 7f, rammedMat, rammedDarkMat, stoneMat);
            QinPlatform(root, new Vector3(36f, 0f, 14f), 9f, 7f, rammedMat, rammedDarkMat, stoneMat);

            // ------------------------------------------------------------ 修陵人工棚遗址
            QinWorkShed(root, new Vector3(-58f, 0f, -32f), earthMat, rammedDarkMat, timberMat,
                timberPaleMat, potteryMat, potteryDarkMat, charcoalMat, stoneMat);

            // ------------------------------------------------------------ 秦权秦量
            QinWeights(root, new Vector3(18f, 0f, -33f), stoneMat, stoneDarkMat,
                bronzeMat, bronzeDarkMat, patinaMat);

            // ------------------------------------------------------------ 植被
            QnFlora(root, grassMat, dryGrassMat, pineMat, cypressMat, earthMat,
                autumnMat, russetMat, shrubMat, timberMat);

            // ------------------------------------------------------------ 线索点
            // ------------------------------------------------------------ 俑道
            // 一号坑在陵园东边，坑口是一整块下挖五米的方坑，西壁是封死的封土，
            // 人能下去的门道全在东端。原来东侧土路到 x≈83 就断了，玩家走到坑西墙前
            // 无路可走，得自己贴着坑沿绕——"再拐一道"的那种走法就是这么来的。
            //
            // 现在把路一次修完：内城甬道尽头右拐 → 贴着坑西沿向北 → 沿坑北沿向东 →
            // 到底左拐南下 → 正对东端坡道口。俑道两侧铺下载来的兵马俑方阵，
            // 立牌就插在右拐进来的路口，站在甬道上就能看见。
            void DirtPath(params Vector3[] pts)
            {
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    var a = pts[i];
                    var b = pts[i + 1];
                    float len = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                    int steps = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
                    for (int k = 0; k < steps; k++)
                    {
                        float t = (k + 0.5f) / steps;
                        float x = Mathf.Lerp(a.x, b.x, t);
                        float z = Mathf.Lerp(a.z, b.z, t);
                        float y = QinlingHeight(x, z);
                        bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
                        var size = alongX
                            ? new Vector3(len / steps + 0.35f, 0.14f, 4.6f)
                            : new Vector3(4.6f, 0.14f, len / steps + 0.35f);
                        Box("PitWalk", root, new Vector3(x, y + 0.06f, z), size, loessMat, false);
                    }
                }
            }

            const float qnWalkX = 80f;      // 坑西沿外的一条南北向路面
            const float qnWalkZ = 27f;      // 坑北沿外的一条东西向主路
            const float qnGateX = 166f;     // 俑道口（左拐南下正对坡道）
            DirtPath(new Vector3(6f, 0f, QnPitZ), new Vector3(qnWalkX, 0f, QnPitZ),
                     new Vector3(qnWalkX, 0f, qnWalkZ), new Vector3(qnGateX, 0f, qnWalkZ),
                     new Vector3(qnGateX, 0f, QnPitZ + 15f));

            // 坑北沿主路的两排树：把"往前走"的方向和林地对上，不让俑道孤零零地铺在黄地上
            for (float x = 92f; x < qnGateX - 6f; x += 9f)
                for (int s = -1; s <= 1; s += 2)
                {
                    float z = qnWalkZ + s * 5.4f;
                    Conifer(root, new Vector3(x, QinlingHeight(x, z), z), 410 + (int)x + s,
                        (s > 0) ? cypressMat : pineMat, timberMat, 0.82f);
                }

            // 俑道：下载的兵马俑方阵（fbx）反复贴齐，贴着坑北沿站成一列仪仗，
            // 拐进俑道的一侧再排两尊，走进去的这几步就走在俑阵中间。
            // 单块底座 2.93 m 见方，scale 1.5 后 4.4 m。摆放位置一律避开坑口矩形
            // （x < 162.8 且 z < 20.9 的地方已经是五米深的坑），否则俑会悬在崖上。
            {
                var squad = TerracottaRow();
                if (squad != null)
                {
                    const float sqScale = 1.5f;
                    void Squad(float x, float z, float yaw)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(squad, root);
                        go.name = "WarriorSquad";
                        go.transform.localScale = Vector3.one * sqScale;
                        go.transform.localPosition = new Vector3(x, QinlingHeight(x, z) - 0.02f, z);
                        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    }

                    for (float x = 104f; x <= 160f; x += 8.2f)
                        Squad(x, qnWalkZ + 5.8f, 180f);
                    for (int i = 0; i < 3; i++)
                        Squad(qnGateX + 6.4f, QnPitZ + 15.4f - i * 4.2f, -90f);
                }
            }

            // 坡道口的木栅：俑道到此为止，往下就是修陵人运俑进来的斜坡门道
            foreach (float px in new[] { qnGateX + 3.8f, qnGateX - 2.0f })
            {
                float py = QinlingHeight(px, QnPitZ + 12f);
                Box("PitGatePost", root, new Vector3(px, py + 0.75f, QnPitZ + 12f),
                    new Vector3(0.30f, 1.5f, 0.30f), timberMat, true);
            }

            // ------------------------------------------------------------ 线索点
            // 三处，按走到的顺序编号：① 俑道口 → ② 封土南 → ③ 工棚；走完到 终 合卷回圆坛。
            // 每处按 E 打开的是 HeritageSpots 里的图文与语音（有影像、没有三维展台）。
            var ptPit = CreateInfoPoint(root, "Pit", new Vector3(qnGateX, QinlingHeight(qnGateX, qnWalkZ + 1.4f), qnWalkZ + 1.4f),
                "兵马俑展示入口", "第一站 · 军阵与工程",
                "从这道口子往里走，就是修陵时运陶俑下坑的斜坡门道。\n\n" +
                "一号坑东西长 230 米、南北宽 62 米、深约 5 米，坑里 10 道夯土隔墙隔出 11 条过洞；" +
                "坑底铺砖，隔墙顶上架地栿、立柱、枋木、棚木，再铺苇席和胶泥，把整个坑封成地下空间。\n\n" +
                "坑里近六千件陶俑面朝东列阵。坑顶后来塌了、烧了，土灌了进来——" +
                "今天看到的是塌下来以后的样子，不是当年的样子。",
                "pit", new Color(0.68f, 0.56f, 0.42f), SourceTier.Confirmed, "军阵编队方式仍有多种解释",
                heritageId: "spot_qn_pit", number: "①");

            var ptMound = CreateInfoPoint(root, "Mound", new Vector3(0f, 0f, QnMoundZ - 36f),
                "封土", "第二站 · 尺度与取舍",
                "封土是陵园的中心，夯土一层层堆成覆斗形——上小下大、四面收坡。\n\n" +
                "外轮廓一级一级往里收，那是每夯一层就缩小一圈留下的台阶，不是自然长成的山。\n\n" +
                "地宫里的事，文献里有「以水银为百川江河大海」这样的说法，但到今天没有发掘。" +
                "凡是复原地宫内部的，都是推测；这个场景就停在封土外面。",
                "mound", new Color(0.72f, 0.66f, 0.48f), SourceTier.Confirmed, "封土原高有争议",
                heritageId: "spot_qn_mound", number: "②");

            var ptHall = CreateInfoPoint(root, "Workshop", new Vector3(-58f, 0.3f, -27.0f),
                "修陵人工棚遗址", "第三站 · 修陵人",
                "陵园周边找到过很多跟修陵有关的遗迹：半地穴的工棚、灰坑、陶窑，还有墓葬。\n\n" +
                "工棚就是在地上挖个浅坑，立柱、架顶，给季节性来的劳作者临时住。" +
                "柱洞、烧土面、炭屑、陶片一起出——住人、烧火、做陶，都在这一小片地方。\n\n" +
                "陶器上有戳印的字，可能是做的人，也可能是管的人。史书里没有这些人的名字，" +
                "这些最不起眼的坑，记的就是他们。",
                "workshop", new Color(0.70f, 0.58f, 0.36f), SourceTier.Inferred, "工棚形制为示意复原，戳印文字仅作形式提示",
                heritageId: "spot_qn_hall", number: "③");

            // 合上这一卷：从工棚往南，走到底就是回序章圆坛的路
            var ptLeave = CreateInfoPoint(root, "Farewell", new Vector3(-20f, QinlingHeight(-20f, -50f), -50f),
                "合上《河山册》第二卷", "秦陵 · 话尽于此",
                "俑、封土、工棚，三样东西说的是同一件事：一个把人力、制度和尺度都推到极限的帝国。",
                null, new Color(0.88f, 0.78f, 0.54f), SourceTier.Confirmed, null,
                number: "终", endScene: "Archive", endSpawnId: "hub_center",
                endLine: "土与俑的一卷，到此合上。回圆坛去，还有一处等着。");

            var iQPit = ptPit.GetComponent<Interactable>();
            var iQMound = ptMound.GetComponent<Interactable>();
            var iQHall = ptHall.GetComponent<Interactable>();
            var iQLeave = ptLeave.GetComponent<Interactable>();
            if (iQPit != null) iQPit.leadTo = ptMound.transform;
            if (iQMound != null) iQMound.leadTo = ptHall.transform;
            if (iQHall != null) iQHall.leadTo = ptLeave.transform;

            // ------------------------------------------------ 玩家、视角、UI
            var player = CreatePlayer(new Vector3(0f, QinlingHeight(0f, -78f) + 0.05f, -78f), 0f);
            ApplyGrade(player, "qinling");

            // 来路边的界石：想回去就站到石前按 E，不必把这一卷走完
            CreateReturnStone(root, "Qinling", new Vector3(4.0f, QinlingHeight(4.0f, -76.6f), -76.6f),
                -109f, "秦始皇帝陵", "qinling", stoneMat, timberMat);

            var startSpawn = Empty("Spawn_from_hub", root, new Vector3(0f, QinlingHeight(0f, -78f) + 0.05f, -78f));
            startSpawn.AddComponent<SpawnPoint>().id = "from_hub";
            var cam = player.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;

            var aerial = CreateAerialCamera(new Vector3(0f, 96f, -96f), new Vector3(40f, 0f, 0f));
            var bird = player.AddComponent<BirdEyeView>();
            bird.playerCamera = cam;
            bird.aerialCamera = aerial;
            bird.controller = player.GetComponent<DesktopPlayerController>();
            bird.aerialPosition = new Vector3(0f, 96f, -96f);
            bird.aerialEuler = new Vector3(40f, 0f, 0f);

            CreateHUD("WASD 移动 · Shift 奔跑 · 「、」跳跃 · E 互动 · Tab 取册子 · V 整体视角 · Q 合卷回山 · Esc 释放鼠标");

            var systems = new GameObject("Systems");
            var tasks = systems.AddComponent<TaskManager>();
            tasks.chapterId = "qinling";
            tasks.chapterLabel = "第二章 · 帝与陵";
            tasks.chapterSubtitle = "秦始皇帝陵园";
            tasks.objectives = new[]
            {
                new Objective { id = "pit", label = "① 兵马俑展示入口：看清隔墙、棚木和军阵" },
                new Objective { id = "mound", label = "② 封土：看它怎么一层层收上去" },
                new Objective { id = "workshop", label = "③ 工棚遗址：看看修陵人的日子" },
            };
            tasks.introBody =
                "眼前是秦陵。从南边的阙门进神道，穿过内外两重夯土城垣，" +
                "再右拐沿俑道往东走到底——那里是一号坑的东端，立着「兵马俑展示入口」。\n\n" +
                "从坡道下坑，看隔墙怎么隔、棚木怎么架、俑怎么站；出坑往北是封土，" +
                "只能走到陵园墙根，墙里不再往里去。\n\n" +
                "西边的工棚遗址是这件大工程另一头：史书里没留下名字的那批人。" +
                "三处看完，按提示合卷回圆坛。按 V 能飞起来俯瞰。\n\n" +
                "这里不带你进地宫。凡是讲地宫里面的，都没有考古依据。";
            tasks.archiveTitle = "六合一统：制度与工程";
            tasks.archiveBody =
                "现场发现\n" +
                "· 陵园由内外两重夯土城垣围合，垣体分层夯筑，层间界面清晰，局部可见夯窝。\n" +
                "· 陪葬坑为竖穴土圹，坑内夯土隔墙分隔走廊，走廊内排列陶俑，坑口原覆棚木。\n" +
                "· 铜车马按真车马约二分之一比例铸造，部件以铸造、镶嵌、销接等多种工艺组合。\n" +
                "· 封土为夯土逐层堆筑的覆斗形，外轮廓呈台阶状收分。\n" +
                "· 陵园周边有半地穴工棚、灰坑与陶窑，陶器上见戳印文字。\n" +
                "· 秦权作覆钟形、秦量为长方形量器，二者多带诏文，是统一度量衡的标准器。\n\n" +
                "历史解释\n" +
                "秦始皇陵始建于秦王政元年，延续至秦二世时期。陵园布局、陪葬坑体系与标准器，" +
                "共同反映出秦代在大规模工程组织与制度统一方面的能力；" +
                "地宫情况因未发掘而无实证，只能存疑。\n\n" +
                "关联线索\n" +
                "与良渚的对照：两者都以大型土方工程表达秩序，但良渚靠水利与稻作组织，" +
                "秦靠郡县与标准化制度——这是比较性的观察，不构成直接发展关系。";
            tasks.returnScene = "Archive";

            CreateDust(root, new Vector3(0f, 4f, -20f), new Vector3(150f, 10f, 150f), 300, 9f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneDir + "/Qinling.unity");
            Debug.Log("[Huaxia] 秦陵场景已保存");
        }

        // ================================================================ 材质质感

        /// <summary>
        /// 把整个秦陵的土、草、树皮从"抛光面"拉回"颗粒面"：
        /// _Glossiness 压到 0.02–0.05 消掉斜阳下的白色高光斑，
        /// _DetailScale 提上去让三平面细节在几米内就能看见（土的颗粒、针叶的碎纹）。
        /// </summary>
        static void SurfaceDetailPass()
        {
            // 名称, 高光, 细节密度, 细节强度, 法线强度, 斑驳强度
            var table = new[]
            {
                // 土与夯土：一整面墙、一整片地，密度不到 1 就是一块抹平的沙
                new object[] { "QnLoess",       0.03f, 0.95f, 0.58f, 0.42f, 0.34f },
                new object[] { "QnEarth",       0.03f, 0.95f, 0.58f, 0.42f, 0.34f },
                new object[] { "QnRammed",      0.03f, 0.80f, 0.46f, 0.28f, 0.34f },
                new object[] { "QnRammedDark",  0.03f, 0.80f, 0.46f, 0.28f, 0.34f },
                new object[] { "QnRammedPale",  0.03f, 0.80f, 0.46f, 0.28f, 0.34f },
                new object[] { "QinlingTerrain",0.02f, 0.90f, 0.62f, 0.50f, 0.34f },
                // 草与树冠：细节密度按米算，低于 2 的针叶冠永远是光面锥体
                new object[] { "QnGrass",       0.02f, 2.60f, 0.52f, 0.60f, 0.26f },
                new object[] { "QnDryGrass",    0.02f, 2.60f, 0.52f, 0.60f, 0.26f },
                new object[] { "QnTurfGrass",   0.02f, 2.20f, 0.50f, 0.58f, 0.28f },
                new object[] { "QnMoundGrass",  0.02f, 2.20f, 0.50f, 0.58f, 0.28f },
                new object[] { "QnPine",        0.02f, 2.60f, 0.50f, 0.62f, 0.24f },
                new object[] { "QnCypress",     0.02f, 2.60f, 0.50f, 0.62f, 0.24f },
                new object[] { "QnShrub",       0.03f, 2.20f, 0.46f, 0.52f, 0.22f },
                new object[] { "QnAutumn",      0.03f, 2.20f, 0.46f, 0.52f, 0.22f },
                new object[] { "QnRusset",      0.03f, 2.20f, 0.46f, 0.52f, 0.22f },
                // 木、砖、石：构件清楚，只加一层细纹，不要变成水磨石
                new object[] { "QnTimber",      0.04f, 1.30f, 0.44f, 0.34f, 0.20f },
                new object[] { "QnTimberPale",  0.04f, 1.30f, 0.44f, 0.34f, 0.20f },
                new object[] { "QnPitBrick",    0.04f, 1.10f, 0.42f, 0.30f, 0.22f },
                new object[] { "QnStone",       0.04f, 1.10f, 0.44f, 0.36f, 0.20f },
                new object[] { "QnStoneDark",   0.04f, 1.10f, 0.44f, 0.36f, 0.20f },
            };

            foreach (var row in table)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + row[0] + ".mat");
                if (m == null) continue;
                m.SetFloat("_Glossiness", (float)row[1]);
                m.SetFloat("_DetailScale", (float)row[2]);
                m.SetFloat("_DetailStrength", (float)row[3]);
                m.SetFloat("_BumpScale", (float)row[4]);
                m.SetFloat("_MacroStrength", (float)row[5]);
                EditorUtility.SetDirty(m);
            }
        }

        // ================================================================ 构件

        /// <summary>四棱台网格：底面在原点，用于阙身这类带收分的夯土台，避免逐层叠盒子。</summary>
        static Mesh QnFrustum(float bw, float bd, float tw, float td, float h)
        {
            var m = new Mesh();
            m.name = "QnFrustum";
            float hw = bw * 0.5f, hd = bd * 0.5f, tw2 = tw * 0.5f, td2 = td * 0.5f;
            var v = new List<Vector3>
            {
                new Vector3(-hw, 0f, -hd), new Vector3(hw, 0f, -hd),
                new Vector3(hw, 0f, hd),   new Vector3(-hw, 0f, hd),
                new Vector3(-tw2, h, -td2), new Vector3(tw2, h, -td2),
                new Vector3(tw2, h, td2),   new Vector3(-tw2, h, td2)
            };
            var uv = new List<Vector2>(8);
            for (int i = 0; i < 8; i++) uv.Add(new Vector2((v[i].x + v[i].z) * 0.30f, v[i].y * 0.30f));
            var t = new List<int>
            {
                0, 4, 5, 0, 5, 1,   // -Z
                1, 5, 6, 1, 6, 2,   // +X
                2, 6, 7, 2, 7, 3,   // +Z
                3, 7, 4, 3, 4, 0,   // -X
                4, 6, 5, 4, 7, 6    // 顶
            };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            // 硬边法线：否则前立面和侧立面被平均成 45°，整面墙是一条从亮到黑的渐变
            return MeshFactory.Faceted(m);
        }

        /// <summary>庑殿顶：四坡汇于一道短正脊，四角起翘，带板厚（单面会从下往上看不见）。</summary>
        static Mesh QnHipRoof(float halfW, float halfD, float h, float ridgeHalf, float lift, float thick)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();

            int Add(Vector3 p)
            {
                v.Add(p);
                uv.Add(new Vector2(p.x * 0.35f, p.z * 0.35f));
                return v.Count - 1;
            }

            void Tri(int a, int b, int c) { t.Add(a); t.Add(b); t.Add(c); }

            Vector3 A = new Vector3(-halfW, lift, -halfD);
            Vector3 B = new Vector3(halfW, lift, -halfD);
            Vector3 C = new Vector3(halfW, lift, halfD);
            Vector3 D = new Vector3(-halfW, lift, halfD);

            // 上表面
            int a1 = Add(A), b1 = Add(B), c1 = Add(C), d1 = Add(D);
            int r1 = Add(new Vector3(0f, h, -ridgeHalf));
            int r2 = Add(new Vector3(0f, h, ridgeHalf));
            Tri(a1, r1, b1);
            Tri(c1, r2, d1);
            Tri(b1, r1, r2); Tri(b1, r2, c1);
            Tri(d1, r2, r1); Tri(d1, r1, a1);

            // 下表面（同形下移板厚，反向绕序）
            Vector3 down = new Vector3(0f, -thick, 0f);
            int a2 = Add(A + down), b2 = Add(B + down), c2 = Add(C + down), d2 = Add(D + down);
            int r3 = Add(new Vector3(0f, h - thick, -ridgeHalf));
            int r4 = Add(new Vector3(0f, h - thick, ridgeHalf));
            Tri(b2, r3, a2);
            Tri(d2, r4, c2);
            Tri(c2, r4, b2); Tri(r3, r4, b2);
            Tri(a2, r4, d2); Tri(a2, r3, r4);

            // 檐口侧板：把上下两片连起来，读成有厚度的瓦顶
            Tri(a1, b1, b2); Tri(a1, b2, a2);
            Tri(b1, c1, c2); Tri(b1, c2, b2);
            Tri(c1, d1, d2); Tri(c1, d2, c2);
            Tri(d1, a1, a2); Tri(d1, a2, d2);

            var m = new Mesh();
            m.name = "QnHipRoof";
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return MeshFactory.Faceted(m);   // 坡面各自平整，屋脊才有折线
        }

        /// <summary>阙门：两座夯土阙 + 木构门道 + 楷书门额。坐标按世界绝对位置给出。</summary>
        static void QinGate(Transform parent, Vector3 pos, Material rammed, Material rammedDark,
                            Material rammedPale, Material timber, Material timberPale, Material stone)
        {
            var g = Empty("QinGate", parent, pos);
            float gy = pos.y;
            var tile = MatAs("QinEaveTile", new Color(0.31f, 0.31f, 0.30f), "tile", 0f, 0.16f);
            var passage = MatAs("QinPassage", new Color(0.14f, 0.13f, 0.12f), "stone", 0f, 0.10f);
            var doorWood = MatAs("QinDoorWood", new Color(0.40f, 0.30f, 0.19f), "wood", 0f, 0.14f);

            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * 11.4f;
                const float baseW = 6.4f;
                const float h = 7.92f;
                float topW = baseW - 1.92f;
                // 一体收分台体；夯层用细密横缝表现，不用分层几何（分块叠起来会读成木箱）
                MeshObject("QueBody", g.transform, new Vector3(x, gy + 0.28f, 0f),
                    QnFrustum(baseW, baseW * 0.94f, topW, topW * 0.94f, h), rammed, true);
                for (int k = 1; k <= 12; k++)
                {
                    float t = k / 13f;
                    float y = gy + 0.28f + h * t;
                    float w = Mathf.Lerp(baseW, topW, t);
                    Box("QueLayer_" + k, g.transform, new Vector3(x, y, 0f),
                        new Vector3(w + 0.035f, 0.030f, w * 0.94f + 0.035f), rammedDark, false);
                }
                Box("QueTop", g.transform, new Vector3(x, gy + h + 0.40f, 0f),
                    new Vector3(topW + 0.26f, 0.24f, topW * 0.94f + 0.26f), rammedPale, true);
                // 阙顶：庑殿四坡 + 正脊，出挑 0.9 m
                float rhw = topW * 0.5f + 0.66f;
                float rhd = topW * 0.47f + 0.62f;
                float ry = gy + h + 0.50f;
                MeshObject("QueRoof", g.transform, new Vector3(x, ry, 0f),
                    QnHipRoof(rhw, rhd, 1.42f, rhd * 0.16f, 0.22f, 0.26f), tile, true);
                Box("QueRidge", g.transform, new Vector3(x, ry + 1.56f, 0f),
                    new Vector3(0.30f, 0.22f, rhd * 0.36f), tile, false);
                Box("QueFoot", g.transform, new Vector3(x, gy + 0.14f, 0f),
                    new Vector3(baseW + 0.64f, 0.30f, baseW * 0.94f + 0.64f), stone, true);
            }

            // 阙与门道之间的夯土墙，把门读成"墙上开洞"而不是浮着两座土墩
            foreach (int s in new[] { -1, 1 })
            {
                Box("GateWall", g.transform, new Vector3(s * 6.35f, gy + 1.58f, 0f),
                    new Vector3(4.30f, 3.16f, 1.60f), rammed, true);
                Box("GateWallTop", g.transform, new Vector3(s * 6.35f, gy + 3.28f, 0f),
                    new Vector3(4.70f, 0.22f, 2.00f), tile, false);
            }

            // 门道：立柱 + 横楣 + 双扇门（门扇半开，门钉与铺首为子物体，父级必须是空物体）
            foreach (int s in new[] { -1, 1 })
            {
                Cyl("GatePost", g.transform, new Vector3(s * 4.4f, gy + 2.7f, 0f), 0.32f, 5.4f, timber);
                Box("GateFoot", g.transform, new Vector3(s * 4.4f, gy + 0.20f, 0f),
                    new Vector3(0.95f, 0.40f, 0.95f), stone, true);
            }
            Box("Lintel", g.transform, new Vector3(0f, gy + 5.30f, 0f), new Vector3(10.1f, 0.50f, 0.66f), timber, true);
            Box("Lintel2", g.transform, new Vector3(0f, gy + 4.80f, 0f), new Vector3(8.9f, 0.26f, 0.46f), timber, false);

            // 双扇门：走近自己开，走远自己合。门后不再放那块黑色挡板，
            // 否则门开了人也只看得到一片黑。
            var doorHinges = new List<Transform>();
            var doorClosed = new List<float>();
            var doorDir = new List<float>();
            const float leafW = 3.95f;
            for (int s = -1; s <= 1; s += 2)
            {
                // 门轴在门框边（x = ±3.95），门扇中心再往门洞中间偏半个门扇宽。
                // 之前门轴和门扇中心重合，门是绕自己中心转的——看着像从中间掰开。
                var hinge = Empty("DoorHinge", g.transform, new Vector3(s * leafW, gy + 2.32f, -0.52f));
                hinge.transform.localRotation = Quaternion.identity;
                doorHinges.Add(hinge.transform);
                doorClosed.Add(0f);
                // 往里开（+z，背离来人的方向）：左扇 -1、右扇 +1
                doorDir.Add(s);

                float leafIn = -s * leafW * 0.5f;
                Box("DoorLeaf", hinge.transform, new Vector3(leafIn, 0f, 0f),
                    new Vector3(leafW, 4.62f, 0.15f), doorWood, true);
                for (int r = 0; r < 5; r++)
                    for (int c = 0; c < 4; c++)
                        Box("DoorStud_" + r + "_" + c, hinge.transform,
                            new Vector3(leafIn - 1.42f + c * 0.95f, -1.62f + r * 0.80f, -0.11f),
                            new Vector3(0.13f, 0.13f, 0.09f), timber, false);
                Box("DoorBand", hinge.transform, new Vector3(leafIn, -2.02f, -0.11f),
                    new Vector3(leafW - 0.02f, 0.12f, 0.06f), timber, false);
                Box("DoorRing", hinge.transform, new Vector3(leafIn - s * 1.55f, -0.12f, -0.14f),
                    new Vector3(0.26f, 0.26f, 0.05f), stone, false);
            }

            var autoDoor = g.AddComponent<AutoDoor>();
            autoDoor.hinges = doorHinges.ToArray();
            autoDoor.closedYaw = doorClosed.ToArray();
            autoDoor.openDirection = doorDir.ToArray();
            // 落点在 8 m 外：触发圈设在 5.2 m，人会看见门「迎着自己」开，
            // 而不是一出生就已经敞着。
            autoDoor.triggerDistance = 5.2f;
            autoDoor.closeDistance = 7.5f;

            // 门额匾（楷书）
            Box("PlaqueFrame", g.transform, new Vector3(0f, gy + 6.22f, -0.40f),
                new Vector3(4.75f, 1.28f, 0.20f), timber, true);
            Box("PlaqueBoard", g.transform, new Vector3(0f, gy + 6.22f, -0.53f),
                new Vector3(4.30f, 0.90f, 0.13f), rammedPale, false);
            CreateTextPlate(g.transform, "丽 山 园", new Vector3(0f, gy + 6.22f, -0.62f), Vector3.zero,
                new Vector2(3.80f, 0.76f), 60, new Color(0.24f, 0.17f, 0.11f), title: true);

            // 门侧石碑
            Box("StelaBase", g.transform, new Vector3(17.4f, gy + 0.18f, -3.4f),
                new Vector3(2.2f, 0.36f, 1.3f), stone, true);
            Box("Stela", g.transform, new Vector3(17.4f, gy + 1.70f, -3.4f),
                new Vector3(1.6f, 2.7f, 0.32f), stone, true);
            CreateTextPlate(g.transform, "秦 始 皇 帝 陵 园\n全国重点文物保护单位\n世界文化遗产",
                new Vector3(17.4f, gy + 1.86f, -3.58f), Vector3.zero,
                new Vector2(1.34f, 2.00f), 32, new Color(0.24f, 0.21f, 0.17f), title: true);
        }

        /// <summary>夯土城垣：分层收分 + 版筑线 + 覆土顶。</summary>
        static void QnWall(Transform parent, string name, Vector3 center, float length, float thickness,
                           float height, float yaw, Material rammed, Material rammedDark,
                           Material rammedPale, Material grass)
        {
            var g = Empty(name, parent, center);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float gy = QinlingHeight(center.x, center.z);

            const int tiers = 4;
            for (int i = 0; i < tiers; i++)
            {
                float t = thickness * (1f - 0.030f * i);
                float y = gy + height * (i + 0.5f) / tiers;
                // 四层同一种夯土：深浅交替会读成木板的横向拼缝
                Box("Body_" + i, g.transform, new Vector3(0f, y, 0f),
                    new Vector3(length, height / tiers, t), rammed, true);
                // 版筑层线（两侧各一条）
                for (int s = -1; s <= 1; s += 2)
                    Box("Line_" + i, g.transform, new Vector3(0f, gy + height * i / tiers + 0.07f, s * t * 0.5f),
                        new Vector3(length + 0.04f, 0.055f, 0.06f), rammedPale, false);
            }
            // 墙顶覆土长草：今天看到的秦陵城垣就是这么一条长满草的土垄
            Box("Top", g.transform, new Vector3(0f, gy + height + 0.07f, 0f),
                new Vector3(length, 0.18f, thickness * 0.62f), grass, false);
            Box("TopDirt", g.transform, new Vector3(0f, gy + height - 0.10f, 0f),
                new Vector3(length, 0.18f, thickness * 0.94f), rammedDark, false);
            int seed = Mathf.Abs(name.GetHashCode()) % 997;
            for (float u = -length * 0.5f + 2.5f; u < length * 0.5f; u += 5.5f)
                GrassTuft(g.transform, new Vector3(u, gy + height + 0.10f, 0f), grass,
                    0.9f + Rnd(seed + Mathf.Abs((int)(u * 7f))) * 0.5f,
                    seed + Mathf.Abs((int)(u * 13f)));
        }

        /// <summary>城墙剖面：一段被切开的夯土城墙，露出夯层、版筑缝与夯窝。</summary>
        static void QnWallSection(Transform parent, Vector3 pos, float thickness, float height,
                                  Material rammed, Material rammedDark, Material rammedPale, Material stone)
        {
            var g = Empty("QinWallSection", parent, pos);
            const float w = 11.0f;
            const float d = 3.0f;                       // 前后进深：是一段墙，不是一块板
            float faceZ = d * 0.5f;
            var dimple = MatAs("QinDimple", new Color(0.36f, 0.30f, 0.21f), "earth", 0f, 0.06f);
            Calm("QinDimple", 0.30f, 0.20f, 0.10f);

            // 台体：一整块夯土，切面就是它的正反两面
            Box("Block", g.transform, new Vector3(0f, height * 0.5f, 0f),
                new Vector3(w, height, d), rammed, true);
            Box("TopDirt", g.transform, new Vector3(0f, height + 0.11f, 0f),
                new Vector3(w, 0.22f, d * 0.92f), rammedDark, false);

            for (int s = -1; s <= 1; s += 2)
            {
                float fz = s * faceZ;
                // 夯层：刻在切面上的细线，极浅，读作土层而不是搁架
                Box("CapLine", g.transform, new Vector3(0f, height - 0.22f, fz + s * 0.012f),
                    new Vector3(w, 0.06f, 0.03f), rammedPale, false);
                // 夯层交给纹理和风化的凹槽，不再刻十条等距黑线——那就是一副木挂板的横缝。
                for (int i = 1; i < 4; i++)
                {
                    float x = -w * 0.5f + w * (i / 4f + (Rnd(i * 3 + (s > 0 ? 9 : 0)) - 0.5f) * 0.06f);
                    float y0 = 0.55f + Rnd(i * 7 + (s > 0 ? 5 : 0)) * 0.5f;
                    float len = height * (0.28f + Rnd(i * 11 + 2) * 0.26f);
                    Box("Seam", g.transform, new Vector3(x, y0 + len * 0.5f, fz + s * 0.007f),
                        new Vector3(0.030f, len, 0.014f), dimple, false);
                }
                // 夯窝：浅浅一圈暗色凹痕，不做凸起，否则读成铆钉
                for (int i = 0; i < 18; i++)
                {
                    float x = (Rnd(i * 7 + 1 + (s > 0 ? 40 : 0)) - 0.5f) * (w - 1.0f);
                    float y = 0.40f + Rnd(i * 13 + 5 + (s > 0 ? 40 : 0)) * (height - 1.0f);
                    var dd = Cyl("Dimple", g.transform, new Vector3(x, y, fz + s * 0.004f), 0.062f, 0.004f, dimple);
                    dd.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
                // 石砌墙基 + 前沿散石
                Box("Footing", g.transform, new Vector3(0f, 0.16f, fz * 0.5f),
                    new Vector3(w + 0.5f, 0.34f, d + 0.5f), stone, true);
                for (int i = 0; i < 6; i++)
                {
                    var r = Box("Rubble", g.transform,
                        new Vector3(-w * 0.5f + 1.2f + i * 1.8f, 0.10f, fz + s * 0.55f),
                        new Vector3(0.58f, 0.20f, 0.40f), stone, false);
                    r.transform.localRotation = Quaternion.Euler(0f, (Rnd(i * 9 + (s > 0 ? 20 : 0)) - 0.5f) * 26f, 0f);
                }
            }

            // 楷书石碑：浅色碑面 + 深色阴刻字，暮色下也能读清
            float sx = -w * 0.5f - 2.2f;
            Box("StelaBase", g.transform, new Vector3(sx, 0.16f, -2.15f),
                new Vector3(1.94f, 0.32f, 1.12f), stone, true);
            Box("Stela", g.transform, new Vector3(sx, 1.52f, -2.15f),
                new Vector3(1.66f, 2.64f, 0.26f), stone, true);
            Box("StelaFace", g.transform, new Vector3(sx, 1.52f, -2.295f),
                new Vector3(1.42f, 2.38f, 0.03f), rammedPale, true);
            CreateTextPlate(g.transform, "夯 土 城 垣\n版 筑 廿 六\n层 层 有 痕",
                new Vector3(sx, 1.52f, -2.326f), Vector3.zero,
                new Vector2(1.26f, 2.18f), 44, Lin(new Color(0.30f, 0.25f, 0.20f)), title: true);
        }

        /// <summary>铜车马坑：土圹 + 两乘铜车马 + 残存棚木。</summary>
        static void QinBronzePit(Transform parent, Vector3 center, float length, float width, float depth,
                                 Material rammed, Material rammedDark, Material earth,
                                 Material timber, Material bronze, Material bronzeDark, Material patina)
        {
            var g = Empty("Pit_Bronze", parent, center);
            float gy = QinlingHeight(center.x, center.z);
            float fy = gy;
            float rim = gy + depth;

            Box("Floor", g.transform, new Vector3(0f, fy + 0.12f, 0f),
                new Vector3(length, 0.24f, width), earth, true);
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Cope_Z", g.transform, new Vector3(0f, rim - 0.08f, s * (width * 0.5f + 0.55f)),
                    new Vector3(length + 1.6f, 0.34f, 1.5f), rammed, true);
                Box("Cope_X", g.transform, new Vector3(s * (length * 0.5f + 0.55f), rim - 0.08f, 0f),
                    new Vector3(1.5f, 0.34f, width + 1.6f), rammedDark, true);
            }

            BronzeChariot(g.transform, new Vector3(-2.6f, fy + 0.24f, -1.4f), 90f, bronze, bronzeDark, patina);
            BronzeChariot(g.transform, new Vector3(2.2f, fy + 0.24f, 1.6f), 90f, bronze, bronzeDark, patina);

            // 棚木只剩西端三根（坑东半部已揭开）
            for (int i = 0; i < 3; i++)
            {
                float x = -length * 0.5f + 1.2f + i * 1.9f;
                bool fallen = (i == 1);
                Box("Beam", g.transform, new Vector3(x, fallen ? fy + 0.72f : rim + 0.16f, 0f),
                    new Vector3(0.18f, 0.18f, width + 0.8f), timber, false);
            }
        }

        /// <summary>铜车马：车舆、双轮、辕衡、伞盖与四马。</summary>
        static void BronzeChariot(Transform parent, Vector3 pos, float yaw,
                                  Material bronze, Material bronzeDark, Material patina)
        {
            var g = Empty("Chariot", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box("Box", g.transform, new Vector3(0f, 0.78f, 0f), new Vector3(1.55f, 0.56f, 1.05f), bronze, false);
            Box("Rail", g.transform, new Vector3(0f, 1.08f, 0f), new Vector3(1.66f, 0.10f, 1.16f), bronzeDark, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var w = Cyl("Wheel", g.transform, new Vector3(s * 0.86f, 0.56f, 0f), 0.56f, 0.07f, bronzeDark);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Box("Axle", g.transform, new Vector3(0f, 0.52f, 0f), new Vector3(2.05f, 0.09f, 0.09f), bronze, false);
            Box("Shaft", g.transform, new Vector3(0f, 0.66f, 1.75f), new Vector3(0.10f, 0.10f, 2.7f), bronze, false);
            Box("Yoke", g.transform, new Vector3(0f, 0.78f, 3.00f), new Vector3(3.60f, 0.09f, 0.09f), bronze, false);

            Cyl("Pole", g.transform, new Vector3(0f, 1.38f, 0f), 0.045f, 1.12f, bronze);

            // 伞盖：小一号的穹顶 + 一圈伞弓，铜色而非铜绿
            var dome = Ball("Canopy", g.transform, new Vector3(0f, 1.92f, 0f), 0.62f, bronzeDark);
            dome.transform.localScale = new Vector3(1.22f, 0.34f, 1.22f);
            Cyl("CanopyRim", g.transform, new Vector3(0f, 1.86f, 0f), 0.62f, 0.05f, bronze);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 0.25f;
                var rib = Box("CanopyRib", g.transform,
                    new Vector3(Mathf.Cos(a) * 0.44f, 1.90f, Mathf.Sin(a) * 0.44f),
                    new Vector3(0.60f, 0.035f, 0.035f), bronze, false);
                rib.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }

            // 四马并驾：横向一排，不再前后重叠
            for (int i = 0; i < 4; i++)
                Horse(g.transform, new Vector3((i - 1.5f) * 0.78f, 0f, 3.45f), bronze, bronzeDark);
        }

        static void Horse(Transform parent, Vector3 pos, Material bronze, Material dark)
        {
            var g = Empty("Horse", parent, pos);
            Box("Body", g.transform, new Vector3(0f, 0.82f, 0f), new Vector3(0.40f, 0.52f, 1.15f), bronze, false);
            Box("Chest", g.transform, new Vector3(0f, 0.86f, 0.52f), new Vector3(0.44f, 0.50f, 0.34f), bronze, false);
            var neck = Box("Neck", g.transform, new Vector3(0f, 1.16f, 0.62f), new Vector3(0.26f, 0.52f, 0.28f), bronze, false);
            neck.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            Box("Head", g.transform, new Vector3(0f, 1.38f, 0.86f), new Vector3(0.20f, 0.22f, 0.44f), bronze, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Ear", g.transform, new Vector3(s * 0.07f, 1.52f, 0.72f), new Vector3(0.05f, 0.12f, 0.05f), dark, false);
                for (int f = -1; f <= 1; f += 2)
                    Box("Leg", g.transform, new Vector3(s * 0.15f, 0.30f, f * 0.42f),
                        new Vector3(0.10f, 0.60f, 0.11f), dark, false);
            }
            Box("Tail", g.transform, new Vector3(0f, 0.86f, -0.62f), new Vector3(0.08f, 0.44f, 0.10f), dark, false);
        }

        // 封土轮廓：三级收分，级间留平台。数值是示意性的比例，不是实测数据。
        // 覆斗形封土：底边 76 m、高 21 m，坡面连续收分、越往上越陡。
        // 不做水平台级——真实封土的夯土台级早被覆土和草盖住了，
        // 从地面看出去只有一道一道连续收分的缓坡。之前加 0.6 m 平台，
        // 平台朝上的那条面在满阳光下烧成一条白腰带，整座封土读成拦河坝。
        // 实测封土：底边约 350 × 345 m、现高 51.3 m，覆斗形，顶面 24 × 10.4 m。
        // 陵园本身已经压缩到 1/4 左右，这里按同一比例折算：底边 81 m、高 20 m、顶面 17 m。
        // 关键是"宽而低"——原来是 78 m 底边配 21 m 高，坡度 41°，读出来是一座锥形土山，
        // 不像照片里那个平缓的方台。
        static readonly float[] QinMoundHalf = { 40.4f, 39.2f, 36.2f, 32.4f, 28.4f, 24.0f, 19.2f, 14.2f, 8.6f };
        static readonly float[] QinMoundY    = { -2.6f,  0.0f,  2.8f,  5.8f,  8.8f, 11.8f, 14.8f, 17.4f, 20.0f };

        static float QinMoundHalfAt(float y)
        {
            for (int i = 0; i < QinMoundY.Length - 1; i++)
            {
                if (y <= QinMoundY[i + 1])
                {
                    float t = Mathf.InverseLerp(QinMoundY[i], QinMoundY[i + 1], y);
                    return Mathf.Lerp(QinMoundHalf[i], QinMoundHalf[i + 1], Mathf.Clamp01(t));
                }
            }
            return QinMoundHalf[QinMoundHalf.Length - 1];
        }

        /// <summary>封土实体：一圈圈方环连成的截锥面，坡面连续，没有层叠的台阶。</summary>
        static Mesh MoundMesh()
        {
            var hs = new List<float>(QinMoundHalf);
            var ys = new List<float>(QinMoundY);

            int n = hs.Count;

            // 环上的四个侧面（k 的顺序要和下面 k=0..3 的顶点顺序一致：-Z、+X、+Z、-X）
            var sideDir = new[]
            {
                new Vector3(0f, 0f, -1f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 1f), new Vector3(-1f, 0f, 0f)
            };

            // 环 r → r+1 之间那一圈面的法线，解析算出来，不靠 RecalculateNormals。
            // 收分时 dh < 0（往上收），坡面外法线 = 水平外法线 * dy + 朝上 * (-dh)。
            // 相邻两环同高（dy≈0）就是那圈 0.6 m 宽的台级平台，法线朝正上。
            var segN = new Vector3[n - 1, 4];
            for (int r = 0; r < n - 1; r++)
            {
                float dh = hs[r + 1] - hs[r];
                float dy = ys[r + 1] - ys[r];
                bool shelf = Mathf.Abs(dy) < 1e-4f;
                for (int k = 0; k < 4; k++)
                    segN[r, k] = shelf ? Vector3.up : (sideDir[k] * dy + Vector3.up * -dh).normalized;
            }

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 每个坡面单独出顶点：环带上下用相邻两段法线的平均，
            // 台级平台与坡面之间保持硬边，封土才不会读成一圈圈明暗横带。
            for (int r = 0; r < n - 1; r++)
            {
                for (int k = 0; k < 4; k++)
                {
                    int k2 = (k + 1) % 4;
                    Vector3 p0 = RingVertex(r, k);
                    Vector3 p1 = RingVertex(r, k2);
                    Vector3 p2 = RingVertex(r + 1, k2);
                    Vector3 p3 = RingVertex(r + 1, k);

                    Vector3 nUp = RingNormal(segN, n, r, k);
                    Vector3 nDn = RingNormal(segN, n, r + 1, k);

                    int b = verts.Count;
                    verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                    norms.Add(nUp); norms.Add(nUp); norms.Add(nDn); norms.Add(nDn);
                    uvs.Add(UvOf(p0)); uvs.Add(UvOf(p1)); uvs.Add(UvOf(p2)); uvs.Add(UvOf(p3));

                    tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 1);
                    tris.Add(b + 0); tris.Add(b + 3); tris.Add(b + 2);
                }
            }

            // 顶面
            {
                float h = hs[n - 1], y = ys[n - 1];
                int b = verts.Count;
                verts.Add(new Vector3(-h, y, -h));
                verts.Add(new Vector3(h, y, -h));
                verts.Add(new Vector3(h, y, h));
                verts.Add(new Vector3(-h, y, h));
                for (int i = 0; i < 4; i++) norms.Add(Vector3.up);
                for (int i = 0; i < 4; i++) uvs.Add(UvOf(verts[b + i]));
                tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b + 0); tris.Add(b + 3); tris.Add(b + 2);
            }

            var mesh = new Mesh();
            mesh.name = "TumulusBody";
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;

            Vector3 RingVertex(int r, int k)
            {
                float h = hs[r], y = ys[r];
                switch (k)
                {
                    case 0: return new Vector3(-h, y, -h);
                    case 1: return new Vector3(h, y, -h);
                    case 2: return new Vector3(h, y, h);
                    default: return new Vector3(-h, y, h);
                }
            }

            Vector2 UvOf(Vector3 v) => new Vector2((v.x + v.z) * 0.30f, (v.y + 2.4f) * 0.30f);

            // 环上的顶点法线：两侧都是坡面就取平均（坡面连续收分，不出现明暗折痕）；
            // 任意一侧是平台就保留本段自己的法线，形成硬边。
            Vector3 RingNormal(Vector3[,] seg, int count, int ring, int k)
            {
                if (ring <= 0) return seg[0, k];
                if (ring >= count - 1) return seg[count - 2, k];
                Vector3 lo = seg[ring - 1, k], hi = seg[ring, k];
                bool loShelf = lo.y > 0.999f, hiShelf = hi.y > 0.999f;
                if (loShelf || hiShelf) return hi;
                return (lo + hi).normalized;
            }
        }

        /// <summary>封土：覆斗形夯土台，连续收分，南面设踏道。只做地面可见的土台，不做地宫。</summary>
        static void QinTumulus(Transform parent, Vector3 center, Material rammed, Material rammedDark,
                               Material rammedPale, Material grass, Material dryGrass, Material stone,
                               Material pine, Material cypress, Material timber, Material lowGrass,
                               Material shrub)
        {
            var g = Empty("Tumulus", parent, center);
            float gy = QinlingHeight(center.x, center.z);
            float top = QinMoundY[QinMoundY.Length - 1];
            float topHalf = QinMoundHalf[QinMoundHalf.Length - 1];

            // 封土本体是草土坡，不是裸夯土：原来的土黄色让整座封土读成人工金字塔。
            // 但它比陵园草地的草色要浅一度——同样的颜色贴在大斜面上，
            // 因为坡面法线比地面更偏斜，读出来会明显比周围暗，像压在场地上的黑石头。
            // 坡面法线比平地更偏斜，同一套明暗参数贴上去会明显偏暗。
            // 这里把 _Wrap 提上去、边缘光压掉，让封土和周围草地落在同一个明度上——
            // 否则它就是一坨压在场地上的黑绿色锥体。
            var moundGrass = MatAs("QnMoundGrass", new Color(0.50f, 0.51f, 0.31f), "foliage", 0f, 0.18f);
            moundGrass.SetFloat("_Wrap", 0.64f);
            moundGrass.SetFloat("_RimStrength", 0.07f);
            moundGrass.SetFloat("_VertexColorInfluence", 0f);
            EditorUtility.SetDirty(moundGrass);
            MeshObject("Body", g.transform, new Vector3(0f, gy, 0f), MoundMesh(), moundGrass, true);

            // 顶面：覆草，中央留一小块夯土
            Box("TopGrass", g.transform, new Vector3(0f, gy + top + 0.04f, 0f),
                new Vector3(topHalf * 2f - 1.2f, 0.20f, topHalf * 2f - 1.2f), moundGrass, false);
            Box("TopCore", g.transform, new Vector3(0f, gy + top + 0.16f, 0f),
                new Vector3(6.2f, 0.14f, 6.2f), rammedPale, false);

            // 不再另外压浅色环带：平台面朝上，在满阳光下本来就是全图最亮的一条，
            // 再叠一层干草色盒子只会把它烧成一条白腰带。

            // 南侧踏道：每一级都贴着当时的坡面
            const int steps = 34;
            float rise = top / steps;
            for (int i = 0; i < steps; i++)
            {
                float y = (i + 0.5f) * rise;
                // 石踏道：5 m 宽的土色带从坡顶一直铺到坡脚，远看就是一条棕色地毯。
                // 收窄到 3.4 m，换成浅石色，才读得出是一级一级砌上去的台阶。
                // 石色踏道在绿坡上是一条深色地毯。改回夯土色，读作踩出来的土路。
                Box("Step_" + i, g.transform, new Vector3(0f, gy + y, -(QinMoundHalfAt(y) + 0.16f)),
                    new Vector3(4.6f, rise * 1.06f, 1.10f), rammed, true);
            }

            // 坡面草：半枯半青，贴着锥面撒，别浮在空中
            for (int i = 0; i < 460; i++)
            {
                float y = 0.4f + Rnd(i * 7 + 3) * (top - 1.6f);
                float half = QinMoundHalfAt(y);
                float u = (Rnd(i * 13 + 9) - 0.5f) * 1.88f;
                int side = i % 4;
                float px = (side == 1) ? half : ((side == 3) ? -half : u * half);
                float pz = (side == 0) ? -half : ((side == 2) ? half : u * half);
                GrassTuft(g.transform, new Vector3(px, y, pz), (i % 3 == 0) ? dryGrass : lowGrass,
                    0.95f + Rnd(i * 5 + 1) * 0.7f, i * 3 + 411);
            }

            // 封土根部散落的干草
            for (int i = 0; i < 54; i++)
            {
                float a = Rnd(i * 5 + 1) * Mathf.PI * 2f;
                float r = QinMoundHalf[0] + 1.2f + Rnd(i * 9 + 3) * 4.6f;
                GrassTuft(g.transform, new Vector3(Mathf.Cos(a) * r, QinlingHeight(
                    center.x + Mathf.Cos(a) * r, center.z + Mathf.Sin(a) * r) - gy + 0.02f,
                    Mathf.Sin(a) * r), dryGrass, 1.3f, i * 3 + 77);
            }

            // 封土顶的树：北半边种，南边留出踏道的落脚处
            for (int i = 0; i < 7; i++)
            {
                float a = -0.15f + Rnd(i * 13 + 5) * Mathf.PI * 1.15f;
                float r = 2.5f + Rnd(i * 17 + 9) * (topHalf * 0.55f);
                float px = Mathf.Cos(a) * r;
                float pz = Mathf.Sin(a) * r + 2.0f;
                Conifer(g.transform, new Vector3(px, top + 0.06f, pz),
                    i * 7 + 601, (i % 2 == 0) ? cypress : pine, timber,
                    0.48f + Rnd(i * 5) * 0.24f);
            }

            // 坡面上的树：今天的封土长满柏树，不是一座光台子。
            // 南面踏道两侧留白，北、东、西三面密一些。
            for (int i = 0; i < 120; i++)
            {
                float y = 2.4f + Rnd(i * 11 + 3) * (top - 5.0f);
                float half = QinMoundHalfAt(y);
                int side = i % 4;
                float u = (Rnd(i * 19 + 7) - 0.5f) * 1.7f;
                if (side == 0 && Mathf.Abs(u) < 0.42f) continue;     // 踏道正中不种
                float px = (side == 1) ? half : ((side == 3) ? -half : u * half);
                float pz = (side == 0) ? -half : ((side == 2) ? half : u * half);
                Conifer(g.transform, new Vector3(px, y, pz), i * 13 + 701,
                    (i % 3 == 0) ? pine : cypress, timber, 0.46f + Rnd(i * 7) * 0.34f);
            }

            // 坡脚灌木
            for (int i = 0; i < 30; i++)
            {
                float a = Rnd(i * 5 + 11) * Mathf.PI * 2f;
                float r = QinMoundHalf[0] - 1.5f + Rnd(i * 7 + 13) * 5.0f;
                float px = Mathf.Cos(a) * r, pz = Mathf.Sin(a) * r;
                var bush = MeshObject("Bush_" + i, g.transform,
                    new Vector3(px, QinlingHeight(center.x + px, center.z + pz) - gy + 0.32f, pz),
                    MeshFactory.Canopy(0.75f + Rnd(i * 17) * 0.55f, i * 3 + 811), shrub, false);
                bush.transform.localScale = new Vector3(1.2f, 0.72f, 1.2f);
            }
        }

        /// <summary>建筑基址：夯土台基 + 柱础 + 散水。上部结构无据可考，不重建。</summary>
        static void QinPlatform(Transform parent, Vector3 center, float w, float d,
                                Material rammed, Material rammedDark, Material stone)
        {
            var g = Empty("Platform", parent, center);
            float gy = QinlingHeight(center.x, center.z);

            for (int i = 0; i < 2; i++)
            {
                Box("Base_" + i, g.transform, new Vector3(0f, gy + 0.30f + i * 0.30f, 0f),
                    new Vector3(w - i * 1.4f, 0.32f, d - i * 1.4f), (i == 0) ? rammedDark : rammed, true);
            }
            Box("BaseTop", g.transform, new Vector3(0f, gy + 0.96f, 0f),
                new Vector3(w - 2.8f, 0.14f, d - 2.8f), rammedDark, false);

            // 散水（台基四周的碎石带）
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Apron_X", g.transform, new Vector3(0f, gy + 0.08f, s * (d * 0.5f + 0.9f)),
                    new Vector3(w + 2.6f, 0.16f, 1.6f), stone, false);
                Box("Apron_Z", g.transform, new Vector3(s * (w * 0.5f + 0.9f), gy + 0.08f, 0f),
                    new Vector3(1.6f, 0.16f, d + 2.6f), stone, false);
            }

            // 柱础：三开间 × 两进
            for (int ix = -1; ix <= 1; ix++)
            {
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    float x = ix * (w * 0.30f);
                    float z = iz * (d * 0.24f);
                    Cyl("Plinth", g.transform, new Vector3(x, gy + 0.98f, z), 0.46f, 0.16f, stone);
                    Cyl("PlinthCore", g.transform, new Vector3(x, gy + 1.10f, z), 0.18f, 0.08f, rammedDark);
                }
            }
        }

        /// <summary>修陵人工棚遗址：半地穴、柱洞、烧土面与陶文戳印残片。</summary>
        static void QinWorkShed(Transform parent, Vector3 center, Material earth, Material rammedDark,
                                Material timber, Material timberPale, Material pottery, Material potteryDark,
                                Material charcoal, Material stone)
        {
            var g = Empty("WorkShed", parent, center);
            float gy = QinlingHeight(center.x, center.z);

            // 半地穴凹地（用一圈低土坎围出）
            Box("HutFloor", g.transform, new Vector3(0f, gy - 0.30f, 0f), new Vector3(11.0f, 0.5f, 8.0f), earth, true);
            for (int s = -1; s <= 1; s += 2)
            {
                Box("HutCurb_X", g.transform, new Vector3(0f, gy + 0.14f, s * 4.4f),
                    new Vector3(12.0f, 0.34f, 0.9f), rammedDark, true);
                Box("HutCurb_Z", g.transform, new Vector3(s * 5.9f, gy + 0.14f, 0f),
                    new Vector3(0.9f, 0.34f, 8.8f), rammedDark, true);
            }

            // 柱洞：成对排列，只留洞与残柱
            for (int i = 0; i < 4; i++)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float x = -4.2f + i * 2.8f;
                    float z = s * 3.2f;
                    Cyl("PostHole", g.transform, new Vector3(x, gy + 0.02f, z), 0.26f, 0.10f, charcoal);
                    if (i % 2 == 0)
                        Cyl("PostStub", g.transform, new Vector3(x, gy + 0.24f, z), 0.13f, 0.46f, timber);
                }
            }

            // 东端尚立的半间棚屋：三根残柱 + 檩 + 一面草顶，让工棚读成"棚"
            for (int i = 0; i < 3; i++)
            {
                Cyl("StandPost", g.transform, new Vector3(2.6f + (i % 2) * 2.2f, gy + 0.95f, -2.0f + i * 2.0f),
                    0.14f, 1.90f, timber);
            }
            Box("Purlin", g.transform, new Vector3(3.7f, gy + 1.92f, 0.1f),
                new Vector3(5.6f, 0.16f, 0.16f), timberPale, true);
            Box("Purlin2", g.transform, new Vector3(3.7f, gy + 1.96f, -2.6f),
                new Vector3(5.6f, 0.14f, 0.14f), timber, false);
            for (int i = 0; i < 5; i++)
            {
                var th = Box("ThatchRoof", g.transform,
                    new Vector3(1.5f + i * 1.1f, gy + 2.20f + (i % 2) * 0.03f, -1.0f),
                    new Vector3(1.20f, 0.10f, 4.6f), timber, true);
                th.transform.localRotation = Quaternion.Euler(0f, (i % 2 == 0) ? 3f : -2f, 8f);
            }

            // 陶瓮两只，压住空地的疏
            for (int i = 0; i < 2; i++)
            {
                Cyl("Jar", g.transform, new Vector3(-4.4f + i * 1.1f, gy + 0.42f, 2.4f - i * 0.8f),
                    0.34f, 0.84f, potteryDark);
                Cyl("JarMouth", g.transform, new Vector3(-4.4f + i * 1.1f, gy + 0.86f, 2.4f - i * 0.8f),
                    0.24f, 0.10f, pottery);
            }

            // 烧土面（灶）
            Cyl("Hearth", g.transform, new Vector3(1.6f, gy + 0.04f, -0.6f), 1.15f, 0.10f, charcoal);
            Cyl("HearthRim", g.transform, new Vector3(1.6f, gy + 0.10f, -0.6f), 1.42f, 0.08f, rammedDark);
            for (int i = 0; i < 9; i++)
            {
                float a = Rnd(i * 7 + 2) * Mathf.PI * 2f;
                Ball("Cinder", g.transform, new Vector3(1.6f + Mathf.Cos(a) * 1.1f, gy + 0.10f, -0.6f + Mathf.Sin(a) * 1.1f),
                    0.09f + Rnd(i * 11) * 0.06f, charcoal);
            }

            // 陶片堆
            for (int i = 0; i < 34; i++)
            {
                float x = (Rnd(i * 3 + 5) - 0.5f) * 9.0f;
                float z = (Rnd(i * 17 + 7) - 0.5f) * 6.4f;
                var shard = Box("Shard", g.transform, new Vector3(x, gy + 0.06f + Rnd(i * 5) * 0.05f, z),
                    new Vector3(0.16f + Rnd(i * 13) * 0.22f, 0.035f, 0.14f + Rnd(i * 19) * 0.20f),
                    (i % 4 == 0) ? potteryDark : pottery, false);
                shard.transform.localRotation = Quaternion.Euler(
                    (Rnd(i * 23) - 0.5f) * 12f, Rnd(i * 29) * 180f, (Rnd(i * 31) - 0.5f) * 12f);
            }

            // 陶文戳印残片：一块立着的砖，压在重点线索处
            var brick = Box("StampedBrick", g.transform, new Vector3(-2.3f, gy + 0.30f, 1.9f),
                new Vector3(0.62f, 0.16f, 0.44f), pottery, true);
            brick.transform.localRotation = Quaternion.Euler(-6f, 24f, 4f);
            Box("StampMark", brick.transform, new Vector3(0f, 0.52f, 0f),
                new Vector3(0.30f, 0.02f, 0.24f), charcoal, false);

            // 石砧与石锤
            Box("Anvil", g.transform, new Vector3(3.4f, gy + 0.24f, 2.2f), new Vector3(1.3f, 0.46f, 0.9f), stone, true);
            Ball("Hammer", g.transform, new Vector3(3.0f, gy + 0.56f, 2.6f), 0.22f, stone);

            // 一截倒塌的木构
            var beam = Box("FallenBeam", g.transform, new Vector3(-3.2f, gy + 0.20f, -2.6f),
                new Vector3(3.4f, 0.20f, 0.20f), timberPale, true);
            beam.transform.localRotation = Quaternion.Euler(0f, 28f, 5f);
            for (int i = 0; i < 4; i++)
                Box("Thatch", g.transform, new Vector3(-2.4f + i * 1.1f, gy + 0.10f, -3.1f + i * 0.5f),
                    new Vector3(1.3f, 0.07f, 0.9f), timber, false);
        }

        /// <summary>秦权秦量：石案上置三枚铜权、一只量器与诏版。</summary>
        static void QinWeights(Transform parent, Vector3 center, Material stone, Material stoneDark,
                               Material bronze, Material bronzeDark, Material patina)
        {
            var g = Empty("Weights", parent, center);
            float gy = QinlingHeight(center.x, center.z);

            Box("Table", g.transform, new Vector3(0f, gy + 0.52f, 0f), new Vector3(4.4f, 0.24f, 2.2f), stone, true);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box("TableLeg", g.transform, new Vector3(sx * 1.7f, gy + 0.22f, sz * 0.78f),
                        new Vector3(0.34f, 0.44f, 0.34f), stoneDark, true);

            // 三枚秦权：钟形，上小下大
            float[] px = { -1.4f, -0.30f, 0.75f };
            float[] ph = { 0.92f, 0.72f, 0.54f };
            for (int i = 0; i < 3; i++)
            {
                float h = ph[i];
                Box("Quan_" + i, g.transform, new Vector3(px[i], gy + 0.64f + h * 0.5f, -0.32f + i * 0.06f),
                    new Vector3(0.52f - i * 0.05f, h, 0.52f - i * 0.05f), bronze, true);
                Box("QuanCap_" + i, g.transform, new Vector3(px[i], gy + 0.64f + h + 0.05f, -0.32f + i * 0.06f),
                    new Vector3(0.30f - i * 0.03f, 0.10f, 0.30f - i * 0.03f), bronzeDark, false);
                Cyl("QuanKnob_" + i, g.transform, new Vector3(px[i], gy + 0.64f + h + 0.16f, -0.32f + i * 0.06f),
                    0.07f, 0.14f, bronzeDark);
                Box("QuanBand_" + i, g.transform, new Vector3(px[i], gy + 0.64f + h * 0.55f, -0.32f + i * 0.06f),
                    new Vector3(0.56f - i * 0.05f, 0.05f, 0.56f - i * 0.05f), patina, false);
            }

            // 秦量：方口量器
            Box("Liang", g.transform, new Vector3(1.85f, gy + 0.88f, 0.55f), new Vector3(0.82f, 0.62f, 0.62f), patina, true);
            Box("LiangRim", g.transform, new Vector3(1.85f, gy + 1.20f, 0.55f), new Vector3(0.90f, 0.08f, 0.70f), bronzeDark, false);

            // 诏版：斜倚在石案前的铜板
            var slab = Box("ZhaoBan", g.transform, new Vector3(-1.9f, gy + 0.42f, 1.18f),
                new Vector3(1.15f, 0.80f, 0.06f), bronze, true);
            slab.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            Box("ZhaoBanText", slab.transform, new Vector3(0f, 0.02f, -1.6f),
                new Vector3(0.86f, 0.62f, 0.02f), bronzeDark, false);

            // 一圈小卵石
            for (int i = 0; i < 14; i++)
            {
                float a = Rnd(i * 5 + 9) * Mathf.PI * 2f;
                Ball("Pebble", g.transform, new Vector3(Mathf.Cos(a) * 2.6f, gy + 0.09f, Mathf.Sin(a) * 1.7f),
                    0.11f + Rnd(i * 7) * 0.07f, stoneDark);
            }
        }

        /// <summary>松柏：直干 + 三层锥冠。锥冠比低模球冠更接近针叶树的轮廓。</summary>
        static void Conifer(Transform parent, Vector3 pos, int seed, Material crownMat,
                            Material trunkMat, float scale)
        {
            var g = Empty("Conifer", parent, pos);
            g.transform.localScale = Vector3.one * scale;
            g.transform.localRotation = Quaternion.Euler(0f, Rnd(seed * 3 + 1) * 360f, 0f);

            // 冠幅要够：原来 1.2 m 半径配 10 m 高，远看就是一根尖锥子，
            // 成排立在塬上像路障。松柏的冠幅大约是高的 1/4。
            float h = 8.4f + Rnd(seed * 5 + 2) * 4.6f;
            float r = 2.05f + Rnd(seed * 7 + 3) * 0.65f;

            Cyl("Trunk", g.transform, new Vector3(0f, h * 0.24f, 0f), 0.17f, h * 0.48f, trunkMat);

            // 冠是一个连续的柱形毛边体：叠几层锥会读成圣诞树
            MeshObject("Crown", g.transform, new Vector3(0f, h * 0.10f, 0f),
                ConiferCrown(r, h * 0.92f, seed * 13 + 5), crownMat);
        }

        /// <summary>柏冠网格：多环柱形，逐环抖动半径，边缘毛糙但轮廓连续。</summary>
        static Mesh ConiferCrown(float radius, float height, int seed)
        {
            const int seg = 10;
            const int rings = 6;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r < rings; r++)
            {
                float t = r / (float)rings * 0.94f;
                float y = height * t;
                float rad = radius * Mathf.Pow(1f - t, 0.58f);
                for (int i = 0; i < seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    float k = 0.80f + Rnd(seed + r * 31 + i) * 0.44f;
                    float rr = rad * k * (r % 2 == 0 ? 1f : 1.10f);
                    verts.Add(new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr));
                    uvs.Add(new Vector2(i / (float)seg, t));
                }
            }
            int apex = verts.Count;
            verts.Add(new Vector3(0f, height, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            for (int r = 0; r < rings - 1; r++)
            {
                for (int i = 0; i < seg; i++)
                {
                    int i1 = (i + 1) % seg;
                    int l0 = r * seg + i, l1 = r * seg + i1;
                    int u0 = (r + 1) * seg + i, u1 = (r + 1) * seg + i1;
                    tris.Add(l0); tris.Add(u0); tris.Add(u1);
                    tris.Add(l0); tris.Add(u1); tris.Add(l1);
                }
            }
            int last = (rings - 1) * seg;
            for (int i = 0; i < seg; i++)
            {
                tris.Add(last + i); tris.Add(apex); tris.Add(last + (i + 1) % seg);
            }

            var m = new Mesh();
            m.name = "ConiferCrown";
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>锥形冠网格：底圈轻微抖动，避免机械对称。</summary>
        static Mesh ConeMesh(float radius, float height, int seed)
        {
            const int seg = 9;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float k = 0.86f + Rnd(seed + i) * 0.30f;
                verts.Add(new Vector3(Mathf.Cos(a) * radius * k, 0f, Mathf.Sin(a) * radius * k));
                uvs.Add(new Vector2(i / (float)seg, 0f));
            }
            verts.Add(new Vector3(0f, height, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            for (int i = 0; i < seg; i++)
            {
                tris.Add(i);
                tris.Add(seg);
                tris.Add((i + 1) % seg);
            }

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>陵园植被：干草铺满空地，松柏成列。避开坑、垣、封土与神道。</summary>
        static void QnFlora(Transform parent, Material grass, Material dryGrass, Material pine,
                            Material cypress, Material earth, Material autumn, Material russet,
                            Material shrub, Material trunk)
        {
            var g = Empty("QnFlora", parent, Vector3.zero);

            bool InCorridor(float x, float z)
            {
                return z > -114f && z < -38f && Mathf.Abs(x) < 13.0f;                                 // 入口与神道
            }

            bool Blocked(float x, float z)
            {
                if (Mathf.Abs(x) < 7.4f && z > QnInnerZ0 - 4f && z < QnMoundZ - 26f) return true;     // 内城甬道
                if (x > QnPitX - 42f && x < QnPitX + 42f && z > QnPitZ - 20f && z < QnPitZ + 20f) return true;
                if (x > QnBronzeX - 11f && x < QnBronzeX + 11f && z > QnBronzeZ - 9f && z < QnBronzeZ + 9f) return true;
                if (Mathf.Abs(x) < 34f && Mathf.Abs(z - QnMoundZ) < 35f) return true;                // 封土
                if (Mathf.Abs(Mathf.Abs(x) - QnOuterX) < 6.5f && z > QnOuterZ0 - 9f && z < QnOuterZ1 + 9f) return true;
                if (Mathf.Abs(z - QnOuterZ0) < 6.0f && Mathf.Abs(x) < QnOuterX) return true;
                if (Mathf.Abs(Mathf.Abs(x) - QnInnerX) < 7.0f && z > QnInnerZ0 - 9f && z < QnInnerZ1 + 9f) return true;
                if (Mathf.Abs(z - QnInnerZ0) < 6.5f && Mathf.Abs(x) < QnInnerX) return true;
                if (Mathf.Abs(z - QnInnerZ1) < 6.5f && Mathf.Abs(x) < QnInnerX) return true;
                if (Mathf.Abs(x + 36f) < 8f && Mathf.Abs(z - 14f) < 7f) return true;                 // 西台基
                if (Mathf.Abs(x - 36f) < 8f && Mathf.Abs(z - 14f) < 7f) return true;                 // 东台基
                if (Mathf.Abs(x + 58f) < 9f && Mathf.Abs(z + 32f) < 7f) return true;                 // 工棚
                if (Mathf.Abs(x - 18f) < 6f && Mathf.Abs(z + 33f) < 5f) return true;                 // 秦权
                if (Mathf.Abs(z - QnPitZ) < 6f && x > QnOuterX + 4f && x < QnPitX - 41f) return true; // 东侧土路
                if (Mathf.Abs(x - 60f) < 16f && z > -15f && z < 27f) return true;                    // 铜车马坑与机位
                if (Mathf.Abs(x + 43f) < 19f && z > -76f && z < -44f) return true;                    // 城垣剖面机位
                return false;
            }

            // 草：近密远疏。原来 4600 次撒在 ±148 m 的整块地上，
            // 平均每 40 平米才一丛，镜头里就是一块抹平的黄地。
            // 现在近场（±70 m）单独跑一遍足量，远处只留轮廓。
            void SowGrass(float halfExtent, int count, int seedBase, int stride)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = (Rnd(i * 3 + 11 + seedBase) - 0.5f) * halfExtent * 2f;
                    float z = (Rnd(i * 7 + 23 + seedBase) - 0.5f) * halfExtent * 2f;
                    if (Blocked(x, z)) continue;
                    if (InCorridor(x, z) && Mathf.Abs(x) < 6.5f) continue;
                    if (stride > 1 && i % stride != 0) continue;
                    Material m = (i % 5 == 0) ? dryGrass : grass;
                    GrassTuft(g.transform, new Vector3(x, QinlingHeight(x, z) - 0.02f, z),
                        m, 1.00f + Rnd(i * 13 + seedBase) * 0.80f, i + seedBase + 500);
                }
            }
            // 每丛草是 6 个物体，密度不能无脑堆：总量控制在 3600 丛左右，
            // 靠"近处实、远处虚"而不是靠数量填满。
            SowGrass(70f, 2100, 0, 1);
            SowGrass(148f, 1300, 977, 2);

            // 松柏成丛，不再均匀撒点：一丛 3–8 棵，大小拉开
            for (int c = 0; c < 54; c++)
            {
                float cx = (Rnd(c * 17 + 31) - 0.5f) * 292f;
                float cz = (Rnd(c * 23 + 43) - 0.5f) * 292f;
                if (Blocked(cx, cz) || InCorridor(cx, cz)) continue;
                int n = 3 + (int)(Rnd(c * 29 + 5) * 6f);
                for (int k = 0; k < n; k++)
                {
                    float x = cx + (Rnd(c * 31 + k * 7 + 3) - 0.5f) * 22f;
                    float z = cz + (Rnd(c * 37 + k * 11 + 5) - 0.5f) * 22f;
                    if (Blocked(x, z) || InCorridor(x, z)) continue;
                    if (Mathf.Abs(x) < 26f && z > -62f && z < 22f) continue;
                    Material crown = ((c + k) % 2 == 0) ? cypress : pine;
                    Conifer(g.transform, new Vector3(x, QinlingHeight(x, z) - 0.05f, z),
                        c * 47 + k * 7 + 91, crown, trunk, 0.58f + Rnd(k * 5 + c) * 0.72f);
                    // 林下灌木：只有树没有灌丛，远处看就是「草坪上插了一圈牙签」
                    int under = 3 + (int)(Rnd(c * 41 + k * 13 + 7) * 5f);
                    for (int u = 0; u < under; u++)
                    {
                        float ux = x + (Rnd(c * 53 + k * 17 + u * 7 + 1) - 0.5f) * 16f;
                        float uz = z + (Rnd(c * 59 + k * 19 + u * 11 + 3) - 0.5f) * 16f;
                        if (Blocked(ux, uz) || InCorridor(ux, uz)) continue;
                        var bush = MeshObject("Under_" + c + "_" + k + "_" + u, g.transform,
                            new Vector3(ux, QinlingHeight(ux, uz) + 0.30f, uz),
                            MeshFactory.Canopy(0.7f + Rnd(u * 5 + c) * 0.8f, u * 3 + c * 11 + 2101),
                            shrub, false);
                        bush.transform.localScale = new Vector3(1.3f, 0.62f, 1.3f);
                    }
                }
            }

            // 陵园外缘的针叶林带：把平原和远山接起来，也遮住地形网格的边。
            // 原来只种到 r≈156 m，镜头拉高以后，陵园外围是一整圈空黄地，
            // 中间一座城像个沙盘。现在一直种到 300 m，并让密度随距离衰减，
            // 近处成林、远处成带，平原才有"从这里一直长到山脚"的延续感。
            for (int i = 0; i < 460; i++)
            {
                float a = Rnd(i * 19 + 9) * Mathf.PI * 2f;
                float r = 78f + Mathf.Pow(Rnd(i * 23 + 3), 0.72f) * 222f;
                float x = Mathf.Cos(a) * r;
                float z = 8f + Mathf.Sin(a) * r;
                if (Mathf.Abs(x) < 16f && z < 0f) continue;          // 南入口视廊
                if (Blocked(x, z)) continue;
                if (r > 190f && i % 2 != 0) continue;                // 远场只留一半，省实例
                Conifer(g.transform, new Vector3(x, QinlingHeight(x, z) - 0.05f, z),
                    i * 11 + 1201, (i % 3 == 0) ? pine : cypress, trunk,
                    (0.75f + Rnd(i * 7) * 0.65f) * Mathf.Lerp(1f, 1.25f, Mathf.InverseLerp(80f, 300f, r)));
            }

            // 深秋落叶乔木：零星散布在塬上和外缘，暖黄赭红压住满屏土色
            for (int i = 0; i < 46; i++)
            {
                float x = (Rnd(i * 13 + 61) - 0.5f) * 280f;
                float z = (Rnd(i * 29 + 73) - 0.5f) * 280f;
                if (Blocked(x, z) || InCorridor(x, z)) continue;
                if (Mathf.Abs(x) < 22f && z > -64f && z < 26f) continue;
                MakeTree(g.transform, new Vector3(x, QinlingHeight(x, z) - 0.05f, z),
                    i * 17 + 1301, (i % 3 == 0) ? russet : autumn, trunk,
                    0.75f + Rnd(i * 5) * 0.55f);
            }

            // 灌木：墙根、路边、封土外圈，压出近景的杂木层
            for (int i = 0; i < 110; i++)
            {
                float x = (Rnd(i * 31 + 17) - 0.5f) * 230f;
                float z = (Rnd(i * 41 + 37) - 0.5f) * 230f;
                if (Blocked(x, z) || InCorridor(x, z)) continue;
                if (Mathf.Abs(x) < 4f) continue;
                var bush = MeshObject("Shrub_" + i, g.transform,
                    new Vector3(x, QinlingHeight(x, z) + 0.28f, z),
                    MeshFactory.Canopy(0.55f + Rnd(i * 7) * 0.65f, i * 3 + 1501), shrub, false);
                bush.transform.localScale = new Vector3(1.25f, 0.68f, 1.25f);
            }

            // 封土北侧与东西两侧的柏树林，压出陵园的纵深感
            for (int i = 0; i < 70; i++)
            {
                float a = Rnd(i * 11 + 7) * Mathf.PI * 2f;
                float r = 36f + Rnd(i * 13 + 5) * 22f;
                float x = Mathf.Cos(a) * r;
                float z = QnMoundZ + Mathf.Sin(a) * r;
                if (z < QnMoundZ + 6f) continue;                     // 南侧（神道方向）留出视廊
                if (Blocked(x, z)) continue;
                Conifer(g.transform, new Vector3(x, QinlingHeight(x, z) - 0.05f, z),
                    i * 5 + 311, (i % 3 == 0) ? pine : cypress, trunk, 1.00f + Rnd(i * 3) * 0.40f);
            }

            // 骊山北坡的林带：只撒 8–120 m 海拔、陵园以外（r > 165）的坡面，
            // 远看是一层毛茸茸的深绿，近走才是一棵棵松柏。
            for (int i = 0; i < 3200; i++)
            {
                float x = (Rnd(i * 53 + 29) - 0.5f) * 780f;
                float z = (Rnd(i * 59 + 37) - 0.5f) * 780f;
                float r = Mathf.Sqrt(x * x + (z - 8f) * (z - 8f));
                if (r < 165f) continue;
                float hgt = QinlingHeight(x, z);
                if (hgt < 6f || hgt > 112f) continue;
                // 林线以上和陡壁留白：噪声决定哪一片长树、哪一片露岩。
                // 阈值 0.30 时树太散，远看是一根根黑钉子；提到 0.44 让它们成片，
                // 疏的地方才露出岩坡——这才是山坡上"林斑"的读法。
                if (Mathf.PerlinNoise(x * 0.03f + 17.1f, z * 0.03f + 5.3f) < 0.44f) continue;
                Conifer(g.transform, new Vector3(x, hgt - 0.15f, z),
                    i * 7 + 2201, (i % 3 == 0) ? pine : cypress, trunk,
                    0.55f + Rnd(i * 3) * 0.45f);
            }
        }

    }
}
