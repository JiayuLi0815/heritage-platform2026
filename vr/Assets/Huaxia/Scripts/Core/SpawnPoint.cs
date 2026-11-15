using UnityEngine;

namespace Huaxia
{
    /// <summary>
    /// 场景里的一个落点。
    /// 换场景之前把要去的 id 写进 GameState.pendingSpawn，
    /// 新场景的玩家一出生就找它站好、朝它的正前方看出去——
    /// 这样从哪条岔路走的，回来就还站在那条岔路口。
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        public string id = "default";

        public static SpawnPoint Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var all = FindObjectsOfType<SpawnPoint>();
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].id == id) return all[i];
            return null;
        }

        public void Place(Transform player)
        {
            var cc = player.GetComponent<CharacterController>();
            bool was = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            player.position = transform.position;
            player.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (cc != null) cc.enabled = was;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.86f, 0.70f, 0.44f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.3f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.9f, transform.forward * 1.5f);
        }
#endif
    }
}
