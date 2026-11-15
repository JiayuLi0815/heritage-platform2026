using System.Collections;
using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 进场说明。挂在场景里，Start 后过 delay 秒自动弹一次模态。
    /// 序章用它交代「为什么是这三处」；遗址用它补一句「这一处看什么」。
    /// </summary>
    public class SceneIntro : MonoBehaviour
    {
        public string title = "河山册";
        public string subtitle = "";
        [TextArea(4, 16)] public string body = "";
        public string footer = "[空格] 起身";
        public float delay = 1.0f;
        [Tooltip("只在还没进过任何遗址时弹（序章的开场说明用）")]
        public bool onlyIfNoVisits;

        IEnumerator Start()
        {
            if (string.IsNullOrEmpty(body)) yield break;
            if (onlyIfNoVisits && GameState.VisitedCount > 0) yield break;
            yield return new WaitForSeconds(delay);

            // 进场时如果已经有别的弹窗（比如从过场回来），不要抢
            var ui = UIManager.Instance;
            if (ui == null || ui.ModalOpen) yield break;
            ui.ShowModal(title, subtitle, body, footer);
        }
    }
}
