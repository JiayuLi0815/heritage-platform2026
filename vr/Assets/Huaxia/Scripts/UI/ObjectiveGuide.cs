using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 左上角目标栏底下的那一行「下一处在哪」。
    /// 每半秒挑一处还没看过的线索，算出方向与距离，写成一句人话。
    /// 遗址里只认本卷主线索；序章里按「没去过的那一卷」优先，其次才是最近的牌子。
    /// </summary>
    public class ObjectiveGuide : MonoBehaviour
    {
        public float refreshSeconds = 0.4f;

        float timer;
        Interactable[] cache;
        ScenePortal[] portals;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = refreshSeconds;

            var ui = UIManager.Instance;
            var pc = FindObjectOfType<DesktopPlayerController>();
            if (ui == null || pc == null) return;

            if (cache == null || cache.Length == 0)
                cache = FindObjectsOfType<Interactable>();

            Vector3 from = pc.transform.position;

            // 山谷里没有 TaskManager：目标就是「下一卷」那道界门。
            // 之前只认 Interactable，而路尽头的 ScenePortal 不是互动点，
            // 所以刚回山谷时引导会往回指到一块说明牌——人不知道该往哪走。
            if (TaskManager.Instance == null)
            {
                if (portals == null || portals.Length == 0)
                    portals = FindObjectsOfType<ScenePortal>();

                ScenePortal gate = NextPortal(from);
                if (gate != null)
                {
                    Vector3 gto = gate.transform.position - from;
                    gto.y = 0f;
                    string gateName = string.IsNullOrEmpty(gate.destinationLabel)
                        ? gate.sceneToLoad
                        : gate.destinationLabel;
                    ui.SetGuide("下一程　" + gateName + "　<color=#B9AE97>" +
                                DirectionName(pc.ViewForward, gto) + " " +
                                Mathf.RoundToInt(gto.magnitude) + " 米</color>");
                    return;
                }
            }

            Interactable best = null;
            float bestScore = float.MaxValue;

            foreach (var it in cache)
            {
                if (it == null || it.Consumed) continue;

                // 遗址里：只指主线索，可选线索不抢方向
                if (TaskManager.Instance != null && !string.IsNullOrEmpty(it.objectiveId))
                {
                    if (!TaskManager.Instance.IsPrimary(it.objectiveId)) continue;
                }

                float d = Vector3.Distance(from, it.transform.position);

                // 序章里：还没去过的那一卷优先，哪怕它比身边的牌子远
                float score = d;
                if (it.kind == InteractKind.Chapter && !string.IsNullOrEmpty(it.sceneToLoad))
                {
                    string id = it.sceneToLoad.ToLowerInvariant();
                    if (!GameState.IsChapterVisited(id)) score -= 1000f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = it;
                }
            }

            if (best == null)
            {
                ui.SetGuide("<color=#8C8579>本卷的线索都看过了 —— 合卷回山（Q），或沿原路回入口界石</color>");
                return;
            }

            Vector3 to = best.transform.position - from;
            to.y = 0f;
            float dist = to.magnitude;
            string dir = DirectionName(pc.ViewForward, to);
            ui.SetGuide("下一处　" + best.title + "　<color=#B9AE97>" + dir + " " +
                        Mathf.RoundToInt(dist) + " 米</color>");
        }

        /// <summary>
        /// 山谷里该往哪道门走：没去过的卷优先，其次是去过但没读完的卷；
        /// 三卷都读完了才指藏卷阁（终章）。全都读完时不再指旧路。
        /// </summary>
        ScenePortal NextPortal(Vector3 from)
        {
            ScenePortal best = null;
            float bestScore = float.MaxValue;

            foreach (var p in portals)
            {
                if (p == null || string.IsNullOrEmpty(p.sceneToLoad)) continue;

                string id = p.sceneToLoad.ToLowerInvariant();
                int chapter = System.Array.IndexOf(GameState.Chapters, id);
                float score;

                if (chapter >= 0)
                {
                    if (!GameState.IsChapterVisited(id)) score = -3000f;
                    else if (!GameState.IsChapterComplete(id)) score = -2000f;
                    else score = 0f;            // 读完了：只有别无选择才指回去
                }
                else if (p.sceneToLoad == "Epilogue")
                {
                    // 门开不等于该去：三卷都读完才把终章端出来
                    if (!GameState.AllChaptersComplete) continue;
                    score = -4000f;
                }
                else
                {
                    continue;
                }

                score += Vector3.Distance(from, p.transform.position);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            return best;
        }

        static string DirectionName(Vector3 forward, Vector3 to)
        {
            if (to.sqrMagnitude < 0.01f) return "就在脚下";

            float angle = Vector3.SignedAngle(forward, to.normalized, Vector3.up);
            float a = Mathf.Abs(angle);
            string side = angle >= 0f ? "右" : "左";

            if (a < 22.5f) return "正前方";
            if (a < 67.5f) return side + "前方";
            if (a < 112.5f) return side + "侧";
            if (a < 157.5f) return side + "后方";
            return "正后方";
        }
    }
}
