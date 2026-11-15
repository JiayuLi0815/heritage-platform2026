using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 藏卷阁的两扇正门。
    /// 三处遗址都进去过，门自己转开；没走齐，门闩就横着——
    /// 不用文字解释，站到门前先看见的是一道闭着的门。
    /// </summary>
    public class HallDoor : MonoBehaviour
    {
        public Transform leftHinge;
        public Transform rightHinge;

        [Tooltip("门闩：开门时撤掉")]
        public GameObject latch;

        [Tooltip("完全打开时门扇转过的角度")]
        public float openAngle = 84f;

        [Tooltip("开门速度（度/秒）")]
        public float speed = 60f;

        float t;        // 0 = 关着，1 = 全开
        float target;

        void Start()
        {
            bool open = GameState.AllChaptersVisited;
            target = open ? 1f : 0f;
            if (latch != null) latch.SetActive(!open);
            Apply();
        }

        void Update()
        {
            if (Mathf.Approximately(t, target)) return;
            t = Mathf.MoveTowards(t, target, Time.deltaTime * speed / Mathf.Max(1f, openAngle));
            Apply();
        }

        void Apply()
        {
            float a = t * openAngle;
            if (leftHinge != null) leftHinge.localRotation = Quaternion.Euler(0f, a, 0f);
            if (rightHinge != null) rightHinge.localRotation = Quaternion.Euler(0f, -a, 0f);
        }
    }
}
