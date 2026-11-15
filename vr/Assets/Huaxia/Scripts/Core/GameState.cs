using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 跨场景保存的探索进度。
    /// v6 只有三处遗址：良渚（水）、秦陵（土）、泉州（海）。
    /// 进度不显示成数字——它只让书变干、让墨显出来。
    /// </summary>
    public static class GameState
    {
        const string Prefix = "huaxia_chapter_";
        const string NotePrefix = "huaxia_note_";
        const string VisitedPrefix = "huaxia_visited_";

        /// <summary>三卷的顺序即书的页序。</summary>
        // 必须与各章 TaskManager.chapterId 逐字一致（卷二在场景里写的是 "qinling"），
        // 否则该章的书页永远不标记完成、页边注也落不下去
        public static readonly string[] Chapters = { "liangzhu", "qinling", "quanzhou" };

        public static bool IsChapterComplete(string id)
        {
            return PlayerPrefs.GetInt(Prefix + id, 0) == 1;
        }

        public static void CompleteChapter(string id)
        {
            PlayerPrefs.SetInt(Prefix + id, 1);
            PlayerPrefs.Save();
        }

        public static bool HasNote(string id)
        {
            return PlayerPrefs.GetInt(NotePrefix + id, 0) == 1;
        }

        /// <summary>页边注：次装置与静观留下的字。</summary>
        public static void SetNote(string id)
        {
            PlayerPrefs.SetInt(NotePrefix + id, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 进去过就算数：不要求把这一卷的线索全部触发。
        /// 旧存档没有 visited 键，但若那一卷已读完，同样算去过——
        /// 否则升级后门会突然倒退回「还差三处」。
        /// </summary>
        public static bool IsChapterVisited(string id)
        {
            return PlayerPrefs.GetInt(VisitedPrefix + id, 0) == 1 || IsChapterComplete(id);
        }

        public static void VisitChapter(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (IsChapterVisited(id)) return;
            PlayerPrefs.SetInt(VisitedPrefix + id, 1);
            PlayerPrefs.Save();
        }

        public static bool AllChaptersVisited
        {
            get
            {
                for (int i = 0; i < Chapters.Length; i++)
                    if (!IsChapterVisited(Chapters[i])) return false;
                return true;
            }
        }

        public static int VisitedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Chapters.Length; i++)
                    if (IsChapterVisited(Chapters[i])) n++;
                return n;
            }
        }

        public static bool AllChaptersComplete
        {
            get
            {
                for (int i = 0; i < Chapters.Length; i++)
                    if (!IsChapterComplete(Chapters[i])) return false;
                return true;
            }
        }

        public static int CompletedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Chapters.Length; i++)
                    if (IsChapterComplete(Chapters[i])) n++;
                return n;
            }
        }

        /// <summary>
        /// 下一个场景的落点 id。换场景之前写好，新场景的玩家读完就清空。
        /// 不落盘——它只管一次转身。
        /// </summary>
        public static string pendingSpawn;

        public static void ResetAll()
        {
            for (int i = 0; i < Chapters.Length; i++)
            {
                PlayerPrefs.DeleteKey(Prefix + Chapters[i]);
                PlayerPrefs.DeleteKey(NotePrefix + Chapters[i]);
                PlayerPrefs.DeleteKey(VisitedPrefix + Chapters[i]);
            }
            PlayerPrefs.Save();
            pendingSpawn = null;
        }
    }
}
