using System.Collections;
using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 把《河山册》挂在玩家身上：低头抬手就能看见这本书。
    /// VR 里它应该挂在左手手柄上；桌面端先挂相机下方，Tab 抬起、滚轮或左右方向键翻页。
    /// 书不是面板——它挡住的视野很小，放着不管也不会打断走路。
    /// </summary>
    public class BookHolder : MonoBehaviour
    {
        [Header("输入")]
        public KeyCode raiseKey = KeyCode.Tab;
        public KeyCode nextPageKey = KeyCode.RightArrow;
        public KeyCode prevPageKey = KeyCode.LeftArrow;

        [Header("抬手位姿（相机本地坐标）")]
        public Vector3 raisedPos = new Vector3(0f, -0.175f, 0.40f);
        public Vector3 raisedEuler = new Vector3(-58f, 0f, 0f);
        // 收在视界下沿：看得见一角，但不占画面
        public Vector3 loweredPos = new Vector3(0f, -0.72f, 0.24f);
        public Vector3 loweredEuler = new Vector3(-84f, 0f, 0f);
        public float raiseSpeed = 6.5f;

        [Header("开场")]
        [Tooltip("进入场景时自动抬一次书，然后落回去——告诉体验者身上有这么一本")]
        public bool greetOnStart = true;

        public HandBook Book { get; private set; }

        [Header("自动姿态")]
        [Tooltip("次线索落笔时抬手的时间：太短看不清，太长像被没收了操作")]
        public float peekSeconds = 2.8f;

        public bool Raised { get; private set; }
        float raiseT;
        float greetTimer;
        Coroutine showRoutine;

        void Awake()
        {
            var prefab = Resources.Load<GameObject>("HandBook");
            if (prefab == null)
            {
                Debug.LogWarning("[Huaxia] 找不到 Resources/HandBook.prefab，书没有生成");
                enabled = false;
                return;
            }

            var go = Instantiate(prefab, transform, false);
            go.name = "HeshanBook";
            go.transform.localPosition = loweredPos;
            go.transform.localEulerAngles = loweredEuler;
            Book = go.GetComponent<HandBook>();
            raiseT = 0f;
            greetTimer = greetOnStart ? 2.6f : -1f;
        }

        void Update()
        {
            if (Book == null) return;

            // 弹窗或过场时不吃输入：空格在翻页、E 在确认，别让书把这些键抢走
            bool acceptingInput = UIManager.Instance == null || !UIManager.Instance.BlocksPlayer;

            if (acceptingInput && Input.GetKeyDown(raiseKey))
            {
                // 玩家自己抬手就接管：别让自动姿态跟他抢
                if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
                Raised = !Raised;
                greetTimer = -1f;
            }

            if (greetTimer > 0f)
            {
                greetTimer -= Time.deltaTime;
                if (greetTimer <= 0f) Raised = false;
                else if (!Raised) Raised = true;
            }

            // 抬起时看书：手会自然把书往前提一点，这里用一个缓动代替
            raiseT = Mathf.MoveTowards(raiseT, Raised ? 1f : 0f, raiseSpeed * Time.deltaTime);
            float e = Mathf.SmoothStep(0f, 1f, raiseT);
            transform.localPosition = Vector3.Lerp(loweredPos, raisedPos, e);
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(loweredEuler), Quaternion.Euler(raisedEuler), e);

            if (!Raised || !acceptingInput) return;

            if (Input.GetKeyDown(nextPageKey)) Book.TurnNext();
            if (Input.GetKeyDown(prevPageKey)) Book.TurnPrev();

            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.1f) Book.TurnNext();
            else if (scroll < -0.1f) Book.TurnPrev();
        }

        // ------------------------------------------------------- 手柄 / VR 调用

        /// <summary>手柄 grip：抬书 / 收书。和 Tab 走同一条路。</summary>
        public void ToggleRaised()
        {
            if (Book == null) return;
            if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
            Raised = !Raised;
            greetTimer = -1f;
        }

        /// <summary>手柄摇杆左右 / 扳机：翻页。没抬书就先把书抬起来。</summary>
        public void TurnPage(int direction)
        {
            if (Book == null) return;
            if (!Raised) ToggleRaised();
            if (direction >= 0) Book.TurnNext();
            else Book.TurnPrev();
        }

        // ------------------------------------------------------- 自动姿态

        /// <summary>
        /// 本卷读完：书自己抬到眼前，看着这一叶从湿到干、字浮出来，然后落回腰间。
        /// 不做「恭喜完成」的弹窗——进度就在这张纸上。
        /// </summary>
        public void Present(string chapterId, float holdSeconds)
        {
            if (Book == null) return;
            ShowRoutine(PresentRoutine(chapterId, holdSeconds));
        }

        /// <summary>页边落笔：抬手看一眼纸边多出来的那行字，再落回去。</summary>
        public void Peek(float seconds)
        {
            if (Book == null || Raised) return;
            ShowRoutine(PeekRoutine(seconds));
        }

        void ShowRoutine(IEnumerator routine)
        {
            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(routine);
        }

        IEnumerator PresentRoutine(string chapterId, float holdSeconds)
        {
            greetTimer = -1f;
            Raised = true;
            yield return new WaitForSeconds(0.55f);
            Book.FinishChapter(chapterId);
            yield return new WaitForSeconds(holdSeconds);
            Raised = false;
            showRoutine = null;
        }

        IEnumerator PeekRoutine(float seconds)
        {
            greetTimer = -1f;
            Raised = true;
            yield return new WaitForSeconds(seconds);
            Raised = false;
            showRoutine = null;
        }
    }
}
