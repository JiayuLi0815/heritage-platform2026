using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 地上的引导圈。每个触发点只摆一只：序章开场在步道中轴上一只，
    /// 三块遗产立牌前各一只。玩家走进去，弘文才朝他走过来、招手、
    /// 开口讲这一处 —— 一进场景是不说话的；讲完她往前带几步，指着下一处。
    ///
    /// 圈本身只有视觉与判定：一圈柔光慢慢呼吸，黄昏的广场上远远就看得见地上有圈。
    /// </summary>
    public class GuideRing : MonoBehaviour
    {
        public string heritageId = "intro";

        [Tooltip("玩家进到这么近就算踩进圈里")]
        public float radius = 1.15f;

        [Tooltip("圈后面那块牌，用来算她该站哪一侧")]
        public Transform board;

        [Tooltip("她停在圈外多远：不站进圈里跟玩家抢位置，也不挡牌面")]
        public float standGap = 1.25f;

        [Tooltip("踩进来时她原地不动（只转身开口）。序章开场她本来就站在牌前")]
        public bool stayInPlace;

        Transform player;
        Vector3 baseScale = Vector3.one;
        bool inside;
        float phase;

        void Awake()
        {
            baseScale = transform.localScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            phase = Mathf.Repeat(phase + dt * 0.5f, 1f);

            // 呼吸：圈自己大一点小一点，黄昏的广场上远远就看得见地上有东西
            float breath = 1f + 0.05f * Mathf.Sin(phase * Mathf.PI * 2f);
            transform.localScale = new Vector3(baseScale.x * breath, baseScale.y, baseScale.z * breath);

            if (player == null) player = Player();
            if (player == null) return;

            // 开场说明、立牌面板开着的时候先别触发：那时玩家的注意力不在地上。
            // 状态照旧保留 —— 关掉面板后人还站在圈里，不该再触发一遍。
            if (Busy()) return;

            Vector3 to = player.position - transform.position;
            to.y = 0f;
            float flat = to.magnitude;

            if (!inside && flat <= radius)
            {
                // 她正在带路时接不了这一处：这时别把「踩过」记下来，
                // 否则玩家走到立牌前站定，反倒没人招呼了 —— 下一帧接着问。
                if (DigitalHuman.Instance != null &&
                    DigitalHuman.Instance.Encounter(heritageId, StandPoint(), board, stayInPlace))
                    inside = true;
            }
            else if (inside && flat > radius + 0.4f)
            {
                // 走出去一段才重新武装，站在边缘晃不会连着播
                inside = false;
            }
        }

        /// <summary>她该站的位置：圈的靠玩家这一侧，也就是牌面的反方向。</summary>
        public Vector3 StandPoint()
        {
            Vector3 here = transform.position;
            Vector3 away = here - (board != null ? board.position : here - Vector3.forward * 3f);
            away.y = 0f;
            if (away.sqrMagnitude < 0.04f) away = Vector3.forward;
            away.Normalize();
            Vector3 p = here + away * standGap;
            p.y = here.y;
            return p;
        }

        static bool Busy()
        {
            var ui = UIManager.Instance;
            if (ui != null && ui.ModalOpen) return true;
            var panel = HeritagePanel.Instance;
            return panel != null && panel.IsOpen;
        }

        static Transform Player()
        {
            var pc = FindObjectOfType<DesktopPlayerController>();
            if (pc != null) return pc.transform;
            var cam = Camera.main;
            return cam != null ? cam.transform : null;
        }
    }
}
