using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Huaxia
{
    [Serializable]
    public class Objective
    {
        public string id;
        public string label;

        /// <summary>次线索：不做也读得完这一卷，做了会在书页边上留一条注。</summary>
        public bool optional;
    }

    /// <summary>章节目标追踪、文明档案解锁。</summary>
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance;

        public string chapterId = "liangzhu";
        public string chapterLabel = "第一章 · 文明初兴";
        public string chapterSubtitle = "良渚古城遗址";

        public Objective[] objectives;

        [Header("开场提示")]
        public string introBody;

        [Header("文明档案")]
        public string archiveTitle = "文明初兴：早期城市与社会组织";
        [TextArea(4, 14)] public string archiveBody;
        public string archiveFooter = "[空格] 合卷 · 回山谷";
        public string returnScene = "Archive";

        readonly HashSet<string> done = new HashSet<string>();
        bool unlocked;
        bool allSeenAnnounced;

        /// <summary>主线索（必做）：完成即本卷读完。</summary>
        public int PrimaryCount
        {
            get
            {
                if (objectives == null) return 0;
                int n = 0;
                for (int i = 0; i < objectives.Length; i++)
                    if (!objectives[i].optional) n++;
                return n;
            }
        }

        public int PrimaryDone
        {
            get
            {
                if (objectives == null) return 0;
                int n = 0;
                for (int i = 0; i < objectives.Length; i++)
                    if (!objectives[i].optional && done.Contains(objectives[i].id)) n++;
                return n;
            }
        }

        public bool IsPrimary(string id)
        {
            if (objectives == null || string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < objectives.Length; i++)
                if (objectives[i].id == id) return !objectives[i].optional;
            return false;
        }

        public bool IsKnown(string id)
        {
            if (objectives == null || string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < objectives.Length; i++)
                if (objectives[i].id == id) return true;
            return false;
        }

        void Awake()
        {
            Instance = this;

            // 遗址里随时能合卷回山：Q。读完三卷自动回，这是中途离开的出口。
            if (GetComponent<ReturnHome>() == null)
                gameObject.AddComponent<ReturnHome>();
        }

        void Start()
        {
            // 进场就算「去过」：终章的门只看三处去没去过，不要求把线索全触发
            GameState.VisitChapter(chapterId);

            if (UIManager.Instance != null)
                UIManager.Instance.SetChapterTitle(chapterLabel, chapterSubtitle);
            Refresh();
            if (!string.IsNullOrEmpty(introBody))
                StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            yield return new WaitForSeconds(0.8f);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowModal(
                    chapterLabel,
                    chapterSubtitle,
                    introBody,
                    "[空格] 开始探索　·　中途离开按 Q 合卷回山");
        }

        public void Complete(string id)
        {
            if (string.IsNullOrEmpty(id) || done.Contains(id)) return;

            // 次线索不算进度：它只让书的页边多一行字
            if (!IsPrimary(id))
            {
                if (!IsKnown(id)) return;
                done.Add(id);
                Refresh();
                RevealNote();
                AnnounceIfAllSeen();
                return;
            }

            done.Add(id);
            Refresh();
            AnnounceIfAllSeen();

            if (PrimaryDone >= PrimaryCount && !unlocked)
            {
                unlocked = true;
                StartCoroutine(UnlockRoutine());
            }
        }

        /// <summary>次线索看完：书页边上多一条注，抬手看一眼就落回去。</summary>
        void RevealNote()
        {
            var holder = FindObjectOfType<BookHolder>();
            if (holder == null || holder.Book == null) return;
            holder.Book.AddNote(chapterId);
            holder.Peek(2.8f);
        }

        IEnumerator UnlockRoutine()
        {
            yield return new WaitForSeconds(1.1f);
            GameState.CompleteChapter(chapterId);

            // 这一卷读完：书自己抬起来，让体验者看着纸从湿到干、字浮出来，然后才回到档案
            var holder = FindObjectOfType<BookHolder>();
            if (holder != null && holder.Book != null)
            {
                float hold = holder.Book.dryingSeconds + 1.2f;
                holder.Present(chapterId, hold);
                yield return new WaitForSeconds(hold + 0.75f);
            }

            if (UIManager.Instance != null)
                UIManager.Instance.ShowModal(
                    "文明档案 · " + archiveTitle,
                    chapterLabel + "　" + chapterSubtitle,
                    archiveBody,
                    archiveFooter,
                    delegate
                    {
                        // 从哪条岔路来的，就回那条岔路的尽头站着
                        UIManager.Instance.LoadScene(returnScene, "back_" + chapterId);
                    });
        }

        /// <summary>这一卷的装置全部看过：不用再回头找了，主动说一声可以往下走。</summary>
        void AnnounceIfAllSeen()
        {
            if (allSeenAnnounced || objectives == null) return;
            for (int i = 0; i < objectives.Length; i++)
                if (!done.Contains(objectives[i].id)) return;
            allSeenAnnounced = true;
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("这一卷看完了　·　合卷回山谷，往下一卷去", 7f);
        }

        /// <summary>
        /// 左上角的线索表：每开一个装置就画一个勾。
        /// 以前用◆／◇，看着更像排版符号而不是「做完了没有」。
        /// </summary>
        void Refresh()
        {
            if (UIManager.Instance == null) return;

            int allDone = 0;
            for (int i = 0; i < objectives.Length; i++)
                if (done.Contains(objectives[i].id)) allDone++;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#8C8579>本卷线索　" + allDone + " / " + objectives.Length + "</color>");

            bool optionalHeader = false;
            for (int i = 0; i < objectives.Length; i++)
            {
                var o = objectives[i];
                bool ok = done.Contains(o.id);
                if (o.optional && !optionalHeader)
                {
                    optionalHeader = true;
                    sb.AppendLine("<color=#8C8579>其余可看可不看</color>");
                }
                string mark = ok ? "<color=#D8B678>✓</color>" : "<color=#6E685E>○</color>";
                string label = o.optional
                    ? "<color=" + (ok ? "#A79F8F" : "#6E685E") + ">" + o.label + "</color>"
                    : o.label;
                sb.AppendLine(mark + " " + label);
            }
            UIManager.Instance.SetObjectives(sb.ToString());
        }
    }
}
