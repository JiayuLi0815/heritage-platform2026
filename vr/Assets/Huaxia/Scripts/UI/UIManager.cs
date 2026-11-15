using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Huaxia
{
    /// <summary>
    /// 全局 HUD / 信息面板 / 场景淡入淡出。
    /// 目前使用屏幕空间 UI；接 VR 时可整体替换为世界空间画布。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        [Header("HUD")]
        public Text chapterTitle;
        public Text chapterSubtitle;
        public Text objectivesText;
        public Text guideText;
        public Text promptText;
        public Text hintText;
        public GameObject crosshair;
        public GameObject toastRoot;
        public Text toastText;

        [Header("Modal")]
        public GameObject modalRoot;
        public Text modalTitle;
        public Text modalSubtitle;
        public Text modalBody;
        public Text modalFooter;

        [Header("Fade")]
        public Image fadeImage;
        public Text fadeText;

        bool modalOpen;
        Action confirmAction;
        int openedFrame = -1;
        bool transitioning;
        bool promptSticky;
        bool overlayOpen;

        // 底部那条提示条平时不该占着一块半透明的黑。没人说话就整个淡掉，
        // 有话的时候才浮上来——淡入淡出都走同一个 CanvasGroup。
        CanvasGroup promptGroup;
        float promptTarget;
        Coroutine toastTimer;

        public bool BlocksPlayer { get { return modalOpen || transitioning || overlayOpen; } }
        public bool ModalOpen { get { return modalOpen; } }

        /// <summary>整屏面板（遗产立牌）占用输入时调这个。和模态弹窗各管各的。</summary>
        public void SetOverlay(bool open)
        {
            overlayOpen = open;
        }

        void Awake()
        {
            Instance = this;
            if (fadeImage != null)
            {
                var c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
                fadeImage.raycastTarget = false;
            }
            if (modalRoot != null) modalRoot.SetActive(false);
            SetupPromptFade();
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (toastRoot != null) toastRoot.SetActive(false);
        }

        /// <summary>提示条整块（底板 + 文字）挂一个 CanvasGroup，平时 alpha = 0。</summary>
        void SetupPromptFade()
        {
            if (promptText == null || promptText.transform.parent == null) return;
            var panel = promptText.transform.parent.gameObject;
            promptGroup = panel.GetComponent<CanvasGroup>();
            if (promptGroup == null) promptGroup = panel.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
        }

        void UpdatePromptFade()
        {
            if (promptGroup == null) return;
            promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, promptTarget,
                Time.unscaledDeltaTime * 6f);
            if (promptTarget <= 0f && promptGroup.alpha <= 0.01f &&
                promptText != null && promptText.gameObject.activeSelf)
                promptText.gameObject.SetActive(false);
        }

        void Start()
        {
            if (fadeImage != null) StartCoroutine(FadeInRoutine());
        }

        IEnumerator FadeInRoutine()
        {
            float t = 0.6f;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                var c = fadeImage.color;
                c.a = Mathf.Clamp01(t / 0.6f);
                fadeImage.color = c;
                yield return null;
            }
        }

        public void SetChapterTitle(string title, string subtitle = "")
        {
            if (chapterTitle != null) chapterTitle.text = VRKeyLabels.Fix(title);
            if (chapterSubtitle != null) chapterSubtitle.text = VRKeyLabels.Fix(subtitle);
        }

        public void SetObjectives(string text)
        {
            if (objectivesText != null) objectivesText.text = VRKeyLabels.Fix(text);
        }

        /// <summary>「下一处往哪走」：由 <see cref="ObjectiveGuide"/> 每半秒刷新。</summary>
        public void SetGuide(string text)
        {
            if (guideText == null) return;
            guideText.text = VRKeyLabels.Fix(text);
            guideText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void ShowPrompt(string text)
        {
            ShowPrompt(text, false);
        }

        /// <summary>
        /// sticky = 这行字归我一帧一帧地留着，别人 HidePrompt 收不走。
        /// 路的尽头那种「按 E 过去」用它，免得视线一挪字就没了。
        /// </summary>
        public void ShowPrompt(string text, bool sticky)
        {
            if (promptText == null) return;
            if (promptSticky && !sticky) return;
            promptSticky = sticky;
            promptText.text = VRKeyLabels.Fix(text);
            if (!promptText.gameObject.activeSelf) promptText.gameObject.SetActive(true);
            promptTarget = 1f;
        }

        public void HidePrompt()
        {
            if (promptSticky) return;
            promptTarget = 0f;
        }

        /// <summary>强行收回提示（含 sticky）。</summary>
        public void ClearPrompt()
        {
            promptSticky = false;
            promptTarget = 0f;
            if (promptGroup != null) promptGroup.alpha = 0f;
            if (promptText != null) promptText.gameObject.SetActive(false);
        }

        /// <summary>seconds > 0 时自己收回去；0 是常驻（序章开场那种）。</summary>
        public void ShowToast(string text, float seconds = 0f)
        {
            if (toastRoot == null) return;
            toastRoot.SetActive(true);
            if (toastText != null)
            {
                toastText.text = VRKeyLabels.Fix(text);
                // 讲稿有长有短：条子按文字实际高度长，别让长句从屏幕顶上溢出去。
                var toastRt = toastRoot.GetComponent<RectTransform>();
                if (toastRt != null)
                {
                    float h = Mathf.Clamp(toastText.preferredHeight + 12f, 46f, 280f);
                    toastRt.sizeDelta = new Vector2(toastRt.sizeDelta.x, h);
                }
            }
            if (toastTimer != null) StopCoroutine(toastTimer);
            toastTimer = seconds > 0f ? StartCoroutine(HideToastAfter(seconds)) : null;
        }

        public void HideToast()
        {
            if (toastTimer != null) { StopCoroutine(toastTimer); toastTimer = null; }
            if (toastRoot != null) toastRoot.SetActive(false);
        }

        IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            toastTimer = null;
            if (toastRoot != null) toastRoot.SetActive(false);
        }

        public void ShowModal(string title, string subtitle, string body, string footer, Action onConfirm = null)
        {
            modalOpen = true;
            openedFrame = Time.frameCount;
            confirmAction = onConfirm;
            if (modalTitle != null) modalTitle.text = VRKeyLabels.Fix(title);
            if (modalSubtitle != null) modalSubtitle.text = VRKeyLabels.Fix(subtitle);
            if (modalBody != null) modalBody.text = VRKeyLabels.Fix(body);
            if (modalFooter != null) modalFooter.text = VRKeyLabels.Fix(footer);
            if (modalRoot != null) modalRoot.SetActive(true);
            HidePrompt();
        }

        public void CloseModal()
        {
            modalOpen = false;
            confirmAction = null;
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        void Update()
        {
            UpdatePromptFade();
            if (!modalOpen) return;
            if (Time.frameCount <= openedFrame) return;

            // 确认只认空格 / 回车 / 左键。E 只属于世界交互，
            // 否则站在互动点前读弹窗，一按 E 就把门外那道界门也按了。
            bool confirmPressed = Input.GetKeyDown(KeyCode.Space)
                                  || Input.GetKeyDown(KeyCode.Return)
                                  || Input.GetMouseButtonDown(0);
            bool cancelPressed = Input.GetKeyDown(KeyCode.Escape);

            if (confirmAction != null && confirmPressed)
            {
                var action = confirmAction;
                CloseModal();
                action();
            }
            else if (cancelPressed || (confirmAction == null && (confirmPressed || Input.GetMouseButtonDown(0))))
            {
                CloseModal();
            }
        }

        /// <summary>手柄 A / 扳机：确认当前弹窗。和空格走同一条路。</summary>
        public void Confirm()
        {
            if (!modalOpen) return;
            var action = confirmAction;
            CloseModal();
            if (action != null) action();
        }

        /// <summary>手柄 B / 菜单：关掉弹窗。</summary>
        public void Cancel()
        {
            if (modalOpen) CloseModal();
        }

        public void LoadScene(string sceneName)
        {
            LoadScene(sceneName, null);
        }

        /// <summary>
        /// 淡黑换场景，落到目标场景里 id 匹配的 SpawnPoint 上。
        /// label 不为空时，黑场中间留一行地名——过场不再是一段没有着落的黑。
        /// </summary>
        public void LoadScene(string sceneName, string spawnId, string label = null)
        {
            if (transitioning) return;
            GameState.pendingSpawn = spawnId;
            ClearPrompt();
            StartCoroutine(LoadRoutine(sceneName, label));
        }

        /// <summary>
        /// 体验到此为止：先淡黑，把最后一句话留在黑上，停一会儿，再回到序章。
        /// 不做字幕表、不做统计——这本书摊开就是这么一页。
        /// </summary>
        public void EndExperience(string sceneName, string spawnId, string lastLine)
        {
            if (transitioning) return;
            StartCoroutine(EndRoutine(sceneName, spawnId, lastLine));
        }

        IEnumerator EndRoutine(string sceneName, string spawnId, string lastLine)
        {
            transitioning = true;
            ClearPrompt();
            if (toastRoot != null) toastRoot.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            float duration = 1.6f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var c = fadeImage.color;
                c.a = Mathf.Clamp01(t / duration);
                fadeImage.color = c;
                yield return null;
            }

            if (fadeText != null)
            {
                fadeText.text = VRKeyLabels.Fix(lastLine);
                fadeText.color = new Color(fadeText.color.r, fadeText.color.g, fadeText.color.b, 1f);
            }

            yield return new WaitForSeconds(3.0f);

            GameState.pendingSpawn = spawnId;
            SceneManager.LoadScene(sceneName);
        }

        IEnumerator LoadRoutine(string sceneName, string label)
        {
            transitioning = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            float duration = 0.55f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var c = fadeImage.color;
                c.a = Mathf.Clamp01(t / duration);
                fadeImage.color = c;
                yield return null;
            }

            // 黑场上停一下，写清「这是哪儿」。没有门缝透光，至少别让切换是一片空白。
            if (fadeText != null && !string.IsNullOrEmpty(label))
            {
                fadeText.text = label;
                var lc = fadeText.color;
                lc.a = 1f;
                fadeText.color = lc;
                yield return new WaitForSeconds(0.85f);
                fadeText.text = "";
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
