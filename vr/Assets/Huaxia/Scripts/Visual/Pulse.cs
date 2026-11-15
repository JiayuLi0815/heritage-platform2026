using UnityEngine;

namespace Huaxia
{
    /// <summary>信息点的轻微呼吸动画，避免使用夸张的发光符文。</summary>
    public class Pulse : MonoBehaviour
    {
        public float amplitude = 0.07f;
        public float speed = 1.6f;

        Vector3 baseScale;
        Light glow;
        float baseIntensity;

        void Awake()
        {
            baseScale = transform.localScale;
            glow = GetComponentInChildren<Light>();
            if (glow != null) baseIntensity = glow.intensity;
        }

        void Update()
        {
            float s = Mathf.Sin(Time.time * speed);
            transform.localScale = baseScale * (1f + s * amplitude);
            if (glow != null) glow.intensity = baseIntensity * (0.85f + 0.15f * s);
        }
    }
}
