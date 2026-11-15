using UnityEngine;
using UnityEngine.UI;

namespace Huaxia
{
    /// <summary>
    /// 图集翻页动画。素材是遗产站的 36 帧序列（human-full.webp，6×6 拼成一张图集），
    /// 网页里靠 canvas 换帧，这里靠改 UV——世界里的面片和 UI 里的头像共用同一张图集。
    /// 注意：这是绘制序列，不是骨骼动画，角度不能自由旋转。
    /// </summary>
    public class Flipbook : MonoBehaviour
    {
        public Texture2D atlas;
        public int columns = 6;
        public int rows = 6;
        public int frameCount = 36;
        public float fps = 14f;
        public bool loop = true;
        [Tooltip("聊天时放慢一点，像人在说话")]
        public float idleFps = 9f;

        [Header("输出（二选一或都填）")]
        public Renderer targetRenderer;
        public RawImage targetImage;

        int frame;
        float timer;
        MaterialPropertyBlock block;
        static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

        public bool Paused { get; set; }
        public bool Speaking { get; set; }
        public int Frame { get { return frame; } }

        void OnEnable()
        {
            Apply();
        }

        void Update()
        {
            if (Paused) return;
            float rate = Speaking ? fps : idleFps;
            timer += Time.unscaledDeltaTime;
            float step = 1f / Mathf.Max(1f, rate);
            while (timer >= step)
            {
                timer -= step;
                frame++;
                if (frame >= frameCount)
                {
                    if (!loop) { frame = frameCount - 1; break; }
                    frame = 0;
                }
                Apply();
            }
        }

        public void Restart()
        {
            frame = 0;
            timer = 0f;
            Apply();
        }

        void Apply()
        {
            if (atlas == null) return;
            int cols = Mathf.Max(1, columns);
            int r = frame / cols;
            int c = frame % cols;

            if (targetImage != null)
            {
                targetImage.texture = atlas;
                float w = 1f / cols;
                float h = 1f / Mathf.Max(1, rows);
                // 图集第 0 行在最上面，UV 的 y 从下往上
                targetImage.uvRect = new Rect(c * w, 1f - (r + 1) * h, w, h);
            }

            if (targetRenderer != null)
            {
                if (block == null) block = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(block);
                block.SetVector(MainTexST, new Vector4(1f / cols, 1f / Mathf.Max(1, rows), c * (1f / cols),
                    1f - (r + 1) * (1f / Mathf.Max(1, rows))));
                targetRenderer.SetPropertyBlock(block);
            }
        }
    }
}
