using UnityEngine;

namespace Huaxia
{
    /// <summary>世界空间标签始终朝向主摄像机（仅绕 Y 轴）。</summary>
    public class Billboard : MonoBehaviour
    {
        public bool lockVertical = true;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 dir = transform.position - cam.transform.position;
            if (lockVertical) dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
