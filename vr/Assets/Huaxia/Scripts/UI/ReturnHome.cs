using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 在遗址里随时合卷回山（Q）。
    /// 三条主线索读满会自动合卷回去；没读完想提前走，不必再走几十米回入口的界石——
    /// 书就在手里，合卷就是归途。进度存在 PlayerPrefs 里，下次进来接着看。
    /// </summary>
    public class ReturnHome : MonoBehaviour
    {
        public KeyCode key = KeyCode.Q;

        void Update()
        {
            if (!Input.GetKeyDown(key)) return;
            Ask();
        }

        /// <summary>弹出「合卷回山」确认框。键位之外也留一个入口，方便走查脚本与将来做按钮。</summary>
        public void Ask()
        {
            var ui = UIManager.Instance;
            var tasks = TaskManager.Instance;
            if (ui == null || tasks == null) return;
            if (ui.BlocksPlayer) return;

            string where = string.IsNullOrEmpty(tasks.chapterSubtitle)
                ? tasks.chapterLabel
                : tasks.chapterLabel + "　" + tasks.chapterSubtitle;

            ui.ShowModal(
                "合卷回山",
                where,
                "已经记下的会留在《河山册》里，下次再来接着看。\n\n" +
                "现在回山谷吗？",
                "[空格] 回山谷　·　[Esc] 再走走",
                // 回的是三岔口正中那方圆坛，站定了正对三条路——不是退回某一条路的尽头
                delegate { ui.LoadScene("Archive", "hub_center"); });
        }
    }
}
