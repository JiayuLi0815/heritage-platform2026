using System.IO;
using UnityEditor.Android;

// PICO's launcher refuses to start a VR activity that does not declare hand-tracking
// support while the headset is in hand mode. PICO's own SDK writes this meta-data when
// its hand-tracking feature is on, so declaring it is the supported way to let the app launch.
public class AndroidManifestTweak : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath)) return;

        string text = File.ReadAllText(manifestPath);
        if (text.Contains("name=\"handtracking\"")) return;

        text = text.Replace(
            "</application>",
            "    <meta-data android:name=\"handtracking\" android:value=\"1\" />\n  </application>");
        File.WriteAllText(manifestPath, text);
        UnityEngine.Debug.Log("[PicoSetup] injected handtracking meta-data into " + manifestPath);
    }
}
