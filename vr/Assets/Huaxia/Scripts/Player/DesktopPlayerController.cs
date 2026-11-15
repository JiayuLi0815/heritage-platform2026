using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 桌面端第一人称控制：WASD + 鼠标视角 + 中心射线互动。
    /// 后续接入头显时，替换为 XR Origin，但交互层（Interactable）可复用。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayerController : MonoBehaviour
    {
        public Camera playerCamera;
        public float walkSpeed = 3.0f;
        public float runSpeed = 5.5f;
        public float mouseSensitivity = 2.0f;
        public float interactDistance = 4.5f;
        public float gravity = -18f;
        public float jumpHeight = 1.1f;

        CharacterController controller;
        float pitch;
        float verticalVelocity;
        Interactable focused;
        bool cursorLocked;
        BookHolder book;

        [Header("VR（由 XRInputBridge 写入）")]
        public bool xrActive;
        public Vector2 externalMove;
        public Transform xrRay;
        [Tooltip("右手 A 按住时的步速倍率")]
        public float sprintSpeedScale = 1.2f;
        public bool externalSprint;

#if UNITY_EDITOR
        // 调试用：R 键回到本场景落点。只在编辑器里生效，打出来的包没有。
        Vector3 debugSpawnPos;
        Quaternion debugSpawnRot;
#endif

        void Start()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            book = GetComponentInChildren<BookHolder>();
            ApplyPendingSpawn();
#if UNITY_EDITOR
            debugSpawnPos = transform.position;
            debugSpawnRot = transform.rotation;
#endif
            LockCursor(true);
        }

        /// <summary>上一个场景点的那条路，决定这次从哪儿站出来、脸朝哪边。</summary>
        void ApplyPendingSpawn()
        {
            var spawn = SpawnPoint.Find(GameState.pendingSpawn);
            GameState.pendingSpawn = null;
            if (spawn == null) return;
            spawn.Place(transform);
            verticalVelocity = -2f;
            pitch = 0f;
            if (playerCamera != null) playerCamera.transform.localEulerAngles = Vector3.zero;
        }

        void LockCursor(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            var ui = UIManager.Instance;
            if (ui != null && ui.BlocksPlayer)
            {
                ClearFocus();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(false);
                ClearFocus();
                return;
            }

            if (!cursorLocked)
            {
                if (Input.GetMouseButtonDown(0)) LockCursor(true);
                ClearFocus();
                return;
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.R))
            {
                // R：回本场景的落点；Shift+R：跨场景回序章山门外。
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    GameState.pendingSpawn = "start";
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Archive");
                }
                else
                {
                    controller.enabled = false;
                    transform.SetPositionAndRotation(debugSpawnPos, debugSpawnRot);
                    controller.enabled = true;
                    verticalVelocity = -2f;
                    pitch = 0f;
                    if (playerCamera != null) playerCamera.transform.localEulerAngles = Vector3.zero;
                }
                return;
            }
#endif

            if (!xrActive) Look();
            Move();
            Scan();

            // 书抬在眼前时不吃 E：人在看书，不该顺手把身后那道门按开
            bool reading = book != null && book.Raised;
            if (!reading && focused != null && Input.GetKeyDown(KeyCode.E))
                focused.Interact();
        }

        /// <summary>手柄扳机：对当前聚焦的互动点做一次交互。返回有没有东西可交互。</summary>
        public bool InteractFocused()
        {
            if (UIManager.Instance != null && UIManager.Instance.BlocksPlayer) return false;
            if (book != null && book.Raised) return false;
            if (focused == null) return false;
            focused.Interact();
            return true;
        }

        /// <summary>手柄摇杆：整步转身（不用平滑转，少晕）。</summary>
        public void SnapTurn(float degrees)
        {
            transform.Rotate(Vector3.up * degrees);
        }

        /// <summary>手柄 B 短按：起跳。和桌面端「、」键走同一套竖直速度。</summary>
        public void Jump()
        {
            if (controller == null || !controller.isGrounded) return;
            if (book != null && book.Raised) return;
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
        }

        void Look()
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mx);
            pitch = Mathf.Clamp(pitch - my, -80f, 80f);
            if (playerCamera != null)
                playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        /// <summary>
        /// 人眼睛朝哪。头显的追踪空间未必跟 Player 根节点同向（OpenXR 的正面是 -Z），
        /// 摇杆、方位提示都得认这个方向，不能认 transform.forward。
        /// </summary>
        public Vector3 ViewForward
        {
            get
            {
                Transform t = playerCamera != null ? playerCamera.transform : transform;
                Vector3 f = t.forward;
                f.y = 0f;
                if (f.sqrMagnitude < 1e-4f) f = transform.forward;
                return f.normalized;
            }
        }

        public Vector3 ViewRight { get { return Vector3.Cross(Vector3.up, ViewForward); } }

        void Move()
        {
            // 头显里左右摇杆都能走（XRInputBridge 已合成到 externalMove）。
            float h = xrActive ? externalMove.x : Input.GetAxisRaw("Horizontal");
            float v = xrActive ? externalMove.y : Input.GetAxisRaw("Vertical");

            Vector3 dir;
            if (xrActive)
            {
                // 以视线为基准：往前推就往眼睛看的方向走，跟头显追踪空间怎么摆无关
                dir = ViewRight * h + ViewForward * v;
            }
            else
            {
                dir = transform.right * h + transform.forward * v;
            }
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            float speed = walkSpeed;
            if (!xrActive && Input.GetKey(KeyCode.LeftShift)) speed = runSpeed;
            if (xrActive && externalSprint) speed *= sprintSpeedScale;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

            // 跳跃改到「、」键（Backslash）：空格留给弹窗确认，两件事不再共用一只手。
            // 迈不过去的坎、或者只是想跨一下，都留个手动出口。
            bool reading = book != null && book.Raised;
            if (controller.isGrounded && !xrActive && !reading && Input.GetKeyDown(KeyCode.Backslash))
                verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = dir * speed + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        void Scan()
        {
            var ui = UIManager.Instance;
            if (playerCamera == null || ui == null) return;

            Interactable hitInteractable = null;
            RaycastHit hit;
            // VR 里从手柄往前打，桌面端从眼睛往前打
            Transform rayOrigin = (xrActive && xrRay != null) ? xrRay : playerCamera.transform;
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward,
                                out hit, interactDistance, ~0, QueryTriggerInteraction.Collide))
            {
                hitInteractable = hit.collider.GetComponentInParent<Interactable>();
            }

            focused = hitInteractable;

            if (focused != null)
                ui.ShowPrompt(focused.Prompt + "　" + focused.title);
            else
                ui.HidePrompt();
        }

        void ClearFocus()
        {
            focused = null;
            if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
        }
    }
}
