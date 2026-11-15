using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 序章：暮色古道。一条山谷里的三岔路口——不是展厅，也不是档案馆。
    /// 玩家从南山道走进来，在古亭下读到残卷，再沿三条支路各自走向一处遗产遗址。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        // 三条支路的方位角（自 +Z 起算，度）：良渚 / 秦陵 / 泉州
        static readonly float[] RouteAngle = { -46f, 0f, 46f };
        const float RouteLength = 25f;
        const float GateZ = -24f;

        // 问道亭是布景，不是要去的地方：立在广场西缘，正面朝东对着广场。
        // 原先搁在北边两条路中间，走过来会以为那是下一步该去的地方，还把甬道口挡了。
        // 挪到边上以后踏跺直接落在广场石墁上，不另铺路，也不再挡三岔口往藏卷阁的视线。
        static readonly Vector3 PavilionPos = new Vector3(-10.70f, 0f, -1.00f);
        const float PavilionYaw = -90f;   // 正面（局部 -Z）转向 +X，朝广场心里

        static Vector3 RouteDir(int i)
        {
            float rad = RouteAngle[i] * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        // 三根幡柱贴在路口内侧：先看见幡，知道这边有条路；再往前才是立牌，最后是界门。
        // 以前三根幡和立牌都在同一个半径上，从山门看过去挤成一簇，看不出先后。
        // 卷二那根留在北向甬道中轴上当望柱：偏到一边会和立牌叠在一条视线上。
        const float WaypoleRadius = 8.2f;
        const float WaypoleLateral = 2.4f;

        static readonly Vector3[] WaystonePos =
        {
            Waypole(0), Waypole(1), Waypole(2),
        };

        /// <summary>幡柱立位：贴着自己这条路的左外侧，让开路面也避开立牌。</summary>
        static Vector3 Waypole(int i)
        {
            if (i == 1) return new Vector3(0f, 0f, 10.60f);
            var dir = RouteDir(i);
            var side = new Vector3(dir.z, 0f, -dir.x);
            float sign = i == 0 ? -1f : 1f;
            return dir * WaypoleRadius + side * (WaypoleLateral * sign);
        }

        static readonly Vector3 GateLookAt = new Vector3(0f, 0f, GateZ - 2f);

        // ================================================================ 场景

        public static void BuildArchiveScene()
        {
            LoadFonts();
            EnsureFolder(MatDir);
            EnsureFolder(SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupArchiveEnvironment();
            // 换成钢琴曲《Meditation Impromptu 01》，它本身比合成曲轻，音量抬一点
            AddSceneAmbient("amb_archive", 0.62f);

            var root = new GameObject("Valley").transform;

            // ------------------------------------------------------------ 材质
            var groundMat = TerrainMat();
            var stoneMat = Mat("StoneRock", HuaxiaPalette.Rock, 0f, 0.18f);
            var stoneDarkMat = Mat("StoneDark", HuaxiaPalette.RockDark, 0f, 0.2f);
            var pathMat = Mat("PathStone", Color.Lerp(HuaxiaPalette.Rock, HuaxiaPalette.Sand, 0.35f), 0f, 0.2f);
            var woodMat = Mat("WoodPost", Color.Lerp(HuaxiaPalette.WoodBrown, HuaxiaPalette.Sand, 0.22f), 0f, 0.14f);
            var woodDarkMat = Mat("WoodBeam", Color.Lerp(HuaxiaPalette.WoodDark, HuaxiaPalette.WoodBrown, 0.35f), 0f, 0.12f);
            var roofMat = MatAs("RoofTile", Color.Lerp(HuaxiaPalette.InkTeal, HuaxiaPalette.BlueGrey, 0.55f), "tile", 0f, 0.22f);
            var ridgeMat = Mat("RoofRidge", Color.Lerp(HuaxiaPalette.InkTealDeep, HuaxiaPalette.InkTeal, 0.65f), 0f, 0.24f);
            var paperMat = Mat("PaperScroll", HuaxiaPalette.PaperWhite, 0f, 0.06f);
            var bronzeMat = Mat("Bronze", Color.Lerp(HuaxiaPalette.Amber, HuaxiaPalette.WoodDark, 0.5f), 0.1f, 0.26f);
            var mapleMat = Mat("LeafMaple", Color.Lerp(HuaxiaPalette.AutumnRed, HuaxiaPalette.Terracotta, 0.35f), 0f, 0f);
            var maplePaleMat = Mat("LeafMaplePale", Color.Lerp(HuaxiaPalette.AutumnRed, HuaxiaPalette.Amber, 0.45f), 0f, 0f);
            var pineMat = Mat("LeafPine", Color.Lerp(HuaxiaPalette.PineGreen, HuaxiaPalette.FoliageGreen, 0.55f), 0f, 0f);
            var bambooMat = Mat("LeafBamboo", HuaxiaPalette.FoliageGreen, 0f, 0f);
            var bushMat = Mat("LeafBush", Color.Lerp(HuaxiaPalette.FoliageDeep, HuaxiaPalette.FoliageGreen, 0.5f), 0f, 0f);
            var waterMat = WaterMat("ValleyWater", HuaxiaPalette.Azurite);
            var farMat = Mat("MountainFar", Color.Lerp(HuaxiaPalette.DuskFog, HuaxiaPalette.BlueGrey, 0.45f), 0f, 0.04f);
            var midMat = Mat("MountainMid", Color.Lerp(HuaxiaPalette.BlueGrey, HuaxiaPalette.InkTeal, 0.55f), 0f, 0.04f);
            var nearMat = Mat("MountainNear", Color.Lerp(HuaxiaPalette.InkTeal, HuaxiaPalette.InkTealDeep, 0.5f), 0f, 0.04f);
            var lampMat = EmissiveMat("LanternPaper", HuaxiaPalette.PaperWarm, HuaxiaPalette.AmberPale * 0.42f, 0f, 0.2f);
            var wallMat = Mat("HallPlaster", Color.Lerp(HuaxiaPalette.PaperWarm, HuaxiaPalette.Sand, 0.18f), 0f, 0.08f);
            var woodHallMat = Mat("WoodHall", Color.Lerp(HuaxiaPalette.WoodBrown, HuaxiaPalette.Cinnabar, 0.28f), 0f, 0.12f);

            // ------------------------------------------------------------ 山谷地形
            MeshObject("Ground", root, Vector3.zero,
                MeshFactory.Terrain(180f, 110, ValleyHeight, ValleyTint), groundMat, true);

            // ------------------------------------------------------------ 山门
            BuildPaifang(root, new Vector3(0f, 0f, GateZ), woodMat, woodDarkMat, roofMat, ridgeMat, stoneDarkMat, lampMat);

            // ------------------------------------------------------------ 南山道 + 三条支路
            // 连续的碎石夯土基底：石板之间的缝隙里是路面，不是草地
            var pathBaseMat = MatAs("PathBase", Color.Lerp(HuaxiaPalette.RockDark, HuaxiaPalette.Sand, 0.45f),
                "earth", 0f, 0.05f);
            Cyl("PathBase_Hub", root, new Vector3(0f, -0.11f, 0f), 13.5f, 0.34f, pathBaseMat, false);
            Box("PathBase_S", root, new Vector3(0f, -0.10f, -18.6f), new Vector3(3.7f, 0.34f, 31f), pathBaseMat, false);
            Box("PathBase_N", root, new Vector3(0f, -0.10f, 23.2f), new Vector3(4.1f, 0.34f, 23.2f), pathBaseMat, false);
            for (int r = 0; r < RouteAngle.Length; r++)
            {
                Vector3 rdir = RouteDir(r);
                var rbase = Box("PathBase_R" + r, root, new Vector3(rdir.x * 14.5f, -0.10f, rdir.z * 14.5f),
                    new Vector3(2.9f, 0.32f, 25f), pathBaseMat, false);
                rbase.transform.localRotation = Quaternion.Euler(0f, RouteAngle[r], 0f);
            }

            for (int i = 0; i < 13; i++)
            {
                float z = -31.5f + i * 2.15f;
                var slab = Box("Slab_S" + i, root, new Vector3(Mathf.Sin(i * 1.7f) * 0.09f, -0.01f, z),
                    new Vector3(3.1f, 0.14f, 2.06f), pathMat);
                slab.transform.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 0.9f, 0f);
            }

            for (int r = 0; r < RouteAngle.Length; r++)
            {
                Vector3 dir = RouteDir(r);
                for (int i = 0; i < 9; i++)
                {
                    float radius = 3.4f + i * 2.45f;
                    var p = dir * radius + Vector3.right * (Mathf.Sin(i * 2.3f + r) * 0.12f);
                    var slab = Box("Slab_R" + r + "_" + i, root, new Vector3(p.x, -0.01f, p.z),
                        new Vector3(2.35f, 0.14f, 2.10f), pathMat);
                    slab.transform.localRotation = Quaternion.Euler(0f, RouteAngle[r] + (i % 2 == 0 ? 1.2f : -1.2f), 0f);
                }
            }

            // 北向甬道：从「卷二」望柱背后一路铺到主楼踏跺，形成前庭中轴
            for (int i = 0; i < 15; i++)
            {
                float z = 12.2f + i * 1.62f;
                var slab = Box("Slab_N" + i, root, new Vector3(Mathf.Sin(i * 1.9f) * 0.07f, 0.10f, z),
                    new Vector3(3.6f, 0.16f, 1.56f), pathMat);
                slab.transform.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 0.5f, 0f);
            }
            // 前庭夯土：甬道两侧原来是一片空草地，主楼退远以后更显得远。
            // 铺一层同样的夯土，让广场、甬道、台阶连成一整片院子。
            Box("Court_N", root, new Vector3(0f, 0.02f, 24.5f), new Vector3(11.2f, 0.30f, 20.6f),
                pathBaseMat, false);

            // 甬道两侧牙石
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Slab_Curb", root, new Vector3(side * 2.05f, 0.15f, 23.6f),
                    new Vector3(0.36f, 0.26f, 24.4f), stoneMat);
            }

            // ------------------------------------------------------------ 三方望柱（章节入口）
            ChapterNode(root, 0, "Liangzhu", "良渚古城遗址", "卷一 · 城与玉", true, null,
                "城与玉——良渚文明", "良渚古城　距今约五千三百年",
                "从古城、水利系统与玉礼文化中，探索中华文明的早期形态。\n\n" +
                "左边这条路，走到头是良渚。那儿有一座泡在水里的城：城墙外头绕着稻田，八座水门进进出出。\n" +
                "走完一圈，你得把水重新放回田里去。\n\n" +
                "—— 一直走到路尽头那块石头跟前，站定了按 E，就过去了。");

            ChapterNode(root, 1, "Qin", "秦始皇陵", "卷二 · 帝与陵", true, null,
                "帝与陵——秦代文明", "秦始皇陵与兵马俑坑",
                "通过秦始皇陵与兵马俑，感受大一统时代的国家秩序与帝国气象。\n\n" +
                "中间这条路，走到头是秦陵。先过一道界门，再往上就是封土。\n" +
                "陪葬坑里的棚木得一根一根架起来——架完了，才看得见底下站着的人。\n\n" +
                "—— 走到界门跟前站定了按 E，就过去了。");

            ChapterNode(root, 2, "Quanzhou", "泉州", "卷三 · 港与海", true, null,
                "港与海——宋元泉州", "泉州　宋元的海上码头",
                "从刺桐古港出发，体验海上贸易、多元文化与文明交流。\n\n" +
                "右边这条路，走到头是泉州。从后渚港的码头走到九日山，等一阵风来，把船送出海。\n\n" +
                "—— 一直走到路尽头那块石头跟前，站定了按 E，就过去了。");

            // 进谷左侧的牌子：原来写的是「三卷分路」，现在换成这项 VR 体验的介绍。
            // 牌面先给书名，站定了按 E 才展开正文。
            BuildIntroSign(root, new Vector3(-5.50f, 0f, -20.60f), -20f, stoneDarkMat, bronzeMat);

            // 三条支路各点四盏石灯，把「路」一路引到雾里（甬道那条要让开牙石）
            for (int r = 0; r < 3; r++)
            {
                Vector3 rd = RouteDir(r);
                Vector3 rs = new Vector3(rd.z, 0f, -rd.x);
                float lo = r == 1 ? 3.05f : 1.95f;
                for (int k = 0; k < 2; k++)
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        var lp = rd * (15.4f + k * 4.6f) + rs * (lo * sgn);
                        BuildStoneLamp(root, lp, stoneDarkMat, lampMat, false);
                    }
            }

            // ------------------------------------------------------------ 古亭与残卷
            // 亭子在广场西缘（见 PavilionPos）：台基踩在广场石墁上，踏跺朝东下来。
            var pavilion = BuildPavilion(root, PavilionPos, PavilionYaw, woodMat, woodDarkMat, roofMat, ridgeMat,
                stoneDarkMat, bronzeMat, paperMat);
            // 石案上摊着残卷，只是布景——不挂 Interactable，走近没有 E。
            BuildScrollTable(pavilion, new Vector3(0f, 0f, 0.4f), stoneDarkMat, bronzeMat, paperMat);

            // ------------------------------------------------------------ 山尽头的主楼：藏卷阁
            BuildGreatHall(root, new Vector3(0f, 0f, 46.5f), 0f,
                woodHallMat, woodDarkMat, roofMat, ridgeMat, stoneMat, stoneDarkMat, wallMat, lampMat);

            // 东西配殿：围合前庭，避免主楼孤立
            BuildSideHall(root, new Vector3(-17.6f, 0f, 40.6f), 17f, 5.2f, 3.6f,
                "抄书处", "门锁着　纸还没裁完",
                woodHallMat, woodDarkMat, roofMat, ridgeMat, stoneMat, stoneDarkMat, wallMat, lampMat);
            BuildSideHall(root, new Vector3(17.6f, 0f, 40.6f), -17f, 5.2f, 3.6f,
                "值房", "守山人住的地方",
                woodHallMat, woodDarkMat, roofMat, ridgeMat, stoneMat, stoneDarkMat, wallMat, lampMat);

            // ------------------------------------------------------------ 三条路的尽头
            // 以前走到路口只有一根幡柱，按 E 就整个场景换掉，人不知道路是干什么的。
            // 现在路口只指方向，真正的门户在路尽头：一道界门、一块界石。
            RouteEnd(root, 0, "Liangzhu", "Liangzhu", "from_hub", "良渚", "卷一 · 城与玉", "back_liangzhu",
                21.5f, 0f, 0f, woodMat, roofMat, stoneDarkMat, lampMat, pineMat, bambooMat);
            RouteEnd(root, 2, "Quanzhou", "Quanzhou", "from_hub", "泉州", "卷三 · 港与海", "back_quanzhou",
                21.5f, 0f, 0f, woodMat, roofMat, stoneDarkMat, lampMat, pineMat, bambooMat);
            // 中路是通往藏卷阁的甬道，不能横着立门：秦陵的门退到甬道左手边，斜对着来路。
            // 退到 23.5 是为了让从秦陵回来的落点在甬道与支路接头处，不在路上也不在建筑上。
            RouteEnd(root, 1, "Qinling", "Qinling", "from_hub", "秦陵", "卷二 · 帝与陵", "back_qinling",
                23.5f, -6.6f, 0f, woodMat, roofMat, stoneDarkMat, lampMat, pineMat, bambooMat);

            // 秦陵的门退在甬道西侧，得有一小段路把它和甬道接起来
            Box("Spur_Qinling", root, new Vector3(-2.95f, 0.02f, 23.5f), new Vector3(3.4f, 0.30f, 3.2f),
                pathBaseMat, false);
            for (int i = 0; i < 3; i++)
                Box("Slab_Q" + i, root, new Vector3(-1.55f - i * 1.15f, 0.10f, 23.5f + (i % 2 - 0.5f) * 0.2f),
                    new Vector3(1.16f, 0.14f, 2.5f), pathMat);

            // 藏卷阁正门口：这间就是终章。三处遗址都进去过，门自己开；
            // 不要求把每卷的线索看完——门认的是「去过」，不是「读完」。
            // 门后面就是那间书房，所以跨过门槛直接过去，不用再按一次 E。
            var hallGate = Empty("HallGate", root, new Vector3(0f, 0.1f, 43.8f));
            var hallPortal = hallGate.AddComponent<ScenePortal>();
            hallPortal.sceneToLoad = "Epilogue";
            hallPortal.spawnId = "epilogue_start";
            hallPortal.line = "推门进去　·　终章 · 河山册";
            hallPortal.destinationLabel = "终章 · 河山册";
            hallPortal.lockedLine = "门还闩着　—— 三处遗址都进去过才开";
            hallPortal.requireAllChapters = true;
            hallPortal.autoEnter = true;
            hallPortal.radius = 2.4f;
            hallPortal.innerRadius = 1.70f;

            // ------------------------------------------------------------ 出生点
            // 跨场景回来时，人要站在原来那条路的路口，而不是被扔回山门外。
            var startSpawn = Empty("Spawn_start", root, new Vector3(0f, 0.1f, -29.5f));
            startSpawn.AddComponent<SpawnPoint>().id = "start";

            var epilogueSpawn = Empty("Spawn_after_epilogue", root, new Vector3(0f, 0.1f, -20.5f));
            epilogueSpawn.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            epilogueSpawn.AddComponent<SpawnPoint>().id = "after_epilogue";

            // 从遗址里按 Q 合卷回来，落在这方圆坛上，正面朝着三条岔路。
            // 原来退在被走的那条路尽头、脸朝山谷——转身以前看着像被退回了半路。
            var hubCenter = Empty("Spawn_hub_center", root, new Vector3(0f, 0.1f, -2.2f));
            hubCenter.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);   // 朝 +Z，正对三根望柱
            hubCenter.AddComponent<SpawnPoint>().id = "hub_center";

            // ------------------------------------------------------------ 水潭
            BuildPond(root, new Vector3(13.5f, 0f, -8.5f), waterMat, stoneMat, bambooMat);
            BuildArchBridge(root, new Vector3(13.5f, 0f, -8.5f), 0f, 11.4f, 2.7f, 1.35f, stoneMat, stoneDarkMat);

            // ------------------------------------------------------------ 石灯 / 香炉 / 石碑
            Vector3[] lamps =
            {
                new Vector3(-2.9f, 0f, -26.5f), new Vector3(2.9f, 0f, -22.5f),
                new Vector3(-2.9f, 0f, -18.5f), new Vector3(2.9f, 0f, -14.5f),
                new Vector3(-2.9f, 0f, -10.0f), new Vector3(-3.9f, 0f, 1.4f),
                new Vector3(3.9f, 0f, 1.4f),
            };
            foreach (var p in lamps) BuildStoneLamp(root, p, stoneDarkMat, lampMat);

            BuildCenser(root, new Vector3(0f, 0f, 0.6f), bronzeMat, stoneDarkMat);

            // 碑面朝路心：原来的 74° 让字背对主路，玩家看到的是碑背。
            // 正文手动断行，每行控制在 13 字内，避免「，」被挤到单独一行。
            BuildStele(root, new Vector3(-6.6f, 0f, -8.2f), 254f, stoneDarkMat,
                "复原说明", "本园区为研究性数字复原。\n几何、材质与器物形制\n均为示意，不作为考古依据。");

            // ------------------------------------------------------------ 草木
            BuildTree(root, "Maple_Hero", new Vector3(-15.0f, 0f, -8.0f), 1.45f, mapleMat, maplePaleMat, false, 3);
            BuildTree(root, "Maple_B", new Vector3(-13.0f, 0f, -11.0f), 1.0f, mapleMat, maplePaleMat, false, 19);
            BuildTree(root, "Maple_C", new Vector3(11.6f, 0f, -18.0f), 0.95f, mapleMat, maplePaleMat, false, 27);
            BuildTree(root, "Maple_D", new Vector3(-23.6f, 0f, 4.6f), 1.15f, maplePaleMat, mapleMat, false, 33);
            BuildTree(root, "Pine_A", new Vector3(-19.6f, 0f, 20.5f), 1.15f, pineMat, pineMat, true, 7);
            BuildTree(root, "Pine_B", new Vector3(20.0f, 0f, 23.0f), 1.2f, pineMat, pineMat, true, 11);
            BuildTree(root, "Pine_C", new Vector3(-20.5f, 0f, -5.5f), 1.0f, pineMat, pineMat, true, 23);
            BuildTree(root, "Pine_D", new Vector3(22.5f, 0f, -6.5f), 1.1f, pineMat, pineMat, true, 29);
            BuildBamboo(root, new Vector3(-9.8f, 0f, -19.0f), bambooMat, 0.95f);
            BuildBamboo(root, new Vector3(9.2f, 0f, -21.0f), bambooMat, 0.8f);
            BuildBamboo(root, new Vector3(-22.0f, 0f, 12.0f), bambooMat, 1.1f);
            BuildBamboo(root, new Vector3(19.5f, 0f, 14.0f), bambooMat, 1.0f);

            BuildTree(root, "Pine_E", new Vector3(-26.2f, 0f, 25.0f), 1.3f, pineMat, pineMat, true, 41);
            BuildTree(root, "Pine_F", new Vector3(26.4f, 0f, 26.5f), 1.35f, pineMat, pineMat, true, 43);
            BuildTree(root, "Pine_G", new Vector3(-14.0f, 0f, 41.0f), 1.5f, pineMat, pineMat, true, 47);
            BuildTree(root, "Pine_H", new Vector3(15.0f, 0f, 42.0f), 1.45f, pineMat, pineMat, true, 53);
            BuildTree(root, "Maple_E", new Vector3(-24.5f, 0f, -2.0f), 1.2f, mapleMat, maplePaleMat, false, 59);
            BuildTree(root, "Maple_F", new Vector3(24.0f, 0f, -3.5f), 1.15f, maplePaleMat, mapleMat, false, 61);
            BuildTree(root, "Maple_G", new Vector3(-25.0f, 0f, 27.5f), 1.25f, mapleMat, maplePaleMat, false, 67);
            BuildTree(root, "Maple_H", new Vector3(25.2f, 0f, 26.5f), 1.2f, maplePaleMat, mapleMat, false, 71);

            Vector3[] bushes =
            {
                new Vector3(-8.0f, 0f, -13.0f), new Vector3(7.5f, 0f, -12.0f),
                new Vector3(-8.0f, 0f, 0.9f), new Vector3(10.5f, 0f, 2.5f),
                new Vector3(-12.6f, 0f, 18.4f), new Vector3(6.5f, 0f, 18.5f),
                new Vector3(18.5f, 0f, -14.5f), new Vector3(-16.0f, 0f, -16.0f),
            };
            for (int i = 0; i < bushes.Length; i++)
            {
                var bush = MeshObject("Bush_" + i, root, bushes[i],
                    MeshFactory.Canopy(0.95f + (i % 3) * 0.22f, 5 + i), bushMat, false);
                bush.transform.localScale = new Vector3(1f, 0.55f, 1f);
            }

            // ------------------------------------------------------------ 远山
            MeshObject("Ridge_Near", root, new Vector3(-40f, 0f, 86f),
                MeshFactory.MountainRidge(330f, 40f, 6, 13, 18f), nearMat, false);
            MeshObject("Ridge_Mid", root, new Vector3(46f, 0f, 118f),
                MeshFactory.MountainRidge(450f, 56f, 7, 8, 26f), midMat, false);
            MeshObject("Ridge_Far", root, new Vector3(-26f, 0f, 158f),
                MeshFactory.MountainRidge(580f, 74f, 8, 3, 34f), farMat, false);
            MeshObject("Ridge_East", root, new Vector3(116f, 0f, 14f),
                MeshFactory.MountainRidge(340f, 46f, 6, 29, 22f), midMat, false).transform.localRotation =
                Quaternion.Euler(0f, 90f, 0f);
            MeshObject("Ridge_West", root, new Vector3(-116f, 0f, 10f),
                MeshFactory.MountainRidge(340f, 48f, 6, 31, 22f), midMat, false).transform.localRotation =
                Quaternion.Euler(0f, -90f, 0f);
            MeshObject("Ridge_Back", root, new Vector3(0f, 0f, -104f),
                MeshFactory.MountainRidge(460f, 44f, 7, 21, 28f), farMat, false).transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);

            // ------------------------------------------------------------ 光照
            var sunGO = new GameObject("DuskSun", typeof(Light));
            sunGO.transform.SetParent(root, false);
            sunGO.transform.rotation = Quaternion.Euler(15f, 160f, 0f);
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Lin(new Color(1f, 0.94f, 0.86f));
            sun.intensity = 1.70f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.90f;
            sun.shadowBias = 0.05f;
            RenderSettings.sun = sun;

            // 冷色补光（不投影）：避免背光面靠 _ShadowTint 硬压成黑块
            var fillGO = new GameObject("FillLight", typeof(Light));
            fillGO.transform.SetParent(root, false);
            fillGO.transform.rotation = Quaternion.Euler(28f, 342f, 0f);
            var fill = fillGO.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Lin(new Color(0.54f, 0.66f, 0.84f));
            fill.intensity = 0.74f;
            fill.shadows = LightShadows.None;

            AddPoint(root, "Fill_Hub", new Vector3(0f, 3.4f, 2.0f), HuaxiaPalette.AmberPale, 20f, 0.70f);
            AddPoint(root, "Fill_Pavilion", PavilionPos + new Vector3(0f, 2.7f, 0f),
                HuaxiaPalette.AmberPale, 13f, 0.55f);
            AddPoint(root, "Fill_Road", new Vector3(0f, 3.0f, -16f), HuaxiaPalette.DuskAmbient, 24f, 0.45f);
            AddPoint(root, "Fill_Pond", new Vector3(13.5f, 2.2f, -8.5f), HuaxiaPalette.DuskAmbient, 18f, 0.35f);
            AddPoint(root, "Fill_Hall_Up", new Vector3(0f, 8.3f, 46.5f), HuaxiaPalette.AmberPale, 19f, 0.95f);
            AddPoint(root, "Fill_Hall_Low", new Vector3(0f, 4.9f, 46.5f), HuaxiaPalette.AmberPale, 17f, 0.80f);

            CreateDust(root, new Vector3(0f, 3.0f, -8f), new Vector3(44f, 7f, 46f), 240, 9f);

            // ------------------------------------------------------------ 遗产立牌 · 展台 · 讲解员
            // 每条岔口一块立牌；三处遗产的微缩模型藏在 y=-800 的展台里，只有面板的展台相机看得见
            EnsureLayers();
            BuildHeritageBoards(root);
            BuildHeritageShowcases(root);
            // 弘文站在《河山册》总说明牌前面（牌面右前方、靠步道这一头），面朝山门：
            // 玩家一进谷抬头看见书名，她就立在牌子底下，但她不主动开口 ——
            // 玩家顺着步道走过来踩到路上那只圈，她才迎上来讲开场白。
            var hongwenPos = new Vector3(-3.30f, 0f, -22.30f);
            hongwenPos.y = GroundYAt(hongwenPos, 0.02f) + 0.02f;
            var toPlayer = new Vector3(0f, 0f, -29.5f) - hongwenPos;
            var hongwen = CreateDigitalHuman(root, hongwenPos, Mathf.Atan2(-toPlayer.x, -toPlayer.z) * Mathf.Rad2Deg);

            // 开场圈摆在步道中轴上、说明牌正南：牌和她都在玩家左手边，
            // 一路走过来必定踩进来。圈是给玩家的落脚点，画在她自己脚下反而没人敢站。
            var introAnchor = Empty("IntroRingAnchor", root, new Vector3(0f, 0f, -22.30f));
            var introRing = BuildGuideRing(root, introAnchor.transform, "intro", 0f, 2.2f);
            introRing.board = null;              // 圈不在牌前，算落脚点时按「迎向玩家」那一支走
            introRing.radius = 1.9f;             // 步道宽 3.7 m，中轴左右各 1.85 m 都算踩到
            introRing.stayInPlace = true;        // 她就站在牌前不动：玩家走进圈，她转身开口即可

            // 开场白讲完，她把人领到三岔口南头站着：三块立牌都在十来米内，
            // 玩家在那之后再踩圈，她几步就到，不用横穿整个广场。
            var parkSpot = new Vector3(1.90f, 0f, -4.20f);
            parkSpot.y = FloorYAt(parkSpot, 0.02f) + 0.02f;
            hongwen.parkSpot = parkSpot;

            // ------------------------------------------------------------ 玩家与 UI
            // 点光源的 ForwardAdd 通道会整块叠加到半透明水面上，把水面层从点光源剔除
            foreach (var pl in Object.FindObjectsOfType<Light>())
                if (pl.type == LightType.Point)
                    pl.cullingMask &= ~(1 << 8);

            var player = CreatePlayer(new Vector3(0f, 0.1f, -29.5f), 0f);
            ApplyGrade(player, "archive");
            var cam = player.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;

            var ui = CreateHUD("WASD 走 · 鼠标看 · E 互动 · Tab 取《河山册》 · 滚轮翻页 · Esc 松开鼠标");
            ui.SetChapterTitle("暮色古道", "序章");
            ui.SetObjectives(
                "<color=#8C8579>眼下</color>\n" +
                "◇ 顺着山道走进山谷，地上那只光圈站一下\n" +
                "◇ 听弘文讲完，再跟她往三岔口走\n" +
                "◇ 每条岔口一块立牌，走到牌前站一下\n" +
                "◇ 认幡柱看方向，站定了按 E 细看\n" +
                "◇ 听她指的路，走到路尽头那道界门按 E\n" +
                "◇ 亭子里那半卷残卷，可以翻一翻\n\n" +
                "<color=#8C8579>按 E 说话 / 开门　Tab 取册子 · 滚轮翻页　空格 确认　弘文会一路跟着讲</color>");
            ui.ShowToast("你手里只有半卷《河山册》。三条岔口各立了一块牌，弘文就立在山门里那块书名牌子前等你。");
            // 三卷的勾要按存下来的进度现算，构建时写死的那份会被它覆盖
            ui.gameObject.AddComponent<HubBoard>();

            // 进门不再弹整屏说明：开场那段话交给弘文在引导圈里讲，
            // 玩家一进门看到的是山谷和书名，而不是一堵挡住视线的字。

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneDir + "/Archive.unity");
            Debug.Log("[Huaxia] 序章「暮色古道」已保存");
        }

        // ================================================================ 地形

        static float CorridorMask(float ax, float az, float bx, float bz, float halfW, float feather, float x, float z)
        {
            float abx = bx - ax, abz = bz - az;
            float len2 = abx * abx + abz * abz;
            float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(((x - ax) * abx + (z - az) * abz) / len2);
            float cx = ax + abx * t, cz = az + abz * t;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
            return Mathf.Clamp01(1f - Mathf.InverseLerp(halfW, halfW + feather, d));
        }

        static float ValleyHeight(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.062f + 7.3f, z * 0.062f + 2.9f) - 0.5f;
            float n2 = Mathf.PerlinNoise(x * 0.017f + 31f, z * 0.017f + 5f) - 0.5f;

            // 山脊噪声：1-|noise| 产生尖脊，避免山体变成光滑的沙丘拱包
            float ridged = 0f, amp = 1f, norm = 0f, freq = 0.026f;
            for (int o = 0; o < 3; o++)
            {
                float v = 1f - Mathf.Abs(Mathf.PerlinNoise(x * freq + 13.7f * o, z * freq + 43.3f * o) * 2f - 1f);
                ridged += v * amp;
                norm += amp;
                amp *= 0.52f;
                freq *= 2.15f;
            }
            ridged /= Mathf.Max(norm, 1e-4f);

            float hills =
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14f, 52f, Mathf.Abs(x))) * (17f + n2 * 20f) +
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(28f, 86f, z)) * (27f + n2 * 14f) +
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-28f, -84f, z)) * (22f + n2 * 12f);
            hills *= 0.62f + 0.82f * ridged;

            float flat = CorridorMask(0f, -33f, 0f, -4f, 3.2f, 8f, x, z);
            for (int i = 0; i < RouteAngle.Length; i++)
            {
                Vector3 d = RouteDir(i);
                flat = Mathf.Max(flat,
                    CorridorMask(0f, 0f, d.x * (RouteLength + 5f), d.z * (RouteLength + 5f), 2.4f, 8f, x, z));
            }
            flat = Mathf.Max(flat, Mathf.Clamp01(1f - Mathf.InverseLerp(7.5f, 17f, Mathf.Sqrt(x * x + z * z))));

            // 山尽头主楼的台基场地：先压平再落建筑，避免台基被地形吃掉
            float padD = Mathf.Sqrt(x * x * 0.72f + (z - 46.5f) * (z - 46.5f));
            flat = Mathf.Max(flat, Mathf.Clamp01(1f - Mathf.InverseLerp(9f, 22f, padD)));

            // 水潭：压平池岸，让水面与地面齐平，池形不被地形噪声切碎
            float pondD = Mathf.Sqrt((x - 13.5f) * (x - 13.5f) + (z + 8.5f) * (z + 8.5f));
            flat = Mathf.Max(flat, Mathf.Clamp01(1f - Mathf.InverseLerp(7.2f, 11.5f, pondD)));

            // 前庭：主楼前的院落整体压平成广场，容纳东西配殿
            float courtD = Mathf.Sqrt(x * x * 0.34f + (z - 41f) * (z - 41f));
            flat = Mathf.Max(flat, Mathf.Clamp01(1f - Mathf.InverseLerp(13f, 34f, courtD)));

            return hills * (1f - flat) + n * 0.16f * flat;
        }

        static Color ValleyTint(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.11f + 5.7f, z * 0.11f + 13.2f);
            float g = Mathf.PerlinNoise(x * 0.028f + 41f, z * 0.028f + 17f);
            float slope = Mathf.Clamp01(Mathf.PerlinNoise(x * 0.05f + 61f, z * 0.05f + 9f) * 1.4f - 0.2f);

            // 土色去饱和（偏灰褐），草地压成苔绿；高处偏岩灰，避免整体发橙
            var earth = Color.Lerp(new Color(0.40f, 0.37f, 0.30f), new Color(0.50f, 0.46f, 0.37f), n);
            var grass = Color.Lerp(new Color(0.26f, 0.33f, 0.21f), new Color(0.19f, 0.26f, 0.17f), n);
            var rock = Color.Lerp(new Color(0.42f, 0.41f, 0.38f), HuaxiaPalette.RockDark, slope * 0.5f);

            var c = Color.Lerp(earth, grass, Mathf.Clamp01((g - 0.24f) * 2.4f));

            // 前庭广场：踩踏过的夯土，压掉「草坪感」
            float fore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(21f, 14f, Mathf.Abs(x)))
                       * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-3f, 6f, z))
                       * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(58f, 46f, z));
            c = Color.Lerp(c, new Color(0.47f, 0.43f, 0.34f), fore * 0.70f);

            return Color.Lerp(c, rock, slope * 0.45f);
        }

        // ================================================================ 章节望柱

        /// <summary>
        /// 三岔口的路标：一根幡柱 + 幡上卷名 + 竿头灯。
        /// 幡柱立在广场北缘：6.2 m 竿 + 2.9 m 纸幡，从牌坊（37 m 外）看过去是三个有颜色的竖条，
        /// 走近才读得到幡上的卷名。远处靠幡色和竿头灯认出「那里有路」，近处靠字认出「那是哪条路」。
        /// </summary>
        static Interactable ChapterNode(Transform parent, int index, string key, string label,
                                        string volumeName, bool unlocked, string sceneToLoad,
                                        string title, string subtitle, string body)
        {
            Vector3 pos = WaystonePos[index];

            // 望柱正面（局部 -Z）掉头朝南山道，玩家从牌坊进来正好读到幡上的卷名
            Vector3 toGate = GateLookAt - pos;
            float yaw = Mathf.Atan2(-toGate.x, -toGate.z) * Mathf.Rad2Deg;

            var stoneMat = Mat("StoneDark", HuaxiaPalette.RockDark, 0f, 0.2f);
            var woodDarkMat = Mat("WoodDark", HuaxiaPalette.WoodDark, 0f, 0.18f);
            var bronzeMat = Mat("Bronze", Color.Lerp(HuaxiaPalette.Amber, HuaxiaPalette.WoodDark, 0.5f), 0.1f, 0.26f);
            var g = Empty("Waypole_" + key, parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // 三卷各一种幡色：水青、土赭、海蓝
            Color volumeTint = index == 0 ? HuaxiaPalette.InkTeal
                              : index == 1 ? HuaxiaPalette.Terracotta
                                           : HuaxiaPalette.Azurite;
            Color bannerBase = Color.Lerp(HuaxiaPalette.PaperWarm, volumeTint, 0.42f);

            // 柱脚原来有一圈自发光地圈，暮色里在三条路面上各亮一摊黄，晃眼。
            // 现在只留石座，路口靠幡色和竿头灯认。
            Box("Pedestal", g.transform, new Vector3(0f, 0.24f, 0f), new Vector3(1.36f, 0.48f, 1.36f), stoneMat);
            Cyl("Shaft", g.transform, new Vector3(0f, 3.36f, 0f), 0.095f, 6.22f, woodDarkMat);
            Cyl("ShaftBase", g.transform, new Vector3(0f, 0.86f, 0f), 0.16f, 1.10f, stoneMat);

            // 竿顶横臂：幡挂在臂上，向一侧飘出去，三根并排时不会互相压住
            Box("Arm", g.transform, new Vector3(0.52f, 6.00f, 0f), new Vector3(1.28f, 0.09f, 0.09f),
                woodDarkMat, false);
            Box("ArmTip", g.transform, new Vector3(1.14f, 5.94f, 0f), new Vector3(0.10f, 0.20f, 0.10f),
                bronzeMat, false);

            var bannerMat = EmissiveMat("WayBanner", bannerBase,
                Color.Lerp(bannerBase, volumeTint, 0.55f) * (unlocked ? 0.85f : 0.22f), 0f, 0.28f);
            Box("Banner", g.transform, new Vector3(0.60f, 4.52f, 0f), new Vector3(0.94f, 2.86f, 0.03f),
                bannerMat, false);
            Box("BannerRodT", g.transform, new Vector3(0.60f, 5.98f, 0f), new Vector3(1.02f, 0.06f, 0.08f),
                bronzeMat, false);
            Box("BannerRodB", g.transform, new Vector3(0.60f, 3.06f, 0f), new Vector3(1.02f, 0.05f, 0.08f),
                bronzeMat, false);

            // 竿头一盏小灯：远处只有这一点暖光点得着的亮
            var lampMat = EmissiveMat("LanternPaper", HuaxiaPalette.PaperWarm, HuaxiaPalette.AmberPale * 0.42f, 0f, 0.2f);
            var orb = Ball("Orb", g.transform, new Vector3(0f, 6.66f, 0f), 0.20f, lampMat);
            if (unlocked) orb.AddComponent<Pulse>().amplitude = 0.08f;

            var lightGO = new GameObject("Glow");
            lightGO.transform.SetParent(g.transform, false);
            lightGO.transform.localPosition = new Vector3(0.30f, 2.20f, -1.10f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Lin(unlocked ? Color.Lerp(HuaxiaPalette.AmberPale, volumeTint, 0.25f)
                                       : HuaxiaPalette.BlueGrey);
            // 亮度压下来：2.4 的暖光在脚边洇出一大摊黄，正是要去掉的那股「引导光」
            light.range = 9.0f;
            light.intensity = unlocked ? 1.15f : 0.30f;
            light.shadows = LightShadows.None;

            // 幡上只写卷名（三字），站名刻在柱脚的小石板上
            string shortName = volumeName;
            int dot = volumeName.IndexOf('·');
            if (dot >= 0 && dot + 1 < volumeName.Length) shortName = volumeName.Substring(dot + 1).Trim();

            string vertical = string.Empty;
            for (int i = 0; i < shortName.Length; i++)
                vertical += (i == 0 ? "" : "\n") + shortName[i];

            var plate = CreateTextPlate(g.transform, vertical, new Vector3(0.60f, 4.52f, -0.028f), Vector3.zero,
                new Vector2(0.90f, 2.82f), 400,
                unlocked ? HuaxiaPalette.InkTealDeep : HuaxiaPalette.RockDark);
            var pt = plate.GetComponentInChildren<UnityEngine.UI.Text>();
            if (pt != null)
            {
                pt.verticalOverflow = VerticalWrapMode.Overflow;
                pt.lineSpacing = 0.95f;
            }

            CreateTextPlate(g.transform, label, new Vector3(0f, 0.26f, -0.702f), Vector3.zero,
                new Vector2(1.06f, 0.22f), 74, unlocked ? HuaxiaPalette.AmberPale : HuaxiaPalette.BlueGrey);

            var interactable = g.AddComponent<Interactable>();
            // 幡柱只负责「这条路去哪」；真正过去的地方在路尽头（ScenePortal）。
            // 站在路口就一步跨进遗址，会让人不知道路是干什么用的。
            interactable.kind = string.IsNullOrEmpty(sceneToLoad) ? InteractKind.Info : InteractKind.Chapter;
            interactable.prompt = string.IsNullOrEmpty(sceneToLoad)
                ? "[E] 看看这条路"
                : "[E] 走进「" + volumeName + "」";
            interactable.title = title;
            interactable.subtitle = subtitle;
            interactable.body = body;
            interactable.sceneToLoad = unlocked ? sceneToLoad : null;
            interactable.markerRenderer = orb.GetComponent<Renderer>();
            interactable.markerColor = unlocked ? HuaxiaPalette.AmberPale : HuaxiaPalette.BlueGrey;

            var trigger = g.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.3f;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            return interactable;
        }

        // ================================================================ 路的尽头

        /// <summary>
        /// 一条支路的尽头：一道界门、一块界石、两盏灯，门后一堆山石把路收住。
        /// 走到跟前会浮出「按 E 过去」，按了就淡黑落到下一处遗址。
        /// </summary>
        static void RouteEnd(Transform parent, int index, string key, string sceneName, string spawnId,
                             string placeName, string volumeName, string spawnBack,
                             float radius, float lateral, float yawOffset,
                             Material wood, Material roof, Material stone, Material lamp,
                             Material leaf, Material leafAlt)
        {
            Vector3 dir = RouteDir(index);
            Vector3 side = new Vector3(dir.z, 0f, -dir.x);

            var g = Empty("RouteEnd_" + key, parent, dir * radius + side * lateral);
            g.transform.localRotation = Quaternion.Euler(0f, RouteAngle[index] + yawOffset, 0f);

            // 门前石坪：让门站得住，也让「这儿是尽头」看得见
            Box("Apron", g.transform, new Vector3(0f, 0.02f, 0.1f), new Vector3(5.4f, 0.10f, 2.8f), stone, false);

            for (int s = -1; s <= 1; s += 2)
            {
                Box("PostFoot", g.transform, new Vector3(1.92f * s, 0.13f, 0f),
                    new Vector3(0.62f, 0.26f, 0.62f), stone);
                Box("Post", g.transform, new Vector3(1.92f * s, 1.75f, 0f),
                    new Vector3(0.30f, 3.05f, 0.30f), stone);
            }
            Box("Lintel", g.transform, new Vector3(0f, 3.22f, 0f), new Vector3(4.40f, 0.22f, 0.34f), wood);
            Box("PlaqueBoard", g.transform, new Vector3(0f, 2.90f, 0f), new Vector3(2.05f, 0.46f, 0.10f), wood);
            CreateTextPlate(g.transform, volumeName, new Vector3(0f, 2.90f, -0.062f), Vector3.zero,
                new Vector2(1.95f, 0.40f), 58, HuaxiaPalette.PaperWarm, title: true);

            MeshObject("GateRoof", g.transform, new Vector3(0f, 3.30f, 0f),
                MeshFactory.CurvedRoof(2.62f, 1.16f, 0.84f, 0.24f, 0.11f, 14, 12), roof, false);

            BuildStele(g.transform, new Vector3(-2.95f, 0f, 0.75f), 14f, stone,
                placeName, volumeName + "\n\n站到门前按 E 进入");

            BuildStoneLamp(g.transform, new Vector3(-2.55f, 0f, 1.75f), stone, lamp, false);
            BuildStoneLamp(g.transform, new Vector3(2.55f, 0f, 1.75f), stone, lamp, false);

            // 门后把路收住：山石、一株松、一丛竹。看得见的是「到头了」，看不见路还往哪拐
            MeshObject("Rock_A", g.transform, new Vector3(0.2f, 0f, 3.9f),
                MeshFactory.Canopy(1.55f, 91), stone, false).transform.localScale = new Vector3(1.25f, 0.80f, 1.05f);
            MeshObject("Rock_B", g.transform, new Vector3(-2.1f, 0f, 4.9f),
                MeshFactory.Canopy(1.25f, 92), stone, false).transform.localScale = new Vector3(1.10f, 0.95f, 1.0f);
            MeshObject("Rock_C", g.transform, new Vector3(2.2f, 0f, 5.1f),
                MeshFactory.Canopy(1.15f, 93), stone, false).transform.localScale = new Vector3(1.05f, 0.70f, 1.15f);

            BuildTree(g.transform, "Pine_End", new Vector3(4.6f, 0f, 7.0f), 1.15f, leaf, leafAlt, true, 71 + index * 4);
            BuildBamboo(g.transform, new Vector3(-4.4f, 0f, 6.2f), leafAlt, 0.95f);

            var portal = g.AddComponent<ScenePortal>();
            portal.sceneToLoad = sceneName;
            portal.spawnId = spawnId;
            portal.line = "[E] 走进「" + placeName + "」";
            portal.destinationLabel = volumeName + "　" + placeName;
            portal.radius = 3.15f;

            // 从遗址回来时站在这儿：面朝山谷，路在眼前
            Vector3 backPos = dir * (radius - 3.7f) + side * lateral;
            var back = Empty("Spawn_" + spawnBack, parent, new Vector3(backPos.x, 0.1f, backPos.z));
            back.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg, 0f);
            var sp = back.AddComponent<SpawnPoint>();
            sp.id = spawnBack;

            AddPoint(parent, "PortalGlow_" + key, dir * (radius - 0.8f) + side * lateral + Vector3.up * 2.7f,
                HuaxiaPalette.AmberPale, 13f, 0.55f);
        }

        // ================================================================ 体验介绍牌

        /// <summary>
        /// 进谷左侧的介绍牌：牌面先写书名与英文名，站定了按 E 展开这项 VR 体验的简介。
        /// </summary>
        static void BuildIntroSign(Transform parent, Vector3 pos, float yaw, Material stone, Material bronze)
        {
            var g = Empty("IntroSign", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box("Base", g.transform, new Vector3(0f, 0.15f, 0f), new Vector3(3.30f, 0.30f, 1.02f), stone);
            for (int s = -1; s <= 1; s += 2)
                Box("Post", g.transform, new Vector3(1.36f * s, 0.72f, 0f), new Vector3(0.22f, 1.44f, 0.30f), stone);

            Box("Panel", g.transform, new Vector3(0f, 1.92f, 0f), new Vector3(2.94f, 1.62f, 0.14f), stone);
            // 灯箱式牌面：暮色里石面读不出字，改成一片发光的纸面 + 墨字
            Box("Paper", g.transform, new Vector3(0f, 1.92f, -0.077f), new Vector3(2.78f, 1.46f, 0.02f),
                EmissiveMat("SignPaper", HuaxiaPalette.PaperWarm, HuaxiaPalette.AmberPale * 0.45f, 0f, 0.3f), false);
            Box("Rail", g.transform, new Vector3(0f, 2.75f, 0f), new Vector3(3.04f, 0.09f, 0.20f), bronze);
            Box("Rail_Low", g.transform, new Vector3(0f, 1.09f, 0f), new Vector3(3.04f, 0.08f, 0.20f), bronze);

            // 书名两行 + 英文名，中间一道细铜线
            CreateTextPlate(g.transform, "《河山册：华夏文明\n沉浸探索之旅》", new Vector3(0f, 2.18f, -0.088f),
                Vector3.zero, new Vector2(2.62f, 0.66f), 42, HuaxiaPalette.InkTealDeep, title: true);
            Box("Rule", g.transform, new Vector3(0f, 1.78f, -0.090f), new Vector3(2.30f, 0.018f, 0.008f), bronze, false);
            CreateTextPlate(g.transform, "The Atlas of Chinese Civilization", new Vector3(0f, 1.50f, -0.088f),
                Vector3.zero, new Vector2(2.62f, 0.26f), 21, HuaxiaPalette.InkTealDeep);

            var lgo = new GameObject("Light", typeof(Light));
            lgo.transform.SetParent(g.transform, false);
            lgo.transform.localPosition = new Vector3(0f, 2.45f, -0.55f);
            var l = lgo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = Lin(HuaxiaPalette.AmberPale);
            l.range = 8.0f;
            l.intensity = 1.35f;
            l.shadows = LightShadows.None;

            var interactable = g.AddComponent<Interactable>();
            interactable.kind = InteractKind.Info;
            interactable.hideTier = true;      // 这是体验介绍，不是考据牌
            interactable.prompt = "[E] 了解《河山册》";
            interactable.title = "《河山册：华夏文明沉浸探索之旅》";
            interactable.subtitle = "The Atlas of Chinese Civilization";
            interactable.body =
                "《河山册》是一项以中华文明发展脉络为叙事主线的VR沉浸式文化体验项目，选取良渚古城遗址、" +
                "秦始皇陵及泉州：宋元中国的世界海洋商贸中心三处世界文化遗产作为体验场景，" +
                "构建跨越不同历史时期的文明探索之旅。\n\n" +
                "项目以一册承载文明记忆的古籍为叙事线索，观众将以第一人称视角进入三个历史场景，" +
                "在空间探索、遗址观察与交互体验中，感知中华文明从早期城市文明、秦代国家建构到宋元海洋贸易的演进历程。" +
                "三个场景分别围绕良渚文明的城市营建与信仰体系、秦陵所体现的帝国秩序与地下世界，" +
                "以及泉州所呈现的海上贸易与多元文化交流展开。\n\n" +
                "项目基于Unity引擎开发，采用PICO 4 Enterprise头显设备，通过6DoF空间追踪与沉浸式交互技术，" +
                "将文化遗产知识融入可探索、可感知的三维环境，探索世界文化遗产的数字化阐释与沉浸式体验设计。";
            interactable.markerRenderer = null;

            var trigger = g.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.1f;
            trigger.center = new Vector3(0f, 1.5f, 0f);
        }

        // ================================================================ 灯与尘

        static Light AddPoint(Transform parent, string name, Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        static void CreateDust(Transform parent, Vector3 pos, Vector3 size, int max, float rate)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(16f, 26f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.010f, 0.030f);
            main.startColor = new Color(0.95f, 0.87f, 0.70f, 0.20f);
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.10f, 0.10f);
            vel.y = new ParticleSystem.MinMaxCurve(0.015f, 0.06f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.10f, 0.10f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial.color = new Color(0.96f, 0.88f, 0.70f, 0.20f);
        }

        // ================================================================ 正门预览

        /// <summary>只拍藏卷阁正门：关着、开着、以及开着时从门框往里看。</summary>
        [MenuItem("Huaxia/渲染藏卷阁正门预览")]
        public static void RenderHallDoorPreviews()
        {
            string scene = SceneDir + "/Archive.unity";
            Vector3 from = new Vector3(0f, 1.86f, 33.6f);
            Vector3 at = new Vector3(0f, 3.90f, 42.6f);

            RenderPreview(scene, from, at, "Previews/archive_door_closed.png", 58f);

            // 开着的样子：门轴转过去、门闩撤掉，和运行时 HallDoor 做的一样
            RenderPreview(scene, from, at, "Previews/archive_door_open.png", 58f, -1f, delegate
            {
                OpenHallDoorForPreview();
            });

            RenderPreview(scene, new Vector3(0f, 1.86f, 38.8f), new Vector3(0f, 2.90f, 48f),
                "Previews/archive_door_inside.png", 62f, -1f, delegate
            {
                OpenHallDoorForPreview();
            });

            // 从院子里看踏跺和门：这几级得接得上，人才走得上去
            RenderPreview(scene, new Vector3(0f, 2.40f, 30.5f), new Vector3(0f, 2.55f, 40.5f),
                "Previews/archive_door_approach.png", 62f);
        }

        static void OpenHallDoorForPreview()
        {
            var door = Object.FindObjectOfType<HallDoor>();
            if (door == null) return;
            if (door.leftHinge != null) door.leftHinge.localRotation = Quaternion.Euler(0f, door.openAngle, 0f);
            if (door.rightHinge != null) door.rightHinge.localRotation = Quaternion.Euler(0f, -door.openAngle, 0f);
            if (door.latch != null) door.latch.SetActive(false);
        }
    }
}
