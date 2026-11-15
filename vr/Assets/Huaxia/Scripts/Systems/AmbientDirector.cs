using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 全场唯一的环境音乐播放器，跨场景常驻。
    /// 每个场景开始时调用 <see cref="Play"/>，两条 AudioSource 交叉淡入淡出，
    /// 走路穿过场景边界时音乐不会断。
    /// 音频文件在 Resources/Audio 下，用不带扩展名的路径加载。
    /// </summary>
    public class AmbientDirector : MonoBehaviour
    {
        public static AmbientDirector Instance { get; private set; }

        const float DefaultFade = 2.5f;

        AudioSource a;
        AudioSource b;
        bool aIsActive;
        string currentPath;
        float targetVolume = 0.55f;
        float fadeSpeed = 1f / DefaultFade;

        public static AmbientDirector Ensure()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("AmbientDirector");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AmbientDirector>();
            return Instance;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            a = CreateSource("A");
            b = CreateSource("B");
            aIsActive = true;

            // 终章这类没有玩家相机的场景也要听得见：备一个 2D 监听器，
            // 只有在场景里找不到别的 AudioListener 时才启用。
            listenerGO = new GameObject("Ambient Listener");
            listenerGO.transform.SetParent(transform, false);
            fallbackListener = listenerGO.AddComponent<AudioListener>();
            fallbackListener.enabled = false;
        }

        AudioSource CreateSource(string name)
        {
            var go = new GameObject("Ambient " + name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.bypassEffects = true;
            src.bypassListenerEffects = true;
            src.priority = 64;
            return src;
        }

        /// <summary>播当前场景的环境音乐；同一首则只调整音量，不重播。</summary>
        public static void Play(string resourcePath, float volume = 0.55f, float fade = DefaultFade)
        {
            var d = Ensure();
            d.PlayInternal(resourcePath, volume, fade);
        }

        /// <summary>淡出（例如进入阅读书页的静场，或终章之后）。</summary>
        public static void Stop(float fade = DefaultFade)
        {
            if (Instance == null) return;
            Instance.currentPath = null;
            Instance.fadeSpeed = 1f / Mathf.Max(0.05f, fade);
        }

        void PlayInternal(string resourcePath, float volume, float fade)
        {
            targetVolume = volume;
            fadeSpeed = 1f / Mathf.Max(0.05f, fade);
            if (string.IsNullOrEmpty(resourcePath) || resourcePath == currentPath) return;

            var clip = Resources.Load<AudioClip>("Audio/" + resourcePath);
            if (clip == null)
            {
                Debug.LogWarning("[Huaxia] 找不到环境音乐 Resources/Audio/" + resourcePath);
                return;
            }

            currentPath = resourcePath;
            var next = aIsActive ? b : a;
            var prev = aIsActive ? a : b;
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            aIsActive = !aIsActive;
            // prev 由 Update 里自然淡出，不用 Stop，避免爆音
            prevFadingOut = prev;
        }

        AudioSource prevFadingOut;
        GameObject listenerGO;
        AudioListener fallbackListener;
        float listenerCheck;

        void Update()
        {
            var active = aIsActive ? a : b;
            // Stop() 之后 currentPath 为空，此时当前源要往 0 走而不是往 targetVolume 走，
            // 两段分开写会让两个 MoveTowards 互相抵消，结果永远淡不出去。
            float goal = string.IsNullOrEmpty(currentPath) ? 0f : targetVolume;
            if (active != null && active.isPlaying)
            {
                active.volume = Mathf.MoveTowards(active.volume, goal, fadeSpeed * Time.unscaledDeltaTime);
                if (goal <= 0f && active.volume <= 0.001f) active.Stop();
            }

            if (prevFadingOut != null && prevFadingOut != active)
            {
                prevFadingOut.volume = Mathf.MoveTowards(
                    prevFadingOut.volume, 0f, fadeSpeed * Time.unscaledDeltaTime);
                if (prevFadingOut.volume <= 0.001f)
                {
                    prevFadingOut.Stop();
                    prevFadingOut.clip = null;
                    prevFadingOut = null;
                }
            }

            listenerCheck -= Time.unscaledDeltaTime;
            if (listenerCheck <= 0f)
            {
                listenerCheck = 0.5f;
                if (fallbackListener != null)
                    fallbackListener.enabled = FindObjectOfType<AudioListener>() == null;
            }
        }
    }
}
