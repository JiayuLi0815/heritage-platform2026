using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 一叶书页。《河山册》的每一叶都是一个真实物体：
    /// 湿度决定纸色与水渍，显墨决定上一个守册人的字露出来多少。
    /// 三层墨分开显影：卷名地名 / 页面字与手记 / 页边注。
    /// 运行时它会给自己复制一份材质，避免多叶共用同一组参数。
    /// </summary>
    public class BookLeaf : MonoBehaviour
    {
        [Tooltip("这一叶的纸面渲染器")]
        public Renderer face;

        [Tooltip("这一叶的背面：翻过去看到的那一面，同一种纸、没有字")]
        public Renderer back;

        [Tooltip("翻页枢轴（在书脊上），绕本地 Z 轴转 180° 就是翻过去")]
        public Transform pivot;

        [Range(0f, 1f)] public float wetness = 1f;

        [Tooltip("卷名与地名：还没做到这一章也认得出是哪一卷")]
        [Range(0f, 1f)] public float ink = 1f;
        [Tooltip("页面字与主手记：纸干了才显")]
        [Range(0f, 1f)] public float note;
        [Tooltip("页边注：次装置与静观留下的字，比手记更淡")]
        [Range(0f, 1f)] public float foot;

        [Tooltip("这一叶是否已经翻到左边")]
        public bool turned;

        [Tooltip("这一叶是否已经可以读（前一叶没干则锁着）")]
        public bool unlocked;

        [Tooltip("这一叶对应的章节 id；末页留空")]
        public string chapterId;

        [HideInInspector] public Vector3 pivotBase;

        Material mat;
        Material backMat;
        float wetTarget = 1f;
        float inkTarget;
        float noteTarget;
        float footTarget;
        float wetSpeed = 1f;
        float inkSpeed = 1f;
        float noteSpeed = 1f;
        float footSpeed = 1f;

        static readonly int WetId = Shader.PropertyToID("_Wetness");
        static readonly int InkId = Shader.PropertyToID("_Ink");
        static readonly int NoteId = Shader.PropertyToID("_Note");
        static readonly int FootId = Shader.PropertyToID("_Foot");
        static readonly int StainId = Shader.PropertyToID("_StainStrength");

        public bool Dried { get { return wetness <= 0.02f; } }

        void Awake()
        {
            if (pivot != null) pivotBase = pivot.localPosition;
            if (face != null && face.sharedMaterial != null)
            {
                mat = new Material(face.sharedMaterial);
                face.material = mat;
            }
            if (back != null && back.sharedMaterial != null)
            {
                backMat = new Material(back.sharedMaterial);
                back.material = backMat;
            }
            wetTarget = wetness;
            inkTarget = ink;
            noteTarget = note;
            footTarget = foot;
            Apply();
        }

        void Update()
        {
            if (Mathf.Abs(wetness - wetTarget) > 0.0005f)
                wetness = Mathf.MoveTowards(wetness, wetTarget, wetSpeed * Time.deltaTime);
            if (Mathf.Abs(ink - inkTarget) > 0.0005f)
                ink = Mathf.MoveTowards(ink, inkTarget, inkSpeed * Time.deltaTime);
            if (Mathf.Abs(note - noteTarget) > 0.0005f)
                note = Mathf.MoveTowards(note, noteTarget, noteSpeed * Time.deltaTime);
            if (Mathf.Abs(foot - footTarget) > 0.0005f)
                foot = Mathf.MoveTowards(foot, footTarget, footSpeed * Time.deltaTime);
            Apply();
        }

        /// <summary>纸干：水渍退、纸色回暖。seconds <= 0 表示立刻生效。</summary>
        public void Dry(float seconds)
        {
            wetTarget = 0f;
            wetSpeed = seconds > 0.01f ? 1f / seconds : 1000f;
            if (seconds <= 0.01f) { wetness = 0f; Apply(); }
        }

        /// <summary>页面字与主手记浮出来。</summary>
        public void RevealNote(float seconds, float target = 1f)
        {
            noteTarget = Mathf.Clamp01(target);
            noteSpeed = seconds > 0.01f ? 1f / seconds : 1000f;
            if (seconds <= 0.01f) { note = noteTarget; Apply(); }
        }

        /// <summary>页边注：比手记更淡，出现得更慢。</summary>
        public void RevealFoot(float seconds, float target = 1f)
        {
            footTarget = Mathf.Clamp01(target);
            footSpeed = seconds > 0.01f ? 1f / seconds : 1000f;
            if (seconds <= 0.01f) { foot = footTarget; Apply(); }
        }

        public void SetWetImmediate(float v) { wetness = wetTarget = Mathf.Clamp01(v); Apply(); }
        public void SetInkImmediate(float v) { ink = inkTarget = Mathf.Clamp01(v); Apply(); }
        public void SetNoteImmediate(float v) { note = noteTarget = Mathf.Clamp01(v); Apply(); }
        public void SetFootImmediate(float v) { foot = footTarget = Mathf.Clamp01(v); Apply(); }

        /// <summary>写进材质；没有独立材质时退回改共用材质（Editor 建 Scene 时用）。</summary>
        public void Apply()
        {
            var m = mat != null ? mat : (face != null ? face.sharedMaterial : null);
            if (m == null) return;
            m.SetFloat(WetId, wetness);
            m.SetFloat(InkId, ink);
            m.SetFloat(NoteId, note);
            m.SetFloat(FootId, foot);
            m.SetFloat(StainId, Mathf.Lerp(0.35f, 1f, wetness));

            var bm = backMat != null ? backMat : (back != null ? back.sharedMaterial : null);
            if (bm != null)
            {
                bm.SetFloat(WetId, wetness);
                bm.SetFloat(StainId, Mathf.Lerp(0.30f, 0.85f, wetness));
            }
        }
    }
}
