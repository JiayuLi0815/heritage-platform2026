using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 路的尽头：一道会说话的界。
    /// 走进范围，底下浮一行字；按 E，画面淡黑，落到下一个场景的指定落点。
    /// 不做发光传送门——它只是一道门槛，过了就是另一处地方。
    /// </summary>
    public class ScenePortal : MonoBehaviour
    {
        [Tooltip("要去的场景名（Build Settings 里的名字）")]
        public string sceneToLoad;

        [Tooltip("到了那边站在哪个落点；留空则用那边的默认出生点")]
        public string spawnId;

        [Tooltip("走近时底下浮的一行字")]
        public string line = "按 E，往前走";

        [Tooltip("黑场上留的一行字，如「卷一 · 水之卷　良渚」；留空则什么都不写")]
        public string destinationLabel;

        [Tooltip("条件不够时的一行字")]
        public string lockedLine = "还不能过去";

        [Tooltip("勾上则三处遗址都进去过才放行（不要求线索全触发）")]
        public bool requireAllChapters;

        [Tooltip("勾上则不用按 E：走进 innerRadius 就过去。门里就是另一处地方时，不该再问一次")]
        public bool autoEnter;

        [Tooltip("自动过去的半径（米），要比 radius 小，人得真的跨过去")]
        public float innerRadius = 1.1f;

        [Tooltip("感应范围（米）")]
        public float radius = 3.1f;

        Transform player;
        BookHolder book;
        bool inside;

        /// <summary>人此刻站在哪道界门前。手柄扳机没有键盘事件可听，靠这个找门。</summary>
        static ScenePortal nearest;

        /// <summary>
        /// 手柄扳机落在界门附近时走这里。看不到互动点也照样能进门——
        /// 界门不是 Interactable，桌面端一直只听 E 键。
        /// </summary>
        public static bool TryActivate()
        {
            if (nearest == null) return false;
            nearest.Enter();
            return true;
        }

        void Start()
        {
            var pc = FindObjectOfType<DesktopPlayerController>();
            if (pc != null) player = pc.transform;
            book = FindObjectOfType<BookHolder>();
        }

        void Update()
        {
            if (player == null) return;
            var ui = UIManager.Instance;
            if (ui == null) return;

            Vector3 d = player.position - transform.position;
            d.y = 0f;
            bool near = d.magnitude <= radius && Mathf.Abs(player.position.y - transform.position.y) < 3.5f;

            if (!near)
            {
                if (nearest == this) nearest = null;
                if (inside)
                {
                    inside = false;
                    ui.ClearPrompt();
                }
                return;
            }

            nearest = this;
            bool open = !requireAllChapters || GameState.AllChaptersVisited;
            if (!inside)
            {
                inside = true;
                ui.ShowPrompt(open ? line : lockedLine, true);
            }

            if (ui.BlocksPlayer) return;
            // 抬着书时 E 是翻页键：不能一翻页就把人送走
            if (book != null && book.Raised) return;

            // 走进门里就算进去了：这扇门后面是另一处地方，不该再让人按一次 E
            if (autoEnter && open && d.magnitude <= innerRadius && !string.IsNullOrEmpty(sceneToLoad))
            {
                inside = false;
                ui.ClearPrompt();
                ui.LoadScene(sceneToLoad, spawnId, destinationLabel);
                return;
            }

            if (Input.GetKeyDown(KeyCode.E)) Enter();
        }

        /// <summary>过这道门：桌面端按 E，头显里按右手扳机。</summary>
        public void Enter()
        {
            var ui = UIManager.Instance;
            if (ui == null || ui.BlocksPlayer) return;
            if (player == null) return;

            Vector3 d = player.position - transform.position;
            d.y = 0f;
            if (d.magnitude > radius) return;
            // 抬着书时扳机是翻页键：不能一翻页就把人送走
            if (book != null && book.Raised) return;

            bool open = !requireAllChapters || GameState.AllChaptersVisited;
            if (!open)
            {
                int left = GameState.Chapters.Length - GameState.VisitedCount;
                ui.ShowModal("还差 " + left + " 处没去",
                    "藏卷阁 · 三处遗址都去过了才开门",
                    "这一阁收的是你一路看见的东西。\n\n" +
                    "现在还有 " + left + " 处遗址没进去过——不用把线索全看完，" +
                    "沿着山谷里那三条岔路各走一趟，回来就能进。\n\n" +
                    "（走过的路，路边都有界石，站在界石旁按 E 就回山谷。）",
                    "[空格] 知道了");
                return;
            }

            if (string.IsNullOrEmpty(sceneToLoad)) return;
            inside = false;
            ui.ClearPrompt();
            ui.LoadScene(sceneToLoad, spawnId, destinationLabel);
        }
    }
}
