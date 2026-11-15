using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 全屏胶片调色：曝光、ACES 曲线、对比度、暗冷亮暖分离、暗角。
    /// 每个章节进场景时由 HuaxiaBuilder 挂到主相机上并赋一组预设，
    /// 预览渲染（HuaxiaBuilder.RenderPreview）也会把这一组参数复制到离屏相机。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CinematicGrade : MonoBehaviour
    {
        public float exposure = 1.05f;
        [Range(0.5f, 1.5f)] public float contrast = 1.10f;
        [Range(0f, 2f)] public float saturation = 1.0f;
        [Range(0f, 1f)] public float vignette = 0.28f;

        public Color liftColor = new Color(0f, 0f, 0f, 1f);
        public Color gammaColor = new Color(1f, 1f, 1f, 1f);
        public Color gainColor = new Color(1f, 1f, 1f, 1f);

        /// <summary>rgb 乘在暗部，a 是强度。</summary>
        public Color shadowsTint = new Color(0.90f, 0.96f, 1.08f, 0.45f);
        /// <summary>rgb 乘在亮部，a 是强度。</summary>
        public Color highlightsTint = new Color(1.06f, 1.00f, 0.93f, 0.45f);

        Material mat;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null)
            {
                var sh = Shader.Find("Hidden/Huaxia/Grade");
                if (sh == null || !sh.isSupported)
                {
                    Graphics.Blit(src, dst);
                    return;
                }
                mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            }
            ApplyTo(mat);
            Graphics.Blit(src, dst, mat);
        }

        /// <summary>把实例参数写进材质，供 OnRenderImage 与离屏预览共用。</summary>
        public void ApplyTo(Material m)
        {
            m.SetFloat("_Exposure", exposure);
            m.SetFloat("_Contrast", contrast);
            m.SetFloat("_Saturation", saturation);
            m.SetFloat("_Vignette", vignette);
            m.SetColor("_LiftColor", liftColor);
            m.SetColor("_GammaColor", gammaColor);
            m.SetColor("_GainColor", gainColor);
            m.SetColor("_ShadowsTint", shadowsTint);
            m.SetColor("_HighlightsTint", highlightsTint);
        }

        void OnDestroy()
        {
            if (mat != null) DestroyImmediate(mat);
        }
    }
}
