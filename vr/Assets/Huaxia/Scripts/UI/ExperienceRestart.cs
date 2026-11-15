using UnityEngine;
using UnityEngine.SceneManagement;

namespace Huaxia
{
    /// <summary>
    /// 两个「回到开头」：本章重来、整段重来。
    /// 头显里要能随时退回去讲一遍，桌面端也留同一套入口。
    /// </summary>
    public static class ExperienceRestart
    {
        /// <summary>回到这一章的开头：重载本场景，落在场景自己的默认落点。已记下的进度不丢。</summary>
        public static void RestartChapter()
        {
            var ui = UIManager.Instance;
            if (ui == null || ui.BlocksPlayer) return;

            string scene = SceneManager.GetActiveScene().name;
            ui.ShowModal(
                "本章重来",
                "回到这一章开头",
                "这一章会从头再走一遍，已经记下的东西不会丢。\n\n现在就重来吗？",
                "[空格] 重来　·　[Esc] 再走走",
                delegate { ui.LoadScene(scene, null); });
        }

        /// <summary>回到最开始：清空三卷进度，回序章山门外。</summary>
        public static void RestartAll()
        {
            var ui = UIManager.Instance;
            if (ui == null || ui.BlocksPlayer) return;

            ui.ShowModal(
                "从头开始",
                "《河山册》重新摊开",
                "三卷的进度都会清空，从序章的山门外重新走。\n\n确定从头开始吗？",
                "[空格] 从头开始　·　[Esc] 再走走",
                delegate
                {
                    GameState.ResetAll();
                    ui.LoadScene("Archive", "start", "序章 · 山门");
                });
        }
    }
}
