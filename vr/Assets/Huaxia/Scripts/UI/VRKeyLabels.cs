using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.XR;

namespace Huaxia
{
    /// <summary>
    /// 哪只手在哪儿。整屏面板（遗产立牌）自己收手柄输入时要找设备，
    /// 没必要为了两行代码把 XRInputBridge 绑上去。
    /// </summary>
    public static class VrInput
    {
        static InputDevice left, right;
        static float nextPoll;

        public static InputDevice Left { get { Poll(); return left; } }
        public static InputDevice Right { get { Poll(); return right; } }

        static void Poll()
        {
            if (Time.unscaledTime < nextPoll && left.isValid) return;
            nextPoll = Time.unscaledTime + 1f;
            left = Find(XRNode.LeftHand, InputDeviceCharacteristics.Left);
            right = Find(XRNode.RightHand, InputDeviceCharacteristics.Right);
        }

        static InputDevice Find(XRNode node, InputDeviceCharacteristics hand)
        {
            var byNode = InputDevices.GetDeviceAtXRNode(node);
            if (byNode.isValid && byNode.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 _))
                return byNode;

            var list = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | hand, list);
            return list.Count > 0 ? list[0] : default(InputDevice);
        }

        public static bool Button(InputDevice d, InputFeatureUsage<bool> usage)
        {
            return d.isValid && d.TryGetFeatureValue(usage, out bool v) && v;
        }

        public static float Axis(InputDevice d, InputFeatureUsage<float> usage)
        {
            return d.isValid && d.TryGetFeatureValue(usage, out float v) ? v : 0f;
        }

        public static Vector2 Stick(InputDevice d)
        {
            return d.isValid && d.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v)
                ? v : Vector2.zero;
        }
    }

    /// <summary>
    /// 头显里不该出现键盘键名。游戏层所有写着「按 E」「Tab」「空格」的字，
    /// 在 <see cref="Active"/> 为真时过一遍这里，换成手柄上的键。
    ///
    /// 只做字面替换，不碰语义：句子还是原来那句，只是末了告诉人用哪只手的哪个键。
    /// 键位跟 <see cref="XRInputBridge"/> 是同一套：
    ///   右手扳机 = 桌面端的 E / 空格 / 鼠标左键
    ///   右手 B   = Esc
    ///   左手 grip = Tab（取册子）
    ///   左手 Y   = Q（合卷回山）
    ///   左摇杆按下 = V（整体视角）
    /// </summary>
    public static class VRKeyLabels
    {
        /// <summary>XR 接管后由 <see cref="XRInputBridge"/> 置真。</summary>
        public static bool Active;

        static readonly Regex SingleE = new Regex(@"(?<![A-Za-z])E(?![A-Za-z])");
        static readonly Regex SingleQ = new Regex(@"(?<![A-Za-z])Q(?![A-Za-z])");
        static readonly Regex SingleV = new Regex(@"(?<![A-Za-z])V(?![A-Za-z])");
        static readonly Regex WordTab = new Regex(@"\bTab\b");
        static readonly Regex WordEsc = new Regex(@"\bEsc\b");
        static readonly Regex RepeatSep = new Regex(@"(\s*·\s*){2,}");
        static readonly Regex SepBeforeNewline = new Regex(@"·[ \t]*\n");
        static readonly Regex DoubleSpace = new Regex(@"[ \t]{2,}");

        public static string Fix(string s)
        {
            if (!Active || string.IsNullOrEmpty(s)) return s;

            s = s.Replace("鼠标左键", "右手扳机")
                 .Replace("鼠标右键", "右手扳机")
                 .Replace("鼠标看", "转头看")
                 .Replace("松开鼠标", "")
                 .Replace("释放鼠标", "")
                 .Replace("松开光标", "")
                 .Replace("WASD", "左摇杆")
                 .Replace("Shift 奔跑", "")
                 .Replace("Shift", "")
                 .Replace("「、」跳跃", "")
                 .Replace("空格", "扳机")
                 .Replace("滚轮", "扳机")
                 .Replace("［E］", "［扳机］")
                 .Replace("【E】", "［扳机］")
                 .Replace("按 E", "按扳机")
                 .Replace("按\nE", "按扳机")
                 .Replace("按 V", "按左摇杆")
                 .Replace("按 Q", "按左手 Y")
                 .Replace("E 说话", "扳机 说话")
                 .Replace("E 互动", "扳机 互动")
                 .Replace("E 合上", "扳机 合上")
                 .Replace("E 开门", "扳机 开门")
                 .Replace("E 细看", "扳机 细看")
                 .Replace("E 回顾", "扳机 回顾");

            s = WordTab.Replace(s, "左手 grip");
            s = WordEsc.Replace(s, "右手 B");
            s = SingleE.Replace(s, "扳机");
            s = SingleQ.Replace(s, "左手 Y");
            s = SingleV.Replace(s, "左摇杆按下");

            // 删掉 Shift / 跳跃之后，会剩下「 ·  · 」这样的空档
            s = RepeatSep.Replace(s, " · ");
            s = SepBeforeNewline.Replace(s, "\n");
            s = DoubleSpace.Replace(s, " ");
            s = s.Replace("鼠标", "转头");

            return s.Trim();
        }

        /// <summary>
        /// 场景里写死的 HUD（底部提示条、目标栏的键位说明）不会被任何脚本重设，
        /// 进场时扫一遍。只在 XR 接管时调用一次。
        /// </summary>
        public static void SweepSceneTexts()
        {
            if (!Active) return;
            var texts = Object.FindObjectsOfType<UnityEngine.UI.Text>(true);
            foreach (var t in texts)
            {
                if (t == null || !t.gameObject.scene.IsValid()) continue;
                string before = t.text;
                if (string.IsNullOrEmpty(before)) continue;
                string after = Fix(before);
                if (after != before) t.text = after;
            }
        }
    }
}
