using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>序章山谷里的建筑构件：山门、古亭、残卷石案、水潭。</summary>
    public static partial class HuaxiaBuilder
    {
        static float Rnd(int seed)
        {
            uint h = (uint)(seed * 2654435761u);
            h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }

        // ================================================================ 山门牌坊

        static void BuildPaifang(Transform parent, Vector3 pos, Material wood, Material woodDark,
                                 Material roof, Material ridge, Material stone, Material lamp)
        {
            var g = Empty("Paifang", parent, pos);

            float[] xs = { -5.1f, -2.0f, 2.0f, 5.1f };
            foreach (float x in xs)
            {
                Box("Base", g.transform, new Vector3(x, 0.20f, 0f), new Vector3(0.92f, 0.40f, 0.92f), stone);
                Cyl("Post", g.transform, new Vector3(x, 2.72f, 0f), 0.21f, 5.04f, wood);
            }

            Box("Lintel", g.transform, new Vector3(0f, 4.88f, 0f), new Vector3(11.8f, 0.36f, 0.36f), woodDark);
            Box("LintelLow", g.transform, new Vector3(0f, 4.10f, 0f), new Vector3(11.8f, 0.22f, 0.28f), woodDark);

            MeshObject("Roof_Mid", g.transform, new Vector3(0f, 5.25f, 0f),
                MeshFactory.CurvedRoof(3.05f, 1.20f, 1.00f, 0.44f, 0.16f, 16, 10), roof, false);
            Box("Ridge_Mid", g.transform, new Vector3(0f, 6.10f, 0f), new Vector3(0.24f, 0.20f, 2.7f), ridge, false);
            Box("Finial_Mid", g.transform, new Vector3(0f, 6.30f, 0f), new Vector3(0.30f, 0.34f, 0.20f), ridge, false);

            MeshObject("Roof_L", g.transform, new Vector3(-3.55f, 4.48f, 0f),
                MeshFactory.CurvedRoof(1.95f, 0.98f, 0.76f, 0.34f, 0.14f, 14, 8), roof, false);
            MeshObject("Roof_R", g.transform, new Vector3(3.55f, 4.48f, 0f),
                MeshFactory.CurvedRoof(1.95f, 0.98f, 0.76f, 0.34f, 0.14f, 14, 8), roof, false);

            Box("Plaque", g.transform, new Vector3(0f, 4.36f, -0.32f), new Vector3(2.6f, 0.64f, 0.10f), woodDark);
            CreateTextPlate(g.transform, "华 夏 文 明 探 源", new Vector3(0f, 4.36f, -0.385f), Vector3.zero,
                new Vector2(2.3f, 0.52f), 58, HuaxiaPalette.PaperWhite, title: true);

            BuildHangingLantern(g.transform, new Vector3(-3.55f, 3.62f, 0f), lamp);
            BuildHangingLantern(g.transform, new Vector3(3.55f, 3.62f, 0f), lamp);
        }

        static void BuildHangingLantern(Transform parent, Vector3 pos, Material lamp)
        {
            var g = Empty("Lantern", parent, pos);
            g.transform.localPosition = pos;

            Cyl("Cord", g.transform, new Vector3(0f, 0.34f, 0f), 0.012f, 0.68f, lamp);
            var body = Ball("Body", g.transform, Vector3.zero, 0.24f, lamp);
            body.transform.localScale = new Vector3(0.46f, 0.58f, 0.46f);
            Cyl("CapTop", g.transform, new Vector3(0f, 0.25f, 0f), 0.10f, 0.06f, lamp);
            Cyl("CapBot", g.transform, new Vector3(0f, -0.25f, 0f), 0.10f, 0.06f, lamp);

            var lgo = new GameObject("Light", typeof(Light));
            lgo.transform.SetParent(g.transform, false);
            var l = lgo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = Lin(HuaxiaPalette.AmberPale);
            l.range = 7f;
            l.intensity = 1.15f;
            l.shadows = LightShadows.None;
        }

        // ================================================================ 古亭

        static Transform BuildPavilion(Transform parent, Vector3 pos, float yaw, Material wood, Material woodDark,
                                  Material roof, Material ridge, Material stone, Material bronze, Material paper)
        {
            var g = Empty("Pavilion", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box("Platform", g.transform, new Vector3(0f, 0.20f, 0f), new Vector3(6.4f, 0.40f, 5.6f), stone);
            Box("Step_A", g.transform, new Vector3(0f, 0.16f, -3.02f), new Vector3(3.2f, 0.32f, 0.58f), stone);
            Box("Step_B", g.transform, new Vector3(0f, 0.09f, -3.48f), new Vector3(3.2f, 0.18f, 0.52f), stone);
            Box("Step_C", g.transform, new Vector3(0f, 0.04f, -3.88f), new Vector3(3.2f, 0.08f, 0.46f), stone);

            float[] cx = { -2.15f, 2.15f };
            float[] cz = { -1.85f, 1.85f };
            foreach (float x in cx)
                foreach (float z in cz)
                {
                    Cyl("Foot", g.transform, new Vector3(x, 0.46f, z), 0.25f, 0.13f, stone);
                    Cyl("Column", g.transform, new Vector3(x, 1.90f, z), 0.17f, 2.94f, wood);
                    Box("Bracket_A", g.transform, new Vector3(x, 3.42f, z), new Vector3(0.54f, 0.14f, 0.36f), woodDark, false);
                    Box("Bracket_B", g.transform, new Vector3(x, 3.54f, z), new Vector3(0.36f, 0.12f, 0.54f), woodDark, false);
                }

            Box("Beam_F", g.transform, new Vector3(0f, 3.32f, -1.85f), new Vector3(5.0f, 0.26f, 0.24f), woodDark);
            Box("Beam_B", g.transform, new Vector3(0f, 3.32f, 1.85f), new Vector3(5.0f, 0.26f, 0.24f), woodDark);
            Box("Beam_L", g.transform, new Vector3(-2.15f, 3.32f, 0f), new Vector3(0.24f, 0.24f, 4.1f), woodDark);
            Box("Beam_R", g.transform, new Vector3(2.15f, 3.32f, 0f), new Vector3(0.24f, 0.24f, 4.1f), woodDark);

            MeshObject("Roof", g.transform, new Vector3(0f, 3.62f, 0f),
                MeshFactory.CurvedRoof(3.50f, 3.05f, 1.32f, 0.56f, 0.18f, 20, 18), roof, false);
            Box("RidgeLine", g.transform, new Vector3(0f, 4.92f, 0f), new Vector3(0.26f, 0.22f, 5.4f), ridge, false);
            Cyl("Finial", g.transform, new Vector3(0f, 5.14f, 0f), 0.13f, 0.30f, ridge, false);
            Ball("FinialBall", g.transform, new Vector3(0f, 5.36f, 0f), 0.15f, ridge);

            Box("Plaque", g.transform, new Vector3(0f, 3.34f, -1.99f), new Vector3(1.85f, 0.52f, 0.09f), woodDark);
            CreateTextPlate(g.transform, "问 道 亭", new Vector3(0f, 3.34f, -2.05f), Vector3.zero,
                new Vector2(1.6f, 0.44f), 62, HuaxiaPalette.AmberPale, title: true);

            _ = bronze;
            _ = paper;
            return g.transform;
        }

        // ================================================================ 残卷石案
        // 亭里的陈设，纯布景：不挂 Interactable、不放触发器。
        // 亭子现在是广场边上的装饰，走近不该浮出「按 E」。

        static void BuildScrollTable(Transform parent, Vector3 pos, Material stone, Material bronze, Material paper)
        {
            var g = Empty("ScrollTable", parent, pos);

            Box("Top", g.transform, new Vector3(0f, 0.94f, 0f), new Vector3(2.95f, 0.14f, 1.38f), stone);
            Box("Leg_L", g.transform, new Vector3(-1.15f, 0.47f, 0f), new Vector3(0.30f, 0.94f, 1.06f), stone);
            Box("Leg_R", g.transform, new Vector3(1.15f, 0.47f, 0f), new Vector3(0.30f, 0.94f, 1.06f), stone);

            Box("Scroll", g.transform, new Vector3(0f, 1.02f, 0f), new Vector3(2.52f, 0.02f, 1.06f), paper, false);
            Box("Weight_L", g.transform, new Vector3(-1.16f, 1.06f, 0f), new Vector3(0.11f, 0.07f, 1.02f), bronze, false);
            Box("Weight_R", g.transform, new Vector3(1.16f, 1.06f, 0f), new Vector3(0.11f, 0.07f, 1.02f), bronze, false);
            Box("Seal", g.transform, new Vector3(-0.98f, 1.05f, 0.36f), new Vector3(0.09f, 0.05f, 0.09f),
                EmissiveMat("SealCinnabar", HuaxiaPalette.Cinnabar, HuaxiaPalette.Cinnabar * 0.8f, 0f, 0.3f), false);

            CreateTextPlate(g.transform, "华 夏 图 谱 · 残 卷", new Vector3(0f, 1.045f, -0.16f),
                new Vector3(90f, 0f, 0f), new Vector2(1.9f, 0.34f), 52, HuaxiaPalette.InkTealDeep, title: true);

        }

        // ================================================================ 水潭

        static void BuildPond(Transform parent, Vector3 pos, Material water, Material stone, Material leaf)
        {
            var g = Empty("Pond", parent, pos);

                        // 水面与地面齐平：池底压到地面以下，避免池底/池沿把水面挡住
            var pondBed = Box("Bed", g.transform, new Vector3(0f, -0.34f, 0f), new Vector3(10.6f, 0.02f, 7.6f),
                Mat("PondBed", Color.Lerp(HuaxiaPalette.InkTealDeep, HuaxiaPalette.Azurite, 0.35f), 0f, 0.1f), false);
            var pondSurf = Box("Surface", g.transform, new Vector3(0f, 0.045f, 0f), new Vector3(10.4f, 0.03f, 7.4f), water, false);
            // 水面层：点光源的 ForwardAdd 不乘 alpha，打在半透明水面上会叠出屏幕对齐的浅色块
            pondBed.layer = WaterLayer;
            pondSurf.layer = WaterLayer;

            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                float rx = 5.75f + Rnd(i * 31) * 0.55f;
                float rz = 4.30f + Rnd(i * 57 + 7) * 0.50f;
                var p = new Vector3(Mathf.Cos(a) * rx, 0.02f + Rnd(i) * 0.08f, Mathf.Sin(a) * rz);
                var rock = Box("Bank_" + i, g.transform, p,
                    new Vector3(0.85f + Rnd(i * 13) * 0.65f, 0.42f + Rnd(i * 17) * 0.30f, 0.70f + Rnd(i * 23) * 0.55f),
                    stone);
                rock.transform.localRotation = Quaternion.Euler(Rnd(i * 7) * 16f, a * Mathf.Rad2Deg, Rnd(i * 11) * 14f);
            }

            var lotusMat = Mat("LotusPetal", Color.Lerp(HuaxiaPalette.PaperWhite, HuaxiaPalette.Terracotta, 0.35f), 0f, 0.12f);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3((Rnd(i * 41) - 0.5f) * 8.4f, 0.055f, (Rnd(i * 53 + 3) - 0.5f) * 6.0f);
                var pad = Cyl("LotusPad_" + i, g.transform, p, 0.26f + Rnd(i * 61) * 0.20f, 0.02f, leaf);
                pad.transform.localRotation = Quaternion.Euler((Rnd(i) - 0.5f) * 8f, Rnd(i * 3) * 360f, (Rnd(i * 5) - 0.5f) * 8f);
            }
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3((Rnd(i * 71 + 5) - 0.5f) * 7.0f, 0.13f, (Rnd(i * 83 + 9) - 0.5f) * 5.0f);
                var bud = Ball("LotusBud_" + i, g.transform, p, 0.11f, lotusMat);
                // 覆盖 localScale 会丢掉 Ball 按半径设定的尺寸，必须相乘
                bud.transform.localScale = Vector3.Scale(bud.transform.localScale, new Vector3(0.85f, 1.6f, 0.85f));
            }
        }
    }
}
