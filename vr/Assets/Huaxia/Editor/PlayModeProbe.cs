#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 播放模式自动走查：进 Play Mode，按真实运行时把玩家依次放到
    /// 序章 / 良渚 / 秦陵 / 泉州 / 终章的关键位置，读 HUD、引导行、弹窗、
    /// 门的开合角度，并把画面截到 Previews/play_*.png。
    ///
    /// 注意：它模拟的是「站到某处之后系统给出什么」，不是人手按 WASD。
    /// 只能验证状态与文案，验证不了手感、观感、眩晕和读字舒适度。
    ///
    /// 用法：Unity 以图形界面打开本工程，菜单「Huaxia/播放模式走查」；
    /// 或命令行 -executeMethod Huaxia.EditorTools.PlayModeProbe.Run。
    /// 跑完自动退出 Unity。
    /// </summary>
    public static class PlayModeProbe
    {
        const string SessionKey = "huaxia_play_probe";
        static double startTime;
        static int step;
        static int pass, fail;
        static string outDir;
        static Interactable liangzhuTarget;

        [MenuItem("Huaxia/播放模式走查")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Probe] 已经在播放模式里。");
                return;
            }

            outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Previews");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            // 走查要的是"第一次来"的状态：上次跑留下的 visited/chapter 键会让
            // 开场说明不再弹、终章门直接开，测出来的就不是新玩家看到的。
            GameState.ResetAll();
            Debug.Log("[Probe] 已清空进度键");

            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Archive.unity");
            Debug.Log("[Probe] 进入播放模式");
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static double Now { get { return EditorApplication.timeSinceStartup; } }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (startTime == 0)
            {
                startTime = Now;
                step = 0;
                pass = fail = 0;
                return;
            }

            double t = Now - startTime;
            if (t > 60.0)
            {
                Fail("整体超时", "step=" + step);
                Finish();
                return;
            }

            try
            {
                if (step == 0 && t > 2.0) StepArchive();
                else if (step == 1 && t > 3.4) StepArchiveGuide();
                else if (step == 2 && t > 6.4) StepLiangzhu();
                else if (step == 3 && t > 7.4) StepLiangzhuPoint();
                else if (step == 4 && t > 8.2) StepLiangzhuInteract();
                else if (step == 5 && t > 10.4) StepLiangzhuReturned();
                else if (step == 6 && t > 13.2) StepQinlingSpawn();
                else if (step == 7 && t > 15.2) StepQinlingDoorOpened();
                else if (step == 8 && t > 16.4) StepQinlingFar();
                else if (step == 9 && t > 19.2) StepQinlingClosed();
                else if (step == 10 && t > 19.8) StepQinlingNear();
                else if (step == 11 && t > 22.8) StepQinlingInside();
                // 换场景要 0.55 s 淡黑 + 0.85 s 地名，再留 1 s 让环境音淡进来
                else if (step == 12 && t > 26.6) StepQuanzhou();
                else if (step == 13 && t > 30.0) StepEpilogue();
            }
            catch (Exception e)
            {
                Fail("走查抛异常 step=" + step, e.Message);
                Finish();
            }
        }

        // ------------------------------------------------------------ 序章

        static void StepArchive()
        {
            step = 1;
            var ui = UIManager.Instance;
            Expect("序章有 UIManager", ui != null, "");
            Expect("序章有引导行", ui != null && ui.guideText != null, "");
            Expect("序章有玩家", Player() != null, "");

            Info("场景", SceneManager.GetActiveScene().name);
            Info("玩家位置", Player().transform.position.ToString("0.0"));
            Info("弹窗（开场说明）", ui != null && ui.ModalOpen ? "已弹出" : "未弹出");
            Info("开场说明标题", ui != null && ui.modalTitle != null ? ui.modalTitle.text : "—");
            Info("引导行", ui != null && ui.guideText != null ? ui.guideText.text : "—");
            Info("环境音", AudioState());

            Capture("play_01_archive_intro.png");

            // 关掉开场说明，站到良渚那道界门前
            if (ui != null)
            {
                if (ui.ModalOpen) ui.Confirm();
                // 真正的门是路尽头的 ScenePortal，不是路口的幡柱
                ScenePortal portal = null;
                foreach (var p in UnityEngine.Object.FindObjectsOfType<ScenePortal>())
                    if (p.sceneToLoad == "Liangzhu") { portal = p; break; }
                if (portal != null)
                {
                    Teleport(portal.transform.position + portal.transform.forward * 2.4f);
                    Info("已传送到良渚界门前", Player().transform.position.ToString("0.0"));
                    Info("界门提示", ui.promptText != null && ui.promptText.gameObject.activeSelf
                        ? ui.promptText.text : "（没显示）");
                }
                else Fail("序章找得到良渚界门", "没有 sceneToLoad=Liangzhu 的 ScenePortal");
            }
        }

        static void StepArchiveGuide()
        {
            step = 2;
            var ui = UIManager.Instance;
            string g = ui != null && ui.guideText != null ? ui.guideText.text : "";
            Info("界门前的引导行", g);
            Expect("引导行指向良渚", g.Contains("良渚"), g);
            Expect("引导行有方向", g.Contains("前方") || g.Contains("侧") || g.Contains("后方"), g);
            Capture("play_02_archive_guide.png");

            ui.LoadScene("Liangzhu", "from_hub", "走查 · 良渚");
        }

        // ------------------------------------------------------------ 良渚

        static void StepLiangzhu()
        {
            step = 3;
            var ui = UIManager.Instance;
            var tasks = TaskManager.Instance;
            Expect("已切到良渚", SceneManager.GetActiveScene().name == "Liangzhu",
                SceneManager.GetActiveScene().name);
            Expect("良渚有 TaskManager", tasks != null, "");
            Expect("进场就记为去过", GameState.IsChapterVisited("liangzhu"), "");
            Expect("良渚 HUD 有方向引导", ui != null && ui.guideText != null, "");
            Info("进场弹窗", ui != null && ui.ModalOpen ? "已弹出" : "未弹出");
            Info("弹窗正文开头", ui != null && ui.modalBody != null
                ? ui.modalBody.text.Substring(0, Mathf.Min(48, ui.modalBody.text.Length)) : "—");
            Info("引导行", ui != null && ui.guideText != null ? ui.guideText.text : "—");
            Capture("play_03_liangzhu_intro.png");
            if (ui != null && ui.ModalOpen) ui.Confirm();
        }

        static void StepLiangzhuPoint()
        {
            step = 4;
            var tasks = TaskManager.Instance;
            var ui = UIManager.Instance;
            liangzhuTarget = null;
            foreach (var it in UnityEngine.Object.FindObjectsOfType<Interactable>())
            {
                if (string.IsNullOrEmpty(it.objectiveId)) continue;
                if (tasks != null && !tasks.IsPrimary(it.objectiveId)) continue;
                if (it.kind == InteractKind.Info) { liangzhuTarget = it; break; }
            }
            if (liangzhuTarget == null) { Fail("良渚找得到主线索点", ""); return; }

            FacePoint(liangzhuTarget);
            Info("站到主线索点前", liangzhuTarget.objectiveId + " / " + liangzhuTarget.title);
            Info("引导行", ui != null && ui.guideText != null ? ui.guideText.text : "—");
        }

        static void StepLiangzhuInteract()
        {
            step = 5;
            var ui = UIManager.Instance;
            var pc = Player();

            // 不能用「HUD 提示有没有显示」来判断：编辑器里 Game 视图没拿到焦点时
            // cursorLocked=false，DesktopPlayerController 会整帧跳过 Scan，提示永远不出现。
            // 这里直接按它同样的射线段打一次，看落点是不是挂在 Interactable 上。
            bool focusable = false;
            string hitName = "什么也没打到";
            if (pc != null && pc.playerCamera != null)
            {
                var cam = pc.playerCamera;
                if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                                    out RaycastHit hit, pc.interactDistance,
                                    ~0, QueryTriggerInteraction.Collide))
                {
                    var owner = hit.collider.GetComponentInParent<Interactable>();
                    focusable = owner != null;
                    hitName = hit.collider.name + (owner != null ? "（父级有 Interactable）" : "（父级无 Interactable）");
                }
            }
            Info("准星射线落点", hitName);
            Expect("准星射线能拾取到线索点", focusable, hitName);

            // 直接调用 Interact（等价于按 E 一次），看任务与弹窗
            var tasks = TaskManager.Instance;
            if (liangzhuTarget != null)
            {
                int before = tasks != null ? tasks.PrimaryDone : 0;
                liangzhuTarget.Interact();
                Expect("看线索后弹窗打开", ui != null && ui.ModalOpen, "");
                Info("弹窗标题", ui != null && ui.modalTitle != null ? ui.modalTitle.text : "—");
                if (ui != null && ui.ModalOpen) ui.Confirm();
                if (tasks != null)
                    Info("主线索进度", before + " → " + tasks.PrimaryDone + " / " + tasks.PrimaryCount);
            }
            Capture("play_04_liangzhu_point.png");

            // 合卷回山：调用 ReturnHome.Ask()（等价于按 Q），确认后应回到序章的 back_liangzhu 落点。
            var back = UnityEngine.Object.FindObjectOfType<ReturnHome>();
            Expect("良渚挂了合卷回山（Q）", back != null, "");
            if (back != null)
            {
                back.Ask();
                Expect("按 Q 弹出合卷确认框", ui != null && ui.ModalOpen, "");
                Info("合卷弹窗标题", ui != null && ui.modalTitle != null ? ui.modalTitle.text : "—");
                if (ui != null && ui.ModalOpen) ui.Confirm();
            }
        }

        static void StepLiangzhuReturned()
        {
            step = 6;
            var ui = UIManager.Instance;
            var pc = Player();
            Expect("合卷后回到序章", SceneManager.GetActiveScene().name == "Archive",
                SceneManager.GetActiveScene().name);

            var spawn = SpawnPoint.Find("back_liangzhu");
            Expect("序章有 back_liangzhu 落点", spawn != null, "");
            if (spawn != null && pc != null)
            {
                float d = Vector3.Distance(pc.transform.position, spawn.transform.position);
                Info("回山谷落点误差", d.ToString("0.00") + " m");
                Expect("落在 back_liangzhu", d < 2f, d.ToString("0.00") + " m");
            }

            string g = ui != null && ui.guideText != null ? ui.guideText.text : "";
            Info("回山谷后的引导行", g);
            Expect("回山谷后引导指向下一卷，不再指良渚", g.Contains("下一程") && !g.Contains("良渚"), g);
            Capture("play_09_archive_back.png");

            if (ui != null) ui.LoadScene("Qinling", "from_hub", "走查 · 秦陵");
        }

        // ------------------------------------------------------------ 秦陵

        static void StepQinlingSpawn()
        {
            step = 7;
            var ui = UIManager.Instance;
            Expect("已切到秦陵", SceneManager.GetActiveScene().name == "Qinling",
                SceneManager.GetActiveScene().name);
            var door = UnityEngine.Object.FindObjectOfType<AutoDoor>();
            Expect("秦陵有自动门", door != null, "");
            Info("落点", Player().transform.position.ToString("0.0"));
            if (door != null)
            {
                Info("落点到门", Vector3.Distance(Player().transform.position, door.transform.position)
                    .ToString("0.00") + " m（触发 " + door.triggerDistance + " m）");
                Info("门轴角度", HingeYaw(door));
                Expect("落点不该已经触发开门", Vector3.Distance(
                    Player().transform.position, door.transform.position) > door.triggerDistance,
                    "落点就在触发圈里，门一出生就开");
            }
            Info("引导行", ui != null && ui.guideText != null ? ui.guideText.text : "—");
            Capture("play_05_qinling_spawn.png");
            if (ui != null && ui.ModalOpen) ui.Confirm();

            // 落点在 8 m 外（触发半径 5.2 m），原地站着当然不会开。
            // 走到门前 4 m，留 2 秒给门自己开。
            if (door != null) Teleport(door.transform.position + new Vector3(0f, 0f, -4f));
        }

        static void StepQinlingDoorOpened()
        {
            step = 8;
            var door = UnityEngine.Object.FindObjectOfType<AutoDoor>();
            Info("门前 4 m 站 2 秒后的门轴角度", HingeYaw(door));
            if (door != null)
                Expect("站在门前门会开", Mathf.Abs(HingeYawValue(door)) > 30f, HingeYaw(door));
        }

        static void StepQinlingFar()
        {
            step = 9;
            var door = UnityEngine.Object.FindObjectOfType<AutoDoor>();
            // 走远：沿 +Z 退 18 m，离开闭合半径 11.5 m
            Teleport(Player().transform.position + new Vector3(0f, 0f, -18f));
            Info("退到远处", Player().transform.position.ToString("0.0"));
            if (door != null)
                Info("离开后门轴角度", HingeYaw(door));
        }

        static void StepQinlingClosed()
        {
            step = 10;
            var door = UnityEngine.Object.FindObjectOfType<AutoDoor>();
            if (door != null)
            {
                Info("远处停留后的门轴角度", HingeYaw(door));
                Expect("走远后门会合上", Mathf.Abs(HingeYawValue(door)) < 20f, HingeYaw(door));
            }
            // 走近到门洞南侧 4 m（落点那一侧）
            if (door != null)
                Teleport(door.transform.position + new Vector3(0f, 0f, -4f));
            Info("走近到", Player().transform.position.ToString("0.0"));
        }

        static void StepQinlingNear()
        {
            step = 11;
            var door = UnityEngine.Object.FindObjectOfType<AutoDoor>();
            Info("近处 3 秒后的门轴角度", HingeYaw(door));
            if (door != null)
                Expect("走近后门重新打开", Mathf.Abs(HingeYawValue(door)) > 30f, HingeYaw(door));
        }

        static void StepQinlingInside()
        {
            step = 12;
            var ui = UIManager.Instance;
            // 穿过门洞站到里面
            var door = UnityEngine.Object.FindObjectOfType<AutoDoor>();
            if (door != null) Teleport(door.transform.position + new Vector3(0f, 0f, 8f));
            Info("站在门内", Player().transform.position.ToString("0.0"));
            Info("门内引导行", ui != null && ui.guideText != null ? ui.guideText.text : "—");
            Expect("门内有下一处引导", ui != null && ui.guideText != null
                && ui.guideText.text.Contains("下一处"), "");
            Capture("play_06_qinling_inside.png");
            ui.LoadScene("Quanzhou", "from_hub", "走查 · 泉州");
        }

        // ------------------------------------------------------------ 泉州 / 终章

        static void StepQuanzhou()
        {
            step = 13;
            var ui = UIManager.Instance;
            Expect("已切到泉州", SceneManager.GetActiveScene().name == "Quanzhou",
                SceneManager.GetActiveScene().name);
            Info("环境音", AudioState());
            Info("引导行", ui != null && ui.guideText != null ? ui.guideText.text : "—");
            Expect("泉州引导行有内容", ui != null && ui.guideText != null
                && !string.IsNullOrEmpty(ui.guideText.text), "");
            Capture("play_07_quanzhou.png");
            if (ui != null && ui.ModalOpen) ui.Confirm();
            ui.LoadScene("Epilogue", "epilogue_start", "走查 · 终章");
        }

        static void StepEpilogue()
        {
            step = 14;
            var ui = UIManager.Instance;
            Expect("已切到终章", SceneManager.GetActiveScene().name == "Epilogue",
                SceneManager.GetActiveScene().name);
            Info("引导行", ui != null && ui.guideText != null ? ui.guideText.text : "—");
            Info("环境音", AudioState());
            Capture("play_08_epilogue.png");
            Finish();
        }

        // ------------------------------------------------------------ 工具

        static DesktopPlayerController Player()
        {
            return UnityEngine.Object.FindObjectOfType<DesktopPlayerController>();
        }

        static void FacePoint(Interactable it)
        {
            var pc = Player();
            if (pc == null) return;

            // 玩家可能从墙背后被传送过来，此时"朝玩家那边站"会被墙挡住。
            // 绕着线索点试 8 个方向，取第一个准星能真正打到它的落脚点。
            Vector3 target = it.transform.position;
            var col = it.GetComponentInChildren<Collider>();
            Vector3 aim = col != null ? col.bounds.center : target;

            Vector3 pref = pc.transform.position - target;
            pref.y = 0f;
            if (pref.sqrMagnitude < 0.01f) pref = Vector3.forward;
            pref.Normalize();

            Vector3 bestAway = pref;
            bool found = false;
            for (int i = 0; i < 8; i++)
            {
                Vector3 away = Quaternion.Euler(0f, i * 45f, 0f) * pref;
                Vector3 stand = target + away * 2.4f;
                stand.y = GroundY(stand) + 0.08f;
                Vector3 eye = stand + Vector3.up * 1.55f;
                Vector3 d = aim - eye;
                if (Physics.Raycast(eye, d.normalized, out RaycastHit hit, d.magnitude + 0.8f,
                                    ~0, QueryTriggerInteraction.Collide))
                {
                    var owner = hit.collider.GetComponentInParent<Interactable>();
                    if (owner == it)
                    {
                        bestAway = away;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                Info("线索点遮挡", it.objectiveId + "：8 个方向都没有无遮挡的落脚点");

            Vector3 p = target + bestAway * 2.4f;
            float yaw = Mathf.Atan2(-bestAway.x, -bestAway.z) * Mathf.Rad2Deg;
            Teleport(p, yaw);

            // 准星要真的对准线索点：Teleport 只设了水平朝向（yaw），俯仰还停在上一处。
            // 按「眼睛 → 碰撞球中心」补上俯仰角，写回控制器的私有 pitch，
            // 否则射线会打飞，把「能点到」误判成「点不到」。
            var cam = pc.playerCamera;
            if (cam != null)
            {
                Vector3 eye = cam.transform.position;
                Vector3 dir = aim - eye;
                float pitchDeg = -Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
                var f = typeof(DesktopPlayerController).GetField("pitch",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) f.SetValue(pc, pitchDeg);
                cam.transform.localEulerAngles = new Vector3(pitchDeg, 0f, 0f);
            }
        }

        static float GroundY(Vector3 pos)
        {
            Vector3 probe = pos + Vector3.up * 40f;
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 120f, ~0,
                                QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return pos.y;
        }

        /// <summary>把玩家脚底放到该点附近的地面上，可选朝向。</summary>
        static void Teleport(Vector3 pos, float yaw = float.NaN)
        {
            var pc = Player();
            if (pc == null) return;
            var cc = pc.GetComponent<CharacterController>();

            Vector3 probe = pos + Vector3.up * 40f;
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 120f, ~0,
                                QueryTriggerInteraction.Ignore))
                pos.y = hit.point.y + 0.08f;

            cc.enabled = false;
            pc.transform.position = pos;
            if (!float.IsNaN(yaw)) pc.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cc.enabled = true;
        }

        static float HingeYawValue(AutoDoor door)
        {
            if (door == null || door.hinges == null || door.hinges.Length == 0) return 0f;
            return Mathf.DeltaAngle(0f, door.hinges[0].localEulerAngles.y);
        }

        static string HingeYaw(AutoDoor door)
        {
            if (door == null) return "—";
            var s = "";
            for (int i = 0; i < door.hinges.Length; i++)
                s += door.hinges[i].localEulerAngles.y.ToString("0.0") + "° ";
            return s;
        }

        static string AudioState()
        {
            var d = AmbientDirector.Instance;
            if (d == null) return "（没有常驻播放器）";
            // AudioSource 挂在子物体 Ambient A / B 上，不在 director 自己身上
            var sources = d.GetComponentsInChildren<AudioSource>(true);
            var s = "";
            foreach (var a in sources)
                if (a.isPlaying) s += (a.clip != null ? a.clip.name : "?") + " @ " + a.volume.ToString("0.00") + " ";
            return string.IsNullOrEmpty(s) ? "（静音）" : s;
        }

        static void Capture(string file)
        {
            if (Application.isBatchMode) return;
            try
            {
                // 进播放模式会重载域，静态字段 outDir 会丢，这里补回来
                if (string.IsNullOrEmpty(outDir))
                    outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Previews");
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                ScreenCapture.CaptureScreenshot(Path.Combine(outDir, file));
                Debug.Log("[Probe] 截图 → " + file);
            }
            catch (Exception e) { Debug.LogWarning("[Probe] 截图失败：" + e.Message); }
        }

        static void Info(string what, string value)
        {
            Debug.Log("[Probe] INFO " + what + "：" + value);
        }

        static void Expect(string what, bool ok, string detail)
        {
            if (ok) { pass++; Debug.Log("[Probe] OK   " + what); }
            else { fail++; Debug.LogError("[Probe] FAIL " + what + (string.IsNullOrEmpty(detail) ? "" : "　→ " + detail)); }
        }

        static void Fail(string what, string detail)
        {
            fail++;
            Debug.LogError("[Probe] FAIL " + what + "　→ " + detail);
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.EraseBool(SessionKey);
            Debug.Log("[Probe] 结束　pass=" + pass + " fail=" + fail);
            EditorApplication.Exit(fail == 0 ? 0 : 2);
        }
    }
}
#endif
