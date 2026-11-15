using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>序章山谷的核心建筑：重檐楼阁、石台基、栏杆、斗拱与石拱桥。</summary>
    public static partial class HuaxiaBuilder
    {
        // ================================================================ 藏卷阁（重檐楼阁）

        /// <summary>二级重檐楼阁：石台基 + 上下两层身 + 腰檐 + 顶檐 + 斗拱 + 栏杆。</summary>
        static void BuildGreatHall(Transform parent, Vector3 pos, float yaw,
                                   Material wood, Material woodDark, Material roof, Material ridge,
                                   Material stone, Material stoneDark, Material wall, Material lamp)
        {
            var g = Empty("GreatHall", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            const float halfW = 8.0f;   // 面阔一半（五开间）
            const float halfD = 5.6f;   // 进深一半
            var paper = Mat("PaperWindow", Color.Lerp(HuaxiaPalette.AmberPale, Color.white, 0.58f), 0f, 0.06f);

            const float baseTop = 1.55f;
            const float eaveLow = 6.88f;    // 腰檐檐口高度
            const float eaveHigh = 11.30f;  // 顶檐檐口高度

            // ------------------------------------------------ 台基（三层）
            Box("Terrace_0", g.transform, new Vector3(0f, 0.22f, 0f), new Vector3(halfW * 2f + 7.6f, 0.44f, halfD * 2f + 7.0f), stoneDark);
            Box("Terrace_1", g.transform, new Vector3(0f, 0.66f, 0f), new Vector3(halfW * 2f + 6.0f, 0.44f, halfD * 2f + 5.4f), stone);
            Box("Terrace_2", g.transform, new Vector3(0f, 1.10f, 0f), new Vector3(halfW * 2f + 4.4f, 0.44f, halfD * 2f + 3.8f), stone);
            Box("Terrace_3", g.transform, new Vector3(0f, baseTop - 0.22f, 0f), new Vector3(halfW * 2f + 2.4f, 0.46f, halfD * 2f + 2.0f), stone);

            // 御路踏跺（正面大台阶）。
            // 原来只有五级、停在 1.32 高，往上还压着两级 0.44 的台基——比 stepOffset
            // （0.35）高，人走到一半就再也上不去。现在踏跺一路接到最上一层台面，
            // 七级每级 0.22，全程没有超过 stepOffset 的高差。
            const float stairFront = -halfD - 4.95f;   // 第一级前缘
            const float tread = 0.55f;
            const float rise = 0.22f;
            const int stepCount = 7;
            for (int i = 0; i < stepCount; i++)
            {
                float top = rise * (i + 1);
                Box("Step_F" + i, g.transform,
                    new Vector3(0f, top * 0.5f, stairFront + tread * (i + 0.5f)),
                    new Vector3(4.4f - i * 0.02f, top, tread + 0.06f), stone);
            }
            Box("ImperialWay", g.transform, new Vector3(0f, 0.02f, stairFront - 0.75f),
                new Vector3(1.9f, 0.06f, 1.5f), stoneDark, false);

            // ------------------------------------------------ 台基栏杆
            Balustrade(g.transform, new Vector3(0f, baseTop, 0f),
                new Vector2(halfW + 1.5f, halfD + 1.35f), stone, 1.02f, 0.34f, true, "Balu_T");

            // 檐下椽头：打断大面积平檐底，近看有木构层次
            void Rafters(float y, float hw, float hd)
            {
                for (float x = -hw + 0.30f; x <= hw - 0.29f; x += 0.60f)
                {
                    foreach (float sgn in new[] { -1f, 1f })
                        Box("Rafter", g.transform, new Vector3(x, y, sgn * (hd + 0.40f)),
                            new Vector3(0.12f, 0.12f, 1.05f), woodDark, false);
                }
                for (float z = -hd + 0.55f; z <= hd - 0.54f; z += 0.60f)
                {
                    foreach (float sgn in new[] { -1f, 1f })
                        Box("Rafter", g.transform, new Vector3(sgn * (hw + 0.40f), y, z),
                            new Vector3(1.05f, 0.12f, 0.12f), woodDark, false);
                }
            }

            // ------------------------------------------------ 下层身（五开间）
            float[] bayX = { -halfW + 0.9f, -4.2f, -1.4f, 1.4f, 4.2f, halfW - 0.9f };
            float[] rowZ = { -halfD + 0.8f, halfD - 0.8f };
            foreach (float x in bayX)
            {
                foreach (float z in rowZ)
                {
                    Cyl("Col_L", g.transform, new Vector3(x, baseTop + 2.42f, z), 0.26f, 4.84f, wood);
                    Cyl("Base_L", g.transform, new Vector3(x, baseTop + 0.12f, z), 0.34f, 0.24f, stoneDark);
                }
            }

            // 墙与窗：正面开三间，其余做白墙。
            // 正中一间是真的门洞——原来整面墙砌死，只在墙外贴三扇纸格扇，
            // 走到跟前是一堵墙，玩家找不到入口。现在墙让开，装两扇能开的门。
            const float gateHalf = 1.45f;              // 门洞半宽
            float wallHalfX = halfW - 0.10f;           // 原 Wall_F 半宽
            float sideW = wallHalfX - gateHalf;
            float wallBaseY = baseTop + 0.10f;
            float wallTopY = baseTop + 4.50f;
            float gateTopY = baseTop + 3.72f;          // 门洞上沿

            Box("Wall_F_L", g.transform, new Vector3(-(gateHalf + sideW * 0.5f), baseTop + 2.30f, -halfD + 0.86f),
                new Vector3(sideW, wallTopY - wallBaseY, 0.18f), wall);
            Box("Wall_F_R", g.transform, new Vector3(gateHalf + sideW * 0.5f, baseTop + 2.30f, -halfD + 0.86f),
                new Vector3(sideW, wallTopY - wallBaseY, 0.18f), wall);
            Box("Wall_F_Head", g.transform, new Vector3(0f, (gateTopY + wallTopY) * 0.5f, -halfD + 0.86f),
                new Vector3(gateHalf * 2f, wallTopY - gateTopY, 0.18f), wall);

            Box("Wall_B", g.transform, new Vector3(0f, baseTop + 2.30f, halfD - 0.86f),
                new Vector3(halfW * 2f - 0.2f, 4.40f, 0.18f), wall);
            Box("Wall_L", g.transform, new Vector3(-halfW + 0.86f, baseTop + 2.30f, 0f),
                new Vector3(0.18f, 4.40f, halfD * 2f - 1.7f), wall);
            Box("Wall_R", g.transform, new Vector3(halfW - 0.86f, baseTop + 2.30f, 0f),
                new Vector3(0.18f, 4.40f, halfD * 2f - 1.7f), wall);

            // 格扇门（暖色纸面 + 木框）：只留左右两间，正中让给真门
            float[] doorX = { -2.8f, 2.8f };
            foreach (float x in doorX)
            {
                Box("DoorFrame", g.transform, new Vector3(x, baseTop + 1.62f, -halfD + 0.80f),
                    new Vector3(2.42f, 3.10f, 0.12f), woodDark, false);
                // 门是关着的：给碰撞体，人才不会穿过门扇走进空屋子
                Box("DoorLeaf", g.transform, new Vector3(x, baseTop + 1.58f, -halfD + 0.70f),
                    new Vector3(2.16f, 2.86f, 0.06f), paper);
                Box("DoorBar", g.transform, new Vector3(x, baseTop + 2.08f, -halfD + 0.64f),
                    new Vector3(0.08f, 2.86f, 0.05f), woodDark, false);
                Box("DoorSill", g.transform, new Vector3(x, baseTop + 0.16f, -halfD + 0.70f),
                    new Vector3(2.30f, 0.14f, 0.22f), woodDark, false);
            }

            // 直棂窗（两侧开间）
            float[] winX = { -5.9f, 5.9f };
            foreach (float x in winX)
            {
                Box("WinFrame", g.transform, new Vector3(x, baseTop + 3.10f, -halfD + 0.80f),
                    new Vector3(1.62f, 1.48f, 0.10f), woodDark, false);
                Box("WinLeaf", g.transform, new Vector3(x, baseTop + 3.10f, -halfD + 0.72f),
                    new Vector3(1.40f, 1.26f, 0.05f), paper, false);
                for (int k = -2; k <= 2; k++)
                    Box("WinBar", g.transform, new Vector3(x + k * 0.26f, baseTop + 3.10f, -halfD + 0.66f),
                        new Vector3(0.05f, 1.26f, 0.04f), woodDark, false);
            }

            // 正门：三处遗址都进去过才开（运行时有 HallDoor 自己判断）
            BuildHallGate(g.transform, baseTop, halfD, wood, woodDark, stoneDark);

            // 门一开，隔着门看见的是一道亮着的隔扇——不是一间空屋子。
            // 这间屋子真正的内容是下一场那间书房（Epilogue），人一跨门槛就过去，
            // 所以这里只要把「里面亮着灯、有人住」演对，不堆东西。
            Box("Hall_Floor", g.transform, new Vector3(0f, baseTop + 0.02f, 0f),
                new Vector3(halfW * 2f - 1.9f, 0.06f, halfD * 2f - 1.9f), stoneDark, false);

            var screen = Empty("Hall_Screen", g.transform, new Vector3(0f, baseTop, -2.35f));
            float[] sx = { -1.24f, 0f, 1.24f };
            foreach (float x in sx)
            {
                var panel = Empty("ScreenPanel", screen.transform, new Vector3(x, 0f, 0f));
                BuildLatticePanel(panel.transform, 1.18f, 3.55f, woodDark, wood, paper);
            }
            // 灯在屏后：纸面透光，从院子里看是「里头点着灯」
            AddPoint(screen.transform, "ScreenFill", new Vector3(0f, 1.75f, 0.85f),
                new Color(1f, 0.88f, 0.68f), 6.0f, 1.55f);

            // 下层额枋
            Box("Beam_L1", g.transform, new Vector3(0f, baseTop + 4.94f, -halfD + 0.86f),
                new Vector3(halfW * 2f, 0.34f, 0.26f), woodDark);
            Box("Beam_L2", g.transform, new Vector3(0f, baseTop + 4.94f, halfD - 0.86f),
                new Vector3(halfW * 2f, 0.34f, 0.26f), woodDark);
            Box("Beam_L3", g.transform, new Vector3(-halfW + 0.86f, baseTop + 4.94f, 0f),
                new Vector3(0.26f, 0.34f, halfD * 2f - 1.7f), woodDark);
            Box("Beam_L4", g.transform, new Vector3(halfW - 0.86f, baseTop + 4.94f, 0f),
                new Vector3(0.26f, 0.34f, halfD * 2f - 1.7f), woodDark);

            // ------------------------------------------------ 下层斗拱
            DougongRow(g.transform, baseTop + 4.62f, halfW - 0.9f, halfD - 0.8f, woodDark);

            // ------------------------------------------------ 腰檐（下层屋顶）
            MeshObject("EaveLow", g.transform, new Vector3(0f, eaveLow, 0f),
                MeshFactory.CurvedRoof(halfW + 1.15f, halfD + 1.05f, 1.12f, 0.50f, 0.32f, 26, 22), roof, false);

            Rafters(eaveLow - 0.26f, halfW + 0.45f, halfD + 0.40f);

            // ------------------------------------------------ 上层身
            float upBase = eaveLow + 0.12f;
            float[] upX = { -4.9f, -2.45f, 0f, 2.45f, 4.9f };
            foreach (float x in upX)
            {
                Cyl("Col_U", g.transform, new Vector3(x, upBase + 2.15f, -halfD + 2.0f), 0.20f, 4.30f, wood);
                Cyl("Col_U", g.transform, new Vector3(x, upBase + 2.15f, halfD - 2.0f), 0.20f, 4.30f, wood);
            }

            Box("Wall_UF", g.transform, new Vector3(0f, upBase + 2.00f, -halfD + 2.06f),
                new Vector3(halfW * 2f - 3.4f, 4.00f, 0.16f), wall, false);
            Box("Wall_UB", g.transform, new Vector3(0f, upBase + 2.00f, halfD - 2.06f),
                new Vector3(halfW * 2f - 3.4f, 4.00f, 0.16f), wall, false);
            Box("Wall_UL", g.transform, new Vector3(-halfW + 2.0f, upBase + 2.00f, 0f),
                new Vector3(0.16f, 4.00f, halfD * 2f - 4.0f), wall, false);
            Box("Wall_UR", g.transform, new Vector3(halfW - 2.0f, upBase + 2.00f, 0f),
                new Vector3(0.16f, 4.00f, halfD * 2f - 4.0f), wall, false);

            // 上层横披窗
            for (int i = -1; i <= 1; i++)
            {
                Box("Win_U_Frame", g.transform, new Vector3(i * 3.1f, upBase + 1.85f, -halfD + 2.02f),
                    new Vector3(2.34f, 1.78f, 0.07f), woodDark, false);
                Box("Win_U", g.transform, new Vector3(i * 3.1f, upBase + 1.85f, -halfD + 1.94f),
                    new Vector3(2.1f, 1.55f, 0.06f), paper, false);
                for (int k = -3; k <= 3; k++)
                    Box("Win_U_Bar", g.transform, new Vector3(i * 3.1f + k * 0.30f, upBase + 1.85f, -halfD + 1.88f),
                        new Vector3(0.05f, 1.55f, 0.04f), woodDark, false);
            }

            Box("Beam_U1", g.transform, new Vector3(0f, upBase + 4.02f, -halfD + 2.0f),
                new Vector3((halfW - 1.9f) * 2f, 0.30f, 0.24f), woodDark);
            Box("Beam_U2", g.transform, new Vector3(0f, upBase + 4.02f, halfD - 2.0f),
                new Vector3((halfW - 1.9f) * 2f, 0.30f, 0.24f), woodDark);

            // ------------------------------------------------ 上层斗拱
            DougongRow(g.transform, upBase + 3.74f, halfW - 2.0f, halfD - 2.0f, woodDark);

            // ------------------------------------------------ 顶檐（重檐）
            MeshObject("EaveHigh", g.transform, new Vector3(0f, eaveHigh, 0f),
                MeshFactory.CurvedRoof(halfW + 0.95f, halfD + 0.92f, 2.58f, 0.58f, 0.36f, 32, 28), roof, false);
            Box("Ridge_High", g.transform, new Vector3(0f, eaveHigh + 2.56f, 0f),
                new Vector3(0.34f, 0.30f, (halfD + 1.00f) * 2f * 0.94f), ridge, false);

            Rafters(eaveHigh - 0.26f, halfW + 0.35f, halfD + 0.32f);

            // 檐口封边：压深一号，避免屋顶边缘像一张薄纸
            foreach (float z in new[] { -(halfD + 0.98f), halfD + 0.98f })
            {
                Box("Fascia_F", g.transform, new Vector3(0f, eaveHigh + 0.10f, z),
                    new Vector3((halfW + 1.05f) * 2f * 0.97f, 0.30f, 0.22f), ridge, false);
            }

            // 正脊两端的鸱吻
            foreach (float z in new[] { -(halfD + 0.55f), halfD + 0.55f })
            {
                Box("Chiwen", g.transform, new Vector3(0f, eaveHigh + 2.76f, z),
                    new Vector3(0.24f, 0.66f, 0.34f), ridge, false);
                Box("ChiwenTail", g.transform, new Vector3(0f, eaveHigh + 3.10f, z * 1.03f),
                    new Vector3(0.20f, 0.30f, 0.20f), ridge, false);
            }

            Ball("Finial", g.transform, new Vector3(0f, eaveHigh + 2.92f, 0f), 0.17f, ridge);

            // ------------------------------------------------ 匾额
            Box("PlaqueBoard", g.transform, new Vector3(0f, eaveLow - 0.62f, -halfD - 0.62f),
                new Vector3(3.5f, 0.92f, 0.12f), woodDark, false);
            CreateTextPlate(g.transform, "藏 卷 阁", new Vector3(0f, eaveLow - 0.62f, -halfD - 0.69f), Vector3.zero,
                new Vector2(3.2f, 0.78f), 80, HuaxiaPalette.AmberPale, title: true);

            // ------------------------------------------------ 檐下灯笼
            BuildHangingLantern(g.transform, new Vector3(-6.6f, eaveLow - 1.05f, -halfD - 1.45f), lamp);
            BuildHangingLantern(g.transform, new Vector3(6.6f, eaveLow - 1.05f, -halfD - 1.45f), lamp);

            // 台基上的石灯
            BuildStoneLamp(g.transform, new Vector3(-8.8f, 0f, -6.9f), stoneDark, lamp);
            BuildStoneLamp(g.transform, new Vector3(8.8f, 0f, -6.9f), stoneDark, lamp);
        }

        // ================================================================ 藏卷阁正门

        /// <summary>
        /// 门框、门槛、两扇挂在门轴上的门板，外加一根运行时会被撤掉的门闩。
        /// 门轴用空物体，门板挂在轴的子层——绕轴转才像门，整块平移像帘子。
        /// </summary>
        static void BuildHallGate(Transform parent, float baseTop, float halfD,
                                  Material wood, Material woodDark, Material stoneDark)
        {
            const float gateHalf = 1.45f;
            float wallZ = -halfD + 0.86f;      // 墙心
            float leafZ = -halfD + 0.72f;      // 门板贴在墙外皮，朝院子那一面
            float sillTop = baseTop + 0.15f;

            // 门槛：一脚踩上去，就知道这里是门不是墙
            Box("Gate_Sill", parent, new Vector3(0f, baseTop + 0.075f, wallZ),
                new Vector3(gateHalf * 2f + 0.60f, 0.15f, 0.46f), stoneDark);

            foreach (float sgn in new[] { -1f, 1f })
                Box("Gate_Jamb", parent, new Vector3(sgn * (gateHalf + 0.10f), baseTop + 1.90f, wallZ),
                    new Vector3(0.22f, 3.80f, 0.34f), woodDark);
            Box("Gate_Lintel", parent, new Vector3(0f, baseTop + 3.94f, wallZ),
                new Vector3(gateHalf * 2f + 0.64f, 0.24f, 0.38f), woodDark);

            var gate = Empty("HallGate_Doors", parent, new Vector3(0f, 0f, leafZ));
            var hingeL = Empty("Hinge_L", gate.transform, new Vector3(-gateHalf, sillTop, 0f));
            var hingeR = Empty("Hinge_R", gate.transform, new Vector3(gateHalf, sillTop, 0f));
            // 门板用浅一号的木，压条和门闩压深——不然整扇门是一块黑，远看只剩一个洞
            BuildDoorLeaf(hingeL.transform, 1f, wood, woodDark);
            BuildDoorLeaf(hingeR.transform, -1f, wood, woodDark);

            // 门闩：三卷没走齐时横在两扇门中间。开了门它先撤，门再转开
            var latch = Box("Gate_Latch", gate.transform, new Vector3(0f, baseTop + 1.05f, -0.09f),
                new Vector3(gateHalf * 2f - 0.10f, 0.13f, 0.08f), woodDark, false);

            var door = gate.AddComponent<HallDoor>();
            door.leftHinge = hingeL.transform;
            door.rightHinge = hingeR.transform;
            door.latch = latch;
        }

        /// <summary>一扇门板：side = +1 往左轴里伸，-1 往右轴里伸；轴在门板下沿的内边。</summary>
        static void BuildDoorLeaf(Transform hinge, float side, Material frame, Material panel)
        {
            const float w = 1.44f;
            const float h = 3.60f;
            float cx = side * w * 0.5f;

            Box("Leaf", hinge, new Vector3(cx, h * 0.5f, 0f), new Vector3(w, h, 0.10f), frame);

            // 四道横向压条：远看是一整扇板门，近看才有木作的层次
            for (int i = 0; i < 4; i++)
                Box("Leaf_Batten", hinge, new Vector3(cx, 0.48f + i * 0.88f, -0.06f),
                    new Vector3(w - 0.20f, 0.12f, 0.05f), panel, false);

            // 门钹：两块门板中间那对环，是「推」的手位
            Ball("Leaf_Ring", hinge, new Vector3(cx + side * 0.42f, 1.58f, -0.14f), 0.11f, frame);
        }

        // ================================================================ 东西配殿

        /// <summary>单檐配殿：小体量、单层，用来围合前庭，避免主楼孤零零立在空地上。</summary>
        static void BuildSideHall(Transform parent, Vector3 pos, float yaw, float halfW, float halfD,
                                  string title, string note,
                                  Material wood, Material woodDark, Material roof, Material ridge,
                                  Material stone, Material stoneDark, Material wall, Material lamp)
        {
            var g = Empty("SideHall", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var paper = Mat("PaperWindow", Color.Lerp(HuaxiaPalette.AmberPale, Color.white, 0.58f), 0f, 0.06f);

            const float baseTop = 0.92f;
            float bodyTop = baseTop + 3.30f;
            float eave = bodyTop + 0.34f;

            // 台基
            Box("SH_Base0", g.transform, new Vector3(0f, 0.20f, 0f),
                new Vector3(halfW * 2f + 4.2f, 0.40f, halfD * 2f + 4.0f), stoneDark);
            Box("SH_Base1", g.transform, new Vector3(0f, 0.60f, 0f),
                new Vector3(halfW * 2f + 3.0f, 0.38f, halfD * 2f + 2.9f), stone);
            Box("SH_Base2", g.transform, new Vector3(0f, baseTop - 0.15f, 0f),
                new Vector3(halfW * 2f + 2.2f, 0.30f, halfD * 2f + 2.1f), stone);

            for (int i = 0; i < 4; i++)
            {
                Box("SH_Step" + i, g.transform,
                    new Vector3(0f, 0.78f - i * 0.22f, -halfD - 2.1f - i * 0.34f),
                    new Vector3(3.2f, 0.22f, 0.42f), stone);
            }

            // 柱网
            float[] xs = { -halfW + 0.65f, -halfW * 0.36f, halfW * 0.36f, halfW - 0.65f };
            foreach (float x in xs)
            {
                foreach (float z in new[] { -halfD + 0.65f, halfD - 0.65f })
                {
                    Cyl("SH_Col", g.transform, new Vector3(x, baseTop + 1.65f, z), 0.22f, 3.30f, wood);
                    Cyl("SH_ColBase", g.transform, new Vector3(x, baseTop + 0.10f, z), 0.28f, 0.20f, stoneDark);
                }
            }

            // 墙
            Box("SH_WallB", g.transform, new Vector3(0f, baseTop + 1.65f, halfD - 0.70f),
                new Vector3(halfW * 2f - 0.1f, 3.30f, 0.16f), wall);
            Box("SH_WallL", g.transform, new Vector3(-halfW + 0.70f, baseTop + 1.65f, 0f),
                new Vector3(0.16f, 3.30f, halfD * 2f - 1.4f), wall);
            Box("SH_WallR", g.transform, new Vector3(halfW - 0.70f, baseTop + 1.65f, 0f),
                new Vector3(0.16f, 3.30f, halfD * 2f - 1.4f), wall);
            Box("SH_WallF", g.transform, new Vector3(0f, baseTop + 1.65f, -halfD + 0.70f),
                new Vector3(halfW * 2f - 0.1f, 3.30f, 0.16f), wall);

            // 正中格扇门与两侧直棂窗
            Box("SH_DoorFrame", g.transform, new Vector3(0f, baseTop + 1.35f, -halfD + 0.64f),
                new Vector3(2.30f, 2.60f, 0.12f), woodDark, false);
            Box("SH_DoorLeaf", g.transform, new Vector3(0f, baseTop + 1.32f, -halfD + 0.56f),
                new Vector3(2.04f, 2.36f, 0.06f), paper);
            Box("SH_DoorBar", g.transform, new Vector3(0f, baseTop + 1.80f, -halfD + 0.50f),
                new Vector3(0.07f, 2.36f, 0.05f), woodDark, false);

            foreach (float x in new[] { -halfW * 0.62f, halfW * 0.62f })
            {
                Box("SH_WinFrame", g.transform, new Vector3(x, baseTop + 1.85f, -halfD + 0.68f),
                    new Vector3(1.42f, 1.20f, 0.10f), woodDark, false);
                Box("SH_WinLeaf", g.transform, new Vector3(x, baseTop + 1.85f, -halfD + 0.56f),
                    new Vector3(1.20f, 0.98f, 0.05f), paper, false);
            }

            // 门楣下挂一块小牌：这屋子是干什么的，写在门上
            if (!string.IsNullOrEmpty(title))
            {
                Box("SH_NameBoard", g.transform, new Vector3(0f, baseTop + 2.80f, -halfD + 0.50f),
                    new Vector3(1.72f, 0.40f, 0.08f), woodDark, false);
                CreateTextPlate(g.transform, title, new Vector3(0f, baseTop + 2.80f, -halfD + 0.44f), Vector3.zero,
                    new Vector2(1.60f, 0.34f), 54, HuaxiaPalette.PaperWarm, title: true);
            }
            if (!string.IsNullOrEmpty(note))
            {
                CreateTextPlate(g.transform, note, new Vector3(0f, baseTop + 2.42f, -halfD + 0.44f), Vector3.zero,
                    new Vector2(2.30f, 0.20f), 30, HuaxiaPalette.WoodDark);
            }

            // 额枋与斗拱
            Box("SH_BeamF", g.transform, new Vector3(0f, bodyTop - 0.22f, -halfD + 0.70f),
                new Vector3(halfW * 2f, 0.30f, 0.24f), woodDark);
            Box("SH_BeamB", g.transform, new Vector3(0f, bodyTop - 0.22f, halfD - 0.70f),
                new Vector3(halfW * 2f, 0.30f, 0.24f), woodDark);
            Box("SH_BeamL", g.transform, new Vector3(-halfW + 0.70f, bodyTop - 0.22f, 0f),
                new Vector3(0.24f, 0.30f, halfD * 2f - 1.4f), woodDark);
            Box("SH_BeamR", g.transform, new Vector3(halfW - 0.70f, bodyTop - 0.22f, 0f),
                new Vector3(0.24f, 0.30f, halfD * 2f - 1.4f), woodDark);
            DougongRow(g.transform, bodyTop - 0.30f, halfW - 0.65f, halfD - 0.65f, woodDark);

            // 单檐
            MeshObject("SH_Roof", g.transform, new Vector3(0f, eave, 0f),
                MeshFactory.CurvedRoof(halfW + 1.05f, halfD + 1.00f, 1.95f, 0.48f, 0.32f, 28, 24), roof, false);
            Box("SH_Ridge", g.transform, new Vector3(0f, eave + 1.90f, 0f),
                new Vector3(0.28f, 0.26f, (halfD + 1.00f) * 2f * 0.94f), ridge, false);
            Ball("SH_Finial", g.transform, new Vector3(0f, eave + 2.16f, 0f), 0.13f, ridge);

            foreach (float z in new[] { -(halfD + 0.98f), halfD + 0.98f })
            {
                Box("SH_Fascia", g.transform, new Vector3(0f, eave + 0.08f, z),
                    new Vector3((halfW + 1.05f) * 2f * 0.97f, 0.26f, 0.20f), ridge, false);
            }

            BuildHangingLantern(g.transform, new Vector3(-halfW * 0.55f, eave - 0.72f, -halfD - 1.30f), lamp);
            BuildHangingLantern(g.transform, new Vector3(halfW * 0.55f, eave - 0.72f, -halfD - 1.30f), lamp);
        }

        // ================================================================ 斗拱

        /// <summary>沿檐下一圈布置简化斗拱：坐斗 + 横栱 + 挑檐枋。</summary>
        static void DougongRow(Transform parent, float y, float halfW, float halfD, Material mat)
        {
            var g = Empty("Dougong", parent, Vector3.zero);

            for (float x = -halfW; x <= halfW + 0.01f; x += 1.05f)
            {
                foreach (float z in new[] { -halfD, halfD })
                    Dougong(g.transform, new Vector3(x, y, z), mat);
            }
            for (float z = -halfD + 1.05f; z < halfD - 0.01f; z += 1.05f)
            {
                foreach (float x in new[] { -halfW, halfW })
                    Dougong(g.transform, new Vector3(x, y, z), mat);
            }

            // 挑檐枋：一圈压住斗拱
            Box("Apron_F", g.transform, new Vector3(0f, y + 0.34f, -halfD), new Vector3(halfW * 2f + 1.0f, 0.16f, 0.24f), mat, false);
            Box("Apron_B", g.transform, new Vector3(0f, y + 0.34f, halfD), new Vector3(halfW * 2f + 1.0f, 0.16f, 0.24f), mat, false);
            Box("Apron_L", g.transform, new Vector3(-halfW, y + 0.34f, 0f), new Vector3(0.24f, 0.16f, halfD * 2f + 1.0f), mat, false);
            Box("Apron_R", g.transform, new Vector3(halfW, y + 0.34f, 0f), new Vector3(0.24f, 0.16f, halfD * 2f + 1.0f), mat, false);
        }

        static void Dougong(Transform parent, Vector3 pos, Material mat)
        {
            Box("LuDou", parent, pos + new Vector3(0f, 0.10f, 0f), new Vector3(0.34f, 0.20f, 0.34f), mat, false);
            Box("Gong_A", parent, pos + new Vector3(0f, 0.24f, 0f), new Vector3(1.02f, 0.12f, 0.20f), mat, false);
            Box("Gong_B", parent, pos + new Vector3(0f, 0.34f, 0f), new Vector3(0.22f, 0.10f, 0.92f), mat, false);
            Box("Ang_A", parent, pos + new Vector3(0f, 0.46f, 0f), new Vector3(0.66f, 0.10f, 0.16f), mat, false);
        }

        // ================================================================ 栏杆

        /// <summary>矩形石栏杆：望柱 + 栏板 + 扶手。gapAtFront 在正面留出踏跺开口。</summary>
        static void Balustrade(Transform parent, Vector3 origin, Vector2 half, Material mat,
                               float height, float spacing, bool gapAtFront, string name)
        {
            var g = Empty(name, parent, origin);

            // origin 已是栏杆基点，内部一律用局部高度，避免重复叠加 origin.y
            System.Action<Vector3, Vector3> rail = (a, b) =>
            {
                Vector3 mid = (a + b) * 0.5f;
                Vector3 dir = (b - a);
                float len = dir.magnitude;
                if (len < 0.12f) return;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + 90f;

                var sweep = Box("Sweep", g.transform, new Vector3(mid.x, height * 0.10f, mid.z),
                    new Vector3(len, 0.14f, 0.18f), mat, false);
                sweep.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                var panel = Box("Panel", g.transform, new Vector3(mid.x, height * 0.50f, mid.z),
                    new Vector3(len, height * 0.56f, 0.09f), mat, false);
                panel.transform.localRotation = sweep.transform.localRotation;

                var hand = Box("HandRail", g.transform, new Vector3(mid.x, height * 0.94f, mid.z),
                    new Vector3(len, 0.11f, 0.17f), mat, false);
                hand.transform.localRotation = sweep.transform.localRotation;
            };

            float fx0 = -half.x, fx1 = half.x - 0.22f;
            if (gapAtFront)
            {
                rail(new Vector3(fx0, 0f, -half.y), new Vector3(-2.7f, 0f, -half.y));
                rail(new Vector3(2.7f, 0f, -half.y), new Vector3(fx1, 0f, -half.y));
            }
            else rail(new Vector3(fx0, 0f, -half.y), new Vector3(fx1, 0f, -half.y));
            rail(new Vector3(fx0, 0f, half.y), new Vector3(fx1, 0f, half.y));        // 背面
            rail(new Vector3(-half.x, 0f, -half.y + 0.22f), new Vector3(-half.x, 0f, half.y - 0.22f));
            rail(new Vector3(half.x, 0f, -half.y + 0.22f), new Vector3(half.x, 0f, half.y - 0.22f));

            System.Action<float, float, float, float> post = (x, z, w, d) =>
                Box("Post", g.transform, new Vector3(x, height * 0.5f, z),
                    new Vector3(w, height, d), mat, false);

            for (float x = -half.x; x <= half.x - 0.05f; x += spacing * 2.6f)
            {
                post(x, -half.y, 0.20f, 0.20f);
                post(x, half.y, 0.20f, 0.20f);
            }
            for (float z = -half.y + spacing * 2.6f; z < half.y - 0.05f; z += spacing * 2.6f)
            {
                post(-half.x, z, 0.20f, 0.20f);
                post(half.x, z, 0.20f, 0.20f);
            }

            // 开口两侧补望柱
            post(-2.7f, -half.y, 0.24f, 0.24f);
            post(2.7f, -half.y, 0.24f, 0.24f);
        }

        // ================================================================ 石拱桥

        /// <summary>跨水的三折石拱桥：桥面沿弧线抬升，两侧设矮栏。</summary>
        static void BuildArchBridge(Transform parent, Vector3 pos, float yaw, float span, float width,
                                    float rise, Material stone, Material stoneDark)
        {
            var g = Empty("ArchBridge", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            const int seg = 14;
            float step = span / seg;
            for (int i = 0; i < seg; i++)
            {
                float t = (i + 0.5f) / seg;            // 0..1
                float u = t * 2f - 1f;                 // -1..1
                float y = rise * (1f - u * u);
                float z = -span * 0.5f + (i + 0.5f) * step;
                float tilt = -Mathf.Atan2(rise * -2f * u, span * 0.5f) * Mathf.Rad2Deg;

                var deck = Box("Deck_" + i, g.transform, new Vector3(0f, y + 0.16f, z),
                    new Vector3(width, 0.34f, step * 1.06f), stone);
                deck.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);

                if (i % 2 == 0)
                {
                    foreach (float x in new[] { -width * 0.5f + 0.14f, width * 0.5f - 0.14f })
                    {
                        Box("Rail", g.transform, new Vector3(x, y + 0.62f, z),
                            new Vector3(0.13f, 0.62f, 0.13f), stoneDark, false);
                    }
                }
            }

            // 桥头雁翅石
            foreach (float s in new[] { -1f, 1f })
            {
                Box("Wing_L", g.transform, new Vector3(-width * 0.5f - 0.5f, 0.18f, s * (span * 0.5f + 0.6f)),
                    new Vector3(1.35f, 0.36f, 1.5f), stone);
                Box("Wing_R", g.transform, new Vector3(width * 0.5f + 0.5f, 0.18f, s * (span * 0.5f + 0.6f)),
                    new Vector3(1.35f, 0.36f, 1.5f), stone);
            }

            // 桥两侧的纵向扶手（简化：分段短杆）
            for (int i = 0; i < seg - 1; i++)
            {
                float t0 = (i + 0.5f) / seg, t1 = (i + 1.5f) / seg;
                float u0 = t0 * 2f - 1f, u1 = t1 * 2f - 1f;
                float y0 = rise * (1f - u0 * u0), y1 = rise * (1f - u1 * u1);
                float z0 = -span * 0.5f + (i + 0.5f) * step, z1 = z0 + step;
                var mid = new Vector3(width * 0.5f - 0.14f, (y0 + y1) * 0.5f + 0.90f, (z0 + z1) * 0.5f);
                var rail = Box("HandRail", g.transform, mid, new Vector3(0.09f, 0.09f, step * 1.12f), stoneDark, false);
                rail.transform.localRotation = Quaternion.Euler(
                    -Mathf.Atan2(y1 - y0, z1 - z0) * Mathf.Rad2Deg, 0f, 0f);

                var midL = new Vector3(-width * 0.5f + 0.14f, mid.y, mid.z);
                var railL = Box("HandRail", g.transform, midL, new Vector3(0.09f, 0.09f, step * 1.12f), stoneDark, false);
                railL.transform.localRotation = rail.transform.localRotation;
            }
        }
    }
}
