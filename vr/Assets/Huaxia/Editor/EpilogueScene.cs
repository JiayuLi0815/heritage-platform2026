using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 终章：河山册合璧。
    /// 回到藏卷阁的案前。三片册页归位，最后一叶一直是空白，
    /// 页上写着「此页留给后来者。」，体验者伸手把书合上，身后的门随之合上。
    /// 不做字幕表、不做统计页、不做「感谢体验」。
    /// 房间按真实小阁收窄（5.5 × 8.8 m），密度靠隔扇、卷柜、挂轴、灯架、香几堆出来。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        const float EpiHalfW = 2.75f;
        const float EpiWinZ = 4.40f;
        const float EpiDoorZ = -4.40f;
        const float EpiCeil = 4.30f;
        const float EpiWallT = 0.40f;
        const float EpiWinY = 2.18f;
        const float EpiWinR = 0.98f;
        const float EpiDeskZ = 2.10f;
        const float EpiDeskTop = 0.82f;

        public static void BuildEpilogueScene()
        {
            LoadFonts();
            EnsureFolder(MatDir);
            EnsureFolder(SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEpilogueEnvironment();
            AddSceneAmbient("amb_epilogue", 0.55f);

            var root = new GameObject("HiddenPavilion").transform;

            // ------------------------------------------------------------------ 材质
            var floorMat = MatAs("EpiFloorSlab", new Color(0.47f, 0.45f, 0.41f), null, 0f, 0.16f);
            var groutMat = Mat("EpiFloorGrout", new Color(0.20f, 0.19f, 0.17f), 0f, 0.08f);
            var wallMat = MatAs("EpiPaperWall", new Color(0.86f, 0.83f, 0.75f), null, 0f, 0.04f);
            var lacquer = MatAs("EpiLacquerRed", new Color(0.40f, 0.16f, 0.12f), "plaster", 0f, 0.32f);
            var woodMat = MatAs("EpiWoodFrame", Color.Lerp(HuaxiaPalette.WoodBrown, HuaxiaPalette.Cinnabar, 0.22f), null, 0f, 0.18f);
            var woodDark = MatAs("EpiWoodEbony", HuaxiaPalette.WoodDark, null, 0f, 0.14f);
            var woodPale = MatAs("EpiWoodPear", Color.Lerp(HuaxiaPalette.WoodBrown, HuaxiaPalette.Sand, 0.44f), null, 0f, 0.14f);
            var stoneMat = MatAs("EpiStoneGrey", HuaxiaPalette.Rock, null, 0f, 0.20f);
            var stoneDark = MatAs("EpiStoneDark", Color.Lerp(HuaxiaPalette.Rock, HuaxiaPalette.InkTealDeep, 0.42f), null, 0f, 0.18f);
            var paper = MatAs("EpiPaperSheet", new Color(0.91f, 0.88f, 0.79f), null, 0f, 0.04f);
            var paperOld = MatAs("EpiPaperOld", new Color(0.81f, 0.76f, 0.65f), null, 0f, 0.04f);
            var paperDim = MatAs("EpiPaperDim", new Color(0.64f, 0.60f, 0.51f), null, 0f, 0.04f);
            var silk = MatAs("EpiSilkCover", Color.Lerp(HuaxiaPalette.InkTealDeep, HuaxiaPalette.WoodDark, 0.40f), null, 0f, 0.12f);
            var silkRed = MatAs("EpiSilkRed", Color.Lerp(HuaxiaPalette.Cinnabar, HuaxiaPalette.WoodDark, 0.16f), null, 0f, 0.10f);
            var gold = MatAs("EpiGoldFitting", Color.Lerp(HuaxiaPalette.Amber, HuaxiaPalette.AmberPale, 0.45f), null, 0.10f, 0.34f);
            var bronze = MatAs("EpiBronzeBurner", Color.Lerp(HuaxiaPalette.Amber, HuaxiaPalette.InkTealDeep, 0.58f), null, 0.10f, 0.28f);
            var ceilMat = MatAs("EpiCeilBoard", Color.Lerp(HuaxiaPalette.WoodDark, HuaxiaPalette.Sand, 0.46f), null, 0f, 0.10f);
            var inkStone = MatAs("EpiInkStone", new Color(0.15f, 0.15f, 0.14f), null, 0f, 0.34f);
            var lampMat = EmissiveMat("EpiLampPaper", HuaxiaPalette.PaperWarm, HuaxiaPalette.AmberPale * 0.60f, 0f, 0.2f);
            var flameMat = EmissiveMat("EpiCandleFlame", new Color(1f, 0.82f, 0.48f), new Color(1f, 0.64f, 0.22f) * 1.20f, 0f, 0.5f);
            var skyMat = EmissiveMat("EpiWindowSky", new Color(0.78f, 0.78f, 0.75f),
                new Color(0.62f, 0.62f, 0.60f), 0f, 0.05f);
            var hillFar = EmissiveMat("EpiHillFar", new Color(0.46f, 0.49f, 0.51f),
                new Color(0.43f, 0.46f, 0.48f), 0f, 0.04f);
            var hillMid = EmissiveMat("EpiHillMid", new Color(0.27f, 0.29f, 0.30f),
                new Color(0.26f, 0.28f, 0.29f), 0f, 0.04f);
            var hillNear = EmissiveMat("EpiHillNear", new Color(0.26f, 0.28f, 0.30f),
                new Color(0.25f, 0.27f, 0.29f), 0f, 0.04f);
            var inkBranch = EmissiveMat("EpiInkBranch", new Color(0.08f, 0.08f, 0.08f),
                new Color(0.07f, 0.07f, 0.07f), 0f, 0.05f);

            var jade = MatAs("EpiJadeCeladon", Color.Lerp(new Color(0.66f, 0.74f, 0.66f), HuaxiaPalette.InkTealDeep, 0.24f), null, 0f, 0.30f);
            var inkWell = Mat("EpiInkPool", new Color(0.09f, 0.09f, 0.11f), 0f, 0.46f);

            // 室内木作压低纹理强度：近景大面上原来的花斑会读成「脏」
            Calm("EpiWoodFrame", 0.26f, 0.06f, 0.10f);
            Calm("EpiWoodEbony", 0.22f, 0.05f, 0.08f);
            Calm("EpiWoodPear", 0.26f, 0.06f, 0.10f);
            Calm("EpiCeilBoard", 0.20f, 0.05f, 0.08f);
            Calm("EpiFloorSlab", 0.30f, 0.12f, 0.16f);
            Calm("EpiPaperWall", 0.14f, 0.06f, 0.10f);
            Calm("EpiLacquerRed", 0.14f, 0.04f, 0.06f);
            Calm("EpiHillFar", 0.06f, 0.02f, 0.02f);
            Calm("EpiHillMid", 0.06f, 0.02f, 0.02f);
            Calm("EpiHillNear", 0.06f, 0.02f, 0.02f);
            // 天光与墨枝必须是干净的平色，否则石材纹理会在窗里读出满屏噪点
            Calm("EpiWindowSky", 0f, 0f, 0f);
            Calm("EpiInkBranch", 0f, 0f, 0f);
            Calm("EpiLampPaper", 0.05f, 0.02f, 0.02f);
            Calm("EpiCandleFlame", 0f, 0f, 0f);

            var plaqueInk = new Color(0.92f, 0.84f, 0.64f);
            var coupletInk = new Color(0.96f, 0.93f, 0.86f);
            var ink = new Color(0.13f, 0.12f, 0.10f);
            var inkFaint = new Color(0.27f, 0.25f, 0.21f);

            // ------------------------------------------------------------------ 地面
            Box("FloorGrout", root, new Vector3(0f, -0.17f, 0f),
                new Vector3(EpiHalfW * 2f + 0.9f, 0.32f, EpiWinZ - EpiDoorZ + 0.9f), groutMat);
            int nx = 7, nz = 11;
            float sw = (EpiHalfW * 2f - 0.08f) / nx;
            float sd = (EpiWinZ - EpiDoorZ - 0.08f) / nz;
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float px = -EpiHalfW + 0.04f + sw * (ix + 0.5f);
                    float pz = EpiDoorZ + 0.04f + sd * (iz + 0.5f);
                    Box("FloorSlab_" + iz + "_" + ix, root,
                        new Vector3(px, -0.02f + Hash01(ix * 37 + iz * 19) * 0.008f, pz),
                        new Vector3(sw - 0.020f, 0.09f, sd - 0.020f), floorMat, false);
                }
            Box("DoorStone", root, new Vector3(0f, 0.012f, EpiDoorZ + 0.60f),
                new Vector3(1.86f, 0.05f, 0.82f), stoneDark, false);
            Box("FloorMat", root, new Vector3(0f, 0.030f, EpiDeskZ - 0.90f),
                new Vector3(1.60f, 0.02f, 0.92f), silkRed, false);
            Box("FloorMatTrim", root, new Vector3(0f, 0.036f, EpiDeskZ - 0.90f),
                new Vector3(1.52f, 0.016f, 0.84f), woodDark, false);

            // ------------------------------------------------------------------ 侧墙 + 隔扇
            float wallCx = EpiHalfW + EpiWallT * 0.5f;
            for (int s = -1; s <= 1; s += 2)
            {
                Box("WallSide_" + s, root, new Vector3(s * wallCx, EpiCeil * 0.5f, 0f),
                    new Vector3(EpiWallT, EpiCeil, EpiWinZ - EpiDoorZ), wallMat);
                Box("Dado_" + s, root, new Vector3(s * (EpiHalfW - 0.02f), 0.19f, 0f),
                    new Vector3(0.06f, 0.38f, EpiWinZ - EpiDoorZ), woodDark, false);
                Box("DadoCap_" + s, root, new Vector3(s * (EpiHalfW - 0.04f), 0.395f, 0f),
                    new Vector3(0.10f, 0.03f, EpiWinZ - EpiDoorZ), woodMat, false);

                for (int p = 0; p < 3; p++)
                {
                    float pz = -2.85f + p * 2.85f;
                    var pan = Empty("Lattice_" + s + "_" + p, root, new Vector3(s * (EpiHalfW - 0.06f), 0.06f, pz));
                    pan.transform.localRotation = Quaternion.Euler(0f, s < 0 ? 90f : -90f, 0f);
                    BuildLatticePanel(pan.transform, 1.30f, 2.62f, woodMat, woodDark, paperOld);
                }
                Box("Transom_" + s, root, new Vector3(s * (EpiHalfW - 0.05f), 2.86f, 0f),
                    new Vector3(0.12f, 0.16f, EpiWinZ - EpiDoorZ), woodDark, false);
            }

            // ------------------------------------------------------------------ 窗墙：月洞窗
            float wallCz = EpiWinZ + EpiWallT * 0.5f;
            float outHalf = EpiHalfW + EpiWallT;

            // 整面墙做成「带圆孔的板」，圆孔是真正切出来的，不是拿条块拼的
            var winWall = MeshFactory.PlateWithHole(outHalf, EpiWinY, EpiWinR, 64);
            MeshObject("WallWinPlate", root, new Vector3(0f, EpiWinY, wallCz), winWall, wallMat, true);

            // 窗套：一圈整石环板，内缘压住洞口，外缘一圈浅线脚
            MeshObject("WinRing", root, new Vector3(0f, EpiWinY, EpiWinZ + 0.04f),
                MeshFactory.PlateRing(EpiWinR - 0.02f, EpiWinR + 0.20f, 96), stoneMat, false);
            MeshObject("WinRingLip", root, new Vector3(0f, EpiWinY, EpiWinZ + 0.10f),
                MeshFactory.PlateRing(EpiWinR + 0.02f, EpiWinR + 0.09f, 96), stoneDark, false);
            Box("WinSill", root, new Vector3(0f, EpiWinY - EpiWinR - 0.15f, EpiWinZ - 0.10f),
                new Vector3(1.90f, 0.16f, 0.34f), stoneMat, false);
            Box("WinSillLip", root, new Vector3(0f, EpiWinY - EpiWinR - 0.24f, EpiWinZ - 0.12f),
                new Vector3(1.98f, 0.05f, 0.40f), stoneDark, false);

            // 窗外：一层天光 + 三重山影 + 一支枯枝。全部自发光，不被室内光线冲白，也不塌成黑洞
            Box("WinSky", root, new Vector3(0f, EpiWinY + 0.95f, EpiWinZ + 2.60f),
                new Vector3(7.20f, 5.20f, 0.06f), skyMat, false);

            var ridgeFar = MeshFactory.MountainRidge(6.60f, 1.42f, 5, 17, 0.34f);
            var ridgeMid = MeshFactory.MountainRidge(4.80f, 0.82f, 6, 53, 0.20f);
            var ridgeNear = MeshFactory.MountainRidge(3.60f, 0.40f, 4, 91, -0.16f);
            for (int i = -1; i <= 1; i += 2)
            {
                MeshObject("HillFar_" + i, root, new Vector3(0f, 1.34f, EpiWinZ + 1.90f),
                    ridgeFar, hillFar, false).transform.localRotation = Quaternion.Euler(0f, i < 0 ? 0f : 180f, 0f);
                MeshObject("HillMid_" + i, root, new Vector3(-0.18f, 1.26f, EpiWinZ + 1.52f),
                    ridgeMid, hillMid, false).transform.localRotation = Quaternion.Euler(0f, i < 0 ? 0f : 180f, 0f);
                MeshObject("HillNear_" + i, root, new Vector3(0.22f, 1.16f, EpiWinZ + 1.12f),
                    ridgeNear, hillNear, false).transform.localRotation = Quaternion.Euler(0f, i < 0 ? 0f : 180f, 0f);
            }
            // 山脚一层与天同色的薄雾，把三重山拉开距离
            Box("WinMist", root, new Vector3(-0.10f, 1.30f, EpiWinZ + 1.34f),
                new Vector3(3.10f, 0.07f, 0.02f), skyMat, false);

            // 一支枯枝自右上斜进窗内，破掉圆洞的规整
            var brRoot = Empty("WinBranchRoot", root, new Vector3(0.74f, EpiWinY + 0.86f, EpiWinZ + 0.92f));
            brRoot.transform.localRotation = Quaternion.Euler(0f, 0f, 34f);
            Box("WinBranchA", brRoot.transform, new Vector3(0f, 0f, 0f), new Vector3(0.022f, 1.34f, 0.022f), inkBranch, false);
            Box("WinBranchB", brRoot.transform, new Vector3(0.16f, 0.28f, 0.01f), new Vector3(0.014f, 0.46f, 0.014f), inkBranch, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Box("WinBranchC", brRoot.transform, new Vector3(-0.14f, -0.14f, 0.01f), new Vector3(0.013f, 0.40f, 0.013f), inkBranch, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
            Box("WinBranchD", brRoot.transform, new Vector3(0.24f, -0.02f, 0.01f), new Vector3(0.010f, 0.26f, 0.010f), inkBranch, false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -34f);
            // 几朵梅：与枝同墨，只作为末梢的点
            var blossom = EmissiveMat("EpiBlossom", new Color(0.20f, 0.19f, 0.18f),
                new Color(0.17f, 0.16f, 0.15f), 0f, 0.10f);
            Calm("EpiBlossom", 0f, 0f, 0f);
            float[] blx = { 0.30f, 0.36f, 0.20f, -0.24f, -0.28f, -0.10f, 0.33f };
            float[] bly = { 0.52f, 0.44f, 0.34f, -0.30f, -0.22f, -0.20f, -0.07f };
            for (int i = 0; i < 7; i++)
                Ball("Blossom_" + i, brRoot.transform,
                    new Vector3(blx[i], bly[i], 0.02f), 0.011f + Hash01(i * 41) * 0.004f, blossom);

            // 匾额：藏卷閣（窗上）
            BuildPlaque(root, new Vector3(0f, 3.47f, EpiWinZ - 0.03f), 0.96f, 0.30f,
                "藏卷閣", 150, 180f, woodDark, gold, plaqueInk);

            // ------------------------------------------------------------------ 门墙
            float doorW = 1.44f, doorH = 2.30f;
            float doorCz = EpiDoorZ - EpiWallT * 0.5f;
            float sideW = outHalf - doorW * 0.5f;
            Box("WallDoorL", root, new Vector3(-(doorW * 0.5f + sideW * 0.5f), EpiCeil * 0.5f, doorCz),
                new Vector3(sideW, EpiCeil, EpiWallT), wallMat);
            Box("WallDoorR", root, new Vector3(doorW * 0.5f + sideW * 0.5f, EpiCeil * 0.5f, doorCz),
                new Vector3(sideW, EpiCeil, EpiWallT), wallMat);
            Box("WallDoorTop", root, new Vector3(0f, (EpiCeil + doorH) * 0.5f, doorCz),
                new Vector3(doorW, EpiCeil - doorH, EpiWallT), wallMat);

            // 门框：抱框 + 上槛 + 门槛石
            for (int i = -1; i <= 1; i += 2)
            {
                Box("DoorJamb_" + i, root, new Vector3(i * (doorW * 0.5f + 0.07f), doorH * 0.5f, EpiDoorZ + 0.09f),
                    new Vector3(0.14f, doorH + 0.10f, 0.26f), woodMat, false);
                Box("DoorJambBase_" + i, root, new Vector3(i * (doorW * 0.5f + 0.07f), 0.12f, EpiDoorZ + 0.09f),
                    new Vector3(0.22f, 0.24f, 0.34f), stoneMat, false);
            }
            Box("DoorLintel", root, new Vector3(0f, doorH + 0.10f, EpiDoorZ + 0.09f),
                new Vector3(doorW + 0.56f, 0.20f, 0.26f), woodDark, false);
            Box("DoorThreshold", root, new Vector3(0f, 0.06f, EpiDoorZ + 0.02f),
                new Vector3(doorW + 0.14f, 0.12f, 0.26f), stoneDark, false);

            // 双扇门，只留一线门缝（终章合门时再完全关闭）
            for (int i = -1; i <= 1; i += 2)
            {
                var hinge = Empty("DoorHinge_" + i, root, new Vector3(i * doorW * 0.5f, 0f, EpiDoorZ + 0.04f));
                hinge.transform.localRotation = Quaternion.Euler(0f, -i * 9f, 0f);
                var leaf = Empty("DoorLeaf_" + i, hinge.transform, new Vector3(-i * doorW * 0.25f, 0f, 0f));
                Box("Board", leaf.transform, new Vector3(0f, doorH * 0.5f + 0.06f, 0f),
                    new Vector3(doorW * 0.5f, doorH - 0.04f, 0.075f), woodMat, false);
                for (int b = -1; b <= 1; b += 2)
                    Box("Stile_" + b, leaf.transform, new Vector3(b * (doorW * 0.25f - 0.05f), doorH * 0.5f + 0.06f, 0.045f),
                        new Vector3(0.09f, doorH - 0.04f, 0.055f), woodDark, false);
                for (int r = 0; r < 4; r++)
                    Box("Rail_" + r, leaf.transform, new Vector3(0f, 0.30f + r * 0.60f, 0.045f),
                        new Vector3(doorW * 0.5f - 0.06f, 0.07f, 0.05f), woodDark, false);
                for (int r = 0; r < 4; r++)
                    for (int c = -1; c <= 1; c++)
                        Ball("Stud_" + r + "_" + c, leaf.transform,
                            new Vector3(c * 0.16f, 0.46f + r * 0.52f, -0.045f), 0.024f, gold);
                Ball("Pull_" + i, leaf.transform, new Vector3(-i * 0.13f, 1.14f, -0.06f), 0.055f, gold);
            }

            // 门额匾与门联
            BuildPlaque(root, new Vector3(0f, 2.86f, EpiDoorZ + 0.10f), 1.06f, 0.38f,
                "藏卷閣", 132, 0f, woodDark, gold, plaqueInk);
            BuildPlaque(root, new Vector3(-0.94f, 1.58f, EpiDoorZ + 0.10f), 0.26f, 1.66f,
                "一\n卷\n山\n河\n收\n眼\n底", 288, 0f, lacquer, gold, coupletInk);
            BuildPlaque(root, new Vector3(0.94f, 1.58f, EpiDoorZ + 0.10f), 0.26f, 1.66f,
                "千\n年\n風\n雨\n到\n窗\n前", 288, 0f, lacquer, gold, coupletInk);

            // 门墙墙裙与两幅挂屏，免得大面积空墙
            Box("DadoDoor", root, new Vector3(0f, 0.21f, EpiDoorZ + 0.05f),
                new Vector3(EpiHalfW * 2f - 0.06f, 0.42f, 0.10f), woodDark, false);
            Box("DadoDoorCap", root, new Vector3(0f, 0.44f, EpiDoorZ + 0.06f),
                new Vector3(EpiHalfW * 2f - 0.04f, 0.035f, 0.14f), woodMat, false);
            for (int i = -1; i <= 1; i += 2)
            {
                var gp = Empty("HangingPanel_" + i, root, new Vector3(i * 1.94f, 1.78f, EpiDoorZ + 0.06f));
                Box("Frame", gp.transform, Vector3.zero, new Vector3(0.54f, 0.94f, 0.05f), woodDark, false);
                Box("Face", gp.transform, new Vector3(0f, 0f, 0.03f), new Vector3(0.44f, 0.84f, 0.012f), paper, false);
                for (int k = 0; k < 3; k++)
                {
                    // 三叠远山：越近越宽越厚，压在下半幅，才读成一幅小水墨
                    float dw = 0.19f + k * 0.085f;
                    float dh = 0.055f + k * 0.032f;
                    Box("Hill_" + k, gp.transform, new Vector3(-0.02f + k * 0.005f, 0.05f - k * 0.095f, 0.042f),
                        new Vector3(dw, dh, 0.004f), paper, false)
                        .GetComponent<Renderer>().sharedMaterial =
                        MatAs("EpiPanelInk" + k, Color.Lerp(ink, paper.color, 0.56f - k * 0.16f), null, 0f, 0.04f);
                }
                Box("PanelSeal", gp.transform, new Vector3(0.13f, -0.30f, 0.042f),
                    new Vector3(0.036f, 0.036f, 0.004f), silkRed, false);
            }

            // 隔扇之间的空档塞上书格，侧墙不留大片白
            for (int s = -1; s <= 1; s += 2)
            {
                var bk = Empty("WallShelf_" + s, root, new Vector3(s * (EpiHalfW - 0.19f), 0f, -1.42f));
                bk.transform.localRotation = Quaternion.Euler(0f, s < 0 ? 90f : -90f, 0f);
                BuildWallShelf(bk.transform, 1.24f, 1.94f, 0.34f,
                    woodDark, woodMat, paper, paperOld, silkRed, silk, gold);
            }

            // ------------------------------------------------------------------ 柱、梁、天花
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    string tag = (sx < 0 ? "L" : "R") + (sz < 0 ? "D" : "W");
                    var col = Cyl("Column_" + tag, root, new Vector3(sx * 2.56f, EpiCeil * 0.5f, sz * 3.62f),
                        0.155f, EpiCeil, lacquer, true);
                    Cyl("ColumnBase_" + tag, root, new Vector3(sx * 2.56f, 0.09f, sz * 3.62f),
                        0.21f, 0.18f, stoneMat);
                    Box("Capital_" + tag, root, new Vector3(sx * 2.56f, 3.90f, sz * 3.62f),
                        new Vector3(0.46f, 0.16f, 0.46f), woodDark, false);
                    Box("Bracket_" + tag, root, new Vector3(sx * 2.56f, 3.99f, sz * 3.62f),
                        new Vector3(0.66f, 0.10f, 0.30f), woodMat, false);
                }

            Box("Ceiling", root, new Vector3(0f, EpiCeil + 0.05f, 0f),
                new Vector3(EpiHalfW * 2f + 0.8f, 0.10f, EpiWinZ - EpiDoorZ + 0.8f), ceilMat, false);
            for (int i = 0; i < 4; i++)
            {
                float bz = -3.30f + i * 2.20f;
                Box("BeamX_" + i, root, new Vector3(0f, EpiCeil - 0.16f, bz),
                    new Vector3(EpiHalfW * 2f + 0.2f, 0.22f, 0.18f), woodMat, false);
            }
            for (int i = -1; i <= 1; i += 2)
                Box("BeamZ_" + i, root, new Vector3(i * 1.30f, EpiCeil - 0.11f, 0f),
                    new Vector3(0.15f, 0.14f, EpiWinZ - EpiDoorZ), woodPale, false);
            for (int i = 0; i < 8; i++)
                Box("CeilSlat_" + i, root, new Vector3(0f, EpiCeil - 0.02f, -3.95f + i * 1.13f),
                    new Vector3(EpiHalfW * 2f + 0.2f, 0.05f, 0.05f), woodDark, false);

            // 梁下两盏灯笼，给天花一点暖
            for (int i = -1; i <= 1; i += 2)
            {
                var lp = Empty("HangLantern_" + i, root, new Vector3(i * 1.30f, 3.44f, -0.62f));
                Cyl("Cord", lp.transform, new Vector3(0f, 0.56f, 0f), 0.011f, 0.30f, woodDark);
                Box("Cap", lp.transform, new Vector3(0f, 0.30f, 0f), new Vector3(0.27f, 0.05f, 0.27f), woodDark, false);
                Box("Body", lp.transform, Vector3.zero, new Vector3(0.31f, 0.44f, 0.31f), lampMat, false);
                Box("Foot", lp.transform, new Vector3(0f, -0.25f, 0f), new Vector3(0.27f, 0.05f, 0.27f), woodDark, false);
                Box("Tassel", lp.transform, new Vector3(0f, -0.37f, 0f), new Vector3(0.05f, 0.20f, 0.05f), silkRed, false);
                AddPoint(lp.transform, "Glow_" + i, Vector3.zero, new Color(1f, 0.84f, 0.58f), 2.4f, 0.42f);
            }

            // ------------------------------------------------------------------ 卷柜（两侧靠窗）
            for (int s = -1; s <= 1; s += 2)
            {
                var cab = Empty("ScrollCabinet_" + s, root, new Vector3(s * 1.90f, 0f, EpiWinZ - 0.44f));
                cab.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                BuildScrollCabinet(cab.transform, 1.06f, 2.14f, 0.52f,
                    woodDark, woodMat, paper, paperOld, silkRed, silk, gold);
            }

            // ------------------------------------------------------------------ 香几 + 铜炉（门侧）
            {
                var tbl = Empty("CenserTable", root, new Vector3(-1.92f, 0f, EpiDoorZ + 1.05f));
                Box("Top", tbl.transform, new Vector3(0f, 0.66f, 0f), new Vector3(0.62f, 0.07f, 0.62f), woodMat, false);
                Box("TopLip", tbl.transform, new Vector3(0f, 0.695f, 0f), new Vector3(0.66f, 0.022f, 0.66f), woodDark, false);
                // 束腰：牙板内收一圈，再下接四腿，才像香几不像现代边桌
                Box("Waist", tbl.transform, new Vector3(0f, 0.585f, 0f), new Vector3(0.44f, 0.07f, 0.44f), woodDark, false);
                Box("Apron", tbl.transform, new Vector3(0f, 0.535f, 0f), new Vector3(0.52f, 0.06f, 0.52f), woodMat, false);
                for (int i = 0; i < 4; i++)
                {
                    float lx = (i % 2 == 0 ? -1f : 1f) * 0.225f;
                    float lz = (i / 2 == 0 ? -1f : 1f) * 0.225f;
                    var leg = Cyl("Leg_" + i, tbl.transform, new Vector3(lx, 0.29f, lz), 0.023f, 0.54f, woodDark);
                    // 微微外撇，接近三弯腿的走势
                    leg.transform.localRotation = Quaternion.Euler(lz > 0f ? -3.2f : 3.2f, 0f, lx > 0f ? -3.2f : 3.2f);
                    Cyl("LegFoot_" + i, tbl.transform, new Vector3(lx * 1.06f, 0.02f, lz * 1.06f), 0.030f, 0.04f, woodDark);
                }
                Box("Shelf", tbl.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.44f, 0.026f, 0.44f), woodDark, false);

                var burner = Empty("Burner", tbl.transform, new Vector3(0f, 0.70f, 0f));
                Cyl("Body", burner.transform, new Vector3(0f, 0.058f, 0f), 0.100f, 0.10f, bronze);
                Cyl("Mouth", burner.transform, new Vector3(0f, 0.108f, 0f), 0.108f, 0.024f, bronze);
                Cyl("Lid", burner.transform, new Vector3(0f, 0.126f, 0f), 0.086f, 0.020f, bronze);
                Cyl("LidTop", burner.transform, new Vector3(0f, 0.140f, 0f), 0.052f, 0.014f, bronze);
                Ball("Knob", burner.transform, new Vector3(0f, 0.156f, 0f), 0.019f, gold);
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.PI * 2f / 3f + 0.4f;
                    Cyl("Foot_" + i, burner.transform,
                        new Vector3(Mathf.Cos(a) * 0.072f, 0.008f, Mathf.Sin(a) * 0.072f), 0.010f, 0.030f, bronze);
                }
                for (int i = -1; i <= 1; i += 2)
                    Box("Handle_" + i, burner.transform, new Vector3(i * 0.104f, 0.152f, 0f),
                        new Vector3(0.014f, 0.070f, 0.014f), bronze, false);
            }

            // ------------------------------------------------------------------ 侧墙挂轴（靠窗那侧的隔扇空当）
            for (int s = -1; s <= 1; s += 2)
                BuildHangingScroll(root,
                    new Vector3(s * (EpiHalfW - 0.02f), 2.10f, 1.42f),
                    s < 0 ? -90f : 90f, paper, woodDark, gold, ink, inkFaint);

            // ------------------------------------------------------------------ 灯架（案前两侧）
            for (int s = -1; s <= 1; s += 2)
            {
                var stand = Empty("LanternStand_" + s, root, new Vector3(s * 2.34f, 0f, 1.52f));
                Box("Base", stand.transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.34f, 0.07f, 0.34f), woodDark, false);
                Box("BaseTop", stand.transform, new Vector3(0f, 0.085f, 0f), new Vector3(0.26f, 0.04f, 0.26f), woodMat, false);
                Cyl("Pole", stand.transform, new Vector3(0f, 0.90f, 0f), 0.030f, 1.62f, woodDark);
                Box("ArmTop", stand.transform, new Vector3(0f, 1.72f, 0f), new Vector3(0.24f, 0.05f, 0.24f), woodMat, false);
                Cyl("LampTop", stand.transform, new Vector3(0f, 1.90f, 0f), 0.075f, 0.05f, woodDark);
                Cyl("LampBody", stand.transform, new Vector3(0f, 1.76f, 0f), 0.135f, 0.27f, lampMat);
                for (int r = 0; r < 6; r++)
                {
                    float a = r * 60f * Mathf.Deg2Rad;
                    var rib = Box("Rib_" + r, stand.transform,
                        new Vector3(Mathf.Cos(a) * 0.128f, 1.76f, Mathf.Sin(a) * 0.128f),
                        new Vector3(0.014f, 0.28f, 0.014f), woodDark, false);
                    rib.transform.localRotation = Quaternion.Euler(0f, -r * 60f, 0f);
                }
                AddPoint(stand.transform, "LanternLight", new Vector3(0f, 1.76f, 0f),
                    new Color(1f, 0.86f, 0.62f), 2.60f, 0.62f);
            }

            // ------------------------------------------------------------------ 案
            var desk = Empty("Desk", root, new Vector3(0f, 0f, EpiDeskZ));
            Box("Top", desk.transform, new Vector3(0f, EpiDeskTop - 0.05f, 0f),
                new Vector3(2.24f, 0.10f, 0.74f), woodDark, true);
            for (int i = -1; i <= 1; i += 2)
                Box("Wing_" + i, desk.transform, new Vector3(i * 1.14f, EpiDeskTop + 0.01f, 0f),
                    new Vector3(0.10f, 0.17f, 0.74f), woodMat, false);
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    Box("Leg_" + i + "_" + j, desk.transform,
                        new Vector3(i * 0.98f, (EpiDeskTop - 0.10f) * 0.5f, j * 0.28f),
                        new Vector3(0.09f, EpiDeskTop - 0.10f, 0.09f), woodMat, false);
            for (int j = -1; j <= 1; j += 2)
            {
                Box("Apron_" + j, desk.transform, new Vector3(0f, 0.66f, j * 0.33f),
                    new Vector3(2.10f, 0.15f, 0.05f), woodMat, false);
                Box("Stretcher_" + j, desk.transform, new Vector3(0f, 0.20f, j * 0.26f),
                    new Vector3(1.94f, 0.05f, 0.05f), woodDark, false);
            }
            for (int i = -1; i <= 1; i += 2)
                Box("ApronSide_" + i, desk.transform, new Vector3(i * 0.98f, 0.66f, 0f),
                    new Vector3(0.05f, 0.15f, 0.60f), woodMat, false);
            for (int i = -1; i <= 1; i += 2)
            {
                Box("Drawer_" + i, desk.transform, new Vector3(i * 0.42f, 0.70f, -0.36f),
                    new Vector3(0.60f, 0.13f, 0.03f), woodPale, false);
                Ball("DrawerPull_" + i, desk.transform, new Vector3(i * 0.42f, 0.70f, -0.385f), 0.028f, gold);
            }

            // ------------------------------------------------------------------ 河山册
            // 用预制体实例：你在一路翻过来的那本书，最后就摊在这张案上。
            // 前三叶已经翻到左边，右页停在末页——那句「此頁留給後來者」是场上唯一没写完的字。
            PlaceRealBook(desk.transform);

            // 三片归位的册页：水 / 土 / 海——从书中升起，浮在案面上方，不压住书本身
            string[] marks = { "水", "土", "海" };
            float[] flx = { -0.31f, 0f, 0.31f };
            float[] fly = { 0.135f, 0.180f, 0.135f };
            float[] flyaw = { -14f, 0f, 14f };
            float[] fltilt = { -54f, -46f, -54f };
            float[] flroll = { 7f, -5f, 8f };
            for (int i = 0; i < 3; i++)
            {
                var pg = Empty("FlyingLeaf_" + i, desk.transform, new Vector3(flx[i], EpiDeskTop + fly[i], 0.11f));
                pg.transform.localRotation = Quaternion.Euler(fltilt[i], flyaw[i], flroll[i]);
                Box("Sheet", pg.transform, Vector3.zero, new Vector3(0.148f, 0.208f, 0.004f), paper, false);
                Box("SheetEdge", pg.transform, new Vector3(0f, -0.102f, 0f),
                    new Vector3(0.148f, 0.006f, 0.005f), paperDim, false);
                CreateTextPlate(pg.transform, marks[i], new Vector3(0f, 0.009f, -0.005f),
                    new Vector3(0f, 180f, 0f), new Vector2(0.106f, 0.106f), 300, ink);
            }

            // ------------------------------------------------------------------ 案上物
            Box("Inkstone", desk.transform, new Vector3(-0.60f, EpiDeskTop + 0.016f, 0.24f),
                new Vector3(0.19f, 0.032f, 0.13f), inkStone, false);
            Box("InkWell", desk.transform, new Vector3(-0.60f, EpiDeskTop + 0.032f, 0.24f),
                new Vector3(0.13f, 0.004f, 0.075f), inkWell, false);
            Box("InkStick", desk.transform, new Vector3(-0.68f, EpiDeskTop + 0.040f, 0.245f),
                new Vector3(0.085f, 0.012f, 0.026f), inkStone, false);

            for (int i = 0; i < 3; i++)
            {
                var brush = Empty("Brush_" + i, desk.transform,
                    new Vector3(-0.60f + (i - 1) * 0.052f, EpiDeskTop + 0.062f, -0.14f));
                brush.transform.localRotation = Quaternion.Euler(0f, 0f, 22f + i * 4f);
                Cyl("Shaft", brush.transform, Vector3.zero, 0.0085f, 0.26f, woodMat);
                Cyl("Tip", brush.transform, new Vector3(0f, -0.155f, 0f), 0.0085f, 0.05f, inkStone);
            }
            Box("BrushRest", desk.transform, new Vector3(-0.60f, EpiDeskTop + 0.028f, -0.14f),
                new Vector3(0.22f, 0.026f, 0.05f), jade, false);

            for (int i = -1; i <= 1; i += 2)
                Box("Weight_" + i, desk.transform, new Vector3(i * 0.36f, EpiDeskTop + 0.030f, 0.30f),
                    new Vector3(0.14f, 0.022f, 0.038f), jade, false);
            Box("SealBox", desk.transform, new Vector3(0.56f, EpiDeskTop + 0.030f, 0.24f),
                new Vector3(0.085f, 0.055f, 0.085f), silkRed, false);
            Ball("SealKnob", desk.transform, new Vector3(0.56f, EpiDeskTop + 0.068f, 0.24f), 0.022f, gold);

            // 烛台
            for (int i = -1; i <= 1; i += 2)
            {
                var cd = Empty("Candle_" + i, desk.transform, new Vector3(i * 0.88f, 0f, -0.02f));
                Cyl("Dish", cd.transform, new Vector3(0f, EpiDeskTop + 0.018f, 0f), 0.062f, 0.030f, bronze);
                Cyl("Stem", cd.transform, new Vector3(0f, EpiDeskTop + 0.070f, 0f), 0.016f, 0.100f, bronze);
                Cyl("Cup", cd.transform, new Vector3(0f, EpiDeskTop + 0.128f, 0f), 0.030f, 0.034f, bronze);
                Cyl("Wax", cd.transform, new Vector3(0f, EpiDeskTop + 0.205f, 0f), 0.021f, 0.135f, paper);
                Ball("Flame", cd.transform, new Vector3(0f, EpiDeskTop + 0.292f, 0f), 0.021f, flameMat);
                AddPoint(cd.transform, "CandleLight", new Vector3(0f, EpiDeskTop + 0.30f, 0f),
                    new Color(1f, 0.80f, 0.55f), 1.70f, 0.34f);
            }

            // 案前拜垫
            Box("Cushion", root, new Vector3(0f, 0.055f, EpiDeskZ - 0.98f),
                new Vector3(0.92f, 0.11f, 0.62f), silkRed, false);
            Box("CushionTop", root, new Vector3(0f, 0.112f, EpiDeskZ - 0.98f),
                new Vector3(0.84f, 0.020f, 0.54f), silk, false);

            // ------------------------------------------------------------------ 光与尘
            AddPoint(root, "WindowFill", new Vector3(0f, 2.30f, EpiWinZ - 1.30f),
                new Color(1f, 0.94f, 0.82f), 6.20f, 2.30f);
            AddPoint(root, "DeskFill", new Vector3(0f, 1.55f, EpiDeskZ - 0.95f),
                new Color(1f, 0.90f, 0.74f), 3.20f, 0.95f);
            AddPoint(root, "DoorFill", new Vector3(0f, 2.35f, EpiDoorZ + 0.90f),
                new Color(0.86f, 0.90f, 0.94f), 3.00f, 0.34f);
            for (int i = -1; i <= 1; i += 2)
            {
                AddPoint(root, "CeilFill_" + i, new Vector3(i * 1.30f, 3.86f, 1.60f),
                    new Color(1f, 0.92f, 0.80f), 6.00f, 0.55f);
                AddPoint(root, "CeilFillBack_" + i, new Vector3(i * 1.30f, 3.86f, -2.20f),
                    new Color(1f, 0.92f, 0.80f), 5.50f, 0.42f);
            }
            CreateDust(root, new Vector3(0f, 2.05f, 1.90f), new Vector3(3.2f, 2.4f, 4.2f), 300, 11);

            // ------------------------------------------------------------------ 玩家与 UI
            // 这一章以前只有布景，没有相机也没有 HUD：从序章走进来是一片黑。
            // 现在有一个人站在这儿，能走到案前，把书合上，然后回山谷。
            var player = CreatePlayer(new Vector3(0f, 0.1f, -0.62f), 0f);
            ApplyGrade(player, "epilogue");
            var startSpawn = Empty("Spawn_epilogue_start", root, new Vector3(0f, 0.1f, -0.62f));
            startSpawn.AddComponent<SpawnPoint>().id = "epilogue_start";

            var ui = CreateHUD("WASD 走 · 鼠标看 · 鼠标左键 / Esc 释放 · E 合上");
            ui.SetChapterTitle("河山册", "终章");
            ui.SetObjectives(
                "<color=#8C8579>此刻</color>\n" +
                "◇ 走到案前，看看摊开的那本书\n" +
                "◇ 最后一叶，等你合上\n\n" +
                "<color=#8C8579>按 E 合书</color>");
            ui.ShowToast("三处都去过了。案上摊着那本册子，最后一叶还是空的。");

            // 案上的书：按 E 合上 -> 淡黑 -> 一句话 -> 回山谷
            var ending = Empty("BookEnding", root, new Vector3(0f, EpiDeskTop + 0.10f, EpiDeskZ));
            var endingBox = ending.AddComponent<BoxCollider>();
            endingBox.size = new Vector3(1.30f, 0.34f, 1.00f);
            endingBox.isTrigger = true;
            var endingIt = ending.AddComponent<Interactable>();
            endingIt.kind = InteractKind.Info;
            endingIt.hideTier = true;
            endingIt.prompt = "[E] 合上";
            endingIt.title = "合上册子";
            endingIt.subtitle = "河山册 · 末页";
            endingIt.body =
                "三处都去过了：水、土、海，一页一页摊在案上。\n" +
                "看过多少，书页上就留下多少；没看完的，仍旧是湿的。\n\n" +
                "最后一叶还是空的。\n" +
                "空着吧——这一页本来就不该由我写。\n\n" +
                "（按 E，把书合上。）";
            endingIt.endScene = "Archive";
            endingIt.endSpawnId = "after_epilogue";
            endingIt.endLine = "此页留给后来者。";

            EditorSceneManager.SaveScene(scene, SceneDir + "/Epilogue.unity");
        }

        // ====================================================================== 构件
        static void BuildLatticePanel(Transform pan, float w, float h, Material frame, Material dark, Material paper)
        {
            float hw = w * 0.5f, t = 0.075f, d = 0.085f;
            for (int i = -1; i <= 1; i += 2)
                Box("Stile_" + i, pan, new Vector3(i * (hw - t * 0.5f), h * 0.5f, 0f),
                    new Vector3(t, h, d), frame, false);
            Box("RailBot", pan, new Vector3(0f, 0.10f, 0f), new Vector3(w, 0.14f, d), frame, false);
            Box("RailMid", pan, new Vector3(0f, 0.905f, 0f), new Vector3(w - t, 0.075f, d), dark, false);
            Box("RailTop", pan, new Vector3(0f, h - 0.11f, 0f), new Vector3(w, 0.14f, d), frame, false);
            Box("Skirt", pan, new Vector3(0f, 0.49f, -0.01f), new Vector3(w - t * 2f, 0.66f, 0.05f), dark, false);
            Box("SkirtBoss", pan, new Vector3(0f, 0.49f, 0.02f), new Vector3(0.20f, 0.20f, 0.02f), frame, false);
            Box("Waist", pan, new Vector3(0f, 0.98f, -0.01f), new Vector3(w - t * 2f, 0.10f, 0.05f), frame, false);

            float y0 = 1.055f, y1 = h - 0.19f;
            float ih = y1 - y0, iw = w - t * 2f;
            for (int v = 0; v < 4; v++)
                Box("VBar_" + v, pan, new Vector3(-iw * 0.5f + iw * (v + 1f) / 5f, y0 + ih * 0.5f, 0f),
                    new Vector3(0.030f, ih, d * 0.7f), frame, false);
            for (int q = 0; q < 6; q++)
                Box("HBar_" + q, pan, new Vector3(0f, y0 + ih * (q + 1f) / 7f, 0f),
                    new Vector3(iw, 0.026f, d * 0.7f), frame, false);
            Box("PaperSheet", pan, new Vector3(0f, y0 + ih * 0.5f, -0.045f),
                new Vector3(iw, ih, 0.008f), paper, false);
        }

        static void BuildPlaque(Transform parent, Vector3 pos, float w, float h, string text,
            int fontSize, float yaw, Material board, Material trim, Color textColor)
        {
            var g = Empty("Plaque", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box("Board", g.transform, Vector3.zero, new Vector3(w, h, 0.07f), board, false);
            float fr = Mathf.Min(w, h) * 0.075f;
            for (int i = -1; i <= 1; i += 2)
            {
                Box("TrimH_" + i, g.transform, new Vector3(0f, i * (h * 0.5f - fr * 0.5f), 0f),
                    new Vector3(w, fr, 0.085f), trim, false);
                Box("TrimV_" + i, g.transform, new Vector3(i * (w * 0.5f - fr * 0.5f), 0f, 0f),
                    new Vector3(fr, h, 0.085f), trim, false);
            }
            CreateTextPlate(g.transform, text, new Vector3(0f, 0f, 0.045f),
                Vector3.zero, new Vector2(w * 0.78f, h * 0.66f), fontSize, textColor, title: true);
        }

        static void BuildScrollCabinet(Transform cab, float w, float h, float d,
            Material frame, Material front, Material paper, Material paperOld, Material silkRed, Material silk, Material gold)
        {
            float hw = w * 0.5f, hd = d * 0.5f, t = 0.062f;
            for (int i = -1; i <= 1; i += 2)
                Box("Side_" + i, cab, new Vector3(i * (hw - t * 0.5f), h * 0.5f, 0f),
                    new Vector3(t, h, d), frame, false);
            Box("Top", cab, new Vector3(0f, h - 0.045f, 0f), new Vector3(w, 0.09f, d), front, false);
            Box("Bottom", cab, new Vector3(0f, 0.055f, 0f), new Vector3(w, 0.11f, d), frame, false);
            Box("Crown", cab, new Vector3(0f, h + 0.035f, 0f), new Vector3(w + 0.12f, 0.07f, d + 0.08f), front, false);
            Box("Back", cab, new Vector3(0f, h * 0.5f, -hd + 0.03f), new Vector3(w - t * 2f, h - 0.18f, 0.04f), paperOld, false);
            for (int i = 0; i < 4; i++)
                Box("Board_" + i, cab, new Vector3(0f, 0.62f + i * 0.52f, 0.01f),
                    new Vector3(w - t * 2f, 0.048f, d - 0.06f), frame, false);

            for (int r = 0; r < 4; r++)
            {
                float y = 0.15f + r * 0.52f;
                // 卷轴竖插成一排：正面看到的是一个个圆头，才像卷柜不像面包架
                int cols = 5;
                for (int i = 0; i < cols; i++)
                {
                    float px = (i - (cols - 1) * 0.5f) * 0.168f;
                    for (int row = 0; row < 2; row++)
                    {
                        float rad = 0.042f + Hash01(r * 31 + i * 7 + row * 3) * 0.008f;
                        var roll = Cyl("Roll_" + r + "_" + i + "_" + row, cab,
                            new Vector3(px, y + 0.048f + row * 0.092f, 0.02f), rad, 0.30f,
                            (r + i + row) % 6 == 0 ? silkRed : ((r + i + row) % 2 == 0 ? paper : paperOld));
                        roll.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        Ball("RollCap_" + r + "_" + i + "_" + row, cab,
                            new Vector3(px, y + 0.048f + row * 0.092f, 0.17f), 0.013f, gold);
                    }
                }
            }
            Box("RailLow", cab, new Vector3(0f, 0.19f, hd - 0.02f), new Vector3(w - t * 2f, 0.03f, 0.03f), front, false);
        }

        static void BuildHangingScroll(Transform parent, Vector3 pos, float yaw,
            Material paper, Material woodDark, Material gold, Color ink, Color inkFaint)
        {
            var g = Empty("HangingScroll", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box("Mount", g.transform, Vector3.zero, new Vector3(0.50f, 1.34f, 0.012f), paper, false);
            Box("MountLiner", g.transform, new Vector3(0f, 0f, -0.004f),
                new Vector3(0.42f, 1.14f, 0.010f), paper, false);
            for (int i = -1; i <= 1; i += 2)
            {
                var bar = Cyl("Bar_" + i, g.transform, new Vector3(0f, i * 0.70f, 0f), 0.016f, 0.58f, woodDark);
                bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                for (int e = -1; e <= 1; e += 2)
                    Ball("Knob_" + i + "_" + e, g.transform, new Vector3(e * 0.295f, i * 0.70f, 0f), 0.026f, gold);
            }
            // 淡墨远山与题款：三层山影 + 一列款字 + 一方朱印，山影压在画心下三分之一，上部留白
            float[] ridgeY = { -0.150f, -0.265f, -0.380f };
            float[] ridgeW = { 0.400f, 0.470f, 0.350f };
            float[] ridgeH = { 0.115f, 0.090f, 0.062f };
            for (int i = 0; i < 3; i++)
            {
                var ridge = Box("Ink_" + i, g.transform, new Vector3(-0.01f + i * 0.015f, ridgeY[i], -0.012f),
                    new Vector3(ridgeW[i], ridgeH[i], 0.006f), paper, false);
                ridge.GetComponent<Renderer>().sharedMaterial =
                    MatAs("EpiInk" + i, Color.Lerp(ink, paper.color, 0.24f + i * 0.16f), null, 0f, 0.04f);
                ridge.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1f) * 1.8f);
            }
            for (int i = 0; i < 6; i++)
                Box("Col_" + i, g.transform, new Vector3(0.150f, 0.40f - i * 0.062f, -0.014f),
                    new Vector3(0.030f, 0.024f, 0.005f), paper, false)
                    .GetComponent<Renderer>().sharedMaterial = MatAs("EpiInkLine", inkFaint, null, 0f, 0.04f);
            Box("Seal", g.transform, new Vector3(0.150f, 0.010f, -0.014f), new Vector3(0.036f, 0.036f, 0.005f), paper, false)
                .GetComponent<Renderer>().sharedMaterial =
                MatAs("EpiSealRed", Color.Lerp(HuaxiaPalette.Cinnabar, ink, 0.18f), null, 0f, 0.06f);
        }

        /// <summary>终章案上的《河山册》：预制体实例，停在末页状态。</summary>
        static void PlaceRealBook(Transform desk)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Huaxia/Resources/HandBook.prefab");
            if (prefab == null)
            {
                Debug.LogWarning("[Huaxia] 找不到 HandBook.prefab，终章的册子缺席");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, desk);
            go.name = "HeshanBook";
            go.transform.localPosition = new Vector3(0f, EpiDeskTop, 0.04f);
            go.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
            go.transform.localScale = Vector3.one * 1.15f;

            var hb = go.GetComponent<HandBook>();
            if (hb == null || hb.leaves == null) return;

            for (int i = 0; i < hb.leaves.Length; i++)
            {
                var leaf = hb.leaves[i];
                if (leaf == null || leaf.pivot == null) continue;
                leaf.pivotBase = leaf.pivot.localPosition;
                leaf.unlocked = true;
                // 前三叶翻到左边；末页留在右页——书停在「没写完」的那一叶
                leaf.turned = i < hb.leaves.Length - 1;
                if (leaf.turned)
                    leaf.pivot.localRotation = Quaternion.Euler(0f, 0f, 180f);
                DryLeaf(leaf);
            }

            hb.current = hb.leaves.Length - 1;
            hb.Restack();
        }

        /// <summary>烘进场景的干纸材质：不改预制体上的共享材质，只影响这一本。</summary>
        static void DryLeaf(BookLeaf leaf)
        {
            if (leaf.face != null && leaf.face.sharedMaterial != null)
            {
                var m = new Material(leaf.face.sharedMaterial);
                // 留一点潮气：全干的纸在这间屋子里会过曝成一片白
                m.SetFloat("_Wetness", 0.42f);
                m.SetFloat("_Ink", 1f);
                m.SetFloat("_Note", 1f);
                m.SetFloat("_Foot", 0f);
                leaf.face.sharedMaterial = m;
            }
            // 叶背保持预制体里那个「半干」的状态：全干会发灰发脏
        }

        /// <summary>侧墙书格：背板 + 四层搁板 + 册页与卷轴。</summary>
        static void BuildWallShelf(Transform parent, float w, float h, float d,
            Material woodDark, Material woodMat, Material paper, Material paperOld,
            Material silkRed, Material silk, Material gold)
        {
            Box("Back", parent, new Vector3(0f, h * 0.5f, -d * 0.44f), new Vector3(w, h, 0.05f), woodDark, false);
            for (int i = -1; i <= 1; i += 2)
                Box("Side_" + i, parent, new Vector3(i * (w * 0.5f - 0.03f), h * 0.5f, 0f),
                    new Vector3(0.06f, h, d), woodMat, false);
            Box("Top", parent, new Vector3(0f, h - 0.04f, 0f), new Vector3(w, 0.08f, d + 0.04f), woodMat, false);
            Box("Plinth", parent, new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.10f, d + 0.03f), woodMat, false);

            for (int k = 0; k < 4; k++)
            {
                float sy = 0.44f + k * 0.55f;
                Box("Shelf_" + k, parent, new Vector3(0f, sy, 0f), new Vector3(w - 0.10f, 0.045f, d - 0.03f), woodMat, false);

                // 册页横叠
                int books = 3 + (k % 2);
                for (int b = 0; b < books; b++)
                    Box("Book_" + k + "_" + b, parent,
                        new Vector3(-w * 0.24f + b * 0.008f, sy + 0.045f + b * 0.030f, -0.02f),
                        new Vector3(0.30f, 0.028f, 0.20f), b % 2 == 0 ? paper : paperOld, false);

                // 立卷
                for (int r = 0; r < 2; r++)
                {
                    var roll = Cyl("Roll_" + k + "_" + r, parent,
                        new Vector3(w * 0.16f + r * 0.10f, sy + 0.075f, 0.02f), 0.036f, 0.22f,
                        r == 0 ? paperOld : silk);
                    roll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Ball("RollCap_" + k + "_" + r, parent,
                        new Vector3(w * 0.16f + r * 0.10f + (r == 0 ? 0.115f : -0.115f), sy + 0.075f, 0.02f),
                        0.020f, gold);
                }
            }
            Box("Tassel", parent, new Vector3(w * 0.5f - 0.10f, h - 0.30f, d * 0.4f),
                new Vector3(0.03f, 0.34f, 0.03f), silkRed, false);
        }

        /// <summary>重设已有材质的纹理强弱，用来把某类构件的表面压平。</summary>
        static void Calm(string name, float detail, float macro, float bump)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + name + ".mat");
            if (m == null) return;
            m.SetFloat("_DetailStrength", detail);
            m.SetFloat("_MacroStrength", macro);
            m.SetFloat("_BumpScale", bump);
            EditorUtility.SetDirty(m);
        }

        /// <summary>重设已有材质的固有色与边缘光，用来把某类构件的明度拉回画面里。</summary>
        static void Tone(string name, Color color, float rim)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + name + ".mat");
            if (m == null) return;
            m.SetColor("_Color", Lin(color));
            m.SetFloat("_RimStrength", rim);
            EditorUtility.SetDirty(m);
        }

        static void SetupEpilogueEnvironment()
        {
            var sky = SkyMat("Sky_Epilogue", HuaxiaPalette.DuskZenith, HuaxiaPalette.DuskHorizon, 0.70f, 0.88f, 0.74f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(HuaxiaPalette.BlueGrey) * 0.92f;
            RenderSettings.ambientEquatorColor = Lin(HuaxiaPalette.Sand) * 0.78f;
            RenderSettings.ambientGroundColor = Lin(HuaxiaPalette.WoodDark) * 0.66f;
            RenderSettings.ambientIntensity = 0.98f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.007f;
            RenderSettings.fogColor = HuaxiaPalette.DuskFog;

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sunGo.transform.rotation = Quaternion.Euler(26f, 196f, 0f);
            QualitySettings.shadowDistance = 45f;
        }
    }
}
