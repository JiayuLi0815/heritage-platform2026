using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 遗产内容在序章里的落地：三块立牌（路口）、三座三维展台（场外）、一位讲解员（山门里）。
    ///
    /// 三件事都绕开「往场景里塞模型」：立牌是石头 + 世界空间文字板，
    /// 展台摆在 y = -800 的地底、靠图层 30 让主线相机看不见，
    /// 弘文是照着遗产站立绘拼出来的三维小人（低面数旋转体 + 方块，没有骨骼）。
    /// </summary>
    public static partial class HuaxiaBuilder
    {
        // ================================================================ 图层

        /// <summary>把 30 层命名为 HeritageShowcase，展台与主线场景靠它分家。</summary>
        public static void EnsureLayers()
        {
            const string path = "ProjectSettings/TagManager.asset";
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);
            if (objs == null || objs.Length == 0)
            {
                Debug.LogError("[HeritageUI] 读不到 " + path + "，展台会被主线相机拍进画面");
                return;
            }

            var so = new SerializedObject(objs[0]);
            var layers = so.FindProperty("layers");
            if (layers == null || layers.arraySize <= HeritageShowcase.Layer)
            {
                Debug.LogError("[HeritageUI] TagManager 里没有第 " + HeritageShowcase.Layer + " 层");
                return;
            }

            var slot = layers.GetArrayElementAtIndex(HeritageShowcase.Layer);
            if (slot.stringValue == HeritageShowcase.LayerName) return;

            if (!string.IsNullOrEmpty(slot.stringValue))
            {
                Debug.LogError("[HeritageUI] 第 " + HeritageShowcase.Layer + " 层已被「" + slot.stringValue +
                               "」占用，展台图层无法启用");
                return;
            }

            slot.stringValue = HeritageShowcase.LayerName;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[HeritageUI] 图层 " + HeritageShowcase.Layer + " → " + HeritageShowcase.LayerName);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }

        // ================================================================ 面板

        static readonly Color PanelInk = new Color(0.040f, 0.047f, 0.058f, 1f);
        static readonly Color WellInk = new Color(0.015f, 0.018f, 0.024f, 1f);
        static readonly Color PageText = new Color(0.87f, 0.85f, 0.79f, 1f);

        /// <summary>整屏遗产面板。必须挂在 HUD 画布下、且在淡入淡出层之前。</summary>
        public static HeritagePanel CreateHeritagePanel(Transform canvas)
        {
            var rootGO = new GameObject("HeritagePanel", typeof(RectTransform));
            rootGO.transform.SetParent(canvas, false);
            var rootRT = rootGO.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            var dim = rootGO.AddComponent<Image>();
            dim.color = new Color(0.012f, 0.016f, 0.022f, 0.93f);
            dim.raycastTarget = false;

            var group = rootGO.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var panel = rootGO.AddComponent<HeritagePanel>();
            panel.root = rootGO;
            panel.group = group;

            // ---------------------------------------------------------- 卡面
            var card = Panel(rootGO.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1790f, 986f), PanelInk);

            Panel(card, "Accent", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -46f),
                new Vector2(220f, 3f), Gold);

            // 左栏：卷名、题名、副题、著录、正文、看点
            panel.volume = TextAt(card, "Volume", "", 19, Gold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(54f, -60f), new Vector2(600f, 26f));
            panel.title = TextAt(card, "Title", "", 40, Cream, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(54f, -90f), new Vector2(800f, 56f), true);
            panel.subtitle = TextAt(card, "Subtitle", "", 18, MutedText, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(56f, -150f), new Vector2(800f, 26f));
            panel.meta = TextAt(card, "Meta", "", 16, MutedText, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(56f, -182f), new Vector2(800f, 52f));

            Panel(card, "Rule", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -246f),
                new Vector2(772f, 1f), HuaxiaPalette.A(Gold, 0.3f));

            panel.body = TextAt(card, "Body", "", 18, PageText, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(56f, -262f), new Vector2(772f, 520f));
            panel.keys = TextAt(card, "Keys", "", 16, MutedText, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(56f, -800f), new Vector2(772f, 120f));

            // 右栏：影像 / 三维
            var frame = Panel(card, "PhotoFrame", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(862f, -50f), new Vector2(874f, 478f), WellInk);

            panel.photo = RawStretch(frame, "Photo", new Vector2(6f, 6f), new Vector2(-6f, -6f), Color.white);
            panel.photo.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            panel.viewport = RawStretch(frame, "Viewport", new Vector2(6f, 6f), new Vector2(-6f, -6f), Color.white);
            panel.viewport.gameObject.SetActive(false);
            panel.viewHint = TextAt(panel.viewport.transform, "ViewHint", "", 14,
                HuaxiaPalette.A(HuaxiaPalette.PaperWhite, 0.7f), TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(10f, 8f), new Vector2(560f, 22f));

            panel.pager = TextAt(card, "Pager", "", 15, MutedText, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(864f, -536f), new Vector2(874f, 24f));

            panel.thumbs = new RawImage[7];
            for (int i = 0; i < panel.thumbs.Length; i++)
            {
                panel.thumbs[i] = Raw(card, "Thumb_" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(862f + i * 124f, -568f), new Vector2(118f, 66f), Color.white);
            }

            panel.btnPhoto = Hit(card, "TabPhoto", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(862f, -650f), new Vector2(120f, 40f), HuaxiaPalette.A(Gold, 0.16f),
                "影像", 17, Color.white, out panel.labelPhoto);
            panel.btnModel = Hit(card, "TabModel", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(994f, -650f), new Vector2(120f, 40f), HuaxiaPalette.A(Gold, 0.16f),
                "三维", 17, Color.white, out panel.labelModel);

            panel.btnPrev = Hit(card, "BtnPrev", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(874f, -264f), new Vector2(44f, 88f), new Color(0.02f, 0.025f, 0.03f, 0.6f),
                "‹", 40, Cream, out _);
            panel.btnNext = Hit(card, "BtnNext", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(1680f, -264f), new Vector2(44f, 88f), new Color(0.02f, 0.025f, 0.03f, 0.6f),
                "›", 40, Cream, out _);

            // 弘文：会动的头像 + 她的两行话
            var portraitFrame = Panel(card, "PortraitFrame", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(862f, -712f), new Vector2(156f, 152f), new Color(0.06f, 0.075f, 0.095f, 1f));
            panel.portrait = RawStretch(portraitFrame, "Portrait", new Vector2(4f, 4f), new Vector2(-4f, -4f), Color.white);

            panel.portraitBook = panel.portrait.gameObject.AddComponent<Flipbook>();
            panel.portraitBook.atlas = Resources.Load<Texture2D>("Heritage/hongwen_atlas_cut");
            panel.portraitBook.columns = 6;
            panel.portraitBook.rows = 6;
            panel.portraitBook.frameCount = 36;
            panel.portraitBook.fps = 14f;
            panel.portraitBook.idleFps = 9f;
            panel.portraitBook.targetImage = panel.portrait;
            panel.portraitBook.Paused = true;
            if (panel.portraitBook.atlas == null)
                Debug.LogWarning("[HeritageUI] 缺 Assets/Huaxia/Resources/Heritage/hongwen_atlas_cut.png");

            panel.speakerName = TextAt(card, "SpeakerName", "", 19, Gold, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1034f, -716f), new Vector2(560f, 26f), true);
            panel.speakerLine = TextAt(card, "SpeakerLine", "", 16, MutedText, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1034f, -748f), new Vector2(560f, 56f));

            panel.btnVoice = Hit(card, "BtnVoice", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-54f, -716f), new Vector2(150f, 40f), HuaxiaPalette.A(Gold, 0.16f),
                "♪ 重听", 16, Color.white, out _);

            panel.btnClose = Hit(card, "BtnClose", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-30f, -22f), new Vector2(150f, 44f), HuaxiaPalette.A(HuaxiaPalette.Azurite, 0.22f),
                "✕ 合上", 18, Color.white, out _);

            panel.footer = TextAt(card, "Footer", "", 15, MutedText, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1200f, 26f));

            return panel;
        }

        static RawImage Raw(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                            Vector2 pos, Vector2 size, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<RawImage>();
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        static RawImage RawStretch(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.AddComponent<RawImage>();
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// 面板里没有 EventSystem（整个项目都没有），按钮就是一整块可点矩形：
        /// HeritagePanel 用 RectangleContainsScreenPoint 自己判命中。
        /// </summary>
        static RectTransform Hit(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                                 Vector2 pos, Vector2 size, Color color, string text, int fontSize, Color textColor,
                                 out Text label)
        {
            var rt = Panel(parent, name, anchor, pivot, pos, size, color);
            label = TextStretch(rt, "Label", text, fontSize, textColor, TextAnchor.MiddleCenter,
                new Vector2(4f, 2f), new Vector2(-4f, -2f));
            return rt;
        }

        // ================================================================ 立牌

        /// <summary>三条岔路口的立牌。正面朝路口中央，玩家从山门走过来就看得见。</summary>
        public static void BuildHeritageBoards(Transform root)
        {
            var stone = Mat("HeritageBoardStone", Color.Lerp(HuaxiaPalette.Rock, HuaxiaPalette.Sand, 0.12f), 0f, 0.14f);
            var stoneDark = Mat("HeritageBoardStoneDark", HuaxiaPalette.RockDark, 0f, 0.18f);
            var bronze = Mat("HeritageBoardBronze", Color.Lerp(HuaxiaPalette.WoodDark, HuaxiaPalette.Amber, 0.32f), 0f, 0.34f);
            // 牌面要自己亮一点：黄昏的逆光里一块纯漫反射的白石板会读成脏灰
            var paper = EmissiveMat("HeritageBoardPaper", HuaxiaPalette.PaperWhite,
                HuaxiaPalette.PaperWhite * 0.34f, 0f, 0.08f);

            var group = Empty("HeritageBoards", root, Vector3.zero);
            for (int i = 0; i < HeritageArchive.All.Length; i++)
                BuildHeritageBoard(group.transform, i, stone, stoneDark, bronze, paper);
        }

        // 三岔口的动线：先看见幡柱（这儿有条路），再走到立牌前（这儿有话说），
        // 牌讲完了再一路往前到界门（这儿能进去）。三者沿路由依次拉远，
        // 不再挤在同一个半径上互相遮挡。
        const float BoardRadius = 13.0f;
        const float BoardLateral = 3.2f;

        static void BuildHeritageBoard(Transform parent, int route, Material stone, Material stoneDark,
                                       Material bronze, Material paper)
        {
            var site = HeritageArchive.All[route];

            // 立在路的外侧肩上：比幡柱远、比界门近，横向再让开 3.2 m，不压路面。
            // 三条路统一立在左手边（泉州原来在右手边，走起来和另两条不一致）。
            var dir = RouteDir(route);
            var perp = new Vector3(dir.z, 0f, -dir.x);
            var pos = dir * BoardRadius - perp * BoardLateral;
            pos.y = 0.02f;

            var g = Empty("HeritageBoard_" + site.id, parent, pos);
            var look = new Vector3(0f, 0f, -6f);
            g.transform.localRotation = Quaternion.Euler(0f,
                Mathf.Atan2(-(look.x - pos.x), -(look.z - pos.z)) * Mathf.Rad2Deg, 0f);

            // 埋在地里的石脚：山坡有起伏，不留这一段就会看到立牌悬空
            Box("Foot", g.transform, new Vector3(0f, -0.16f, 0f), new Vector3(1.94f, 0.46f, 1.02f), stoneDark, false);
            Box("Base", g.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.70f, 0.22f, 0.66f), stone);
            Box("BaseTop", g.transform, new Vector3(0f, 0.31f, 0f), new Vector3(1.46f, 0.14f, 0.54f), stone);
            Box("Body", g.transform, new Vector3(0f, 1.14f, 0f), new Vector3(1.34f, 1.54f, 0.13f), stone);
            Box("Cap", g.transform, new Vector3(0f, 1.95f, 0f), new Vector3(1.50f, 0.10f, 0.22f), bronze, false);
            Box("CapCrest", g.transform, new Vector3(0f, 2.03f, 0f), new Vector3(1.26f, 0.06f, 0.17f), bronze, false);

            // 牌面：纸白的一块，浮出石面 2 cm，文字刻在它前面 4 mm
            Box("Paper", g.transform, new Vector3(0f, 1.15f, -0.075f), new Vector3(1.12f, 1.42f, 0.02f), paper, false);
            const float Face = -0.089f;

            CreateTextPlate(g.transform, site.name, new Vector3(0f, 1.70f, Face), Vector3.zero,
                new Vector2(1.04f, 0.22f), site.name.Length <= 7 ? 60 : 40, HuaxiaPalette.InkTealDeep, true);

            // 竖排的卷名（水与玉 / 帝与陵 / 港与海）：三字一列，是这块牌上最先被读到的东西
            string tail = site.volume;
            int dot = tail.IndexOf('·');
            if (dot >= 0 && dot + 1 < tail.Length) tail = tail.Substring(dot + 1).Trim();
            string vertical = string.Empty;
            for (int i = 0; i < tail.Length; i++)
                vertical += (i == 0 ? "" : "\n") + tail[i];

            var plate = CreateTextPlate(g.transform, vertical, new Vector3(0.32f, 1.15f, Face), Vector3.zero,
                new Vector2(0.34f, 0.86f), 320, HuaxiaPalette.InkTealDeep, true);
            var vt = plate.GetComponentInChildren<Text>();
            if (vt != null)
            {
                vt.verticalOverflow = VerticalWrapMode.Overflow;
                vt.lineSpacing = 0.96f;
            }

            // 一句对句，就按文案里那个全角空档断成两行：上句写年代，下句写主角。
            // 别交给自动换行——中英混排下它会按像素宽度把字切成「两千年前 / 的土」这种半句
            string couplet = site.tagline.Replace("\u3000", "\n");
            // 字号卡在 78：这块画布横向只有 1000 单位，中文字宽约等于字号，78 刚好一行六个字
            CreateTextPlate(g.transform, couplet, new Vector3(-0.17f, 1.24f, Face), Vector3.zero,
                new Vector2(0.70f, 0.34f), 78, HuaxiaPalette.InkTealDeep);

            CreateTextPlate(g.transform, site.code, new Vector3(0f, 0.72f, Face), Vector3.zero,
                new Vector2(1.0f, 0.13f), 22, HuaxiaPalette.WoodDark);
            CreateTextPlate(g.transform, "［E］细　看", new Vector3(0f, 0.55f, Face), Vector3.zero,
                new Vector2(0.80f, 0.15f), 50, HuaxiaPalette.Cinnabar, true);

            // 牌前一小盏暖光：夜里走近才看得清纸面，光又不会泼到路上
            AddPoint(g.transform, "BoardLight", new Vector3(0f, 1.55f, -0.95f), HuaxiaPalette.Amber, 3.4f, 0.5f);

            var it = g.AddComponent<Interactable>();
            it.kind = InteractKind.Info;
            it.prompt = "［E］细看这块立牌";
            it.heritageId = site.id;
            it.board = true;
            it.tier = SourceTier.Confirmed;
            it.hideTier = true;

            var trigger = g.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.25f;
            trigger.center = new Vector3(0f, 1.15f, 0f);

            // 牌前地上的引导圈：玩家走进去她才走过来开口。
            // 一处只放一只圈 —— 圈是「站到这里」的落脚点，不是一路铺过去的路标；
            // 牌讲完她把人往前带几步、抬手一指，下一处该去哪自己就看得见了。
            BuildGuideRing(parent, g.transform, site.id);
        }

        // ================================================================ 引导圈

        /// <summary>
        /// 牌前地上那只发光的圈。两个用途：告诉玩家「站到这里」，
        /// 以及踩进去时叫醒弘文 —— 她走过来、招手，然后讲这一处。
        /// 位置取牌面正前方（牌朝本地 -Z），落在坡上时按射线找地面。
        /// </summary>
        public static GuideRing BuildGuideRing(Transform parent, Transform board, string heritageId,
                                               float distance = 2.6f, float radius = 1.15f)
        {
            Vector3 fwd = board.rotation * Vector3.back;
            Vector3 pos = board.position + fwd * distance;
            // 广场石墁没有碰撞体，射线只打到下面的地形（比石墁低 4–5 cm），
            // 圈会被石墁埋掉；所以把铺装顶面也算进来，再留一点余量。
            float floorY = FloorYAt(pos, board.position.y);
            pos.y = floorY + 0.04f;

            var g = Empty("GuideRing_" + heritageId, parent, pos);
            // 圈的大小按半径等比缩：牌前一只 1.15 m，弘文脚下那只 2 m
            g.transform.localScale = Vector3.one * (radius / 1.15f);

            // 只画一圈：套两个同心圆读起来像准星，不像「站到这里」。
            // 用加法柔光材质，压到三成出头，圈是一层透出来的光，石板纹理还在底下。
            var glow = GuideGlowMat("GuideRingGlow", HuaxiaPalette.Amber, 0.40f, 1.45f, 0.22f, 0.5f);
            var band = MeshObject("Ring", g.transform, Vector3.zero,
                MeshFactory.PlateRing(0.90f, 1.20f, 64), glow, false, false);
            band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var ring = g.AddComponent<GuideRing>();
            ring.heritageId = heritageId;
            ring.radius = radius;
            ring.board = board;
            ring.standGap = 1.25f;
            return ring;
        }

        /// <summary>建场景时找地面高度；找不到就用立牌自己的 y。</summary>
        static float GroundYAt(Vector3 pos, float fallback)
        {
            if (Physics.Raycast(pos + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 120f, ~0,
                                QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return fallback;
        }

        static Renderer[] pavement;

        /// <summary>
        /// 这一处地面（连铺装一起）的顶面高度。广场石墁、甬道石板都没有碰撞体，
        /// 射线只能打到地形，画在铺装上的圈会被埋掉几厘米，所以再量一遍铺装的包围盒。
        /// </summary>
        static float FloorYAt(Vector3 pos, float fallback)
        {
            float y = Mathf.Max(GroundYAt(pos, fallback), fallback);
            if (pavement == null) CollectPavement();
            if (pavement == null) return y;

            for (int i = 0; i < pavement.Length; i++)
            {
                var b = pavement[i].bounds;
                if (pos.x < b.min.x || pos.x > b.max.x) continue;
                if (pos.z < b.min.z || pos.z > b.max.z) continue;
                if (b.max.y > y) y = b.max.y;
            }
            return y;
        }

        static void CollectPavement()
        {
            var all = Object.FindObjectsOfType<Renderer>();
            var list = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].gameObject.name;
                if (n.StartsWith("PathBase_") || n.StartsWith("Slab_") || n == "Court_N") list.Add(all[i]);
            }
            if (list.Count > 0) pavement = list.ToArray();
        }

        // ================================================================ 展台

        /// <summary>
        /// 三座微缩展台，摆在 y = -800 的地底空场：主线相机剔掉 30 层，看不见它们；
        /// 面板切到「三维」时自己开一台只拍 30 层的相机渲到 RenderTexture 上。
        /// </summary>
        public static void BuildHeritageShowcases(Transform root)
        {
            var lot = Empty("HeritageShowcases", root, Vector3.zero);
            var baseMat = Mat("ShowcaseBase", Color.Lerp(HuaxiaPalette.RockDark, HuaxiaPalette.InkTealDeep, 0.3f), 0f, 0.24f);
            var rimMat = EmissiveMat("ShowcaseRim", HuaxiaPalette.Amber,
                HuaxiaPalette.Amber * 0.42f, 0f, 0.5f);

            for (int i = 0; i < HeritageArchive.All.Length; i++)
            {
                var site = HeritageArchive.All[i];
                // 没有三维展台的卷（良渚的立牌只留影像）就不必在 y = -800 铺展位
                if (!site.HasStage) continue;
                var g = Empty("Showcase_" + site.id, lot.transform, new Vector3(i * 6f, -800f, 0f));

                var showcase = g.AddComponent<HeritageShowcase>();
                showcase.siteId = site.id;
                showcase.cameraOffset = new Vector3(0f, 1.05f, -2.85f);
                showcase.cameraTarget = new Vector3(0f, 0.72f, 0f);
                showcase.hint = "按住左键拖动 · 绕着看";

                Cyl("Base", g.transform, new Vector3(0f, 0.03f, 0f), 1.02f, 0.06f, baseMat);
                var rim = MeshObject("BaseRing", g.transform, new Vector3(0f, 0.062f, 0f),
                    MeshFactory.PlateRing(0.96f, 1.02f, 48), rimMat, false, false);
                rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                var turn = Empty("Turntable", g.transform, Vector3.zero);
                showcase.turntable = turn.transform;

                switch (site.model)
                {
                    case "1592": BuildLiangzhuRelics(turn.transform); break;
                    case "441": BuildQinTerracottaRow(turn.transform); break;
                    default: BuildQuanzhouPagoda(turn.transform); break;
                }

                // 展台自己的三盏灯：AddPoint 不改 cullingMask，这里必须自己建
                StageLight(g.transform, new Vector3(-1.7f, 2.3f, -2.0f), new Color(1f, 0.95f, 0.86f), 6f, 2.2f);
                StageLight(g.transform, new Vector3(2.0f, 1.4f, -1.4f), new Color(0.60f, 0.72f, 0.92f), 5f, 0.9f);
                StageLight(g.transform, new Vector3(0f, 1.7f, 1.9f), new Color(1f, 0.86f, 0.62f), 5f, 1.2f);

                SetLayerRecursive(g, HeritageShowcase.Layer);
                g.SetActive(false);
            }
        }

        static void StageLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject("StageLight", typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << HeritageShowcase.Layer;
        }

        /// <summary>良渚：正面立着一枚玉璧（28 段石片拼成环），旁边一方玉琮。</summary>
        static void BuildLiangzhuRelics(Transform t)
        {
            var jade = Mat("ShowcaseJade", new Color(0.66f, 0.73f, 0.62f), 0f, 0.44f);
            var jadeDark = Mat("ShowcaseJadeDark", new Color(0.46f, 0.54f, 0.46f), 0f, 0.36f);

            const int seg = 28;
            const float rMid = 0.335f;
            const float band = 0.33f;
            const float depth = 0.055f;
            float span = 2f * Mathf.PI * rMid / seg * 1.2f;

            for (int k = 0; k < seg; k++)
            {
                float rad = k * Mathf.PI * 2f / seg;
                var p = new Vector3(Mathf.Cos(rad) * rMid, Mathf.Sin(rad) * rMid + 0.70f, 0f);
                var piece = Box("Bi_" + k, t, p, new Vector3(span, band, depth), k % 2 == 0 ? jade : jadeDark, false);
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, rad * Mathf.Rad2Deg + 90f);
            }

            // 玉琮：外方内圆，四面墙围出一个上下贯通的方孔
            var cong = Empty("Cong", t, new Vector3(-0.68f, 0f, 0.22f));
            const float half = 0.085f;
            const float wall = 0.05f;
            const float h = 0.56f;
            float cy = 0.06f + h * 0.5f;
            for (int s = 0; s < 2; s++)
            {
                float sign = s == 0 ? 1f : -1f;
                Box("WallX", cong.transform, new Vector3(sign * half, cy, 0f), new Vector3(wall, h, 0.22f), jadeDark, false);
                Box("WallZ", cong.transform, new Vector3(0f, cy, sign * half), new Vector3(0.22f, h, wall), jadeDark, false);
            }
            Box("CongRim", cong.transform, new Vector3(0f, 0.06f + h + 0.02f, 0f), new Vector3(0.24f, 0.04f, 0.24f), jade, false);
        }

        /// <summary>秦陵：下载来的兵马俑 FBX（减面到 2.2 万面的整排俑），换成展台上的一小排。</summary>
        static void BuildQinTerracottaRow(Transform t)
        {
            var prefab = TerracottaRow();
            if (prefab == null)
            {
                // FBX 没进工程时退回原来的程序化单俑，至少展台不空
                BuildQinWarrior(t);
                return;
            }
            var row = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            row.name = "TerracottaRow";
            row.transform.SetParent(t, false);
            // 整排底宽 2.93 m、单俑高 1.0 m；缩到展台上约 1.3 m 宽
            const float s = 0.44f;
            row.transform.localPosition = new Vector3(0f, 0.065f, 0.06f);
            row.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            row.transform.localScale = new Vector3(s, s, s);
        }

        static GameObject terracottaRowCache;

        /// <summary>兵马俑整排 FBX（Resources 里没有就返回 null）。</summary>
        public static GameObject TerracottaRow()
        {
            if (terracottaRowCache != null) return terracottaRowCache;
            terracottaRowCache = Resources.Load<GameObject>("Heritage/Models/qin_terracotta_row");
            if (terracottaRowCache == null)
                Debug.LogWarning("[HeritageUI] 缺少 Resources/Heritage/Models/qin_terracotta_row.fbx，俑坑退回程序化陶俑");
            return terracottaRowCache;
        }

        /// <summary>秦陵：一尊陶俑（袍、胸、肩、头、发髻、双臂、手、腰带）。</summary>
        static void BuildQinWarrior(Transform t)
        {
            var clay = Mat("ShowcaseClay", new Color(0.60f, 0.43f, 0.33f), 0f, 0.18f);
            var clayDark = Mat("ShowcaseClayDark", new Color(0.46f, 0.32f, 0.25f), 0f, 0.14f);

            Box("Robe", t, new Vector3(0f, 0.42f, 0f), new Vector3(0.36f, 0.72f, 0.24f), clay, false);
            Box("Belt", t, new Vector3(0f, 0.72f, 0f), new Vector3(0.38f, 0.07f, 0.26f), clayDark, false);
            Box("Torso", t, new Vector3(0f, 0.95f, 0f), new Vector3(0.42f, 0.34f, 0.26f), clay, false);
            Box("Shoulder", t, new Vector3(0f, 1.11f, 0f), new Vector3(0.50f, 0.10f, 0.28f), clay, false);
            Cyl("Neck", t, new Vector3(0f, 1.19f, 0f), 0.062f, 0.10f, clayDark);

            var head = Ball("Head", t, new Vector3(0f, 1.28f, 0f), 0.10f, clay);
            head.transform.localScale = new Vector3(0.19f, 0.22f, 0.20f);
            Ball("Hair", t, new Vector3(0.015f, 1.40f, 0.015f), 0.055f, clayDark);

            for (int s = 0; s < 2; s++)
            {
                float sign = s == 0 ? 1f : -1f;
                var arm = Box("Arm", t, new Vector3(sign * 0.245f, 0.92f, 0f), new Vector3(0.11f, 0.42f, 0.14f), clay, false);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, sign * -5f);
                Box("Hand", t, new Vector3(sign * 0.252f, 0.69f, 0.02f), new Vector3(0.09f, 0.10f, 0.12f), clayDark, false);
            }
        }

        /// <summary>泉州：五层石塔（塔基 + 五层身檐 + 塔刹）。</summary>
        static void BuildQuanzhouPagoda(Transform t)
        {
            var granite = Mat("ShowcaseGranite", new Color(0.64f, 0.62f, 0.57f), 0f, 0.12f);
            var graniteDark = Mat("ShowcaseGraniteDark", new Color(0.48f, 0.47f, 0.44f), 0f, 0.10f);

            Box("Plinth", t, new Vector3(0f, 0.12f, 0f), new Vector3(0.50f, 0.12f, 0.50f), graniteDark, false);

            float y = 0.18f;
            for (int k = 0; k < 5; k++)
            {
                float w = 0.34f - k * 0.028f;
                Box("Storey" + k, t, new Vector3(0f, y + 0.08f, 0f), new Vector3(w, 0.16f, w), granite, false);
                y += 0.16f;
                Box("Eave" + k, t, new Vector3(0f, y + 0.022f, 0f), new Vector3(w + 0.16f, 0.045f, w + 0.16f), graniteDark, false);
                y += 0.045f;
            }

            Cyl("Spire", t, new Vector3(0f, y + 0.07f, 0f), 0.035f, 0.14f, graniteDark);
            Ball("Pearl", t, new Vector3(0f, y + 0.17f, 0f), 0.045f, granite);
        }

        // ================================================================ 弘文

        /// <summary>
        /// 数智讲解员「弘文」。人像是用户自己做的 FBX（白针织衫、蓝牛仔、低马尾、
        /// 胸前一枚蓝徽），带骨骼和十段动画；这里只负责把她摆进山门、调好尺度和灯。
        /// 走路、挥手、讲话手势、指路、庆祝都由 DigitalHuman 按剧情编排。
        /// </summary>
        public static DigitalHuman CreateDigitalHuman(Transform root, Vector3 pos, float yaw)
        {
            var g = Empty("Hongwen", root, pos);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var visual = Empty("Visual", g.transform, Vector3.zero);
            var body = Empty("Body", visual.transform, Vector3.zero);
            var head = BuildHongwenFigure(body.transform);
            var anim = body.GetComponentInChildren<Animation>(true);

            // 脚下那只铜环：她还是位「数智」讲解员，总得有个落点，
            // 不然一身白衣服站在暮色里会像浮着的
            var ringMat = EmissiveMat("HongwenRing", HuaxiaPalette.Amber, HuaxiaPalette.Amber * 0.40f, 0f, 0.5f);
            var ring = MeshObject("Halo", g.transform, new Vector3(0f, 0.03f, 0f),
                MeshFactory.PlateRing(0.44f, 0.51f, 48), ringMat, false, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreateWorldLabel(g.transform, "弘文 · 数智讲解员", new Vector3(0f, 2.00f, 0f),
                HuaxiaPalette.A(HuaxiaPalette.AmberPale, 0.92f), 1.55f);

            // 暮色里五官全靠这点暖光才看得清，灯放在斜前方，别把脸打平
            AddPoint(g.transform, "HongwenLight", new Vector3(0.34f, 1.74f, -1.55f),
                new Color(1f, 0.90f, 0.78f), 3.8f, 1.05f);

            var dh = g.AddComponent<DigitalHuman>();
            dh.visual = visual.transform;
            dh.body = body.transform;
            dh.head = head;
            dh.anim = anim;
            dh.faceRadius = 11f;
            dh.bobAmplitude = 0f;      // 有骨骼动画了，别再加一层手动呼吸
            dh.bobSpeed = 1.15f;
            dh.walkSeconds = 3.4f;     // 立牌前指路只走三四步，别让玩家干等
            dh.walkSpeed = 1.15f;
            dh.hurrySpeed = 2.1f;      // 她站在山门里的说明牌前，玩家在广场那头踩圈时得赶一段
            if (Resources.Load<AudioClip>("Heritage/Audio/intro") == null)
                Debug.LogWarning("[HeritageUI] 缺 Heritage/Audio/intro.wav");
            if (anim == null) Debug.LogWarning("[HeritageUI] 弘文身上没有 Animation 组件");

            return dh;
        }

        // ================================================================ 弘文的人像本体

        const string HongwenModelPath = "Heritage/DigitalHuman/Hongwen";

        /// <summary>FBX 原始模型从鞋底到头顶约 1.09 个单位（HongwenImport.Measure 量的）。</summary>
        const float HongwenSourceHeight = 1.09f;

        /// <summary>场景里她该有多高：比牌坊的人矮一点，站在 1.70 m 的玩家面前正好平视。</summary>
        const float HongwenHeight = 1.70f;

        /// <summary>
        /// 人像本体：实例化 Resources/Heritage/DigitalHuman/Hongwen.fbx——用户自己做的模型，
        /// 51 根骨骼、十段 Legacy 动画（walk / greet_01 / greet_02 / agree / wait /
        /// look_around / fold_arms / clap / cheer / dig）。
        ///
        /// 相对这个物体：面朝 -Z（Tripo 导出正面朝 +Z，所以转 180°），鞋底落在 y = 0，头顶约 1.70 m。
        /// 返回头骨（留给以后做注视，现在动作全部交给剪辑）。
        /// </summary>
        static Transform BuildHongwenFigure(Transform parent)
        {
            var prefab = Resources.Load<GameObject>(HongwenModelPath);
            if (prefab == null)
            {
                Debug.LogError("[HeritageUI] 找不到 Resources/" + HongwenModelPath + ".fbx，弘文没模型可用");
                return parent;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "HongwenModel";
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            float s = HongwenHeight / HongwenSourceHeight;
            go.transform.localScale = Vector3.one * s;
            // 原始模型鞋底沉在原点下 0.078，抬回来才踩得实
            go.transform.localPosition = new Vector3(0f, 0.078f * s, 0f);

            var anim = go.GetComponent<Animation>();
            if (anim == null) anim = go.AddComponent<Animation>();
            anim.playAutomatically = false;
            anim.wrapMode = WrapMode.ClampForever;
            // 按渲染器可见性剔除时，人一离开视野动画就冻住（CrossFade 也切不过去），她得一直算
            anim.cullingType = AnimationCullingType.AlwaysAnimate;
            var clips = Resources.LoadAll<AnimationClip>(HongwenModelPath);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null || string.IsNullOrEmpty(clips[i].name)) continue;
                if (anim.GetClip(clips[i].name) != null) continue;
                anim.AddClip(clips[i], clips[i].name);
            }
            if (anim.GetClipCount() == 0)
                Debug.LogError("[HeritageUI] " + HongwenModelPath + " 没导入到动画剪辑，检查 HongwenImport.Configure");

            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin != null)
            {
                // 暮色里的山谷会把她照得发灰，给一层很淡的自发光把白衣和白脸提起来
                var mats = skin.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    mats[i].EnableKeyword("_EMISSION");
                    mats[i].SetColor("_EmissionColor", new Color(0.10f, 0.088f, 0.075f));
                    mats[i].SetFloat("_Glossiness", 0.16f);
                }
                skin.shadowCastingMode = ShadowCastingMode.On;
                skin.receiveShadows = true;
            }

            Transform head = null;
            var bones = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].name.ToLowerInvariant().Contains("head")) { head = bones[i]; break; }
            }
            return head != null ? head : go.transform;
        }
    }
}
