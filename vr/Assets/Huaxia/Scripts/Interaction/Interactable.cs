using UnityEngine;

namespace Huaxia
{
    public enum InteractKind
    {
        Info,
        Chapter
    }

    /// <summary>史实分层（GAME_DESIGN §9.1）：每块考据牌都必须标出这条信息有多硬。</summary>
    public enum SourceTier
    {
        /// <summary>有考古报告 / 权威出版物支撑。牌面不加记号。</summary>
        Confirmed,
        /// <summary>主流观点但仍有争议。牌面加「○」+ 一句争议说明。</summary>
        Inferred,
        /// <summary>无可靠来源，为表达需要所做。牌面加「△」+ 明示是艺术处理。</summary>
        Unverified
    }

    /// <summary>可聚焦、可按 E 触发的信息点或章节节点。</summary>
    public class Interactable : MonoBehaviour
    {
        public InteractKind kind = InteractKind.Info;
        public string prompt = "[E] 查看";
        public string title;
        public string subtitle;
        [TextArea(3, 12)] public string body;
        public string objectiveId;
        public SourceTier tier = SourceTier.Confirmed;
        [Tooltip("学界推断写一句争议在哪；待考证写哪部分是艺术处理")]
        public string tierNote;
        [Tooltip("这一条不是考据，别加「已证实」的层级行（如终章合书）")]
        public bool hideTier;

        [Header("遗产立牌")]
        [Tooltip("填 UNESCO 编号就打开遗产面板：1592 良渚 / 441 秦陵 / 1561 泉州；点位则填 spot_lz_water 这类编号")]
        public string heritageId;
        [Tooltip("这是序章里的遗产立牌（不是现场点位）：合上面板后弘文转身朝那条岔路讲路怎么走")]
        public bool board;
        [Tooltip("点位读完之后，弘文朝哪一处抬手示意「往前走」——填下一个点位的 transform")]
        public Transform leadTo;

        [Header("体验终点")]
        [Tooltip("填了场景名：按 E 之后淡黑、留一句末语，再回到该场景的这个出生点")]
        public string endScene;
        public string endSpawnId;
        [TextArea(2, 4)] public string endLine;
        public string sceneToLoad;
        public Renderer markerRenderer;
        public Color markerColor = new Color(0.95f, 0.78f, 0.45f);

        bool consumed;

        public string Prompt { get { return VRKeyLabels.Fix(consumed ? "[E] 回顾" : prompt); } }

        /// <summary>考据牌首行：层级 + 争议/艺术处理的说明。</summary>
        public string TierLine()
        {
            string head;
            switch (tier)
            {
                case SourceTier.Inferred: head = "○ 学界推断"; break;
                case SourceTier.Unverified: head = "△ 待考证 · 此为艺术处理"; break;
                default: head = "● 已证实"; break;
            }
            // 已证实也可以带注（如「水门闸形制为示意」），注不因层级而丢
            return string.IsNullOrEmpty(tierNote) ? head : head + "　" + tierNote;
        }
        public bool Consumed { get { return consumed; } }

        public void Interact()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;

            // 遗产立牌：面板自己管一整屏，优先级高于普通弹窗
            if (!string.IsNullOrEmpty(heritageId))
            {
                if (HeritagePanel.Instance != null) HeritagePanel.Instance.Open(heritageId, leadTo, board);
                else ui.ShowModal(title, subtitle, body, "[空格] 关闭");
                MarkConsumed();
                if (!string.IsNullOrEmpty(objectiveId) && TaskManager.Instance != null)
                    TaskManager.Instance.Complete(objectiveId);
                return;
            }

            if (kind == InteractKind.Chapter)
            {
                if (string.IsNullOrEmpty(sceneToLoad))
                    ui.ShowModal(title, subtitle, body, "[空格] 关闭");
                else
                    ui.ShowModal(title, subtitle, body, "[空格] 进入现场", delegate { ui.LoadScene(sceneToLoad); });
                return;
            }

            if (!string.IsNullOrEmpty(endScene))
            {
                ui.ShowModal(title, subtitle, body, "[空格] 合上", delegate
                {
                    ui.EndExperience(endScene, endSpawnId, endLine);
                });
                MarkConsumed();
                return;
            }

            // 层级写在正文首行：先交代这条信息有多硬，再谈看见了什么
            ui.ShowModal(title, subtitle, hideTier ? body : TierLine() + "\n\n" + body, "[空格] 关闭");
            if (!string.IsNullOrEmpty(objectiveId) && TaskManager.Instance != null)
                TaskManager.Instance.Complete(objectiveId);
            MarkConsumed();
        }

        public void MarkConsumed()
        {
            if (consumed) return;
            consumed = true;
            if (markerRenderer != null)
            {
                var m = markerRenderer.material;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", markerColor * 0.12f);
            }
        }
    }
}
