#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EditorAutoBuilder
{
    // Call via: Unity.exe -batchmode -projectPath <path> -executeMethod EditorAutoBuilder.PerformAndroidBuild -logFile <log>
    public static void PerformAndroidBuild()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogError("No enabled scenes in Build Settings. Add at least one scene.");
            EditorApplication.Exit(1);
            return;
        }

        string outDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "Builds/Android");
        if (!System.IO.Directory.Exists(outDir)) System.IO.Directory.CreateDirectory(outDir);
        string apkPath = System.IO.Path.Combine(outDir, "QuestHouseExporter.apk");

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log("Starting Android build to: " + apkPath);
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report == null)
        {
            Debug.LogError("BuildPipeline returned null report");
            EditorApplication.Exit(1);
            return;
        }

        var summary = report.summary;
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + apkPath);
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("Build failed: " + summary.result + " - " + summary.totalErrors + " errors");
            EditorApplication.Exit(1);
        }
    }
}
#endif
