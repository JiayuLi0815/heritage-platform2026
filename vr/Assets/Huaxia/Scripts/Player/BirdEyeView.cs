using UnityEngine;

namespace Huaxia
{
    /// <summary>良渚场景的“局部—整体”视点切换（按 V）。</summary>
    public class BirdEyeView : MonoBehaviour
    {
        public Camera playerCamera;
        public Camera aerialCamera;
        public DesktopPlayerController controller;
        public Vector3 aerialPosition = new Vector3(0f, 58f, -20f);
        public Vector3 aerialEuler = new Vector3(48f, 0f, 0f);

        bool active;

        void Update()
        {
            var ui = UIManager.Instance;
            if (ui != null && ui.BlocksPlayer) return;

            if (Input.GetKeyDown(KeyCode.V)) Toggle();
        }

        public void Toggle()
        {
            active = !active;

            if (active)
            {
                if (playerCamera != null) playerCamera.enabled = false;
                if (controller != null) controller.enabled = false;
                if (aerialCamera != null)
                {
                    aerialCamera.transform.position = aerialPosition;
                    aerialCamera.transform.eulerAngles = aerialEuler;
                    aerialCamera.gameObject.SetActive(true);
                }
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HidePrompt();
                    UIManager.Instance.ShowToast("整体视角 · 按 V 返回地面");
                }
            }
            else
            {
                if (aerialCamera != null) aerialCamera.gameObject.SetActive(false);
                if (playerCamera != null) playerCamera.enabled = true;
                if (controller != null) controller.enabled = true;
                if (UIManager.Instance != null) UIManager.Instance.HideToast();
            }
        }
    }
}
