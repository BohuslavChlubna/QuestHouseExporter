using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ADBTools
{
    const string outputDir = "Builds/Android";
    const string apkName = "QuestHouseDesign.apk";

    [MenuItem("Tools/QuestHouseDesign/ADB/Build APK")]
    public static void BuildApk()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabled = new System.Collections.Generic.List<string>();
        foreach (var s in scenes) if (s.enabled) enabled.Add(s.path);
        if (enabled.Count == 0)
        {
            UnityEngine.Debug.LogError("No enabled scenes in Build Settings.");
            return;
        }

        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        string apkPath = Path.Combine(outputDir, apkName);

        var opts = new BuildPlayerOptions
        {
            scenes = enabled.ToArray(),
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        UnityEngine.Debug.Log("Starting build to: " + apkPath);
        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            UnityEngine.Debug.Log("Build succeeded: " + apkPath);
        else
            UnityEngine.Debug.LogError("Build failed: " + report.summary.result + " errors: " + report.summary.totalErrors);
    }

    [MenuItem("Tools/QuestHouseDesign/ADB/Set ADB Path")]
    public static void SetAdbPath()
    {
        string cur = EditorPrefs.GetString("QuestExporter_ADBPath", "");
        string sel = EditorUtility.OpenFilePanel("Select adb executable", string.IsNullOrEmpty(cur) ? "C:\\" : Path.GetDirectoryName(cur), "exe");
        if (!string.IsNullOrEmpty(sel))
        {
            EditorPrefs.SetString("QuestExporter_ADBPath", sel);
            UnityEngine.Debug.Log("ADB path set to: " + sel);
        }
    }

    [MenuItem("Tools/QuestHouseDesign/ADB/Build & Install APK")]
    public static void BuildAndInstall()
    {
        BuildApk();
        string apkPath = Path.Combine(outputDir, apkName);
        if (!File.Exists(apkPath))
        {
            UnityEngine.Debug.LogError("APK not found: " + apkPath);
            return;
        }

        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH. Please install platform-tools or add adb to PATH.");
            return;
        }

        var devices = RunAdbCommand("devices");
        if (!devices.Contains("device") && !devices.Contains("unauthorized"))
        {
            UnityEngine.Debug.LogWarning("No device detected by adb. Output:\n" + devices);
            // Still attempt install; adb will error if no device
        }

        UnityEngine.Debug.Log("Installing APK to device...");
        var installOut = RunAdbCommand($"install -r \"{apkPath}\"");
        UnityEngine.Debug.Log("adb install output:\n" + installOut);
        // If install succeeded, try to start the app
        try
        {
            if (installOut != null && installOut.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string packageId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
                if (string.IsNullOrEmpty(packageId)) packageId = "com.veksco.questhousedesign";
                string activity = packageId + "/com.unity3d.player.UnityPlayerActivity";
                UnityEngine.Debug.Log("Starting app: " + activity);
                var startOut = RunAdbCommand($"shell am start -n {activity}");
                UnityEngine.Debug.Log("adb start output:\n" + startOut);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Install output did not indicate success; skipping auto-start.");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to start app after install: " + ex.Message);
        }
    }

    [MenuItem("Tools/QuestHouseDesign/ADB/Pull Exports from Device")]
    public static void PullExports()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH. Please install platform-tools or add adb to PATH.");
            return;
        }

        string packageId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        if (string.IsNullOrEmpty(packageId)) packageId = "com.veksco.questhousedesign";
        string remote = $"/sdcard/Android/data/{packageId}/files/QuestHouseExport";
        string local = Path.Combine(Environment.CurrentDirectory, "PulledExports");
        if (!Directory.Exists(local)) Directory.CreateDirectory(local);

        UnityEngine.Debug.Log($"Pulling exports from device: {remote} -> {local}");
        var outp = RunAdbCommand($"pull \"{remote}\" \"{local}\"");
        UnityEngine.Debug.Log("adb pull output:\n" + outp);
        // Also try to pull log file if present
        var remoteLog = $"/sdcard/Android/data/{packageId}/files/QuestHouseExport/export_log.txt";
        var outp2 = RunAdbCommand($"pull \"{remoteLog}\" \"{local}\"  ");
        UnityEngine.Debug.Log("adb pull log output:\n" + outp2);
    }

    static bool IsAdbAvailable()
    {
        try
        {
            var adb = GetAdbExecutable();
            if (string.IsNullOrEmpty(adb)) return false;
            var outp = RunAdbCommandWithExe(adb, "version");
            return !string.IsNullOrEmpty(outp);
        }
        catch { return false; }
    }

    static string RunAdbCommand(string args)
    {
        var adb = GetAdbExecutable();
        if (string.IsNullOrEmpty(adb)) return "";
        return RunAdbCommandWithExe(adb, args);
    }

    static string RunAdbCommandWithExe(string adbPath, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = adbPath,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(adbPath) ?? Environment.CurrentDirectory
        };
        using (var p = Process.Start(psi))
        {
            var outp = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (!string.IsNullOrEmpty(err)) outp += "\nERR:\n" + err;
            return outp;
        }
    }

    static string GetAdbExecutable()
    {
        // 1) custom from EditorPrefs
        string custom = EditorPrefs.GetString("QuestExporter_ADBPath", "");
        if (!string.IsNullOrEmpty(custom) && File.Exists(custom)) return custom;

        // 2) try 'where adb' to resolve from PATH
        try
        {
            var psi = new ProcessStartInfo { FileName = "where", Arguments = "adb", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            using (var p = Process.Start(psi))
            {
                var outp = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (!string.IsNullOrEmpty(outp))
                {
                    var first = outp.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    if (File.Exists(first))
                    {
                        EditorPrefs.SetString("QuestExporter_ADBPath", first);
                        return first;
                    }
                }
            }
        }
        catch { }

        // 3) common locations
        string[] common = new[] {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "platform-tools", "adb.exe"),
            Path.Combine("C:", "RSL", "platform-tools", "adb.exe"),
            Path.Combine("C:", "platform-tools", "adb.exe")
        };
        foreach (var pth in common) if (File.Exists(pth)) { EditorPrefs.SetString("QuestExporter_ADBPath", pth); return pth; }

        return null;
    }
}
