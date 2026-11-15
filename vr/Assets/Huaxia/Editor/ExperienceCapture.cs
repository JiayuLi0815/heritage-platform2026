#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 录「体验视频」：用 Unity Recorder 把 Game 视图和 AudioListener 的混音一起录进 MP4，
    /// 再用脚本把玩家依次带到关键位置。走路速度仍走 DesktopPlayerController 的
    /// CharacterController，观感接近真人用 WASD 走，只是没有鼠标抖动。
    ///
    /// 用法：菜单「Huaxia/录制体验视频/序章·入门到全局（带声音）」。
    /// 录完自动退出播放模式，文件落在工程根目录 Recordings/ 下。
    /// </summary>
    public static class ExperienceCapture
    {
        const string SessionKey = "huaxia_capture_session";
        const string ModeKey = "huaxia_capture_mode";
        const string OutDirName = "Recordings";

        static bool started;
        static RecorderController controller;
        static readonly Stack<IEnumerator> routineStack = new Stack<IEnumerator>();

        static DesktopPlayerController pc;
        static Transform player;
        static Camera cam;
        static UIManager ui;
        static DigitalHuman dh;

        static float yaw, pitch, yawTarget, pitchTarget;
        static bool walking;
        static Vector3 walkDest;
        static float walkSpeed, walkDeadline;
        static float walkYawBias;      // 边走边看：朝行进方向偏一点，别一路死盯着正前方

        static string outputFile;
        static string mode = "prologue";
        static float takeStart;

        static readonly FieldInfo NarratingField =
            typeof(DigitalHuman).GetField("narrating", BindingFlags.NonPublic | BindingFlags.Instance);

        [MenuItem("Huaxia/录制体验视频/序章·入门到全局（带声音）")]
        public static void RunPrologue()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Capture] 已经在播放模式里。");
                return;
            }

            GameState.ResetAll();
            PlayerPrefs.Save();

            mode = "prologue";
            outputFile = OutputPath(mode);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            SessionState.SetString(ModeKey, mode);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Archive.unity");
            Debug.Log("[Capture] 序章已打开，进入播放模式。输出：" + outputFile);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Huaxia/录制体验视频/卷一·走进良渚（带声音）")]
        public static void RunLiangzhu()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Capture] 已经在播放模式里。");
                return;
            }

            GameState.ResetAll();
            PlayerPrefs.Save();

            mode = "liangzhu";
            outputFile = OutputPath(mode);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            SessionState.SetString(ModeKey, mode);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Liangzhu.unity");
            Debug.Log("[Capture] 良渚已打开，进入播放模式。输出：" + outputFile);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Huaxia/录制体验视频/卷二·走进秦陵（带声音）")]
        public static void RunQinling()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Capture] 已经在播放模式里。");
                return;
            }

            GameState.ResetAll();
            PlayerPrefs.Save();

            mode = "qinling";
            outputFile = OutputPath(mode);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            SessionState.SetString(ModeKey, mode);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Qinling.unity");
            Debug.Log("[Capture] 秦陵已打开，进入播放模式。输出：" + outputFile);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Huaxia/录制体验视频/卷三·走进泉州（带声音）")]
        public static void RunQuanzhou()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Capture] 已经在播放模式里。");
                return;
            }

            GameState.ResetAll();
            PlayerPrefs.Save();

            mode = "quanzhou";
            outputFile = OutputPath(mode);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            SessionState.SetString(ModeKey, mode);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Quanzhou.unity");
            Debug.Log("[Capture] 泉州已打开，进入播放模式。输出：" + outputFile);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Huaxia/录制体验视频/终章·河山册（带声音）")]
        public static void RunEpilogue()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Capture] 已经在播放模式里。");
                return;
            }

            GameState.ResetAll();
            PlayerPrefs.Save();

            mode = "epilogue";
            outputFile = OutputPath(mode);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            SessionState.SetString(ModeKey, mode);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Epilogue.unity");
            Debug.Log("[Capture] 终章已打开，进入播放模式。输出：" + outputFile);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            if (!EditorApplication.isPlaying || !Application.isPlaying) return;

            if (!started)
            {
                Init();
                return;
            }

            PumpRoutine();
            UpdateLook();
        }

        /// <summary>
        /// 手动驱动协程。Unity 正常调度时会把 yield return 的 IEnumerator 展开成子协程，
        /// 但手动 MoveNext 不会——必须自己压栈，否则 Wait/WalkTo 这些嵌套协程一步就过去了。
        /// </summary>
        static void PumpRoutine()
        {
            int guard = 0;
            while (routineStack.Count > 0 && guard++ < 16)
            {
                var top = routineStack.Peek();
                if (top.MoveNext())
                {
                    if (top.Current is IEnumerator nested)
                    {
                        routineStack.Push(nested);
                        continue;
                    }
                    break;                       // yield return null：等下一次编辑器 update
                }
                routineStack.Pop();              // 子协程结束，回到父协程
            }
        }

        static void Init()
        {
            // 进播放模式会重载脚本域，静态字段全部归零：模式和路径在这里重算一次。
            mode = SessionState.GetString(ModeKey, "prologue");
            outputFile = OutputPath(mode);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            pc = UnityEngine.Object.FindObjectOfType<DesktopPlayerController>();
            ui = UIManager.Instance;
            dh = DigitalHuman.Instance;
            if (pc == null || ui == null) return;

            player = pc.transform;
            cam = pc.playerCamera != null ? pc.playerCamera : player.GetComponentInChildren<Camera>();
            if (cam == null) return;

            started = true;
            takeStart = Time.realtimeSinceStartup;

            // 录制期间游戏时间按真实时间走，别让 captureFramerate / timeScale 拖慢演出
            Time.captureFramerate = 0;
            Time.timeScale = 1f;

            // 用 VR 通道接管移动：xrActive 为真时控制脚本不再读鼠标，
            // externalMove 就是平时手柄摇杆那一路，走起来仍然是 CharacterController。
            pc.xrActive = true;
            pc.externalMove = Vector2.zero;
            yaw = player.eulerAngles.y;
            pitch = 0f;
            yawTarget = yaw;
            pitchTarget = 0f;

            // 秦陵这卷不从谷口慢慢走：直接站到铜车马坑南侧，照预览图那个角度往北看。
            if (mode == "qinling") TeleportToGround(new Vector3(60f, 0f, -6.5f), 0f);

            try
            {
                StartRecorder();
            }
            catch (Exception e)
            {
                Debug.LogError("[Capture] Recorder 启动失败：" + e);
                SessionState.SetBool(SessionKey, false);
                EditorApplication.isPlaying = false;
                return;
            }

            routineStack.Clear();
            if (mode == "liangzhu") routineStack.Push(SequenceLiangzhu());
            else if (mode == "qinling") routineStack.Push(SequenceQinling());
            else if (mode == "quanzhou") routineStack.Push(SequenceQuanzhou());
            else if (mode == "epilogue") routineStack.Push(SequenceEpilogue());
            else routineStack.Push(Sequence());
            Log("开始录制（" + mode + "）");
        }

        static void StartRecorder()
        {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.FrameRate = 30f;
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Variable;
            controllerSettings.CapFrameRate = false;
            controllerSettings.SetRecordModeToManual();

            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "PrologueTake";
            movie.Enabled = true;
            movie.OutputFile = outputFile;
            movie.FrameRate = 30f;
            movie.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };
            movie.CaptureAudio = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
                TargetBitRate = 16f
            };

            controllerSettings.AddRecorderSettings(movie);
            controller = new RecorderController(controllerSettings);
            controller.PrepareRecording();
            bool ok = controller.StartRecording();
            Log("Recorder 启动：" + ok);
            if (!ok) throw new Exception("StartRecording 返回 false，见上方 Recorder 日志");
        }

        // ============================================================ 路线

        static IEnumerator Sequence()
        {
            yield return Wait(1.0f);                         // 等开场 0.6 s 的黑场淡完

            Log("1/5 走进山门，踩上引导圈");
            yield return WalkTo(new Vector3(0f, 0.1f, -22.9f), 2.0f, 9f);

            // 弘文在圈里开口。开场白 48.7 s，之后她还会朝三岔口指路 17.6 s，
            // narrating 从 EncounterRoutine 开始一直到整段结束才清空。
            Log("2/5 等弘文讲完开场白与指路");
            float wait = 0f;
            while (!IsNarrating() && wait < 6f) { wait += Time.deltaTime; yield return null; }
            yield return LookAt(new Vector3(-3.30f, 1.55f, -22.30f), 1.4f);

            float talkClock = 0f;
            while (IsNarrating() && talkClock < 100f)
            {
                // 讲的时候极慢地转一点，别让机位像张照片
                yawTarget = -74f + Mathf.Sin(talkClock * 0.12f) * 3f;
                talkClock += Time.deltaTime;
                yield return null;
            }
            Log("弘文讲完，用时 " + talkClock.ToString("F1") + " s");
            yield return Wait(1.0f);

            Log("3/5 走到进谷左侧的介绍牌前");
            var sign = GameObject.Find("IntroSign");
            Interactable signInteractable = sign != null ? sign.GetComponent<Interactable>() : null;
            Vector3 signPanel = new Vector3(-5.53f, 1.92f, -20.67f);
            yield return WalkTo(new Vector3(-3.60f, 0.1f, -20.90f), 1.5f, 9f);
            yield return LookAt(signPanel, 1.2f);

            if (signInteractable != null)
            {
                signInteractable.Interact();
                Log("打开介绍牌弹窗");
            }
            else
            {
                Log("没找到 IntroSign，跳过弹窗");
            }

            float openWait = 0f;
            while (!ui.ModalOpen && openWait < 3f) { openWait += Time.deltaTime; yield return null; }
            yield return Wait(17.0f);                        // 留出读牌面的时间
            if (ui.ModalOpen) ui.Cancel();
            yield return Wait(1.0f);

            Log("4/5 往山谷里走，到三岔口");
            yield return LookAt(new Vector3(0f, 1.6f, -18f), 0.8f);
            yield return WalkTo(new Vector3(0f, 0.1f, -19.4f), 1.8f, 9f);
            yield return WalkTo(new Vector3(0f, 0.1f, -12.6f), 2.4f, 12f);
            yield return WalkTo(new Vector3(0f, 0.1f, -3.6f), 2.4f, 14f);

            // 三岔口：左右各扫一眼三块立牌，再回正
            yield return LookAt(new Vector3(0f, 1.6f, 6f), 0.8f);
            yield return PanTo(-34f, 3.2f);
            yield return PanTo(-6f, 2.2f);
            yield return PanTo(32f, 3.4f);
            yield return PanTo(0f, 2.4f);

            Log("5/5 起高，给序章一个全局俯瞰");
            yield return Aerial(10.5f);

            Log("录制收尾");
            yield return Wait(0.6f);
            yield return FadeToBlack(1.6f);
            if (controller != null)
            {
                controller.StopRecording();
                controller = null;
            }
            Log("已停止，文件：" + outputFile + ".mp4");
            yield return Wait(1.5f);

            SessionState.SetBool(SessionKey, false);
            EditorApplication.isPlaying = false;
        }

        /// <summary>
        /// 卷一 · 良渚：从城外湿地大路起步，过河桥，穿过南陆门，
        /// 在城内木桥上抬头看莫角山，再沿坡道上台地回望全城。约 45 秒。
        /// </summary>
        static IEnumerator SequenceLiangzhu()
        {
            // 进场黑场 0.6 s，章节介绍 0.8 s 后自己弹出来
            yield return Wait(1.6f);
            Log("1/4 章节卡：第一章 · 城与玉");
            yield return Wait(2.2f);
            if (ui != null && ui.ModalOpen) ui.Cancel();
            yield return Wait(0.5f);

            Log("2/4 沿湿地大路走到南陆门");
            yield return WalkTo(new Vector3(0f, 0.1f, -44f), 2.9f, 10f);   // 过河木桥
            yield return WalkTo(new Vector3(0f, 0.1f, -14f), 2.9f, 16f);
            yield return WalkTo(new Vector3(0f, 0.1f, 3.6f), 2.6f, 12f);   // 城门外的土垄

            Log("3/4 穿过南陆门");
            yield return WalkTo(new Vector3(0f, 0.1f, 10.5f), 1.5f, 8f);   // 门洞里放慢
            yield return WalkTo(new Vector3(0f, 0.1f, 14.8f), 1.7f, 6f);   // 城内木桥
            yield return LookAt(new Vector3(0f, 4.2f, 38f), 2.0f);          // 抬头看莫角山

            Log("4/4 沿坡道上莫角山，回望全城");
            yield return WalkTo(new Vector3(0f, 0.1f, 23.8f), 1.9f, 9f);
            yield return LookAt(new Vector3(0f, 1.4f, -24f), 4.6f);         // 转身回望来路

            Log("录制收尾");
            yield return Wait(0.5f);
            yield return FadeToBlack(1.5f);
            if (controller != null)
            {
                controller.StopRecording();
                controller = null;
            }
            Log("已停止，文件：" + outputFile + ".mp4");
            yield return Wait(1.5f);

            SessionState.SetBool(SessionKey, false);
            EditorApplication.isPlaying = false;
        }

        /// <summary>
        /// 卷二 · 秦陵：先看铜车马坑里两乘带伞盖的铜车马，黑场切到一号坑东门道，
        /// 从坡道下到过洞，站在俑列之间左右看。约 42 秒。
        /// </summary>
        static IEnumerator SequenceQinling()
        {
            // 进场黑场 0.6 s，章节介绍 0.8 s 后自己弹出来
            yield return Wait(1.6f);
            Log("1/3 章节卡：第二章 · 帝与陵");
            yield return Wait(2.0f);
            if (ui != null && ui.ModalOpen) ui.Cancel();
            yield return Wait(0.4f);

            Log("2/3 铜车马坑：两乘带伞盖的铜车马");
            yield return LookAt(new Vector3(60f, 1.0f, 6.5f), 2.4f);
            yield return PanTo(-12f, 2.0f);                  // 先看西边那乘
            yield return PanTo(12f, 2.4f);                   // 再扫到东边那乘
            yield return LookAt(new Vector3(60f, 0.9f, 6f), 1.2f);

            // 两处相距一百多米，中间没有可看的东西：黑场切过去，别让观众看空路。
            Log("黑场切到一号坑东门道");
            yield return FadeTo(1f, 0.7f);
            TeleportToGround(new Vector3(166f, 0f, 27.5f), 180f);
            yield return Wait(0.25f);
            yield return FadeTo(0f, 0.7f);
            yield return Wait(0.4f);

            Log("3/3 从东门道下到俑坑过洞");
            yield return LookAt(new Vector3(158f, 2.4f, 19f), 1.0f);      // 先看一眼坡道
            yield return WalkTo(new Vector3(166f, 0.1f, 19.0f), 2.4f, 8f);  // 穿过木栅门
            yield return WalkTo(new Vector3(166f, 0.1f, 12.85f), 1.8f, 8f); // 沿坑东沿往南
            yield return WalkTo(new Vector3(161.8f, 0.1f, 12.85f), 1.8f, 8f);
            yield return LookAt(new Vector3(146f, 0.9f, 12.85f), 1.0f);    // 低头看坡
            yield return WalkTo(new Vector3(150f, 0.1f, 12.85f), 2.0f, 10f);

            walkYawBias = -12f;                                            // 边走边偏头看左侧俑列
            yield return WalkTo(new Vector3(142f, 0.1f, 12.85f), 1.6f, 10f);
            walkYawBias = 0f;

            Log("过洞里左右看看两排俑");
            yield return PanTo(-118f, 2.2f);
            yield return PanTo(-62f, 2.4f);
            yield return LookAt(new Vector3(110f, 1.3f, 12.85f), 2.4f);   // 沿过洞望出去

            Log("录制收尾");
            yield return Wait(0.4f);
            HideHud();                                      // 黑幕在 HUD 下面，先收 HUD 才能黑干净
            yield return FadeTo(1f, 1.2f);
            yield return Wait(0.6f);                        // 留一点纯黑，别在灰半截处断掉
            if (controller != null)
            {
                controller.StopRecording();
                controller = null;
            }
            Log("已停止，文件：" + outputFile + ".mp4");
            yield return Wait(1.5f);

            SessionState.SetBool(SessionKey, false);
            EditorApplication.isPlaying = false;
        }

        /// <summary>
        /// 卷三 · 泉州：从后渚港码头南端起步，先在桥头看洛阳桥，沿石砌岸壁往北，
        /// 看泊着的福船，再走上栈桥望海；黑场切到开元寺双塔收尾。约 50 秒。
        /// </summary>
        static IEnumerator SequenceQuanzhou()
        {
            // 进场黑场 0.6 s，章节介绍 0.8 s 后自己弹出来
            yield return Wait(1.6f);
            Log("1/4 章节卡：第三章 · 港与海");
            yield return Wait(2.0f);
            if (ui != null && ui.ModalOpen) ui.Cancel();
            yield return Wait(0.4f);

            Log("2/4 桥头：洛阳桥的石墩一路伸进海里");
            yield return WalkTo(new Vector3(6.6f, 0.1f, -38.6f), 2.4f, 10f);
            yield return LookAt(new Vector3(-30f, 1.2f, -44.8f), 2.0f);
            yield return PanTo(-78f, 1.0f);                   // 先顺桥身往北扫
            yield return PanTo(-112f, 1.2f);                  // 再回到西南的海面
            yield return LookAt(new Vector3(-30f, 1.2f, -44.8f), 0.5f);

            Log("3/4 沿码头往北：货棚、系缆桩、泊船");
            yield return WalkTo(new Vector3(2.8f, 0.1f, -36f), 3.0f, 8f);
            walkYawBias = -5f;                                // 边走边偏头看左边的海
            yield return WalkTo(new Vector3(2.8f, 0.1f, -26f), 3.6f, 8f);
            walkYawBias = 0f;
            yield return WalkTo(new Vector3(3.0f, 0.1f, -12f), 3.6f, 8f);

            Log("福船：十三舱的船身压在泊位里");
            yield return LookAt(new Vector3(-6.6f, 3.4f, -8f), 2.8f);

            Log("走上栈桥，到桥头望海");
            // 栈桥北沿有栏杆一直到 x≈7.2：先从栏杆东头绕下来，再沿桥面往西，
            // 别斜着跨那条栏杆线，否则角色会贴在上面磨好几秒。
            yield return WalkTo(new Vector3(8.4f, 0.1f, 10.0f), 3.4f, 9f);
            yield return WalkTo(new Vector3(8.4f, 0.1f, 16.6f), 3.0f, 6f);
            yield return WalkTo(new Vector3(-2.0f, 0.1f, 16.6f), 2.2f, 8f);
            yield return LookAt(new Vector3(-24f, 2.0f, 16.4f), 2.8f);

            // 港口和寺塔隔着一整片城：黑场切过去，别让观众看空路。
            Log("黑场切到开元寺双塔");
            yield return FadeTo(1f, 0.8f);
            TeleportToGround(new Vector3(78f, 0f, -9f), 0f);
            yield return Wait(0.2f);
            yield return FadeTo(0f, 0.7f);
            yield return Wait(0.3f);

            Log("4/4 塔前：石仿木的斗拱与五层收分");
            yield return LookAt(new Vector3(78f, 18.1f, 21f), 4.0f);
            yield return PanTo(-4f, 1.4f);
            yield return PanTo(4f, 1.8f);
            yield return LookAt(new Vector3(78f, 18.1f, 21f), 1.2f);

            Log("录制收尾");
            yield return Wait(0.3f);
            HideHud();                                        // 黑幕在 HUD 下面，先收 HUD 才能黑干净
            yield return FadeTo(1f, 1.2f);
            yield return Wait(0.6f);
            if (controller != null)
            {
                controller.StopRecording();
                controller = null;
            }
            Log("已停止，文件：" + outputFile + ".mp4");
            yield return Wait(1.5f);

            SessionState.SetBool(SessionKey, false);
            EditorApplication.isPlaying = false;
        }

        /// <summary>
        /// 终章 · 河山册：回到藏卷阁案前，先看一眼圆窗和两侧卷柜，回头看看来时的门，
        /// 再走到案前看那本摊开的册子，按 E 把最后一叶合上，留一句「此页留给后来者」。约 39 秒。
        /// </summary>
        static IEnumerator SequenceEpilogue()
        {
            // 进场黑场 0.6 s，HUD 和开场那句话自己出来
            yield return Wait(1.6f);
            Log("1/4 终章：藏卷阁");
            yield return Wait(2.4f);

            Log("案与圆窗");
            yield return LookAt(new Vector3(0f, 2.05f, 4.4f), 2.2f);

            Log("2/4 回头看：来时的门");
            yield return LookAt(new Vector3(0f, 1.90f, -4.4f), 4.4f);

            Log("3/4 回身：卷柜与案上的册子");
            yield return LookAt(new Vector3(0f, 1.75f, 4.4f), 3.4f);
            yield return PanTo(30f, 2.0f);                    // 先看东侧卷柜
            yield return PanTo(-30f, 2.2f);                   // 再扫到西侧
            yield return LookAt(new Vector3(0f, 1.75f, 4.4f), 1.6f);

            yield return WalkTo(new Vector3(0f, 0.1f, 0.85f), 1.4f, 4f);
            yield return LookAt(new Vector3(0f, 0.96f, 2.06f), 2.2f);
            yield return Wait(1.2f);

            Log("4/4 合上最后一叶");
            // 册页一翻开就是整屏的正文，先把 HUD 收掉：
            // 不然「下一处」那行引导会正好压在弹窗左边，黑场上也留着亮字。
            HideHud();
            var ending = GameObject.Find("BookEnding");
            var endingIt = ending != null ? ending.GetComponent<Interactable>() : null;
            if (endingIt != null) endingIt.Interact();
            else Log("找不到 BookEnding");
            yield return Wait(9.0f);                          // 停在册页上，留时间读完

            Log("合上，留一句末语");
            yield return Wait(0.3f);
            if (ui != null) ui.Confirm();                     // 淡黑 1.6 s + 末语 3 s
            yield return Wait(4.2f);

            if (controller != null)
            {
                controller.StopRecording();
                controller = null;
            }
            Log("已停止，文件：" + outputFile + ".mp4");
            yield return Wait(1.5f);

            SessionState.SetBool(SessionKey, false);
            EditorApplication.isPlaying = false;
        }

        // ============================================================ 镜头 / 移动

        static void UpdateLook()
        {
            if (player == null || cam == null) return;

            if (walking)
            {
                Vector3 to = walkDest - player.position;
                to.y = 0f;
                if (to.magnitude < 0.35f || Time.realtimeSinceStartup > walkDeadline)
                {
                    walking = false;
                    pc.externalMove = Vector2.zero;
                }
                else
                {
                    float desired = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg + walkYawBias;
                    yawTarget = desired;
                    float diff = Mathf.Abs(Mathf.DeltaAngle(yaw, desired));
                    pc.walkSpeed = walkSpeed;
                    pc.externalMove = diff < 42f ? new Vector2(0f, 1f) : Vector2.zero;
                }
            }
            else if (pc != null)
            {
                pc.externalMove = Vector2.zero;
            }

            yaw = Mathf.MoveTowardsAngle(yaw, yawTarget, 72f * Time.deltaTime);
            pitch = Mathf.MoveTowards(pitch, pitchTarget, 55f * Time.deltaTime);
            player.rotation = Quaternion.Euler(0f, yaw, 0f);
            cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        static IEnumerator WalkTo(Vector3 dest, float speed, float timeout)
        {
            walkDest = dest;
            walkSpeed = speed;
            float dist = Vector3.Distance(
                new Vector3(player.position.x, 0f, player.position.z),
                new Vector3(dest.x, 0f, dest.z));
            walkDeadline = Time.realtimeSinceStartup + Mathf.Max(timeout, dist / Mathf.Max(0.2f, speed) * 3f);
            walking = true;
            while (walking) yield return null;
            yield return Wait(0.2f);
        }

        static IEnumerator LookAt(Vector3 point, float hold)
        {
            Vector3 to = point - cam.transform.position;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            yawTarget = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            pitchTarget = -Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg;
            yield return Wait(hold);
        }

        static IEnumerator PanTo(float yawDegrees, float seconds)
        {
            yawTarget = yawDegrees;
            yield return Wait(seconds);
        }

        static IEnumerator Aerial(float seconds)
        {
            // 收 HUD 和手里那本书，把玩家整体抬到空中，镜头慢慢压低成俯瞰
            HideHud();
            if (pc != null)
            {
                var holder = pc.GetComponentInChildren<BookHolder>(true);
                if (holder != null) holder.gameObject.SetActive(false);
                pc.enabled = false;
            }
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 from = player.position;
            Vector3 to = new Vector3(0f, 28f, -32f);
            float pitchFrom = pitch;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds));
                player.position = Vector3.Lerp(from, to, s);
                yaw = yawTarget = Mathf.LerpAngle(yaw, 0f, s);
                pitch = pitchTarget = Mathf.Lerp(pitchFrom, 33f, s);
                yield return null;
            }
            yield return Wait(3.0f);
        }

        /// <summary>只收 HUD 各块，不关整个 Canvas——收尾还要用它自己的淡出黑幕。</summary>
        static void HideHud()
        {
            if (ui == null) return;
            if (ui.chapterTitle != null) ui.chapterTitle.transform.parent.gameObject.SetActive(false);
            if (ui.hintText != null) ui.hintText.gameObject.SetActive(false);
            if (ui.crosshair != null) ui.crosshair.SetActive(false);
            if (ui.promptText != null) ui.promptText.transform.parent.gameObject.SetActive(false);
            if (ui.toastRoot != null) ui.toastRoot.SetActive(false);
        }

        static IEnumerator FadeToBlack(float seconds)
        {
            yield return FadeTo(1f, seconds);
        }

        /// <summary>把黑幕透明度推到 target（1 黑 / 0 透明），两头都能用。</summary>
        static IEnumerator FadeTo(float target, float seconds)
        {
            if (ui == null || ui.fadeImage == null) yield break;
            ui.fadeImage.gameObject.SetActive(true);
            float from = ui.fadeImage.color.a;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                var c = ui.fadeImage.color;
                c.a = Mathf.Lerp(from, target, Mathf.Clamp01(t / seconds));
                ui.fadeImage.color = c;
                yield return null;
            }
            var end = ui.fadeImage.color;
            end.a = target;
            ui.fadeImage.color = end;
        }

        /// <summary>
        /// 黑场里连人带镜头挪到别处。CharacterController 开着的时候直接改 position 会打架，
        /// 所以先关掉；落点高度用向下射线找地面，免得悬空或陷进去。
        /// </summary>
        static void TeleportToGround(Vector3 xz, float yawDegrees)
        {
            if (player == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            float y = xz.y;
            if (Physics.Raycast(new Vector3(xz.x, 300f, xz.z), Vector3.down,
                    out RaycastHit hit, 600f, ~0, QueryTriggerInteraction.Ignore))
            {
                y = hit.point.y;
            }

            player.position = new Vector3(xz.x, y + 0.08f, xz.z);
            yaw = yawTarget = yawDegrees;
            pitch = pitchTarget = 0f;
            player.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cam != null) cam.transform.localEulerAngles = Vector3.zero;

            if (cc != null) cc.enabled = true;
        }

        // ============================================================ 工具

        static string OutputPath(string which)
        {
            string name = which == "liangzhu" ? "liangzhu_02_entry"
                        : which == "qinling" ? "qinling_03_entry"
                        : which == "quanzhou" ? "quanzhou_04_entry"
                        : which == "epilogue" ? "epilogue_05_entry"
                        : "prologue_01_entry";
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutDirName, name);
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        static bool IsNarrating()
        {
            if (dh == null || NarratingField == null) return false;
            return !string.IsNullOrEmpty((string)NarratingField.GetValue(dh));
        }

        static void Log(string message)
        {
            Debug.Log("[Capture +" + (Time.realtimeSinceStartup - takeStart).ToString("F1") + "s] " + message);
        }
    }
}
#endif
