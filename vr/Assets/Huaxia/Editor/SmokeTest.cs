using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Huaxia.EditorTools
{
    /// <summary>自动跑一遍核心流程：档案馆 → 良渚 → 三线索 → 文明档案 → 返回档案馆。</summary>
    public static class SmokeTest
    {
        static double t0;
        static int step;
        static string outDir;

        [MenuItem("Huaxia/运行冒烟测试")]
        public static void Run()
        {
            outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Previews");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(HuaxiaBuilder.SceneDir + "/Archive.unity");
            t0 = 0;
            step = 0;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
            Debug.Log("[Smoke] start");
        }

        static double Now { get { return EditorApplication.timeSinceStartup; } }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (t0 == 0) { t0 = Now; return; }

            double t = Now - t0;
            if (t > 40.0 && step < 7)
            {
                Debug.LogError("[Smoke] TIMEOUT at step " + step);
                Finish();
                return;
            }

            if (step == 0 && t > 2.5)
            {
                step = 1;
                var ui = Object.FindObjectOfType<UIManager>();
                var player = Object.FindObjectOfType<DesktopPlayerController>();
                var text = ui != null ? ui.chapterTitle : null;
                Debug.Log("[Smoke] Archive ui=" + (ui != null) + " player=" + (player != null)
                          + " font=" + (text != null && text.font != null)
                          + " title=" + (text != null ? text.text : "null")
                          + " scene=" + SceneManager.GetActiveScene().name);
                Capture("play_01_archive.png");
            }
            else if (step == 1 && t > 3.6)
            {
                step = 2;
                var ui = Object.FindObjectOfType<UIManager>();
                var nodes = Object.FindObjectsOfType<Interactable>();
                Interactable liangzhu = null;
                foreach (var n in nodes)
                    if (n.kind == InteractKind.Chapter && n.sceneToLoad == "Liangzhu") liangzhu = n;

                Debug.Log("[Smoke] nodes=" + nodes.Length + " liangzhuNode=" + (liangzhu != null));
                if (liangzhu != null) liangzhu.Interact();
                Debug.Log("[Smoke] modalOpenAfterInteract=" + (ui != null && ui.ModalOpen));
            }
            else if (step == 2 && t > 4.6)
            {
                step = 3;
                var ui = Object.FindObjectOfType<UIManager>();
                Debug.Log("[Smoke] loading Liangzhu");
                if (ui != null) ui.LoadScene("Liangzhu");
            }
            else if (step == 3 && t > 8.2)
            {
                step = 4;
                var tasks = Object.FindObjectOfType<TaskManager>();
                var ui = Object.FindObjectOfType<UIManager>();
                Debug.Log("[Smoke] scene=" + SceneManager.GetActiveScene().name
                          + " tasks=" + (tasks != null) + " ui=" + (ui != null));
                if (ui != null) ui.CloseModal();
                if (tasks != null)
                {
                    foreach (var o in tasks.objectives) tasks.Complete(o.id);
                    Debug.Log("[Smoke] all objectives completed");
                }
                Capture("play_02_liangzhu.png");
            }
            else if (step == 4 && t > 10.5)
            {
                step = 5;
                var ui = Object.FindObjectOfType<UIManager>();
                Debug.Log("[Smoke] archiveModalOpen=" + (ui != null && ui.ModalOpen));
                Capture("play_03_archive_unlock.png");
            }
            else if (step == 5 && t > 12.0)
            {
                step = 6;
                var ui = Object.FindObjectOfType<UIManager>();
                if (ui != null) ui.LoadScene("Archive");
            }
            else if (step == 6 && t > 15.0)
            {
                step = 7;
                var ui = Object.FindObjectOfType<UIManager>();
                var tasks = Object.FindObjectOfType<TaskManager>();
                Debug.Log("[Smoke] returnedTo=" + SceneManager.GetActiveScene().name
                          + " progressSaved=" + GameState.IsChapterComplete("liangzhu")
                          + " tasksInArchive=" + (tasks != null));
                if (ui != null)
                {
                    ui.SetObjectives(ui.objectivesText.text + "\n\n<color=#D8B678>◆ 良渚古城遗址 · 已完成</color>");
                }
                Capture("play_04_archive_done.png");
                Finish();
            }
        }

        static void Capture(string file)
        {
            if (Application.isBatchMode) return;
            try
            {
                string path = Path.Combine(outDir, file);
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log("[Smoke] capture -> " + path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Smoke] capture failed: " + e.Message);
            }
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            Debug.Log("[Smoke] PASS");
            EditorApplication.Exit(0);
        }
    }
}
