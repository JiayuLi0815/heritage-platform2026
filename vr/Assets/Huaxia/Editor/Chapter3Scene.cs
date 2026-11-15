using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 第三章 · 泉州（后渚港）。
    /// 空间序列：石砌码头 → 栈桥与泊船 → 干坞（十二道隔舱板的剖面）→ 货场 → 三教碑 → 祈风石刻。
    /// 视觉关键词：海风、雾气、流动。色调：海蓝、石灰、暖白。
    /// 取舍：只做港口本身与可验证的碑刻，不做市井奇观，不做异域想象。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        // ---------------------------------------------------------------- 布局常量

        const float QzWaterY = 0.00f;    // 海平面
        const float QzQuayY = 1.50f;    // 码头面
        const float QzQuayX1 = 9.0f;    // 码头内侧（陆侧）边线
        const float QzQuayZ0 = -64.0f;
        const float QzQuayZ1 = 76.0f;
        const float QzPierZ = 16.0f;    // 栈桥中线
        const float QzPierX = -24.0f;   // 栈桥伸入海面的端点
        const float QzPierY = 1.66f;
        const float QzShipX = -6.6f;    // 泊在码头外的福船
        const float QzShipZ = -8.0f;
        const float QzDockX = 34.0f;    // 干坞
        const float QzDockZ = -34.0f;
        const float QzWallX = 20.0f;    // 三教碑
        const float QzWallZ = 8.0f;
        const float QzSteleX = 52.0f;   // 祈风石刻
        const float QzSteleZ = 34.0f;
        const float QzCargoX = 13.0f;   // 货场
        const float QzCargoZ = -20.0f;
        const float QzTempleY = 9.0f;   // 开元寺台地面

        // ---------------------------------------------------------------- 地形
        // QnRect / QnHill（矩形掩膜、锥形山包）定义在 Chapter2Scene.cs，是全工程共用的地形工具。

        static float QuanzhouHeight(float x, float z)
        {
            // 岸线在 x≈0：西侧是海床，东侧抬到码头面
            float shore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-7f, 3f, x));
            float h = Mathf.Lerp(-3.6f, QzQuayY - 0.10f, shore);

            // 陆地缓升（往东是清源山方向）
            h += 3.2f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 108f, x)) * shore;

            // 细碎起伏只作用在岸上，海床保持平顺
            h += (Mathf.Sin(x * 0.062f + 2.1f) + Mathf.Cos(z * 0.049f - 1.4f)) * 0.30f * shore;

            // 缓丘与坡地：岸上的大面不能是一个桌面，远看要有起伏的层次
            float open = shore * (1f - QnRect(x, z, -2f, 28f, -70f, 82f, 7f));
            float rollA = Mathf.PerlinNoise(x * 0.018f + 11.3f, z * 0.017f + 5.7f) - 0.5f;
            float rollB = Mathf.PerlinNoise(x * 0.055f + 31.1f, z * 0.052f + 9.2f) - 0.5f;
            h += (rollA * 3.6f + rollB * 1.15f) * open;

            // 九日山：祈风石刻所在的低丘
            h += QnHill(x, z, 58f, 40f, 26f, 24f, 6.2f);
            // 清源山余脉（岛内丘陵）
            h += QnHill(x, z, 110f, 40f, 60f, 72f, 12.0f);
            h += QnHill(x, z, 88f, -118f, 62f, 56f, 7.5f);

            // 港区平台与干坞场院：人工平整出来的地面
            float apron = QnRect(x, z, -2f, 28f, -70f, 82f, 7f);
            apron = Mathf.Max(apron, QnRect(x, z, 20f, 50f, -54f, -14f, 7f));
            h = Mathf.Lerp(h, QzQuayY - 0.10f, apron);

            // 开元寺台地：双塔要站在同一块平地上，山坡先压出一级台地
            h = Mathf.Lerp(h, QzTempleY, QnRect(x, z, 60f, 96f, 4f, 38f, 12f));

            return h;
        }

        static Color QuanzhouColor(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.048f + 3.1f, z * 0.048f + 8.4f);
            float n2 = Mathf.PerlinNoise(x * 0.155f + 21.6f, z * 0.155f + 4.2f);
            float n3 = Mathf.PerlinNoise(x * 0.031f + 61.2f, z * 0.031f + 17.8f);

            // 海床：湿泥偏青
            Color seabed = Color.Lerp(new Color(0.24f, 0.29f, 0.28f), new Color(0.31f, 0.34f, 0.30f), n);

            // 岸上：闽南的花岗岩风化土——灰，不是黄。干草只成片点缀，不能整片草绿。
            Color land = Color.Lerp(new Color(0.25f, 0.27f, 0.26f), new Color(0.22f, 0.26f, 0.23f), n);
            land = Color.Lerp(land, new Color(0.32f, 0.33f, 0.34f), Mathf.SmoothStep(0.50f, 0.80f, n2) * 0.62f);
            // 斑块状的枯草与低矮灌丛，让大片地面不至于读成一整块平色
            land = Color.Lerp(land, new Color(0.29f, 0.26f, 0.19f), Mathf.SmoothStep(0.62f, 0.88f, n3) * 0.30f);
            land = Color.Lerp(land, new Color(0.16f, 0.21f, 0.16f), Mathf.SmoothStep(0.56f, 0.86f, n) * 0.58f);

            float shore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-1.5f, 4.0f, x));
            Color c = Color.Lerp(seabed, land, shore);

            // 潮间带：湿石与藻色
            c = Color.Lerp(c, new Color(0.30f, 0.33f, 0.28f),
                Mathf.InverseLerp(0f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-1.2f, 2.2f, x))) *
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.4f, 1.0f, x)));

            // 港区：踩实的碎石地面（比土更灰、更亮一点，但不许发黄）
            float apron = Mathf.Max(QnRect(x, z, -2f, 28f, -70f, 82f, 7f),
                                    QnRect(x, z, 20f, 50f, -54f, -14f, 7f));
            Color gravel = Color.Lerp(new Color(0.30f, 0.31f, 0.32f), new Color(0.25f, 0.27f, 0.28f), n2);
            c = Color.Lerp(c, gravel, apron * 0.72f);

            // 丘陵：常绿林的暗绿，海拔越高越偏青灰
            float hill = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 13f, QuanzhouHeight(x, z)));
            c = Color.Lerp(c, new Color(0.21f, 0.27f, 0.22f), hill * 0.76f);
            float high = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(13f, 24f, QuanzhouHeight(x, z)));
            c = Color.Lerp(c, new Color(0.35f, 0.39f, 0.39f), high * 0.7f);

            return c;
        }

        // ---------------------------------------------------------------- 环境

        static void SetupQuanzhouEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(new Color(0.70f, 0.75f, 0.78f));
            RenderSettings.ambientEquatorColor = Lin(new Color(0.63f, 0.64f, 0.60f));
            RenderSettings.ambientGroundColor = Lin(new Color(0.40f, 0.41f, 0.37f));
            RenderSettings.ambientIntensity = 1.00f;

            // 海雾：exp² 比线性雾更像海面，近处清楚、远处迅速泛白
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0062f;
            RenderSettings.fogColor = Lin(new Color(0.76f, 0.80f, 0.83f));

            // 闽南的早晨：高、薄、泛白的天，地面反光是暖白
            RenderSettings.skybox = SkyMat("Sky_Quanzhou",
                new Color(0.52f, 0.67f, 0.80f), new Color(0.80f, 0.80f, 0.77f), 0.80f, 0.74f, 0.93f);
            DynamicGI.UpdateEnvironment();

            QualitySettings.shadowDistance = 130f;
        }

        // ---------------------------------------------------------------- 主构建

        public static void BuildQuanzhouScene()
        {
            LoadFonts();
            EnsureFolder(MatDir);
            EnsureFolder(SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupQuanzhouEnvironment();
            AddSceneAmbient("amb_quanzhou", 0.48f);

            var sunGO = new GameObject("MorningSun", typeof(Light));
            sunGO.transform.rotation = Quaternion.Euler(32f, 38f, 0f);
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Lin(new Color(1f, 0.95f, 0.87f));
            sun.intensity = 1.70f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.90f;
            sun.shadowBias = 0.045f;
            RenderSettings.sun = sun;

            var fillGO = new GameObject("SeaFill", typeof(Light));
            fillGO.transform.rotation = Quaternion.Euler(26f, 218f, 0f);   // 与主光相对：补北墙与东墙
            var fill = fillGO.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Lin(new Color(0.68f, 0.78f, 0.88f));
            fill.intensity = 0.70f;
            fill.shadows = LightShadows.None;

            var root = Empty("Quanzhou", null, Vector3.zero).transform;

            // ------------------------------------------------------------ 地形
            var groundMat = TerrainMat("Terrain_Quanzhou");
            groundMat.SetFloat("_DetailScale", 0.16f);
            groundMat.SetFloat("_DetailStrength", 0.90f);
            groundMat.SetFloat("_MacroScale", 0.026f);
            groundMat.SetFloat("_MacroStrength", 0.34f);
            groundMat.SetFloat("_BumpScale", 0.70f);
            MeshObject("Terrain", root, Vector3.zero,
                MeshFactory.Terrain(340f, 220, QuanzhouHeight, QuanzhouColor), groundMat, true, false);

            // ------------------------------------------------------------ 材质
            // 铺石压暗一档、褪掉青灰：0.55 的青灰在 1.7 倍太阳下会烧成 0xC8C8C0，
            // 整座码头读成刚浇完的水泥。泉州老石埕是暖灰的旧花岗岩。
            var paveMat = MatAs("QzPave", new Color(0.46f, 0.46f, 0.44f), "stone", 0.02f, 0.20f);
            var paveWornMat = MatAs("QzPaveWorn", new Color(0.53f, 0.52f, 0.49f), "stone", 0.02f, 0.18f);
            var paveDarkMat = MatAs("QzPaveDark", new Color(0.40f, 0.40f, 0.38f), "stone", 0.02f, 0.20f);
            var stoneMat = MatAs("QzStone", new Color(0.52f, 0.53f, 0.53f), "stone", 0.02f, 0.18f);
            var stonePaleMat = MatAs("QzStonePale", new Color(0.60f, 0.61f, 0.61f), "stone", 0.02f, 0.22f);
            var stoneDarkMat = MatAs("QzStoneDark", new Color(0.33f, 0.34f, 0.35f), "stone", 0.02f, 0.14f);
            foreach (var gm in new[] { paveMat, paveWornMat, paveDarkMat })
            {
                gm.SetFloat("_DetailScale", 0.55f);
                gm.SetFloat("_BumpScale", 0.50f);
                gm.SetFloat("_MacroStrength", 0.30f);
            }
            foreach (var gm in new[] { stoneMat, stonePaleMat })
            {
                gm.SetFloat("_DetailScale", 0.60f);
                gm.SetFloat("_BumpScale", 0.42f);
            }
            var wetStoneMat = MatAs("QzWetStone", new Color(0.31f, 0.33f, 0.31f), "stone", 0.02f, 0.38f);
            var reefMat = MatAs("QzReef", new Color(0.39f, 0.39f, 0.36f), "stone", 0.02f, 0.14f);
            var woodMat = MatAs("QzWood", new Color(0.40f, 0.31f, 0.22f), "wood", 0f, 0.18f);
            var woodPaleMat = MatAs("QzWoodPale", new Color(0.52f, 0.41f, 0.27f), "wood", 0f, 0.20f);
            var hullMat = MatAs("QzHull", new Color(0.32f, 0.24f, 0.17f), "wood", 0f, 0.16f);
            var hullInnerMat = MatAs("QzHullInner", new Color(0.44f, 0.34f, 0.23f), "wood", 0f, 0.18f);
            var plankNewMat = MatAs("QzPlankNew", new Color(0.60f, 0.48f, 0.31f), "wood", 0f, 0.24f);
            var ropeMat = MatAs("QzRope", new Color(0.61f, 0.53f, 0.37f), "thatch", 0f, 0.14f);
            var sailMat = MatAs("QzSail", new Color(0.86f, 0.82f, 0.70f), "fabric", 0f, 0.12f);
            var potteryMat = MatAs("QzPottery", new Color(0.50f, 0.39f, 0.28f), "pottery", 0f, 0.18f);
            var potteryDarkMat = MatAs("QzPotteryDark", new Color(0.36f, 0.29f, 0.22f), "pottery", 0f, 0.14f);
            var celadonMat = MatAs("QzCeladon", new Color(0.56f, 0.63f, 0.57f), "pottery", 0f, 0.32f);
            var spiceMat = MatAs("QzSpice", new Color(0.44f, 0.31f, 0.19f), "earth", 0f, 0.10f);
            var sackMat = MatAs("QzSack", new Color(0.63f, 0.58f, 0.46f), "fabric", 0f, 0.12f);
            var tileMat = MatAs("QzRoofTile", new Color(0.47f, 0.46f, 0.44f), "tile", 0f, 0.16f);
            var grassMat = MatAs("QzGrass", new Color(0.50f, 0.52f, 0.34f), "foliage", 0f, 0.22f);
            var reedMat = MatAs("QzReed", new Color(0.58f, 0.56f, 0.38f), "thatch", 0f, 0.16f);
            var banyanMat = MatAs("QzBanyanLeaf", new Color(0.29f, 0.40f, 0.27f), "foliage", 0f, 0.24f);
            var leafMat = MatAs("QzLeaf", new Color(0.38f, 0.46f, 0.30f), "foliage", 0f, 0.24f);
            var flowerMat = MatAs("QzErythrina", new Color(0.64f, 0.26f, 0.21f), "foliage", 0f, 0.30f);
            var barkMat = MatAs("QzBark", new Color(0.35f, 0.31f, 0.26f), "bark", 0f, 0.12f);
            var brickMat = MatAs("QzBrickRed", new Color(0.46f, 0.23f, 0.17f), "brick", 0f, 0.16f);
            var plasterMat = MatAs("QzPlaster", new Color(0.80f, 0.78f, 0.72f), "plaster", 0f, 0.10f);
            var graniteMat = MatAs("QzGranitePale", new Color(0.62f, 0.62f, 0.59f), "stone", 0.02f, 0.18f);
            var mossMat = MatAs("QzMoss", new Color(0.33f, 0.39f, 0.26f), "foliage", 0f, 0.14f);
            var farMat = MatAs("QzFar", new Color(0.46f, 0.52f, 0.55f), "ridge", 0f, 0.06f);
            var lanternMat = EmissiveMat("QzLantern", new Color(0.86f, 0.66f, 0.40f),
                new Color(0.36f, 0.235f, 0.115f), 0f, 0.30f);

            // ------------------------------------------------------------ 海
            var seaMat = WaterMat("QzSea", new Color(0.24f, 0.37f, 0.42f, 0.60f), 0.92f);
            seaMat.SetFloat("_AlphaBase", 0.90f);
            seaMat.SetColor("_DeepColor", Lin(new Color(0.15f, 0.29f, 0.35f, 1f)));
            seaMat.SetColor("_ShallowColor", Lin(new Color(0.36f, 0.50f, 0.50f, 1f)));
            seaMat.SetColor("_SkyTint", Lin(new Color(0.86f, 0.89f, 0.86f)) * 0.42f);
            seaMat.SetFloat("_WaveStrength", 0.36f);

            var sea = WaterQuad("Sea", root, new Vector3(-84f, QzWaterY, 0f), 176f, 340f, seaMat);
            QzNoShadow(sea);

            // 近岸浪线与礁石群
            QzShore(root, reefMat, wetStoneMat, stoneDarkMat, reedMat, grassMat);
            QzBoulders(root, reefMat, graniteMat, stoneDarkMat, mossMat);

            // ------------------------------------------------------------ 远山：清源山三重
            MeshObject("Ridge_Far", root, new Vector3(60f, -2f, 214f),
                MeshFactory.MountainRidge(760f, 88f, 5, 41, 30f), farMat, false);
            MeshObject("Ridge_Mid", root, new Vector3(120f, -1f, 176f),
                MeshFactory.MountainRidge(520f, 54f, 6, 13, 22f), farMat, false).transform.localRotation =
                Quaternion.Euler(0f, 24f, 0f);
            MeshObject("Ridge_Sea", root, new Vector3(-190f, -3f, 40f),
                MeshFactory.MountainRidge(560f, 34f, 4, 67, 18f), farMat, false).transform.localRotation =
                Quaternion.Euler(0f, 88f, 0f);

            // ------------------------------------------------------------ 码头
            QzYardPave(root, paveMat, paveWornMat, paveDarkMat);
            QzYardClutter(root, woodMat, woodPaleMat, plankNewMat, ropeMat, potteryMat,
                potteryDarkMat, sackMat, tileMat, stoneMat);
                        QzQuay(root, paveMat, paveWornMat, paveDarkMat, stoneMat, stonePaleMat, stoneDarkMat, wetStoneMat,
                woodMat, woodPaleMat, ropeMat, potteryMat, potteryDarkMat, sackMat, tileMat, lanternMat);

            // ------------------------------------------------------------ 栈桥
            QzPier(root, woodMat, woodPaleMat, stoneDarkMat, ropeMat, plankNewMat,
                potteryMat, sackMat, lanternMat);

            // ------------------------------------------------------------ 泊船：福船
            QzShip(root, hullMat, hullInnerMat, woodPaleMat, plankNewMat, sailMat,
                ropeMat, tileMat, lanternMat);

            // ------------------------------------------------------------ 干坞：水密隔舱剖面
            QzDryDock(root, stoneMat, stonePaleMat, stoneDarkMat, woodMat, woodPaleMat,
                hullMat, hullInnerMat, plankNewMat, ropeMat);

            // ------------------------------------------------------------ 货场
            QzCargo(root, woodMat, woodPaleMat, potteryMat, potteryDarkMat, celadonMat,
                spiceMat, sackMat, ropeMat, tileMat);

            // ------------------------------------------------------------ 三教碑
            QzThreeTeachWall(root, stoneMat, stonePaleMat, stoneDarkMat, tileMat, woodMat);

            // ------------------------------------------------------------ 祈风石刻
            QzPrayerStele(root, stonePaleMat, stoneMat, stoneDarkMat, woodMat, lanternMat, mossMat);

            // ------------------------------------------------------------ 洛阳桥
            // 洛阳桥在洛阳江入海口，宋代跨海梁式石桥的地标。场景里没有它的实体，
            // 只把一个「桥头」做出来：筏形基础（抛石成堤、堤上砌墩）、石梁桥面、
            // 桥基上养的牡蛎（蛎房把石块箍成一体）——三样都是这座桥真正成立的道理。
            QzLuoyangBridge(root, stoneMat, stonePaleMat, stoneDarkMat, mossMat);

            // ------------------------------------------------------------ 石塔（远岸航标）
            QzPagoda(root, stonePaleMat, stoneDarkMat, graniteMat);

            // ------------------------------------------------------------ 植被
            QzVillage(root, brickMat, graniteMat, stoneDarkMat, tileMat, woodMat, plasterMat,
                leafMat, barkMat);
            QzFlora(root, grassMat, reedMat, banyanMat, leafMat, flowerMat, barkMat);

            // ------------------------------------------------------------ 雾中帆影
            QzDistantSails(root, hullMat, sailMat, woodMat);

            // ------------------------------------------------------------ 三处站点
            // 泉州只留两处能走到的「看点」：桥头一处、石塔一处，末了在原地合卷。
            // 桥上、塔下的图文与语音都来自 HeritageSpots，这里只负责站位和串联。
            var ptBridge = CreateInfoPoint(root, "Bridge",
                new Vector3(4.2f, QzQuayY + 0.02f, -44.0f),
                "洛阳桥", "第一处 · 跨海梁式石桥",
                "北宋皇祐五年始建，六年而成。江底是烂泥，桥基站不住，于是抛石成堤、堤上砌墩，" +
                "再在桥基上养牡蛎，让蛎房把石块箍成一体。",
                "bridge", new Color(0.72f, 0.74f, 0.70f), SourceTier.Confirmed,
                "桥基、桥墩、蛎房固基均有考古与文献双重依据",
                heritageId: "spot_qz_bridge", number: "①");

            var ptPagoda = CreateInfoPoint(root, "PagodaSpot",
                new Vector3(78f, QuanzhouHeight(78f, 8f) + 0.02f, 8.0f),
                "开元寺双塔", "第二处 · 石头做的木塔",
                "南宋绍定元年起改石构。五层八角，逐层收分，斗拱与出檐都用花岗岩做出木构的样子——" +
                "这是把木塔的做法搬进石头里。",
                "pagoda", new Color(0.70f, 0.68f, 0.60f), SourceTier.Confirmed,
                "双塔的年代、高度、形制均有实测与文献依据",
                heritageId: "spot_qz_pagoda", number: "②");

            var ptLeave = CreateInfoPoint(root, "Leave",
                new Vector3(16f, QuanzhouHeight(16f, 26f) + 0.02f, 26f),
                "合上这一卷", "终 · 回圆坛",
                "三卷都看过了。\n\n" +
                "洛阳桥把桥墩立在烂泥上，靠的是抛石成堤、养蛎固基；开元寺双塔把木构的斗拱出檐" +
                "做进花岗岩里，靠的是逐层收分。\n\n" +
                "一个解决“怎么站住”，一个解决“怎么立得高”。都是石头，思路不一样。\n\n" +
                "按 E 合上这一卷，回序章的圆坛。",
                "leave", new Color(0.80f, 0.72f, 0.50f), SourceTier.Confirmed,
                "两处遗产的年代、形制均有实测与文献依据",
                endScene: "Archive", endSpawnId: "hub_center",
                endLine: "合上这一卷。回圆坛去罢。");


            var iBridge = ptBridge.GetComponent<Interactable>();
            var iPagoda = ptPagoda.GetComponent<Interactable>();
            if (iBridge != null) iBridge.leadTo = ptPagoda.transform;
            if (iPagoda != null) iPagoda.leadTo = ptLeave.transform;

            // ------------------------------------------------ 玩家、视角、UI
            var player = CreatePlayer(new Vector3(5.4f, QzQuayY + 0.06f, -56f), 0f);
            ApplyGrade(player, "quanzhou");

            // 来路边的界石：想回去就站到石前按 E，不必把这一卷走完
            CreateReturnStone(root, "Quanzhou", new Vector3(9.2f, QzQuayY + 0.06f, -54.8f),
                -108f, "泉州后渚港", "quanzhou", stoneMat, woodMat);

            var startSpawn = Empty("Spawn_from_hub", root, new Vector3(5.4f, QzQuayY + 0.06f, -56f));
            startSpawn.AddComponent<SpawnPoint>().id = "from_hub";
            var cam = player.GetComponentInChildren<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;

            var aerial = CreateAerialCamera(new Vector3(58f, 62f, -84f), new Vector3(34f, 0f, 0f));
            var bird = player.AddComponent<BirdEyeView>();
            bird.playerCamera = cam;
            bird.aerialCamera = aerial;
            bird.controller = player.GetComponent<DesktopPlayerController>();
            bird.aerialPosition = new Vector3(58f, 62f, -84f);
            bird.aerialEuler = new Vector3(34f, 0f, 0f);

            CreateHUD("WASD 移动 · Shift 奔跑 · 「、」跳跃 · E 互动 · Tab 取册子 · V 整体视角 · Q 合卷回山 · Esc 释放鼠标");

            var systems = new GameObject("Systems");
            var tasks = systems.AddComponent<TaskManager>();
            tasks.chapterId = "quanzhou";
            tasks.chapterLabel = "第三章 · 港与海";
            tasks.chapterSubtitle = "泉州后渚港";
            tasks.objectives = new[]
            {
                new Objective { id = "bridge", label = "在桥头看洛阳桥怎么在烂泥上站住" },
                new Objective { id = "pagoda", label = "在石塔下看石头怎么模仿木头" },
            };
            tasks.introBody =
                "眼前是泉州后渚港，宋元时期泉州港的主要港区之一。\n\n" +
                "转过身就是洛阳桥的桥头：江底是烂泥，桥基站不住，于是往江里抛石成堤，" +
                "堤上砌墩，再在桥基上养牡蛎，让蛎房把石块箍成一体。\n\n" +
                "往东北是开元寺双塔的方向。两座石塔把木构的斗拱、出檐都用花岗岩做出来，" +
                "逐层收分，七百多年不倒。\n\n" +
                "看完这两处，就在原地合卷回圆坛。按 V 能飞起来俯瞰；雾大，远处桅杆只剩一道影子。";

            tasks.archiveTitle = "梯航万国：港口与交流";
            tasks.archiveBody =
                "现场发现\n" +
                "· 后渚港南宋海船残长约 24 米，船体以隔舱板分为 13 舱，隔舱板与壳板、肋骨榫接。\n" +
                "· 沉船舱内出土香料、药物、瓷器等货物，说明其为远洋商船而非内河船。\n" +
                "· 石砌码头以条石干砌、压顶石收边；栈桥以密排木桩承托横铺厚板。\n" +
                "· 缆桩上部见长期磨出的凹痕，是系泊活动的直接痕迹。\n" +
                "· 洛阳桥北宋皇祐五年（1053）始建、嘉祐四年（1059）成，桥基沿桥线抛石成堤，" +
                "堤上砌墩，是为「筏形基础」；墩基上养殖牡蛎，以蛎房把散石箍成一体，" +
                "现存蛎房与分水尖遗迹。\n" +
                "· 开元寺双塔南宋改石构：东塔镇国塔高 48.24 米，西塔仁寿塔高 44.06 米，" +
                "皆五层八角，塔身以花岗岩做出斗拱、出檐与逐层收分。\n" +
                "· 九日山存宋元祈风石刻十余方，记郡守为海舶祈风、事毕题名。\n" +
                "· 泉州存汉文、阿拉伯文、拉丁文、泰米尔文等多种文字的碑刻，出自不同社群。\n\n" +
                "历史解释\n" +
                "宋元时期，泉州是海上丝绸之路的重要枢纽，被称「梯航万国」。" +
                "水密隔舱、季风信期、官方祈风与多社群共处，共同构成这套海洋贸易体系的技术与制度基础。" +
                "2021 年，「泉州：宋元中国的世界海洋商贸中心」列入世界遗产名录。\n\n" +
                "关联线索\n" +
                "与良渚、秦陵的对照：三处都是「用一套办法把大人群组织起来」的样本——" +
                "良渚靠水利与稻作，秦靠郡县与标准器，泉州靠航线与信用。" +
                "这是比较性的观察，不构成直接发展关系。";
            tasks.returnScene = "Archive";

            CreateDust(root, new Vector3(-12f, 3.0f, 10f), new Vector3(120f, 6f, 120f), 220, 7f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneDir + "/Quanzhou.unity");
            Debug.Log("[Huaxia] 泉州场景已保存");
        }

        // ================================================================ 通用小件

        static void QzNoShadow(GameObject go)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        /// <summary>两点之间的缆绳：用细圆柱对齐方向，避免为每根绳子单独建网格。</summary>
        static GameObject QzRope(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material mat)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 0.0004f) return null;
            var go = Cyl(name, parent, (a + b) * 0.5f, radius, d.magnitude, mat);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return go;
        }

        // ================================================================ 海与岸

        static void QzShore(Transform parent, Material reef, Material wet, Material dark,
                            Material reed, Material grass)
        {
            var g = Empty("Shore", parent, Vector3.zero);

            // 礁石只沿岸线散布，码头段让开：这些石头是岸的一部分，不是装饰摆件
            for (int i = 0; i < 34; i++)
            {
                float t = i / 33f;
                float z = Mathf.Lerp(-104f, 104f, t) + Mathf.Sin(i * 2.7f) * 5.0f;
                if (z > QzQuayZ0 - 3f && z < QzQuayZ1 + 3f) continue;
                float x = -0.9f - Mathf.Abs(Mathf.Sin(i * 1.9f)) * 4.2f;
                float s = 0.75f + Mathf.Abs(Mathf.Cos(i * 3.1f)) * 1.7f;
                var r = Box("Reef_" + i, g.transform,
                    new Vector3(x, -0.28f - s * 0.22f, z),
                    new Vector3(s * 1.9f, s * 0.95f, s * 1.5f), i % 3 == 0 ? wet : reef, false);
                r.transform.localRotation = Quaternion.Euler(i * 9f, i * 41f, i * 13f);
            }

            // 高潮线附近的湿石带：贴在岸壁外的几块大石，颜色更深
            for (int i = 0; i < 10; i++)
            {
                float z = -100f + i * 21f;
                if (z > QzQuayZ0 - 2f && z < QzQuayZ1 + 2f) continue;
                Box("WetRock_" + i, g.transform, new Vector3(-1.6f, -0.55f, z),
                    new Vector3(3.0f, 1.0f, 2.4f), dark, false);
            }

            // 南端的滩地：芦苇与红树林，给港口一个野的边界
            QzReeds(g.transform, new Vector3(-6f, 0f, -86f), 26f, 16f, reed, grass, 130);
            QzReeds(g.transform, new Vector3(2f, 0f, 88f), 22f, 14f, reed, grass, 96);
            QzReeds(g.transform, new Vector3(-30f, 0f, 60f), 14f, 10f, reed, grass, 44);
        }

        /// <summary>成片芦苇/草束：细长圆锥成簇，避免用一个大盒子冒充植被。</summary>
        static void QzReeds(Transform parent, Vector3 center, float w, float d,
                            Material reed, Material grass, int count)
        {
            var g = Empty("Reeds", parent, center);
            for (int i = 0; i < count; i++)
            {
                float u = Hash01(i * 3 + 1), v = Hash01(i * 7 + 5);
                float x = (u - 0.5f) * w, z = (v - 0.5f) * d;
                float ground = QuanzhouHeight(center.x + x, center.z + z);
                if (ground < -0.6f) continue;   // 别长到水里
                float h = 0.45f + Hash01(i * 11 + 3) * 0.55f;
                var blade = Cyl("Reed_" + i, g.transform,
                    new Vector3(x, h * 0.42f, z), 0.05f, h, i % 5 == 0 ? reed : grass, false);
                blade.transform.localRotation = Quaternion.Euler(
                    (Hash01(i * 13) - 0.5f) * 16f, Hash01(i * 17) * 360f, (Hash01(i * 19) - 0.5f) * 16f);
            }
        }

        static float Hash01(int n)
        {
            float f = Mathf.Sin(n * 12.9898f + 78.233f) * 43758.5453f;
            return f - Mathf.Floor(f);
        }

        // ================================================================ 港区场院

        /// <summary>港区场院铺装：块石随地形起伏铺。一整片裸地无论怎么调顶点色都会读成沙滩。</summary>
        static void QzYardPave(Transform parent, Material pave, Material paveWorn, Material paveDark)
        {
            var g = Empty("YardPave", parent, Vector3.zero);
            // 缝宽从 6 cm 收到 2.8 cm：铺装占掉画面下半幅时，
            // 每一道黑缝都会被读成"棋盘上的黑线"，整片场院像一块塑料地垫。
            const float sw = 1.86f, sl = 1.58f, gap = 0.028f;
            float x0 = QzQuayX1 + 0.06f, x1 = 31.5f, z0 = -56f, z1 = 45.5f;
            int nx = Mathf.FloorToInt((x1 - x0) / sw);
            int nz = Mathf.FloorToInt((z1 - z0) / sl);
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float px = x0 + sw * (ix + 0.5f);
                    float pz = z0 + sl * (iz + 0.5f) + (ix % 2) * 0.18f;
                    // 三教碑的基座自己落地，别让石板插进去
                    if (Mathf.Abs(px - QzWallX) < 4.6f && Mathf.Abs(pz - QzWallZ) < 3.4f) continue;
                    // 干坞坞底比场院低一米半，石板铺过去会悬空
                    if (Mathf.Abs(px - QzDockX) < 13.0f && Mathf.Abs(pz - QzDockZ) < 21.5f) continue;
                    float yt = QuanzhouHeight(px, pz);
                    if (yt < 0.9f) continue;
                    float hgt = 0.42f + Hash01(ix * 137 + iz * 19) * 0.05f;
                    var pm = Hash01(ix * 73 + iz * 41) > 0.82f ? paveWorn
                           : Hash01(ix * 29 + iz * 97) > 0.92f ? paveDark : pave;
                    Box("YardSlab_" + iz + "_" + ix, g.transform,
                        new Vector3(px, yt - 0.30f + hgt * 0.5f, pz),
                        new Vector3(sw - gap, hgt, sl - gap), pm, false)
                        .transform.localRotation = Quaternion.Euler(0f,
                            (Hash01(ix * 53 + iz * 61) - 0.5f) * 2.2f, 0f);
                }
        }


        /// <summary>场院杂物：木箱、陶瓮、麻袋、缆绳、木料堆。
        /// 空场院是"建筑模型"，码头是干过活的地方——这些是让地面有生活痕迹的最低成本。</summary>
        static void QzYardClutter(Transform parent, Material wood, Material woodPale, Material plank,
                                  Material rope, Material pottery, Material potteryDark,
                                  Material sack, Material tile, Material stone)
        {
            var g = Empty("YardClutter", parent, Vector3.zero);

            void Crate(float x, float z, float s, float yaw, int seed)
            {
                float y = QuanzhouHeight(x, z);
                var c = Box("A_Crate", g.transform, new Vector3(x, y + s * 0.5f, z),
                    new Vector3(s, s * 0.82f, s * 0.92f), (seed % 5 == 0) ? plank : wood, true);
                c.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                Box("A_CrateLid", g.transform, new Vector3(x, y + s * 0.84f, z),
                    new Vector3(s * 1.03f, s * 0.06f, s * 0.95f), woodPale, false)
                    .transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }

            void Barrel(float x, float z, float s, int seed)
            {
                float y = QuanzhouHeight(x, z);
                Cyl("A_Barrel", g.transform, new Vector3(x, y + 0.30f * s, z), 0.30f * s, 0.60f * s,
                    (seed % 2 == 0) ? pottery : potteryDark);
                Cyl("A_BarrelHoop", g.transform, new Vector3(x, y + 0.44f * s, z), 0.315f * s, 0.04f * s, rope);
            }

            void SackPile(float x, float z, int n, int seed)
            {
                float y = QuanzhouHeight(x, z);
                for (int i = 0; i < n; i++)
                {
                    float ox = (Rnd(seed * 13 + i * 7) - 0.5f) * 0.55f;
                    float oz = (Rnd(seed * 17 + i * 11) - 0.5f) * 0.45f;
                    float hh = 0.34f + Rnd(seed * 19 + i * 5) * 0.14f;
                    var b = Box("A_Sack", g.transform, new Vector3(x + ox, y + hh * 0.5f + (i > 1 ? 0.30f : 0f), z + oz),
                        new Vector3(0.46f, hh, 0.32f), sack, false);
                    b.transform.localRotation = Quaternion.Euler(0f, Rnd(seed * 23 + i) * 60f, 0f);
                }
            }

            void Timber(float x, float z, float yaw, int seed)
            {
                float y = QuanzhouHeight(x, z);
                var t = Empty("A_Timber", g.transform, new Vector3(x, y, z));
                t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                for (int l = 0; l < 3; l++)
                    for (int i = 0; i < 4; i++)
                        Cyl("A_Log", t.transform,
                            new Vector3((i - 1.5f) * 0.24f, 0.14f + l * 0.26f, 0f), 0.11f, 3.1f, plank)
                            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            void RopeCoil(float x, float z, float s)
            {
                float y = QuanzhouHeight(x, z);
                Cyl("A_Rope", g.transform, new Vector3(x, y + 0.05f, z), 0.36f * s, 0.10f * s, rope);
                Cyl("A_Rope2", g.transform, new Vector3(x, y + 0.13f, z), 0.30f * s, 0.07f * s, rope);
            }

            var plan = new (float x, float z, int kind, float s, float yaw)[]
            {
                (11.2f, -48.5f, 0, 1.05f, 12f), (12.4f, -47.6f, 0, 0.86f, -18f),
                (13.1f, -49.2f, 0, 1.18f, 34f), (10.4f, -46.8f, 2, 1f, 0f),
                (14.6f, -44.0f, 1, 1.15f, 0f), (15.6f, -44.6f, 1, 0.95f, 0f),
                (14.9f, -42.8f, 3, 1f, 8f), (16.8f, -46.5f, 4, 1.1f, 0f),
                (9.8f, -42.2f, 2, 1f, 0f), (12.8f, -40.5f, 0, 1.0f, -28f),
                (15.4f, -39.0f, 4, 0.9f, 0f), (11.0f, -37.6f, 1, 1.0f, 0f),
                (17.4f, -41.0f, 0, 1.15f, 20f), (18.0f, -38.4f, 3, 1f, -14f),
                (13.6f, -35.2f, 2, 1f, 0f), (9.6f, -33.8f, 4, 1f, 0f),
                (16.2f, -33.0f, 1, 1.05f, 0f), (18.6f, -35.6f, 0, 0.95f, 44f),
            };

            for (int i = 0; i < plan.Length; i++)
            {
                var p = plan[i];
                switch (p.kind)
                {
                    case 0: Crate(p.x, p.z, p.s, p.yaw, i); break;
                    case 1: Barrel(p.x, p.z, p.s, i); break;
                    case 2: SackPile(p.x, p.z, 3 + (i % 2), i); break;
                    case 3: Timber(p.x, p.z, p.yaw, i); break;
                    case 4: RopeCoil(p.x, p.z, p.s); break;
                }
            }
        }

        // ================================================================ 码头

        static void QzQuay(Transform parent, Material pave, Material paveWorn, Material paveDark,
                           Material stone, Material stonePale, Material stoneDark,
                           Material wet, Material wood, Material woodPale, Material rope,
                           Material pottery, Material potteryDark, Material sack, Material tile, Material lantern)
        {
            var g = Empty("Quay", parent, Vector3.zero);
            float zc = (QzQuayZ0 + QzQuayZ1) * 0.5f;
            float zl = QzQuayZ1 - QzQuayZ0;

            // 码头面：底层整体，面上一层块石。整块大板会被读成一片沙丘，
            // 必须靠分缝、逐块的高差与深浅把「石铺」这件事做出来。
            Box("QuayPave", g.transform, new Vector3(QzQuayX1 * 0.5f - 0.1f, QzQuayY - 0.26f, zc),
                new Vector3(QzQuayX1, 0.28f, zl), stoneDark);
            {
                const float sw = 1.42f, sl = 1.18f, gap = 0.055f;
                int nx = Mathf.FloorToInt((QzQuayX1 - 0.2f) / sw);
                int nz = Mathf.FloorToInt(zl / sl);
                for (int iz = 0; iz < nz; iz++)
                    for (int ix = 0; ix < nx; ix++)
                    {
                        float px = 0.1f + sw * (ix + 0.5f);
                        float pz = QzQuayZ0 + sl * (iz + 0.5f) + (ix % 2) * 0.14f;
                        float hgt = 0.15f + Hash01(ix * 131 + iz * 17) * 0.035f;
                        var pm = Hash01(ix * 71 + iz * 37) > 0.86f ? paveWorn
                               : Hash01(ix * 23 + iz * 91) > 0.88f ? paveDark : pave;
                        Box("QuaySlab_" + iz + "_" + ix, g.transform,
                            new Vector3(px, QzQuayY - 0.20f + hgt * 0.5f, pz),
                            new Vector3(sw - gap, hgt, sl - gap), pm, false);
                    }
            }

            // 岸壁：一直落到海床，压顶石收边
            Box("QuayWall", g.transform, new Vector3(0.55f, QzQuayY - 2.60f, zc),
                new Vector3(1.35f, 5.20f, zl), stone);
            Box("QuayCoping", g.transform, new Vector3(0.55f, QzQuayY + 0.08f, zc),
                new Vector3(1.62f, 0.24f, zl), stoneDark);

            // 岸壁面上的凸出条石：砌筑的起伏
            for (int i = 0; i < 15; i++)
            {
                float z = QzQuayZ0 + 6f + i * ((zl - 12f) / 14f);
                Box("QuayBlock_" + i, g.transform,
                    new Vector3(-0.20f, QzQuayY - 0.52f - (i % 3) * 0.66f, z),
                    new Vector3(0.26f, 0.56f, 1.0f + (i % 4) * 0.38f),
                    i % 2 == 0 ? stone : stoneDark, false);
            }

            // 下水石阶：靠南侧一段，退潮时能踩到
            for (int s = 0; s < 4; s++)
                Box("QuayStep_" + s, g.transform,
                    new Vector3(-0.34f - s * 0.86f, QzQuayY - 0.36f - s * 0.46f, -30f),
                    new Vector3(0.88f, 0.46f, 3.2f), stoneDark);

            // 系缆桩：等距布置，部分保留缆绳
            int idx = 0;
            for (float z = -57f; z <= 73f; z += 11f, idx++)
            {
                Cyl("Bollard_" + idx, g.transform, new Vector3(1.45f, QzQuayY + 0.40f, z),
                    0.21f, 0.80f, stoneDark, true);
                Cyl("BollardCap_" + idx, g.transform, new Vector3(1.45f, QzQuayY + 0.83f, z),
                    0.245f, 0.09f, stone);
                // 缆绳长期摩擦留下的深色磨痕
                Cyl("BollardWear_" + idx, g.transform, new Vector3(1.45f, QzQuayY + 0.62f, z),
                    0.208f, 0.14f, wet);
                if (idx % 3 == 1)
                    Cyl("BollardCoil_" + idx, g.transform, new Vector3(1.45f, QzQuayY + 0.26f, z),
                        0.30f, 0.11f, rope);
            }

            // 泊船的缆：前后各带两根，把船压在泊位里
            QzRope(g.transform, "MoorLine_Head", new Vector3(1.45f, QzQuayY + 0.66f, -13f),
                new Vector3(-2.55f, 2.58f, -19.2f), 0.055f, rope);
            QzRope(g.transform, "MoorLine_Stern", new Vector3(1.45f, QzQuayY + 0.66f, -2f),
                new Vector3(-2.55f, 2.72f, 2.4f), 0.055f, rope);
            QzRope(g.transform, "MoorLine_Spring", new Vector3(1.45f, QzQuayY + 0.60f, -13f),
                new Vector3(-2.55f, 2.10f, -21.4f), 0.05f, rope);
            QzRope(g.transform, "MoorLine_Spring2", new Vector3(1.45f, QzQuayY + 0.60f, -2f),
                new Vector3(-2.55f, 2.05f, 4.6f), 0.05f, rope);

            // 散放的缆绳盘
            for (int i = 0; i < 3; i++)
                Cyl("RopeCoil_" + i, g.transform,
                    new Vector3(2.6f + i * 0.9f, QzQuayY + 0.09f, 8f + i * 1.1f),
                    0.44f, 0.18f, rope);

            // 货棚：四柱、瓦顶，檐口压得很低，挡南方的斜雨
            var shed = Empty("QuayShed", g.transform, new Vector3(6.2f, QzQuayY, -30f));
            for (int i = 0; i < 4; i++)
            {
                float px = (i % 2 == 0) ? -1.7f : 1.7f;
                float pz = (i < 2) ? -2.6f : 2.6f;
                Cyl("ShedPost_" + i, shed.transform, new Vector3(px, 1.45f, pz), 0.13f, 2.9f, wood, true);
            }
            Box("ShedBeamX", shed.transform, new Vector3(0f, 2.90f, -2.6f), new Vector3(3.9f, 0.20f, 0.18f), wood);
            Box("ShedBeamX2", shed.transform, new Vector3(0f, 2.90f, 2.6f), new Vector3(3.9f, 0.20f, 0.18f), wood);
            MeshObject("ShedRoof", shed.transform, new Vector3(0f, 2.96f, 0f),
                MeshFactory.GableRoof(4.6f, 6.2f, 1.05f, 0.55f), tile, false);
            MeshObject("ShedRoofRidge", shed.transform, new Vector3(0f, 4.02f, 0f),
                MeshFactory.GableRoof(0.30f, 6.3f, 0.16f, 0.10f), stoneDark, false);

            // 棚下：几条长凳与待运的陶罐
            Box("ShedBench", shed.transform, new Vector3(-1.2f, 0.52f, 0.2f), new Vector3(0.5f, 0.10f, 2.8f), woodPale);
            for (int i = 0; i < 5; i++)
                Cyl("ShedJar_" + i, shed.transform,
                    new Vector3(0.9f + (i % 2) * 0.55f, 0.30f + (i / 2) * 0.02f, -1.6f + i * 0.78f),
                    0.20f, 0.60f, i % 2 == 0 ? pottery : potteryDark, false);

            // 一盏风灯：挂在棚柱上
            Cyl("ShedLanternPost", shed.transform, new Vector3(1.7f, 2.1f, -2.6f), 0.05f, 1.0f, wood);
            Ball("ShedLantern", shed.transform, new Vector3(1.7f, 1.58f, -2.6f), 0.16f, lantern);
            var ll = new GameObject("ShedLanternLight", typeof(Light));
            ll.transform.SetParent(shed.transform, false);
            ll.transform.localPosition = new Vector3(1.7f, 1.55f, -2.6f);
            var lg = ll.GetComponent<Light>();
            lg.type = LightType.Point;
            lg.color = Lin(new Color(1f, 0.80f, 0.55f));
            lg.range = 7f;
            lg.intensity = 1.15f;
            lg.cullingMask &= ~(1 << WaterLayer);

            // 南端的石堆：卸下来还没运走的荒料
            for (int i = 0; i < 8; i++)
                Box("QuayStonePile_" + i, g.transform,
                    new Vector3(0.3f + Hash01(i * 5) * 1.5f, QzQuayY + 0.12f + (i % 3) * 0.24f, -47f + (i / 3) * 1.4f - Hash01(i * 7) * 0.4f),
                    new Vector3(0.84f + Hash01(i * 11) * 0.3f, 0.34f, 0.62f + Hash01(i * 13) * 0.22f),
                    i % 2 == 0 ? stonePale : stone, false);
        }

        // ================================================================ 栈桥

        static void QzPier(Transform parent, Material wood, Material woodPale, Material stone,
                           Material rope, Material plankNew, Material pottery, Material sack, Material lantern)
        {
            var g = Empty("Pier", parent, Vector3.zero);
            float len = QzQuayX1 - QzPierX;
            float cx = (QzQuayX1 + QzPierX) * 0.5f;

            // 桥面：厚板横铺，板缝以窄条压出
            Box("PierDeck", g.transform, new Vector3(cx, QzPierY - 0.05f, QzPierZ),
                new Vector3(len, 0.24f, 3.6f), woodPale);
            for (float x = QzPierX + 0.7f; x < QzQuayX1 - 0.4f; x += 0.92f)
                Box("PierPlankLine", g.transform, new Vector3(x, QzPierY + 0.075f, QzPierZ),
                    new Vector3(0.07f, 0.035f, 3.5f), wood, false);

            // 木桩：成对打入淤泥，水线一段更深色
            for (float x = QzPierX + 1.2f; x <= QzQuayX1 - 0.8f; x += 2.2f)
                for (int s = -1; s <= 1; s += 2)
                {
                    Cyl("PierPile", g.transform, new Vector3(x, QzPierY - 1.60f, QzPierZ + s * 1.56f),
                        0.16f, 3.30f, wood, true);
                    Cyl("PierPileWet", g.transform, new Vector3(x, -0.55f, QzPierZ + s * 1.56f),
                        0.176f, 1.10f, wood, false);
                }

            // 桥下横梁
            for (float x = QzPierX + 2.0f; x <= QzQuayX1 - 1.0f; x += 4.4f)
                Box("PierBeam", g.transform, new Vector3(x, QzPierY - 0.32f, QzPierZ),
                    new Vector3(0.28f, 0.24f, 3.5f), wood, false);

            // 北侧栏杆；南侧留空以便靠船与系缆
            for (float x = QzPierX + 2.2f; x < QzQuayX1 - 1.2f; x += 2.4f)
                Cyl("PierRailPost", g.transform, new Vector3(x, QzPierY + 0.62f, QzPierZ - 1.56f),
                    0.055f, 1.25f, wood);
            Box("PierRailTop", g.transform, new Vector3(cx, QzPierY + 1.18f, QzPierZ - 1.56f),
                new Vector3(len - 3.6f, 0.09f, 0.10f), wood);
            Box("PierRailMid", g.transform, new Vector3(cx, QzPierY + 0.62f, QzPierZ - 1.56f),
                new Vector3(len - 3.6f, 0.06f, 0.08f), wood, false);

            // 端头平台与系缆柱
            Box("PierHeadDeck", g.transform, new Vector3(QzPierX, QzPierY - 0.05f, QzPierZ),
                new Vector3(5.0f, 0.26f, 5.0f), woodPale);
            for (int s = -1; s <= 1; s += 2)
            {
                Cyl("PierHeadBollard", g.transform,
                    new Vector3(QzPierX - 1.7f, QzPierY + 0.34f, QzPierZ + s * 1.9f),
                    0.20f, 0.70f, stone, true);
                Cyl("PierHeadBollardCap", g.transform,
                    new Vector3(QzPierX - 1.7f, QzPierY + 0.72f, QzPierZ + s * 1.9f),
                    0.235f, 0.09f, wood);
            }

            // 端头灯笼柱
            Cyl("PierLanternPost", g.transform, new Vector3(QzPierX + 1.4f, QzPierY + 1.10f, QzPierZ + 1.6f),
                0.09f, 2.20f, wood, true);
            Box("PierLanternArm", g.transform, new Vector3(QzPierX + 0.85f, QzPierY + 2.14f, QzPierZ + 1.6f),
                new Vector3(1.20f, 0.10f, 0.10f), wood, false);
            Ball("PierLantern", g.transform, new Vector3(QzPierX + 0.32f, QzPierY + 2.06f, QzPierZ + 1.6f),
                0.19f, lantern);
            var pl = new GameObject("PierLanternLight", typeof(Light));
            pl.transform.SetParent(g.transform, false);
            pl.transform.localPosition = new Vector3(QzPierX + 0.32f, QzPierY + 2.00f, QzPierZ + 1.6f);
            var plg = pl.GetComponent<Light>();
           
            plg.type = LightType.Point;
            plg.color = Lin(new Color(1f, 0.78f, 0.52f));
            plg.range = 9f;
            plg.intensity = 1.25f;
            plg.cullingMask &= ~(1 << WaterLayer);

            // 桥头堆货：待装船的陶罐与麻袋
            for (int i = 0; i < 6; i++)
                Cyl("PierJar_" + i, g.transform,
                    new Vector3(QzPierX + 3.6f + (i % 3) * 0.62f, QzPierY + 0.24f, QzPierZ - 1.2f + (i / 3) * 0.80f),
                    0.22f, 0.48f, pottery, true);
            for (int i = 0; i < 4; i++)
                Box("PierSack_" + i, g.transform,
                    new Vector3(QzPierX + 5.4f, QzPierY + 0.30f + (i % 2) * 0.44f, QzPierZ - 0.9f + i * 0.62f),
                    new Vector3(0.62f, 0.44f, 0.50f), sack, true);

            // 靠在桥边的小舢板
            var boat = Empty("Sampan", g.transform, new Vector3(QzPierX + 6.5f, QzWaterY + 0.30f, QzPierZ + 3.4f));
            boat.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);
            MeshObject("SampanHull", boat.transform, Vector3.zero,
                QzHullMesh(6.4f, 1.9f, 0.72f, 14, 12, false, 0f, false), wood, true);
            Box("SampanTransom", boat.transform, new Vector3(3.15f, 0.05f, 0f), new Vector3(0.10f, 0.72f, 1.42f), wood);
            Box("SampanDeck", boat.transform, new Vector3(0.4f, 0.30f, 0f), new Vector3(3.6f, 0.09f, 1.44f), plankNew);
            MeshObject("SampanCanopy", boat.transform, new Vector3(1.1f, 0.62f, 0f),
                MeshFactory.CurvedRoof(0.90f, 1.30f, 0.46f, 0.10f, 0.06f, 10, 8), stone, false);
            QzRope(g.transform, "SampanPainter", new Vector3(QzPierX + 2.0f, QzPierY + 0.2f, QzPierZ + 1.2f),
                new Vector3(QzPierX + 9.4f, QzWaterY + 0.55f, QzPierZ + 3.9f), 0.035f, rope);
        }

        // ================================================================ 船体放样

        /// <summary>站号 t（0 艏 / 1 艉）处的半宽：艏部尖、中段最宽、艉部保留平尾。</summary>
        static float QzHalfBeam(float t, float beam)
        {
            float hb = beam * 0.5f;
            if (t < 0.58f) return hb * Mathf.Sin(Mathf.PI * 0.5f * Mathf.Pow(Mathf.Clamp01(t / 0.58f), 0.62f));
            return hb * Mathf.Lerp(1f, 0.80f, Mathf.Pow(Mathf.Clamp01((t - 0.58f) / 0.42f), 1.25f));
        }

        /// <summary>站号处的吃水深度：艏艉起翘，中段最深。</summary>
        static float QzDepthAt(float t, float depth)
        {
            return depth * (0.55f + 0.45f * Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(t), 0.55f)));
        }

        /// <summary>站号处的舷顶高度：艏高艉中，中段最低，是福船的舷弧。</summary>
        static float QzSheerAt(float t, float depth)
        {
            return depth * (0.34f + 0.30f * Mathf.Pow(1f - t, 1.6f) +
                            0.20f * Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 2.2f));
        }

        /// <summary>
        /// 放样船体：沿 X 逐站推进，每站在 Z-Y 平面画一条从舷顶经龙骨到另一舷的弧。
        /// half=true 只造 z≥0 的一侧（干坞半剖）；inset 收进壁厚；inward=true 翻面做内壳。
        /// </summary>
        static Mesh QzHullMesh(float length, float beam, float depth, int stations, int sections,
                               bool half, float inset, bool inward)
        {
            int kFrom = half ? sections / 2 : 0;
            int cols = sections - kFrom + 1;
            int rows = stations + 1;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int i = 0; i < rows; i++)
            {
                float t = (float)i / (rows - 1);
                float hb = Mathf.Max(0.03f, QzHalfBeam(t, beam) - inset);
                float dp = Mathf.Max(0.06f, QzDepthAt(t, depth) - inset);
                float tp = QzSheerAt(t, depth) - inset * 0.4f;
                float x = -length * 0.5f + length * t;
                for (int k = kFrom; k <= sections; k++)
                {
                    float u = (float)k / sections;
                    float a = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, u);
                    float sa = Mathf.Sin(a);
                    float z = hb * sa;
                    float y = -dp * Mathf.Cos(a) + tp * Mathf.Pow(Mathf.Abs(sa), 3f);
                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(t * length * 0.16f, u * beam * 0.22f));
                }
            }

            // 船壳一律双面。反面必须用独立顶点，否则 RecalculateNormals 会把正反两套
            // 三角形平均成接近零的法线，渲染出来是一块块焦黑。
            int front = verts.Count;
            for (int v = 0; v < front; v++) { verts.Add(verts[v]); uvs.Add(uvs[v]); }

            for (int i = 0; i < rows - 1; i++)
                for (int k = 0; k < cols - 1; k++)
                {
                    int a0 = i * cols + k, b0 = (i + 1) * cols + k;
                    int c0 = (i + 1) * cols + k + 1, d0 = i * cols + k + 1;
                    tris.Add(a0); tris.Add(b0); tris.Add(c0);
                    tris.Add(a0); tris.Add(c0); tris.Add(d0);
                    tris.Add(front + a0); tris.Add(front + c0); tris.Add(front + b0);
                    tris.Add(front + a0); tris.Add(front + d0); tris.Add(front + c0);
                }

            var m = new Mesh { name = "QzHull" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateTangents();
            return m;
        }

        // ================================================================ 泊船：福船

        static void QzShip(Transform parent, Material hull, Material hullInner, Material woodPale,
                           Material plankNew, Material sail, Material rope, Material tile, Material lantern)
        {
            const float L = 26f, B = 8f, D = 3.2f;
            float deckY = D * 0.30f;

            var ship = Empty("Ship", parent, new Vector3(QzShipX, QzWaterY + 0.90f, QzShipZ));
            ship.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // 双层船壳：外壳承力，内壳让敞开的甲板口看得出厚度
            MeshObject("HullOuter", ship.transform, Vector3.zero,
                QzHullMesh(L, B, D, 24, 18, false, 0f, false), hull, true);
            MeshObject("HullInner", ship.transform, Vector3.zero,
                QzHullMesh(L, B, D, 24, 18, false, 0.12f, true), hullInner, false);
            Box("Transom", ship.transform, new Vector3(L * 0.5f - 0.04f, deckY * 0.4f, 0f),
                new Vector3(0.12f, D * 0.62f, B * 0.78f), hull);

            // 甲板：逐段跟随船宽收放，不用一整块箱子冒充
            const int segs = 14;
            for (int i = 0; i < segs; i++)
            {
                float tm = (i + 0.5f) / segs;
                float hb = Mathf.Max(0.10f, QzHalfBeam(tm, B) - 0.14f);
                float x = -L * 0.5f + L * tm;
                Box("DeckSeg_" + i, ship.transform, new Vector3(x, deckY, 0f),
                    new Vector3(L / segs + 0.02f, 0.11f, hb * 2f), plankNew, false);
                // 舷墙
                for (int s = -1; s <= 1; s += 2)
                    Box("Bulwark_" + i + "_" + s, ship.transform,
                        new Vector3(x, deckY + 0.26f, s * (hb - 0.05f)),
                        new Vector3(L / segs + 0.02f, 0.52f, 0.13f), hull, false);
            }

            // 甲板构件：桅座、货舱口、篾篷
            Box("Hatch", ship.transform, new Vector3(-1.2f, deckY + 0.16f, 0f),
                new Vector3(4.6f, 0.22f, 3.4f), woodPale);
            Box("HatchCover", ship.transform, new Vector3(-1.2f, deckY + 0.30f, 0f),
                new Vector3(4.4f, 0.10f, 3.2f), hull, false);

            MeshObject("MidCanopy", ship.transform, new Vector3(-4.6f, deckY + 1.42f, 0f),
                MeshFactory.CurvedRoof(1.70f, 3.10f, 0.72f, 0.18f, 0.09f, 14, 12), sail, false);
            for (int i = 0; i < 4; i++)
            {
                float px = -4.6f + ((i % 2 == 0) ? -2.6f : 2.6f);
                float pz = (i < 2) ? -1.5f : 1.5f;
                Cyl("CanopyPost_" + i, ship.transform, new Vector3(px, deckY + 0.70f, pz), 0.07f, 1.40f, woodPale);
            }

            // 艉楼：住舱，出檐很深
            float sternX = L * 0.32f;
            Box("PoopCabin", ship.transform, new Vector3(sternX, deckY + 0.78f, 0f),
                new Vector3(6.4f, 1.56f, 5.4f), hull);
            Box("PoopRail", ship.transform, new Vector3(sternX, deckY + 1.60f, 0f),
                new Vector3(6.8f, 0.14f, 5.8f), woodPale, false);
            MeshObject("PoopRoof", ship.transform, new Vector3(sternX, deckY + 1.70f, 0f),
                MeshFactory.CurvedRoof(3.20f, 3.90f, 1.10f, 0.46f, 0.13f, 16, 14), tile, false);
            for (int s = -1; s <= 1; s += 2)
                MeshObject("PoopRoofEave_" + s, ship.transform, new Vector3(sternX, deckY + 1.66f, s * 3.9f),
                    MeshFactory.CurvedRoof(3.30f, 0.42f, 0.42f, 0.14f, 0.10f, 16, 4), tile, false);

            // 艉部窗与灯笼
            for (int i = 0; i < 3; i++)
                Box("PoopWin_" + i, ship.transform,
                    new Vector3(sternX + 3.22f, deckY + 0.92f, -1.6f + i * 1.6f),
                    new Vector3(0.06f, 0.68f, 0.82f), lantern, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Ball("SternLantern_" + s, ship.transform,
                    new Vector3(sternX + 3.6f, deckY + 1.20f, s * 2.5f), 0.20f, lantern);
                var sl = new GameObject("SternLanternLight_" + s, typeof(Light));
                sl.transform.SetParent(ship.transform, false);
                sl.transform.localPosition = new Vector3(sternX + 3.6f, deckY + 1.15f, s * 2.5f);
                var slg = sl.GetComponent<Light>();
                slg.type = LightType.Point;
                slg.color = Lin(new Color(1f, 0.76f, 0.48f));
                slg.range = 11f;
                slg.intensity = 1.35f;
                slg.cullingMask &= ~(1 << WaterLayer);
            }

            // 双桅：桅杆立着，帆收起横绑在帆桁上
            float[] masts = { L * 0.10f, -L * 0.22f };
            float[] mh = { 17.5f, 14.5f };
            for (int m = 0; m < 2; m++)
            {
                float mx = masts[m], height = mh[m];
                Cyl("Mast_" + m, ship.transform, new Vector3(mx, deckY + height * 0.5f, 0f),
                    0.17f, height, woodPale, true);
                Cyl("MastFoot_" + m, ship.transform, new Vector3(mx, deckY + 0.30f, 0f),
                    0.32f, 0.60f, hull);

                float yardY = deckY + height * 0.68f;
                Cyl("Yard_" + m, ship.transform, new Vector3(mx, yardY, 0f), 0.09f, B * 0.86f, woodPale)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                // 帆收在帆桁上：半径细、长度横跨船宽。参数是 (半径, 高)，写反会变成一片竖直圆盘
                Cyl("FurledSail_" + m, ship.transform, new Vector3(mx, yardY - 0.24f, 0f),
                    0.34f, B * 0.62f, sail).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Cyl("FurledSail2_" + m, ship.transform, new Vector3(mx, yardY - 0.60f, 0f),
                    0.27f, B * 0.50f, sail).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                // 索具：桅顶到艏、艉与两舷
                float topY = deckY + height;
                QzRope(ship.transform, "Stay_F" + m, new Vector3(mx, topY, 0f),
                    new Vector3(-L * 0.48f, deckY + 1.10f, 0f), 0.032f, rope);
                QzRope(ship.transform, "Stay_A" + m, new Vector3(mx, topY, 0f),
                    new Vector3(L * 0.47f, deckY + 0.60f, 0f), 0.032f, rope);
                for (int s = -1; s <= 1; s += 2)
                {
                    QzRope(ship.transform, "Shroud_" + m + "_" + s, new Vector3(mx, topY, 0f),
                        new Vector3(mx + 1.4f, deckY + 0.55f, s * (QzHalfBeam(0.5f + mx / L, B) - 0.2f)),
                        0.028f, rope);
                    QzRope(ship.transform, "Shroud2_" + m + "_" + s, new Vector3(mx, topY - 2.4f, 0f),
                        new Vector3(mx - 2.2f, deckY + 0.55f, s * (QzHalfBeam(0.5f + mx / L, B) - 0.2f)),
                        0.026f, rope);
                }
            }

            // 舵与石锚
            Box("Rudder", ship.transform, new Vector3(L * 0.5f + 0.22f, -0.35f, 0f),
                new Vector3(0.18f, 2.30f, 0.95f), hull);
            Cyl("RudderPost", ship.transform, new Vector3(L * 0.5f + 0.14f, 0.60f, 0f), 0.12f, 2.60f, woodPale);

            Box("AnchorStock", ship.transform, new Vector3(-L * 0.46f, deckY + 0.20f, 1.5f),
                new Vector3(1.10f, 0.30f, 0.32f), woodPale);
            for (int s = -1; s <= 1; s += 2)
                Box("AnchorFluke_" + s, ship.transform,
                    new Vector3(-L * 0.46f, deckY - 0.06f, 1.5f + s * 0.58f),
                    new Vector3(0.28f, 0.52f, 0.30f), woodPale, false);

            // 舷侧挂的护舷木与绳圈
            for (int i = 0; i < 5; i++)
            {
                float x = -L * 0.34f + i * (L * 0.17f);
                Cyl("Fender_" + i, ship.transform,
                    new Vector3(x, deckY - 0.30f, -(QzHalfBeam(0.5f + x / L, B) + 0.06f)),
                    0.10f, 1.10f, woodPale).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        // ================================================================ 干坞：水密隔舱剖面

        static void QzDryDock(Transform parent, Material stone, Material stonePale, Material stoneDark,
                              Material wood, Material woodPale, Material hull, Material hullInner,
                              Material plankNew, Material rope)
        {
            const float L = 24f, B = 7.4f, D = 3.0f;
            const float gy = QzQuayY - 0.10f;

            var g = Empty("DryDock", parent, new Vector3(QzDockX, gy, QzDockZ));

            // 场院：碎石铺地，四周用矮石墙围出边界
            Box("DockYard", g.transform, new Vector3(0f, -0.16f, 0f), new Vector3(23f, 0.32f, 41f), stoneDark);
            // 坞底铺石：一整块灰板会被读成水泥地，必须有分缝与深浅
            {
                const float sw = 1.28f, sl = 1.05f, gap = 0.05f;
                int nx = Mathf.FloorToInt(22.6f / sw), nz = Mathf.FloorToInt(40.4f / sl);
                for (int iz = 0; iz < nz; iz++)
                    for (int ix = 0; ix < nx; ix++)
                    {
                        float px = -11.3f + sw * (ix + 0.5f);
                        float pz = -20.2f + sl * (iz + 0.5f) + (ix % 2) * 0.12f;
                        float hgt = 0.16f + Hash01(ix * 149 + iz * 23) * 0.03f;
                        var pm = Hash01(ix * 83 + iz * 47) > 0.94f ? stonePale : stone;
                        Box("DockSlab_" + iz + "_" + ix, g.transform,
                            new Vector3(px, 0.02f - hgt * 0.5f, pz),
                            new Vector3(sw - gap, hgt, sl - gap), pm, false);
                    }
            }
            Box("DockWallE", g.transform, new Vector3(11.1f, 0.55f, 0f), new Vector3(0.55f, 1.10f, 41f), stone);
            for (int s = -1; s <= 1; s += 2)
                Box("DockWallNS_" + s, g.transform, new Vector3(0f, 0.45f, s * 20.4f),
                    new Vector3(23f, 0.90f, 0.50f), stone);
            Box("DockCoping", g.transform, new Vector3(11.1f, 1.14f, 0f), new Vector3(0.72f, 0.14f, 41f), stoneDark);

            // 入口：从码头这边进来的缺口，铺斜石
            Box("DockRamp", g.transform, new Vector3(-11.4f, 0.02f, -6f), new Vector3(3.0f, 0.24f, 6.0f), stoneDark);

            // 船台横梁：把船体架离地面
            for (int i = 0; i < 7; i++)
            {
                Box("Cradle_" + i, g.transform, new Vector3(B * 0.46f, 0.30f, -L * 0.5f + 0.6f + i * (L - 1.2f) / 6f),
                    new Vector3(B * 0.92f, 0.34f, 0.62f), wood);
                for (int s = -1; s <= 1; s += 2)
                    Box("CradleWedge_" + i + "_" + s, g.transform,
                        new Vector3(B * 0.40f + s * 0.20f, 0.60f, -L * 0.5f + 0.6f + i * (L - 1.2f) / 6f),
                        new Vector3(0.34f, 0.30f, 0.56f), woodPale);
            }

            // 半剖船体：外壳 + 内壳，剖口朝向码头
            var shipT = Empty("DockHull", g.transform, new Vector3(B * 0.50f, 0.52f, 0f));
            shipT.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            // 完整船壳但不铺甲板：从舷边看进去就是一排隔舱板，这才是「水密隔舱」要讲的东西
            MeshObject("DockHullOuter", shipT.transform, Vector3.zero,
                QzHullMesh(L, B, D, 22, 16, false, 0f, false), hull, false);
            MeshObject("DockHullInner", shipT.transform, Vector3.zero,
                QzHullMesh(L, B, D, 22, 16, false, 0.11f, true), hullInner, false);

            // 12 道隔舱板 = 13 个舱。板面横跨整个船宽，板顶落在舷口之下，
            // 站在船台边往里看就是一排「肋骨」——这才看得出水密隔舱。
            for (int i = 0; i < 12; i++)
            {
                float t = (i + 1f) / 13f;
                float hb = Mathf.Max(0.30f, QzHalfBeam(t, B) - 0.16f);
                float dp = QzDepthAt(t, D);
                float tp = QzSheerAt(t, D);
                float hgt = (tp + dp) * 0.80f;
                // 板顶探出舷口 0.5 m：拆掉舷侧上段之后，骨架从船体里露出来，
                // 平视也能看出「这是一条正在修的船」，而不是一排封死的立板。
                float yc = tp + 0.50f - hgt * 0.5f;
                Box("Bulkhead_" + i, shipT.transform,
                    new Vector3(-L * 0.5f + L * t, yc, 0f),
                    new Vector3(0.14f, hgt, hb * 2f), wood);
                // 隔舱板与壳板的接头：板端外露的小榫
                for (int s = -1; s <= 1; s += 2)
                    Box("BulkheadTenon_" + i + "_" + s, shipT.transform,
                        new Vector3(-L * 0.5f + L * t, yc + hgt * 0.32f, s * (hb + 0.03f)),
                        new Vector3(0.24f, 0.15f, 0.13f), wood);
            }

            // 敞甲板：掀掉的面板只零星补回几块，其余露出舱口
            for (int i = 0; i < 6; i++)
            {
                float t = 0.10f + i * 0.16f;
                if (i % 3 == 1) continue;
                float tp = QzSheerAt(t, D);
                float hb = QzHalfBeam(t, B);
                Box("DeckBeam_" + i, shipT.transform,
                    new Vector3(-L * 0.5f + L * t, tp + 0.02f, 0f),
                    new Vector3(0.16f, 0.12f, hb * 2f), wood);
                Box("DeckPlank_" + i, shipT.transform,
                    new Vector3(-L * 0.5f + L * (t + 0.075f), tp + 0.07f, hb * 0.45f),
                    new Vector3(L * 0.14f, 0.07f, hb * 0.85f), plankNew);
            }

            // 肋骨：隔舱之间的竖向板条，贴着舷侧内壁
            for (int i = 0; i < 12; i++)
            {
                float t = (i + 0.5f) / 12f;
                float hb = Mathf.Max(0.30f, QzHalfBeam(t, B) - 0.24f);
                float dp = QzDepthAt(t, D);
                float tp = QzSheerAt(t, D);
                float hgt = (tp + dp) * 0.70f;
                for (int s = -1; s <= 1; s += 2)
                    Box("Frame_" + i + "_" + s, shipT.transform,
                        new Vector3(-L * 0.5f + L * t, tp - 0.20f - hgt * 0.5f, s * hb),
                        new Vector3(0.10f, hgt, 0.12f), wood);
            }

            // 支撑木：斜撑顶住舷侧
            for (int i = 0; i < 6; i++)
            {
                float t = 0.12f + i * 0.155f;
                float hb = QzHalfBeam(t, B);
                float tp = QzSheerAt(t, D);
                QzRope(g.transform, "Prop_" + i,
                    new Vector3(B * 0.52f + hb * 0.72f, 0.34f, -L * 0.5f + L * t),
                    new Vector3(B * 0.52f + hb * 0.10f, tp - 0.10f, -L * 0.5f + L * t),
                    0.09f, wood);
            }

            // 新板料与桐油桶：正在修船
            for (int i = 0; i < 8; i++)
                Box("PlankStack_" + i, g.transform,
                    new Vector3(5.2f + (i % 2) * 0.9f, 0.16f + (i / 2) * 0.17f, -18.6f - (i % 3) * 0.4f),
                    new Vector3(2.6f, 0.16f, 0.55f), i % 3 == 0 ? plankNew : woodPale, false);
            for (int i = 0; i < 3; i++)
            {
                Cyl("TungOil_" + i, g.transform, new Vector3(9.2f, 0.34f, 14.2f + i * 1.1f), 0.34f, 0.68f, wood, true);
                Cyl("TungOilBand_" + i, g.transform, new Vector3(9.2f, 0.34f, 14.2f + i * 1.1f), 0.355f, 0.12f, stoneDark);
            }

            // 竹脚手架：修船时贴着舷侧搭的，撑杆与被踩得发亮的横档
            for (int b = 0; b < 5; b++)
            {
                float zz = -9.5f + b * 4.2f;
                for (int s = -1; s <= 1; s += 2)
                    Cyl("Bamboo_" + b + "_" + s, g.transform,
                        new Vector3(B * 0.52f + 1.55f, 2.05f, zz + s * 1.35f), 0.055f, 4.1f, woodPale, true);
                for (int r = 0; r < 3; r++)
                    Box("BambooTread_" + b + "_" + r, g.transform,
                        new Vector3(B * 0.52f + 1.55f, 0.85f + r * 1.25f, zz),
                        new Vector3(1.30f, 0.09f, 2.85f), r == 1 ? plankNew : woodPale, false);
                Cyl("BambooTie_" + b, g.transform,
                    new Vector3(B * 0.52f + 1.55f, 3.30f, zz), 0.045f, 2.75f, rope, false);
            }

            // 龙骨大料与待装的肋骨料：架在两只木马上
            Box("KeelBeam", g.transform, new Vector3(-6.2f, 0.72f, -2.4f), new Vector3(0.44f, 0.44f, 9.4f), wood);
            for (int i = 0; i < 5; i++)
            {
                float zz = -6.2f + i * 2.0f;
                for (int s = -1; s <= 1; s += 2)
                    Box("KeelHorse_" + i + "_" + s, g.transform,
                        new Vector3(-6.2f, 0.34f, zz + s * 0.34f), new Vector3(0.72f, 0.68f, 0.20f), woodPale, false);
            }
            for (int i = 0; i < 7; i++)
                Box("RibBlank_" + i, g.transform,
                    new Vector3(-8.4f + (i % 2) * 0.34f, 0.13f + (i / 2) * 0.20f, 6.2f + (i % 4) * 0.30f),
                    new Vector3(0.28f, 0.19f, 3.1f), i % 3 == 0 ? plankNew : wood, false);

            // 刨花与木屑：绕在船台周围，地面才不空
            for (int i = 0; i < 26; i++)
            {
                float a = Hash01(i * 37) * 6.283f;
                float r = 1.4f + Hash01(i * 53) * 6.4f;
                Ball("Shaving_" + i, g.transform,
                    new Vector3(B * 0.52f + Mathf.Cos(a) * r, 0.06f, Mathf.Sin(a) * r * 1.5f),
                    0.14f + Hash01(i * 91) * 0.18f, woodPale).transform.localScale =
                    new Vector3(1f, 0.28f, 1f);
            }

            // 石墩与缆绳盘：坞口总是堆着这些
            for (int i = 0; i < 4; i++)
                Cyl("MooringBollard_" + i, g.transform,
                    new Vector3(-9.2f, 0.30f, -9.0f + i * 4.6f), 0.24f, 0.60f, stoneDark, true);
            for (int i = 0; i < 3; i++)
            {
                Cyl("RopeCoil_" + i, g.transform,
                    new Vector3(-7.4f + i * 1.1f, 0.10f + i * 0.02f, 11.0f - i * 0.8f),
                    0.46f - i * 0.05f, 0.16f, rope, true);
                Cyl("RopeCoilIn_" + i, g.transform,
                    new Vector3(-7.4f + i * 1.1f, 0.20f + i * 0.02f, 11.0f - i * 0.8f),
                    0.24f - i * 0.03f, 0.10f, rope, false);
            }

            // 楷书说明牌：立在入口内侧
            CreateTextPlate(g.transform, "水 密 隔 舱", new Vector3(-10.04f, 1.42f, 4.2f),
                new Vector3(0f, -90f, 0f), new Vector2(1.90f, 0.44f), 150, new Color(0.16f, 0.15f, 0.13f));
            CreateTextPlate(g.transform, "十 三 舱", new Vector3(-10.04f, 0.80f, 4.2f),
                new Vector3(0f, -90f, 0f), new Vector2(1.30f, 0.34f), 105, new Color(0.24f, 0.22f, 0.18f));
            Cyl("SignPost", g.transform, new Vector3(-10.12f, 0.70f, 4.2f), 0.07f, 1.40f, wood, true);
        }

        /// <summary>竖排文字：逐字下落，用于碑额与匾。汉字碑刻的传统排法。</summary>
        /// 字号按「字本体的高度」给，不再让调用点猜 fontSize：CreateTextPlate 的 500 单位画布
        /// 决定了字只占牌面的 fontSize/500，直接传 fontSize 会做出远小于预期的字。
        static void QzVerticalText(Transform parent, string text, Vector3 pos, Vector3 euler,
                                   float plateMeters, float glyphMeters, Color color, float stepY)
        {
            int fontSize = Mathf.RoundToInt(glyphMeters / Mathf.Max(0.01f, plateMeters) * 500f);
            for (int i = 0; i < text.Length; i++)
                CreateTextPlate(parent, text[i].ToString(),
                    new Vector3(pos.x, pos.y - i * stepY, pos.z), euler,
                    new Vector2(plateMeters, plateMeters), fontSize, color);
        }

        // ================================================================ 货场

        static void QzCargo(Transform parent, Material wood, Material woodPale, Material pottery,
                            Material potteryDark, Material celadon, Material spice, Material sack,
                            Material rope, Material tile)
        {
            var g = Empty("Cargo", parent, new Vector3(QzCargoX, QzQuayY, QzCargoZ));

            // 货台
            Box("CargoPlatform", g.transform, new Vector3(0f, 0.16f, 0f), new Vector3(11f, 0.32f, 15f), woodPale);

            // 香料袋：麻袋不是方块，压出塌陷的顶
            for (int i = 0; i < 16; i++)
            {
                float x = -3.4f + (i % 4) * 1.15f;
                float z = -4.6f + (i / 4) * 1.35f;
                float squash = 0.34f + Hash01(i * 7) * 0.12f;
                var s = Box("SpiceSack_" + i, g.transform,
                    new Vector3(x, 0.32f + squash * 0.5f, z),
                    new Vector3(0.86f, squash, 0.66f), i % 3 == 0 ? spice : sack, true);
                s.transform.localRotation = Quaternion.Euler(
                    (Hash01(i * 11) - 0.5f) * 7f, Hash01(i * 13) * 40f, (Hash01(i * 17) - 0.5f) * 7f);
                Ball("SpiceSackTop_" + i, g.transform,
                    new Vector3(x + (Hash01(i * 19) - 0.5f) * 0.2f, 0.32f + squash, z),
                    0.34f, i % 3 == 0 ? spice : sack).transform.localScale = new Vector3(0.86f, 0.30f, 0.66f);
                // 袋口的捆绳
                Cyl("SackTie_" + i, g.transform,
                    new Vector3(x - 0.44f, 0.32f + squash * 0.55f, z), 0.12f, 0.10f, rope)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // 陶罐：大小不一，口沿加一圈深色
            for (int i = 0; i < 10; i++)
            {
                float r = 0.24f + Hash01(i * 5) * 0.16f;
                float h = 0.52f + Hash01(i * 9) * 0.42f;
                var p = new Vector3(1.2f + (i % 5) * 0.72f, 0.32f + h * 0.5f, 1.8f + (i / 5) * 0.95f);
                Cyl("Jar_" + i, g.transform, p, r, h, i % 2 == 0 ? pottery : potteryDark, true);
                Cyl("JarLip_" + i, g.transform, new Vector3(p.x, p.y + h * 0.5f, p.z), r * 1.06f, 0.07f, potteryDark);
                Cyl("JarMouth_" + i, g.transform, new Vector3(p.x, p.y + h * 0.5f - 0.02f, p.z), r * 0.62f, 0.06f, spice);
            }

            // 青瓷：成摞码放，层间垫草
            for (int stack = 0; stack < 3; stack++)
            {
                float bx = -1.6f + stack * 1.5f, bz = 4.4f;
                for (int i = 0; i < 6; i++)
                {
                    Cyl("Celadon_" + stack + "_" + i, g.transform,
                        new Vector3(bx + (Hash01(stack * 31 + i) - 0.5f) * 0.10f,
                            0.40f + i * 0.19f, bz + (Hash01(stack * 37 + i) - 0.5f) * 0.10f),
                        0.34f, 0.16f, celadon, i == 5);
                    if (i < 5)
                        Cyl("CeladonStraw_" + stack + "_" + i, g.transform,
                            new Vector3(bx, 0.33f + i * 0.19f, bz), 0.30f, 0.05f, spice);
                }
            }

            // 木箱与竹筐
            for (int i = 0; i < 5; i++)
                Box("Crate_" + i, g.transform,
                    new Vector3(-4.2f, 0.34f + (i % 2) * 0.62f, 2.2f + (i / 2) * 1.15f),
                    new Vector3(1.00f, 0.60f, 0.86f), wood, true);
            for (int i = 0; i < 4; i++)
                Cyl("Basket_" + i, g.transform,
                    new Vector3(-4.6f, 0.52f, -1.0f + i * 1.05f), 0.38f, 0.68f, sack, true);

            // 缆绳盘与杆秤
            Cyl("CargoRopeCoil", g.transform, new Vector3(4.2f, 0.42f, -5.4f), 0.52f, 0.20f, rope);
            Cyl("CargoRopeCoil2", g.transform, new Vector3(4.2f, 0.62f, -5.4f), 0.44f, 0.18f, rope);
            Cyl("SteelyardRod", g.transform, new Vector3(2.4f, 1.28f, -6.4f), 0.045f, 1.9f, wood)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 84f);
            Ball("SteelyardWeight", g.transform, new Vector3(3.3f, 0.96f, -6.4f), 0.14f, potteryDark);
            Cyl("SteelyardHook", g.transform, new Vector3(1.5f, 0.96f, -6.4f), 0.03f, 0.42f, woodPale);

            // 货棚的瓦檐：只做一片顶，暗示棚的边界，不做完整建筑
            for (int i = 0; i < 4; i++)
            {
                float px = -4.6f + (i % 2) * 9.2f;
                float pz = -6.4f + (i / 2) * 12.8f;
                Cyl("CargoPost_" + i, g.transform, new Vector3(px, 1.70f, pz), 0.14f, 3.4f, wood, true);
            }
            // 檐口封板与联系梁：让屋顶读成架起来的棚，而不是浮一片瓦
            for (int s = -1; s <= 1; s += 2)
            {
                Box("CargoFascia_" + s, g.transform, new Vector3(s * 5.72f, 3.34f, 0f),
                    new Vector3(0.20f, 0.30f, 15.0f), wood);
                Box("CargoBeam_" + s, g.transform, new Vector3(0f, 3.26f, s * 6.4f),
                    new Vector3(9.4f, 0.20f, 0.18f), wood);
            }
            MeshObject("CargoRoof", g.transform, new Vector3(0f, 3.40f, 0f),
                MeshFactory.GableRoof(10.4f, 14.2f, 1.50f, 0.55f), tile, false);
        }

        // ================================================================ 三教碑

        static void QzThreeTeachWall(Transform parent, Material stone, Material stonePale, Material stoneDark,
                                     Material tile, Material wood)
        {
            float gy = QuanzhouHeight(QzWallX, QzWallZ);
            var g = Empty("ThreeTeachWall", parent, new Vector3(QzWallX, gy, QzWallZ));

            // 台基与壁身：面朝南，正对从码头过来的路
            Box("WallBase", g.transform, new Vector3(0f, 0.18f, 0f), new Vector3(8.2f, 0.36f, 1.9f), stone);
            Box("WallBody", g.transform, new Vector3(0f, 1.86f, 0f), new Vector3(7.4f, 3.00f, 0.62f), stonePale);
            for (int s = -1; s <= 1; s += 2)
                Box("WallPier_" + s, g.transform, new Vector3(s * 3.55f, 1.86f, 0f),
                    new Vector3(0.55f, 3.20f, 0.80f), stone);
            MeshObject("WallEave", g.transform, new Vector3(0f, 3.36f, 0f),
                MeshFactory.CurvedRoof(4.30f, 0.85f, 0.46f, 0.16f, 0.11f, 18, 6), tile, false);

            // 三格碑面：三格各嵌一块深色碑板，外压浅色石框
            for (int i = 0; i < 3; i++)
            {
                float px = -2.35f + i * 2.35f;
                Box("PanelRecess_" + i, g.transform, new Vector3(px, 1.92f, -0.34f),
                    new Vector3(2.00f, 2.24f, 0.05f), stoneDark, false);
                Box("PanelFrame_" + i, g.transform, new Vector3(px, 1.92f, -0.365f),
                    new Vector3(1.84f, 2.08f, 0.03f), stonePale, false);
            }

            // 三格：儒、释、道各一大字，楷书
            string[] glyphs = { "儒", "釋", "道" };
            for (int i = 0; i < 3; i++)
            {
                float px = -2.35f + i * 2.35f;
                CreateTextPlate(g.transform, glyphs[i], new Vector3(px, 1.94f, -0.40f),
                    new Vector3(0f, 180f, 0f), new Vector2(1.30f, 1.30f), 500,
                    new Color(0.10f, 0.10f, 0.09f), title: true);
            }

            // 壁前的石香炉与供台
            Box("AltarTable", g.transform, new Vector3(0f, 0.62f, -1.75f), new Vector3(2.30f, 0.16f, 0.82f), stoneDark);
            for (int s = -1; s <= 1; s += 2)
                Box("AltarLeg_" + s, g.transform, new Vector3(s * 0.95f, 0.32f, -1.75f),
                    new Vector3(0.22f, 0.62f, 0.62f), wood);
            Cyl("IncenseBurner", g.transform, new Vector3(0f, 0.92f, -1.75f), 0.34f, 0.42f, stoneDark);
            Cyl("IncenseBurnerLip", g.transform, new Vector3(0f, 1.14f, -1.75f), 0.37f, 0.07f, stone);
            Ball("IncenseBurnerLid", g.transform, new Vector3(0f, 1.22f, -1.75f), 0.20f, stonePale);
            for (int i = 0; i < 3; i++)
                Cyl("IncenseStick_" + i, g.transform,
                    new Vector3(-0.10f + i * 0.10f, 1.40f, -1.75f), 0.008f, 0.34f, stone, false);

            // 侧边的两块残碑：倒在地上的断石，说明这里是一处遗存而不是新修景点
            Box("BrokenSteleA", g.transform, new Vector3(3.9f, 0.42f, -2.3f), new Vector3(1.10f, 0.28f, 0.62f), stonePale, false)
                .transform.localRotation = Quaternion.Euler(0f, 22f, 6f);
            Box("BrokenSteleB", g.transform, new Vector3(-3.9f, 0.30f, -2.6f), new Vector3(0.90f, 0.22f, 0.55f), stoneDark, false)
                .transform.localRotation = Quaternion.Euler(0f, -34f, -4f);
        }

        // ================================================================ 祈风石刻

        static void QzPrayerStele(Transform parent, Material stonePale, Material stone, Material stoneDark,
                                  Material wood, Material lantern, Material moss)
        {
            float gy = QuanzhouHeight(QzSteleX, QzSteleZ);
            float deckWorldY = Mathf.Max(QuanzhouHeight(QzSteleX, QzSteleZ - 7.4f),
                                         QuanzhouHeight(QzSteleX, QzSteleZ - 4.0f)) + 0.16f;
            var g = Empty("PrayerStele", parent, new Vector3(QzSteleX, gy, QzSteleZ));
            g.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // 上山石阶：从山脚斜着上来，每级都贴地
            var steps = Empty("PrayerStairs", parent, Vector3.zero);
            float stX0 = 44.8f, stZ0 = 16.8f, stX1 = 50.6f, stZ1 = 24.2f;
            float stY0 = QuanzhouHeight(stX0, stZ0) + 0.16f;
            for (int i = 0; i < 22; i++)
            {
                float t = (i + 0.5f) / 22f;
                float x = Mathf.Lerp(stX0, stX1, t);
                float z = Mathf.Lerp(stZ0, stZ1, t);
                float y = Mathf.Lerp(stY0, deckWorldY + 0.02f, t);
                Box("PrayerStep_" + i, steps.transform,
                    new Vector3(x, y, z), new Vector3(1.24f, 0.46f, 0.92f), stone);
            }
            {
                Vector3 p0 = new Vector3(stX0, stY0, stZ0);
                Vector3 p1 = new Vector3(stX1, deckWorldY + 0.02f, stZ1);
                Vector3 d = p1 - p0;
                var fill = Box("PrayerStairFill", steps.transform, (p0 + p1) * 0.5f + Vector3.down * 0.30f,
                    new Vector3(1.30f, 0.58f, d.magnitude + 0.5f), stone);
                fill.transform.rotation = Quaternion.LookRotation(d.normalized);
            }

            // 岩面：几块大石叠成一堵，其中一块被凿平
            var rocks = new[]
            {
                new Vector4(-1.9f, 0.9f, 2.6f, 1.1f),
                new Vector4( 0.4f, 0.4f, 3.4f, 1.4f),
                new Vector4( 2.3f, 0.8f, 2.2f, 0.9f),
                new Vector4(-0.6f, 2.0f, 2.0f, 0.8f),
                new Vector4( 1.6f, -0.4f, 1.8f, 0.7f),
                new Vector4(-2.6f, 2.5f, 1.6f, 1.0f),
                new Vector4( 1.2f, 2.9f, 1.3f, 0.9f),
                new Vector4( 3.1f, 2.3f, 1.2f, 0.8f),
                new Vector4( 4.0f, 0.5f, 1.9f, 0.9f),
            };
            // 每块岩体用一大两小的碎石叠成，单块方盒会读成"切好的石板"
            for (int i = 0; i < rocks.Length; i++)
            {
                float s = rocks[i].w;
                Material m = i % 2 == 0 ? stone : stonePale;
                var b = Empty("Boulder_" + i, g.transform,
                    new Vector3(rocks[i].x, rocks[i].y - 0.35f, rocks[i].z)).transform;
                b.localRotation = Quaternion.Euler(i * 2.5f, i * 11f, i * 3.5f);
                Box("Core", b, Vector3.zero, new Vector3(s * 2.05f, s * 1.42f, s * 1.45f), m, true);
                Box("Flank", b, new Vector3(s * 0.52f, s * 0.20f, -s * 0.28f),
                    new Vector3(s * 1.24f, s * 0.96f, s * 1.02f), m, false)
                    .transform.localRotation = Quaternion.Euler(0f, 26f, 8f);
                Box("Chip", b, new Vector3(-s * 0.60f, -s * 0.16f, s * 0.36f),
                    new Vector3(s * 0.92f, s * 0.82f, s * 0.88f), stoneDark, false)
                    .transform.localRotation = Quaternion.Euler(5f, -29f, -6f);
            }

            // 被凿平的岩面：贴在整组岩石最外侧，碑就刻在石面上
            const float faceZ = 5.05f;
            Box("SteleFace", g.transform, new Vector3(-0.10f, 1.62f, faceZ), new Vector3(2.86f, 3.16f, 0.16f), stonePale);
            Box("SteleFaceFoot", g.transform, new Vector3(-0.10f, 0.26f, faceZ + 0.02f), new Vector3(3.10f, 0.54f, 0.22f), stone);

            // 祈风二字的竖排楷书
            QzVerticalText(g.transform, "祈風", new Vector3(-0.60f, 2.34f, faceZ + 0.11f),
                new Vector3(0f, 0f, 0f), 1.00f, 0.92f, new Color(0.11f, 0.11f, 0.10f), 1.20f);

            // 碑前平台：把坡地削平，香炉石灯才有地方落
            float deckTop = deckWorldY - gy;
            Box("PrayerTerrace", g.transform, new Vector3(0.2f, deckTop - 1.70f, 6.9f),
                new Vector3(10.4f, 3.40f, 7.2f), stone);
            Box("PrayerTerraceTop", g.transform, new Vector3(0.2f, deckTop - 0.11f, 6.9f),
                new Vector3(10.4f, 0.22f, 7.2f), stonePale);
            Box("PrayerTerraceBelt", g.transform, new Vector3(0.2f, deckTop - 1.55f, 6.9f),
                new Vector3(10.52f, 0.26f, 7.32f), stoneDark);
            Box("PrayerTerraceEdge", g.transform, new Vector3(0.2f, deckTop - 0.16f, 10.5f),
                new Vector3(10.4f, 0.36f, 0.42f), stoneDark);
            Box("PrayerTerraceLipL", g.transform, new Vector3(-5.0f, deckTop - 0.16f, 6.9f),
                new Vector3(0.42f, 0.36f, 7.2f), stoneDark);
            Box("PrayerTerraceLipR", g.transform, new Vector3(5.4f, deckTop - 0.16f, 6.9f),
                new Vector3(0.42f, 0.36f, 7.2f), stoneDark);

            // 挡土墙面的粗石：打散整块平台的水泥感，两皮错缝，逐块微差
            for (int row = 0; row < 3; row++)
            {
                float ry = deckTop - 2.42f + row * 0.78f;
                for (int i = 0; i < 17; i++)
                {
                    float rx = -5.0f + i * 0.62f + (row % 2 == 1 ? 0.31f : 0f);
                    if (rx > 5.35f) continue;
                    float jr = Hash01(i * 7 + row * 31 + 3);
                    float jh = Hash01(i * 13 + row * 17 + 5);
                    Box("TerraceStone_" + row + "_" + i, g.transform,
                        new Vector3(rx, ry + (jr - 0.5f) * 0.16f, 10.58f + jr * 0.12f),
                        new Vector3(0.56f + jh * 0.14f, 0.60f + jr * 0.14f, 0.18f),
                        (i + row) % 5 == 0 ? stone : stonePale, false);
                }
            }

            // 石香炉与供石
            Cyl("PrayerBurner", g.transform, new Vector3(-2.6f, deckTop + 0.30f, 7.4f), 0.40f, 0.60f, stoneDark, true);
            Cyl("PrayerBurnerLip", g.transform, new Vector3(-2.6f, deckTop + 0.62f, 7.4f), 0.44f, 0.08f, stone);
            Box("PrayerOffering", g.transform, new Vector3(-2.6f, deckTop + 0.10f, 8.8f), new Vector3(1.20f, 0.20f, 0.90f), stone);

            // 一对石灯
            for (int s = -1; s <= 1; s += 2)
            {
                Cyl("PrayerLampPost_" + s, g.transform, new Vector3(s * 3.6f, deckTop + 0.72f, 8.6f), 0.10f, 1.44f, stone, true);
                Box("PrayerLampHouse_" + s, g.transform, new Vector3(s * 3.6f, deckTop + 1.62f, 8.6f),
                    new Vector3(0.42f, 0.44f, 0.42f), lantern, false);
                MeshObject("PrayerLampCap_" + s, g.transform, new Vector3(s * 3.6f, deckTop + 1.90f, 8.6f),
                    MeshFactory.CurvedRoof(0.40f, 0.40f, 0.22f, 0.06f, 0.05f, 8, 8), stone, false);
            }
        }

        // ================================================================ 洛阳桥（桥头）

        static void QzLuoyangBridge(Transform parent, Material stone, Material stonePale,
                                    Material stoneDark, Material moss)
        {
            var g = Empty("LuoyangBridge", parent, Vector3.zero);
            const float cz = -44f;          // 桥中线
            const float halfW = 2.9f;       // 桥宽的一半
            const float step = 6.4f;        // 墩距

            // 桥头与岸壁咬合的桥台
            Box("BridgeAbutment", g.transform, new Vector3(0.2f, 0.30f, cz),
                new Vector3(2.6f, 2.4f, halfW * 2f + 0.8f), stoneDark, true);
            Box("BridgeAbutmentTop", g.transform, new Vector3(0.2f, 1.42f, cz),
                new Vector3(2.8f, 0.18f, halfW * 2f + 1.0f), stonePale);

            for (int i = 0; i < 10; i++)
            {
                float x = -2.6f - i * step;

                // 筏形基础：往江里抛填石块，养成一条矮石堤，再在堤上砌墩
                Box("BridgeRaft_" + i, g.transform, new Vector3(x, -0.70f, cz),
                    new Vector3(step * 0.92f, 1.70f, halfW * 2f + 1.5f), stoneDark, true);

                // 蛎房：桥基上养牡蛎，让蛎房把石块箍成一体
                for (int k = 0; k < 7; k++)
                {
                    float u = Hash01(i * 97 + k * 31);
                    float v = Hash01(i * 53 + k * 71);
                    float px = x + (u - 0.5f) * step * 0.80f;
                    float pz = cz + (v - 0.5f) * (halfW * 2f + 0.9f);
                    Box("BridgeOyster_" + i + "_" + k, g.transform,
                        new Vector3(px, -0.28f + Hash01(i * 17 + k) * 0.30f, pz),
                        new Vector3(0.34f + u * 0.30f, 0.22f + v * 0.16f, 0.36f + v * 0.26f),
                        (k % 3 == 0) ? stonePale : moss, false);
                }

                // 墩身：条石错缝，向上略收。墩要露出水面一截，才看得出「墩」
                Box("BridgePier_" + i, g.transform, new Vector3(x, 0.20f, cz),
                    new Vector3(2.30f, 1.60f, halfW * 2f - 0.30f), stone, true);
                Box("BridgePierCap_" + i, g.transform, new Vector3(x, 1.02f, cz),
                    new Vector3(2.70f, 0.26f, halfW * 2f - 0.10f), stonePale);

                // 墩的分水尖：迎水面收出一个尖角，水才咬不住墩
                var nose = Box("BridgeNose_" + i, g.transform, new Vector3(x, 0.20f, cz),
                    new Vector3(1.90f, 1.58f, 1.90f), stone, true);
                nose.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

                // 石梁：两墩之间架上整条花岗岩石板（整块满铺，中间不留缝）
                Box("BridgeBeam_" + i, g.transform,
                    new Vector3(x + step * 0.5f, 1.34f, cz),
                    new Vector3(step * 1.06f, 0.32f, halfW * 2f), stonePale, true);
                // 梁缝压石：一条细石带，读得出是一根一根架上去的
                Box("BridgeBeamSeam_" + i, g.transform,
                    new Vector3(x + step * 0.5f, 1.52f, cz),
                    new Vector3(0.16f, 0.06f, halfW * 2f), stoneDark, false);

                // 石栏：矮柱 + 压面石
                if (i < 6)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        float sz = cz + (s == 0 ? -halfW + 0.18f : halfW - 0.18f);
                        float px = x + step * 0.5f;
                        Box("BridgePost_" + i + "_" + s, g.transform, new Vector3(px, 1.96f, sz),
                            new Vector3(0.26f, 0.92f, 0.26f), stoneDark, false);
                    }
                    for (int s = 0; s < 2; s++)
                    {
                        float sz = cz + (s == 0 ? -halfW + 0.18f : halfW - 0.18f);
                        Box("BridgeRail_" + i + "_" + s, g.transform,
                            new Vector3(x + step * 0.5f, 2.32f, sz),
                            new Vector3(step * 1.02f, 0.22f, 0.30f), stonePale, false);
                    }
                }
            }

            // 桥头最后一级石梁落到岸面上
            Box("BridgeEnd", g.transform, new Vector3(-2.6f + step * 0.5f, 1.34f, cz),
                new Vector3(step * 1.06f, 0.32f, halfW * 2f), stonePale, true);
        }

        // ================================================================ 开元寺双塔

        /// <summary>开元寺双塔：东为镇国塔、西为仁寿塔，同立一块台基上，五层八角。</summary>
        static void QzPagoda(Transform parent, Material stonePale, Material stoneDark, Material granite)
        {
            var g = Empty("KaiyuanTemple", parent, new Vector3(78f, QzTempleY, 21f));

            // 台基：把坡地压成一块平地，双塔才站得住
            Box("TempleTerrace", g.transform, new Vector3(0f, -0.60f, 0f),
                new Vector3(30f, 1.70f, 20f), granite, true);
            Box("TempleTerraceTop", g.transform, new Vector3(0f, 0.30f, 0f),
                new Vector3(29.2f, 0.24f, 19.2f), stonePale);

            // 踏跺：台基前沿三级石阶
            for (int s = 0; s < 3; s++)
            {
                Box("TempleStep_" + s, g.transform,
                    new Vector3(0f, -0.10f - s * 0.34f, -10.1f - s * 0.46f),
                    new Vector3(7.6f, 0.34f, 1.10f), stonePale, true);
            }

            // 台基前沿的矮栏，把台地收边
            for (int s = 0; s < 12; s++)
            {
                float x = -14.4f + s * 2.4f;
                if (Mathf.Abs(x) < 4.0f) continue;
                Box("TempleRailPost_" + s, g.transform, new Vector3(x, 0.86f, -9.8f),
                    new Vector3(0.30f, 0.90f, 0.30f), stoneDark, false);
            }

            // 镇国塔（东）、仁寿塔（西）：层数相同，高度略有差
            QzStoneTower(g.transform, "ZhengGuoPagoda", new Vector3(7.6f, 0.42f, 0f), 26.0f, 5,
                stonePale, stoneDark, granite);
            QzStoneTower(g.transform, "RenShouPagoda", new Vector3(-7.6f, 0.42f, 0f), 23.0f, 5,
                stonePale, stoneDark, granite);
        }

        /// <summary>一座五层八角石塔：须弥座、逐层收分的塔身、出檐、塔刹。</summary>
        static void QzStoneTower(Transform parent, string name, Vector3 pos, float height, int tiers,
                                 Material stonePale, Material stoneDark, Material granite)
        {
            var t = Empty(name, parent, pos);

            // 须弥座
            Box("Base", t.transform, new Vector3(0f, 0.50f, 0f), new Vector3(7.0f, 1.00f, 7.0f), stoneDark, true);
            Box("Base2", t.transform, new Vector3(0f, 1.16f, 0f), new Vector3(6.0f, 0.34f, 6.0f), stonePale);
            Box("Base3", t.transform, new Vector3(0f, 1.44f, 0f), new Vector3(6.4f, 0.22f, 6.4f), stoneDark);

            float y = 1.55f;
            float tierH = (height - 1.55f - 3.4f) / tiers;
            for (int i = 0; i < tiers; i++)
            {
                float k = 1f - i * 0.088f;
                float bodyH = tierH * 0.74f;
                float eaveH = tierH * 0.26f;
                float radius = 2.55f * k;

                // 塔身：八角，每层收一点
                MeshObject("Tier_" + i, t.transform, new Vector3(0f, y, 0f),
                    MeshFactory.Prism(radius, bodyH, 8, 1.6f), stonePale, true);
                y += bodyH;

                // 出檐：一圈挑出的八角薄板，边角比塔身宽出一截
                MeshObject("Eave_" + i, t.transform, new Vector3(0f, y - 0.06f, 0f),
                    MeshFactory.Prism(radius * 1.34f, eaveH, 8, 1.4f), granite, false);
                // 檐下的斗拱带：一圈窄条，读得出「石做的木构」
                MeshObject("Bracket_" + i, t.transform, new Vector3(0f, y + eaveH - 0.10f, 0f),
                    MeshFactory.Prism(radius * 1.06f, 0.16f, 8, 1.4f), stonePale, false);
                y += eaveH;
            }

            // 塔刹
            MeshObject("Spire", t.transform, new Vector3(0f, y, 0f),
                MeshFactory.Prism(0.62f, 1.20f, 8, 1.2f), stoneDark, false);
            y += 1.20f;
            Cyl("SpireStem", t.transform, new Vector3(0f, y + 0.62f, 0f), 0.14f, 1.70f, stoneDark);
            for (int i = 0; i < 4; i++)
                Cyl("SpireRing_" + i, t.transform, new Vector3(0f, y + 0.34f + i * 0.28f, 0f),
                    0.34f - i * 0.05f, 0.08f, stonePale);
        }

        // ================================================================ 植被

        static bool QzPaved(float x, float z)
        {
            if (x > -1f && x < QzQuayX1 + 0.5f && z > QzQuayZ0 - 1f && z < QzQuayZ1 + 1f) return true;
            if (x > QzDockX - 12f && x < QzDockX + 12f && z > QzDockZ - 21f && z < QzDockZ + 21f) return true;
            if (x > QzCargoX - 7f && x < QzCargoX + 7f && z > QzCargoZ - 9f && z < QzCargoZ + 9f) return true;
            if (x > QzWallX - 5f && x < QzWallX + 5f && z > QzWallZ - 4f && z < QzWallZ + 4f) return true;
            return false;
        }

        /// <summary>港区作业面：码头、干坞、货场连成的整片人工地面。自然物不往这里长。</summary>
        static bool QzWorks(float x, float z)
        {
            return QnRect(x, z, -6f, 54f, -66f, 90f, 8f) > 0.02f;
        }

        static void QzBanyan(Transform parent, Vector3 pos, float scale,
                             Material bark, Material foliage, Material grass)
        {
            var g = Empty("Banyan", parent, pos);
            Cyl("BanyanTrunk", g.transform, new Vector3(0f, 1.7f * scale, 0f),
                0.30f * scale, 3.4f * scale, bark, true);
            // 分支
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 26f;
                var b = Cyl("BanyanBranch_" + i, g.transform,
                    new Vector3(0f, 3.3f * scale, 0f), 0.12f * scale, 2.6f * scale, bark);
                b.transform.localRotation = Quaternion.Euler(0f, a, 34f);
            }
            // 冠：三五团叠起来，不用一整颗球
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                float r = (i == 0) ? 0f : 1.5f * scale;
                Ball("BanyanCanopy_" + i, g.transform,
                    new Vector3(Mathf.Cos(a) * r, (4.6f + (i % 2) * 0.7f) * scale, Mathf.Sin(a) * r),
                    (2.0f + Home01(i) * 0.9f) * scale, foliage);
            }
            // 气根：从冠下垂到地面，是榕树最认得出的特征
            for (int i = 0; i < 7; i++)
            {
                float a = Home01(i * 3 + 1) * Mathf.PI * 2f;
                float r = (1.6f + Home01(i * 5) * 1.7f) * scale;
                float h = (2.6f + Home01(i * 7) * 2.2f) * scale;
                Cyl("BanyanRoot_" + i, g.transform,
                    new Vector3(Mathf.Cos(a) * r, h * 0.5f, Mathf.Sin(a) * r),
                    0.035f * scale, h, bark, false);
            }
        }

        static float Home01(int n)
        {
            float f = Mathf.Sin(n * 78.233f + 12.9898f) * 43758.5453f;
            return f - Mathf.Floor(f);
        }

        static void QzErythrina(Transform parent, Vector3 pos, float scale,
                                Material bark, Material leaf, Material flower)
        {
            var g = Empty("Erythrina", parent, pos);
            Cyl("ErythrinaTrunk", g.transform, new Vector3(0f, 1.6f * scale, 0f),
                0.22f * scale, 3.2f * scale, bark, true);
            for (int i = 0; i < 3; i++)
            {
                var b = Cyl("ErythrinaBranch_" + i, g.transform,
                    new Vector3(0f, 3.1f * scale, 0f), 0.09f * scale, 2.2f * scale, bark);
                b.transform.localRotation = Quaternion.Euler(0f, i * 120f, 30f);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                float r = (i == 0) ? 0f : 1.1f * scale;
                Ball("ErythrinaLeaf_" + i, g.transform,
                    new Vector3(Mathf.Cos(a) * r, (4.1f + (i % 2) * 0.5f) * scale, Mathf.Sin(a) * r),
                    (1.5f + Home01(i * 11) * 0.6f) * scale, leaf);
            }
            // 刺桐的红花：成簇点在冠上
            for (int i = 0; i < 6; i++)
            {
                float a = Home01(i * 13) * Mathf.PI * 2f;
                float r = 0.8f + Home01(i * 17) * 1.5f;
                Ball("ErythrinaFlower_" + i, g.transform,
                    new Vector3(Mathf.Cos(a) * r * scale, (3.9f + Home01(i * 19) * 1.6f) * scale,
                        Mathf.Sin(a) * r * scale), 0.42f * scale, flower);
            }
        }

        static void QzPalm(Transform parent, Vector3 pos, float scale, Material bark, Material leaf)
        {
            var g = Empty("Palm", parent, pos);

            // 树干分段微弯，向海一侧偏；直杆会是「棒棒糖」，弯杆才像椰子树
            float lean = 3.5f + Home01(Mathf.RoundToInt(pos.x * 7f + pos.z * 3f)) * 3.5f;
            var trunk = Empty("PalmTrunk", g.transform, Vector3.zero).transform;
            trunk.localRotation = Quaternion.Euler(0f, 0f, lean);
            for (int i = 0; i < 3; i++)
            {
                float r = (0.20f - i * 0.035f) * scale;
                float h = 1.9f * scale;
                Cyl("PalmTrunkSeg_" + i, trunk, new Vector3(0f, h * 0.5f + i * h * 0.94f, 0f),
                    r, h, bark, i == 0);
                // 环状叶痕
                if (i > 0)
                    Cyl("PalmRing_" + i, trunk, new Vector3(0f, i * h * 0.94f, 0f),
                        r * 1.12f, 0.07f * scale, bark);
            }

            var crown = Empty("PalmCrown", trunk, new Vector3(0f, 5.62f * scale, 0f)).transform;
            Ball("PalmCrownBulb", crown, Vector3.zero, 0.24f * scale, bark);

            // 九片叶，每片三段，越往外越垂
            for (int i = 0; i < 9; i++)
            {
                float yaw = i * 40f + Home01(Mathf.RoundToInt(pos.z * 11f) + i * 3) * 14f;
                var frond = Empty("PalmFrond_" + i, crown, Vector3.zero).transform;
                frond.localRotation = Quaternion.Euler(0f, yaw, 0f);

                Transform cur = frond;
                float segLen = 1.25f * scale;
                float wide = 0.56f * scale;
                for (int s = 0; s < 3; s++)
                {
                    Box("FrondSeg_" + i + "_" + s, cur,
                        new Vector3(segLen * 0.5f, 0f, 0f),
                        new Vector3(segLen, 0.045f * scale, wide), leaf, false);

                    // 关节挂在未缩放的节点上，缩放只属于叶片本身
                    var joint = Empty("Joint", cur, new Vector3(segLen, 0f, 0f)).transform;
                    joint.localRotation = Quaternion.Euler(0f, 0f, -(13f + s * 20f));
                    cur = joint;
                    segLen *= 0.80f;
                    wide *= 0.74f;
                }
            }

            // 叶下的椰果
            for (int i = 0; i < 3; i++)
                Ball("PalmNut_" + i, crown,
                    new Vector3(Mathf.Cos(i * 2.1f) * 0.20f * scale, -0.26f * scale,
                                Mathf.Sin(i * 2.1f) * 0.20f * scale), 0.11f * scale, bark);
        }

        static void QzFlora(Transform parent, Material grass, Material reed, Material banyan,
                            Material leaf, Material flower, Material bark)
        {
            var g = Empty("Flora", parent, Vector3.zero);

            // 岸上的树：避开铺装，靠水的地方密一些
            var spots = new[]
            {
                new Vector4(-4.5f, 44f, 1.15f, 0f),
                new Vector4(-6.0f, 62f, 1.00f, 1f),
                new Vector4( 3.5f, 84f, 1.25f, 0f),
                new Vector4(-9.0f, 96f, 0.90f, 2f),
                new Vector4( 8.0f, 52f, 0.95f, 1f),
                new Vector4( 9.5f, -68f, 1.30f, 0f),
                new Vector4(-4.0f, -78f, 1.05f, 1f),
                new Vector4( 6.0f, -96f, 1.15f, 2f),
                new Vector4( 22f, 34f, 1.10f, 1f),
                new Vector4( 30f, 52f, 1.20f, 0f),
                new Vector4( 42f, 60f, 1.05f, 2f),
                new Vector4( 56f, 48f, 1.15f, 0f),
                new Vector4( 68f, 34f, 1.00f, 1f),
                new Vector4( 74f, 10f, 1.10f, 0f),
                new Vector4( 46f, -6f, 0.95f, 2f),
                new Vector4( 58f, -30f, 1.20f, 1f),
                new Vector4( 72f, -60f, 1.05f, 0f),
                new Vector4( 40f, -78f, 1.15f, 1f),
                new Vector4( 20f, -96f, 1.00f, 2f),
                new Vector4( 96f, 42f, 0.90f, 1f),
                new Vector4(110f, 60f, 1.05f, 0f),
                new Vector4( 62f, 74f, 1.10f, 2f),
                new Vector4( 88f, -46f, 1.00f, 1f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                float x = spots[i].x, z = spots[i].z, s = spots[i].w;
                if (QzPaved(x, z)) continue;
                float y = QuanzhouHeight(x, z);
                if (y < 0.2f) continue;
                int kind = (int)spots[i].w;
                var p = new Vector3(x, y, z);
                switch (kind)
                {
                    case 0: QzBanyan(g.transform, p, s, bark, banyan, grass); break;
                    case 1: QzErythrina(g.transform, p, s, bark, leaf, flower); break;
                    default: QzPalm(g.transform, p, s, bark, leaf); break;
                }
            }

            // 灌木丛：成窝生长，一窝三五株，避免满地均匀撒点
            for (int c = 0; c < 150; c++)
            {
                float cx = -12f + Home01(c * 29) * 130f;
                float cz = -104f + Home01(c * 31) * 208f;
                if (QzWorks(cx, cz)) continue;
                float cy = QuanzhouHeight(cx, cz);
                if (cy < 0.35f) continue;
                int n = 2 + (int)(Home01(c * 41) * 4f);
                for (int k = 0; k < n; k++)
                {
                    float a = Home01(c * 53 + k * 7) * 6.283f;
                    float r = 0.30f + Home01(c * 59 + k * 11) * 1.10f;
                    float x = cx + Mathf.Cos(a) * r, z = cz + Mathf.Sin(a) * r;
                    if (QzPaved(x, z)) continue;
                    float y = QuanzhouHeight(x, z);
                    if (y < 0.30f) continue;
                    float s2 = 0.55f + Home01(c * 61 + k * 13) * 0.85f;
                    Ball("Bush_" + c + "_" + k, g.transform,
                        new Vector3(x, y + s2 * 0.34f, z),
                        s2, k % 3 == 0 ? leaf : banyan).transform.localScale =
                        new Vector3(s2 * 2f, s2 * 1.45f, s2 * 2f);
                }
            }

            // 草丛与芦苇：只在水线和坡地上，草色要亮起来
            for (int i = 0; i < 420; i++)
            {
                float x = -10f + Home01(i * 29 + 3) * 128f;
                float z = -104f + Home01(i * 31 + 5) * 208f;
                if (QzPaved(x, z)) continue;
                if (QzWorks(x, z)) continue;
                float y = QuanzhouHeight(x, z);
                if (y < 0.20f) continue;
                bool wet = x < 4.5f;
                QzReeds(g.transform, new Vector3(x, y, z), wet ? 1.15f : 0.85f, 1f,
                        wet ? reed : grass, grass, wet ? 3 : 2);
            }
        }

        // ================================================================ 雾中帆影

        static void QzDistantSails(Transform parent, Material hull, Material sail, Material wood)
        {
            var g = Empty("DistantSails", parent, Vector3.zero);
            // 远处帆布不能再和近船共用同一种暖白：150–200 m 的雾几乎把帆染成雾色，
            // 只比背景亮一点点，结果海面上浮着一块块边缘笔直的浅色方块。
            // 给远帆单独压暗两档，读成雾里的剪影，才是"帆影"。
            var sailFar = MatAs("QzSailFar", new Color(0.50f, 0.49f, 0.43f), "fabric", 0f, 0.08f);
            // (x, z, 船体长度, 船体高度, 桅高)：外海上的船，只做剪影，不靠岸
            var ships = new[]
            {
                (-146f, -52f, 22f, 3.4f, 15f),
                (-168f, 34f, 26f, 4.0f, 18f),
                (-192f, -128f, 30f, 4.6f, 21f),
                (-215f, 96f, 24f, 3.8f, 16f),
            };
            for (int i = 0; i < ships.Length; i++)
            {
                var d = ships[i];
                var s = Empty("Sail_" + i, g.transform, new Vector3(d.Item1, QzWaterY, d.Item2));
                s.transform.localRotation = Quaternion.Euler(0f, i * 47f, i % 2 == 0 ? 3f : -2f);

                float len = d.Item3, hi = d.Item4, mast = d.Item5;

                Box("SailHull_" + i, s.transform, new Vector3(0f, hi * 0.30f, 0f),
                    new Vector3(len, hi, len * 0.30f), hull, false);

                // 艏艉楼：福建船的剪影靠这两块抬高的船身认出来
                Box("SailBow_" + i, s.transform, new Vector3(len * 0.40f, hi * 0.86f, 0f),
                    new Vector3(len * 0.20f, hi * 0.62f, len * 0.22f), hull, false);
                Box("SailStern_" + i, s.transform, new Vector3(-len * 0.38f, hi * 1.00f, 0f),
                    new Vector3(len * 0.24f, hi * 0.90f, len * 0.24f), hull, false);

                float[] masts = { -0.30f, 0.02f, 0.34f };
                float[] heights = { 0.62f, 0.80f, 0.58f };
                for (int m = 0; m < masts.Length; m++)
                {
                    float mh = mast * heights[m];
                    Cyl("SailMast_" + i + "_" + m, s.transform,
                        new Vector3(masts[m] * len, mh * 0.5f, 0f), 0.13f, mh, wood);

                    float sh = mh * 0.52f, sw = len * 0.40f;
                    MeshObject("SailCloth_" + i + "_" + m, s.transform,
                        new Vector3(masts[m] * len + 0.16f, mh * 0.70f, 0f),
                        QzJunkSail(sw * 0.90f, sw, sh * 0.82f, 0.12f), sailFar, false);
                }
            }
        }


        /// <summary>硬帆：梯形、迎风鼓起。泉州外海的福船帆是竹撑条撑起的平板帆，不是软帆。</summary>
        static Mesh QzJunkSail(float halfBottom, float halfTop, float h, float belly)
        {
            const int nx = 5, ny = 6;
            var verts = new Vector3[(nx + 1) * (ny + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new List<int>();
            for (int j = 0; j <= ny; j++)
            {
                float v = (float)j / ny;
                float y = (v - 0.5f) * h;
                float hw = Mathf.Lerp(halfBottom, halfTop, v);
                for (int k = 0; k <= nx; k++)
                {
                    float u = (float)k / nx;
                    float z = Mathf.Sin(Mathf.PI * u) * belly * (0.35f + 0.65f * (1f - v));
                    uvs[j * (nx + 1) + k] = new Vector2(u, v);
                    // 帆面竖在 Y-Z 平面：y 取高度，z 取宽度，鼓向沿 ±X
                    verts[j * (nx + 1) + k] = new Vector3(-z, y, (u - 0.5f) * 2f * hw);
                }
            }
            // 帆要双面：反面必须用独立顶点，否则法线被平均成零，渲染出来是一片没明暗的糊块
            int front = verts.Length;
            var all = new Vector3[front * 2];
            var allUv = new Vector2[front * 2];
            Array.Copy(verts, all, front);
            Array.Copy(verts, 0, all, front, front);
            Array.Copy(uvs, allUv, front);
            Array.Copy(uvs, 0, allUv, front, front);

            for (int j = 0; j < ny; j++)
                for (int k = 0; k < nx; k++)
                {
                    int a = j * (nx + 1) + k, b = (j + 1) * (nx + 1) + k;
                    int c = b + 1, d = a + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                    tris.Add(front + a); tris.Add(front + c); tris.Add(front + b);
                    tris.Add(front + a); tris.Add(front + d); tris.Add(front + c);
                }
            var m = new Mesh { name = "JunkSail" };
            m.vertices = all;
            m.uv = allUv;
            m.triangles = tris.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ---------------------------------------------------------------- 闽南红砖厝

        /// <summary>燕尾脊：脊线两端翘起如燕尾。用五段渐次抬起的短脊拼出来，远看就是那道弧。</summary>
        static void QzSwallowRidge(Transform parent, float halfLen, float y, float z,
                                   Material ridge, Material tip)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                // 节点必须是 scale=1 的空物体：方块本身带缩放，做它的子物体会被逐级放大
                Transform node = Empty("SwallowRidge_" + s, parent, new Vector3(0f, y, z)).transform;
                float seg = halfLen * 0.30f;
                for (int i = 0; i < 4; i++)
                {
                    Box("Tail_" + s + "_" + i, node,
                        new Vector3(s * seg * 0.5f, 0f, 0f),
                        new Vector3(seg * 1.10f, 0.16f, 0.30f), ridge, false);
                    var joint = Empty("J", node, new Vector3(s * seg, 0f, 0f)).transform;
                    joint.localRotation = Quaternion.Euler(0f, 0f, s * (13f + i * 7f));
                    node = joint;
                    seg *= 0.82f;
                }
                Ball("TailTip_" + s, node, Vector3.zero, 0.11f, tip);
            }
            Box("RidgeBody", parent, new Vector3(0f, y, z),
                new Vector3(halfLen * 1.02f, 0.22f, 0.32f), ridge, false);
        }

        /// <summary>一座闽南红砖厝：石埕、红砖墙、白石檐带、燕尾脊。泉州最一眼可辨的民居形制。</summary>
        static void QzMinnanHouse(Transform parent, Vector3 pos, float yaw, float scale,
                                  Material brick, Material stonePale, Material stoneDark,
                                  Material tile, Material wood, Material plaster)
        {
            var g = Empty("MinnanHouse", parent, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            float w = 8.2f * scale;   // 面阔
            float d = 6.0f * scale;   // 进深
            float hgt = 3.4f * scale;

            // 石埕：屋前的花岗岩铺地，红砖厝必有
            Box("Court", g.transform, new Vector3(0f, 0.06f, -d * 0.5f - 2.4f * scale),
                new Vector3(w * 1.22f, 0.12f, 4.6f * scale), stonePale);

            // 石基、红砖墙身、白石檐带
            Box("Plinth", g.transform, new Vector3(0f, 0.22f * scale, 0f),
                new Vector3(w * 1.03f, 0.44f * scale, d * 1.03f), stoneDark);
            Box("Body", g.transform, new Vector3(0f, 0.44f * scale + hgt * 0.5f, 0f),
                new Vector3(w, hgt, d), brick);
            Box("EaveBand", g.transform, new Vector3(0f, 0.44f * scale + hgt + 0.13f * scale, 0f),
                new Vector3(w * 1.04f, 0.26f * scale, d * 1.04f), stonePale);

            // 凹寿门廊：正面中间退进一块，是闽南宅院的入口做法
            Box("PorchRecess", g.transform, new Vector3(0f, 1.55f * scale, -d * 0.5f + 0.12f),
                new Vector3(2.4f * scale, 2.6f * scale, 0.30f), stoneDark, false);
            Box("DoorFrame", g.transform, new Vector3(0f, 1.32f * scale, -d * 0.5f - 0.06f),
                new Vector3(1.5f * scale, 2.16f * scale, 0.10f), stonePale, false);
            Box("DoorLeaf", g.transform, new Vector3(0f, 1.28f * scale, -d * 0.5f - 0.13f),
                new Vector3(1.12f * scale, 2.02f * scale, 0.06f), wood, false);
            // 门两侧的石雕窗
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Window_" + s, g.transform,
                    new Vector3(s * w * 0.30f, 1.86f * scale, -d * 0.5f - 0.06f),
                    new Vector3(1.1f * scale, 1.0f * scale, 0.08f), stonePale, false);
                Box("WindowGrille_" + s, g.transform,
                    new Vector3(s * w * 0.30f, 1.86f * scale, -d * 0.5f - 0.11f),
                    new Vector3(0.86f * scale, 0.10f, 0.05f), stoneDark, false);
            }

            // 屋顶：脊线沿面阔（X）。GableRoof 的脊沿 Z，套一层 yaw 90 转过来
            var roof = Empty("RoofPivot", g.transform, new Vector3(0f, 0.44f * scale + hgt + 0.26f * scale, 0f)).transform;
            roof.localRotation = Quaternion.Euler(0f, 90f, 0f);
            MeshObject("Roof", roof, Vector3.zero,
                MeshFactory.GableRoof(d * 0.60f, w * 0.56f, 1.34f * scale, 0.62f * scale), tile, false);

            // 燕尾脊：转回世界朝向放在屋脊两端
            QzSwallowRidge(g.transform, w * 0.56f,
                0.44f * scale + hgt + 0.26f * scale + 1.34f * scale + 0.10f, 0f,
                tile, plaster);

            // 山墙上的灰塑与窗
            for (int s = -1; s <= 1; s += 2)
                Box("GableWindow_" + s, g.transform,
                    new Vector3(s * w * 0.505f, 0.44f * scale + hgt * 0.62f, 0f),
                    new Vector3(0.06f, 0.86f * scale, 0.86f * scale), stoneDark, false);
        }

        static void QzVillage(Transform parent, Material brick, Material stonePale, Material stoneDark,
                              Material tile, Material wood, Material plaster, Material leaf, Material bark)
        {
            var g = Empty("Village", parent, Vector3.zero);

            // 五座红砖厝沿缓坡排开，朝向海；错落，不排成一条线
            var houses = new[]
            {
                new Vector4(26.5f, 30f, 1.00f, -6f),
                new Vector4(34.0f, 40f, 1.12f, 8f),
                new Vector4(25.0f, 50f, 0.92f, 4f),
                new Vector4(35.5f, 60f, 1.06f, -10f),
                new Vector4(27.0f, 68f, 0.98f, 2f),
            };
            for (int i = 0; i < houses.Length; i++)
            {
                float x = houses[i].x, z = houses[i].y, sc = houses[i].z, yaw = houses[i].w + 180f;
                float y = QuanzhouHeight(x, z);
                QzMinnanHouse(g.transform, new Vector3(x, y, z), yaw, sc,
                    brick, stonePale, stoneDark, tile, wood, plaster);

                // 屋后几棵树，压住天际线
                QzBanyan(g.transform, new Vector3(x + 3.6f * sc, QuanzhouHeight(x + 3.6f * sc, z + 3.0f), z + 3.0f),
                    0.85f * sc, bark, leaf, leaf);
            }
        }

        // ---------------------------------------------------------------- 花岗岩露头

        /// <summary>岸上零散的花岗岩露头。三块互相咬合、各自转过角，避免读成一堆方块。</summary>
        static void QzBoulders(Transform parent, Material rock, Material rockPale, Material rockDark,
                               Material moss)
        {
            var g = Empty("Boulders", parent, Vector3.zero);

            for (int i = 0; i < 120; i++)
            {
                float x = -14f + Home01(i * 17 + 3) * 150f;
                float z = -110f + Home01(i * 23 + 7) * 224f;
                if (QzPaved(x, z)) continue;
                float y = QuanzhouHeight(x, z);
                // 港区平台一律不放石头：那里看起来只会像乱堆的货箱
                if (QzWorks(x, z)) continue;
                if (y < 2.2f) continue;

                // 越往丘陵越密、越大
                float big = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.5f, 15f, y));
                float s = (0.55f + Home01(i * 41) * 1.05f) * (0.7f + big * 1.9f);
                Material m = i % 5 == 0 ? rockPale : (i % 7 == 0 ? rockDark : rock);

                var b = Empty("Boulder_" + i, g.transform, new Vector3(x, y - s * 0.30f, z)).transform;
                b.localRotation = Quaternion.Euler(Home01(i * 13) * 12f, Home01(i * 19) * 360f, Home01(i * 29) * 12f);

                Box("Core", b, Vector3.zero, new Vector3(s * 2.1f, s * 0.78f, s * 1.6f), m, false);
                Box("Flank", b, new Vector3(s * 0.55f, s * 0.18f, -s * 0.30f),
                    new Vector3(s * 1.30f, s * 0.58f, s * 1.10f), m, false)
                    .transform.localRotation = Quaternion.Euler(0f, 24f, 9f);
                Box("Chip", b, new Vector3(-s * 0.62f, -s * 0.12f, s * 0.38f),
                    new Vector3(s * 0.95f, s * 0.46f, s * 0.90f), rockDark, false)
                    .transform.localRotation = Quaternion.Euler(6f, -31f, -7f);

                // 石头下的苔与草，让石头「长」在地里而不是浮在面上
                if (i % 2 == 0)
                {
                    Cyl("Moss", b, new Vector3(0f, -s * 0.44f, 0f), s * 1.35f, 0.10f, moss);
                    Ball("Tuft", b, new Vector3(-s * 0.45f, -s * 0.34f, s * 0.20f),
                        s * 0.55f, moss);
                }
            }
        }
    }
}
