#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Huaxia.EditorTools
{
    /// <summary>
    /// XR Plug-in Management 的工程侧配置。
    ///
    /// 之前工程里装了 OpenXR 包、也有 OpenXRLoader.asset，但没有 XRGeneralSettings
    /// 这个「每个构建目标挂哪些 loader」的资产，所以打包时 OpenXR 根本不会被初始化，
    /// XRInputBridge 永远等不到 XRSettings.isDeviceActive。
    ///
    /// 这里补上这层配置：Standalone 与 Android 各挂一个 OpenXRLoader。
    /// 注意 OpenXR 官方的 Standalone 支持只有 Windows64；Mac 上只能在真机
    /// （PICO 4 Enterprise / Android）上跑，桌面编辑器里不会激活。
    /// </summary>
    public static class XRSetup
    {
        const string LoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
        const string SettingsPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";

        [MenuItem("Huaxia/XR/启用 OpenXR 加载器（PICO 4）")]
        public static void EnableOpenXR()
        {
            var perTarget = LoadOrCreateSettings();

            foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
            {
                if (!perTarget.HasSettingsForBuildTarget(group))
                    perTarget.CreateDefaultSettingsForBuildTarget(group);
                if (!perTarget.HasManagerSettingsForBuildTarget(group))
                    perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

                var manager = perTarget.ManagerSettingsForBuildTarget(group);
                if (manager == null)
                {
                    Debug.LogError("[Huaxia XR] " + group + " 没拿到 XRManagerSettings，跳过。");
                    continue;
                }

                // 默认新建出来的 manager 两项都是关的，等于「loader 挂了但永远不启动」。
                manager.automaticLoading = true;
                manager.automaticRunning = true;

                bool ok = XRPackageMetadataStore.AssignLoader(manager, LoaderType, group);
                Debug.Log("[Huaxia XR] " + group + " OpenXR loader "
                          + (ok ? "已挂上" : "挂载失败")
                          + "，当前 loader 数 " + manager.activeLoaders.Count);
            }

            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        }

        [MenuItem("Huaxia/XR/查看 XR 配置状态")]
        public static void Report()
        {
            XRGeneralSettingsPerBuildTarget perTarget = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget);
            if (perTarget == null)
            {
                Debug.LogWarning("[Huaxia XR] 尚无 XRGeneralSettings —— 运行「Huaxia/XR/启用 OpenXR 加载器」。");
                return;
            }

            foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
            {
                var manager = perTarget.ManagerSettingsForBuildTarget(group);
                int count = manager != null ? manager.activeLoaders.Count : 0;
                Debug.Log("[Huaxia XR] " + group + "："
                          + (count > 0 ? "已配置 " + count + " 个 loader" : "未配置 loader"));
            }
        }

        static XRGeneralSettingsPerBuildTarget LoadOrCreateSettings()
        {
            XRGeneralSettingsPerBuildTarget settings = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out settings);
            if (settings != null) return settings;

            var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (settings == null)
            {
                System.IO.Directory.CreateDirectory("Assets/XR/Settings");
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            return settings;
        }
    }
}
#endif
