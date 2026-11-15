using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>序章山谷里的陈设与植被：石灯、香炉、石碑、树、竹。</summary>
    public static partial class HuaxiaBuilder
    {
        // ================================================================ 石灯

        static void BuildStoneLamp(Transform parent, Vector3 pos, Material stone, Material lamp,
                                  bool withLight = true)
        {
            var g = Empty("StoneLamp", parent, pos);

            Box("Base", g.transform, new Vector3(0f, 0.09f, 0f), new Vector3(0.54f, 0.18f, 0.54f), stone);
            Cyl("Shaft", g.transform, new Vector3(0f, 0.55f, 0f), 0.085f, 0.74f, stone);
            Cyl("Seat", g.transform, new Vector3(0f, 0.96f, 0f), 0.20f, 0.07f, stone);

            Box("FireBox", g.transform, new Vector3(0f, 1.20f, 0f), new Vector3(0.34f, 0.40f, 0.34f), lamp, false);
            float[] px = { -0.20f, 0.20f };
            foreach (float x in px)
                foreach (float z in px)
                    Box("Post", g.transform, new Vector3(x, 1.20f, z), new Vector3(0.07f, 0.44f, 0.07f), stone, false);

            MeshObject("Cap", g.transform, new Vector3(0f, 1.44f, 0f),
                MeshFactory.CurvedRoof(0.38f, 0.38f, 0.20f, 0.05f, 0.06f, 10, 10), stone, false);
            Ball("Pearl", g.transform, new Vector3(0f, 1.70f, 0f), 0.075f, stone);

            // 引路灯是「远看一点暖光」，不必每盏都挂点光源：序章点光源一多，批处理渲染会崩
            if (!withLight) return;

            var lgo = new GameObject("Light", typeof(Light));
            lgo.transform.SetParent(g.transform, false);
            lgo.transform.localPosition = new Vector3(0f, 1.20f, 0f);
            var l = lgo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = Lin(HuaxiaPalette.AmberPale);
            l.range = 6.5f;
            l.intensity = 1.05f;
            l.shadows = LightShadows.None;
        }

        // ================================================================ 香炉

        static void BuildCenser(Transform parent, Vector3 pos, Material bronze, Material stone)
        {
            var g = Empty("Censer", parent, pos);

            Box("Stand", g.transform, new Vector3(0f, 0.10f, 0f), new Vector3(0.96f, 0.20f, 0.96f), stone);
            var body = Ball("Bowl", g.transform, new Vector3(0f, 0.56f, 0f), 0.42f, bronze);
            // Ball() 已把 localScale 设为直径，这里按「扁圆鼎身」重新给三轴缩放
            body.transform.localScale = new Vector3(0.84f, 0.62f, 0.84f);
            Cyl("Rim", g.transform, new Vector3(0f, 0.88f, 0f), 0.34f, 0.10f, bronze);
            Cyl("Lid", g.transform, new Vector3(0f, 0.97f, 0f), 0.30f, 0.10f, bronze);
            Box("Handle_L", g.transform, new Vector3(-0.42f, 0.80f, 0f), new Vector3(0.10f, 0.26f, 0.09f), bronze, false);
            Box("Handle_R", g.transform, new Vector3(0.42f, 0.80f, 0f), new Vector3(0.10f, 0.26f, 0.09f), bronze, false);

            float[] lp = { -0.22f, 0.22f };
            foreach (float x in lp)
            {
                Box("Leg_X", g.transform, new Vector3(x, 0.24f, 0f), new Vector3(0.11f, 0.34f, 0.11f), bronze, false);
                Box("Leg_Z", g.transform, new Vector3(0f, 0.24f, x), new Vector3(0.11f, 0.34f, 0.11f), bronze, false);
            }
        }

        // ================================================================ 石碑

        static void BuildStele(Transform parent, Vector3 pos, float yaw, Material stone, string title, string body)
        {
            var g = Empty("Stele", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box("Base", g.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.70f, 0.30f, 0.72f), stone);
            Box("Body", g.transform, new Vector3(0f, 1.30f, 0f), new Vector3(1.22f, 1.96f, 0.26f), stone);
            MeshObject("Cap", g.transform, new Vector3(0f, 2.30f, 0f),
                MeshFactory.CurvedRoof(0.75f, 0.36f, 0.16f, 0.05f, 0.05f, 10, 8), stone, false);

            CreateTextPlate(g.transform, title, new Vector3(0f, 2.02f, -0.142f), Vector3.zero,
                new Vector2(0.86f, 0.22f), 56, HuaxiaPalette.AmberPale, title: true);
            // 碑身正文宽 0.98 m：0.90 m 时 13 个汉字正好卡在边界上，
            // 句末的「，」「。」会被挤到下一行单独成行。
            CreateTextPlate(g.transform, body, new Vector3(0f, 1.30f, -0.142f), Vector3.zero,
                new Vector2(0.98f, 1.02f), 34, HuaxiaPalette.PaperWarm);
        }

        // ================================================================ 树

        static void BuildTree(Transform parent, string name, Vector3 pos, float scale,
                              Material leaf, Material leafAlt, bool pine, int seed)
        {
            var g = Empty(name, parent, pos);
            var trunkMat = Mat("WoodPost", HuaxiaPalette.WoodBrown, 0f, 0.16f);

            if (pine)
            {
                Cyl("Trunk", g.transform, new Vector3(0f, 1.75f * scale, 0f), 0.17f * scale, 3.5f * scale, trunkMat);
                float[] levels = { 2.10f, 2.85f, 3.50f, 4.05f };
                float[] radii = { 2.05f, 1.62f, 1.20f, 0.78f };
                for (int i = 0; i < levels.Length; i++)
                {
                    var c = MeshObject("Layer_" + i, g.transform,
                        new Vector3((Rnd(seed + i) - 0.5f) * 0.4f, levels[i] * scale, (Rnd(seed + i * 3) - 0.5f) * 0.4f),
                        MeshFactory.Canopy(radii[i] * scale, seed * 7 + i), i % 2 == 0 ? leaf : leafAlt, false);
                    c.transform.localScale = new Vector3(1f, 0.52f, 1f);
                    c.transform.localRotation = Quaternion.Euler((Rnd(seed + i * 5) - 0.5f) * 9f, Rnd(seed + i) * 360f, 0f);
                }
            }
            else
            {
                Cyl("Trunk", g.transform, new Vector3(0f, 1.30f * scale, 0f), 0.20f * scale, 2.60f * scale, trunkMat);
                var b1 = Cyl("Branch_A", g.transform, new Vector3(0.42f * scale, 2.35f * scale, 0.10f * scale),
                    0.11f * scale, 1.70f * scale, trunkMat);
                b1.transform.localRotation = Quaternion.Euler(0f, 0f, -26f);
                var b2 = Cyl("Branch_B", g.transform, new Vector3(-0.40f * scale, 2.30f * scale, -0.14f * scale),
                    0.10f * scale, 1.60f * scale, trunkMat);
                b2.transform.localRotation = Quaternion.Euler(0f, 0f, 27f);

                var c1 = MeshObject("Canopy_A", g.transform, new Vector3(0f, 3.55f * scale, 0f),
                    MeshFactory.Canopy(2.30f * scale, seed * 5 + 1), leaf, false);
                c1.transform.localScale = new Vector3(1.05f, 0.78f, 1.05f);
                var c2 = MeshObject("Canopy_B", g.transform, new Vector3(0.95f * scale, 3.05f * scale, 0.55f * scale),
                    MeshFactory.Canopy(1.55f * scale, seed * 5 + 2), leafAlt, false);
                c2.transform.localScale = new Vector3(1f, 0.75f, 1f);
                var c3 = MeshObject("Canopy_C", g.transform, new Vector3(-0.85f * scale, 4.05f * scale, -0.45f * scale),
                    MeshFactory.Canopy(1.35f * scale, seed * 5 + 3), leaf, false);
                c3.transform.localScale = new Vector3(1f, 0.72f, 1f);
            }
        }

        // ================================================================ 竹

        static void BuildBamboo(Transform parent, Vector3 pos, Material leaf, float scale)
        {
            var g = Empty("Bamboo", parent, pos);
            var culmMat = Mat("BambooCulm", Color.Lerp(HuaxiaPalette.FoliageGreen, HuaxiaPalette.Sand, 0.45f), 0f, 0.12f);

            for (int i = 0; i < 9; i++)
            {
                float ox = (Rnd(i * 17 + 3) - 0.5f) * 2.2f;
                float oz = (Rnd(i * 29 + 5) - 0.5f) * 2.0f;
                float h = (3.6f + Rnd(i * 37) * 2.0f) * scale;
                var culm = Cyl("Culm_" + i, g.transform, new Vector3(ox, h * 0.5f, oz), 0.035f * scale, h, culmMat);
                culm.transform.localRotation = Quaternion.Euler((Rnd(i * 11) - 0.5f) * 5f, 0f, (Rnd(i * 13) - 0.5f) * 5f);

                var top = MeshObject("Leaf_" + i, g.transform, new Vector3(ox, h * 0.98f, oz),
                    MeshFactory.Canopy(0.62f * scale, 40 + i), leaf, false);
                top.transform.localScale = new Vector3(1f, 1.75f, 1f);
            }
        }
    }
}
