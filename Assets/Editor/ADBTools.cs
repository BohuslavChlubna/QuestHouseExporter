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

    [MenuItem("Tools/QHD Build APK")]
    public static void BuildApk()
    {
        BuildApkInternal(showDialog: true);
    }

    static BuildResult BuildApkInternal(bool showDialog)
    {
        var scenes = EditorBuildSettings.scenes;
        var enabled = new System.Collections.Generic.List<string>();
        foreach (var s in scenes) if (s.enabled) enabled.Add(s.path);
        if (enabled.Count == 0)
        {
            UnityEngine.Debug.LogError("No enabled scenes in Build Settings.");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Build Error", "No enabled scenes in Build Settings.\n\nAdd your scene to Build Settings first:\nFile ? Build Settings ? Add Open Scenes", "OK");
            }
            return BuildResult.Failed;
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
        {
            UnityEngine.Debug.Log($"? Build succeeded: {apkPath} ({new FileInfo(apkPath).Length / 1024 / 1024} MB)");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Build Complete", 
                    $"APK built successfully!\n\nLocation: {apkPath}\nSize: {new FileInfo(apkPath).Length / 1024 / 1024} MB\n\nReady to install on Quest.", 
                    "OK");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Build failed: " + report.summary.result + " errors: " + report.summary.totalErrors);
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Build Failed", $"Build failed with {report.summary.totalErrors} error(s).\n\nResult: {report.summary.result}\n\nCheck Console for details.", "OK");
            }
        }
        
        return report.summary.result;
    }

    [MenuItem("Tools/QHD Build and Install")]
    public static void BuildAndInstall()
    {
        // Check device connection first
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH. Please install platform-tools or add adb to PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB executable not found. Please:\n1. Install Android Platform Tools\n2. Add platform-tools to PATH", "OK");
            return;
        }

        var devices = RunAdbCommand("devices");
        bool hasDevice = devices.Contains("device") && !devices.Contains("unauthorized");
        bool isWireless = devices.Contains(":5555");
        
        if (!hasDevice)
        {
            UnityEngine.Debug.LogWarning("No Quest device detected. Output:\n" + devices);
            
            // Offer wireless connection option
            var choice = EditorUtility.DisplayDialogComplex("No Device Detected", 
                "No Quest device found.\n\n" +
                "USB: Connect Quest via USB cable\n" +
                "WiFi: Connect via wireless ADB\n\n" +
                "Make sure:\n" +
                "• USB debugging is enabled\n" +
                "• Authorization prompt is accepted on headset", 
                "Connect via WiFi", 
                "Build Anyway", 
                "Cancel");
            
            if (choice == 0) // Connect via WiFi
            {
                string lastIP = EditorPrefs.GetString("QuestExporter_LastQuestIP", "");
                WirelessADBWindow.ShowWindow(lastIP);
                return;
            }
            else if (choice == 2) // Cancel
            {
                return;
            }
            // choice == 1 falls through to build anyway
        }
        else if (isWireless)
        {
            UnityEngine.Debug.Log("[WiFi] Using wireless ADB connection");
        }
        else
        {
            UnityEngine.Debug.Log("[USB] Using USB connection");
        }

        // Build silently (no intermediate dialog)
        var buildResult = BuildApkInternal(showDialog: false);
        
        if (buildResult != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Build Failed", "APK build failed. Check Console for details.", "OK");
            return;
        }
        
        string apkPath = Path.Combine(outputDir, apkName);
        if (!File.Exists(apkPath))
        {
            UnityEngine.Debug.LogError("APK not found after build: " + apkPath);
            EditorUtility.DisplayDialog("Build Failed", 
                $"APK was not created.\n\nExpected location:\n{apkPath}\n\nPossible reasons:\n• Build failed (check Console)\n• No scenes in Build Settings\n• Build was cancelled", 
                "OK");
            return;
        }

        UnityEngine.Debug.Log($"APK found at: {apkPath} ({new FileInfo(apkPath).Length / 1024 / 1024} MB)");

        // Re-check device before install
        if (!hasDevice)
        {
            UnityEngine.Debug.LogWarning("Skipping install - no device connected.");
            EditorUtility.DisplayDialog("Build Complete", $"APK built successfully:\n{apkPath}\n\nInstall manually via SideQuest or connect Quest and retry.", "OK");
            return;
        }

        UnityEngine.Debug.Log("Installing APK to device...");
        
        // Use absolute path for ADB (resolve full Windows path)
        string absolutePath = Path.GetFullPath(apkPath);
        var installOut = RunAdbCommand($"install -r \"{absolutePath}\"");
        UnityEngine.Debug.Log("adb install output:\n" + installOut);
        
        // Check install result (no success dialog, only log)
        if (installOut != null && installOut.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            UnityEngine.Debug.Log("[SUCCESS] Install successful - starting app...");
        }
        else
        {
            UnityEngine.Debug.LogWarning("Install may have failed. Check output above.");
            EditorUtility.DisplayDialog("Install Failed", $"Install output:\n{installOut}\n\nCheck Console for details.", "OK");
        }
        
        // If install succeeded, try to start the app
        bool installSuccess = installOut != null && installOut.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0;
        bool appStarted = false;
        
        try
        {
            if (installSuccess)
            {
                string packageId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
                if (string.IsNullOrEmpty(packageId)) packageId = "com.veksco.questhousedesign";
                
                // Try launching with monkey command
                UnityEngine.Debug.Log("Starting app via monkey...");
                var startOut = RunAdbCommand($"shell monkey -p {packageId} -c android.intent.category.LAUNCHER 1");
                UnityEngine.Debug.Log("App launch output:\n" + startOut);
                
                appStarted = !startOut.Contains("monkey aborted");
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
        
        // Final summary dialog
        if (installSuccess)
        {
            string message;
            if (appStarted)
            {
                message = "[OK] Build complete\n[OK] Installed on Quest\n[OK] App started\n\nCheck your Quest headset!";
            }
            else
            {
                message = "[OK] Build complete\n[OK] Installed on Quest\n[WARN] App auto-start failed\n\nLaunch manually from Unknown Sources.";
            }
            
            EditorUtility.DisplayDialog("Success", message, "OK");
        }
    }

    [MenuItem("Tools/ADB/Enable Wireless ADB")]
    public static void EnableWirelessADB()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB not found. Install Android Platform Tools and add to PATH.", "OK");
            return;
        }

        var devices = RunAdbCommand("devices");
        if (!devices.Contains("device") || devices.Contains("unauthorized"))
        {
            UnityEngine.Debug.LogWarning("No USB device detected. Connect Quest via USB first.");
            EditorUtility.DisplayDialog("No USB Device", 
                "Quest not connected via USB.\n\n" +
                "To enable wireless ADB:\n" +
                "1. Connect Quest via USB cable\n" +
                "2. Enable USB debugging\n" +
                "3. Run this command again\n\n" +
                "After that, you can disconnect the cable and use WiFi.", 
                "OK");
            return;
        }

        UnityEngine.Debug.Log("Enabling wireless ADB on port 5555...");
        var output = RunAdbCommand("tcpip 5555");
        UnityEngine.Debug.Log("ADB tcpip output:\n" + output);

        if (output.Contains("restarting") || output.Contains("5555"))
        {
            // Try to get Quest IP address
            string ipAddress = GetQuestIPAddress();
            
            string message = "[OK] Wireless ADB enabled!\n\n";
            if (!string.IsNullOrEmpty(ipAddress))
            {
                message += $"Quest IP: {ipAddress}\n\n";
                message += "You can now:\n";
                message += "1. Disconnect USB cable\n";
                message += $"2. Use 'Connect to Quest' with IP: {ipAddress}\n\n";
                
                // Save IP for later
                EditorPrefs.SetString("QuestExporter_LastQuestIP", ipAddress);
            }
            else
            {
                message += "You can now:\n";
                message += "1. Disconnect USB cable\n";
                message += "2. Find Quest IP in Settings ? WiFi\n";
                message += "3. Use 'Connect to Quest' menu\n\n";
            }
            
            message += "Note: Quest and PC must be on the same WiFi network.";
            
            EditorUtility.DisplayDialog("Wireless ADB Enabled", message, "OK");
            UnityEngine.Debug.Log("[ADB] Wireless mode enabled. You can disconnect USB cable now.");
        }
        else
        {
            EditorUtility.DisplayDialog("Failed", 
                $"Could not enable wireless ADB.\n\nOutput:\n{output}\n\nMake sure Quest is connected via USB.", 
                "OK");
        }
    }

    [MenuItem("Tools/ADB/Connect to Quest")]
    public static void ConnectWirelessADB()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB not found. Install Android Platform Tools and add to PATH.", "OK");
            return;
        }

        // Get saved IP or ask user
        string lastIP = EditorPrefs.GetString("QuestExporter_LastQuestIP", "");
        
        // Try to auto-detect IP if USB is connected
        var devices = RunAdbCommand("devices");
        if (devices.Contains("device") && !devices.Contains("unauthorized") && !devices.Contains(":5555"))
        {
            UnityEngine.Debug.Log("USB device detected, trying to auto-detect IP...");
            string autoIP = GetQuestIPAddress();
            if (!string.IsNullOrEmpty(autoIP))
            {
                lastIP = autoIP;
                UnityEngine.Debug.Log($"Auto-detected Quest IP: {autoIP}");
            }
        }
        
        // Show window with IP input
        WirelessADBWindow.ShowWindow(lastIP);
    }

    [MenuItem("Tools/ADB/Disconnect Wireless")]
    public static void DisconnectWirelessADB()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            return;
        }

        string lastIP = EditorPrefs.GetString("QuestExporter_LastQuestIP", "");
        if (!string.IsNullOrEmpty(lastIP))
        {
            UnityEngine.Debug.Log($"Disconnecting from {lastIP}...");
            var output = RunAdbCommand($"disconnect {lastIP}:5555");
            UnityEngine.Debug.Log("Disconnect output:\n" + output);
        }
        
        var output2 = RunAdbCommand("disconnect");
        UnityEngine.Debug.Log("Disconnect all output:\n" + output2);
        
        EditorUtility.DisplayDialog("Disconnected", "Wireless ADB disconnected.\n\nTo reconnect, use USB or 'Connect to Quest'.", "OK");
    }

    public static void ConnectToQuestIP(string ipAddress)
    {
        UnityEngine.Debug.Log($"Connecting to Quest at {ipAddress}:5555...");
        var output = RunAdbCommand($"connect {ipAddress}:5555");
        UnityEngine.Debug.Log("ADB connect output:\n" + output);

        if (output.Contains("connected") || output.Contains("already connected"))
        {
            EditorPrefs.SetString("QuestExporter_LastQuestIP", ipAddress);
            EditorUtility.DisplayDialog("Connected!", 
                $"[OK] Connected to Quest via WiFi\n\n" +
                $"IP: {ipAddress}:5555\n\n" +
                $"You can now build and install without USB cable!", 
                "OK");
        }
        else if (output.Contains("cannot connect") || output.Contains("failed"))
        {
            EditorUtility.DisplayDialog("Connection Failed", 
                $"[ERROR] Could not connect to Quest\n\n" +
                $"IP: {ipAddress}\n\n" +
                $"Make sure:\n" +
                $"• Quest is on the same WiFi network\n" +
                $"• Wireless ADB is enabled (use USB first)\n" +
                $"• IP address is correct\n\n" +
                $"Output:\n{output}", 
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Unknown Result", 
                $"ADB connect output:\n{output}\n\n" +
                $"Check console for details.", 
                "OK");
        }
    }

    static string GetQuestIPAddress()
    {
        try
        {
            // Try to get IP via adb shell
            var output = RunAdbCommand("shell ip addr show wlan0");
            
            // Parse IP from output (look for "inet 192.168.x.x")
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("inet ") && !line.Contains("inet6"))
                {
                    var parts = line.Trim().Split(' ');
                    if (parts.Length > 1)
                    {
                        var ipPart = parts[1].Split('/')[0]; // Remove /24 suffix
                        if (System.Net.IPAddress.TryParse(ipPart, out _))
                        {
                            return ipPart;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("Could not auto-detect Quest IP: " + ex.Message);
        }
        
        return null;
    }

    [MenuItem("Tools/QHD Uninstall App")]
    public static void UninstallApp()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB not found. Install Android Platform Tools and add to PATH.", "OK");
            return;
        }

        var devices = RunAdbCommand("devices");
        bool hasDevice = devices.Contains("device") && !devices.Contains("unauthorized");
        
        if (!hasDevice)
        {
            UnityEngine.Debug.LogWarning("No Quest device detected. Output:\n" + devices);
            
            // Offer wireless connection option (same as Build and Install)
            var choice = EditorUtility.DisplayDialogComplex("No Device Detected", 
                "No Quest device found.\n\n" +
                "USB: Connect Quest via USB cable\n" +
                "WiFi: Connect via wireless ADB\n\n" +
                "Make sure:\n" +
                "• USB debugging is enabled\n" +
                "• Authorization prompt is accepted on headset", 
                "Connect via WiFi", 
                "Cancel", 
                "Retry");
            
            if (choice == 0) // Connect via WiFi
            {
                string lastIP = EditorPrefs.GetString("QuestExporter_LastQuestIP", "");
                WirelessADBWindow.ShowWindow(lastIP);
                return;
            }
            else if (choice == 2) // Retry
            {
                UninstallApp(); // Recursively retry
                return;
            }
            // choice == 1 is Cancel - just return
            return;
        }

        string packageId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        if (string.IsNullOrEmpty(packageId)) packageId = "com.veksco.questhousedesign";

        // Confirm before uninstalling
        bool confirm = EditorUtility.DisplayDialog("Uninstall App", 
            $"Are you sure you want to uninstall:\n\n{packageId}\n\nThis will remove all app data including exports!", 
            "Uninstall", 
            "Cancel");
        
        if (!confirm) return;

        UnityEngine.Debug.Log($"Uninstalling {packageId}...");
        var output = RunAdbCommand($"uninstall {packageId}");
        UnityEngine.Debug.Log("Uninstall output:\n" + output);

        if (output.Contains("Success"))
        {
            EditorUtility.DisplayDialog("Uninstall Complete", 
                $"[OK] App uninstalled successfully!\n\nPackage: {packageId}\n\nYou can now do a clean install.", 
                "OK");
        }
        else if (output.Contains("not installed"))
        {
            EditorUtility.DisplayDialog("Not Installed", 
                $"App is not installed on device.\n\nPackage: {packageId}", 
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Uninstall Failed", 
                $"Could not uninstall app.\n\nOutput:\n{output}\n\nCheck Console for details.", 
                "OK");
        }
    }

    [MenuItem("Tools/QHD Pull Exports")]
    public static void PullExports()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB not found. Install Android Platform Tools and add to PATH.", "OK");
            return;
        }

        var devices = RunAdbCommand("devices");
        if (!devices.Contains("device") || devices.Contains("unauthorized"))
        {
            UnityEngine.Debug.LogWarning("No device detected.");
            EditorUtility.DisplayDialog("No Device", "Quest not connected or not authorized.\n\nConnect Quest via USB and enable USB debugging.", "OK");
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
        var remoteLog = $"/sdcard/Android/data/{packageId}/files/QuestHouseDesign/export_log.txt";
        var outp2 = RunAdbCommand($"pull \"{remoteLog}\" \"{local}\"");
        UnityEngine.Debug.Log("adb pull log output:\n" + outp2);

        // Show result dialog
        if (outp.Contains("pulled") || outp.Contains("file"))
        {
            EditorUtility.DisplayDialog("Pull Complete", $"Exports pulled to:\n{local}\n\nCheck folder for GLB/OBJ/SVG/JSON files.", "OK");
            // Open folder in Explorer
            System.Diagnostics.Process.Start("explorer.exe", local.Replace("/", "\\"));
        }
        else
        {
            EditorUtility.DisplayDialog("Pull Failed", $"Could not pull exports.\n\nMake sure you ran Export in the app first.\n\nOutput:\n{outp}", "OK");
        }
    }

    [MenuItem("Tools/ADB/Logcat/Show Logcat (Unity)")]
    public static void ShowLogcat()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB not found. Install Android Platform Tools and add to PATH.", "OK");
            return;
        }

        var devices = RunAdbCommand("devices");
        if (!devices.Contains("device") || devices.Contains("unauthorized"))
        {
            UnityEngine.Debug.LogWarning("No device detected.");
            EditorUtility.DisplayDialog("No Device", "Quest not connected.\n\nConnect Quest via USB or WiFi first.", "OK");
            return;
        }

        string packageId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        if (string.IsNullOrEmpty(packageId)) packageId = "com.veksco.questhousedesign";

        UnityEngine.Debug.Log($"Starting logcat for {packageId}...");
        UnityEngine.Debug.Log("Close the terminal window to stop logcat.");
        
        // Start logcat in new terminal window, filtered for Unity
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k adb logcat -s Unity ActivityManager PackageManager DEBUG",
            UseShellExecute = true,
            CreateNoWindow = false
        };
        Process.Start(psi);
    }

    [MenuItem("Tools/ADB/Logcat/Clear Logcat")]
    public static void ClearLogcat()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB not found. Install Android Platform Tools and add to PATH.", "OK");
            return;
        }

        var devices = RunAdbCommand("devices");
        if (!devices.Contains("device") || devices.Contains("unauthorized"))
        {
            UnityEngine.Debug.LogWarning("No device detected.");
            EditorUtility.DisplayDialog("No Device", "Quest not connected.\n\nConnect Quest via USB or WiFi first.", "OK");
            return;
        }

        UnityEngine.Debug.Log("Clearing logcat buffer...");
        var output = RunAdbCommand("logcat -c");
        UnityEngine.Debug.Log("Logcat cleared.");
        EditorUtility.DisplayDialog("Logcat Cleared", "Logcat buffer has been cleared.\n\nYou can now start fresh logging.", "OK");
    }

    [MenuItem("Tools/ADB/Logcat/Save Logcat")]
    public static void SaveLogcat()
    {
        if (!IsAdbAvailable())
        {
            UnityEngine.Debug.LogError("ADB not found in PATH.");
            EditorUtility.DisplayDialog("ADB Not Found", "ADB not found. Install Android Platform Tools and add to PATH.", "OK");
            return;
        }

        var devices = RunAdbCommand("devices");
        if (!devices.Contains("device") || devices.Contains("unauthorized"))
        {
            UnityEngine.Debug.LogWarning("No device detected.");
            EditorUtility.DisplayDialog("No Device", "Quest not connected.\n\nConnect Quest via USB or WiFi first.", "OK");
            return;
        }

        string logFolder = Path.Combine(Environment.CurrentDirectory, "Logs");
        if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
        
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logPath = Path.Combine(logFolder, $"logcat_{timestamp}.txt");

        UnityEngine.Debug.Log($"Saving logcat to: {logPath}");
        var output = RunAdbCommand("logcat -d -s Unity ActivityManager PackageManager DEBUG");
        
        File.WriteAllText(logPath, output);
        UnityEngine.Debug.Log($"Logcat saved to: {logPath}");
        
        EditorUtility.DisplayDialog("Logcat Saved", 
            $"Logcat saved to:\n{logPath}\n\nFile size: {new FileInfo(logPath).Length / 1024} KB", 
            "OK");
        
        // Open folder in Explorer
        System.Diagnostics.Process.Start("explorer.exe", logFolder.Replace("/", "\\"));
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

    public static string RunAdbCommand(string args)
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
