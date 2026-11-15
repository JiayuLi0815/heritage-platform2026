using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 数智讲解员「弘文」。站在三块立牌前的广场中央，一进序章先不开口 ——
    /// 等玩家踩进某只引导圈（GuideRing），她才走过去、招手、讲这一处；
    /// 走近立牌打开面板时，面板里那个会动的头像也是她。
    ///
    /// 场景里的是带骨骼的自制模型（Resources/Heritage/DigitalHuman/Hongwen），
    /// 十段 Legacy 动画：walk / greet_01 / greet_02 / agree / wait / look_around /
    /// fold_arms / clap / cheer / dig。她一个人把整条动线串起来：
    /// 踩圈 → 走过来讲这一处 → 待机轮播 → 听你读立牌 → 给你指路 → 三卷读完庆祝。
    /// </summary>
    public class DigitalHuman : MonoBehaviour
    {
        public static DigitalHuman Instance;

        [Header("外观")]
        public Transform visual;          // 有骨骼动画时不用抖，留着给旧的无骨骼模型
        public float bobAmplitude;
        public float bobSpeed = 1.1f;

        [Header("朝向")]
        public Transform body;            // 只绕 Y 轴转，脚不跟着动
        public Transform head;
        public float faceRadius = 11f;
        public float turnSpeed = 2.2f;
        [Tooltip("最多转多少度去看玩家，别让她把后背留给场景")]
        public float turnLimit = 68f;

        [Header("动作剪辑")]
        public Animation anim;
        public string restClip = "wait";
        [Tooltip("没人说话的时候轮着播，轮播才不木")]
        public string[] idleClips = { "wait", "look_around", "fold_arms" };
        [Tooltip("讲话时换着做手势")]
        public string[] talkClips = { "agree", "wait", "look_around", "fold_arms" };
        public string greetWaveClip = "greet_01";
        public string waveClip = "greet_02";
        public string walkClip = "walk";
        public string listenClip = "agree";
        public string celebrateClip = "cheer";
        [Tooltip("这些剪辑首尾相接，播完要继续")]
        // walk 必须在这里：她走长路时动画要一直循环，漏了就会播一遍定格、人在地上滑
        public string[] loopClips = { "wait", "look_around", "fold_arms", "walk", "clap", "cheer", "dig" };

        [Header("语音")]
        [Tooltip("玩家踩进引导圈后她走到圈外侧，最多走这么久")]
        public float walkSeconds = 3.2f;
        public float walkSpeed = 1.15f;
        [Tooltip("玩家在十几米外踩圈时她得赶过去：超过 hurryFrom 就换成这个速度")]
        public float hurrySpeed = 2.1f;
        public float hurryFrom = 5f;
        [Tooltip("迎过去最多走这么久（秒）；乘 speed 就是她最远肯跑多远")]
        public float reachSeconds = 20f;

        [Header("开场带路")]
        [Tooltip("开场白讲完她把玩家领到这儿站着（由建场景时算好地面高度）；留零就不带路")]
        public Vector3 parkSpot;

        [Tooltip("开场白念到第几秒开始边走边说")]
        public float introWalkAt = 12f;

        [Tooltip("带路时的步速：玩家走 3 米/秒，这里太慢会被甩开")]
        public float leadSpeed = 1.2f;

        [Tooltip("送到的立牌（引导圈 id）；空着就在 Start 时按 id 找那只圈")]
        public string boardRingId = "1592";
        Vector3 boardSpot;                     // 立牌前的落脚点，Start 时从引导圈算出来

        [Header("字幕")]
        public float subtitleLead = 2f;

        const string CheerLine = "三卷都读完了。五千年的城、两千年的军队、一千年的港口，都在你手里这本册子里了。";

        /// <summary>三卷里读过哪几卷，读完三卷她会庆祝一次。</summary>
        static readonly HashSet<string> Viewed = new HashSet<string>();

        /// <summary>哪几处已经指过路：同一处不来回念两遍（踩圈一次、合面板一次）。</summary>
        static readonly HashSet<string> Guided = new HashSet<string>();
        static bool celebrated;

        AudioSource voice;
        Vector3 visualHome;
        float bobPhase;
        float homeYaw;

        string narrating;              // 正在讲哪一处（引导圈 id），防止同一个圈连播
        bool introDone;                // 开场白只讲一次
        bool guiding;                  // 正在指路：这时候别让圈把她的话打断
        Coroutine cue;                 // 当前的动作编排
        string playing = "";
        float faceHold;                // 指路时强制朝向，倒计时归零后交还给玩家

        // 走路剪辑可能自带根位移：动画把人推出去，名牌却挂在根节点上，就会一前一后。
        // 走的时候把这个节点的水平位移按住，只留上下起伏。
        Transform animRoot;
        Vector3 animRootHome;
        Transform pelvis;
        Transform labelPlate;
        float nextWalkLog;
        Vector3 faceDir;

        void Awake()
        {
            Instance = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0.35f;
            voice.volume = 0.95f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 2.5f;
            voice.maxDistance = 22f;
            if (visual != null) visualHome = visual.localPosition;
            if (body != null) homeYaw = body.localEulerAngles.y;
            bobPhase = Random.value * 6.28f;
        }

        void Start()
        {
            // 默认按渲染器可见性剔除动画，VR 里数字人常常不在视野内（截图/走查更是一帧都不渲染），
            // 会导致动画被裁掉、时间冻结、CrossFade 永远切不过去。她是要开口讲话的主角，一直算。
            if (anim != null) anim.cullingType = AnimationCullingType.AlwaysAnimate;
            if (introWalkAt <= 0f) introWalkAt = 12f;
            if (leadSpeed <= 0f) leadSpeed = 1.2f;
            TryFindBoardSpot();
            CacheWalkRig();
            Play(restClip, 0f);
            Cue(IdleCycle());
        }

        /// <summary>找走路动画的根节点、骨盆和头顶名牌，走的时候好把位移对回来。</summary>
        void CacheWalkRig()
        {
            if (anim == null) return;
            Transform model = anim.transform;
            animRoot = FindDeep(model, "Armature");
            if (animRoot == null && model.childCount > 0) animRoot = model.GetChild(0);
            if (animRoot != null) animRootHome = animRoot.localPosition;
            pelvis = FindDeep(model, "Pelvis");

            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                if (c.name.StartsWith("Label_")) labelPlate = c;
            }

            Debug.Log("[Huaxia] 弘文骨架 model=" + model.name +
                      " animRoot=" + (animRoot != null ? animRoot.name : "无") +
                      " pelvis=" + (pelvis != null ? pelvis.name : "无") +
                      " label=" + (labelPlate != null ? labelPlate.name : "无"));
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindDeep(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>
        /// 动画在 LateUpdate 之前更新：这里把根节点的水平位移按回去，
        /// 人就不会一边走一边被动画推离名牌。
        /// </summary>
        void LateUpdate()
        {
            if (!guiding || animRoot == null) return;
            Vector3 p = animRoot.localPosition;
            p.x = animRootHome.x;
            p.z = animRootHome.z;
            animRoot.localPosition = p;
        }

        /// <summary>
        /// 良渚立牌前的落脚点：圈是「站到这里」的位置，人站在圈外侧一点，不挡牌面。
        /// 由引导圈自己算，牌挪了位置也不会失准。
        /// </summary>
        bool TryFindBoardSpot()
        {
            if (boardSpot != Vector3.zero) return true;
            var rings = FindObjectsOfType<GuideRing>();
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null || rings[i].heritageId != boardRingId) continue;
                boardSpot = rings[i].StandPoint();
                return true;
            }
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (visual != null && bobAmplitude > 0f)
            {
                bobPhase += dt * bobSpeed;
                visual.localPosition = visualHome + Vector3.up * (Mathf.Sin(bobPhase) * bobAmplitude);
            }

            if (body == null) return;

            if (faceHold > 0f)
            {
                faceHold -= dt;
                TurnTowards(faceDir, dt);
                return;
            }

            // 站着的活人不会钉在一个朝向上：玩家走近就把上半身转过去看着
            float target = homeYaw;
            var player = LookTarget();
            if (player != null)
            {
                Vector3 to = player.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < faceRadius * faceRadius && to.sqrMagnitude > 0.05f)
                {
                    // 模型面朝 -Z，所以 +Z 要指向玩家的反方向
                    float want = Mathf.Atan2(-to.x, -to.z) * Mathf.Rad2Deg;
                    float local = Mathf.DeltaAngle(homeYaw, want - transform.eulerAngles.y);
                    target = homeYaw + Mathf.Clamp(local, -turnLimit, turnLimit);
                }
            }
            float yaw = Mathf.LerpAngle(body.localEulerAngles.y, target, 1f - Mathf.Exp(-turnSpeed * dt));
            body.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void TurnTowards(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            float want = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg - transform.eulerAngles.y;
            float yaw = Mathf.LerpAngle(body.localEulerAngles.y, want, 1f - Mathf.Exp(-turnSpeed * dt));
            body.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        static Transform LookTarget()
        {
            var pc = FindObjectOfType<DesktopPlayerController>();
            if (pc != null) return pc.transform;
            var cam = Camera.main;
            return cam != null ? cam.transform : null;
        }

        // ================================================================ 剪辑播放

        public void Play(string name, float fade = 0.3f)
        {
            if (anim == null || string.IsNullOrEmpty(name)) return;
            var state = anim[name];
            if (state == null) return;
            if (playing == name && state.enabled) return;
            state.wrapMode = Loops(name) ? WrapMode.Loop : WrapMode.ClampForever;
            state.blendMode = AnimationBlendMode.Blend;
            state.speed = 1f;
            anim.CrossFade(name, Mathf.Max(0f, fade), PlayMode.StopSameLayer);
            playing = name;
        }

        bool Loops(string name)
        {
            if (loopClips == null) return false;
            for (int i = 0; i < loopClips.Length; i++)
                if (loopClips[i] == name) return true;
            return false;
        }

        public float ClipLength(string name)
        {
            if (anim == null || string.IsNullOrEmpty(name)) return 0f;
            var clip = anim.GetClip(name);
            return clip != null ? clip.length : 0f;
        }

        string Pick(string[] pool)
        {
            if (pool == null || pool.Length == 0) return null;
            if (pool.Length == 1) return pool[0];
            for (int i = 0; i < 6; i++)
            {
                string c = pool[Random.Range(0, pool.Length)];
                if (c != playing && anim != null && anim[c] != null) return c;
            }
            return pool[Random.Range(0, pool.Length)];
        }

        void Cue(IEnumerator routine)
        {
            if (cue != null) StopCoroutine(cue);
            cue = StartCoroutine(routine);
        }

        // ================================================================ 编排

        IEnumerator IdleCycle()
        {
            yield return new WaitForSeconds(Random.Range(1.2f, 2.6f));
            while (true)
            {
                string clip = Pick(idleClips);
                if (clip == null) yield break;
                Play(clip);
                yield return new WaitForSeconds(Mathf.Max(2f, ClipLength(clip) * Random.Range(0.75f, 1.0f)));
            }
        }

        /// <summary>讲话时换着手势，别一段录音配一个动作定住。</summary>
        IEnumerator TalkLoop(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                string clip = Pick(talkClips);
                Play(clip, 0.45f);
                float slot = Mathf.Min(Mathf.Max(2.6f, ClipLength(clip) * 0.8f), Random.Range(3.4f, 5.2f));
                yield return new WaitForSeconds(slot);
                t += slot;
            }
        }

        IEnumerator WaveRoutine(string clip, float maxSeconds = 0f)
        {
            if (string.IsNullOrEmpty(clip)) yield break;
            Play(clip, 0.2f);
            float len = Mathf.Max(0.6f, ClipLength(clip));
            if (maxSeconds > 0f) len = Mathf.Min(len, maxSeconds);
            yield return new WaitForSeconds(len);
        }

        /// <summary>玩家踩进某个引导圈：走过去、招手、开口讲这一处。</summary>
        IEnumerator EncounterRoutine(string siteId, Vector3 ringPos, Transform board, bool stayInPlace)
        {
            narrating = siteId;
            guiding = false;
            StopVoice();                       // 上一处念到一半也算翻篇
            // 序章开场她本来就站在《河山册》牌前，玩家走过来时她原地转身即可：
            // 让她从别处趟过来，等于把「牌前有人」这件事又抹掉了。
            if (stayInPlace) FaceFor(600f);
            else yield return WalkToRing(ringPos, board);
            yield return WaveRoutine(waveClip, 1.6f);

            FaceFor(600f);                     // 讲的时候对着玩家，别对着牌面
            var clip = Resources.Load<AudioClip>("Heritage/Audio/" + RingVoice(siteId));
            if (clip != null) Speak(clip, RingLine(siteId));

            if (siteId == "intro")
            {
                // 开场白不再站着讲完：先站定讲十来秒，再边走边说，
                // 最后几句刚好在良渚立牌前收住（见 IntroLead）。
                yield return IntroLead(clip);
            }
            else
            {
                yield return TalkLoop(clip != null ? clip.length : 3f);
                yield return WaveRoutine(waveClip, 1.4f);

                // 讲完了不留在原地：立牌之后沿这条路再往前带几步
                if (board != null) yield return LeadForward(siteId);
            }

            narrating = null;
            faceHold = 0f;
            Cue(IdleCycle());
        }

        /// <summary>
        /// 序章开场白：先站定讲到第 introWalkAt 秒，然后一边讲一边往良渚立牌走。
        /// 剩下的语音在牌前讲完，这一段全程算「带路中」——玩家提前踩进立牌的圈，
        /// 引导圈会等他，不会把她的话截断。
        /// </summary>
        IEnumerator IntroLead(AudioClip clip)
        {
            float stand = Mathf.Clamp(introWalkAt, 0f, clip != null ? clip.length : 0f);
            if (stand > 0f) yield return TalkLoop(stand);
            yield return LeadToBoard();
        }

        /// <summary>
        /// 从《河山册》牌前走到良渚立牌：先并到三岔口，再拐上左手那条路。
        /// 两段都是直线——这一段是广场石墁和夯土道，没有要绕的树和栏杆。
        /// </summary>
        IEnumerator LeadToBoard()
        {
            if (!TryFindBoardSpot()) yield break;
            if (string.IsNullOrEmpty(walkClip)) { while (IsSpeaking) yield return null; yield break; }

            guiding = true;
            Vector3 start = transform.position;
            yield return WalkLeg(new Vector3(parkSpot.x, start.y, parkSpot.z));
            yield return WalkLeg(new Vector3(boardSpot.x, start.y, boardSpot.z));

            // 到了牌前：转身对着玩家，招手；剩下的话就在这儿讲完
            FaceFor(600f);
            yield return WaveRoutine(waveClip, 1.2f);
            while (IsSpeaking) yield return null;

            guiding = false;
            faceHold = 0f;
        }

        /// <summary>走一段直线。玩家落在十二米外就停下等一会儿，别一个人走远。</summary>
        IEnumerator WalkLeg(Vector3 dest)
        {
            Vector3 from = transform.position;
            Vector3 to = dest - from;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.15f) yield break;

            Vector3 step = to / dist;
            float speed = Mathf.Max(0.4f, leadSpeed);
            faceDir = step;
            faceHold = 600f;               // 走的时候朝前，不回头找玩家
            Play(walkClip, 0.25f);
            WalkRate(1f);

            float travelled = 0f;
            while (travelled < dist)
            {
                var player = LookTarget();
                if (player != null && Vector3.Distance(player.position, transform.position) > 12f)
                {
                    Play(restClip, 0.35f);
                    yield return new WaitForSeconds(0.5f);
                    Play(walkClip, 0.25f);
                    WalkRate(1f);
                    continue;
                }

                travelled = Mathf.Min(dist, travelled + speed * Time.deltaTime);
                Vector3 p = from + step * travelled;
                p.y = from.y;
                transform.position = p;

                if (Time.time >= nextWalkLog)
                {
                    nextWalkLog = Time.time + 1f;
                    Vector3 ar = animRoot != null ? animRoot.localPosition - animRootHome : Vector3.zero;
                    Vector3 pv = pelvis != null ? pelvis.position - transform.position : Vector3.zero;
                    Vector3 lb = labelPlate != null ? labelPlate.position - transform.position : Vector3.zero;
                    Debug.Log("[Huaxia] 走 root=(" + p.x.ToString("F1") + "," + p.z.ToString("F1") +
                              ") animRoot=(" + ar.x.ToString("F2") + "," + ar.z.ToString("F2") +
                              ") pelvis=(" + pv.x.ToString("F2") + "," + pv.z.ToString("F2") +
                              ") label=(" + lb.x.ToString("F2") + "," + lb.z.ToString("F2") + ")");
                }

                yield return null;
            }
        }

        /// <summary>
        /// 立牌讲完之后的那一小段带路：先并到这条路的轴线上，再往前挪四五米，
        /// 站到路侧回头招手，顺手把「这条路一直走到界门、按 E」指一遍。
        /// 已经指过路的那一处只做动作，不再重复念。
        /// </summary>
        IEnumerator LeadForward(string siteId)
        {
            Vector3 dir;
            if (!RouteDirection(siteId, out dir)) yield break;
            if (string.IsNullOrEmpty(walkClip)) yield break;

            // 立牌在路的外侧肩上，直接顺着 dir 走会斜着切进花木里：先把横向偏摆收掉
            Vector3 side = new Vector3(dir.z, 0f, -dir.x);
            float lateral = Vector3.Dot(transform.position, side);
            Vector3 from = transform.position;
            Vector3 dest = transform.position + dir * 4.6f - side * (lateral - 1.35f);
            dest.y = from.y;

            Vector3 to = dest - from;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.9f) yield break;

            guiding = true;
            Vector3 step = to / dist;
            float walk = Mathf.Max(0.4f, walkSpeed);
            float limit = Mathf.Min(dist - 0.15f, walk * Mathf.Max(0.4f, walkSeconds) * 1.35f);

            Play(walkClip, 0.2f);
            faceDir = step;
            faceHold = limit / walk + 0.35f;
            float travelled = 0f;
            while (travelled < limit)
            {
                travelled = Mathf.Min(limit, travelled + walk * Time.deltaTime);
                transform.position = from + step * travelled;
                yield return null;
            }

            FaceFor(600f);
            yield return WaveRoutine(waveClip, 1.8f);

            if (Guided.Add(siteId))
            {
                var clip = Resources.Load<AudioClip>("Heritage/Audio/guide_" + siteId);
                if (clip != null) Speak(clip, GuideLine(siteId));
                yield return TalkLoop(clip != null ? clip.length : 0f);
            }

            guiding = false;
            faceHold = 0f;
        }

        /// <summary>真的挪动她：播 walk（原地循环）的同时按方向匀速走过去。</summary>
        IEnumerator WalkToRing(Vector3 ringPos, Transform board)
        {
            Vector3 from = transform.position;
            Vector3 dest;

            if (board != null)
            {
                // ringPos 已经是圈靠玩家那一侧的落脚点：站过去，把牌面整块让给玩家看
                dest = ringPos;
            }
            else
            {
                // 圈画在她脚下：迎上去，停在玩家一米六以外
                var player = LookTarget();
                Vector3 toPlayer = player != null ? player.position - from : Vector3.zero;
                toPlayer.y = 0f;
                float gap = toPlayer.magnitude - 1.6f;
                dest = gap > 0.1f ? from + toPlayer.normalized * gap : from;
            }
            dest.y = from.y;

            Vector3 to = dest - from;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.8f || string.IsNullOrEmpty(walkClip))
            {
                FaceFor(2.4f);                 // 已经站到位了，转身对着玩家就好
                yield break;
            }

            Vector3 step = to / dist;
            float walk = Mathf.Max(0.4f, walkSpeed);
            // 玩家在广场另一头踩圈时她离着十几二十米：照原速挪过去玩家要干等，
            // 远就换成快步，剪辑也跟着提速，不然脚下像在冰上滑。
            float speed = dist > Mathf.Max(1f, hurryFrom) ? Mathf.Max(walk, hurrySpeed) : walk;
            float limit = Mathf.Min(dist - 0.2f, speed * Mathf.Max(0.4f, reachSeconds));
            Play(walkClip, 0.2f);
            WalkRate(speed / walk);
            faceDir = step;
            faceHold = limit / speed + 0.35f;

            float travelled = 0f;
            while (travelled < limit)
            {
                travelled = Mathf.Min(limit, travelled + speed * Time.deltaTime);
                transform.position = from + step * travelled;
                yield return null;
            }
            WalkRate(1f);
        }

        /// <summary>走多快，剪辑就播多快：脚步跟得上位移，才不像在地面上滑。</summary>
        void WalkRate(float rate)
        {
            if (anim == null || string.IsNullOrEmpty(walkClip)) return;
            var state = anim[walkClip];
            if (state != null) state.speed = Mathf.Clamp(rate, 0.6f, 2.4f);
        }

        /// <summary>
        /// 开场白讲完的那段带路：把玩家从山门里领到三岔口南头，边走边说「跟我来」，
        /// 到了站定、招手，把三块牌分别通哪儿交代一句。玩家没跟上就停下来等，
        /// 等太久（四十秒）也不再耗着，自己先站过去。
        /// </summary>
        IEnumerator LeadToFork()
        {
            if (string.IsNullOrEmpty(walkClip)) yield break;
            Vector3 from = transform.position;
            Vector3 to = new Vector3(parkSpot.x - from.x, 0f, parkSpot.z - from.z);
            float dist = to.magnitude;
            if (dist < 2f) yield break;

            guiding = true;
            Vector3 step = to / dist;
            float walk = Mathf.Max(0.4f, walkSpeed);

            Play(walkClip, 0.25f);
            WalkRate(1f);
            faceDir = step;
            faceHold = 0.5f;

            if (Guided.Add("intro"))
            {
                var clip = Resources.Load<AudioClip>("Heritage/Audio/guide_intro");
                if (clip != null) Speak(clip, GuideLine("intro"));
            }

            float travelled = 0f;
            float clock = 0f;
            bool waiting = false;
            while (travelled < dist - 0.05f && clock < 40f)
            {
                float dt = Time.deltaTime;
                clock += dt;

                // 玩家落在八九米外就别一个人往前冲了，站着等一会儿
                var player = LookTarget();
                float behind = player != null
                    ? Vector3.Distance(player.position, from + step * travelled) : 0f;
                if (travelled > 1.5f && player != null && behind > 9f)
                {
                    if (!waiting) { Play(restClip, 0.35f); waiting = true; }
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                if (waiting) { Play(walkClip, 0.3f); WalkRate(1f); waiting = false; }

                travelled = Mathf.Min(dist, travelled + walk * dt);
                transform.position = from + step * travelled;
                yield return null;
            }

            FaceFor(600f);
            yield return WaveRoutine(waveClip, 1.6f);
            guiding = false;
        }

        /// <summary>
        /// 站定版的开场带路：她不出画，只转身朝三岔口抬手，把三块牌子各通哪儿说一遍。
        /// 玩家顺着她的手臂望过去就能看见岔口，不用她领着走。
        /// </summary>
        IEnumerator PointToFork()
        {
            Vector3 dir = parkSpot - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.25f)
            {
                faceDir = dir.normalized;
                faceHold = 600f;
            }

            guiding = true;
            Play(waveClip, 0.25f);
            yield return new WaitForSeconds(Mathf.Min(1.6f, ClipLength(waveClip)));

            if (Guided.Add("intro"))
            {
                var clip = Resources.Load<AudioClip>("Heritage/Audio/guide_intro");
                if (clip != null) Speak(clip, GuideLine("intro"));
                yield return TalkLoop(clip != null ? clip.length : 0f);
            }

            guiding = false;
            faceHold = 0f;
            // 这里不要 Cue(IdleCycle())：Cue 会停掉正在等本协程的 EncounterRoutine，
            // 后面那句 narrating = null 就永远执行不到，整段讲解会被当成一直在讲。
            // 回到 EncounterRoutine 后它会统一清状态并切待机。
        }

        void FaceFor(float seconds)
        {
            var player = LookTarget();
            if (player == null) return;
            faceDir = player.position - transform.position;
            faceHold = seconds;
        }

        /// <summary>引导圈 → 语音包的名字：总说明牌用开场白，三块立牌各有一条到点招呼。</summary>
        public static string RingVoice(string siteId)
        {
            return siteId == "intro" ? "intro" : "arrive_" + siteId;
        }

        static string RingLine(string siteId)
        {
            switch (siteId)
            {
                case "intro":
                    return "弘文：您好，我是您的智能讲解员弘文，欢迎来到华夏文明探源·沉浸式体验空间。" +
                           "这是一场关于中华文明源头的行走式展览，展厅就在山谷里。" +
                           "三条岔路，通向三处世界遗产——浙江余杭的良渚古城，陕西临潼的秦始皇陵，福建泉州的宋元海港。" +
                           "水、土、海，三种把人群聚在一起的力量。" +
                           "每一处，都有据可查的年代、面积和出土器物；也有还没说清的地方，我会替你把两者分开讲。" +
                           "走到立牌跟前按 E，我就给你细说。" +
                           "请跟随我的指引，翻开《河山册》，开启这段穿越千年的探索旅程吧！";
                case "1592":
                    return "弘文：良渚古城遗址，在浙江余杭，距今五千年。内城三百公顷，城外一圈水坝，是中国最早的大型水利系统。" +
                           "这块牌上写的是水与玉；按 E，我带你细看。";
                case "441":
                    return "弘文：秦始皇陵，在陕西临潼。陵园修了三十九年，动了七十多万人，封土底下至今没有打开。" +
                           "这块牌上写的是帝与陵；按 E，我带你细看。";
                case "1561":
                    return "弘文：泉州，宋元时候叫刺桐，是当时世界上最大的港口之一，一共二十二处遗产点。" +
                           "这块牌上写的是港与海；按 E，我带你细看。";
                default:
                    return "";
            }
        }

        IEnumerator GuideRoutine(string siteId)
        {
            guiding = true;
            // 合上册页就算读过这一卷；三卷都读完，就不用再指路了
            Viewed.Add(siteId);
            if (!celebrated && Viewed.Count >= 3)
            {
                celebrated = true;
                yield return CelebrateRoutine();
                guiding = false;
                yield break;
            }

            Vector3 dir;
            if (RouteDirection(siteId, out dir))
            {
                faceDir = dir;
                faceHold = 6f;
            }

            // 原地起步两下，再抬手示意，然后才开口
            if (!string.IsNullOrEmpty(walkClip))
            {
                Play(walkClip, 0.2f);
                yield return new WaitForSeconds(Mathf.Min(1.1f, ClipLength(walkClip)));
            }
            // greet_02 有 5.6 秒，全播完再开口玩家会以为卡住了，示意一下就走
            yield return WaveRoutine(waveClip, 2f);

            // 这一处的路线已经讲过（刚才带着走过一段）就不再念第二遍，只做手势
            if (Guided.Add(siteId))
            {
                var clip = Resources.Load<AudioClip>("Heritage/Audio/guide_" + siteId);
                if (clip != null) Speak(clip, GuideLine(siteId));
                yield return TalkLoop(clip != null ? clip.length : 0f);
            }
            yield return new WaitForSeconds(0.4f);
            guiding = false;
            narrating = null;
            Cue(IdleCycle());
        }

        IEnumerator ListenRoutine()
        {
            if (!string.IsNullOrEmpty(listenClip))
            {
                Play(listenClip, 0.3f);
                yield return new WaitForSeconds(Mathf.Min(2.4f, ClipLength(listenClip)));
            }
            Play("fold_arms", 0.4f);
            // 抱着手臂听完，别再自己换动作
            while (true) yield return null;
        }

        IEnumerator CelebrateRoutine()
        {
            Play(celebrateClip, 0.25f);
            // 欢呼动作有 12 秒，等两秒半就够了，让她边说边比划
            yield return new WaitForSeconds(2.5f);

            var clip = Resources.Load<AudioClip>("Heritage/Audio/cheer");
            if (clip != null) Speak(clip, CheerLine);
            yield return TalkLoop(clip != null ? clip.length : 0f);

            Play("clap", 0.4f);
            yield return new WaitForSeconds(4f);
            Cue(IdleCycle());
        }

        // ================================================================ 对外接口

        /// <summary>
        /// 玩家踩进了某只引导圈：走过去、招手、开口讲这一处。
        /// stayInPlace 为真时她原地不动（序章开场她本来就站在牌前，不该再走过来）。
        /// 返回 false 表示这会儿接不了（正在带路 / 已经讲过），圈别记「踩过」，等下一帧再来问。
        /// </summary>
        public bool Encounter(string siteId, Vector3 ringPos, Transform board, bool stayInPlace = false)
        {
            if (string.IsNullOrEmpty(siteId)) return false;
            if (guiding) return false;               // 正在指路，别插话
            // 开场白只讲一次：她把人领到三岔口之后，玩家来回走不重念
            if (siteId == "intro" && introDone) return false;
            if (narrating == siteId && IsSpeaking) return false;
            if (siteId == "intro") introDone = true;
            Cue(EncounterRoutine(siteId, ringPos, board, stayInPlace));
            return true;
        }

        /// <summary>面板打开：她安静下来，抱着手臂听玩家读。</summary>
        public void Attend()
        {
            StopVoice();
            narrating = null;
            guiding = false;
            Cue(ListenRoutine());
        }

        /// <summary>面板关掉：转身朝那条岔路，抬手示意，把路怎么走说清楚。</summary>
        public void GuideTo(string siteId)
        {
            if (string.IsNullOrEmpty(siteId)) return;
            Cue(GuideRoutine(siteId));
        }

        /// <summary>
        /// 现场点位读完：不指岔路，只朝下一处抬手示意「往前走」。
        /// 点位没有单独的语音包（内容都在面板里念过了），这里只做动作 + 字幕。
        /// </summary>
        public void Beckon(Vector3 target)
        {
            Cue(BeckonRoutine(target));
        }

        IEnumerator BeckonRoutine(Vector3 target)
        {
            guiding = true;
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.04f)
            {
                faceDir = dir;
                faceHold = 6f;
            }
            if (!string.IsNullOrEmpty(walkClip))
            {
                Play(walkClip, 0.2f);
                yield return new WaitForSeconds(Mathf.Min(0.9f, ClipLength(walkClip)));
            }
            yield return WaveRoutine(waveClip, 1.6f);
            guiding = false;
            Cue(IdleCycle());
        }

        /// <summary>按遗产 id 找那条岔路的方向（用路口界门的位置算，不写死角度）。</summary>
        public bool RouteDirection(string siteId, out Vector3 dir)
        {
            dir = Vector3.zero;
            string scene = SceneOf(siteId);
            if (string.IsNullOrEmpty(scene)) return false;
            var portals = FindObjectsOfType<ScenePortal>();
            for (int i = 0; i < portals.Length; i++)
            {
                if (portals[i].sceneToLoad != scene) continue;
                Vector3 to = portals[i].transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.25f) continue;
                dir = to.normalized;
                return true;
            }
            return false;
        }

        static string SceneOf(string siteId)
        {
            switch (siteId)
            {
                case "1592": return "Liangzhu";
                case "441": return "Qinling";
                case "1561": return "Quanzhou";
                default: return null;
            }
        }

        static string GuideLine(string siteId)
        {
            switch (siteId)
            {
                case "intro":
                    return "顺着这条道往前走，到三岔口就停下。三块牌子一处一块：" +
                           "左手那条通良渚，中间穿过牌坊通秦陵，右手那条通泉州。" +
                           "想看哪一处，走到牌子跟前站一下，我再给你细说。";
                case "1592":
                    return "良渚在最左手那条路。走到头有一道界门，站到门前按 E，我陪你下去。";
                case "441":
                    return "秦陵在正前方。穿过牌坊一直往前，路的尽头就是界门，按 E。";
                case "1561":
                    return "泉州在右手那条路。沿路走到界门跟前，按 E 过去。";
                default:
                    return "那条路就在前面，走到尽头按 E。";
            }
        }

        /// <summary>开口。字幕走 HUD 底部那条提示带，时长按音频长度算。</summary>
        public void Speak(AudioClip clip, string line)
        {
            if (clip == null) return;
            StopVoice();
            voice.clip = clip;
            voice.Play();
            StartCoroutine(SubtitleRoutine(line, clip.length));
        }

        IEnumerator SubtitleRoutine(string line, float seconds)
        {
            if (string.IsNullOrEmpty(line)) yield break;
            var ui = UIManager.Instance;
            if (ui == null) yield break;
            ui.ShowToast(line, Mathf.Max(1.5f, seconds + subtitleLead));
            yield return new WaitForSeconds(seconds);
        }

        public void StopVoice()
        {
            if (voice != null && voice.isPlaying) voice.Stop();
        }

        public bool IsSpeaking { get { return voice != null && voice.isPlaying; } }

        /// <summary>面板打开时把她刚才那句掐掉，免得两路声音打架。</summary>
        public static void Silence()
        {
            if (Instance != null) Instance.StopVoice();
        }
    }
}
