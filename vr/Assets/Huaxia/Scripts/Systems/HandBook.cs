using System.Collections;
using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 《河山册》本体 —— 这个作品唯一的 UI。
    /// 没有数字、没有进度条、没有任务列表：进度是纸的湿度、是墨的浓淡、是能不能翻到下一叶。
    /// 页序：卷一良渚（水）· 卷二秦陵（土）· 卷三泉州（海）· 末页。
    /// </summary>
    public class HandBook : MonoBehaviour
    {
        [Header("结构")]
        public BookLeaf[] leaves;
        public Transform cover;

        [Header("翻页堆叠")]
        public float turnStackBase = 0.0083f;
        public float turnStackStep = 0.0020f;

        [Header("时间")]
        public float turnSeconds = 1.05f;
        public float dryingSeconds = 6f;
        public float inkSeconds = 3.5f;
        public float footSeconds = 5f;

        [Header("状态")]
        public int current;
        public bool coverOpen = true;

        [Header("声音")]
        [Tooltip("Resources 下的路径，不带扩展名")]
        // 翻页声已移除：原来那段合成纸声被听成"怪动静"。
        // 留空就静音；以后换成真实录音时把 Resources 路径填回来即可。
        public string pageTurnResource = "";
        [Range(0f, 1f)] public float pageTurnVolume = 0.55f;

        Coroutine turning;
        Coroutine covering;
        AudioSource pageSource;
        AudioClip pageClip;

        public BookLeaf CurrentLeaf
        {
            get { return leaves != null && current >= 0 && current < leaves.Length ? leaves[current] : null; }
        }

        void Awake()
        {
            ApplyCover(coverOpen);
            pageClip = Resources.Load<AudioClip>(pageTurnResource);
            if (pageClip != null)
            {
                pageSource = gameObject.AddComponent<AudioSource>();
                pageSource.playOnAwake = false;
                pageSource.spatialBlend = 0f;   // 书贴在耳边，不需要空间衰减
                pageSource.volume = pageTurnVolume;
            }
        }

        void Start()
        {
            RestoreFromProgress();
        }

        // ------------------------------------------------------------ 进度

        /// <summary>按已完成的章节复原书的物理状态（重进场景时书不能变回湿的）。</summary>
        public void RestoreFromProgress()
        {
            if (leaves == null) return;
            bool all = GameState.AllChaptersComplete;
            for (int i = 0; i < leaves.Length; i++)
            {
                var leaf = leaves[i];
                if (leaf == null) continue;
                string id = leaf.chapterId;
                bool isChapter = !string.IsNullOrEmpty(id);
                bool done = isChapter && GameState.IsChapterComplete(id);

                leaf.unlocked = i == 0 || (i > 0 && leaves[i - 1] != null && IsRead(leaves[i - 1]));

                if (!isChapter)
                {
                    // 末页：三卷都读完纸才不再是湿的
                    leaf.SetWetImmediate(all ? 0f : 1f);
                    leaf.SetInkImmediate(all ? 1f : 0f);
                    leaf.SetNoteImmediate(all ? 1f : 0f);
                    leaf.SetFootImmediate(0f);
                    continue;
                }

                if (done)
                {
                    leaf.SetWetImmediate(0f);
                    leaf.SetNoteImmediate(1f);
                }
                else
                {
                    leaf.SetWetImmediate(1f);
                    leaf.SetNoteImmediate(0f);
                }
                // 卷名与地名一直在：湿纸上也认得出这是哪一卷
                leaf.SetInkImmediate(1f);
                leaf.SetFootImmediate(GameState.HasNote(id) ? 1f : 0f);
            }
            Restack();
        }

        /// <summary>翻过去的叶挂在枢轴下方：枢轴要按「已经翻过几叶」抬起来，左页才不会互相扎穿。</summary>
        float StackY(int index)
        {
            int stack = 0;
            if (leaves != null)
                for (int i = 0; i < index && i < leaves.Length; i++)
                    if (leaves[i] != null && leaves[i].turned) stack++;
            return turnStackBase + stack * turnStackStep;
        }

        public void Restack()
        {
            if (leaves == null) return;
            int stack = 0;
            for (int i = 0; i < leaves.Length; i++)
            {
                var l = leaves[i];
                if (l == null || l.pivot == null) continue;
                if (l.turned)
                {
                    l.pivot.localPosition = l.pivotBase + new Vector3(0f, turnStackBase + stack * turnStackStep, 0f);
                    stack++;
                }
                else
                    l.pivot.localPosition = l.pivotBase;
            }
        }

        static bool IsRead(BookLeaf leaf)
        {
            return string.IsNullOrEmpty(leaf.chapterId) || GameState.IsChapterComplete(leaf.chapterId);
        }

        /// <summary>某一卷读完：这一叶干了、页面字与手记浮出来、下一页露出来。</summary>
        public void FinishChapter(string chapterId)
        {
            int index = IndexOf(chapterId);
            if (index < 0) return;
            var leaf = leaves[index];
            leaf.Dry(dryingSeconds);
            leaf.RevealNote(inkSeconds);
            if (index + 1 < leaves.Length)
            {
                var next = leaves[index + 1];
                next.unlocked = true;
                // 末页要等三卷都干
                if (string.IsNullOrEmpty(next.chapterId) && !GameState.AllChaptersComplete)
                    next.unlocked = false;
            }
        }

        /// <summary>页边注：不做也能读完故事，做了书更厚。出现得比手记更慢、更淡。</summary>
        public void AddNote(string chapterId)
        {
            int index = IndexOf(chapterId);
            if (index < 0) return;
            var leaf = leaves[index];
            leaf.RevealFoot(footSeconds, 1f);
            GameState.SetNote(chapterId);
        }

        int IndexOf(string chapterId)
        {
            if (leaves == null) return -1;
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i] != null && leaves[i].chapterId == chapterId) return i;
            return -1;
        }

        // ------------------------------------------------------------ 翻页

        public bool CanTurnNext
        {
            get
            {
                if (leaves == null || turning != null) return false;
                if (current >= leaves.Length - 1) return false;
                return leaves[current + 1] != null && leaves[current + 1].unlocked;
            }
        }

        public bool CanTurnPrev
        {
            get { return leaves != null && turning == null && current > 0; }
        }

        public void TurnNext()
        {
            if (!CanTurnNext) return;
            PlayPageTurn();
            turning = StartCoroutine(TurnRoutine(leaves[current], current + 1));
        }

        public void TurnPrev()
        {
            if (!CanTurnPrev) return;
            PlayPageTurn();
            turning = StartCoroutine(TurnRoutine(leaves[current - 1], current - 1));
        }

        /// <summary>纸的声音：音高每次略有不同，连着翻也不会像同一段录音在循环。</summary>
        void PlayPageTurn()
        {
            if (pageSource == null || pageClip == null) return;
            pageSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            pageSource.PlayOneShot(pageClip, 1f);
        }

        IEnumerator TurnRoutine(BookLeaf leaf, int targetIndex)
        {
            bool forward = targetIndex > current;
            float from = forward ? 0f : 180f;
            float to = forward ? 180f : 0f;
            int index = System.Array.IndexOf(leaves, leaf);
            float y0 = leaf.pivot != null ? leaf.pivot.localPosition.y : 0f;
            float y1 = forward ? StackY(index) : leaf.pivotBase.y;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.05f, turnSeconds);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                if (leaf.pivot != null)
                {
                    // 中途抬起来一点，纸才不会穿过底下那一叶
                    float lift = Mathf.Sin(e * Mathf.PI) * 0.026f;
                    leaf.pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, e));
                    leaf.pivot.localPosition = new Vector3(leaf.pivotBase.x,
                        Mathf.Lerp(y0, y1, e) + lift, leaf.pivotBase.z);
                }
                yield return null;
            }

            leaf.turned = forward;
            Restack();
            current = targetIndex;
            turning = null;
        }

        // ------------------------------------------------------------ 封面

        public void SetCoverOpen(bool open, bool instant = false)
        {
            coverOpen = open;
            if (instant || cover == null) { ApplyCover(open); return; }
            if (covering != null) StopCoroutine(covering);
            covering = StartCoroutine(CoverRoutine(open));
        }

        void ApplyCover(bool open)
        {
            if (cover == null) return;
            cover.localRotation = Quaternion.Euler(0f, 0f, open ? -180f : 0f);
        }

        IEnumerator CoverRoutine(bool open)
        {
            float from = cover.localEulerAngles.z;
            if (from > 180f) from -= 360f;
            float to = open ? -180f : 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.1f;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                cover.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, e));
                yield return null;
            }
            covering = null;
        }
    }
}
