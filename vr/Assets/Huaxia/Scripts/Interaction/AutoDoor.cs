using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 走近自己开、走远自己合的双扇门（秦陵阙门用）。
    /// 不做「按 E 开门」：这扇门是路的一部分，不是一道题。
    /// </summary>
    public class AutoDoor : MonoBehaviour
    {
        [Tooltip("门轴：每扇门一个空物体，门板挂在它下面")]
        public Transform[] hinges;

        [Tooltip("每扇门关上时的本地 yaw")]
        public float[] closedYaw;

        [Tooltip("每扇门开门时往哪边转（度，正负）")]
        public float[] openDirection;

        public float openAngle = 95f;
        public float triggerDistance = 8f;
        public float closeDistance = 11f;
        public float degreesPerSecond = 80f;

        [Header("声音")]
        // 开门声已移除：原来那段合成吱呀声不对。留空就静音。
        public string creakResource = "";
        [Range(0f, 1f)] public float creakVolume = 0.55f;

        Transform player;
        float open;          // 0 = 关，1 = 全开
        bool wasOpen;
        AudioSource src;
        AudioClip creak;

        void Start()
        {
            var pc = FindObjectOfType<DesktopPlayerController>();
            if (pc != null) player = pc.transform;
            if (string.IsNullOrEmpty(creakResource)) return;

            creak = Resources.Load<AudioClip>(creakResource);
            if (creak == null) return;

            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 3f;
            src.maxDistance = 28f;
        }

        void Update()
        {
            if (player == null || hinges == null || hinges.Length == 0) return;

            float d = Vector3.Distance(player.position, transform.position);
            bool wantOpen = d <= (wasOpen ? closeDistance : triggerDistance);

            if (wantOpen && !wasOpen && src != null && creak != null)
            {
                src.pitch = Random.Range(0.92f, 1.06f);
                src.PlayOneShot(creak, creakVolume);
            }
            wasOpen = wantOpen;

            float target = wantOpen ? 1f : 0f;
            if (!Mathf.Approximately(open, target))
            {
                open = Mathf.MoveTowards(open, target, degreesPerSecond / Mathf.Max(1f, openAngle) * Time.deltaTime);
                Apply();
            }
        }

        void Apply()
        {
            for (int i = 0; i < hinges.Length; i++)
            {
                if (hinges[i] == null) continue;
                float baseYaw = (closedYaw != null && i < closedYaw.Length) ? closedYaw[i] : 0f;
                float dir = (openDirection != null && i < openDirection.Length) ? openDirection[i] : 1f;
                hinges[i].localRotation = Quaternion.Euler(0f, baseYaw + dir * openAngle * open, 0f);
            }
        }
    }
}
