using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 挂在每个场景里的音乐锚点：进场时告诉 <see cref="AmbientDirector"/> 该播哪一段。
    /// 音乐本身跨场景常驻，这个组件只负责"这一场听什么"。
    /// </summary>
    public class SceneAmbient : MonoBehaviour
    {
        [Tooltip("Resources/Audio 下的文件名，不带扩展名")]
        public string clipName = "amb_archive";

        [Range(0f, 1f)] public float volume = 0.55f;
        public float fadeSeconds = 2.5f;

        void Start()
        {
            // 空名字 = 这一场不要音乐。必须显式 Stop，否则上一场的曲子会
            // 跟着常驻的 AmbientDirector 一直播下去。
            if (string.IsNullOrEmpty(clipName))
            {
                AmbientDirector.Stop(fadeSeconds);
                return;
            }
            AmbientDirector.Play(clipName, volume, fadeSeconds);
        }
    }
}
