using System.Text;
using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 序章左上角的进度板：三卷走过一卷就画一个勾。
    /// 场景里的文字是构建时写死的，可 PlayerPrefs 里的进度只有运行时才知道，
    /// 所以这块板子在 Start 里自己算一遍。
    /// </summary>
    public class HubBoard : MonoBehaviour
    {
        static readonly string[] VolumeNames = { "卷一 · 城与玉", "卷二 · 帝与陵", "卷三 · 港与海" };

        void Start()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;
            ui.SetObjectives(Board());

            // 三处都进去过，门就开了——这是整本书真正的「下一步」。
            // 不要求线索读完：门认的是「去过」，不是「读完」。
            if (GameState.AllChaptersVisited)
                ui.ShowToast("三处都去过了，藏卷阁的门开着。", 8f);
        }

        static string Board()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=#8C8579>河山册 · 三卷</color>");
            for (int i = 0; i < GameState.Chapters.Length && i < VolumeNames.Length; i++)
            {
                bool ok = GameState.IsChapterComplete(GameState.Chapters[i]);
                string mark = ok ? "<color=#D8B678>✓</color>" : "<color=#6E685E>○</color>";
                string name = ok
                    ? VolumeNames[i]
                    : "<color=#6E685E>" + VolumeNames[i] + "</color>";
                sb.AppendLine(mark + " " + name);
            }

            sb.AppendLine();
            sb.AppendLine("<color=#8C8579>眼下</color>");
            if (GameState.AllChaptersVisited)
            {
                sb.AppendLine("· 去山谷尽头，进藏卷阁");
            }
            else
            {
                sb.AppendLine("· 顺着山道走进山谷，到三岔口");
                sb.AppendLine("· 挑一条路走到头，站定了按 E");
                sb.AppendLine("· 亭子里那半卷残卷，可以翻一翻");
            }
            sb.AppendLine();
            sb.Append("<color=#8C8579>按 E 说话 / 进门　Tab 取册子 · 滚轮翻页　空格 确认</color>");
            return sb.ToString();
        }
    }
}
