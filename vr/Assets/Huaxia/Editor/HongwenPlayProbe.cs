#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// 弘文的播放模式走查：进 Play Mode，先确认她站在广场上不吭声；
    /// 再把玩家传送进良渚立牌前的引导圈，看她走过来、招手、讲这一处；
    /// 接着把三块立牌的面板依次开关一遍，看她有没有保持安静、有没有指路、三卷读完有没有庆祝。
    ///
    /// 只验证状态和剪辑名字（Animation 的权重、当前位置、AudioSource 的片段），
    /// 观感还得自己戴头盔看。
    ///
    /// 用法：命令行 -executeMethod Huaxia.EditorTools.HongwenPlayProbe.Run。
    /// </summary>
    public static class HongwenPlayProbe
    {
        const string SessionKey = "huaxia_hongwen_probe";
        const float Timeout = 170f;

        static double startTime;
        static int step;
        static int pass, fail;
        static DigitalHuman dh;
        static readonly HashSet<string> seen = new HashSet<string>();
        static Vector3 walkFrom;
        static Vector3 playerFrom;
        static GuideRing ring;
        static float teleT;
        static float moved;
        static bool walked;

        [MenuItem("Huaxia/弘文动画走查")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Hongwen] 已经在播放模式里。");
                return;
            }
            GameState.ResetAll();
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Archive.unity");
            Debug.Log("[Hongwen] 进入播放模式");
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

            float t = (float)(Now - startTime);
            if (t > Timeout)
            {
                Fail("整体超时", "step=" + step);
                Finish();
                return;
            }

            if (dh == null)
            {
                dh = Object.FindObjectOfType<DigitalHuman>();
                if (dh != null)
                {
                    Debug.Log("[Hongwen] 找到弘文：" + dh.transform.position.ToString("F2"));
                    walkFrom = dh.transform.position;
                }
                return;
            }

            // 记下播过的每一段剪辑，最后看十段里用到了几段
            if (dh.anim != null)
            {
                foreach (AnimationState st in dh.anim)
                    if (st.enabled && st.weight > 0.05f) seen.Add(st.name);
            }

            switch (step)
            {
                case 0:
                    if (t < 1.5f) return;
                    step = 1;
                    // 开场说明那一屏挡着，先替玩家按掉
                    var ui = UIManager.Instance;
                    if (ui != null && ui.ModalOpen) ui.Confirm();
                    Check("弘文挂了 Animation 组件", dh.anim != null, "");
                    Check("剪辑至少十段", dh.anim != null && dh.anim.GetClipCount() >= 10,
                        dh.anim != null ? dh.anim.GetClipCount().ToString() : "无");
                    Check("一进场景就有剪辑在播", Playing(), CurrentStates());
                    Check("一进序章她没开口（等玩家走进圈）", !dh.IsSpeaking, Speaker());
                    var rings = Object.FindObjectsOfType<GuideRing>();
                    Check("广场上摆着引导圈", rings.Length >= 4, "实际 " + rings.Length);
                    ring = null;
                    for (int i = 0; i < rings.Length; i++)
                        if (rings[i].heritageId == "1592") ring = rings[i];
                    Check("良渚立牌前那只引导圈在", ring != null, "");
                    break;

                case 1:
                    // 玩家还站在山门口：她不该自言自语，也不该自己跑起来
                    if (t < 4.0f) return;
                    step = 2;
                    moved = Vector3.Distance(dh.transform.position, walkFrom);
                    Check("玩家没走近，她就不开口", !dh.IsSpeaking, Speaker());
                    Check("玩家没走近，她也不乱跑", moved < 0.6f, "位移 " + moved.ToString("0.00") + " m");
                    if (ring == null) { Finish(); return; }
                    Teleport(ring.transform.position);
                    teleT = t;
                    playerFrom = PlayerPos();
                    Debug.Log("[Hongwen] 把玩家送进良渚的圈：" + ring.transform.position.ToString("F2"));
                    break;

                case 2:
                    // 走进圈里：她该真的走过来，而不是原地转身。
                    // 她平时站在山门里的说明牌前，玩家把她从广场那头叫过来得走三十来米，
                    // 等多久不定 —— 以「她赶到并开口」为准，而不是掐一个固定秒数。
                    if (!walked)
                    {
                        moved = Vector3.Distance(dh.transform.position, walkFrom);
                        walked = moved > 0.8f;
                    }
                    bool arrived = walked && dh.IsSpeaking;
                    if (!arrived && t < teleT + 26f) return;

                    step = 3;
                    moved = Vector3.Distance(dh.transform.position, walkFrom);
                    walked = moved > 0.8f;
                    teleT = t;              // 以她开口这一刻重新起算，后面 +11/+15… 的偏移照旧
                    Check("踩进圈里她会走过来（真的位移）", walked, "位移 " + moved.ToString("0.00") + " m");
                    Check("走过来先招手（greet_01 / greet_02 播过）",
                        seen.Contains("greet_01") || seen.Contains("greet_02"), Seen());
                    Check("走过来之后开口讲良渚", dh.IsSpeaking, "当前 " + Speaker());
                    Check("讲的是良渚那条短介绍", Here("arrive_1592"), Speaker());
                    break;

                case 3:
                    if (t < teleT + 11f) return;
                    step = 4;
                    Check("玩家的位置没被她挤走", Vector3.Distance(PlayerPos(), playerFrom) < 0.5f, "");
                    Check("讲话时她在换手势（不止一段剪辑）", seen.Count >= 3, Seen());
                    OpenPanel("1592");
                    break;

                case 4:
                    if (t < teleT + 15f) return;
                    step = 5;
                    Check("打开面板她就安静下来", !dh.IsSpeaking, Speaker());
                    Check("听讲时抱着手臂等（agree / fold_arms）",
                        seen.Contains("agree") || seen.Contains("fold_arms"), Seen());
                    ClosePanel();
                    break;

                case 5:
                    if (t < teleT + 19f) return;
                    step = 6;
                    Check("合上面板她给你念路线指引", dh.IsSpeaking, Speaker());
                    Check("念的是良渚那条", Here("guide_1592"), Speaker());
                    Check("指路时她朝那条岔路转头", Turned(), Yaw());
                    OpenPanel("441");
                    break;

                case 6:
                    if (t < teleT + 23f) return;
                    step = 7;
                    ClosePanel();
                    break;

                case 7:
                    if (t < teleT + 27f) return;
                    step = 8;
                    Check("第二卷也指了路", Here("guide_441"), Speaker());
                    OpenPanel("1561");
                    break;

                case 8:
                    if (t < teleT + 31f) return;
                    step = 9;
                    ClosePanel();
                    break;

                case 9:
                    if (t < teleT + 35f) return;
                    step = 10;
                    Check("三卷读完，她开始庆祝（cheer 播过）", seen.Contains("cheer"), Seen());
                    Check("庆祝也有台词", dh.IsSpeaking, Speaker());
                    break;

                case 10:
                    if (t < teleT + 50f) return;
                    step = 11;
                    Check("十段剪辑用上了大半", seen.Count >= 6, "用到 " + seen.Count + " 段：" + Seen());
                    Finish();
                    break;
            }
        }

        static bool Here(string clip)
        {
            string now = Speaker();
            return now != null && now.Contains(clip);
        }

        static Vector3 PlayerPos()
        {
            var pc = Object.FindObjectOfType<DesktopPlayerController>();
            return pc != null ? pc.transform.position : Vector3.zero;
        }

        static void Teleport(Vector3 pos)
        {
            var pc = Object.FindObjectOfType<DesktopPlayerController>();
            if (pc == null) { Fail("找不到玩家控制器", ""); return; }
            var cc = pc.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            pos.y = pc.transform.position.y;
            pc.transform.position = pos;
            if (cc != null) cc.enabled = true;
        }

        static void OpenPanel(string id)
        {
            var panel = Object.FindObjectOfType<HeritagePanel>(true);
            if (panel == null) { Fail("找不到遗产面板", ""); return; }
            // 走玩家那条路：由立牌的 Interactable 打开，board/leadTo 才跟真机一致
            var boards = Object.FindObjectsOfType<Interactable>(true);
            for (int i = 0; i < boards.Length; i++)
            {
                if (boards[i].heritageId != id) continue;
                panel.Open(id, boards[i].leadTo, boards[i].board);
                Debug.Log("[Hongwen] 打开面板 " + id);
                return;
            }
            panel.Open(id);
            Debug.Log("[Hongwen] 打开面板 " + id + "（场景里找不到对应立牌）");
        }

        static void ClosePanel()
        {
            var panel = Object.FindObjectOfType<HeritagePanel>(true);
            if (panel == null) { Fail("找不到遗产面板", ""); return; }
            panel.Close();
            Debug.Log("[Hongwen] 合上面板");
        }

        static string Speaker()
        {
            if (dh == null) return "无";
            var src = dh.GetComponent<AudioSource>();
            return src != null && src.clip != null ? src.clip.name : "静音";
        }

        static string Seen()
        {
            var list = new List<string>(seen);
            list.Sort();
            return string.Join("/", list.ToArray());
        }

        static bool Playing()
        {
            if (dh.anim == null) return false;
            foreach (AnimationState st in dh.anim)
                if (st.enabled && st.weight > 0.05f) return true;
            return false;
        }

        static string CurrentStates()
        {
            if (dh.anim == null) return "无 Animation";
            var sb = new System.Text.StringBuilder();
            foreach (AnimationState st in dh.anim)
                if (st.enabled && st.weight > 0.05f)
                    sb.Append(st.name).Append("(").Append(st.weight.ToString("0.00")).Append(") ");
            return sb.ToString();
        }

        static bool Turned()
        {
            if (dh == null || dh.body == null) return false;
            Vector3 dir;
            if (!dh.RouteDirection("1592", out dir)) return false;
            Vector3 fwd = dh.body.rotation * Vector3.back;
            return Vector3.Dot(fwd, dir) > 0.5f;
        }

        static string Yaw()
        {
            if (dh == null || dh.body == null) return "无";
            return "body yaw=" + dh.body.localEulerAngles.y.ToString("0") + "°";
        }

        static void Check(string what, bool ok, string detail)
        {
            if (ok) { pass++; Debug.Log("[Hongwen] OK   " + what); }
            else { fail++; Debug.LogError("[Hongwen] FAIL " + what + (string.IsNullOrEmpty(detail) ? "" : "　→ " + detail)); }
        }

        static void Fail(string what, string detail)
        {
            Check(what, false, detail);
        }

        static void Finish()
        {
            Debug.Log("[Hongwen] " + (fail == 0 ? "通过" : "失败") + "　pass=" + pass + " fail=" + fail);
            SessionState.SetBool(SessionKey, false);
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
#endif
