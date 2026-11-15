using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 场景不变量自检。改完场景生成代码跑一遍，比「我看了一眼预览图」靠谱。
    /// 只读，不改任何东西；命令行：
    ///   Unity -batchmode -projectPath . -executeMethod Huaxia.EditorTools.SceneSelfCheck.Run -logFile /tmp/selfcheck.log
    /// </summary>
    public static class SceneSelfCheck
    {
        static int pass;
        static int fail;

        [MenuItem("Huaxia/自检：场景不变量")]
        public static void Run()
        {
            pass = 0;
            fail = 0;

            CheckArchive();
            CheckHeritage();
            CheckSite("Liangzhu", "liangzhu", 4);
            CheckSite("Qinling", "qinling", 4);
            CheckSite("Quanzhou", "quanzhou", 3);
            CheckEpilogue();

            CheckBookPrefab();

            Debug.Log("[SelfCheck] " + (fail == 0 ? "通过" : "失败") + "　pass=" + pass + " fail=" + fail);
            EditorApplication.Exit(fail == 0 ? 0 : 1);
        }

        static void CheckArchive()
        {
            Open("Archive");
            var portals = Object.FindObjectsOfType<ScenePortal>();
            Expect("Archive 界门数量 = 4", portals.Length == 4, "实际 " + portals.Length);

            foreach (var p in portals)
                Expect("Archive 界门「" + p.sceneToLoad + "」有黑场字幕",
                    !string.IsNullOrEmpty(p.destinationLabel), "destinationLabel 为空");

            ExpectSpawn("Archive", "start");
            ExpectSpawn("Archive", "after_epilogue");
            ExpectSpawn("Archive", "back_liangzhu");
            ExpectSpawn("Archive", "back_qinling");
            ExpectSpawn("Archive", "back_quanzhou");

            var hub = FindPortal(portals, "Liangzhu", "from_hub");
            Expect("Archive 良渚界门落在 from_hub", hub != null, "");
            var hall = FindPortal(portals, "Epilogue", null);
            Expect("Archive 藏卷阁门接终章", hall != null, "");
            if (hall != null)
                Expect("藏卷阁门要三卷齐才开", hall.requireAllChapters, "");

            // 正门：得真有一个门洞和两扇能转的门，不能只是贴在墙上的画
            Expect("藏卷阁正面墙让出门洞",
                GameObject.Find("Wall_F_L") != null && GameObject.Find("Wall_F_R") != null &&
                GameObject.Find("Wall_F") == null, "");
            var hallDoor = Object.FindObjectOfType<HallDoor>();
            Expect("藏卷阁挂了正门 HallDoor", hallDoor != null, "");
            if (hallDoor != null)
            {
                Expect("藏卷阁门两扇都在",
                    hallDoor.leftHinge != null && hallDoor.rightHinge != null &&
                    hallDoor.leftHinge.childCount > 0 && hallDoor.rightHinge.childCount > 0, "");
                Expect("藏卷阁门开角 ≥ 60°", hallDoor.openAngle >= 60f, "实际 " + hallDoor.openAngle);
            }

            CheckHallApproach();

            var hud = Object.FindObjectOfType<UIManager>();
            Expect("Archive HUD 提示滚轮翻页",
                hud != null && hud.hintText != null && hud.hintText.text.Contains("滚轮翻页"),
                hud != null && hud.hintText != null ? hud.hintText.text : "没有 HUD");

            // 进门不弹整屏说明（11.11 后改为：走进引导圈，由弘文当场讲开场白）
            Expect("Archive 进门没有整屏弹窗", Object.FindObjectOfType<SceneIntro>() == null, "还有 SceneIntro");

            Expect("Archive HUD 有方向引导", hud != null && hud.guideText != null, "");
        }

        /// <summary>
        /// 藏卷阁的踏跺：从院子里沿 x=0 一路走到门槛，每 0.25 m 取最高的可站立面。
        /// 相邻高差超过 stepOffset（0.35 m）就是一道迈不过去的台阶——台基那几级
        /// 0.44 的台阶就是这样把人卡在院子里的。
        /// </summary>
        static void CheckHallApproach()
        {
            // 先确认射线真的打在台面上：不然全程采样 -999 也会「通过」
            float ground = TopOfAny(0f, 33f);
            float terrace = TopOfAny(0f, 40.5f);
            Expect("藏卷阁院子与台面都取得到（射线没打空）",
                ground > -1f && ground < 0.6f && Mathf.Abs(terrace - 1.56f) < 0.10f,
                "院子 " + ground.ToString("0.00") + "，台面 " + terrace.ToString("0.00"));

            float worst = 0f, worstZ = 0f;
            float prev = TopOfAny(0f, 32f);
            for (float z = 32.25f; z <= 41f; z += 0.25f)
            {
                float h = TopOfAny(0f, z);
                if (h > -900f && prev > -900f)
                {
                    float step = Mathf.Abs(h - prev);
                    if (step > worst) { worst = step; worstZ = z; }
                }
                prev = h;
            }
            Expect("藏卷阁踏跺能走上去（相邻 0.25 m 高差 ≤ 0.30 m）", worst <= 0.30f,
                "最大 " + worst.ToString("0.00") + " m @ z=" + worstZ.ToString("0.0"));
        }

        static float TopOfAny(float x, float z)
        {
            float top = -999f;
            foreach (var hit in Physics.RaycastAll(new Vector3(x, 60f, z), Vector3.down, 120f))
                if (hit.collider != null && !hit.collider.isTrigger)
                    top = Mathf.Max(top, hit.point.y);
            return top;
        }

        static void CheckSite(string scene, string chapterId, int infoCount)
        {
            Open(scene);
            var portals = Object.FindObjectsOfType<ScenePortal>();
            Expect(scene + " 有回山谷的界石", portals.Length >= 1, "实际 " + portals.Length);

            bool back = false;
            var spawn = SpawnPoint.Find("from_hub");
            foreach (var p in portals)
            {
                if (p.sceneToLoad != "Archive" || p.spawnId != "back_" + chapterId) continue;
                back = true;
                if (spawn != null)
                {
                    // 落地就在感应圈里的话，一进场就被「回山谷」顶住，还可能顺手退回去
                    float d = Vector3.Distance(spawn.transform.position, p.transform.position);
                    Expect(scene + " 界石离落点 > 感应半径 + 0.5 m",
                        d > p.radius + 0.5f, "实际 " + d.ToString("0.00") + " m，半径 " + p.radius);
                }
            }
            Expect(scene + " 界石落在 back_" + chapterId, back, "");
            ExpectSpawn(scene, "from_hub");

            var points = Object.FindObjectsOfType<Interactable>();
            int info = 0;
            bool footerOk = true;
            foreach (var i in points)
                if (i.kind == InteractKind.Info) info++;
            Expect(scene + " 线索点数量 = " + infoCount, info == infoCount, "实际 " + info);

            var tasks = Object.FindObjectOfType<TaskManager>();
            if (tasks != null)
                footerOk = tasks.archiveFooter.Contains("空格") && !tasks.archiveFooter.Contains("[E]");
            Expect(scene + " 文明档案确认键写的是空格", footerOk, "");

            Expect(scene + " 有进场说明", tasks != null && !string.IsNullOrEmpty(tasks.introBody), "");

            var hud = Object.FindObjectOfType<UIManager>();
            Expect(scene + " HUD 写明 Tab 取册子",
                hud != null && hud.hintText != null && hud.hintText.text.Contains("Tab 取册子"), "");
            Expect(scene + " HUD 有方向引导", hud != null && hud.guideText != null, "");
            Expect(scene + " HUD 挂了 ObjectiveGuide",
                hud != null && hud.GetComponent<ObjectiveGuide>() != null, "");

            if (scene == "Qinling") CheckQinlingDoor();
            if (scene == "Liangzhu") CheckLiangzhuTraversal();
        }

        /// <summary>
        /// 良渚能不能真的走进去：两座桥要有碰撞体、桥面要接得上路，
        /// 城门门扇要敞开得下人，宫城台地的坡不能被 slopeLimit 挡住。
        /// </summary>
        static void CheckLiangzhuTraversal()
        {
            CheckBridge("南河木桥", new Vector3(0f, 0f, -44f), 0.15f);
            CheckBridge("城内木桥", new Vector3(0f, 0f, 14.5f), 1.20f);
            CheckBridgeLanding("南河木桥", -44f, 5.3f);
            CheckBridgeLanding("城内木桥", 14.5f, 3.8f);
            CheckWalkLine();

            int leaves = 0;
            float minOpen = 999f;
            foreach (var t in Object.FindObjectsOfType<Transform>())
            {
                if (t.name != "DoorLeaf") continue;
                leaves++;
                float yaw = Mathf.Abs(Mathf.DeltaAngle(0f, t.localEulerAngles.y));
                minOpen = Mathf.Min(minOpen, yaw);
            }
            Expect("良渚城门两扇门扇都在", leaves == 2, "实际 " + leaves);
            Expect("良渚城门门扇敞开 ≥ 70°（中间过人）", leaves == 2 && minOpen >= 70f,
                "最小 " + minOpen.ToString("0") + "°");

            float worstSlope = 0f;
            float worstZ = 0f;
            float prev = HuaxiaBuilder.LiangzhuHeightProbe(0f, -56f);
            for (float z = -55.75f; z <= 21f; z += 0.25f)
            {
                float h = HuaxiaBuilder.LiangzhuHeightProbe(0f, z);
                float slope = Mathf.Atan2(Mathf.Abs(h - prev), 0.25f) * Mathf.Rad2Deg;
                if (slope > worstSlope) { worstSlope = slope; worstZ = z; }
                prev = h;
            }
            Expect("良渚主轴最大坡度 ≤ 45°（人能走上去）", worstSlope <= 45f,
                worstSlope.ToString("0.0") + "° @ z=" + worstZ.ToString("0.0"));

            // 线索点必须落在地面上：以前 ③ 立在反山土丘顶上、① 在城西码头边，
            // 高度对不上地形，人走过去什么也没碰到，等于「一个点位都没有」。
            var infoPoints = new System.Collections.Generic.List<Interactable>();
            foreach (var i in Object.FindObjectsOfType<Interactable>())
                if (i.kind == InteractKind.Info) infoPoints.Add(i);

            Expect("良渚线索点都在（3 处点位 + 1 处合卷）", infoPoints.Count == 4, "实际 " + infoPoints.Count);
            foreach (var i in infoPoints)
            {
                Vector3 p = i.transform.position;
                float ground = HuaxiaBuilder.LiangzhuHeightProbe(p.x, p.z);
                Expect("良渚「" + p.ToString("0.0") + "」的点位踩在地形上",
                    Mathf.Abs(p.y - ground) <= 0.50f,
                    "点位 y=" + p.y.ToString("0.00") + "，地形 " + ground.ToString("0.00"));

                // 也要在城墙范围里、离出生点够近：摆到城外去，玩家一辈子走不到
                bool inside = p.x > -34f && p.x < 34f && p.z > -61f && p.z < 58f;
                Expect("良渚「" + p.ToString("0.0") + "」的点位在城里", inside, "");
            }
        }

        /// <summary>
        /// 桥两头都要落在平地上：桥面端点往里 0.05 m 采样桥板，
        /// 往外 0.20 m 采样地面。高差超过 stepOffset（0.35 m）就会卡在桥头，
        /// 也就是「几个断的位置」里最要命的那一处。
        /// </summary>
        static void CheckBridgeLanding(string name, float centerZ, float halfLength)
        {
            float deckS = TopAt(centerZ - halfLength + 0.05f, "DeckCollider");
            float landS = TopAt(centerZ - halfLength - 0.20f, "Terrain");
            float deckN = TopAt(centerZ + halfLength - 0.05f, "DeckCollider");
            float landN = TopAt(centerZ + halfLength + 0.20f, "Terrain");

            bool found = deckS > -900f && landS > -900f && deckN > -900f && landN > -900f;
            string detail = "南 桥" + deckS.ToString("0.00") + "/岸" + landS.ToString("0.00") +
                            "，北 桥" + deckN.ToString("0.00") + "/岸" + landN.ToString("0.00");
            Expect("良渚" + name + "两头都接得上岸", found, detail);
            if (!found) return;

            float worst = Mathf.Max(Mathf.Abs(deckS - landS), Mathf.Abs(deckN - landN));
            Expect("良渚" + name + "两端高差 ≤ 0.30 m（迈得上去）", worst <= 0.30f,
                "最大 " + worst.ToString("0.00") + " m（" + detail + "）");
        }

        /// <summary>
        /// 从南岸一路走到莫角山坡脚，沿 x=0 每 0.25 m 取「脚下最高的可站立面」
        /// （地形或桥板），相邻两点高差超过 stepOffset 就是一道迈不过去的台阶。
        /// 这比只看地形高度更接近实际——桥面、路板、台地坡都在里面。
        /// </summary>
        static void CheckWalkLine()
        {
            float worst = 0f;
            float worstZ = 0f;
            float prev = TopOfWalk(0f, -55f);
            for (float z = -54.75f; z <= 21f; z += 0.25f)
            {
                float h = TopOfWalk(0f, z);
                if (h > -900f && prev > -900f)
                {
                    float step = Mathf.Abs(h - prev);
                    if (step > worst) { worst = step; worstZ = z; }
                }
                prev = h;
            }
            Expect("良渚主轴全程无台阶（相邻 0.25 m 高差 ≤ 0.30 m）", worst <= 0.30f,
                "最大 " + worst.ToString("0.00") + " m @ z=" + worstZ.ToString("0.0"));
        }

        static float TopOfWalk(float x, float z)
        {
            float top = -999f;
            foreach (var hit in Physics.RaycastAll(new Vector3(x, 60f, z), Vector3.down, 120f))
                if (hit.collider != null &&
                    (hit.collider.name == "Terrain" || hit.collider.name == "DeckCollider"))
                    top = Mathf.Max(top, hit.point.y);
            return top;
        }

        static float TopAt(float z, string colliderName)
        {
            float top = -999f;
            foreach (var hit in Physics.RaycastAll(new Vector3(0f, 60f, z), Vector3.down, 120f))
                if (hit.collider != null && hit.collider.name == colliderName)
                    top = Mathf.Max(top, hit.point.y);
            return top;
        }

        static void CheckBridge(string name, Vector3 at, float expectTop)
        {
            bool found = false;
            float top = -999f;
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 60f, Vector3.down, 120f))
            {
                if (hit.collider == null || hit.collider.name != "DeckCollider") continue;
                found = true;
                top = Mathf.Max(top, hit.point.y);
            }
            Expect("良渚" + name + "有可走的碰撞桥面", found, "");
            if (found)
                Expect("良渚" + name + "桥面与路齐平（±0.15 m）", Mathf.Abs(top - expectTop) <= 0.15f,
                    "顶面 " + top.ToString("0.00") + " m");
        }

        /// <summary>第二场那道门：走过去要自己开，而且要开得比门洞窄的那道缝大。</summary>
        static void CheckQinlingDoor()
        {
            var door = Object.FindObjectOfType<AutoDoor>();
            Expect("Qinling 城门挂了 AutoDoor", door != null, "");
            if (door == null) return;

            Expect("Qinling 城门两扇门扇都在", door.hinges != null && door.hinges.Length == 2,
                door.hinges != null ? "实际 " + door.hinges.Length : "hinges 为空");
            Expect("Qinling 城门开角 ≥ 60°", door.openAngle >= 60f, "实际 " + door.openAngle);
            Expect("Qinling 城门走近就开（触发 ≥ 5 m）", door.triggerDistance >= 5f,
                "实际 " + door.triggerDistance);
        }

        static void CheckEpilogue()
        {
            Open("Epilogue");
            var end = Object.FindObjectOfType<Interactable>();
            Expect("终章有合书的互动点", end != null && end.endScene == "Archive", "");
        }

        static void CheckBookPrefab()
        {
            var prefab = Resources.Load<GameObject>("HandBook");
            Expect("Resources/HandBook.prefab 存在", prefab != null, "");
            if (prefab == null) return;

            var book = prefab.GetComponent<HandBook>();
            Expect("书上有 HandBook 组件", book != null, "");
            if (book == null) return;

            Expect("书的首页有卷名", book.leaves != null && book.leaves.Length >= 4,
                book.leaves != null ? "叶数 " + book.leaves.Length : "leaves 为空");
        }

        /// <summary>
        /// 序章里的遗产区：三块立牌、三座展台、一位讲解员。这些都不进 Build 报告，
        /// 断了也没人喊，所以在这里钉死。
        /// </summary>
        static void CheckHeritage()
        {
            // 数据本身：三处遗产、各自有正文、图、语音、模型
            var sites = HeritageArchive.All;
            Expect("遗产数据共三处", sites.Length == 3, "实际 " + sites.Length);
            for (int i = 0; i < sites.Length; i++)
            {
                var st = sites[i];
                Expect("遗产「" + st.id + "」有正文与要点",
                    st.paragraphs != null && st.paragraphs.Length >= 3 && st.keys != null && st.keys.Length >= 2, "");
                Expect("遗产「" + st.id + "」有图片", st.images != null && st.images.Length >= 3, "");
                Expect("遗产「" + st.id + "」语音包在 Resources 里", HeritageArchive.Voice(st) != null, st.audio);
                Expect("遗产「" + st.id + "」主图能读出来", HeritageArchive.Image(st, 0) != null, st.images[0]);
            }

            // 三块立牌：都挂在序章里、都带 Interactable 与触发器、牌面刻着自己的名字
            var boards = new System.Collections.Generic.List<Interactable>();
            var all = Object.FindObjectsOfType<Interactable>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].kind == InteractKind.Info && !string.IsNullOrEmpty(all[i].heritageId)) boards.Add(all[i]);

            Expect("序章有三块遗产立牌", boards.Count == 3, "实际 " + boards.Count);
            for (int i = 0; i < boards.Count; i++)
            {
                var b = boards[i];
                Expect("立牌「" + b.heritageId + "」能对上遗产数据", Find(sites, b.heritageId) != null, "");
                Expect("立牌「" + b.heritageId + "」有触发器与提示",
                    b.GetComponent<Collider>() != null && !string.IsNullOrEmpty(b.prompt), "");
                Expect("立牌「" + b.heritageId + "」立在岔路口（幡柱之后、界门之前）",
                    b.transform.position.magnitude > 6f && b.transform.position.magnitude < 15.5f,
                    b.transform.position.ToString("F1"));

                var site = Find(sites, b.heritageId);
                bool named = false;
                var texts = b.GetComponentsInChildren<Text>(true);
                for (int t = 0; t < texts.Length && site != null; t++)
                    if (texts[t].text != null && texts[t].text.Replace("\n", "").Contains(site.name)) named = true;
                Expect("立牌「" + b.heritageId + "」牌面上有遗产名", named, site != null ? site.name : "");
            }

            // 动线层次：幡柱在内、立牌居中、界门在外 —— 从山门望过去是三进，不是挤成一簇
            var portals = Object.FindObjectsOfType<ScenePortal>(true);
            var everyTf = Object.FindObjectsOfType<Transform>(true);
            var poles = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < everyTf.Length; i++)
                if (everyTf[i].name.StartsWith("Waypole_")) poles.Add(everyTf[i]);
            Expect("三个路口各立一根幡柱", poles.Count == 3, "实际 " + poles.Count);

            var everyRing = Object.FindObjectsOfType<GuideRing>(true);
            Expect("每个触发点只摆一只圈，一路不铺小圈", everyRing.Length == 4, "实际 " + everyRing.Length);

            for (int i = 0; i < boards.Count; i++)
            {
                var b = boards[i];
                Vector3 bp = b.transform.position;
                float boardR = new Vector2(bp.x, bp.z).magnitude;
                float poleR = NearestR(poles, bp);
                var portal = FindPortal(portals, RouteSceneOf(b.heritageId), null);
                float gateR = portal != null ? portal.transform.position.magnitude : 0f;
                Expect("「" + b.heritageId + "」先看见幡、再看见牌",
                    poleR > 0f && poleR < boardR - 1.5f,
                    "幡 " + poleR.ToString("F1") + " m / 牌 " + boardR.ToString("F1") + " m");
                Expect("「" + b.heritageId + "」牌之后才是界门", gateR > boardR + 3f,
                    "牌 " + boardR.ToString("F1") + " m / 门 " + gateR.ToString("F1") + " m");
            }

            // 两座展台：良渚只留影像、不再出三维，秦陵与泉州各有一座
            var stages = Object.FindObjectsOfType<HeritageShowcase>(true);
            Expect("序章有两座三维展台", stages.Length == 2, "实际 " + stages.Length);
            for (int i = 0; i < stages.Length; i++)
            {
                var sg = stages[i];
                Expect("展台「" + sg.siteId + "」有转台", sg.turntable != null, "");
                Expect("展台「" + sg.siteId + "」摆在主线看不见的地方", sg.transform.position.y < -50f,
                    sg.transform.position.y.ToString("F0"));
                bool layered = true;
                if (sg.turntable != null)
                {
                    var rs = sg.turntable.GetComponentsInChildren<MeshRenderer>(true);
                    if (rs.Length == 0) layered = false;
                    for (int r = 0; r < rs.Length; r++)
                        if (rs[r].gameObject.layer != HeritageShowcase.Layer) layered = false;
                }
                Expect("展台「" + sg.siteId + "」的模型都在 " + HeritageShowcase.LayerName + " 层", layered, "");
            }

            // 面板：挂在 HUD 上，内容槽都接好了
            var panels = Object.FindObjectsOfType<HeritagePanel>(true);
            Expect("序章挂着一块遗产面板", panels.Length == 1, "实际 " + panels.Length);
            if (panels.Length == 1)
            {
                var pn = panels[0];
                Expect("面板的正文/影像/三维槽都接好",
                    pn.root != null && pn.body != null && pn.photo != null && pn.viewport != null, "");
                Expect("面板有缩略图与弘文头像", pn.thumbs != null && pn.thumbs.Length >= 3 && pn.portraitBook != null, "");
                Expect("面板有听语音的入口", pn.btnVoice != null && pn.speakerLine != null, "");
            }

            // 弘文：三维本体 + 语音 + 主动搭话
            var dhs = Object.FindObjectsOfType<DigitalHuman>(true);
            Expect("序章站着一位讲解员", dhs.Length == 1, "实际 " + dhs.Length);
            if (dhs.Length == 1)
            {
                var dh = dhs[0];
                Expect("弘文的三维本体接好了（视觉/身体/头）",
                    dh.visual != null && dh.body != null && dh.head != null, "");
                Expect("弘文的开场白语音包在 Resources 里",
                    Resources.Load<AudioClip>("Heritage/Audio/" + DigitalHuman.RingVoice("intro")) != null, "");
                // 总说明牌：书名那块牌，她该立在牌前，不是广场中央
                var introSign = (Transform)null;
                var interactables = Object.FindObjectsOfType<Interactable>(true);
                for (int i = 0; i < interactables.Length; i++)
                {
                    var t = interactables[i].title;
                    if (t != null && t.StartsWith("《河山册")) { introSign = interactables[i].transform; break; }
                }
                Expect("序章立着《河山册》总说明牌", introSign != null, "");
                if (introSign != null)
                {
                    Vector3 toHim = dh.transform.position - introSign.position;
                    toHim.y = 0f;
                    float gap = toHim.magnitude;
                    Expect("弘文站在《河山册》说明牌前 1.8–3.6 m", gap > 1.8f && gap < 3.6f, gap.ToString("F2") + " m");
                    Vector3 face = introSign.rotation * Vector3.back;
                    Expect("弘文站在说明牌的牌面这一侧",
                        toHim.sqrMagnitude > 0.01f && Vector3.Dot(toHim.normalized, face) > 0.7f, "");
                }
                Expect("弘文面朝山门（玩家一进谷正对着她）",
                    Vector3.Dot(dh.body.rotation * Vector3.back,
                                (new Vector3(0f, 0f, -29.5f) - dh.transform.position).normalized) > 0.7f,
                    "body yaw=" + dh.body.localEulerAngles.y.ToString("0") + "°");
                Expect("弘文不会一进序章就开口（等玩家走进她的圈）",
                    dh.walkSeconds > 0f && dh.walkSpeed > 0f, "");

                // 引导圈：一开始不播报，玩家走进去她才走过来讲
                var rings = Object.FindObjectsOfType<GuideRing>(true);
                Expect("四个触发点各摆一只引导圈", rings.Length == 4, "实际 " + rings.Length);
                string[] entries = { "intro", "1592", "441", "1561" };
                for (int i = 0; i < entries.Length; i++)
                {
                    GuideRing found = null;
                    for (int r = 0; r < rings.Length; r++)
                        if (rings[r].heritageId == entries[i]) { found = rings[r]; break; }
                    Expect("「" + entries[i] + "」入口有一只引导圈", found != null, "");
                    if (found == null) continue;
                    Expect("「" + entries[i] + "」的圈踩得进去（半径 ≥ 1 m）", found.radius >= 1f,
                        found.radius.ToString("F2"));

                    AudioClip voice = Resources.Load<AudioClip>("Heritage/Audio/" + DigitalHuman.RingVoice(entries[i]));
                    Expect("「" + entries[i] + "」的圈有对应语音", voice != null,
                        DigitalHuman.RingVoice(entries[i]));

                    if (entries[i] == "intro")
                    {
                        // 开场圈是给玩家的落脚点：摆在步道中轴上，走过来必踩到，
                        // 但不能画在弘文脚下 —— 那样玩家不知道该站哪
                        Expect("开场圈摆在步道中轴上（玩家走过来必踩到）",
                            Mathf.Abs(found.transform.position.x) < 0.8f, found.transform.position.x.ToString("F2"));
                        float ringGap = Vector3.Distance(
                            new Vector3(found.transform.position.x, 0f, found.transform.position.z),
                            new Vector3(dh.transform.position.x, 0f, dh.transform.position.z));
                        Expect("开场圈离弘文 2–6 m（她迎上来只要几步）", ringGap > 2f && ringGap < 6f,
                            ringGap.ToString("F2") + " m");
                        continue;
                    }
                    // 三块立牌的圈都要落在牌面正前方、牌前两三米
                    string id = entries[i];
                    var signs = Object.FindObjectsOfType<Interactable>(true);
                    Transform board = null;
                    for (int b = 0; b < signs.Length; b++)
                        if (signs[b].heritageId == id) { board = signs[b].transform; break; }
                    Expect("「" + id + "」的圈找得到对应的立牌", board != null, "");
                    if (board == null) continue;

                    Vector3 toRing = found.transform.position - board.position;
                    toRing.y = 0f;
                    float dist = toRing.magnitude;
                    Expect("「" + id + "」的圈立在牌前 2–3.4 m", dist > 2f && dist < 3.4f, dist.ToString("F2") + " m");
                    Vector3 facing = board.rotation * Vector3.back;
                    Expect("「" + id + "」的圈在牌面这一侧", Vector3.Dot(toRing.normalized, facing) > 0.85f, "");
                    Expect("「" + id + "」的圈贴着地面", Mathf.Abs(found.transform.position.y - board.position.y) < 0.4f,
                        found.transform.position.y.ToString("F2"));
                }

                // 人像是带骨骼的 FBX：靠剪辑演，不靠程序化拼体块
                var skin = dh.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Expect("弘文是带骨骼的蒙皮网格", skin != null && skin.bones != null && skin.bones.Length > 20,
                    skin != null && skin.bones != null ? skin.bones.Length + " 根骨骼" : "没有 SkinnedMeshRenderer");
                Expect("弘文身上挂 Animation 组件", dh.anim != null, "");
                if (dh.anim != null)
                {
                    Expect("弘文的动画剪辑 ≥ 10 段", dh.anim.GetClipCount() >= 10, "实际 " + dh.anim.GetClipCount());
                    string[] need = { "walk", "greet_01", "greet_02", "agree", "wait", "look_around",
                                      "fold_arms", "clap", "cheer", "dig" };
                    for (int i = 0; i < need.Length; i++)
                        Expect("弘文有「" + need[i] + "」剪辑", dh.anim.GetClip(need[i]) != null, "");
                    Expect("指路用的三条路线语音在 Resources 里",
                        Resources.Load<AudioClip>("Heritage/Audio/guide_1592") != null &&
                        Resources.Load<AudioClip>("Heritage/Audio/guide_441") != null &&
                        Resources.Load<AudioClip>("Heritage/Audio/guide_1561") != null, "");
                    Expect("收尾庆祝的语音也在", Resources.Load<AudioClip>("Heritage/Audio/cheer") != null, "");

                    Vector3 dir;
                    Expect("弘文能按遗产编号找到岔路方向",
                        dh.RouteDirection("1592", out dir) && dh.RouteDirection("441", out dir) &&
                        dh.RouteDirection("1561", out dir), "");
                }

                // 站直了要有一米七上下、鞋底踩在地面上
                var rs = dh.GetComponentsInChildren<Renderer>(true);
                bool sized = false;
                float top = 0f;
                for (int i = 0; i < rs.Length; i++)
                {
                    if (rs[i].GetComponentInParent<DigitalHuman>() != dh) continue;
                    top = Mathf.Max(top, rs[i].bounds.max.y - dh.transform.position.y);
                }
                // 蒙皮包围盒跟着当前姿势走（抬手的瞬间会比站直高一截），所以给到 2.15 m 的上限
                sized = top > 1.45f && top < 2.15f;
                Expect("弘文的个子在 1.45–2.15 m 之间（含姿势浮动）", sized, top.ToString("0.00") + " m");
            }
        }

        static HeritageSite Find(HeritageSite[] sites, string id)
        {
            for (int i = 0; i < sites.Length; i++)
                if (sites[i].id == id) return sites[i];
            return null;
        }

        static string RouteSceneOf(string id)
        {
            switch (id)
            {
                case "1592": return "Liangzhu";
                case "441": return "Qinling";
                case "1561": return "Quanzhou";
                default: return null;
            }
        }

        /// <summary>离某点最近的那根幡柱有多远（找不到返回 -1）。</summary>
        static float NearestR(System.Collections.Generic.List<Transform> list, Vector3 from)
        {
            float best = -1f;
            for (int i = 0; i < list.Count; i++)
            {
                Vector3 d = list[i].position - from;
                d.y = 0f;
                if (best < 0f || d.magnitude < best) best = d.magnitude;
            }
            return best;
        }

        static ScenePortal FindPortal(ScenePortal[] portals, string scene, string spawn)
        {
            foreach (var p in portals)
                if (p.sceneToLoad == scene && (spawn == null || p.spawnId == spawn)) return p;
            return null;
        }

        static void ExpectSpawn(string scene, string id)
        {
            Expect(scene + " 有落点 " + id, SpawnPoint.Find(id) != null, "");
        }

        static void Open(string name)
        {
            var scene = EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/" + name + ".unity");
            Debug.Log("[SelfCheck] ——— " + name + " ———");
            if (!scene.IsValid()) Debug.LogError("[SelfCheck] 打不开 " + name);
        }

        static void Expect(string what, bool ok, string detail)
        {
            if (ok) { pass++; Debug.Log("[SelfCheck] OK   " + what); }
            else { fail++; Debug.LogError("[SelfCheck] FAIL " + what + (string.IsNullOrEmpty(detail) ? "" : "　→ " + detail)); }
        }
    }
}
