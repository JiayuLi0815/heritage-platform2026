using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Huaxia
{
    /// <summary>
    /// 遗产立牌点开后的整屏面板：左文右图，可切到三维展台，弘文在旁边念解说。
    /// 整个面板由 HuaxiaBuilder 在场景里生成，这里只负责填内容与响应键鼠。
    ///
    /// 键位：← →（或 A/D）换图　Tab 切换影像/三维　空格 听她再说一遍
    /// 　　　Esc 关闭　三维里按住左键拖动可转模型
    /// </summary>
    public class HeritagePanel : MonoBehaviour
    {
        public static HeritagePanel Instance;

        [Header("根")]
        public GameObject root;
        public CanvasGroup group;

        [Header("抬头")]
        public Text volume;
        public Text title;
        public Text subtitle;
        public Text meta;

        [Header("正文")]
        public Text body;
        public Text keys;

        [Header("影像")]
        public RawImage photo;
        public RawImage[] thumbs;
        public Text pager;

        [Header("三维")]
        public RawImage viewport;
        public Text viewHint;

        [Header("按钮")]
        public RectTransform btnPrev;
        public RectTransform btnNext;
        public RectTransform btnClose;
        public RectTransform btnPhoto;
        public RectTransform btnModel;
        public RectTransform btnVoice;
        public Text labelPhoto;
        public Text labelModel;

        [Header("弘文")]
        public RawImage portrait;
        public Flipbook portraitBook;
        public Text speakerName;
        public Text speakerLine;

        [Header("底")]
        public Text footer;

        [Header("展台")]
        public float showcaseFov = 30f;

        static readonly Color TabOn = new Color(0.95f, 0.79f, 0.48f, 1f);
        static readonly Color TabOff = new Color(0.62f, 0.60f, 0.56f, 1f);
        static readonly Color ThumbOn = new Color(1f, 1f, 1f, 1f);
        static readonly Color ThumbOff = new Color(0.55f, 0.55f, 0.58f, 0.75f);

        HeritageSite site;
        Transform nextAnchor;
        int index;
        int mode; // 0 = 影像，1 = 三维
        bool opened;
        bool boarded;   // 这一屏是序章立牌打开的（要指路），还是现场点位（只抬手往前示意）

        // 头显里的按键边沿：按住不重复，松手才认下一次
        bool vrA, vrB, vrX, vrTrigger, vrSideLatch;

        AudioSource voice;
        Coroutine fade;

        HeritageShowcase stage;
        Camera stageCamera;
        RenderTexture stageTexture;
        Transform turntable;
        bool dragging;
        Vector3 lastMouse;
        Vector2 photoHome;   // 两枚页签各自的原始位置，用来在没有三维时把影像页签摆正
        Vector2 modelHome;
        bool homes;

        public bool IsOpen { get { return opened; } }

        void Awake()
        {
            Instance = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            voice.volume = 1f;
            if (root != null) root.SetActive(false);
            if (portraitBook != null) portraitBook.Paused = true;
        }

        void OnDestroy()
        {
            if (stageTexture != null) stageTexture.Release();
        }

        // ------------------------------------------------------------ 开关

        public void Open(string heritageId, Transform leadTo = null, bool isBoard = false)
        {
            var s = HeritageArchive.Get(heritageId);
            if (s == null)
            {
                Debug.LogWarning("[HeritagePanel] 未知遗产编号：" + heritageId);
                return;
            }
            if (opened) Close();

            site = s;
            nextAnchor = leadTo;
            boarded = isBoard;
            index = 0;
            mode = 0;
            opened = true;

            var ui = UIManager.Instance;
            if (ui != null)
            {
                ui.HidePrompt();
                ui.HideToast();
                ui.SetOverlay(true);
            }
            DigitalHuman.Silence();
            if (DigitalHuman.Instance != null) DigitalHuman.Instance.Attend();

            Fill();
            if (root != null) root.SetActive(true);
            if (group != null) group.blocksRaycasts = true;
            if (portraitBook != null) portraitBook.Paused = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Speak();
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeRoutine(0f, 1f, 0.18f));
        }

        public void Close()
        {
            if (!opened) return;
            opened = false;
            if (voice != null) voice.Stop();
            if (portraitBook != null) portraitBook.Paused = true;
            if (group != null) group.blocksRaycasts = false;
            if (fade != null) StopCoroutine(fade);
            fade = null;
            if (root != null)
            {
                if (group != null) group.alpha = 0f;
                root.SetActive(false);
            }
            if (stage != null) stage.gameObject.SetActive(false);
            stage = null;
            turntable = null;

            // 光标交还给玩家控制器：按一下左键就会重新锁回去
            var ui = UIManager.Instance;
            if (ui != null)
            {
                ui.SetOverlay(false);
                ui.ShowToast(boarded ? "合上册页。继续走。" : "合上册页。顺着她的示意往前走。", 2.2f);
            }

            // 立牌：她把路指给你，转身朝那条岔路；现场点位：朝下一处抬手，让人往前走
            if (DigitalHuman.Instance != null && site != null)
            {
                if (boarded) DigitalHuman.Instance.GuideTo(site.id);
                else if (nextAnchor != null) DigitalHuman.Instance.Beckon(nextAnchor.position);
            }
            nextAnchor = null;
        }

        IEnumerator FadeRoutine(float from, float to, float seconds)
        {
            if (group == null) yield break;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            group.alpha = to;
        }

        // ------------------------------------------------------------ 内容

        void Fill()
        {
            if (volume != null) volume.text = site.volume;
            if (title != null) title.text = site.name;
            if (subtitle != null) subtitle.text = site.en;
            if (meta != null)
                meta.text = site.code + "　｜　" + site.year + "　｜　" + site.place + "\n" +
                            site.era + "　｜　" + site.scale;

            if (body != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("<color=#B9AE93>").Append(site.tierLine).Append("</color>\n\n");
                for (int i = 0; i < site.paragraphs.Length; i++)
                    sb.Append(site.paragraphs[i]).Append(i == site.paragraphs.Length - 1 ? "" : "\n\n");
                body.text = sb.ToString();
            }

            if (keys != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("<color=#8C8579>走到现场，先看这四处</color>");
                for (int i = 0; i < site.keys.Length; i++)
                    sb.Append("\n<color=#D8B26A>·</color> ").Append(site.keys[i]);
                keys.text = sb.ToString();
            }

            if (speakerName != null) speakerName.text = "弘文 · 数智讲解员";
            if (footer != null)
                footer.text = VRKeyLabels.Active
                    ? (site.HasStage
                        ? "左摇杆 换图　扳机 影像／三维　X 重听　B 合上"
                        : "左摇杆 换图　X 重听　B 合上")
                    : (site.HasStage
                        ? "← → 换图　Tab 影像／三维　空格 重听　Esc 合上"
                        : "← → 换图　空格 重听　Esc 合上");
            // 没有三维的卷（良渚）：连页签本身一起收起来，否则会剩下一个空框／
            // 一个看不见却还能点的热区
            bool hasStage = site.HasStage;
            if (!homes)
            {
                if (btnPhoto != null) photoHome = btnPhoto.anchoredPosition;
                if (btnModel != null) modelHome = btnModel.anchoredPosition;
                homes = btnPhoto != null && btnModel != null;
            }
            if (btnModel != null) btnModel.gameObject.SetActive(hasStage);
            if (labelModel != null) labelModel.gameObject.SetActive(hasStage);
            // 只剩一枚页签时摆回两枚页签的正中，免得右半边空一块
            if (homes)
                btnPhoto.anchoredPosition = hasStage
                    ? photoHome
                    : new Vector2((photoHome.x + modelHome.x) * 0.5f, photoHome.y);

            FillThumbs();
            ShowImage(0);
            SetMode(0);
        }

        void FillThumbs()
        {
            if (thumbs == null) return;
            for (int i = 0; i < thumbs.Length; i++)
            {
                if (thumbs[i] == null) continue;
                bool used = i < site.images.Length;
                thumbs[i].gameObject.SetActive(used);
                if (!used) continue;
                thumbs[i].texture = HeritageArchive.Image(site, i);
            }
        }

        void ShowImage(int i)
        {
            int count = site.images.Length;
            index = ((i % count) + count) % count;

            var tex = HeritageArchive.Image(site, index);
            if (photo != null)
            {
                photo.texture = tex;
                var fitter = photo.GetComponent<AspectRatioFitter>();
                if (fitter != null && tex != null)
                {
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = tex.width / (float)tex.height;
                }
            }
            if (pager != null)
                pager.text = (index + 1) + " / " + count + "　　" + CaptionOf(index);

            if (thumbs != null)
                for (int k = 0; k < thumbs.Length; k++)
                    if (thumbs[k] != null && k < site.images.Length)
                        thumbs[k].color = k == index ? ThumbOn : ThumbOff;
        }

        string CaptionOf(int i)
        {
            if (site != null && site.captions != null && i < site.captions.Length &&
                !string.IsNullOrEmpty(site.captions[i]))
                return site.captions[i];
            return i == 0 ? "遗址主图" : "影像 " + i.ToString("00") + "（来源：项目遗产库）";
        }

        void SetMode(int m)
        {
            mode = m;
            bool three = m == 1;
            if (photo != null) photo.gameObject.SetActive(!three);
            if (viewport != null) viewport.gameObject.SetActive(three);
            if (pager != null) pager.gameObject.SetActive(!three);

            if (labelPhoto != null) labelPhoto.color = three ? TabOff : TabOn;
            if (labelModel != null) labelModel.color = three ? TabOn : TabOff;

            if (three) OpenStage();
        }

        // ------------------------------------------------------------ 三维展台

        void OpenStage()
        {
            if (stage != null) return;
            stage = HeritageShowcase.Find(site.model);
            if (stage == null)
            {
                if (viewHint != null) viewHint.text = "这一场没有布置展台";
                if (viewport != null) viewport.color = new Color(0.06f, 0.06f, 0.07f, 1f);
                return;
            }

            stage.gameObject.SetActive(true);
            turntable = stage.turntable != null ? stage.turntable : stage.transform;
            turntable.localRotation = Quaternion.identity;

            if (stageCamera == null)
            {
                var go = new GameObject("StageCamera");
                go.transform.SetParent(transform, false);
                stageCamera = go.AddComponent<Camera>();
                stageCamera.enabled = false;
                stageCamera.clearFlags = CameraClearFlags.SolidColor;
                stageCamera.backgroundColor = new Color(0.045f, 0.045f, 0.055f, 1f);
                stageCamera.fieldOfView = showcaseFov;
                stageCamera.nearClipPlane = 0.05f;
                stageCamera.farClipPlane = 30f;
                stageCamera.allowHDR = true;
                stageCamera.cullingMask = 1 << HeritageShowcase.Layer;
            }

            if (stageTexture == null)
            {
                stageTexture = new RenderTexture(1024, 640, 20, RenderTextureFormat.ARGB32);
                stageTexture.antiAliasing = 4;
                stageTexture.Create();
            }
            stageCamera.targetTexture = stageTexture;
            if (viewport != null)
            {
                viewport.texture = stageTexture;
                viewport.color = Color.white;
            }

            // 机位随展台走：展台摆在场景外的一块净土上，镜头只认它自己的坐标
            var look = stage.transform.position + stage.transform.rotation * stage.cameraTarget;
            stageCamera.transform.position = stage.transform.position + stage.transform.rotation * stage.cameraOffset;
            stageCamera.transform.LookAt(look);
            stageCamera.enabled = true;

            if (viewHint != null) viewHint.text = stage.hint;
        }

        // ------------------------------------------------------------ 语音

        void Speak()
        {
            var clip = HeritageArchive.Voice(site);
            if (voice == null) return;
            voice.Stop();
            if (clip == null)
            {
                if (speakerLine != null) speakerLine.text = "（这一段的语音包缺失）";
                return;
            }
            voice.clip = clip;
            voice.Play();
            if (portraitBook != null) portraitBook.Speaking = true;
            if (speakerLine != null)
                speakerLine.text = VRKeyLabels.Active ? "正在讲 · 按 X 重听" : "正在讲 · 空格重听";
            CancelInvoke("VoiceDone");
            Invoke("VoiceDone", clip.length);
        }

        void VoiceDone()
        {
            if (portraitBook != null) portraitBook.Speaking = false;
            if (speakerLine != null)
                speakerLine.text = VRKeyLabels.Active ? "讲完了 · 按 X 重听" : "讲完了 · 空格重听";
        }

        // ------------------------------------------------------------ 输入

        void Update()
        {
            if (!opened) return;

            // 头显里没有键盘：这一屏整屏占着视野，手柄输入由面板自己收
            if (VRKeyLabels.Active)
            {
                UpdateVR();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            {
                Close();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Speak();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Tab) && site != null && site.HasStage)
            {
                SetMode(mode == 0 ? 1 : 0);
                return;
            }

            if (mode == 0)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) ShowImage(index - 1);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) ShowImage(index + 1);
            }

            DragStage();
            Click();
        }

        /// <summary>
        /// 手柄直接操作整屏面板：
        ///   左摇杆左右 换图　右手扳机／A 影像↔三维　左手 X 重听　右手 B 合上
        ///   三维里右摇杆左右转模型（桌面端是按住左键拖）
        /// </summary>
        void UpdateVR()
        {
            var r = VrInput.Right;
            var l = VrInput.Left;

            bool a = VrInput.Button(r, CommonUsages.primaryButton);
            bool b = VrInput.Button(r, CommonUsages.secondaryButton);
            bool x = VrInput.Button(l, CommonUsages.primaryButton);
            bool trig = VrInput.Axis(r, CommonUsages.trigger) >= 0.55f;

            bool aDown = a && !vrA; vrA = a;
            bool bDown = b && !vrB; vrB = b;
            bool xDown = x && !vrX; vrX = x;
            bool trigDown = trig && !vrTrigger; vrTrigger = trig;

            if (bDown) { Close(); return; }
            if (xDown) { Speak(); return; }
            if ((aDown || trigDown) && site != null && site.HasStage)
            {
                SetMode(mode == 0 ? 1 : 0);
                return;
            }

            if (mode == 0)
            {
                float sx = VrInput.Stick(l).x;
                if (Mathf.Abs(sx) > 0.7f && !vrSideLatch)
                {
                    ShowImage(index + (sx > 0f ? 1 : -1));
                    vrSideLatch = true;
                }
                else if (Mathf.Abs(sx) < 0.3f)
                {
                    vrSideLatch = false;
                }
            }
            else if (turntable != null)
            {
                float rx = VrInput.Stick(r).x;
                turntable.Rotate(Vector3.up, -rx * 90f * Time.unscaledDeltaTime, Space.Self);
            }
        }

        void DragStage()
        {
            if (mode != 1 || turntable == null) { dragging = false; return; }

            if (Input.GetMouseButtonDown(0) && Inside(viewport != null ? viewport.rectTransform : null))
            {
                dragging = true;
                lastMouse = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0)) dragging = false;

            if (dragging)
            {
                var delta = Input.mousePosition - lastMouse;
                lastMouse = Input.mousePosition;
                turntable.Rotate(Vector3.up, -delta.x * 0.45f, Space.Self);
            }
        }

        void Click()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            if (Inside(btnClose)) { Close(); return; }
            if (Inside(btnPrev)) { ShowImage(index - 1); return; }
            if (Inside(btnNext)) { ShowImage(index + 1); return; }
            if (Inside(btnPhoto)) { SetMode(0); return; }
            if (Inside(btnModel)) { SetMode(1); return; }
            if (Inside(btnVoice)) { Speak(); return; }

            if (thumbs != null)
                for (int i = 0; i < thumbs.Length; i++)
                    if (thumbs[i] != null && thumbs[i].gameObject.activeSelf && Inside(thumbs[i].rectTransform))
                    {
                        if (mode != 0) SetMode(0);
                        ShowImage(i);
                        return;
                    }
        }

        static bool Inside(RectTransform rt)
        {
            return rt != null && rt.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
        }
    }
}
