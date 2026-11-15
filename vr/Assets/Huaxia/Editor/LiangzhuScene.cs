using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 第一章 · 良渚古城遗址。
    ///
    /// 空间结构依据「良渚古城遗址」的公开考古认识组织，而不是自由虚构：
    ///   湿地平原与河道 → 水稻田 → 干栏式聚落 → 宽厚土垄城牆（陆城门 / 水门）
    ///   → 城内水道 → 莫角山台地（宫殿区基址）→ 反山墓地 → 外围水利系统（塘山长堤与水库）。
    ///   遗址标识以玉琮为形（内圆外方、分节、神人兽面纹的简化表达）。
    ///
    /// 所有几何与材质为研究性示意，不代表遗址原貌。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        // ================================================================ 地形

        const float LiCityFloor  = 1.10f;
        const float LiMojiaoRise = 2.40f;
        const float LiWallRise   = 3.20f;

        const float LiX0 = -34f, LiX1 = 34f, LiZ0 = 6f, LiZ1 = 58f;      // 城墙中心线
        const float LiMoX0 = -22f, LiMoX1 = 22f, LiMoZ0 = 22f, LiMoZ1 = 48f; // 莫角山台地
        const float LiMojiaoTop = LiCityFloor + LiMojiaoRise;

        const float LiWaterS = -0.02f;              // 南引水河道水面（略高于南岸平地）
        const float LiWaterC = LiCityFloor - 0.32f; // 城内河道水面（低于城内地面）
        const float LiWaterR = -0.18f;              // 水库水面（略低于库岸）
        // 水面专用层：从点光源的 cullingMask 中排除，避免 ForwardAdd 叠加产生色块
        const int LiWaterLayer = WaterLayer;

        static float LiRect(float x, float z, float x0, float x1, float z0, float z1, float soft)
        {
            float ax = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(x0 - soft, x0, x));
            float bx = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(x1 + soft, x1, x));
            float az = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(z0 - soft, z0, z));
            float bz = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(z1 + soft, z1, z));
            return ax * bx * az * bz;
        }

        static float LiRing(float x, float z, float gx, float gz, float r)
        {
            float d = Mathf.Sqrt((x - gx) * (x - gx) + (z - gz) * (z - gz));
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r, r * 0.30f, d));
        }

        /// <summary>城墙土垄：以城址矩形边界为中心线的宽厚土垄（良渚城墙底宽数十米）。</summary>
        static float LiWallMask(float x, float z)
        {
            float dx = Mathf.Min(x - LiX0, LiX1 - x);
            float dz = Mathf.Min(z - LiZ0, LiZ1 - z);
            float d = Mathf.Min(dx, dz);
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - Mathf.Abs(d) / 7.5f));
        }

        /// <summary>城门与水门位置：把土垄打断，形成通道。</summary>
        static float LiGateOpen(float x, float z)
        {
            float o = LiRing(x, z, 0f, LiZ0, 7.5f);
            // 南陆门是一条直的穿城通道：环形开口只在门口中心挖得最开，
            // 往城里走 4–7 m 还压着 0.4 m 的土垄，进门后先上一道埂再下一个坑。
            // 这里把走廊整段切开，门内和城内地面拉平。
            o = Mathf.Max(o, LiRect(x, z, -4.2f, 4.2f, 1.5f, 14f, 2.4f));
            o = Mathf.Max(o, LiRing(x, z, 21f, LiZ0, 4.2f));
            o = Mathf.Max(o, LiRing(x, z, -21f, LiZ0, 4.2f));
            o = Mathf.Max(o, LiRing(x, z, LiX0, 32f, 4.2f));
            o = Mathf.Max(o, LiRing(x, z, LiX1, 32f, 4.2f));
            o = Mathf.Max(o, LiRing(x, z, 0f, LiZ1, 7.5f));
            return o;
        }

        /// <summary>
        /// 南城墙考古探沟：在土垄上切出一个凹口，用来露出城墙的堆筑层理。
        /// 位置选在陆城门与水门之间的南城墙段，正对入城道路的右手边。
        /// </summary>
        const float LiTrenchFloor = 0.95f;

        static float LiTrenchMask(float x, float z)
        {
            return LiRect(x, z, 8.6f, 16.8f, -2.6f, 6.20f, 0.55f);
        }

        static float LiCanalMask(float x, float z)
        {
            float a = LiRect(x, z, -84f, 84f, -47f, -41f, 2.2f);
            float b = LiRect(x, z, -34f, 34f, 12.5f, 16.5f, 2.2f);
            // 原来还有一条 c（x −10..10，z 6..16.5）：想在南陆门后开一条纵向水道，
            // 结果把进城主轴整条压低了 1.15 m——路板悬在头顶 1 m，人掉进沟里，
            // 再被城内木桥 0.86 m 的立面顶住，正是「进门卡在缝里」。
            // 城门到木桥的主轴必须是实的，城内东西向水道（b）保留。
            return Mathf.Max(a, b);
        }

        static float LiReservoirMask(float x, float z)
        {
            return LiRect(x, z, -80f, 80f, 64f, 80f, 6f);
        }

        public static float LiangzhuHeightProbe(float x, float z) { return LiangzhuHeight(x, z); }

        static float LiangzhuHeight(float x, float z) { return LiangzhuHeight(x, z, true); }

        /// <summary>trench=false 时不含探沟切口，用来取得城墙原始的堆筑高度（剖面顶部对齐用）。</summary>
        static float LiangzhuHeight(float x, float z, bool trench)
        {
            float n = Mathf.PerlinNoise(x * 0.035f + 17f, z * 0.035f + 9f);
            float plain = (n - 0.5f) * 0.55f;

            // 主路走廊压平，方便铺路与摆件
            float road = LiRect(x, z, -3.2f, 3.2f, -62f, 4f, 3.5f);
            plain = Mathf.Lerp(plain, 0f, road);

            // 河道两侧岸顶压平，否则起伏的岸唇会遮住水面
            float bank = LiRect(x, z, -90f, 90f, -55f, -37f, 7f);
            plain = Mathf.Lerp(plain, 0f, bank);

            // 外圈丘陵
            float d = Mathf.Sqrt(x * x + (z - 20f) * (z - 20f));
            float ring = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(96f, 170f, d));
            float h1 = Mathf.PerlinNoise(x * 0.011f + 51f, z * 0.011f + 23f);
            float h2 = 1f - Mathf.Abs(Mathf.PerlinNoise(x * 0.021f + 71f, z * 0.021f + 37f) * 2f - 1f);
            float hills = ring * (5f + h1 * 10f + h2 * h2 * 22f);

            float city = LiRect(x, z, LiX0, LiX1, LiZ0, LiZ1, 3.5f) * LiCityFloor;
            float wall = LiWallMask(x, z) * LiWallRise * (1f - LiGateOpen(x, z));
            // 台地南坡放宽到 4.0 m：2.0 m 的过渡等于 50° 陡坎，
            // CharacterController 的 slopeLimit 是 50°，走到坡脚就上不去。
            // 4.0 m 时最陡处约 42°，人走得上去；台沿退到 z=22，
            // 让城内运河的岸边（软边到 z≈18.7）先收完，两个起伏不叠在一起。
            float mo = LiRect(x, z, LiMoX0, LiMoX1, LiMoZ0, LiMoZ1, 4.0f) * LiMojiaoRise;

            float low = LiRect(x, z, -84f, 84f, -60f, 2f, 10f) * -0.08f;
            float canal = LiCanalMask(x, z) * -1.15f;
            float res = LiReservoirMask(x, z) * -1.60f;

            // 塘山长堤：北部一道长土垄
            float levee = LiRect(x, z, -96f, 96f, 82f, 94f, 5f) * 3.4f;
            levee = Mathf.Max(levee, LiRect(x, z, -96f, -60f, 52f, 62f, 4f) * 2.6f);

            float h = plain + hills + city + wall + mo + low + canal + res + levee;

            // 探沟：把这一段城墙整平到固定高程，露出堆筑层理
            if (trench) h = Mathf.Lerp(h, LiTrenchFloor, LiTrenchMask(x, z));
            return h;
        }

        static Color LiangzhuColor(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.09f + 3f, z * 0.09f + 11f);
            float n2 = Mathf.PerlinNoise(x * 0.31f + 41f, z * 0.31f + 7f);

            Color wet = new Color(0.30f, 0.38f, 0.24f);   // 湿地苔绿
            Color dry = new Color(0.53f, 0.50f, 0.32f);   // 干草金
            Color c = Color.Lerp(wet, dry, Mathf.Clamp01(n * 0.6f + n2 * 0.3f));

            // 河道与低地：更暗更青
            float canal = LiCanalMask(x, z);
            c = Color.Lerp(c, new Color(0.21f, 0.29f, 0.20f), canal * 0.60f);
            float res = LiReservoirMask(x, z);
            c = Color.Lerp(c, new Color(0.23f, 0.30f, 0.24f), res * 0.50f);

            // 城墙土垄：夯土裸露
            float w = LiWallMask(x, z) * (1f - LiGateOpen(x, z));
            c = Color.Lerp(c, new Color(0.47f, 0.41f, 0.28f), Mathf.Clamp01(w * 1.6f));

            // 城内：踩踏土与草交替
            float city = LiRect(x, z, LiX0, LiX1, LiZ0, LiZ1, 3.5f);
            c = Color.Lerp(c, new Color(0.43f, 0.39f, 0.26f), city * 0.42f);

            // 莫角山台地顶：大片草地
            float mo = LiRect(x, z, LiMoX0, LiMoX1, LiMoZ0, LiMoZ1, 2f);
            c = Color.Lerp(c, new Color(0.39f, 0.43f, 0.24f), mo * 0.65f);

            // 夯土路：南向主路 + 城内南北轴线
            if (Mathf.Abs(x) < 2.9f && z > -62f && z < 6f) c = new Color(0.55f, 0.48f, 0.34f);
            if (Mathf.Abs(x) < 2.4f && z >= 6f && z < 22f) c = new Color(0.53f, 0.47f, 0.33f);

            // 外圈丘陵偏青灰，拉出空间层次
            float d = Mathf.Sqrt(x * x + (z - 20f) * (z - 20f));
            float far = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(90f, 165f, d));
            c = Color.Lerp(c, new Color(0.24f, 0.30f, 0.28f), far * 0.55f);

            return c;
        }

        // ================================================================ 环境

        public static void SetupLiangzhuEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(new Color(0.60f, 0.70f, 0.78f));
            RenderSettings.ambientEquatorColor = Lin(new Color(0.52f, 0.53f, 0.42f));
            RenderSettings.ambientGroundColor = Lin(new Color(0.32f, 0.31f, 0.23f));
            RenderSettings.ambientIntensity = 1.00f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0082f;
            RenderSettings.fogColor = Lin(Color.Lerp(HuaxiaPalette.DuskFog, HuaxiaPalette.PaperWarm, 0.55f));

            // 晨雾：天上暖下冷，接近良渚湿地清晨的湿气
            RenderSettings.skybox = SkyMat("Sky_Liangzhu",
                new Color(0.63f, 0.73f, 0.80f), new Color(0.50f, 0.47f, 0.36f), 0.85f, 0.70f, 0.62f);
            DynamicGI.UpdateEnvironment();

            QualitySettings.shadowDistance = 130f;
        }

        // ================================================================ 主构建

        public static void BuildLiangzhuScene()
        {
            LoadFonts();
            EnsureFolder(MatDir);
            EnsureFolder(SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLiangzhuEnvironment();
            // 良渚：水乡、稻田、玉器。用 Kevin MacLeod《Ripples》——独奏古筝，
            // 57 BPM，无低频持续层（60 Hz 以下能量 ≈ 0），不会再有「诡异」的底噪。
            AddSceneAmbient("amb_liangzhu_music", 0.45f);

            var sunGO = new GameObject("MorningSun", typeof(Light));
            // 光从镜头左后 35° 打过来：南立面有半调，东侧山墙落影，
            // 影子朝画面纵深里投，稻田才有「一道道」的层次。原来的 150° 把整个聚落压成了剪影。
            sunGO.transform.rotation = Quaternion.Euler(30f, 38f, 0f);
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Lin(new Color(1f, 0.93f, 0.80f));
            sun.intensity = 1.70f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.90f;
            sun.shadowBias = 0.045f;
            RenderSettings.sun = sun;

            // 冷色补光：湿地水面与背光面的环境色
            var fillGO = new GameObject("SkyFill", typeof(Light));
            fillGO.transform.rotation = Quaternion.Euler(30f, 218f, 0f);   // 与主光相对：补北墙与东墙
            var fill = fillGO.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Lin(new Color(0.54f, 0.68f, 0.86f));
            fill.intensity = 0.72f;
            fill.shadows = LightShadows.None;

            var root = new GameObject("Liangzhu").transform;

            // ------------------------------------------------ 材质
            var earthMat = MatAs("LiEarth", new Color(0.36f, 0.31f, 0.22f), "earth", 0f, 0.08f);
            var mudMat = MatAs("LiMud", new Color(0.35f, 0.31f, 0.22f), "earth", 0f, 0.06f);
            var roadMat = MatAs("LiRoadClay", new Color(0.55f, 0.48f, 0.34f), "earth", 0f, 0.10f);
            var rammedMat = MatAs("LiRammedEarth", new Color(0.47f, 0.41f, 0.28f), "earth", 0f, 0.07f);
            var grassMat = MatAs("LiGrass", new Color(0.31f, 0.40f, 0.21f), "foliage", 0f, 0.10f);
            var dryGrassMat = MatAs("LiGrassDry", new Color(0.48f, 0.46f, 0.26f), "foliage", 0f, 0.10f);
            var woodMat = MatAs("LiWood", new Color(0.29f, 0.22f, 0.15f), "wood", 0f, 0.10f);
            var woodLightMat = MatAs("LiWoodLight", new Color(0.42f, 0.33f, 0.22f), "wood", 0f, 0.12f);
            var thatchMat = MatAs("LiThatch", new Color(0.53f, 0.43f, 0.27f), "foliage", 0f, 0.14f);
            var reedMat = MatAs("LiReed", new Color(0.44f, 0.45f, 0.27f), "foliage", 0f, 0.12f);
            var riceMat = MatAs("LiRice", new Color(0.42f, 0.52f, 0.26f), "foliage", 0f, 0.10f);
            var stoneMat = MatAs("LiStone", new Color(0.47f, 0.46f, 0.42f), "stone", 0f, 0.14f);
            var potteryMat = MatAs("LiPottery", new Color(0.38f, 0.30f, 0.23f), "stone", 0f, 0.16f);
            // 玉：青白色透闪石玉，去饱和、近乎灰白，靠明度差而非色相反差区分节与槽
            var jadeMat = MatAs("LiJade", new Color(0.74f, 0.76f, 0.70f), "stone", 0.05f, 0.44f);
            var jadeDarkMat = MatAs("LiJadeDark", new Color(0.55f, 0.60f, 0.54f), "stone", 0.04f, 0.32f);
            var clothMat = MatAs("LiCloth", new Color(0.62f, 0.52f, 0.36f), "plaster", 0f, 0.12f);
            var leafMat = MatAs("LiLeaf", new Color(0.33f, 0.41f, 0.22f), "foliage", 0f, 0.10f);
            var pineMat = MatAs("LiLeafDark", new Color(0.24f, 0.33f, 0.20f), "foliage", 0f, 0.10f);
            var autumnMat = MatAs("LiLeafAutumn", new Color(0.52f, 0.42f, 0.20f), "foliage", 0f, 0.10f);
            var barkMat = MatAs("LiBark", new Color(0.25f, 0.20f, 0.15f), "wood", 0f, 0.10f);
            var waterMat = WaterMat("LiWater", new Color(0.27f, 0.41f, 0.37f, 0.62f), 0.9f);
            waterMat.SetFloat("_AlphaBase", 0.95f);
            waterMat.SetColor("_DeepColor", Lin(new Color(0.15f, 0.26f, 0.27f, 0.94f)));
            waterMat.SetColor("_ShallowColor", Lin(new Color(0.24f, 0.35f, 0.33f, 0.92f)));
            waterMat.SetColor("_SkyTint", Lin(new Color(0.86f, 0.90f, 0.86f)) * 0.34f);
            var paddyWaterMat = WaterMat("LiPaddyWater", new Color(0.36f, 0.47f, 0.42f, 0.66f), 0.95f);
            // 稻田水是"浑浊的浅泥水"，不是深潭：偏黄绿、偏亮，否则近景会读成沥青
            paddyWaterMat.SetFloat("_AlphaBase", 0.90f);
            paddyWaterMat.SetColor("_DeepColor", Lin(new Color(0.33f, 0.37f, 0.27f, 0.92f)));
            paddyWaterMat.SetColor("_ShallowColor", Lin(new Color(0.39f, 0.44f, 0.30f, 0.91f)));
            paddyWaterMat.SetColor("_SkyTint", Lin(new Color(0.80f, 0.86f, 0.78f)) * 0.40f);
            // 田里的水几乎不动：把波高和波长都收小，温水面才不像一块刷了漆的板
            paddyWaterMat.SetFloat("_WaveScale", 1.9f);
            paddyWaterMat.SetFloat("_WaveStrength", 0.16f);

            // ------------------------------------------------ 地形
            var terrainMesh = MeshFactory.Terrain(300f, 200, LiangzhuHeight, LiangzhuColor);
            MeshObject("Terrain", root, Vector3.zero, terrainMesh, TerrainMat(), true, false);

            // ------------------------------------------------ 南向夯土主路
            // 路板首尾相接：原来间距 2.55、板深 2.45，路上每隔两米多就露出一道
            // 10 cm 的缝，走起来像「断成好几截」。夯土路本来就该是连续的。
            for (int i = 0; i < 26; i++)
            {
                float z = -61f + i * 2.55f;
                if (z > -47.5f && z < -40.5f) continue;   // 河道处由木桥接续
                Box("Road_" + i, root, new Vector3((Rnd(i * 7) - 0.5f) * 0.05f, -0.035f, z),
                    new Vector3(4.3f, 0.10f, 2.55f), roadMat, false);
            }
            Box("Road_Plaza", root, new Vector3(0f, -0.035f, -34f), new Vector3(9.5f, 0.10f, 12f), roadMat, false);

            // 车辙与散石：避免入口大道是一整块平色板
            for (int i = 0; i < 2; i++)
            {
                Box("Rut_" + i, root, new Vector3(i == 0 ? -1.15f : 1.15f, 0.014f, -38.6f),
                    new Vector3(0.22f, 0.02f, 11.4f), mudMat, false);
            }
            Box("Rut_Fork", root, new Vector3(0f, 0.014f, -31.2f), new Vector3(0.20f, 0.02f, 4.0f), mudMat, false);
            for (int i = 0; i < 10; i++)
            {
                float a = Rnd(i * 13 + 5);
                float b = Rnd(i * 29 + 11);
                Box("RoadStone_" + i, root,
                    new Vector3((a - 0.5f) * 7.4f, 0.028f, -40.5f + b * 17f),
                    new Vector3(0.24f + a * 0.22f, 0.05f, 0.20f + b * 0.28f), stoneMat, false);
            }

            // ------------------------------------------------ 玉琮遗址标识（入口主体）
            SiteMarker(root, new Vector3(8.2f, 0f, -36.5f), stoneMat, jadeMat, jadeDarkMat, rammedMat);

            // ------------------------------------------------ 湿地河道与木桥
            WaterBand(root, "Canal_S", new Vector3(0f, 0f, -44f), 164f, 7.0f, LiWaterS, waterMat, mudMat, 0f, true);
            // 桥要横跨整条河，而且两头必须落在平地上。
            // 河道 z 从 −47 到 −41，但岸边还有 2.2 m 的软过渡（−49.2 就开始下陷）。
            // 桥长 8.2 时两头停在 −48.1 / −39.9，脚下已经是 0.58 m 深的岸坡，
            // 桥面却还在 0.10 m——走到桥头就被 0.68 m 的立面卡住，正是「卡在缝里」。
            // 拉到 10.6，两头落在 −49.3 / −38.7 的平地上，高差只剩一板厚（0.19 m）。
            DeckBridge(root, new Vector3(0f, 0f, -44f), 4.6f, 10.6f, LiWaterS, woodMat, woodLightMat, 0.06f);

            // ------------------------------------------------ 水稻田
            Paddy(root, new Vector3(-26f, 0f, -35f), 13f, 12f, mudMat, paddyWaterMat, riceMat, dryGrassMat);
            Paddy(root, new Vector3(-11.5f, 0f, -35f), 13f, 12f, mudMat, paddyWaterMat, riceMat, dryGrassMat);
            Paddy(root, new Vector3(26f, 0f, -35f), 13f, 12f, mudMat, paddyWaterMat, riceMat, dryGrassMat);
            Paddy(root, new Vector3(11.5f, 0f, -35f), 13f, 12f, mudMat, paddyWaterMat, riceMat, dryGrassMat);
            Paddy(root, new Vector3(-26f, 0f, -19f), 13f, 11.5f, mudMat, paddyWaterMat, riceMat, dryGrassMat);
            Paddy(root, new Vector3(-11.5f, 0f, -19f), 13f, 11.5f, mudMat, paddyWaterMat, riceMat, dryGrassMat);
            Paddy(root, new Vector3(26f, 0f, -19f), 13f, 11.5f, mudMat, paddyWaterMat, riceMat, dryGrassMat);
            Paddy(root, new Vector3(11.5f, 0f, -19f), 13f, 11.5f, mudMat, paddyWaterMat, riceMat, dryGrassMat);

            // 田埂小路 + 灌溉沟
            Box("Dike_E", root, new Vector3(11.5f, 0.12f, -27f), new Vector3(22f, 0.16f, 1.5f), roadMat, false);
            Box("Dike_W", root, new Vector3(-11.5f, 0.12f, -27f), new Vector3(22f, 0.16f, 1.5f), roadMat, false);
            WaterBand(root, "Ditch_W", new Vector3(-19f, 0f, -27f), 30f, 1.5f, 0.10f, waterMat, mudMat, 90f, false);

            // ------------------------------------------------ 干栏式聚落
            StiltHouse(root, new Vector3(-16.5f, 0f, -9f), 16f, 1.0f, woodMat, woodLightMat, clothMat, thatchMat, 3);
            StiltHouse(root, new Vector3(-27f, 0f, -2f), 34f, 1.05f, woodMat, woodLightMat, rammedMat, thatchMat, 7);
            StiltHouse(root, new Vector3(-11f, 0f, -1f), -10f, 0.92f, woodMat, woodLightMat, clothMat, thatchMat, 11);
            StiltHouse(root, new Vector3(15.5f, 0f, -8f), -18f, 1.02f, woodMat, woodLightMat, clothMat, thatchMat, 5);
            StiltHouse(root, new Vector3(26f, 0f, 0f), -32f, 0.95f, woodMat, woodLightMat, rammedMat, thatchMat, 13);
            // 这一栋原本压在探沟上，移到城门外西侧空地
            StiltHouse(root, new Vector3(3.5f, 0f, -9.5f), 24f, 0.88f, woodMat, woodLightMat, clothMat, thatchMat, 17);
            StiltHouse(root, new Vector3(-33f, 0f, -14f), 58f, 0.9f, woodMat, woodLightMat, rammedMat, thatchMat, 19);
            StiltHouse(root, new Vector3(34f, 0f, -12f), -56f, 0.9f, woodMat, woodLightMat, clothMat, thatchMat, 23);

            // 聚落陈设
            FirePit(root, new Vector3(-21f, 0f, -5.5f), stoneMat, woodMat);
            FirePit(root, new Vector3(20.5f, 0f, -4.5f), stoneMat, woodMat);
            DryingRack(root, new Vector3(-8f, 0f, -6f), 8f, woodMat, clothMat, reedMat);
            DryingRack(root, new Vector3(23f, 0f, -7.5f), -14f, woodMat, clothMat, reedMat);
            PotteryCluster(root, new Vector3(-18.6f, 0f, -5.6f), potteryMat, 41);
            PotteryCluster(root, new Vector3(17.4f, 0f, -4.4f), potteryMat, 43);
            PotteryCluster(root, new Vector3(-13.4f, 0f, -1.6f), potteryMat, 47);
            FirewoodStack(root, new Vector3(-24.5f, 0f, -8.5f), woodMat, barkMat);
            FirewoodStack(root, new Vector3(14.2f, 0f, -11.5f), woodMat, barkMat);
            WoodFence(root, new Vector3(-6.4f, 0f, -3f), 10f, 0f, woodMat, woodLightMat);
            WoodFence(root, new Vector3(6.6f, 0f, -3f), 10f, 0f, woodMat, woodLightMat);
            WoodFence(root, new Vector3(-30.5f, 0f, -8f), 12f, 72f, woodMat, woodLightMat);

            // 独木舟
            Dugout(root, new Vector3(-30f, LiWaterS + 0.02f, -44f), 6f, woodMat);

            // 河道两岸的芦苇与岸草（近景有东西压边，水面才不像一块平板）
            var strawMat = MatAs("LiStraw", new Color(0.66f, 0.58f, 0.36f), "grass", 0f, 0.1f);
            for (int i = 0; i < 84; i++)
            {
                float x = -33.5f + i * 0.82f;
                if (Mathf.Abs(x) < 5.0f) continue;
                if (Mathf.Abs(Mathf.Abs(x) - 21f) < 4.6f) continue;
                float z = (i % 2 == 0) ? 10.1f - Rnd(i * 7) * 0.75f : 18.9f + Rnd(i * 11) * 0.95f;
                ReedCluster(root, new Vector3(x, LiangzhuHeight(x, z), z),
                    (i % 2 == 0) ? strawMat : ((i % 5 == 0) ? dryGrassMat : grassMat),
                    0.9f + Rnd(i * 13) * 0.55f, 300 + i);
            }
            for (int i = 0; i < 20; i++)
            {
                float x = -30f + Rnd(i * 29) * 60f;
                if (Mathf.Abs(x) < 5.5f || Mathf.Abs(Mathf.Abs(x) - 21f) < 5f) continue;
                float z = (i % 2 == 0) ? 9.4f + Rnd(i * 17) * 1.2f : 19.4f + Rnd(i * 23) * 1.6f;
                GrassTuft(root, new Vector3(x, LiangzhuHeight(x, z), z),
                    grassMat, 0.8f + Rnd(i * 31) * 0.6f, 400 + i);
            }

            // 城外聚落组团：把密度铺开，让古城读成"有人的聚落"而不是布景
            Hamlet(root, new Vector3(-45f, 0f, -14f), 5, 8.4f, 12f, woodMat, woodLightMat,
                clothMat, thatchMat, potteryMat, clothMat, reedMat, roadMat, barkMat, 31);
            Hamlet(root, new Vector3(-47f, 0f, 16f), 4, 7.8f, -22f, woodMat, woodLightMat,
                rammedMat, thatchMat, potteryMat, clothMat, reedMat, roadMat, barkMat, 37);
            Hamlet(root, new Vector3(46f, 0f, -16f), 5, 8.6f, -70f, woodMat, woodLightMat,
                clothMat, thatchMat, potteryMat, clothMat, reedMat, roadMat, barkMat, 41);
            Hamlet(root, new Vector3(47f, 0f, 14f), 4, 7.6f, 158f, woodMat, woodLightMat,
                rammedMat, thatchMat, potteryMat, clothMat, reedMat, roadMat, barkMat, 43);
            Hamlet(root, new Vector3(24f, 0f, -54f), 4, 8.0f, 40f, woodMat, woodLightMat,
                clothMat, thatchMat, potteryMat, clothMat, reedMat, roadMat, barkMat, 47);

            // 入口大路两侧地被（路面与稻田之间的裸土带）
            for (int i = 0; i < 150; i++)
            {
                float z = -42.5f + Rnd(i * 19 + 3) * 28f;
                float side = (i % 2 == 0) ? 1f : -1f;
                float x = side * (3.30f + Rnd(i * 23 + 7) * 1.35f);
                GrassTuft(root, new Vector3(x, LiangzhuHeight(x, z), z),
                    (i % 4 == 0) ? dryGrassMat : grassMat, 0.7f + Rnd(i * 37) * 0.5f, 620 + i);
            }

            // ------------------------------------------------ 城墙与水门
            // 土垄由地形生成；这里补城门、水门的木构
            GateHouse(root, new Vector3(0f, 0f, LiZ0), woodMat, woodLightMat, thatchMat, rammedMat);
            WaterGate(root, new Vector3(21f, 0f, LiZ0), woodMat, woodLightMat, rammedMat, 1f);
            WaterGate(root, new Vector3(-21f, 0f, LiZ0), woodMat, woodLightMat, rammedMat, -1f);
            WaterGate(root, new Vector3(LiX1, 0f, 32f), woodMat, woodLightMat, rammedMat, 1f, true);
            WaterGate(root, new Vector3(LiX0, 0f, 32f), woodMat, woodLightMat, rammedMat, -1f, true);

            // 南城墙考古探沟（剖面展示棚）
            CityWallSection(root, new Vector3(13.5f, 0f, 0f), woodMat, woodLightMat, thatchMat,
                earthMat, rammedMat, roadMat, stoneMat, dryGrassMat);

            // 城内水道 + 木栈桥
            // 只保留东西向这条。原来还有一条沿主轴（x=0）的纵向水道，从 z=−20 一直
            // 铺到 40，正压在进城的路上，水面高出路面 0.8 m，地形又被它挖低 1.15 m。
            // 陆门到木桥是唯一的入城通道，不能同时当水道，已删。
            WaterBand(root, "Canal_City", new Vector3(0f, 0f, 14.5f), 66f, 6.0f, LiWaterC, waterMat, mudMat, 0f, false);
            // 桥面与城内地面齐平（原来高 0.18 m，像一道门槛）；桥长 7.6，
            // 两头落在运河软岸之外，落差只剩一板厚。
            DeckBridge(root, new Vector3(0f, 0f, 14.5f), 7.6f, 7.6f, LiWaterC, woodMat, woodLightMat,
                LiCityFloor - 0.02f);

            // 城内夯土路（南北轴线）：只铺运河软岸以外的平地，
            // 否则路板会悬在岸坡上空 0.6 m。
            for (int i = 0; i < 5; i++)
            {
                float z = 6.6f + i * 2.4f;
                if (z > 10.1f && z < 18.4f) continue;
                // 板面压到 1.10 m（与城内地面齐平）：原来板顶 1.20 m，
                // 人走在 1.06 m 的地形上，路板像一层 14 cm 高的浮台。
                Box("RoadCity_" + i, root, new Vector3(0f, LiCityFloor - 0.05f, z),
                    new Vector3(3.6f, 0.10f, 2.4f), roadMat, false);
            }

            // ------------------------------------------------ 莫角山台地（宫殿区）
            // 台顶的路从坡沿（z=22）往里铺，原来从 15.5 起就悬在坡上空 2 m。
            Box("Mojiao_Walk", root, new Vector3(0f, LiMojiaoTop + 0.06f, 27.0f), new Vector3(4.2f, 0.12f, 10f), roadMat, false);
            // 坡道木栈：贴着地形铺，越走越高。原来的 11 块高度是反的（越往北越低），
            // 第一块浮在坡面上方 2 m，最后一块埋进台地里。
            for (int i = 0; i < 13; i++)
            {
                float rz = 18.4f + i * 0.30f;
                float rh = LiangzhuHeight(0f, rz);
                Box("Mojiao_Ramp_" + i, root, new Vector3(0f, rh + 0.04f, rz),
                    new Vector3(4.4f, 0.10f, 0.34f), rammedMat, false);
            }

            PostHoleGrid(root, new Vector3(12.5f, LiMojiaoTop, 25.5f), 4, 3, 4.6f, 4.2f, stoneMat, woodMat);
            // 朝向让开敞的南面正对台地坡道，前庭落在台地之内
            GrandHall(root, new Vector3(0f, LiMojiaoTop, 38f), 0f, woodMat, woodLightMat, rammedMat, thatchMat, stoneMat, potteryMat);

            // 瑶山祭坛（城外东北的高等级祭祀与墓葬遗迹）
            AltarTerrace(root, new Vector3(58f, 0f, 40f), rammedMat, earthMat, grassMat, dryGrassMat,
                stoneMat, jadeMat, jadeDarkMat, woodMat);

            // 钟家港木构栈桥码头
            Dock(root, new Vector3(-17.5f, 0f, 9.4f), woodMat, woodLightMat, barkMat,
                potteryMat, reedMat, clothMat);

            // ------------------------------------------------ 反山墓地
            BurialMound(root, new Vector3(-27f, 0f, 28f), earthMat, grassMat, stoneMat, jadeMat, jadeDarkMat);

            // ------------------------------------------------ 外围水利系统
            WaterBand(root, "Reservoir", new Vector3(0f, 0f, 72f), 156f, 17f, LiWaterR, waterMat, mudMat, 0f, true);
            Dam(root, new Vector3(-70f, 0f, 71.5f), 0f, 34f, rammedMat, woodMat, dryGrassMat);
            Dam(root, new Vector3(70f, 0f, 71.5f), 0f, 34f, rammedMat, woodMat, dryGrassMat);
            Dam(root, new Vector3(0f, 0f, 62.5f), 0f, 104f, rammedMat, woodMat, dryGrassMat);

            // ------------------------------------------------ 植被
            BuildWetlandFlora(root, leafMat, pineMat, autumnMat, barkMat, reedMat);

            // ------------------------------------------------ 远山
            var nearMat = MatAs("LiRidgeNear", new Color(0.30f, 0.38f, 0.33f), "foliage", 0f, 0.05f);
            var midMat = MatAs("LiRidgeMid", new Color(0.36f, 0.44f, 0.43f), "foliage", 0f, 0.05f);
            var farMat = MatAs("LiRidgeFar", new Color(0.52f, 0.58f, 0.58f), "foliage", 0f, 0.05f);

            MeshObject("Ridge_N1", root, new Vector3(-60f, 0f, 158f),
                MeshFactory.MountainRidge(400f, 46f, 6, 13, 24f), nearMat, false);
            MeshObject("Ridge_N2", root, new Vector3(70f, 0f, 176f),
                MeshFactory.MountainRidge(460f, 58f, 7, 8, 30f), midMat, false);
            MeshObject("Ridge_N3", root, new Vector3(-10f, 0f, 226f),
                MeshFactory.MountainRidge(620f, 78f, 8, 3, 40f), farMat, false);
            MeshObject("Ridge_E", root, new Vector3(150f, 0f, 40f),
                MeshFactory.MountainRidge(380f, 50f, 6, 29, 26f), midMat, false).transform.localRotation =
                Quaternion.Euler(0f, 90f, 0f);
            MeshObject("Ridge_W", root, new Vector3(-150f, 0f, 34f),
                MeshFactory.MountainRidge(380f, 52f, 6, 31, 26f), midMat, false).transform.localRotation =
                Quaternion.Euler(0f, -90f, 0f);
            MeshObject("Ridge_S", root, new Vector3(0f, 0f, -150f),
                MeshFactory.MountainRidge(520f, 40f, 7, 21, 30f), farMat, false).transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);

            // 章节标识改由城门匾额承担，避免大块文字悬在入口视线正中

            // ------------------------------------------------ 线索点
            // 按参观顺序编号：① 城墙与水门 → ② 莫角山 → ③ 反山玉器 → 合卷。
            // ① 立在陆城门西侧的内路肩：穿过城门往里走两米，光柱就在左手边，
            //   回身是土垄城墙，西侧五米就是西水门，文字和眼前的东西对得上。
            //   原来摆在广场北口（离城墙三十五米，抬头是稻田），后来挪到门内东侧
            //   x=2.6、z=10——那里离东门柱只有八厘米，光柱整根埋进门柱里看不见。
            // ③ 从台沿退进五米：台沿正好在树冠底下，光柱扎进去就看不见了。
            // 每一处按 E 打开的是 HeritageSpots 里的图文与语音（有影像、没有三维展台）。
            var ptWater = CreateInfoPoint(root, "Water", new Vector3(-2.0f, LiangzhuHeight(-2.0f, 8.4f) + 0.05f, 8.4f),
                "城墙与水门", "第一站 · 城市与公共工程",
                "良渚古城不是一圈墙包着房子那么简单：城里有河，城外有坝，城墙在水经过的地方留了口子——那就是水门。\n\n" +
                "转过身就是这道墙：不是薄薄一堵，是一条很宽的土垄，人能在上面走。船从水门进出，" +
                "城里人运东西、走动，大半走水路。\n\n" +
                "修这样的墙、挖这样的河、管这样的水，不是一户人家干得了的。谁来定、怎么调人、" +
                "什么时候开工——这些才是考古学家想从土里读出来的。\n\n" +
                "说明：墙的宽度、水门的数量照公开资料摆，具体尺寸是示意。",
                "water", new Color(0.58f, 0.76f, 0.74f), SourceTier.Confirmed, "水门闸的具体形制为示意",
                heritageId: "spot_lz_water", number: "①");

            var ptMojiao = CreateInfoPoint(root, "Mojiao", new Vector3(6.5f, LiMojiaoTop, 22.5f),
                "莫角山台地", "第二站 · 高台与权力空间",
                "莫角山是一座人工堆起来的大土台，比城里的地面高出一大截。台上有成组的大房子基址，" +
                "一般认为这里是良渚的宫殿区。\n\n" +
                "上台要爬坡道、走木栈道。台面上的柱洞排得很规整，说明这些房子是一起规划过的，" +
                "不是谁想盖就盖。\n\n" +
                "把最重要的房子放在最高的地方——不用写字，人一进来就知道该往哪儿看、该听谁的。\n\n" +
                "说明：台上的房子是示意复原，具体是什么样，学界还在讨论。",
                "mojiao", new Color(0.86f, 0.80f, 0.60f), SourceTier.Confirmed, null,
                heritageId: "spot_lz_mojiao", number: "②");

            // 反山是一堆叠起来的土丘，顶上摆着玉琮。点位不再放在丘顶（那里既走不上去，
            // 从台地上也只能看见一个土包），也不再贴着台沿（台沿在树冠底下），
            // 改立在莫角山西侧的开阔台面上：转过身去，玉琮与丘形都在眼前。
            var ptJade = CreateInfoPoint(root, "Jade", new Vector3(-14f, LiMojiaoTop + 0.02f, 25f),
                "反山墓地与玉器", "第三站 · 器物与考古解释",
                "反山是城里的一处重要墓地，出了很多玉器。玉琮、玉钺是良渚最有名的两样。\n\n" +
                "你看玉琮：外方内圆，身上一节一节，刻着神人兽面纹。做这样一件东西，要选料、开料、" +
                "慢慢磨——一个人一年也未必磨得完。\n\n" +
                "玉只出在少数几座墓里，别的墓没有。这一点很有说服力：做玉的工夫、用玉的资格，" +
                "都攥在少数人手里。\n\n" +
                "但光凭玉器还推不出整个社会怎么分级，得把墓葬、住址、作坊放在一起看。\n\n" +
                "说明：玉器是示意模型，纹样只给个样子，不代表原件的细节。",
                "jade", new Color(0.56f, 0.72f, 0.62f), SourceTier.Inferred, "神人兽面纹的含义有多种解释，此不给结论",
                heritageId: "spot_lz_jade", number: "③");

            // 合上这一卷：三处看完，沿台地北行到台边，按 E 就回序章的圆坛
            var ptFarewell = CreateInfoPoint(root, "Farewell", new Vector3(-19.5f, LiMojiaoTop + 0.02f, 44f),
                "合上《河山册》第一卷", "良渚 · 话尽于此",
                "水、田、城、玉，四样东西说的是同一件事：五千年前，这里已经不只是村落，而是一座城。",
                null, new Color(0.88f, 0.78f, 0.54f), SourceTier.Confirmed, null,
                number: "终", endScene: "Archive", endSpawnId: "hub_center",
                endLine: "水与玉的一卷，到此合上。回圆坛去，还有两处等着。");

            var iWater = ptWater.GetComponent<Interactable>();
            var iMojiao = ptMojiao.GetComponent<Interactable>();
            var iJade = ptJade.GetComponent<Interactable>();
            if (iWater != null) iWater.leadTo = ptMojiao.transform;
            if (iMojiao != null) iMojiao.leadTo = ptJade.transform;
            if (iJade != null) iJade.leadTo = ptFarewell.transform;


            // 火塘与祭祀火堆是点光源，其 ForwardAdd 通道不乘 alpha，打在半透明水面上会叠出
            // 与屏幕对齐的浅色矩形；把水面层从点光源的 cullingMask 中排除，白天几乎看不出差别。
            foreach (var pl in Object.FindObjectsOfType<Light>())
                if (pl.type == LightType.Point)
                    pl.cullingMask &= ~(1 << LiWaterLayer);

            // ------------------------------------------------ 玩家、视角、UI
            var player = CreatePlayer(new Vector3(0f, 0.05f, -56f), 0f);
            ApplyGrade(player, "liangzhu");

            // 来路边的界石：想回去就站到石前按 E，不必把这一卷走完
            // 立在夯土大道的西缘：再往外半步就是稻田的水面
            CreateReturnStone(root, "Liangzhu", new Vector3(-1.5f, LiangzhuHeight(-1.5f, -52.5f), -52.5f),
                100f, "良渚古城", "liangzhu", stoneMat, woodMat);

            var startSpawn = Empty("Spawn_from_hub", root, new Vector3(0f, 0.05f, -56f));
            startSpawn.AddComponent<SpawnPoint>().id = "from_hub";
            var cam = player.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;

            var aerial = CreateAerialCamera(new Vector3(0f, 78f, -70f), new Vector3(44f, 0f, 0f));
            var bird = player.AddComponent<BirdEyeView>();
            bird.playerCamera = cam;
            bird.aerialCamera = aerial;
            bird.controller = player.GetComponent<DesktopPlayerController>();
            bird.aerialPosition = new Vector3(0f, 78f, -70f);
            bird.aerialEuler = new Vector3(44f, 0f, 0f);

            CreateHUD("WASD 移动 · Shift 奔跑 · 「、」跳跃 · E 互动 · Tab 取册子 · V 整体视角 · Q 合卷回山 · Esc 释放鼠标");

            var systems = new GameObject("Systems");
            var tasks = systems.AddComponent<TaskManager>();
            tasks.chapterId = "liangzhu";
            tasks.chapterLabel = "第一章 · 城与玉";
            tasks.chapterSubtitle = "良渚古城遗址";
            tasks.objectives = new[]
            {
                new Objective { id = "water", label = "① 看城墙、水门、河道怎么连成一套" },
                new Objective { id = "mojiao", label = "② 上莫角山，看台地上的房基" },
                new Objective { id = "jade", label = "③ 在反山墓地看那些玉器" },
            };
            tasks.introBody =
                "眼前是良渚。五千三百年前，这里是水乡：稻田铺在南边的湿地上，" +
                "一圈城墙把城围住，河道从八个水门里穿进穿出。\n\n" +
                "一共三处点位，按 ①②③ 的顺序走：先穿过南城门，门内左手边看夯土城墙与水门，" +
                "顺着水道爬上莫角山看台上的房基，再往西看反山的玉。" +
                "走到跟前的立牌前按 E，我把图、影和录音一起给你；读完照着我的手势往前，就是下一处。\n\n" +
                "三处走完再往北，是合卷的牌子——按 E 就回序章的圆坛。按 V 能飞起来看全貌。\n\n" +
                "这些几何和材质都是示意的，不是遗址当年的样子。";

            tasks.archiveTitle = "文明初兴：早期城市与社会组织";
            tasks.archiveBody =
                "现场发现\n" +
                "· 水田、灌溉沟、河道与干栏式住房在空间上紧密相邻，聚落依水而居。\n" +
                "· 城墙为宽厚土垄，在水道通过处留有水门；城外另有水坝与水库，构成水利系统。\n" +
                "· 莫角山为人工堆筑的高台，台上有规整柱网的大型建筑基址。\n" +
                "· 反山墓地随葬玉琮、玉璧、玉钺等玉器，制作耗工，集中于少数墓葬。\n" +
                "· 城墙剖面显示：墙基铺碎石，其上黄土分层堆筑，层间界面清晰，并夹有红烧土与陶片。\n" +
                "· 瑶山为三重方台，台上小墓成列排布、方向一致，随葬玉器。\n" +
                "· 城内河道旁有木构栈桥遗迹，河道堆积出土大量陶器与木器。\n\n" +
                "历史解释\n" +
                "良渚古城遗址位于长江下游，距今约5300—4300年。古城、水利系统与周围聚落共同反映出" +
                "稻作农业、资源管理与大型公共工程组织方面的能力。\n\n" +
                "关联线索\n" +
                "与秦陵、泉州两处遗址对照：良渚以水定型，秦陵以土与礼制定型，泉州以海路定型——三者都靠空间规划表达秩序，但属不同时期、不同性质的比较性认识，不构成直接发展关系。";
            tasks.returnScene = "Archive";

            CreateDust(root, new Vector3(0f, 3.5f, -10f), new Vector3(120f, 9f, 120f), 260, 8f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneDir + "/Liangzhu.unity");
            Debug.Log("[Huaxia] 良渚场景已保存");
        }

        // ================================================================ 构件

        // ================================================================ 城墙剖面

        /// <summary>
        /// 南城墙考古探沟：在土垄南坡切出凹口，露出碎石垫层、分层堆筑的黄土、
        /// 红烧土倒塌层与表土，并按遗址公园的展示方式加木构保护棚与木栈道。
        /// 层理数量与单层厚度为示意，不代表实测数据。
        /// </summary>
        /// <summary>
        /// 南城墙考古探沟：把土垄南坡切平，立起一段分层堆筑的城墙剖面，
        /// 外覆单坡木构保护棚、木栈道与说明牌。层数与单层厚度为示意，非实测数据。
        /// </summary>
        static void CityWallSection(Transform parent, Vector3 pos, Material wood, Material woodLight,
                                    Material thatch, Material earth, Material rammed, Material clay,
                                    Material stone, Material grass)
        {
            // 组内坐标的 ZF 通过该偏移落到世界 z = 6.15 的切面上
            var g = Empty("CityWallSection", parent, new Vector3(0f, 0f, -5.0f));

            const float XA = 9.2f, XB = 16.2f;
            const float mid = 12.7f;
            const float ZF = 11.15f;      // 剖面位置（朝南切开土垄处）

            float pad = LiangzhuHeight(mid, 3.6f);          // 已切平的探沟地面
            float top = LiangzhuHeight(mid, 8.0f, false);   // 墙体原顶（未切）
            float bot = pad - 0.35f;

            var burntMat = MatAs("LiBurntEarth", new Color(0.44f, 0.27f, 0.19f), "earth", 0f, 0.06f);
            var sherdMat = MatAs("LiSherd", new Color(0.41f, 0.33f, 0.24f), "stone", 0f, 0.12f);
            var paleMat = MatAs("LiPaleEarth", new Color(0.72f, 0.64f, 0.46f), "earth", 0f, 0.07f);
            var boardMat = MatAs("LiBoard", new Color(0.73f, 0.65f, 0.48f), "plaster", 0f, 0.10f);

            // 基槽 + 碎石垫层（良渚城墙底部铺垫石块）
            Box("Subgrade", g.transform, new Vector3(mid, bot + 0.12f, ZF + 0.75f),
                new Vector3(XB - XA + 0.4f, 0.46f, 2.10f), earth, false);
            Box("Bedding", g.transform, new Vector3(mid, bot + 0.36f, ZF + 0.62f),
                new Vector3(XB - XA, 0.34f, 1.80f), stone, false);
            for (int i = 0; i < 10; i++)
                Ball("BedStone_" + i, g.transform,
                    new Vector3(XA + 0.40f + i * 0.80f, bot + 0.40f, ZF - 0.60f), 0.16f, stone);

            // 黄土分层堆筑：色带交替 + 层缝，读出"一层层夯上去"的结构
            float y0 = bot + 0.54f;
            float y1 = top - 0.28f;
            const int layers = 11;
            for (int i = 0; i < layers; i++)
            {
                float t0 = Mathf.Lerp(y0, y1, i / (float)layers);
                float t1 = Mathf.Lerp(y0, y1, (i + 1) / (float)layers);
                Material m = (i % 3 == 1) ? paleMat : (i % 3 == 2 ? earth : rammed);
                Box("Layer_" + i, g.transform,
                    new Vector3(mid, (t0 + t1) * 0.5f, ZF + 0.62f),
                    new Vector3(XB - XA - (i % 2) * 0.14f, Mathf.Max(0.06f, t1 - t0 - 0.02f), 1.70f),
                    m, false);
                Box("Seam_" + i, g.transform, new Vector3(mid, t1 - 0.015f, ZF - 0.26f),
                    new Vector3(XB - XA - 0.10f, 0.030f, 0.10f), earth, false);
            }

            // 红烧土倒塌层 + 表土
            Box("Burnt", g.transform, new Vector3(mid, y1 + 0.14f, ZF + 0.62f),
                new Vector3(XB - XA - 0.16f, 0.28f, 1.66f), burntMat, false);
            Box("Topsoil", g.transform, new Vector3(mid, top - 0.10f, ZF + 0.62f),
                new Vector3(XB - XA, 0.22f, 1.70f), grass, false);

            // 剖面里嵌着的陶片与石块
            for (int i = 0; i < 16; i++)
            {
                float sx2 = XA + 0.45f + Rnd(i * 31) * (XB - XA - 0.9f);
                float sy2 = y0 + 0.12f + Rnd(i * 17) * (y1 - y0 - 0.30f);
                Box("Sherd_" + i, g.transform, new Vector3(sx2, sy2, ZF - 0.30f),
                    new Vector3(0.11f + Rnd(i * 3) * 0.09f, 0.05f + Rnd(i * 7) * 0.05f, 0.05f),
                    sherdMat, false);
            }

            // 剖面两侧的护壁木桩与横撑
            for (int side = 0; side < 2; side++)
            {
                float px = side == 0 ? XA - 0.16f : XB + 0.16f;
                for (int i = 0; i < 5; i++)
                    Cyl("WallPile_" + side + "_" + i, g.transform,
                        new Vector3(px, (bot + top) * 0.5f + 0.30f, ZF - 0.5f + i * 0.45f),
                        0.09f, top - bot + 0.9f, wood);
                for (int i = 0; i < 3; i++)
                    Box("WallBatten_" + side + "_" + i, g.transform,
                        new Vector3(px, bot + 0.68f + i * 0.86f, ZF + 0.10f),
                        new Vector3(0.09f, 0.13f, 1.70f), woodLight, false);
            }

            // 探沟排水沟
            Box("Drain", g.transform, new Vector3(mid, pad - 0.05f, ZF - 0.95f),
                new Vector3(XB - XA + 0.6f, 0.18f, 0.44f), earth, false);

            // 观景木栈道
            float deckY = pad + 0.16f;
            for (int i = 0; i < 23; i++)
                Box("Plank_" + i, g.transform,
                    new Vector3(XA + 0.18f + i * 0.352f, deckY, 8.75f),
                    new Vector3(0.33f, 0.09f, 3.80f), woodLight, false);
            for (int i = 0; i < 3; i++)
                Box("DeckBeam_" + i, g.transform,
                    new Vector3(mid, deckY - 0.13f, 7.30f + i * 1.42f),
                    new Vector3(XB - XA + 0.3f, 0.14f, 0.16f), wood, false);
            for (int i = 0; i < 6; i++)
                Cyl("DeckLeg_" + i, g.transform,
                    new Vector3(11.2f + (i % 3) * 2.7f, pad - 0.05f, i < 3 ? 7.35f : 10.05f),
                    0.10f, 0.46f, wood);

            // 探沟两侧的矮护栏（只到膝盖高，不遮挡剖面）
            for (int i = 0; i < 8; i++)
                Box("RailPost_" + i, g.transform,
                    new Vector3(XA + 0.25f + i * 1.10f, deckY + 0.20f, 6.30f),
                    new Vector3(0.09f, 0.40f, 0.09f), wood, false);
            Box("RailTop", g.transform, new Vector3(mid, deckY + 0.38f, 6.30f),
                new Vector3(XB - XA, 0.07f, 0.10f), woodLight, false);

            // 说明牌
            Box("SignPost", g.transform, new Vector3(XA - 1.35f, pad + 0.62f, 4.55f),
                new Vector3(0.11f, 1.30f, 0.11f), wood, false);
            Box("SignPost_B", g.transform, new Vector3(XA - 2.55f, pad + 0.62f, 4.55f),
                new Vector3(0.11f, 1.30f, 0.11f), wood, false);
            Box("SignBoard", g.transform, new Vector3(XA - 1.95f, pad + 1.10f, 4.55f),
                new Vector3(1.42f, 0.78f, 0.07f), boardMat, false);
            CreateTextPlate(g.transform, "城墙剖面\n夯土分层·碎石垫层",
                new Vector3(XA - 1.95f, pad + 1.10f, 4.50f), Vector3.zero,
                new Vector2(1.30f, 0.66f), 24, new Color(0.22f, 0.17f, 0.12f), title: true);

            // 探沟边缘的草与散落石块
            for (int i = 0; i < 12; i++)
                GrassTuft(g.transform,
                    new Vector3(XA - 0.9f + Rnd(i * 11) * (XB - XA + 1.8f), pad + 0.02f,
                                5.3f + Rnd(i * 19) * 0.9f),
                    grass, 0.8f + Rnd(i * 7) * 0.4f, 700 + i);
            for (int i = 0; i < 9; i++)
                Ball("LooseStone_" + i, g.transform,
                    new Vector3(XA + 0.6f + Rnd(i * 13) * (XB - XA - 1.2f), pad + 0.03f,
                                5.7f + Rnd(i * 23) * 1.1f),
                    0.11f + Rnd(i * 3) * 0.05f, stone);
        }

        // ================================================================ 瑶山祭坛

        /// <summary>
        /// 瑶山祭坛：三重逐级缩小的方形土台 + 砾石坎，台上两列共 12 座小墓。
        /// 台数、墓葬排列与砾石坎依据公开考古报道，尺度为示意。
        /// </summary>
        static void AltarTerrace(Transform parent, Vector3 pos, Material rammed, Material earth,
                                 Material grass, Material dryGrass, Material stone,
                                 Material jade, Material jadeDark, Material wood)
        {
            var g = Empty("AltarTerrace", parent, pos);
            float gy = LiangzhuHeight(pos.x, pos.z) - pos.y - 0.12f;
            var ashMat = MatAs("LiAsh", new Color(0.33f, 0.31f, 0.28f), "earth", 0f, 0.05f);
            var burntMat = MatAs("LiBurntEarth", new Color(0.44f, 0.27f, 0.19f), "earth", 0f, 0.06f);

            float[] side = { 23f, 15.6f, 9.8f };
            float[] height = { 1.32f, 1.06f, 0.82f };
            var paleMat = MatAs("LiPaleEarth", new Color(0.58f, 0.51f, 0.36f), "earth", 0f, 0.07f);
            var tierMats = new Material[] { rammed, earth, paleMat };
            float acc = 0f;
            for (int t = 0; t < 3; t++)
            {
                Box("Tier_" + t, g.transform, new Vector3(0f, gy + acc + height[t] * 0.5f, 0f),
                    new Vector3(side[t], height[t], side[t]), tierMats[t], true);
                // 顶面压一层浅色夯土，让每级台阶的边线读得出来
                Box("TierCap_" + t, g.transform, new Vector3(0f, gy + acc + height[t] - 0.05f, 0f),
                    new Vector3(side[t] + 0.14f, 0.12f, side[t] + 0.14f), dryGrass, false);
                acc += height[t];

                // 砾石坎（顶层与底层各一圈）
                if (t == 0 || t == 2)
                {
                    float half = side[t] * 0.5f - 0.35f;
                    int per = Mathf.RoundToInt(side[t] / 1.45f);
                    for (int i = 0; i < per; i++)
                    {
                        float o = -half + (i + 0.5f) * (half * 2f / per);
                        Box("Cant_S_" + t + "_" + i, g.transform,
                            new Vector3(o, gy + acc - 0.20f, -half), new Vector3(1.30f, 0.30f, 0.34f), stone, false);
                        Box("Cant_N_" + t + "_" + i, g.transform,
                            new Vector3(o, gy + acc - 0.20f, half), new Vector3(1.30f, 0.30f, 0.34f), stone, false);
                        Box("Cant_W_" + t + "_" + i, g.transform,
                            new Vector3(-half, gy + acc - 0.20f, o), new Vector3(0.34f, 0.30f, 1.30f), stone, false);
                        Box("Cant_E_" + t + "_" + i, g.transform,
                            new Vector3(half, gy + acc - 0.20f, o), new Vector3(0.34f, 0.30f, 1.30f), stone, false);
                    }
                }
            }
            float topY = gy + acc;

            // 每级台壁根部的杂草丛：打断整片夯土色，读出台阶
            for (int t = 0; t < 3; t++)
            {
                float ty = gy + (t == 0 ? 0f : (t == 1 ? height[0] : height[0] + height[1]));
                int per = t == 0 ? 30 : 20;
                for (int i = 0; i < per; i++)
                {
                    float a = i / (float)per * Mathf.PI * 2f + t * 0.21f;
                    GrassTuft(g.transform,
                        new Vector3(Mathf.Cos(a) * (side[t] * 0.5f + 0.16f), ty + 0.02f,
                                    Mathf.Sin(a) * (side[t] * 0.5f + 0.16f)),
                        (i % 3 == 0) ? dryGrass : grass, 0.75f + Rnd(i * 19 + t) * 0.5f, 900 + t * 40 + i);
                }
            }

            // 台面上的砾石散布
            for (int i = 0; i < 26; i++)
            {
                float a = Rnd(i * 37) * Mathf.PI * 2f;
                float r = 2.0f + Rnd(i * 53) * (side[2] * 0.5f - 2.2f);
                Ball("AltarGravel_" + i, g.transform,
                    new Vector3(Mathf.Cos(a) * r, topY + 0.06f, Mathf.Sin(a) * r),
                    0.10f + Rnd(i * 7) * 0.10f, stone);
            }

            // 南面登台坡道
            for (int i = 0; i < 6; i++)
                Box("AltarStep_" + i, g.transform,
                    new Vector3(0f, gy + 0.12f + i * (acc - 0.2f) / 6f, -side[0] * 0.5f - 1.4f + i * 0.30f),
                    new Vector3(3.0f, 0.34f, 0.44f), rammed, false);

            // 台上的 12 座小墓：两列，每列 6 座
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 6; i++)
                {
                    int idx = row * 6 + i;
                    float px = (i - 2.5f) * 1.36f;
                    float pz = (row == 0 ? -1.9f : 1.9f);
                    float len = 1.9f + Rnd(idx * 13) * 0.5f;
                    float wid = 0.92f + Rnd(idx * 7) * 0.16f;

                    Box("Grave_" + idx, g.transform, new Vector3(px, topY - 0.05f, pz),
                        new Vector3(wid, 0.10f, len), ashMat, false);
                    Box("Grave_S_" + idx, g.transform, new Vector3(px, topY + 0.05f, pz - len * 0.5f),
                        new Vector3(wid + 0.24f, 0.20f, 0.16f), stone, false);
                    Box("Grave_N_" + idx, g.transform, new Vector3(px, topY + 0.05f, pz + len * 0.5f),
                        new Vector3(wid + 0.24f, 0.20f, 0.16f), stone, false);
                    for (int sx = -1; sx <= 1; sx += 2)
                        Box("Grave_Side_" + idx + "_" + sx, g.transform,
                            new Vector3(px + sx * (wid * 0.5f + 0.06f), topY + 0.05f, pz),
                            new Vector3(0.14f, 0.20f, len), stone, false);

                    // 随葬玉器：玉琮（方）与玉璧（圆）
                    Box("JadeCong_" + idx, g.transform, new Vector3(px - 0.16f, topY + 0.13f, pz + 0.30f),
                        new Vector3(0.20f, 0.30f, 0.20f), jade, false);
                    Cyl("JadeBi_" + idx, g.transform, new Vector3(px + 0.18f, topY + 0.10f, pz - 0.22f),
                        0.17f, 0.05f, jadeDark);
                }

            // 燎祭遗迹：红烧土面与灰坑
            Box("BurningFloor", g.transform, new Vector3(-2.6f, gy + height[0] + 0.02f, 5.4f),
                new Vector3(4.2f, 0.06f, 3.0f), burntMat, false);
            for (int i = 0; i < 3; i++)
                Cyl("AshPit_" + i, g.transform,
                    new Vector3(-4.0f + i * 1.7f, gy + height[0] + 0.06f, 4.6f + (i % 2) * 1.1f),
                    0.42f, 0.10f, ashMat);

            // 四角石堆
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                float px = Mathf.Sin(a * Mathf.Deg2Rad) * (side[0] * 0.5f - 1.2f);
                float pz = Mathf.Cos(a * Mathf.Deg2Rad) * (side[0] * 0.5f - 1.2f);
                for (int k = 0; k < 4; k++)
                    Ball("PileStone_" + i + "_" + k, g.transform,
                        new Vector3(px + (Rnd(i * 17 + k) - 0.5f) * 0.7f,
                                    gy + 0.16f + (k % 2) * 0.22f,
                                    pz + (Rnd(i * 23 + k) - 0.5f) * 0.7f),
                        0.20f + Rnd(k * 31 + i) * 0.10f, stone);
            }

            // 祭坛说明碑
            Box("AltarStela", g.transform, new Vector3(0f, gy + 0.72f, -side[0] * 0.5f - 2.6f),
                new Vector3(1.30f, 1.44f, 0.28f), stone, true);
            CreateTextPlate(g.transform, "瑶 山 祭 坛\n三重土台 · 十二座墓葬",
                new Vector3(0f, gy + 0.72f, -side[0] * 0.5f - 2.77f), Vector3.zero,
                new Vector2(1.14f, 1.26f), 26, new Color(0.28f, 0.25f, 0.20f), title: true);

            // 坛外围的方形土垣：给祭坛一个"圣域"边界，远处才读得出这是一处独立场所
            const float RING = 15.6f;
            float ringY = gy + 0.26f;
            Box("RingBank_N", g.transform, new Vector3(0f, ringY, RING - 0.5f),
                new Vector3(RING * 2f, 0.56f, 1.00f), earth, false);
            Box("RingBank_S1", g.transform, new Vector3(-5.6f, ringY, -RING + 0.5f),
                new Vector3(RING * 2f - 8.4f, 0.56f, 1.00f), earth, false);
            Box("RingBank_S2", g.transform, new Vector3(5.6f, ringY, -RING + 0.5f),
                new Vector3(RING * 2f - 8.4f, 0.56f, 1.00f), earth, false);
            Box("RingBank_W", g.transform, new Vector3(-RING + 0.5f, ringY, 0f),
                new Vector3(1.00f, 0.56f, RING * 2f), earth, false);
            Box("RingBank_E", g.transform, new Vector3(RING - 0.5f, ringY, 0f),
                new Vector3(1.00f, 0.56f, RING * 2f), earth, false);
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.Sin(a) * RING) < 4.6f && Mathf.Cos(a) < 0f) continue;
                GrassTuft(g.transform,
                    new Vector3(Mathf.Sin(a) * (RING + 0.55f), gy + 0.04f, Mathf.Cos(a) * (RING + 0.55f)),
                    (i % 3 == 0) ? dryGrass : grass, 0.8f + Rnd(i * 27) * 0.5f, 620 + i);
            }

            // 南面神道：从土垣缺口一路铺到登台坡道，砾石路面
            var gravelMat = MatAs("LiGravel", new Color(0.55f, 0.51f, 0.42f), "stone", 0f, 0.08f);
            for (int i = 0; i < 16; i++)
            {
                float z = -RING - 6.0f + i * 1.35f;
                float w = 3.2f + Rnd(i * 13) * 0.7f;
                Box("Path_" + i, g.transform, new Vector3((Rnd(i * 31) - 0.5f) * 0.5f, gy + 0.04f, z),
                    new Vector3(w, 0.08f, 1.36f), (i % 3 == 0) ? paleMat : gravelMat, false);
            }

            // 台脚草丛与木桩界标
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                GrassTuft(g.transform,
                    new Vector3(Mathf.Sin(a) * (side[0] * 0.5f + 0.9f), gy + 0.02f,
                                Mathf.Cos(a) * (side[0] * 0.5f + 0.9f)),
                    dryGrass, 0.8f + Rnd(i * 7) * 0.4f, 400 + i);
            }
            for (int i = 0; i < 6; i++)
            {
                float px = -side[0] * 0.5f - 1.1f + i * 2.4f;
                Cyl("BoundPost_" + i, g.transform, new Vector3(px, gy + 0.42f, -side[0] * 0.5f - 1.9f),
                    0.07f, 0.84f, wood);
            }
        }

        // ================================================================ 码头

        /// <summary>
        /// 城内河道旁的木构栈桥码头：平行河岸的平台 + 伸入水面的栈桥，
        /// 配系船桩、独木舟、渔网与鱼篓。形制参考河道内木构码头遗迹，尺度为示意。
        /// </summary>
        static
 void Dock(Transform parent, Vector3 pos, Material wood, Material woodLight, Material bark,
                  Material pottery, Material reed, Material cloth)
        {
            var g = Empty("Dock", parent, Vector3.zero);
            const float DECK = 1.34f;

            // 平台铺板（沿 X 一块块铺）
            for (int i = 0; i < 17; i++)
                Box("DeckPlank_" + i, g.transform,
                    new Vector3(-20.7f + i * 0.375f, DECK, 11.6f),
                    new Vector3(0.35f, 0.10f, 3.50f), (i % 2 == 0) ? woodLight : wood, false);
            for (int i = 0; i < 3; i++)
                Box("DeckJoist_" + i, g.transform,
                    new Vector3(-17.7f, DECK - 0.14f, 10.1f + i * 1.50f),
                    new Vector3(6.60f, 0.16f, 0.18f), wood, false);

            // 平台桩柱
            for (int ix = 0; ix < 4; ix++)
                for (int iz = 0; iz < 2; iz++)
                {
                    float px = -20.4f + ix * 1.85f;
                    float pz = iz == 0 ? 10.2f : 13.0f;
                    float gy = LiangzhuHeight(px, pz);
                    float hgt = DECK - gy + 0.55f;
                    Cyl("DockPile_" + ix + "_" + iz, g.transform,
                        new Vector3(px, gy + hgt * 0.5f - 0.10f, pz), 0.13f, hgt, bark);
                }

            // 伸入河道的栈桥
            for (int i = 0; i < 11; i++)
                Box("JettyPlank_" + i, g.transform,
                    new Vector3(-17.7f, DECK - 0.02f, 13.9f + i * 0.375f),
                    new Vector3(3.05f, 0.09f, 0.35f), (i % 2 == 0) ? wood : woodLight, false);
            for (int i = 0; i < 2; i++)
                Box("JettyString_" + i, g.transform,
                    new Vector3(-19.05f + i * 2.7f, DECK - 0.16f, 15.8f),
                    new Vector3(0.16f, 0.14f, 4.10f), wood, false);
            for (int i = 0; i < 3; i++)
            {
                float pz = 14.6f + i * 1.55f;
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    Cyl("JettyPile_" + i + "_" + sx, g.transform,
                        new Vector3(-17.7f + sx * 1.42f, 0.28f, pz), 0.12f, 2.30f, bark);
                }
                Box("JettyCross_" + i, g.transform, new Vector3(-17.7f, 0.70f, pz),
                    new Vector3(3.25f, 0.13f, 0.15f), wood, false);
            }

            // 系船桩与绳圈
            for (int i = 0; i < 3; i++)
            {
                float pz = 14.2f + i * 1.35f;
                Cyl("MoorPost_" + i, g.transform, new Vector3(-16.85f, DECK + 0.30f, pz), 0.10f, 0.72f, wood);
                Cyl("MoorRing_" + i, g.transform, new Vector3(-16.85f, DECK + 0.52f, pz), 0.16f, 0.07f, cloth);
            }

            // 两只独木舟
            for (int i = 0; i < 2; i++)
            {
                var c = Empty("Dugout_" + i, g.transform,
                    new Vector3(-17.6f + i * 0.15f, 0.62f, 15.1f + i * 1.9f));
                c.transform.localRotation = Quaternion.Euler(0f, (i == 0 ? 8f : -6f), 0f);
                Box("Hull", c.transform, Vector3.zero, new Vector3(0.86f, 0.30f, 3.10f), bark, false);
                Box("Hollow", c.transform, new Vector3(0f, 0.14f, 0f),
                    new Vector3(0.62f, 0.12f, 2.62f), wood, false);
                Box("Bow", c.transform, new Vector3(0f, 0.02f, 1.65f),
                    new Vector3(0.40f, 0.22f, 0.42f), bark, false);
                Box("Stern", c.transform, new Vector3(0f, 0.02f, -1.65f),
                    new Vector3(0.44f, 0.22f, 0.42f), bark, false);
                for (int k = 0; k < 2; k++)
                    Box("Seat_" + k, c.transform, new Vector3(0f, 0.10f, -0.75f + k * 1.5f),
                        new Vector3(0.80f, 0.07f, 0.22f), woodLight, false);
            }

            // 渔具与陶器
            for (int i = 0; i < 3; i++)
                Cyl("FishBasket_" + i, g.transform,
                    new Vector3(-20.1f + i * 0.75f, DECK + 0.18f, 10.1f + (i % 2) * 0.55f),
                    0.24f, 0.34f, reed);
            Cyl("WaterJar", g.transform, new Vector3(-19.2f, DECK + 0.28f, 12.9f), 0.28f, 0.56f, pottery);
            Cyl("WaterJar2", g.transform, new Vector3(-18.55f, DECK + 0.22f, 12.75f), 0.20f, 0.44f, pottery);
            Box("Paddle", g.transform, new Vector3(-16.5f, DECK + 0.14f, 12.2f),
                new Vector3(0.16f, 0.06f, 1.55f), woodLight, false);
            Box("PaddleBlade", g.transform, new Vector3(-16.5f, DECK + 0.14f, 11.35f),
                new Vector3(0.28f, 0.05f, 0.52f), woodLight, false);

            // 晾网架
            for (int i = 0; i < 2; i++)
                Cyl("NetPole_" + i, g.transform,
                    new Vector3(-20.4f + i * 3.1f, DECK + 0.85f, 9.3f), 0.07f, 1.70f, wood);
            Box("NetBar", g.transform, new Vector3(-18.85f, DECK + 1.58f, 9.3f),
                new Vector3(3.2f, 0.06f, 0.06f), wood, false);
            for (int i = 0; i < 5; i++)
            {
                var net = Box("Net_" + i, g.transform,
                    new Vector3(-20.1f + i * 0.62f, DECK + 1.12f, 9.3f),
                    new Vector3(0.56f, 0.92f, 0.02f), cloth, false);
                net.transform.localRotation = Quaternion.Euler(0f, 0f, (Rnd(i * 11) - 0.5f) * 7f);
            }

            // 入河台阶
            for (int i = 0; i < 4; i++)
                Box("DockStep_" + i, g.transform,
                    new Vector3(-14.75f, 1.02f - i * 0.22f, 12.9f),
                    new Vector3(0.52f, 0.16f, 1.10f), (i % 2 == 0) ? woodLight : wood, false);

            // 说明牌
            for (int i = 0; i < 2; i++)
                Cyl("DockSignLeg_" + i, g.transform,
                    new Vector3(-19.05f + i * 0.92f, DECK + 0.34f, 10.30f), 0.06f, 0.70f, wood);
            Box("DockSign", g.transform, new Vector3(-18.59f, DECK + 0.92f, 10.30f),
                new Vector3(1.14f, 0.72f, 0.07f), woodLight, false);
            CreateTextPlate(g.transform, "钟 家 港 码 头\n木构栈桥 · 独木舟",
                new Vector3(-18.59f, DECK + 0.93f, 10.25f), Vector3.zero,
                new Vector2(1.02f, 0.64f), 22, new Color(0.24f, 0.20f, 0.15f), title: true);
        }

        /// <summary>单片水平水面（+Y 朝上），避免闭合立方体在掠射角度下出现叠面色块。</summary>
        static GameObject WaterQuad(string name, Transform parent, Vector3 pos,
                                    float sx, float sz, Material mat)
        {
            float hx = sx * 0.5f, hz = sz * 0.5f;
            var mesh = new Mesh { name = name + "_Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz),
                new Vector3(hx, 0f, hz), new Vector3(-hx, 0f, hz)
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            var quad = MeshObject(name, parent, pos, mesh, mat, false);
            quad.layer = LiWaterLayer;
            return quad;
        }

        /// <summary>带状水面：水面 + 湿泥河床，河床从水面向下延伸，避免露出草地。</summary>
        static void WaterBand(Transform parent, string name, Vector3 center, float length, float width,
                              float waterY, Material water, Material mud, float yaw, bool colliders)
        {
            var g = Empty(name, parent, center);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var bed = Box(name + "_Bed", g.transform, new Vector3(0f, waterY - 0.78f, 0f),
                new Vector3(length, 1.0f, width + 0.2f), mud, colliders);
            bed.layer = LiWaterLayer;
            // 河床不投影也不收影：它躺在水面下，一旦接收阴影，级联分界就会透过半透明水面显成方块
            var bedMr = bed.GetComponent<MeshRenderer>();
            if (bedMr != null)
            {
                bedMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bedMr.receiveShadows = false;
            }
            // 水面用单张四边面，不用闭合立方体：立方体的侧面与底面在大角度视线下会叠出屏幕对齐的色块
            var surf = WaterQuad(name + "_Water", g.transform, new Vector3(0f, waterY, 0f),
                length, width, water);
            // 水面不投影：否则互相叠压的水槽会在水里投出巨大方块阴影
            var mr = surf.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // 水面不接收阴影：阴影级联分界会在大片水面上切出方形的明暗台阶
                mr.receiveShadows = false;
            }
        }

        /// <summary>木栈桥：桥面板 + 纵梁 + 栏杆 + 桥墩。</summary>
        static void DeckBridge(Transform parent, Vector3 pos, float width, float length, float waterY,
                               Material wood, Material woodLight, float deckY = 0.18f)
        {
            var g = Empty("DeckBridge", parent, pos);
            int planks = Mathf.Max(7, Mathf.RoundToInt(length / 0.55f));
            float step = length / planks;
            for (int i = 0; i < planks; i++)
            {
                float z = (i + 0.5f) * step - length * 0.5f;
                Box("Plank_" + i, g.transform, new Vector3(0f, deckY, z),
                    new Vector3(width, 0.09f, step * 0.88f), (i % 2 == 0) ? wood : woodLight, false);
            }

            // 木板只是看相，真正让人走过去的是这块看不见的碰撞板。
            // 之前整座桥没有一个碰撞体：走到河心就踩进水里，等于断路。
            // 顶面与木板齐平（deckY + 0.045），两头的引桥也靠它接上路基。
            var deck = Box("DeckCollider", g.transform, new Vector3(0f, deckY - 0.015f, 0f),
                new Vector3(width, 0.12f, length), null, true);
            var deckRenderer = deck.GetComponent<MeshRenderer>();
            if (deckRenderer != null) deckRenderer.enabled = false;

            for (int s = -1; s <= 1; s += 2)
            {
                Box("Stringer", g.transform, new Vector3(s * (width * 0.5f - 0.20f), deckY - 0.09f, 0f),
                    new Vector3(0.20f, 0.20f, length), wood, false);
                Box("Rail", g.transform, new Vector3(s * (width * 0.5f - 0.06f), deckY + 0.68f, 0f),
                    new Vector3(0.10f, 0.12f, length), wood, false);

                int posts = Mathf.Max(2, Mathf.RoundToInt(length / 1.8f));
                for (int i = 0; i <= posts; i++)
                {
                    float z = (i / (float)posts - 0.5f) * (length - 0.25f);
                    Box("RailPost", g.transform, new Vector3(s * (width * 0.5f - 0.06f), deckY + 0.32f, z),
                        new Vector3(0.13f, 0.86f, 0.13f), wood, false);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1) * (length * 0.34f);
                for (int s = -1; s <= 1; s += 2)
                    Box("Pier", g.transform, new Vector3(s * (width * 0.5f - 0.28f), waterY - 0.2f, z),
                        new Vector3(0.24f, 2.2f, 0.24f), wood, false);
            }
        }

        /// <summary>遗址标识：以玉琮为形的标石（内圆外方、分节、简化神人兽面纹）。</summary>
        static void SiteMarker(Transform parent, Vector3 pos, Material stone, Material jade,
                               Material jadeDark, Material rammed)
        {
            var g = Empty("SiteMarker", parent, pos);

            Cyl("Plaza", g.transform, new Vector3(0f, 0.10f, 0f), 6.4f, 0.20f, stone, false);
            Cyl("Plaza_In", g.transform, new Vector3(0f, 0.17f, 0f), 4.8f, 0.16f, rammed, false);
            for (int i = 0; i < 3; i++)
                Cyl("Plaza_Ring_" + i, g.transform, new Vector3(0f, 0.19f, 0f),
                    4.2f - i * 1.1f, 0.03f, stone, false);

            Box("Plinth", g.transform, new Vector3(0f, 0.46f, 0f), new Vector3(2.30f, 0.56f, 2.30f), stone, true);
            Box("PlinthCap", g.transform, new Vector3(0f, 0.78f, 0f), new Vector3(2.52f, 0.12f, 2.52f), rammed, false);

            // 玉琮本体：高宽比接近 4:1，分 6 节，纹样只用细凹槽与极小的眼纹提示
            const int sections = 6;
            const float secH = 0.36f;
            float y = 0.84f;
            for (int i = 0; i < sections; i++)
            {
                float w = Mathf.Lerp(1.16f, 1.02f, i / (float)(sections - 1));
                Box("Cong_" + i, g.transform, new Vector3(0f, y + secH * 0.5f, 0f),
                    new Vector3(w, secH, w), (i % 2 == 0) ? jade : jadeDark, true);
                Box("Groove_" + i, g.transform, new Vector3(0f, y + secH - 0.02f, 0f),
                    new Vector3(w + 0.02f, 0.035f, w + 0.02f), jadeDark, false);

                float d = w * 0.5f;
                for (int f = 0; f < 4; f++)
                {
                    var face = Empty("Face_" + i + "_" + f, g.transform, Vector3.zero);
                    face.transform.localRotation = Quaternion.Euler(0f, f * 90f, 0f);
                    Box("Band", face.transform, new Vector3(0f, y + secH * 0.74f, d),
                        new Vector3(w * 0.80f, 0.030f, 0.02f), jadeDark, false);
                    Box("Band2", face.transform, new Vector3(0f, y + secH * 0.30f, d),
                        new Vector3(w * 0.72f, 0.026f, 0.02f), jadeDark, false);
                    for (int e = -1; e <= 1; e += 2)
                        Box("Eye", face.transform, new Vector3(e * w * 0.17f, y + secH * 0.52f, d),
                            new Vector3(w * 0.10f, 0.052f, 0.02f), jadeDark, false);
                }
                y += secH;
            }

            Cyl("Mouth_Hole", g.transform, new Vector3(0f, y + 0.015f, 0f), 0.34f, 0.06f, jadeDark);
            Cyl("Mouth_Rim", g.transform, new Vector3(0f, y + 0.055f, 0f), 0.40f, 0.09f, jade);

            // 玉璧：平置在矮石台上，不做立环（立环会读成现代雕塑）
            var bi = Empty("Bi", g.transform, new Vector3(-3.5f, 0f, 1.9f));
            Box("Bi_Table", bi.transform, new Vector3(0f, 0.30f, 0f),
                new Vector3(1.90f, 0.60f, 1.90f), stone, true);
            var wheel = Empty("Bi_Wheel", bi.transform, new Vector3(0f, 0.625f, 0f));
            const int biSegs = 24;
            for (int i = 0; i < biSegs; i++)
            {
                float a = i / (float)biSegs * Mathf.PI * 2f;
                var seg = Empty("BiSeg_" + i, wheel.transform,
                    new Vector3(Mathf.Cos(a) * 0.62f, 0f, Mathf.Sin(a) * 0.62f));
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                Box("S", seg.transform, Vector3.zero, new Vector3(0.42f, 0.055f, 0.26f),
                    (i % 2 == 0) ? jade : jadeDark, false);
            }

            Box("Stela", g.transform, new Vector3(3.6f, 0.90f, 1.5f), new Vector3(1.5f, 1.6f, 0.34f), stone, true);
            CreateTextPlate(g.transform, "玉 琮\n内圆外方 · 分节\n良渚文化标志性器类",
                new Vector3(3.6f, 1.76f, 1.66f), Vector3.zero,
                new Vector2(1.30f, 0.95f), 32, new Color(0.30f, 0.26f, 0.20f), title: true);
        }

        /// <summary>水稻田：泥底 + 浅水面 + 田埂 + 稻秧行。</summary>
        static void Paddy(Transform parent, Vector3 center, float w, float d,
                          Material mud, Material water, Material rice, Material dike)
        {
            var g = Empty("Paddy", parent, center);
            float y0 = LiangzhuHeight(center.x, center.z) - center.y;

            // 田面整体抬到地形起伏之上，否则地形会把水面盖掉
            var bed = Box("Bed", g.transform, new Vector3(0f, y0 - 0.20f, 0f), new Vector3(w, 0.82f, d), mud, true);
            bed.layer = LiWaterLayer;
            var bedMr = bed.GetComponent<MeshRenderer>();
            if (bedMr != null)
            {
                bedMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bedMr.receiveShadows = false;
            }
            WaterQuad("Water", g.transform, new Vector3(0f, y0 + 0.29f, 0f),
                w - 0.9f, d - 0.9f, water);

            Box("Dike_N", g.transform, new Vector3(0f, y0 + 0.33f, d * 0.5f - 0.22f),
                new Vector3(w, 0.34f, 0.44f), dike, true);
            Box("Dike_S", g.transform, new Vector3(0f, y0 + 0.33f, -d * 0.5f + 0.22f),
                new Vector3(w, 0.34f, 0.44f), dike, true);
            Box("Dike_W", g.transform, new Vector3(-w * 0.5f + 0.22f, y0 + 0.33f, 0f),
                new Vector3(0.44f, 0.34f, d), dike, false);
            Box("Dike_E", g.transform, new Vector3(w * 0.5f - 0.22f, y0 + 0.33f, 0f),
                new Vector3(0.44f, 0.34f, d), dike, false);

            // 秧苗：成排的十字叶丛。尺寸压到 0.2 m 上下、每丛随机缩放 0.8–1.2，
            // 远看是"行"，近看是"叶"；太大会排成一地绿色小方块。
            int rows = Mathf.Max(10, Mathf.RoundToInt((w - 2.0f) / 0.60f));
            int cols = Mathf.Max(9, Mathf.RoundToInt((d - 2.0f) / 0.60f));
            float spanX = (w - 2.2f) / Mathf.Max(1, rows - 1);
            float spanZ = (d - 2.2f) / Mathf.Max(1, cols - 1);
            int seedBase = (int)(center.x * 3f) + (int)(center.z * 5f);
            for (int i = 0; i < rows; i++)
            {
                float x = (i - (rows - 1) * 0.5f) * spanX;
                for (int k = 0; k < cols; k++)
                {
                    float z = (k - (cols - 1) * 0.5f) * spanZ;
                    float r = Rnd(i * 31 + k * 7 + seedBase);
                    // 手插秧也不会齐成棋盘：每丛抖开 ±9 cm、高度 0.10–0.31，
                    // 远看还是行，近看不再是绿色的方格布。
                    x += (Rnd(i * 43 + k * 13 + seedBase) - 0.5f) * 0.18f;
                    z += (Rnd(i * 47 + k * 19 + seedBase) - 0.5f) * 0.18f;
                    float h = 0.10f + r * 0.21f;
                    float turn = Rnd(i * 17 + k * 23 + seedBase) * 180f;
                    float sc = 0.72f + Rnd(i * 11 + k * 41 + seedBase) * 0.56f;
                    var tuft = Empty("Rice_" + i + "_" + k, g.transform,
                        new Vector3(x, y0 + 0.31f, z));
                    tuft.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
                    tuft.transform.localScale = new Vector3(sc, 1f, sc);

                    // 三片窄叶绕心排开、各自外倾 7°：两片宽"卡片"交叉远看是绿的方块，
                    // 三片窄叶有体积又不会散成像素点。窄到 0.09 m 还看得见，
                    // 是因为预览是 2× 超采样 + 4×MSAA，实机 PICO 上也有 MSAA 托底。
                    Box("Clump", tuft.transform, new Vector3(0f, 0.012f, 0f),
                        new Vector3(0.13f, 0.03f, 0.13f), rice, false);
                    for (int b = 0; b < 3; b++)
                    {
                        float bh = h * (0.86f + Rnd(i * 7 + k * 5 + b * 13 + seedBase) * 0.28f);
                        var leaf = Box("Leaf" + b, tuft.transform,
                            new Vector3((Rnd(i * 3 + k * 9 + b * 17 + seedBase) - 0.5f) * 0.07f, bh * 0.48f,
                                        (Rnd(i * 5 + k * 11 + b * 19 + seedBase) - 0.5f) * 0.07f),
                            new Vector3(0.09f, bh, 0.012f), rice, false);
                        leaf.transform.localRotation = Quaternion.Euler(
                            (Rnd(i * 23 + k * 29 + b * 31 + seedBase) - 0.5f) * 16f,
                            60f * b + Rnd(i * 37 + k * 41 + b * 43 + seedBase) * 26f,
                            (Rnd(i * 43 + k * 47 + b * 53 + seedBase) - 0.5f) * 16f);
                    }
                }
            }
        }

        /// <summary>干栏式建筑：木桩架空、平台、木骨泥墙、茅草屋顶、木梯。</summary>
        static void StiltHouse(Transform parent, Vector3 pos, float yaw, float s,
                               Material wood, Material woodLight, Material wall, Material thatch, int seed)
        {
            var g = Empty("StiltHouse_" + seed, parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float W = 6.2f * s;
            float D = 4.3f * s;
            float floorY = LiangzhuHeight(pos.x, pos.z) - pos.y;
            float deck = floorY + 1.05f * s;
            float wallH = 2.0f * s;

            for (int ix = -1; ix <= 1; ix += 2)
                for (int iz = -1; iz <= 1; iz += 2)
                    Cyl("Post", g.transform, new Vector3(ix * W * 0.42f, deck * 0.5f, iz * D * 0.40f),
                        0.15f * s, deck + 0.6f, wood);

            for (int iz = -1; iz <= 1; iz += 2)
                Cyl("Post_Mid", g.transform, new Vector3(0f, deck * 0.5f, iz * D * 0.40f),
                    0.13f * s, deck + 0.6f, wood);

            Box("Deck", g.transform, new Vector3(0f, deck, 0f), new Vector3(W, 0.22f * s, D), woodLight, true);

            float wy = deck + 0.11f * s + wallH * 0.5f;
            Box("Wall_B", g.transform, new Vector3(0f, wy, -D * 0.44f),
                new Vector3(W - 0.5f, wallH, 0.18f), wall, true);
            Box("Wall_W", g.transform, new Vector3(-W * 0.47f, wy, 0f),
                new Vector3(0.18f, wallH, D - 0.6f), wall, true);
            Box("Wall_E", g.transform, new Vector3(W * 0.47f, wy, 0f),
                new Vector3(0.18f, wallH, D - 0.6f), wall, true);

            float doorW = 1.1f * s;
            float sidePanel = (W - 0.5f - doorW) * 0.5f;
            for (int sx = -1; sx <= 1; sx += 2)
                Box("Wall_F", g.transform,
                    new Vector3(sx * (doorW * 0.5f + sidePanel * 0.5f), wy, D * 0.44f),
                    new Vector3(sidePanel, wallH, 0.18f), wall, true);
            Box("Lintel_F", g.transform, new Vector3(0f, wy + wallH * 0.36f, D * 0.44f),
                new Vector3(doorW + 0.2f, wallH * 0.28f, 0.18f), wall, false);
            Box("Doorway", g.transform, new Vector3(0f, deck + 0.11f * s + wallH * 0.30f, D * 0.44f),
                new Vector3(doorW - 0.1f, wallH * 0.60f, 0.10f), wood, false);

            Box("Window", g.transform, new Vector3(W * 0.47f + 0.02f, wy + 0.15f, 0f),
                new Vector3(0.10f, 0.7f * s, 0.9f * s), wood, false);

            float eaveY = deck + 0.11f * s + wallH;
            float pitch = 2.15f * s;

            // 屋脊沿房屋长轴：在绕 Y 90° 的局部空间里建双坡
            var roofG = Empty("RoofRoot", g.transform, new Vector3(0f, eaveY, 0f));
            roofG.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            float rw = D + 1.4f;   // 局部 X：坡跨
            float rd = W + 1.6f;   // 局部 Z：脊长
            float hwR = rw * 0.5f + 0.30f;
            float hdR = rd * 0.5f + 0.30f;

            MeshObject("Roof", roofG.transform, Vector3.zero,
                MeshFactory.GableRoof(rw, rd, pitch), thatch);
            var ridgeCap = Cyl("RidgeCap", roofG.transform, new Vector3(0f, pitch - 0.07f, 0f),
                               0.13f * s, hdR * 2f, thatch, false);
            ridgeCap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int sx = -1; sx <= 1; sx += 2)
                Box("Eave_" + sx, roofG.transform, new Vector3(sx * hwR, -0.04f, 0f),
                    new Vector3(0.15f, 0.22f, hdR * 2f), thatch, false);

            // 椽条：贴在屋面下，沿坡向
            float slopeDeg = Mathf.Atan2(pitch, hwR - 0.30f) * Mathf.Rad2Deg;
            float rafLen = Mathf.Sqrt(Mathf.Pow(hwR - 0.30f, 2f) + pitch * pitch) - 0.35f;
            for (int i = 0; i < 5; i++)
            {
                float rz = (i - 2f) * (rd / 5.6f);
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    var raf = Box("Rafter", roofG.transform,
                        new Vector3(sx * hwR * 0.5f, pitch * 0.5f - 0.20f, rz),
                        new Vector3(rafLen, 0.11f, 0.11f), wood, false);
                    raf.transform.localRotation = Quaternion.Euler(0f, 0f, -sx * slopeDeg);
                }
            }

            // 茅草分层压边：沿坡向的几道台阶，读出层层压草的厚度
            for (int i = 0; i < 3; i++)
            {
                float t = (i + 1) / 4f;
                float ly = Mathf.Lerp(pitch - 0.12f, 0.10f, t);
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    var lay = Box("ThatchLayer", roofG.transform,
                        new Vector3(sx * Mathf.Lerp(0.30f, hwR - 0.12f, t), ly, 0f),
                        new Vector3(0.34f, 0.10f, hdR * 2f - 0.12f), thatch, false);
                    lay.transform.localRotation = Quaternion.Euler(0f, 0f, -sx * slopeDeg);
                }
            }

            // 檐口草裙：让屋面下缘不像一块切齐的板
            for (int sx = -1; sx <= 1; sx += 2)
                for (int i = 0; i < 6; i++)
                {
                    var sk = Box("EaveSkirt", roofG.transform,
                        new Vector3(sx * (hwR + 0.03f), -0.18f,
                                    -hdR + 0.40f + i * (hdR * 2f - 0.80f) / 5f),
                        new Vector3(0.10f, 0.36f, 0.34f), thatch, false);
                    sk.transform.localRotation = Quaternion.Euler(0f, 0f, sx * 6f);
                }

            Box("Beam", g.transform, new Vector3(0f, eaveY - 0.16f, D * 0.46f),
                new Vector3(W + 0.9f, 0.16f, 0.16f), wood, false);

            // 露明木骨：柱间系梁 + 斜撑 + 门框 + 窗棂
            for (int iz = -1; iz <= 1; iz += 2)
                Box("TieBeam", g.transform, new Vector3(0f, deck + 0.06f * s, iz * D * 0.40f),
                    new Vector3(W * 0.86f, 0.13f, 0.13f), wood, false);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                var br = Box("Brace", g.transform,
                    new Vector3(sx * (W * 0.42f - 0.42f), deck + 0.36f, D * 0.40f),
                    new Vector3(0.92f, 0.10f, 0.10f), wood, false);
                br.transform.localRotation = Quaternion.Euler(0f, 0f, sx * 38f);
            }
            for (int sx = -1; sx <= 1; sx += 2)
                Box("DoorJamb", g.transform,
                    new Vector3(sx * (doorW * 0.5f + 0.05f), deck + 0.11f * s + wallH * 0.30f, D * 0.46f),
                    new Vector3(0.11f, wallH * 0.68f, 0.13f), woodLight, false);
            Box("DoorHead", g.transform,
                new Vector3(0f, deck + 0.11f * s + wallH * 0.64f, D * 0.46f),
                new Vector3(doorW + 0.30f, 0.12f, 0.14f), woodLight, false);
            for (int i = 0; i < 3; i++)
                Box("WinBar", g.transform,
                    new Vector3(W * 0.47f + 0.10f, wy + 0.15f, (i - 1) * 0.30f * s),
                    new Vector3(0.05f, 0.68f * s, 0.05f), woodLight, false);
            Box("WinBarH", g.transform, new Vector3(W * 0.47f + 0.10f, wy + 0.15f, 0f),
                new Vector3(0.05f, 0.05f, 0.86f * s), woodLight, false);

            for (int i = 0; i < 3; i++)
                Box("Step_" + i, g.transform,
                    new Vector3(0f, 0.22f + i * (deck - 0.4f) / 3f, D * 0.5f + 0.55f),
                    new Vector3(0.95f, 0.07f, 0.22f), wood, false);
            for (int sx = -1; sx <= 1; sx += 2)
                Box("Ladder_Rail", g.transform, new Vector3(sx * 0.48f, deck * 0.5f, D * 0.5f + 0.5f),
                    new Vector3(0.07f, deck, 0.10f), wood, false);

            for (int sx = -1; sx <= 1; sx += 2)
                Box("Porch_Rail", g.transform, new Vector3(sx * (W * 0.5f - 0.2f), deck + 0.36f, D * 0.30f),
                    new Vector3(0.09f, 0.5f, D * 0.55f), woodLight, false);
            Box("Porch_Rail_F", g.transform,
                new Vector3(-W * 0.30f, deck + 0.36f, D * 0.5f - 0.1f),
                new Vector3(W * 0.34f, 0.5f, 0.09f), woodLight, false);
        }

        /// <summary>火塘：石圈 + 柴薪 + 低强度暖光。</summary>
        static void FirePit(Transform parent, Vector3 pos, Material stone, Material wood)
        {
            var g = Empty("FirePit", parent, pos);
            float y = LiangzhuHeight(pos.x, pos.z) - pos.y;
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                Cyl("Stone_" + i, g.transform,
                    new Vector3(Mathf.Sin(a) * 0.78f, y + 0.14f, Mathf.Cos(a) * 0.78f),
                    0.20f, 0.28f, stone);
            }
            Box("Ash", g.transform, new Vector3(0f, y + 0.05f, 0f), new Vector3(1.25f, 0.10f, 1.25f), stone, false);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 46f;
                var log = Cyl("Log_" + i, g.transform, new Vector3(0f, y + 0.16f, 0f), 0.09f, 1.3f, wood);
                log.transform.localRotation = Quaternion.Euler(0f, a, 90f);
            }

            var lightGO = new GameObject("FireLight", typeof(Light));
            lightGO.transform.SetParent(g.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, y + 0.6f, 0f);
            var l = lightGO.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = Lin(new Color(1f, 0.72f, 0.42f));
            l.intensity = 1.1f;
            l.range = 9f;
            l.shadows = LightShadows.None;
        }

        /// <summary>晾晒架：两柱一横杆，挂织物与草束。</summary>
        static void DryingRack(Transform parent, Vector3 pos, float yaw,
                               Material wood, Material cloth, Material reed)
        {
            var g = Empty("DryingRack", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float y = LiangzhuHeight(pos.x, pos.z) - pos.y;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                Box("Post", g.transform, new Vector3(sx * 1.9f, y + 1.1f, 0f),
                    new Vector3(0.16f, 2.2f, 0.16f), wood, false);
                Box("Brace", g.transform, new Vector3(sx * 1.9f, y + 1.9f, 0.35f),
                    new Vector3(0.10f, 0.10f, 0.85f), wood, false);
            }
            Box("Pole", g.transform, new Vector3(0f, y + 2.10f, 0f), new Vector3(4.1f, 0.12f, 0.12f), wood, false);

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 1.05f;
                Box("Cloth_" + i, g.transform, new Vector3(x, y + 1.55f, 0f),
                    new Vector3(0.78f, 1.05f, 0.05f), cloth, false);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = (i - 0.5f) * 1.6f;
                Box("Reed_" + i, g.transform, new Vector3(x, y + 1.62f, 0.10f),
                    new Vector3(0.42f, 0.92f, 0.30f), reed, false);
            }
        }

        /// <summary>陶器堆：陶罐、陶豆与陶鼎的示意组合。</summary>
        static void PotteryCluster(Transform parent, Vector3 pos, Material pottery, int seed)
        {
            var g = Empty("Pottery_" + seed, parent, pos);
            float y = LiangzhuHeight(pos.x, pos.z) - pos.y;

            for (int i = 0; i < 4; i++)
            {
                float a = Rnd(seed + i * 7) * 6.28f;
                float r = 0.18f + Rnd(seed + i * 11) * 0.5f;
                var p = new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
                float h = 0.55f + Rnd(seed + i * 17) * 0.45f;
                float rad = 0.20f + Rnd(seed + i * 23) * 0.12f;

                Cyl("Body_" + i, g.transform, new Vector3(p.x, y + h * 0.5f, p.z), rad, h, pottery);
                Cyl("Rim_" + i, g.transform, new Vector3(p.x, y + h + 0.03f, p.z), rad * 0.78f, 0.08f, pottery);
            }

            var tilt = Cyl("Tilted", g.transform, new Vector3(0.85f, y + 0.28f, -0.55f), 0.26f, 0.62f, pottery);
            tilt.transform.localRotation = Quaternion.Euler(0f, 0f, 72f);

            Box("Mat", g.transform, new Vector3(0f, y + 0.03f, 0f), new Vector3(1.9f, 0.05f, 1.9f), pottery, false);
        }

        /// <summary>柴薪堆。</summary>
        static void FirewoodStack(Transform parent, Vector3 pos, Material wood, Material bark)
        {
            var g = Empty("Firewood", parent, pos);
            float y = LiangzhuHeight(pos.x, pos.z) - pos.y;

            for (int layer = 0; layer < 3; layer++)
            {
                int n = 4 - layer;
                for (int i = 0; i < n; i++)
                {
                    float x = (i - (n - 1) * 0.5f) * 0.30f;
                    var log = Cyl("Log_" + layer + "_" + i, g.transform,
                        new Vector3(x, y + 0.16f + layer * 0.28f, 0f), 0.14f, 1.5f,
                        (i + layer) % 2 == 0 ? wood : bark);
                    log.transform.localRotation = Quaternion.Euler(0f, 4f * layer, 90f);
                }
            }
        }

        /// <summary>木栅栏。</summary>
        static void WoodFence(Transform parent, Vector3 pos, float length, float yaw,
                              Material wood, Material woodLight)
        {
            var g = Empty("Fence", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            int posts = Mathf.Max(3, Mathf.RoundToInt(length / 1.6f));
            for (int i = 0; i <= posts; i++)
            {
                float t = (i / (float)posts - 0.5f) * length;
                float px = pos.x + t * Mathf.Sin(yaw * Mathf.Deg2Rad + Mathf.PI * 0.5f);
                float pz = pos.z + t * Mathf.Cos(yaw * Mathf.Deg2Rad + Mathf.PI * 0.5f);
                float y = LiangzhuHeight(px, pz) - pos.y;
                Box("Post_" + i, g.transform, new Vector3(t, y + 0.62f, 0f),
                    new Vector3(0.13f, 1.24f, 0.13f), wood, false);
            }
            for (int r = 0; r < 2; r++)
                Box("Rail_" + r, g.transform, new Vector3(0f, 0.48f + r * 0.46f, 0f),
                    new Vector3(length, 0.10f, 0.09f), woodLight, false);
        }

        /// <summary>独木舟。</summary>
        static void Dugout(Transform parent, Vector3 pos, float yaw, Material wood)
        {
            var g = Empty("Dugout", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box("Hull", g.transform, Vector3.zero, new Vector3(0.95f, 0.42f, 5.4f), wood, false);
            Box("Hollow", g.transform, new Vector3(0f, 0.20f, 0f), new Vector3(0.62f, 0.14f, 4.6f),
                MatAs("LiHullInner", new Color(0.20f, 0.16f, 0.12f), "wood", 0f, 0.08f), false);
            Box("Prow", g.transform, new Vector3(0f, 0.10f, 2.75f), new Vector3(0.55f, 0.34f, 0.7f), wood, false);
            Box("Stern", g.transform, new Vector3(0f, 0.10f, -2.75f), new Vector3(0.62f, 0.34f, 0.5f), wood, false);
            Box("Paddle", g.transform, new Vector3(0.42f, 0.22f, 0.4f), new Vector3(0.10f, 0.06f, 1.6f), wood, false);
        }

        /// <summary>陆城门：木构门架 + 土垄间的木铺坡道。</summary>
        static void GateHouse(Transform parent, Vector3 pos, Material wood, Material woodLight,
                              Material thatch, Material rammed)
        {
            var g = Empty("GateHouse", parent, pos);
            float y = LiangzhuHeight(pos.x, pos.z) - pos.y;

            for (int i = 0; i < 14; i++)
            {
                float z = -4.4f + i * 0.66f;
                float h = LiangzhuHeight(pos.x, pos.z + z) - pos.y;
                Box("Causeway_" + i, g.transform, new Vector3(0f, h + 0.05f, z),
                    new Vector3(4.6f, 0.14f, 0.72f), (i % 2 == 0) ? wood : woodLight, false);
            }

            for (int sx = -1; sx <= 1; sx += 2)
            {
                Cyl("DoorPost", g.transform, new Vector3(sx * 3.0f, y + 2.4f, 0f), 0.32f, 4.8f, wood, true);
                Box("Brace", g.transform, new Vector3(sx * 3.0f, y + 3.4f, 0.9f),
                    new Vector3(0.22f, 0.22f, 2.2f), wood, false);
                Box("Reveal", g.transform, new Vector3(sx * 5.4f, y + 1.0f, 0f),
                    new Vector3(2.2f, 2.2f, 2.6f), rammed, false);
            }

            Box("Lintel", g.transform, new Vector3(0f, y + 4.5f, 0f), new Vector3(7.2f, 0.42f, 0.42f), wood, false);
            Box("Lintel2", g.transform, new Vector3(0f, y + 3.9f, 0f), new Vector3(6.4f, 0.24f, 0.30f), wood, false);

            MeshObject("GateRoof", g.transform, new Vector3(0f, y + 4.72f, 0f),
                MeshFactory.GableRoof(8.6f, 3.2f, 1.1f), thatch);

            for (int sx = -1; sx <= 1; sx += 2)
            {
                // 门扇要真正敞开：28° 时两扇内缘只留 0.7 m，胶囊半径 0.34 m 的人
                // 过不去，走到匾额底下就被顶住。76° 贴向两侧门墙，中间净宽约 2.4 m。
                var leaf = Box("DoorLeaf", g.transform, new Vector3(sx * 1.5f, y + 1.9f, -0.55f),
                    new Vector3(2.6f, 3.6f, 0.12f), woodLight, true);
                leaf.transform.localRotation = Quaternion.Euler(0f, sx * 76f, 0f);
            }

            for (int sx = -1; sx <= 1; sx += 2)
                Cyl("Marker", g.transform, new Vector3(sx * 4.2f, y + 0.75f, -3.6f), 0.16f, 1.5f, wood);

            // 门额：楷书匾
            Box("PlaqueBoard", g.transform, new Vector3(0f, y + 4.02f, -0.22f),
                new Vector3(3.30f, 0.86f, 0.10f), woodLight, false);
            Box("PlaqueFrame", g.transform, new Vector3(0f, y + 4.02f, -0.19f),
                new Vector3(3.52f, 1.06f, 0.08f), wood, false);
            CreateTextPlate(g.transform, "良 渚 古 城", new Vector3(0f, y + 4.02f, -0.285f),
                Vector3.zero, new Vector2(3.05f, 0.72f), 60, new Color(0.16f, 0.13f, 0.09f), title: true);
        }

        /// <summary>水门：跨水木闸与堤岸木构。</summary>
        static void WaterGate(Transform parent, Vector3 pos, Material wood, Material woodLight,
                              Material rammed, float side, bool perpendicular = false)
        {
            var g = Empty("WaterGate", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, perpendicular ? 90f : 0f, 0f);
            float y = LiangzhuHeight(pos.x, pos.z) - pos.y;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                Box("Bank", g.transform, new Vector3(sx * 3.5f, y + 0.2f, 0f),
                    new Vector3(1.5f, 2.4f, 3.0f), rammed, true);
                Cyl("Post", g.transform, new Vector3(sx * 2.6f, y + 1.4f, 0f), 0.24f, 3.6f, wood);
                Box("Deck", g.transform, new Vector3(sx * 3.5f, y + 1.42f, 0f),
                    new Vector3(1.6f, 0.14f, 3.2f), woodLight, false);
            }
            Box("Beam", g.transform, new Vector3(0f, y + 3.0f, 0f), new Vector3(6.0f, 0.34f, 0.34f), wood, false);
            Box("Beam2", g.transform, new Vector3(0f, y + 2.4f, 0f), new Vector3(5.6f, 0.22f, 0.26f), woodLight, false);

            for (int i = 0; i < 4; i++)
                Box("Sluice_" + i, g.transform, new Vector3(0f, y + 0.35f + i * 0.30f, 0f),
                    new Vector3(4.6f, 0.22f, 0.12f), wood, false);
        }

        /// <summary>莫角山台地上的柱洞阵列（石圈示意柱洞，南侧两列补出残柱）。</summary>
        static void PostHoleGrid(Transform parent, Vector3 pos, int nx, int nz,
                                 float sx, float sz, Material stone, Material wood)
        {
            var g = Empty("PostHoles", parent, pos);
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nz; j++)
                {
                    float x = (i - (nx - 1) * 0.5f) * sx;
                    float z = (j - (nz - 1) * 0.5f) * sz;
                    Cyl("Hole_" + i + "_" + j, g.transform, new Vector3(x, 0.06f, z), 0.60f, 0.12f, stone);
                    Cyl("HoleIn_" + i + "_" + j, g.transform, new Vector3(x, 0.10f, z), 0.32f, 0.08f,
                        MatAs("LiPostFill", new Color(0.22f, 0.19f, 0.15f), "earth", 0f, 0.06f));
                    if (i == 0 || j == nz - 1)
                        Cyl("Stub_" + i + "_" + j, g.transform, new Vector3(x, 0.26f, z), 0.22f, 0.40f, wood);
                }
            }
        }

        /// <summary>莫角山台地上的大型建筑基址（示意性复原：台基、柱网、茅草大屋顶）。</summary>
        static void GrandHall(Transform parent, Vector3 pos, float yaw, Material wood, Material woodLight,
                              Material rammed, Material thatch, Material stone, Material pottery)
        {
            var g = Empty("GrandHall", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            const float W = 21f, D = 13f;
            Box("Platform", g.transform, new Vector3(0f, 0.34f, 0f), new Vector3(W + 2.4f, 0.68f, D + 2.4f), rammed, true);
            for (int i = 0; i < 4; i++)
                Box("Step_" + i, g.transform, new Vector3(0f, 0.58f - i * 0.18f, -(D * 0.5f + 1.2f + i * 0.42f)),
                    new Vector3(4.4f, 0.18f, 0.44f), rammed, false);

            // 前庭：夯土甬道 + 两列木柱残迹 + 陶罐，避免台地上只剩一片草
            Box("Apron", g.transform, new Vector3(0f, 0.02f, -(D * 0.5f + 6.0f)),
                new Vector3(7.6f, 0.30f, 11.5f), rammed, false);
            for (int i = 0; i < 6; i++)
            {
                float z = -(D * 0.5f + 1.6f + i * 1.85f);
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    Cyl("CourtPost_" + i + "_" + sx, g.transform, new Vector3(sx * 4.3f, 0.40f, z),
                        0.19f, 0.80f, wood);
                    Box("CourtBase_" + i + "_" + sx, g.transform, new Vector3(sx * 4.3f, 0.10f, z),
                        new Vector3(0.62f, 0.20f, 0.62f), stone, false);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float z = -(D * 0.5f + 2.2f + (i / 2) * 2.6f);
                var jar = Cyl("Jar_" + i, g.transform, new Vector3(side * 6.1f, 0.46f, z),
                              0.34f, 0.72f, pottery);
                Cyl("JarNeck_" + i, g.transform, new Vector3(side * 6.1f, 0.90f, z),
                    0.19f, 0.26f, pottery);
            }

            // 柱网：3 列 × 4 行
            for (int ix = 0; ix < 4; ix++)
            {
                for (int iz = 0; iz < 3; iz++)
                {
                    float x = (ix - 1.5f) * (W / 3.6f);
                    float z = (iz - 1f) * (D / 2.6f);
                    Cyl("Col_" + ix + "_" + iz, g.transform, new Vector3(x, 3.1f, z), 0.34f, 4.9f, wood);
                    Box("Shoe_" + ix + "_" + iz, g.transform, new Vector3(x, 0.78f, z),
                        new Vector3(1.0f, 0.22f, 1.0f), stone, false);
                }
            }

            // 墙：北侧与东西两侧为夯土，南侧开敞
            Box("Wall_N", g.transform, new Vector3(0f, 2.6f, D * 0.5f), new Vector3(W, 3.6f, 0.5f), rammed, true);
            for (int sx = -1; sx <= 1; sx += 2)
                Box("Wall_Side", g.transform, new Vector3(sx * W * 0.5f, 2.6f, 0f),
                    new Vector3(0.5f, 3.6f, D), rammed, true);

            // 正面矮栏：留出中央门道，让大厅读起来是"殿"而不是顶棚
            for (int seg = 0; seg < 2; seg++)
            {
                float cx = (seg == 0 ? -1f : 1f) * (W * 0.28f);
                Box("Rail_S_" + seg, g.transform, new Vector3(cx, 0.90f, -(D * 0.5f)),
                    new Vector3(W * 0.44f, 0.14f, 0.16f), woodLight, false);
                Box("Rail_S2_" + seg, g.transform, new Vector3(cx, 0.46f, -(D * 0.5f)),
                    new Vector3(W * 0.44f, 0.12f, 0.14f), wood, false);
                for (int i = 0; i < 3; i++)
                    Box("RailPost_" + seg + "_" + i, g.transform,
                        new Vector3(cx + (i - 1f) * (W * 0.17f), 0.45f, -(D * 0.5f)),
                        new Vector3(0.16f, 0.90f, 0.16f), wood, false);
            }

            // 外露木骨：贴在夯土墙外侧的壁柱与横梁，是良渚木构建筑的可读特征
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2f) * (W / 5.4f);
                for (int sz = -1; sz <= 1; sz += 2)
                    Box("Pilaster_N" + i + sz, g.transform,
                        new Vector3(x, 1.9f, sz * (D * 0.5f + 0.16f)),
                        new Vector3(0.26f, 3.4f, 0.20f), wood, false);
            }
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Box("Plate_N", g.transform, new Vector3(0f, 3.72f, sz * (D * 0.5f + 0.16f)),
                    new Vector3(W + 0.4f, 0.24f, 0.22f), woodLight, false);
                for (int i = 0; i < 3; i++)
                    Box("Pilaster_Side_" + sz + "_" + i, g.transform,
                        new Vector3(sz * (W * 0.5f + 0.16f), 1.9f, (i - 1) * (D / 3.4f)),
                        new Vector3(0.20f, 3.4f, 0.26f), wood, false);
                Box("Plate_Side_" + sz, g.transform, new Vector3(sz * (W * 0.5f + 0.16f), 3.72f, 0f),
                    new Vector3(0.22f, 0.24f, D + 0.4f), woodLight, false);
            }

            // 屋顶梁架
            for (int iz = 0; iz < 3; iz++)
                Box("Beam_" + iz, g.transform, new Vector3(0f, 5.02f, (iz - 1f) * (D / 2.6f)),
                    new Vector3(W + 0.6f, 0.28f, 0.28f), woodLight, false);

            // 屋顶：双坡屋脊沿建筑长轴，因此在一个绕 Y 90° 的局部空间里构建
            var roofG = Empty("RoofRoot", g.transform, new Vector3(0f, 5.30f, 0f));
            roofG.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            const float pitch = 5.4f;
            float rw = D + 3.2f;   // 局部 X：跨进深方向的坡跨
            float rd = W + 3.4f;   // 局部 Z：沿面阔方向的脊长
            float hwR = rw * 0.5f + 0.35f;
            float hdR = rd * 0.5f + 0.35f;

            MeshObject("Roof", roofG.transform, Vector3.zero,
                MeshFactory.GableRoof(rw, rd, pitch), thatch);
            var ridgeCap = Cyl("RidgeCap", roofG.transform, new Vector3(0f, pitch - 0.06f, 0f),
                               0.32f, hdR * 2f, thatch, false);
            ridgeCap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int sx = -1; sx <= 1; sx += 2)
                Box("Eave_" + sx, roofG.transform, new Vector3(sx * hwR, -0.10f, 0f),
                    new Vector3(0.26f, 0.40f, hdR * 2f), thatch, false);

            // 博风板：山墙两侧顺坡的封檐板，屋顶才不会看成一块悬空的板
            float bargeDeg = Mathf.Atan2(pitch, hwR) * Mathf.Rad2Deg;
            float bargeLen = Mathf.Sqrt(hwR * hwR + pitch * pitch);
            for (int sz = -1; sz <= 1; sz += 2)
            {
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    var board = Box("Barge_" + sz + "_" + sx, roofG.transform,
                        new Vector3(sx * hwR * 0.5f, pitch * 0.5f - 0.02f, sz * (hdR + 0.04f)),
                        new Vector3(bargeLen, 0.20f, 0.20f), woodLight, false);
                    board.transform.localRotation = Quaternion.Euler(0f, 0f, -sx * bargeDeg);
                }
            }

            // 斜梁贴于屋面之下，不穿出茅草
            float slopeDeg = Mathf.Atan2(pitch, hwR - 0.35f) * Mathf.Rad2Deg;
            float rafLen = Mathf.Sqrt(Mathf.Pow(hwR - 0.35f, 2f) + pitch * pitch) - 0.6f;
            for (int iz = 0; iz < 7; iz++)
            {
                float z = (iz - 3f) * (rd / 7.6f);
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    var raf = Box("Rafter", roofG.transform,
                        new Vector3(sx * hwR * 0.5f, pitch * 0.5f - 0.26f, z),
                        new Vector3(rafLen, 0.15f, 0.15f), wood, false);
                    raf.transform.localRotation = Quaternion.Euler(0f, 0f, -sx * slopeDeg);
                }
            }

            // 台面上散置陶器
            for (int i = 0; i < 3; i++)
                Cyl("Pot_" + i, g.transform, new Vector3(4.2f + i * 0.7f, 0.94f, -2.4f + i * 1.2f),
                    0.30f, 0.72f, pottery);
        }

        /// <summary>反山墓地：土丘、石台、玉琮与玉璧。</summary>
        static void BurialMound(Transform parent, Vector3 pos, Material earth, Material grass,
                                Material stone, Material jade, Material jadeDark)
        {
            var g = Empty("BurialMound", parent, pos);
            float y = LiangzhuHeight(pos.x, pos.z) - pos.y;

            Cyl("Mound0", g.transform, new Vector3(0f, y + 0.30f, 0f), 12.5f, 0.60f, earth);
            Cyl("Mound1", g.transform, new Vector3(0f, y + 0.78f, 0f), 10.2f, 0.56f, earth);
            Cyl("Mound2", g.transform, new Vector3(0f, y + 1.20f, 0f), 8.4f, 0.46f, grass);
            Cyl("Summit", g.transform, new Vector3(0f, y + 1.48f, 0f), 7.0f, 0.16f, stone);

            // 墓口石框
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * 90f + 45f;
                var kerb = Box("Kerb_" + i, g.transform,
                    new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 3.2f, y + 1.62f,
                                Mathf.Cos(a * Mathf.Deg2Rad) * 3.2f),
                    new Vector3(4.2f, 0.22f, 0.36f), stone, false);
                kerb.transform.localRotation = Quaternion.Euler(0f, a, 0f);
            }

            // 玉琮三件
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 1.5f;
                var cong = Empty("Cong_" + i, g.transform, new Vector3(x, y + 1.56f, -0.6f));
                // 玉琮只是摆放的器物，不需要碰撞：原来四节都带 collider，
                // 从北侧看这块线索时，准星先打到玉琮、打不到线索点的感应球。
                for (int k = 0; k < 4; k++)
                    Box("Seg_" + k, cong.transform, new Vector3(0f, 0.16f + k * 0.32f, 0f),
                        new Vector3(0.78f - k * 0.03f, 0.30f, 0.78f - k * 0.03f),
                        (k % 2 == 0) ? jade : jadeDark, false);
                Cyl("Hole", cong.transform, new Vector3(0f, 1.46f, 0f), 0.20f, 0.06f, jadeDark);
            }

            // 玉璧两件，平置于石台上
            for (int i = 0; i < 2; i++)
            {
                Cyl("Bi_" + i, g.transform, new Vector3(2.4f + i * 1.1f, y + 1.60f, 1.4f), 0.62f, 0.10f, jade);
                Cyl("BiHole_" + i, g.transform, new Vector3(2.4f + i * 1.1f, y + 1.67f, 1.4f), 0.20f, 0.06f, earth);
            }

            // 木质墓标
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * 360f + 20f;
                Cyl("Post_" + i, g.transform,
                    new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 5.4f, y + 1.9f,
                                Mathf.Cos(a * Mathf.Deg2Rad) * 5.4f), 0.12f, 1.4f, jadeDark);
            }

            CreateTextPlate(g.transform, "反 山 墓 地\n玉琮 · 玉璧 · 玉钺",
                new Vector3(0f, y + 2.55f, 5.6f), Vector3.zero,
                new Vector2(4.2f, 1.5f), 40, new Color(0.92f, 0.88f, 0.76f), title: true);
        }

        /// <summary>水利系统的土坝：夯土坝体 + 木桩护坡 + 草皮顶。</summary>
        static void Dam(Transform parent, Vector3 pos, float yaw, float length,
                        Material rammed, Material wood, Material grass)
        {
            var g = Empty("Dam", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box("Body", g.transform, new Vector3(0f, 0.95f, 0f), new Vector3(length, 6.0f, 11f), rammed, true);
            Box("Crest", g.transform, new Vector3(0f, 3.96f, 0f), new Vector3(length, 0.30f, 9.2f), grass, false);

            int n = Mathf.RoundToInt(length / 2.6f);
            for (int i = 0; i <= n; i++)
            {
                float x = (i / (float)n - 0.5f) * length;
                Cyl("Pile", g.transform, new Vector3(x, 1.55f, -5.4f), 0.16f, 2.2f, wood);
                Cyl("Pile2", g.transform, new Vector3(x, 1.35f, 5.4f), 0.16f, 1.8f, wood);
            }
        }

        /// <summary>湿地植被：乔木、竹林、芦苇与水岸灌丛。</summary>
        static void BuildWetlandFlora(Transform parent, Material leaf, Material pine, Material autumn,
                                      Material bark, Material reed)
        {
            int planted = 0;
            for (int i = 0; i < 320 && planted < 104; i++)
            {
                float x = (Rnd(i * 31 + 5) - 0.5f) * 250f;
                float z = (Rnd(i * 57 + 11) - 0.5f) * 240f + 12f;

                if (x > LiX0 - 6f && x < LiX1 + 6f && z > LiZ0 - 4f && z < LiZ1 + 4f) continue;
                if (Mathf.Abs(x) < 7f && z < 12f) continue;
                if (z > -50f && z < -37f) continue;
                if (z > 58f && z < 86f) continue;
                if (Mathf.Abs(z - 14.5f) < 5f) continue;
                if (z > -43f && z < -12f && Mathf.Abs(x) > 4f && Mathf.Abs(x) < 34f) continue;
                if (Mathf.Abs(x) > 40f && Mathf.Abs(x) < 58f && z > -24f && z < 8f) continue;

                float y = LiangzhuHeight(x, z);
                Material canopy = (i % 5 == 0) ? autumn : ((i % 3 == 0) ? pine : leaf);
                MakeTree(parent, new Vector3(x, y, z), i * 7 + 3, canopy, bark, 0.9f + Rnd(i * 13) * 0.6f);
                planted++;
            }

            // 聚落背后的一道林带：从入口望过去，屋顶后面是树不是一片淡色的坡。
            // 没有这层，整排房子像摆在空布景前面，纵深一下就塌了。
            for (int i = 0; i < 52; i++)
            {
                float x = (Rnd(i * 37 + 101) - 0.5f) * 108f;
                float z = 27f + Rnd(i * 53 + 17) * 20f;
                if (Mathf.Abs(x) < 7.5f) continue;
                if (LiangzhuHeight(x, z) > 3.4f) continue;
                float y = LiangzhuHeight(x, z);
                Material canopy = (i % 4 == 0) ? autumn : ((i % 3 == 0) ? pine : leaf);
                MakeTree(parent, new Vector3(x, y, z), i * 13 + 907, canopy, bark,
                    1.05f + Rnd(i * 29 + 7) * 0.75f);
            }

            for (int c = 0; c < 6; c++)
            {
                float x = (c % 3 - 1) * 44f + (Rnd(c * 17) - 0.5f) * 10f;
                float z = 4f + (c / 3) * 26f + (Rnd(c * 23) - 0.5f) * 8f;
                if (Mathf.Abs(x) < 8f) continue;
                BambooCluster(parent, new Vector3(x, LiangzhuHeight(x, z), z), leaf, bark, c);
            }

            // 河岸芦苇：两岸成排，近景留出芦苇框住水面
            for (int i = 0; i < 62; i++)
            {
                float x = (Rnd(i * 41 + 3) - 0.5f) * 118f;
                float z = (i % 2 == 0) ? -48.6f + Rnd(i * 7) * 1.6f : -39.4f - Rnd(i * 7) * 1.6f;
                if (Mathf.Abs(x) < 4.6f) continue;
                float ry = Mathf.Max(LiangzhuHeight(x, z), -0.10f);
                ReedCluster(parent, new Vector3(x, ry, z), reed, 0.9f + Rnd(i * 19) * 0.5f, i);
            }
            for (int i = 0; i < 20; i++)
            {
                float x = (Rnd(i * 71 + 13) - 0.5f) * 92f;
                float z = -50.6f + Rnd(i * 17) * 1.2f;
                if (Mathf.Abs(x) < 9f) continue;
                ReedCluster(parent, new Vector3(x, LiangzhuHeight(x, z), z), reed, 0.8f + Rnd(i * 23) * 0.4f, i + 80);
            }
            for (int i = 0; i < 22; i++)
            {
                float x = (Rnd(i * 61 + 9) - 0.5f) * 150f;
                float z = 64.5f + Rnd(i * 13) * 2.0f;
                ReedCluster(parent, new Vector3(x, LiangzhuHeight(x, z), z), reed, 0.8f + Rnd(i * 5) * 0.5f, i + 40);
            }

            // 地被：路肩与河岸的矮草与灌木，压住大片平铺的绿色
            for (int i = 0; i < 300; i++)
            {
                float z = -52f + Rnd(i * 13 + 1) * 58f;
                float side = (i % 2 == 0) ? 1f : -1f;
                float x = side * (4.6f + Rnd(i * 29 + 3) * 4.6f);
                if (Rnd(i * 37 + 5) > 0.62f) x = side * (30f + Rnd(i * 43 + 7) * 15f);
                if (z > -34f && z < -27f && Mathf.Abs(x) < 5.4f) continue;
                if (z > -42f && z < -13f && Mathf.Abs(x) < 9.5f) continue;
                var m = (i % 4 == 0) ? reed : leaf;
                GrassTuft(parent, new Vector3(x, LiangzhuHeight(x, z), z), m, 0.75f + Rnd(i * 53) * 0.7f, i);
            }
            // 城内河道两岸地被：码头一带的岸坡不种东西会露成大片裸土
            for (int i = 0; i < 320; i++)
            {
                float x = -33f + Rnd(i * 13 + 7) * 66f;
                bool north = i % 2 == 0;
                float z = north ? 17.9f + Rnd(i * 29 + 11) * 3.6f : 8.0f + Rnd(i * 29 + 11) * 2.9f;
                if (x > -22.6f && x < -13.2f && z < 11.4f) continue;   // 码头铺板与栈桥
                if (x > -12.6f && x < -8.4f && z < 10.6f) continue;    // 入河台阶
                var gm = (i % 3 == 0) ? reed : leaf;
                GrassTuft(parent, new Vector3(x, LiangzhuHeight(x, z), z), gm,
                    0.85f + Rnd(i * 61) * 0.8f, i + 400);
            }

            for (int i = 0; i < 26; i++)
            {
                float z = -52f + Rnd(i * 17 + 5) * 58f;
                float side = (i % 2 == 0) ? 1f : -1f;
                float x = side * (9.5f + Rnd(i * 23 + 9) * 20f);
                if (z > -42f && z < -13f) continue;
                if (x > 7.0f && x < 18.0f && z > -4.2f && z < 8.0f) continue;
                float y = LiangzhuHeight(x, z);
                float br = 1.4f + Rnd(i * 31) * 1.0f;
                MeshObject("Bush_" + i, parent, new Vector3(x, y + br * 0.26f, z),
                    MeshFactory.Canopy(br, i * 5 + 2), leaf);
            }
        }

        /// <summary>矮草丛：几片细叶构成的小簇，用于路肩地被。</summary>
        static void GrassTuft(Transform parent, Vector3 pos, Material m, float scale, int seed)
        {
            var g = Empty("Grass_" + seed, parent, pos);
            for (int i = 0; i < 5; i++)
            {
                float h = (0.26f + Rnd(seed * 11 + i * 3) * 0.26f) * scale;
                var b = Box("Blade_" + i, g.transform,
                    new Vector3((Rnd(seed * 7 + i) - 0.5f) * 0.30f, h * 0.5f,
                                (Rnd(seed * 13 + i) - 0.5f) * 0.30f),
                    new Vector3(0.035f, h, 0.06f), m, false);
                b.transform.localRotation = Quaternion.Euler(0f, Rnd(i * 19 + seed) * 180f,
                    (Rnd(i * 31 + seed) - 0.5f) * 22f);
            }
        }

        /// <summary>乔木：树干 + 两层不规则树冠。</summary>
        static void MakeTree(Transform parent, Vector3 pos, int seed, Material canopy, Material trunk, float scale)
        {
            var g = Empty("Tree_" + seed, parent, pos);
            float h = (3.0f + Rnd(seed) * 1.6f) * scale;
            Cyl("Trunk", g.transform, new Vector3(0f, h * 0.5f, 0f), 0.22f * scale, h, trunk);

            var r1 = (1.85f + Rnd(seed + 3) * 0.55f) * scale;
            MeshObject("Canopy", g.transform, new Vector3(0f, h + r1 * 0.55f, 0f),
                MeshFactory.Canopy(r1, seed * 7 + 3), canopy);
            MeshObject("Canopy2", g.transform,
                new Vector3((Rnd(seed + 9) - 0.5f) * 1.6f * scale, h + r1 * 1.15f, (Rnd(seed + 11) - 0.5f) * 1.4f * scale),
                MeshFactory.Canopy(r1 * 0.68f, seed * 11 + 5), canopy);
            if (seed % 3 == 0)
                MeshObject("Canopy3", g.transform, new Vector3((Rnd(seed + 13) - 0.5f) * 2.2f * scale, h + r1 * 0.6f, 0.7f * scale),
                    MeshFactory.Canopy(r1 * 0.6f, seed * 17 + 7), canopy);
        }

        /// <summary>竹林：细杆 + 窄叶簇。</summary>
        static void BambooCluster(Transform parent, Vector3 pos, Material leaf, Material bark, int seed)
        {
            var g = Empty("Bamboo_" + seed, parent, pos);
            for (int i = 0; i < 9; i++)
            {
                float x = (Rnd(seed * 13 + i * 7) - 0.5f) * 3.4f;
                float z = (Rnd(seed * 19 + i * 5) - 0.5f) * 3.4f;
                float h = 4.6f + Rnd(seed * 29 + i) * 2.6f;
                Cyl("Stalk_" + i, g.transform, new Vector3(x, h * 0.5f, z), 0.07f, h, bark);
                MeshObject("Tuft_" + i, g.transform, new Vector3(x, h * 0.80f, z),
                    MeshFactory.Canopy(0.95f + Rnd(i * 3) * 0.45f, seed * 31 + i), leaf);
                MeshObject("TuftLow_" + i, g.transform, new Vector3(x * 0.9f, h * 0.52f, z * 0.9f),
                    MeshFactory.Canopy(0.72f + Rnd(i * 5) * 0.3f, seed * 37 + i), leaf);
            }
        }

        /// <summary>芦苇丛。</summary>
        static void ReedCluster(Transform parent, Vector3 pos, Material reed, float scale, int seed)
        {
            var g = Empty("Reeds_" + seed, parent, pos);
            for (int i = 0; i < 14; i++)
            {
                float x = (Rnd(seed * 7 + i * 3) - 0.5f) * 1.5f * scale;
                float z = (Rnd(seed * 11 + i * 5) - 0.5f) * 1.5f * scale;
                float h = (1.05f + Rnd(seed * 17 + i) * 0.95f) * scale;
                var b = Box("Blade_" + i, g.transform, new Vector3(x, h * 0.5f, z),
                    new Vector3(0.032f, h, 0.075f), reed, false);
                b.transform.localRotation = Quaternion.Euler(0f, Rnd(i * 23 + seed) * 180f,
                    (Rnd(i * 29 + seed) - 0.5f) * 12f);
            }
        }

        /// <summary>
        /// 城外聚落组团：环绕一族干栏屋，配陶器、柴堆、晾架、火塘、竹篱与小路。
        /// 所有子构件的坐标都是世界坐标（各 helper 内部按世界坐标回查地形高度）。
        /// </summary>
        static void Hamlet(Transform parent, Vector3 origin, int count, float spread, float yawBase,
                           Material wood, Material woodLight, Material wall, Material thatch,
                           Material pottery, Material cloth, Material reed, Material road,
                           Material bark, int seed)
        {
            Empty("Hamlet_" + seed, parent, origin);

            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + (Rnd(seed * 5 + i) - 0.5f) * 0.55f;
                float rx = spread * (0.80f + Rnd(seed * 7 + i) * 0.42f);
                float rz = spread * (0.62f + Rnd(seed * 11 + i) * 0.40f);
                float px = origin.x + Mathf.Cos(a) * rx;
                float pz = origin.z + Mathf.Sin(a) * rz;
                float yaw = yawBase + (Rnd(seed * 13 + i) - 0.5f) * 76f;
                float sc = 0.82f + Rnd(seed * 17 + i) * 0.26f;

                StiltHouse(parent, new Vector3(px, 0f, pz), yaw, sc,
                           wood, woodLight, wall, thatch, seed * 100 + i);

                // 每户门外至少两件生活器物
                PotteryCluster(parent, new Vector3(px + 1.7f, 0f, pz + 1.3f), pottery, seed * 100 + i);
                if (i % 2 == 0)
                    FirewoodStack(parent, new Vector3(px - 2.1f, 0f, pz + 1.5f), wood, bark);
                else
                    DryingRack(parent, new Vector3(px - 1.9f, 0f, pz + 1.6f),
                        yaw + 90f, wood, cloth, reed);

                if (i == 1)
                    FirePit(parent, new Vector3(px + 0.6f, 0f, pz + 2.4f), wall, wood);

                // 通往组团中心的小路
                float segs = 5;
                for (int k = 0; k < segs; k++)
                {
                    float t = (k + 0.5f) / segs;
                    float sx = Mathf.Lerp(origin.x, px, t);
                    float sz = Mathf.Lerp(origin.z, pz, t);
                    var slab = Box("Path_" + i + "_" + k, parent,
                        new Vector3(sx, LiangzhuHeight(sx, sz) + 0.03f, sz),
                        new Vector3(1.15f, 0.06f, 1.15f), road, false);
                    slab.transform.localRotation = Quaternion.Euler(0f,
                        Mathf.Atan2(px - origin.x, pz - origin.z) * Mathf.Rad2Deg +
                        (Rnd(seed + k * 7) - 0.5f) * 14f, 0f);
                }
            }

            // 竹篱与草垛围出组团边界
            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f) * Mathf.PI * 2f + 0.6f;
                float fx = origin.x + Mathf.Cos(a) * (spread + 3.0f);
                float fz = origin.z + Mathf.Sin(a) * (spread + 3.0f);
                WoodFence(parent, new Vector3(fx, 0f, fz), 5.2f, a * Mathf.Rad2Deg,
                          wood, woodLight);
            }
            for (int i = 0; i < 5; i++)
                GrassTuft(parent,
                    new Vector3(origin.x + (Rnd(seed * 3 + i) - 0.5f) * (spread * 3.0f),
                                0f,
                                origin.z + (Rnd(seed * 9 + i) - 0.5f) * (spread * 3.0f)),
                    thatch, 0.9f + Rnd(i * 5 + seed) * 0.5f, seed * 50 + i);
        }
    }
}
