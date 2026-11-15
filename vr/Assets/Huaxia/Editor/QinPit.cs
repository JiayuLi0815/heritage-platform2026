using System.Collections.Generic;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 一号坑（兵马俑坑）。
    ///
    /// 实测：东西 230 m、南北 62 m、深约 5 m；坑内 10 道夯土隔墙隔出 11 条过洞
    /// （过洞宽约 3.5 m、隔墙厚 2–2.5 m、原高 3.2 m）；东西两端各有 5 条斜坡门道，
    /// 长 15–20 m、宽 3.8–6.6 m。坑底铺青砖，隔墙顶自下而上是
    /// 地栿 → 立柱（每 1.4 m 一根）→ 枋木 → 棚木（密排，直径可达 68 cm），
    /// 棚木上铺苇席、胶泥、黄土封顶，地面平平整整看不出下面有东西。
    ///
    /// 这里把东西向按 1:2 压到 76 × 33 m，隔墙缩到 5 道、过洞 6 条、门道 5 条，
    /// 只缩短长度，不动剖面关系。局部坐标：坑心为原点，青砖地面 y≈0.2，
    /// +X 向东（陶俑面向东），+Z 向北。
    ///
    /// 顶棚只保留西端 25 m 的一段——那是原坑唯一还压着棚木的部分；
    /// 以东全部揭开，玩家从东端 5 条门道走进去，站在坑底看整座军阵。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        const float QpLen = 76f;         // 东西（原 230 m，压缩）
        const float QpWid = 33f;         // 南北（原 62 m，压缩）
        const float QpDepth = 5.0f;      // 地表到坑底
        const float QpWallT = 2.4f;      // 隔墙厚（原 2–2.5 m）
        const int QpWalls = 5;           // → 6 条过洞
        const float QpWallH = 3.2f;      // 隔墙顶（原高 3.2 m），地栿就搁在这上面
        const float QpStructX1 = 22f;    // 隔墙东端 = 门道坡脚
        const float QpRampX1 = 38.4f;    // 门道上口（坑沿）
        const float QpRoofX1 = -13f;     // 棚木保存段的东端，以东全部揭开
        const float QpHalfX = QpLen * 0.5f;
        const float QpHalfZ = QpWid * 0.5f;
        const float QpRimY = 5.0f;       // 坑沿 = 原地表
        const float QpFloorTop = 0.20f;  // 青砖地面标高

        // 隔墙顶以上的四层木构（相对坑底）
        const float QpSillY = 3.20f;     // 地栿 3.20–3.42
        const float QpPostY = 3.42f;     // 立柱 3.42–4.04
        const float QpFangY = 4.04f;     // 枋木 4.04–4.26
        const float QpBeamY = 4.26f;     // 棚木 4.26–4.68
        const float QpFillY = 4.68f;     // 苇席、胶泥、黄土 4.68–5.02

        static float QpCorridorW => (QpWid - QpWalls * QpWallT) / (QpWalls + 1f);

        /// <summary>第 c 条过洞（0..5）的南北中心。</summary>
        static float QpCorridorZ(int c) => -QpHalfZ + QpCorridorW * (c + 0.5f) + QpWallT * c;

        /// <summary>第 i 道隔墙（0..4）的南北中心。</summary>
        static float QpWallZ(int i) => -QpHalfZ + QpCorridorW * (i + 1) + QpWallT * (i + 0.5f);

        public static void QinPit(Transform parent, Vector3 center,
                                  Material rammed, Material rammedDark, Material rammedPale, Material earth,
                                  Material brick, Material timber, Material timberPale,
                                  Material terracotta, Material terracottaDark)
        {
            // 地形已在坑口矩形内压平到 -5 m，这里直接以坑底为局部零点
            float fy = QinlingHeight(center.x, center.z);
            var g = Empty("Pit_K1", parent, new Vector3(center.x, fy, center.z));
            var t = g.transform;

            PitShell(t, rammed, rammedDark, rammedPale, earth, brick);
            PitTimber(t, timber, timberPale, earth);
            PitRamps(t, earth, rammed, rammedPale);
            int figures = PitArmy(t, terracotta, terracottaDark, timber, timberPale);
            PitDebris(t, rammedPale, terracottaDark, brick);

            Debug.Log("[Huaxia] 一号坑：" + (QpWalls + 1) + " 过洞 / " + QpWalls + " 隔墙 / "
                      + (QpWalls + 1) + " 条东门道 / " + figures + " 尊陶俑位");
        }

        // ------------------------------------------------------------------ 坑体

        static void PitShell(Transform t, Material rammed, Material rammedDark,
                             Material rammedPale, Material earth, Material brick)
        {
            // 青砖地面：原坑铺砖，砖缝细密，这里用整块砖面（贴图已经带缝）
            Box("PitFloor", t, new Vector3(0f, QpFloorTop - 0.10f, 0f),
                new Vector3(QpLen + 1.8f, 0.20f, QpWid + 1.8f), brick, true);

            // 四壁：把地形被挖开的毛边压平成一层夯土壁
            const float wt = 0.40f;
            float wy = (QpRimY - 0.20f) * 0.5f;   // 顶面正好压在地表
            float wh = QpRimY + 0.20f;
            Box("PitWall_S", t, new Vector3(0f, wy, -QpHalfZ - wt * 0.5f),
                new Vector3(QpLen + 1.6f, wh, wt), rammed, true);
            Box("PitWall_N", t, new Vector3(0f, wy, QpHalfZ + wt * 0.5f),
                new Vector3(QpLen + 1.6f, wh, wt), rammed, true);
            Box("PitWall_W", t, new Vector3(-QpHalfX - wt * 0.5f, wy, 0f),
                new Vector3(wt, wh, QpWid + 1.2f), rammed, true);

            // 坑沿压边：一圈略高出地面的土坎，远看能认出"这里有个坑"
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Rim_Z", t, new Vector3(0f, QpRimY + 0.02f, s * (QpHalfZ + 1.05f)),
                    new Vector3(QpLen + 3.8f, 0.30f, 1.7f), rammedPale, true);
                Box("Rim_W", t, new Vector3(s * (QpHalfX + 1.05f), QpRimY + 0.02f, 0f),
                    new Vector3(1.7f, 0.30f, QpWid + 2.4f), rammedPale, true);
            }

            // ---- 隔墙
            // 结构段：高 3.2 m，上面承木构；东段（门道之间）是没动过的生土，
            // 一直顶到地表——它就是 5 条门道的侧壁，也是坑东端那道 5 m 高的断面。
            float lenW = QpStructX1 - (-QpHalfX);
            float cxW = (QpStructX1 + (-QpHalfX)) * 0.5f;
            float lenE = QpRampX1 - QpStructX1;
            float cxE = (QpRampX1 + QpStructX1) * 0.5f;

            for (int i = 0; i < QpWalls; i++)
            {
                float z = QpWallZ(i);
                var m = (i % 2 == 0) ? rammed : rammedDark;
                Box("Partition_" + i, t, new Vector3(cxW, QpWallH * 0.5f, z),
                    new Vector3(lenW, QpWallH, QpWallT), m, true);
                // 墙顶压一道浅色收边，读作"生土墙包了夯土"
                Box("PartitionCap_" + i, t, new Vector3(cxW, QpWallH + 0.06f, z),
                    new Vector3(lenW, 0.12f, QpWallT + 0.10f), rammedPale, false);
                // 门道之间的生土台
                Box("PartitionE_" + i, t, new Vector3(cxE, QpRimY * 0.5f, z),
                    new Vector3(lenE, QpRimY, QpWallT), m, true);
            }

            // 东端两条边的生土台：门道贴着南北坑壁，但角上要补一小块，不然能看穿
            for (int s = -1; s <= 1; s += 2)
                Box("PartitionE_Edge", t,
                    new Vector3(cxE, QpRimY * 0.5f, s * (QpHalfZ + 0.20f)),
                    new Vector3(lenE, QpRimY, 0.40f), rammedDark, true);
        }

        // ------------------------------------------------------------------ 木构

        static void PitTimber(Transform t, Material timber, Material timberPale, Material earth)
        {
            // ---- 地栿：每道隔墙两侧各一根通长方木，压在墙顶（全长都有）
            float lenW = QpStructX1 - (-QpHalfX);
            float cxW = (QpStructX1 + (-QpHalfX)) * 0.5f;
            for (int i = 0; i < QpWalls; i++)
            {
                float z = QpWallZ(i);
                for (int s = -1; s <= 1; s += 2)
                    Box("Sill_" + i + "_" + s, t,
                        new Vector3(cxW, QpSillY + 0.11f, z + s * (QpWallT * 0.5f - 0.20f)),
                        new Vector3(lenW, 0.22f, 0.32f), timber, false);
            }
            // 两侧外墙的地栿
            for (int s = -1; s <= 1; s += 2)
                Box("SillOut_" + s, t,
                    new Vector3(cxW, QpSillY + 0.11f, s * (QpHalfZ - 0.28f)),
                    new Vector3(lenW, 0.22f, 0.32f), timber, false);

            // ---- 立柱：有棚木的一段按 1.4 m 一根排满
            for (float x = -QpHalfX + 1.0f; x < QpRoofX1 - 0.4f; x += 1.40f)
            {
                for (int i = -1; i <= QpWalls; i++)
                {
                    float z;
                    if (i < 0) z = -(QpHalfZ - 0.28f);
                    else if (i >= QpWalls) z = QpHalfZ - 0.28f;
                    else z = QpWallZ(i);
                    float off = (i < 0 || i >= QpWalls) ? 0f : (QpWallT * 0.5f - 0.20f);
                    int n = (off > 0.01f) ? 2 : 1;
                    for (int k = 0; k < n; k++)
                    {
                        float zz = z + (n == 2 ? (k == 0 ? -off : off) : 0f);
                        Cyl("Post", t, new Vector3(x, QpPostY + 0.31f, zz), 0.115f, 0.62f, timber);
                    }
                }
            }

            // ---- 枋木：压在立柱上的纵向长木，只在有顶的一段
            float lenR = QpRoofX1 - (-QpHalfX);
            float cxR = (QpRoofX1 + (-QpHalfX)) * 0.5f;
            for (int i = 0; i < QpWalls; i++)
            {
                float z = QpWallZ(i);
                for (int s = -1; s <= 1; s += 2)
                    Box("Fang_" + i + "_" + s, t,
                        new Vector3(cxR, QpFangY + 0.11f, z + s * (QpWallT * 0.5f - 0.20f)),
                        new Vector3(lenR, 0.22f, 0.28f), timberPale, false);
            }
            for (int s = -1; s <= 1; s += 2)
                Box("FangOut_" + s, t,
                    new Vector3(cxR, QpFangY + 0.11f, s * (QpHalfZ - 0.28f)),
                    new Vector3(lenR, 0.22f, 0.28f), timberPale, false);

            // ---- 棚木：横跨过洞的密排圆木，直径 0.40，中心到中心 0.52
            for (float x = -QpHalfX + 0.5f; x < QpRoofX1; x += 0.52f)
            {
                var beam = Box("Beam", t,
                    new Vector3(x, QpBeamY + 0.21f, 0f),
                    new Vector3(0.40f, 0.42f, QpWid + 0.5f), timber, false);
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, (Rnd((int)(x * 10f)) - 0.5f) * 1.6f);
            }

            // ---- 揭开的顶：棚木原样露着，只最西端留一小块还压着苇席、胶泥、黄土，
            //      让人一眼看出"下面这层木头本来是封死的"。
            //      （原来在这里铺一整块土板，远看就是一张悬空的桌子，已删。）
            Box("Cap", t, new Vector3(-QpHalfX + 2.6f, QpFillY + 0.14f, 0f),
                new Vector3(5.2f, 0.30f, QpWid + 0.4f), earth, true);
            Box("CapMat", t, new Vector3(-QpHalfX + 2.6f, QpFillY - 0.02f, 0f),
                new Vector3(5.4f, 0.07f, QpWid + 0.4f), timber, false);

            // ---- 塌落段：被土压折、断在坑里的棚木
            for (int i = 0; i < 7; i++)
            {
                float x = QpRoofX1 + (Rnd(310 + i) - 0.5f) * 3.0f;
                float zc = -QpHalfZ + 2f + Rnd(320 + i) * (QpWid - 4f);
                float l = 3.0f + Rnd(330 + i) * 4.5f;
                var b = Box("BeamBroken", t,
                    new Vector3(x, QpFangY + 0.05f, zc),
                    new Vector3(l, 0.30f, 0.34f), timber, false);
                b.transform.localRotation = Quaternion.Euler(
                    (Rnd(340 + i) - 0.5f) * 14f, (Rnd(350 + i) - 0.5f) * 40f, (Rnd(360 + i) - 0.5f) * 24f);
            }

            // ---- 揭顶段：剩下的立柱残桩
            for (float x = QpRoofX1 + 2.0f; x < QpStructX1 - 1.0f; x += 4.2f)
            {
                for (int i = -1; i <= QpWalls; i++)
                {
                    float z = (i < 0) ? -(QpHalfZ - 0.28f)
                            : (i >= QpWalls) ? QpHalfZ - 0.28f
                            : QpWallZ(i);
                    float h = 0.10f + Rnd((int)(x * 7f) + i * 13 + 5) * 0.34f;
                    Cyl("PostStub", t, new Vector3(x, QpPostY + h * 0.5f, z), 0.115f, h, timber);
                }
            }
        }

        // ------------------------------------------------------------------ 门道

        static void PitRamps(Transform t, Material earth, Material rammed, Material rammedPale)
        {
            float run = QpRampX1 - QpStructX1;
            float ang = Mathf.Atan2(QpRimY - QpFloorTop, run) * Mathf.Rad2Deg;
            float slope = run / Mathf.Cos(ang * Mathf.Deg2Rad);

            for (int c = 0; c < QpWalls + 1; c++)
            {
                float z = QpCorridorZ(c);
                float w = QpCorridorW - 0.10f;
                // 坡道面：从坑底 x=22 起，坡度 1:3.3，一直升到 x=38.4 的坑沿
                var deck = Box("Ramp", t,
                    new Vector3((QpStructX1 + QpRampX1) * 0.5f,
                                (QpRimY + QpFloorTop) * 0.5f - 0.16f, z),
                    new Vector3(slope, 0.32f, w), earth, true);
                deck.transform.localRotation = Quaternion.Euler(0f, 0f, ang);

                // 坡道两侧的夯土帮壁（补齐生土台与坡面之间的缝）
                for (int s = -1; s <= 1; s += 2)
                {
                    var bank = Box("RampBank", t,
                        new Vector3((QpStructX1 + QpRampX1) * 0.5f,
                                    (QpRimY + QpFloorTop) * 0.5f + 0.30f, z + s * (w * 0.5f + 0.16f)),
                        new Vector3(slope, 0.40f, 0.32f), rammed, true);
                    bank.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                }

                // 门道口两侧的短挡墙：原坑门道口有封门木，这里只留土坎
                for (int s = -1; s <= 1; s += 2)
                    Box("RampLip", t,
                        new Vector3(QpRampX1 - 0.6f, QpRimY - 0.16f, z + s * (w * 0.5f + 0.42f)),
                        new Vector3(1.2f, 0.6f, 0.5f), rammedPale, false);
            }
        }

        // ------------------------------------------------------------------ 军阵

        static int PitArmy(Transform t, Material terracotta, Material terracottaDark,
                           Material timber, Material timberPale)
        {
            var figParent = Empty("Figures", t, Vector3.zero).transform;
            int figIndex = 0;
            float x0 = -QpHalfX + 3.0f, x1 = QpStructX1 - 3.2f;
            const float rowStep = 1.30f;

            for (int c = 0; c < QpWalls + 1; c++)
            {
                float cz = QpCorridorZ(c);
                float cw = QpCorridorW;
                bool flank = (c == 0 || c == QpWalls);      // 南北两翼：重装

                // 车组的位置先算出来，纵队要给车让位
                var cartXs = new List<float>();
                for (float bx = x0 + 5f; bx < x1 - 5f; bx += 15f) cartXs.Add(bx);

                for (int f = 0; f < 3; f++)                  // 每条过洞 3 路纵队
                {
                    float fz = cz + (f - 1) * (cw / 3.3f);
                    for (float x = x0; x <= x1; x += rowStep)
                    {
                        float r = Rnd(figIndex * 17 + c * 5 + 3);
                        figIndex++;
                        if (r < 0.085f) continue;            // 原坑大量残失，太齐会假

                        // 中间一路给车组让开 5 m
                        if (f == 1 && NearAny(x, cartXs, 4.6f)) continue;

                        bool front = x > x1 - 3.5f;          // 前锋：轻装
                        int variant = front ? 2
                                    : (r > 0.94f) ? 4        // 军吏
                                    : (r > 0.46f) ? 0 : 1;
                        if (flank && variant == 2) variant = 0;

                        float jx = x + (Rnd(figIndex * 31 + 7) - 0.5f) * 0.24f;
                        float jz = fz + (Rnd(figIndex * 43 + 11) - 0.5f) * 0.22f;
                        float yaw = (Rnd(figIndex * 53 + 13) - 0.5f) * 9f;   // 面朝东

                        var go = new GameObject("Warrior");
                        go.transform.SetParent(figParent, false);
                        go.transform.localPosition = new Vector3(jx, QpFloorTop, jz);
                        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        go.AddComponent<MeshFilter>().sharedMesh = FigureMesh(variant);
                        go.AddComponent<MeshRenderer>().sharedMaterial =
                            (Rnd(figIndex * 61 + 5) > 0.55f) ? terracottaDark : terracotta;
                        go.isStatic = true;

                        // 少量倒伏俑
                        if (r > 0.962f)
                            go.transform.localRotation = Quaternion.Euler(80f, yaw, 0f);
                    }
                }

                foreach (float bx in cartXs)
                    ChariotGroup(t, new Vector3(bx, QpFloorTop, cz), terracotta, timber, timberPale, terracottaDark);
            }

            return figIndex;
        }

        static bool NearAny(float x, List<float> xs, float d)
        {
            for (int i = 0; i < xs.Count; i++)
                if (Mathf.Abs(x - xs[i]) < d) return true;
            return false;
        }

        // ------------------------------------------------------------------ 散落物

        static void PitDebris(Transform t, Material pale, Material dark, Material brick)
        {
            var d = Empty("Debris", t, Vector3.zero).transform;
            for (int i = 0; i < 90; i++)
            {
                float x = -QpHalfX + 2f + Rnd(500 + i) * (QpLen - 4f);
                if (x > QpRampX1 - 1f) continue;
                float z = -QpHalfZ + 1.5f + Rnd(600 + i) * (QpWid - 3f);
                float s = 0.10f + Rnd(700 + i) * 0.22f;
                var b = Box("Sherd", d,
                    new Vector3(x, QpFloorTop + 0.02f, z),
                    new Vector3(s * 1.7f, s * 0.22f, s), (Rnd(800 + i) > 0.5f) ? dark : pale, false);
                b.transform.localRotation = Quaternion.Euler(0f, Rnd(900 + i) * 180f, 0f);
            }
        }

        // ------------------------------------------------------------------ 战车

        static void ChariotGroup(Transform parent, Vector3 pos,
                                 Material terracotta, Material timber, Material timberPale, Material dark)
        {
            var g = Empty("ChariotGroup", parent, pos);
            // 车辕朝东（+X），与整座军阵同向

            // 舆（车厢）
            Box("Box", g.transform, new Vector3(0f, 0.72f, 0f), new Vector3(1.06f, 0.80f, 1.36f), timber, false);
            Box("BoxBack", g.transform, new Vector3(-0.50f, 1.02f, 0f), new Vector3(0.10f, 0.62f, 1.32f), timberPale, false);
            Box("BoxRail", g.transform, new Vector3(0f, 1.10f, 0f), new Vector3(1.04f, 0.08f, 1.38f), timberPale, false);
            Box("BoxSide", g.transform, new Vector3(0.48f, 0.96f, 0f), new Vector3(0.10f, 0.44f, 1.30f), timberPale, false);

            // 双轮
            for (int s = -1; s <= 1; s += 2)
            {
                var w = Cyl("Wheel", g.transform, new Vector3(0.04f, 0.72f, s * 0.82f), 0.72f, 0.13f, timber);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Box("Hub", g.transform, new Vector3(0.04f, 0.72f, s * 0.88f), new Vector3(0.16f, 0.20f, 0.10f), timberPale, false);
                for (int k = 0; k < 4; k++)               // 辐条
                {
                    var sp = Box("Spoke", g.transform, new Vector3(0.04f, 0.72f, s * 0.80f),
                        new Vector3(1.30f, 0.055f, 0.05f), timberPale, false);
                    sp.transform.localRotation = Quaternion.Euler(0f, 0f, k * 45f);
                }
            }

            // 轴、辕、衡
            Box("Axle", g.transform, new Vector3(0.04f, 0.72f, 0f), new Vector3(0.16f, 0.16f, 2.14f), timber, false);
            Box("Shaft", g.transform, new Vector3(1.55f, 0.74f, 0f), new Vector3(3.10f, 0.14f, 0.16f), timber, false);
            Box("Yoke", g.transform, new Vector3(3.02f, 1.10f, 0f), new Vector3(0.13f, 0.12f, 1.72f), timberPale, false);

            // 四马
            for (int i = 0; i < 4; i++)
                HorseMeshObject(g.transform, new Vector3(2.90f, 0f, (i - 1.5f) * 0.53f), terracotta, dark);

            // 三名车兵：御手居中、车左、车右（腿站在舆内，不画）
            FigObject(g.transform, new Vector3(0.18f, 0.46f, 0f), 0f, 3, terracotta);
            FigObject(g.transform, new Vector3(-0.32f, 0.40f, -0.44f), -8f, 1, terracotta);
            FigObject(g.transform, new Vector3(-0.32f, 0.40f, 0.44f), 8f, 0, terracotta);
        }

        static void FigObject(Transform parent, Vector3 pos, float yaw, int variant, Material mat)
        {
            var go = new GameObject("Warrior");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = FigureMesh(variant);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
        }

        static void HorseMeshObject(Transform parent, Vector3 pos, Material terracotta, Material dark)
        {
            var go = new GameObject("Horse");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = HorseMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = terracotta;
            go.isStatic = true;
        }

        // ------------------------------------------------------------------ 网格

        static readonly Dictionary<int, Mesh> figCache = new Dictionary<int, Mesh>();
        static Mesh horseMeshCache;

        static Mesh FigureMesh(int variant)
        {
            Mesh m;
            if (figCache.TryGetValue(variant, out m) && m != null) return m;

            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var cs = new List<Color>();
            var uvs = new List<Vector2>();
            var ts = new List<int>();

            bool armor = variant == 0 || variant == 1 || variant == 4;
            bool cap = variant == 4;
            bool driver = variant == 3;
            bool bent = variant == 1;
            float hs = (variant == 4) ? 1.045f : 1f;

            // 陶俑是灰陶，出窑后有的偏黄、有的偏冷，vertex color 拉开这点色差
            Color skin = new Color(1.00f, 0.98f, 0.95f);
            Color robe = new Color(0.93f, 0.90f, 0.86f);
            Color mail = new Color(0.88f, 0.84f, 0.78f);
            Color hair = new Color(0.46f, 0.42f, 0.38f);
            Color sole = new Color(0.40f, 0.37f, 0.34f);

            void B(Vector3 c, Vector3 s, Color col) => AddBox(vs, ns, cs, uvs, ts, c * hs, s * hs, col);

            // 注意轴向：model 面朝 +X，所以 x 是前后进深、z 是左右宽度。
            // 人是"窄而薄"的——胸厚 0.27、肩宽 0.44。原来写反了（进深 0.40、宽 0.34），
            // 站在过洞里看过去就是一块块对着你的门板。
            //
            // 履
            for (int s = -1; s <= 1; s += 2)
                B(new Vector3(0.03f, 0.035f, s * 0.105f), new Vector3(0.28f, 0.07f, 0.12f), sole);
            // 双腿
            for (int s = -1; s <= 1; s += 2)
                B(new Vector3(0f, 0.46f, s * 0.105f), new Vector3(0.16f, 0.78f, 0.17f), robe);
            // 战袍下摆（略外张）
            B(new Vector3(0f, 0.86f, 0f), new Vector3(0.30f, 0.36f, 0.44f), robe);
            // 躯干：从腰到肩一整块，不再拿"束腰"切出一道槽
            B(new Vector3(0f, 1.28f, 0f), new Vector3(0.27f, 0.48f, 0.38f), armor ? mail : robe);
            // 肩：只比躯干宽一点，撑出斜肩的轮廓
            B(new Vector3(0f, 1.45f, 0f), new Vector3(0.28f, 0.12f, 0.44f), armor ? mail : robe);
            // 甲片：做成胸前几道横棱，而不是整块加厚——加厚会在近景把胸读成一个抽屉
            if (armor)
                for (int k = 0; k < 5; k++)
                    B(new Vector3(0.138f, 1.10f + k * 0.085f, 0f), new Vector3(0.016f, 0.028f, 0.30f),
                        new Color(0.80f, 0.77f, 0.72f));
            // 披膊
            if (armor)
                for (int s = -1; s <= 1; s += 2)
                    B(new Vector3(0f, 1.39f, s * 0.225f), new Vector3(0.28f, 0.12f, 0.16f), mail);

            // 双臂
            for (int s = -1; s <= 1; s += 2)
            {
                if (driver)
                    B(new Vector3(0.18f, 1.34f, s * 0.17f), new Vector3(0.34f, 0.13f, 0.13f), robe);
                else if (bent && s > 0)
                {
                    B(new Vector3(0.12f, 1.32f, s * 0.225f), new Vector3(0.28f, 0.13f, 0.13f), robe);
                    B(new Vector3(-0.03f, 1.14f, s * 0.225f), new Vector3(0.14f, 0.30f, 0.13f), robe);
                }
                else
                    B(new Vector3(0.01f, 1.20f, s * 0.225f), new Vector3(0.14f, 0.52f, 0.13f), robe);
            }

            // 颈、头
            B(new Vector3(0f, 1.55f, 0f), new Vector3(0.13f, 0.10f, 0.13f), skin);
            B(new Vector3(0.008f, 1.71f, 0f), new Vector3(0.18f, 0.23f, 0.19f), skin);

            // 发：帽子/发髻只比头宽一点点。原来是 0.21 压在 0.19 的头上，
            // 一圈凸出来的薄片在近距离读成一顶鸭舌帽，整座军阵都是"戴帽子的方块"。
            if (cap)
            {
                B(new Vector3(0f, 1.825f, 0f), new Vector3(0.184f, 0.045f, 0.194f), hair);
                B(new Vector3(-0.02f, 1.868f, 0f), new Vector3(0.20f, 0.045f, 0.11f), hair);
            }
            else
            {
                B(new Vector3(0f, 1.825f, 0f), new Vector3(0.184f, 0.045f, 0.194f), hair);
                B(new Vector3(-0.03f, 1.872f, -0.062f), new Vector3(0.10f, 0.09f, 0.10f), hair);
            }

            m = new Mesh { name = "QinWarrior_" + variant };
            m.SetVertices(vs);
            m.SetNormals(ns);
            m.SetColors(cs);
            m.SetUVs(0, uvs);
            m.SetTriangles(ts, 0);
            m.RecalculateBounds();
            figCache[variant] = m;
            return m;
        }

        static Mesh HorseMesh()
        {
            if (horseMeshCache != null) return horseMeshCache;

            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var cs = new List<Color>();
            var uvs = new List<Vector2>();
            var ts = new List<int>();

            Color body = new Color(1f, 0.98f, 0.95f);
            Color dark = new Color(0.58f, 0.53f, 0.48f);

            // 躯干
            AddBox(vs, ns, cs, uvs, ts, new Vector3(0f, 1.02f, 0f), new Vector3(1.34f, 0.60f, 0.54f), body);
            // 颈
            AddBox(vs, ns, cs, uvs, ts, new Vector3(0.62f, 1.34f, 0f), new Vector3(0.42f, 0.62f, 0.36f), body);
            // 头
            AddBox(vs, ns, cs, uvs, ts, new Vector3(0.92f, 1.56f, 0f), new Vector3(0.46f, 0.24f, 0.25f), body);
            // 耳
            for (int s = -1; s <= 1; s += 2)
                AddBox(vs, ns, cs, uvs, ts, new Vector3(0.80f, 1.74f, s * 0.085f), new Vector3(0.07f, 0.15f, 0.06f), dark);
            // 鬃
            AddBox(vs, ns, cs, uvs, ts, new Vector3(0.58f, 1.58f, 0f), new Vector3(0.36f, 0.10f, 0.13f), dark);
            // 四腿
            for (int a = -1; a <= 1; a += 2)
                for (int s = -1; s <= 1; s += 2)
                    AddBox(vs, ns, cs, uvs, ts, new Vector3(a * 0.50f, 0.39f, s * 0.20f), new Vector3(0.16f, 0.78f, 0.17f), body);
            // 尾
            AddBox(vs, ns, cs, uvs, ts, new Vector3(-0.72f, 0.90f, 0f), new Vector3(0.14f, 0.62f, 0.13f), dark);

            horseMeshCache = new Mesh { name = "QinHorse" };
            horseMeshCache.SetVertices(vs);
            horseMeshCache.SetNormals(ns);
            horseMeshCache.SetColors(cs);
            horseMeshCache.SetUVs(0, uvs);
            horseMeshCache.SetTriangles(ts, 0);
            horseMeshCache.RecalculateBounds();
            return horseMeshCache;
        }

        /// <summary>把一个方块追加进网格缓冲（每个面独立法线，会读成硬边）。</summary>
        static void AddBox(List<Vector3> vs, List<Vector3> ns, List<Color> cs, List<Vector2> uvs, List<int> ts,
                           Vector3 c, Vector3 s, Color col)
        {
            Vector3 h = s * 0.5f;
            float x0 = c.x - h.x, x1 = c.x + h.x;
            float y0 = c.y - h.y, y1 = c.y + h.y;
            float z0 = c.z - h.z, z1 = c.z + h.z;

            // 顺序按"从外面看逆时针"，配合 (0,2,1)(0,3,2) 的三角绕序
            Quad(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.up, col);
            Quad(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), Vector3.down, col);
            Quad(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.forward, col);
            Quad(new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), Vector3.back, col);
            Quad(new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), Vector3.right, col);
            Quad(new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), Vector3.left, col);

            void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Vector3 n, Color color)
            {
                int i = vs.Count;
                vs.Add(a); vs.Add(b); vs.Add(cc); vs.Add(d);
                for (int k = 0; k < 4; k++) { ns.Add(n); cs.Add(color); }
                uvs.Add(new Vector2(a.x + a.y, a.z + a.y));
                uvs.Add(new Vector2(b.x + b.y, b.z + b.y));
                uvs.Add(new Vector2(cc.x + cc.y, cc.z + cc.y));
                uvs.Add(new Vector2(d.x + d.y, d.z + d.y));
                ts.Add(i); ts.Add(i + 2); ts.Add(i + 1);
                ts.Add(i); ts.Add(i + 3); ts.Add(i + 2);
            }
        }
    }
}
