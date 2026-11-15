using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 面板里那块「三维」视口的展台：一件微缩的三维文物，摆在场景外的一块空地上，
    /// 由面板自己开一台只拍这一层的相机渲到 RenderTexture 上。
    ///
    /// 用图层把展台和山谷隔开——主线相机不拍这一层，所以这些模型就摆在
    /// 序章地底下的空场里也不会漏进画面。
    /// </summary>
    public class HeritageShowcase : MonoBehaviour
    {
        /// <summary>展台专用图层（见 HuaxiaBuilder.EnsureLayers）。</summary>
        public const int Layer = 30;
        public const string LayerName = "HeritageShowcase";

        public string siteId;
        public Transform turntable;
        public Vector3 cameraOffset = new Vector3(0f, 1.15f, -2.45f);
        public Vector3 cameraTarget = new Vector3(0f, 0.95f, 0f);
        public string hint = "按住左键拖动 · 绕着看";

        public static HeritageShowcase Find(string id)
        {
            var all = Object.FindObjectsOfType<HeritageShowcase>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].siteId == id) return all[i];
            // 退回第一座，至少别开天窗
            return all.Length > 0 ? all[0] : null;
        }
    }
}
