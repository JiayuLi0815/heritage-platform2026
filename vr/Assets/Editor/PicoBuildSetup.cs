using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

public static class PicoBuildSetup
{
    const string FeatureSetId = "com.picoxr.openxr.features";
    const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";

    [MenuItem("Huaxia/1. Configure Android + PICO")]
    public static void Configure()
    {
        Debug.Log("[PicoSetup] switching build target to Android");
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new Exception("Failed to switch active build target to Android");

        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.huaxia.vr");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_0);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (general == null) throw new Exception("No XR General Settings for Android");
        if (general.Manager == null) throw new Exception("XR Manager is null for Android");
        bool assigned = XRPackageMetadataStore.AssignLoader(general.Manager, OpenXRLoaderType, BuildTargetGroup.Android);
        Debug.Log("[PicoSetup] OpenXR loader assigned = " + assigned);

        var fs = OpenXRFeatureSetManager.GetFeatureSetWithId(BuildTargetGroup.Android, FeatureSetId);
        if (fs == null) throw new Exception("PICO feature set not found - is the PICO OpenXR plugin imported?");
        fs.isEnabled = true;
        OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(BuildTargetGroup.Android);
        Debug.Log("[PicoSetup] PICO feature set enabled");

        AssetDatabase.SaveAssets();

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings != null)
        {
            // Controller interaction profiles must be enabled explicitly, otherwise the headset
            // has no input device and the controllers appear dead in game.
            foreach (var f in settings.GetFeatures<OpenXRFeature>())
            {
                string n = f.GetType().Name;
                if (n.Contains("PICO4UltraControllerProfile") ||
                    n.Contains("PICO4ControllerProfile") ||
                    n.Contains("PICONeo3ControllerProfile") ||
                    n.Contains("PICOG3ControllerProfile"))
                {
                    f.enabled = true;
                    Debug.Log("[PicoSetup] controller profile enabled: " + n);
                }
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            foreach (var f in settings.GetFeatures<OpenXRFeature>())
                Debug.Log($"[PicoSetup] feature {f.GetType().FullName} enabled={f.enabled}");
        }

        Debug.Log("[PicoSetup] CONFIGURE DONE");
    }

    [MenuItem("Huaxia/2. Build APK")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Debug.Log("[PicoSetup] scenes: " + string.Join(", ", scenes));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Huaxia.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log($"[PicoSetup] BUILD result={report.summary.result} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} size={report.summary.totalSize}");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
