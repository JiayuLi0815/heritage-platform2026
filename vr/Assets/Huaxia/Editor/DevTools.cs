using UnityEditor;
using UnityEngine;

namespace Huaxia.EditorTools
{
    /// <summary>走查用的开关。进度存在 PlayerPrefs 里，不点这个就得去删注册表。</summary>
    public static class DevTools
    {
        [MenuItem("Huaxia/清空探索进度（三卷回到都没去过）")]
        public static void ResetProgress()
        {
            GameState.ResetAll();
            PlayerPrefs.Save();
            Debug.Log("[Huaxia] 进度已清空：三卷都回到「没去过」，藏卷阁的门重新闩上，" +
                      "开场说明也会再弹一次。");
        }
    }
}
