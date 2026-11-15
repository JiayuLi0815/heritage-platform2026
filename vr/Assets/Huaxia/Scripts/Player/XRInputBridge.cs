using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace Huaxia
{
    /// <summary>
    /// PICO 4 / OpenXR 手柄接入。只有 <see cref="XRSettings.isDeviceActive"/> 为真时才接管，
    /// 桌面端完全不改变。
    ///
    /// 映射（PICO 4 手柄）：
    ///   左/右摇杆         前后左右走（任一只都行，推得多的那只生效）
    ///   右手 grip + 右摇杆 整步转身 30°（不做平滑转身，少晕）
    ///   右手扳机          互动 / 弹窗确认 / 书抬起时翻下一页
    ///   右手 A            短按确认/互动；按住 1.2 倍速
    ///   右手 B            短按跳；弹窗打开时是关闭
    ///   左手 grip         抬书、收书
    ///
    /// 头显姿态用 UnityEngine.XR.InputTracking 读，不依赖 Input Actions 资产；
    /// HUD 在 VR 里从屏幕空间改成挂在头前的世界空间画布。
    /// </summary>
    [RequireComponent(typeof(DesktopPlayerController))]
    public class XRInputBridge : MonoBehaviour
    {
        [Header("手柄")]
        public float snapTurnDegrees = 30f;
        public float snapTurnCooldown = 0.4f;
        public float triggerThreshold = 0.55f;
        public float rayLength = 5f;
        [Tooltip("组合键回开头的长按秒数：Y 长按＝本章重来，X+A 同按＝从头开始")]
        public float restartHoldSeconds = 3f;
        [Tooltip("右手 A 按住多久算加速；比这短的一按仍然算确认/互动")]
        public float sprintHoldSeconds = 0.25f;

        [Header("画面")]
        [Tooltip("眼纹理超采样倍率：1.0 是运行时默认；1.25 明显更清楚，也更吃性能")]
        public float eyeResolutionScale = 1.25f;
        [Tooltip("多重采样抗锯齿：0 / 2 / 4 / 8")]
        public int msaa = 4;

        DesktopPlayerController pc;
        BookHolder book;
        ReturnHome returnHome;
        BirdEyeView birdEye;
        Camera head;
        GameObject leftGO;
        GameObject rightGO;
        LineRenderer aimLine;
        InputDevice leftHand;
        InputDevice rightHand;
        float turnTimer;
        float nextDevicePoll;
        float nextStickLog;
        float homeHold;
        float bothHold;
        float aHold;
        bool aSprinting;
        bool triggerHeld, gripHeld, confirmHeld, cancelHeld, aerialHeld;
        bool ready;

        void Start()
        {
            pc = GetComponent<DesktopPlayerController>();
            book = GetComponentInChildren<BookHolder>();
            returnHome = GetComponent<ReturnHome>();
            if (returnHome == null) returnHome = FindObjectOfType<ReturnHome>();
            birdEye = FindObjectOfType<BirdEyeView>();
            head = pc.playerCamera != null ? pc.playerCamera : GetComponentInChildren<Camera>();

            if (XRSettings.isDeviceActive) Activate();
            else StartCoroutine(WaitForDevice());
        }

        /// <summary>
        /// OpenXR loader 是自动加载的，但初始化要跨几帧：场景 Start 时
        /// isDeviceActive 可能还是 false。等一小会儿再决定要不要接管，
        /// 桌面端等满 6 秒就安静退出。
        /// </summary>
        IEnumerator WaitForDevice()
        {
            float t = 0f;
            while (t < 6f && !XRSettings.isDeviceActive)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!XRSettings.isDeviceActive)
            {
                enabled = false;
                yield break;
            }
            Activate();
        }

        void Activate()
        {
            pc.xrActive = true;
            pc.externalMove = Vector2.zero;
            ApplyDisplaySettings();
            // 先把「键名」切成手柄的叫法，再改 HUD，场景里写死的字才不会被漏掉
            VRKeyLabels.Active = true;
            BuildHands();
            MoveHudToWorldSpace();
            RefreshDevices();
            ready = true;
            StartCoroutine(SweepLabelsLater());
            Debug.Log("[Huaxia] XR 设备已接管：" + XRSettings.loadedDeviceName);
        }

        /// <summary>
        /// 头显里的清晰度主要看两件事：眼纹理的实际像素数，和边缘抗锯齿。
        /// 超采样比单纯调屏幕分辨率更直接——它是按倍数把两只眼的画面放大再缩回去。
        /// </summary>
        void ApplyDisplaySettings()
        {
            float scale = Mathf.Clamp(eyeResolutionScale, 1f, 2f);
            XRSettings.eyeTextureResolutionScale = scale;

            int aa = msaa >= 8 ? 8 : msaa >= 4 ? 4 : msaa >= 2 ? 2 : 0;
            QualitySettings.antiAliasing = aa;

            Debug.Log("[Huaxia] 画面：眼纹理 x" + scale.ToString("F2") +
                      "，MSAA " + aa + "x，纹理限制 " + QualitySettings.globalTextureMipmapLimit);
        }

        /// <summary>各场景的 TaskManager / 弘文会在 Start 里写一遍 HUD，等一帧再扫，才扫得到。</summary>
        IEnumerator SweepLabelsLater()
        {
            VRKeyLabels.SweepSceneTexts();
            yield return null;
            VRKeyLabels.SweepSceneTexts();
            yield return new WaitForSeconds(1f);
            VRKeyLabels.SweepSceneTexts();
            Debug.Log("[Huaxia] 左=" + (leftHand.isValid ? leftHand.name : "无") +
                      " 右=" + (rightHand.isValid ? rightHand.name : "无") +
                      " 头偏航=" + (head != null ? head.transform.localEulerAngles.y.ToString("F0") : "?"));
        }

        void BuildHands()
        {
            leftGO = new GameObject("XR Left Hand");
            leftGO.transform.SetParent(transform, false);
            rightGO = new GameObject("XR Right Hand");
            rightGO.transform.SetParent(transform, false);
            pc.xrRay = rightGO.transform;

            // 一条不刺眼的瞄准线，让体验者知道手往哪儿指
            aimLine = rightGO.AddComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.useWorldSpace = true;
            aimLine.startWidth = 0.012f;
            aimLine.endWidth = 0.004f;
            aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aimLine.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var m = new Material(shader);
                m.color = new Color(0.95f, 0.82f, 0.55f, 0.55f);
                aimLine.material = m;
            }
        }

        void RefreshDevices()
        {
            leftHand = FindController(XRNode.LeftHand, InputDeviceCharacteristics.Left);
            rightHand = FindController(XRNode.RightHand, InputDeviceCharacteristics.Right);

            // 手柄还没被系统枚举出来时，至少让瞄准线跟头走，别让人完全没法看
            if (!rightHand.isValid && head != null)
            {
                rightGO.transform.position = head.transform.position;
                rightGO.transform.rotation = head.transform.rotation;
            }
        }

        /// <summary>
        /// 先按 XRNode 取：这样拿到的设备和画瞄准线用的那只手是同一只，
        /// 哪怕运行时把左右特征标反了，摇杆也不会跑到另一只手上。
        /// 取不到再退回按 Left/Right 特征找。
        /// </summary>
        static InputDevice FindController(XRNode node, InputDeviceCharacteristics hand)
        {
            var byNode = InputDevices.GetDeviceAtXRNode(node);
            // 手部追踪设备也挂在同一个节点上，但它没有扳机和摇杆：那种就不算手柄
            if (byNode.isValid && byNode.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 _))
                return byNode;

            var wanted = InputDeviceCharacteristics.Controller | hand;
            var list = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(wanted, list);
            return list.Count > 0 ? list[0] : default(InputDevice);
        }

        void Update()
        {
            if (!ready) return;

            nextDevicePoll -= Time.deltaTime;
            if (nextDevicePoll <= 0f)
            {
                nextDevicePoll = 1f;
                RefreshDevices();
            }

            // ---------- 弹窗确认/关闭：弹窗打开时优先 ----------
            bool confirmDown = ButtonDown(rightHand, CommonUsages.primaryButton, ref confirmHeld);
            bool cancelDown = ButtonDown(rightHand, CommonUsages.secondaryButton, ref cancelHeld);
            bool triggerDown = AxisDown(rightHand, CommonUsages.trigger, ref triggerHeld);
            bool gripDown = ButtonDown(leftHand, CommonUsages.gripButton, ref gripHeld);
            bool aerialDown = ButtonDown(leftHand, CommonUsages.primary2DAxisClick, ref aerialHeld);

            // X（左手）+ A（右手）
            bool xNow = Held(leftHand, CommonUsages.primaryButton);
            bool aNow = Held(rightHand, CommonUsages.primaryButton);

            var ui = UIManager.Instance;
            if (ui != null && ui.ModalOpen)
            {
                if (triggerDown || confirmDown) ui.Confirm();
                else if (cancelDown) ui.Cancel();
                ResetHeld();
                return;
            }
            if (ui != null && ui.BlocksPlayer)
            {
                ResetHeld();
                return;
            }

            // ---------- 右手 A：短按确认/互动，按住 1.2 倍速 ----------
            // 长按不再同时触发互动：跑向立牌的路上，手一直按着 A 也不会把面板按开。
            bool aShort = false;
            if (aNow)
            {
                aHold += Time.deltaTime;
                if (aHold >= sprintHoldSeconds) aSprinting = true;
            }
            else
            {
                if (aHold > 0f && !aSprinting && !xNow) aShort = true;
                aHold = 0f;
                aSprinting = false;
            }
            pc.externalSprint = aNow && aSprinting && !xNow;

            // ---------- 右手 B：短按跳 ----------
            if (cancelDown && (book == null || !book.Raised)) pc.Jump();

            // ---------- 书 ----------
            if (gripDown && book != null) book.ToggleRaised();
            if (triggerDown && book != null && book.Raised)
            {
                pc.externalMove = Vector2.zero;
                book.TurnPage(1);
                return;
            }

            // ---------- 左手 X + 右手 A 同按 3 秒：回到最开始 ----------
            // 组合键是故意的：从头开始要清进度，不能跟随手一按混在一起。
            if (xNow && aNow)
            {
                bothHold += Time.deltaTime;
            }
            else if (bothHold > 0f)
            {
                float heldBoth = bothHold;
                bothHold = 0f;
                if (heldBoth >= restartHoldSeconds)
                {
                    ResetHeld();
                    ExperienceRestart.RestartAll();
                    return;
                }
            }

            // ---------- 左手 Y：短按合卷回山（= Q），长按 3 秒回到本章开头 ----------
            bool yHeld = Held(leftHand, CommonUsages.secondaryButton);
            if (yHeld)
            {
                homeHold += Time.deltaTime;
            }
            else if (homeHold > 0f)
            {
                float held = homeHold;
                homeHold = 0f;
                pc.externalMove = Vector2.zero;
                if (held >= restartHoldSeconds) ExperienceRestart.RestartChapter();
                else if (returnHome != null) returnHome.Ask();
                return;
            }

            // ---------- 整体视角（左摇杆按下 = 桌面端的 V） ----------
            if (aerialDown && birdEye != null)
            {
                pc.externalMove = Vector2.zero;
                birdEye.Toggle();
                return;
            }

            // ---------- 移动与转身 ----------
            // 左右摇杆都能走，哪只推得多就听哪只；按住右手 grip 时，右摇杆改回转身。
            bool turnMod = Held(rightHand, CommonUsages.gripButton);
            Vector2 leftStick = Stick(leftHand);
            Vector2 rightStick = Stick(rightHand);

            bool fromRight = false;
            Vector2 move = leftStick;
            if (!turnMod && rightStick.sqrMagnitude > move.sqrMagnitude)
            {
                move = rightStick;
                fromRight = true;
            }
            pc.externalMove = move;

            // 走反了的时候靠这条日志分辨：是摇杆本身反了，还是头显追踪空间转了 180°
            if (move.sqrMagnitude > 0.36f && Time.time >= nextStickLog)
            {
                nextStickLog = Time.time + 2f;
                Debug.Log("[Huaxia] stick=(" + move.x.ToString("F2") + "," + move.y.ToString("F2") +
                          ") 源=" + (fromRight ? "右" : "左") +
                          " headYaw=" + (head != null ? head.transform.localEulerAngles.y.ToString("F0") : "?"));
            }

            if (turnMod)
            {
                turnTimer -= Time.deltaTime;
                if (turnTimer <= 0f && Mathf.Abs(rightStick.x) > 0.7f)
                {
                    pc.SnapTurn(Mathf.Sign(rightStick.x) * snapTurnDegrees);
                    turnTimer = snapTurnCooldown;
                }
            }

            // ---------- 世界互动 ----------
            // 先看手上有没有指着的东西；没有就试脚下这道界门——界门不是 Interactable，
            // 桌面端靠 E 键单独处理，头显里没有键盘事件可听。
            // A 的互动放在松手时判定：按得短才是互动，按得久是加速。
            if (triggerDown || aShort)
            {
                if (!pc.InteractFocused()) ScenePortal.TryActivate();
            }
        }

        void LateUpdate()
        {
            if (!ready) return;

            // 头显姿态
            if (head != null)
            {
                head.transform.localPosition = InputTracking.GetLocalPosition(XRNode.CenterEye);
                head.transform.localRotation = InputTracking.GetLocalRotation(XRNode.CenterEye);
            }

            // 手柄姿态
            if (leftHand.isValid) ApplyPose(XRNode.LeftHand, leftGO.transform, true);
            if (rightHand.isValid) ApplyPose(XRNode.RightHand, rightGO.transform, false);

            // 瞄准线
            if (aimLine != null && rightGO != null)
            {
                Vector3 a = rightGO.transform.position;
                Vector3 b = a + rightGO.transform.forward * rayLength;
                if (Physics.Raycast(a, rightGO.transform.forward, out RaycastHit hit, rayLength,
                                    ~0, QueryTriggerInteraction.Collide))
                    b = hit.point;
                aimLine.SetPosition(0, a);
                aimLine.SetPosition(1, b);
            }
        }

        void ApplyPose(XRNode node, Transform t, bool isLeft)
        {
            Vector3 pos = InputTracking.GetLocalPosition(node);
            Quaternion rot = InputTracking.GetLocalRotation(node);
            if (pos.sqrMagnitude < 1e-6f && rot == Quaternion.identity)
            {
                // 有些运行时在初始几帧不给手柄姿态，给一个腰侧的占位，别让线从脚下射出去
                pos = isLeft ? new Vector3(-0.25f, 1.15f, 0.25f) : new Vector3(0.25f, 1.15f, 0.25f);
                rot = Quaternion.Euler(10f, 0f, 0f);
            }
            t.localPosition = pos;
            t.localRotation = rot;
        }

        static bool Held(InputDevice device, InputFeatureUsage<bool> usage)
        {
            return device.isValid && device.TryGetFeatureValue(usage, out bool v) && v;
        }

        /// <summary>摇杆值，带 0.15 死区：手柄用久了中位会漂，别让玩家自己往后走。</summary>
        static Vector2 Stick(InputDevice device)
        {
            if (!device.isValid) return Vector2.zero;
            if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v)) return Vector2.zero;
            return v.sqrMagnitude < 0.0225f ? Vector2.zero : v;
        }

        /// <summary>弹窗/整屏面板接管时，把移动、加速和长按计时一起清掉。</summary>
        void ResetHeld()
        {
            pc.externalMove = Vector2.zero;
            pc.externalSprint = false;
            homeHold = 0f;
            bothHold = 0f;
            aHold = 0f;
            aSprinting = false;
        }

        static bool ButtonDown(InputDevice device, InputFeatureUsage<bool> usage, ref bool held)
        {
            if (!device.isValid) { held = false; return false; }
            bool now = device.TryGetFeatureValue(usage, out bool v) && v;
            bool down = now && !held;
            held = now;
            return down;
        }

        static bool AxisDown(InputDevice device, InputFeatureUsage<float> usage, ref bool held)
        {
            if (!device.isValid) { held = false; return false; }
            bool now = device.TryGetFeatureValue(usage, out float v) && v >= 0.55f;
            bool down = now && !held;
            held = now;
            return down;
        }

        /// <summary>屏幕空间 HUD 在头显里看不见，改成挂在头前 2.2 m 的世界空间画布。</summary>
        void MoveHudToWorldSpace()
        {
            var ui = UIManager.Instance;
            if (ui == null || head == null) return;

            if (ui.hintText != null)
                ui.hintText.text = "左右摇杆 走　A 按住加速　B 跳　扳机 互动　左手 grip 取册子\n右手 grip+右摇杆 转身　Y 合卷（长按3秒重来）　X+A 3秒 从头";

            var canvas = ui.GetComponent<Canvas>();
            if (canvas == null) return;

            var scaler = ui.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null) scaler.enabled = false;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head;

            var rt = canvas.GetComponent<RectTransform>();
            if (rt == null) return;

            canvas.transform.SetParent(head.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1920f, 1080f);
            // 1.8 m 处、约 60° 宽。数字偏保守：字号在头显里还嫌小的话，
            // 调这里的两项（越近 / 越大越好读，但会挡视野），别去改场景里的 HUD 布局。
            canvas.transform.localPosition = new Vector3(0f, 0f, 1.8f);
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * 0.0011f;
        }
    }
}
