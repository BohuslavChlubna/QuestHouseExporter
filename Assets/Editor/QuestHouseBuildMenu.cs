using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Quest House Design - Simplified Build Menu
/// Build buttons for Production and Test APK (with optional install)
/// </summary>
public class QuestHouseBuildMenu
{
    private static AutoBootstrapper FindBootstrapper()
    {
        var bootstrapper = Object.FindFirstObjectByType<AutoBootstrapper>();
        if (bootstrapper == null)
        {
            Debug.LogWarning("[QuestHouseBuildMenu] AutoBootstrapper not found in current scene!");
        }
        return bootstrapper;
    }

    // ============================================================
    // PRODUCTION BUILD
    // ============================================================

    [MenuItem("Tools/Quest House Design/Build Production APK", false, 1)]
    public static void BuildProductionAPK()
    {
        SetTestModeAndBuild(false, false, false);
    }
    
    [MenuItem("Tools/Quest House Design/Build Production APK and Install", false, 2)]
    public static void BuildProductionAndInstall()
    {
        SetTestModeAndBuild(false, true, false);
    }

    // ============================================================
    // DEVELOPMENT BUILD (for debugging)
    // ============================================================
    
    [MenuItem("Tools/Quest House Design/Build Development APK (with Profiler)", false, 5)]
    public static void BuildDevelopmentAPK()
    {
        SetTestModeAndBuild(false, false, true);
    }
    
    [MenuItem("Tools/Quest House Design/Build Development APK and Install", false, 6)]
    public static void BuildDevelopmentAndInstall()
    {
        SetTestModeAndBuild(false, true, true);
    }

    // ============================================================
    // TEST BUILD (UI ONLY)
    // ============================================================
    
    [MenuItem("Tools/Quest House Design/Build Test APK (UI Only)", false, 10)]
    public static void BuildTestAPK()
    {
        SetTestModeAndBuild(true, false, false);
    }
    
    [MenuItem("Tools/Quest House Design/Build Test APK and Install", false, 11)]
    public static void BuildTestAndInstall()
    {
        SetTestModeAndBuild(true, true, false);
    }

    // ============================================================
    // UTILITIES
    // ============================================================
    
    [MenuItem("Tools/Quest House Design/Uninstall from Quest", false, 50)]
    public static void UninstallFromQuest()
    {
        ADBTools.UninstallApp();
    }
    
    [MenuItem("Tools/Quest House Design/Pull Exports from Quest", false, 51)]
    public static void PullExportsFromQuest()
    {
        ADBTools.PullExports();
    }

    [MenuItem("Tools/Quest House Design/Open Build Folder", false, 100)]
    public static void OpenBuildFolder()
    {
        string buildPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Builds", "Android");
        buildPath = System.IO.Path.GetFullPath(buildPath);
        
        if (!System.IO.Directory.Exists(buildPath))
        {
            bool create = EditorUtility.DisplayDialog(
                "Build Folder Not Found",
                $"Build folder does not exist:\n{buildPath}\n\nDo you want to create it?",
                "Create",
                "Cancel"
            );
            
            if (create)
            {
                System.IO.Directory.CreateDirectory(buildPath);
                Debug.Log($"[QuestHouseBuildMenu] Created build folder: {buildPath}");
            }
            else
            {
                return;
            }
        }
        
        EditorUtility.RevealInFinder(buildPath);
    }
    
    [MenuItem("Tools/Quest House Design/Show Build Files", false, 101)]
    public static void ShowBuildFilesInfo()
    {
        string buildPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Builds", "Android");
        buildPath = System.IO.Path.GetFullPath(buildPath);
        
        if (!System.IO.Directory.Exists(buildPath))
        {
            EditorUtility.DisplayDialog(
                "Build Folder Not Found",
                $"Build folder does not exist:\n{buildPath}\n\nBuild an APK first.",
                "OK"
            );
            return;
        }
        
        var apkFiles = System.IO.Directory.GetFiles(buildPath, "*.apk");
        
        if (apkFiles.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No APK Files Found",
                $"No APK files found in:\n{buildPath}\n\nBuild an APK first.",
                "OK"
            );
            return;
        }
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Build Folder: {buildPath}\n");
        sb.AppendLine($"Found {apkFiles.Length} APK file(s):\n");
        
        foreach (var apkPath in apkFiles)
        {
            var fileInfo = new System.IO.FileInfo(apkPath);
            string fileName = System.IO.Path.GetFileName(apkPath);
            long sizeInMB = fileInfo.Length / (1024 * 1024);
            string lastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            string prefix = fileName.Contains("_TestMode") ? "TEST: " : "PROD: ";
            
            sb.AppendLine($"{prefix}{fileName}");
            sb.AppendLine($"   Size: {sizeInMB} MB ({fileInfo.Length:N0} bytes)");
            sb.AppendLine($"   Modified: {lastModified}");
            sb.AppendLine();
        }
        
        int result = EditorUtility.DisplayDialogComplex(
            "Build Files Information",
            sb.ToString(),
            "Open Folder",
            "Close",
            "Copy Path"
        );
        
        if (result == 0)
        {
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (result == 2)
        {
            EditorGUIUtility.systemCopyBuffer = buildPath;
            Debug.Log($"[QuestHouseBuildMenu] Copied to clipboard: {buildPath}");
        }
    }

    // ============================================================
    // INTERNAL HELPER
    // ============================================================

    private static void SetTestModeAndBuild(bool enableTestMode, bool alsoInstall, bool developmentBuild = false)
    {
        var bootstrapper = FindBootstrapper();
        
        if (bootstrapper == null)
        {
            EditorUtility.DisplayDialog(
                "AutoBootstrapper Not Found",
                "Cannot find AutoBootstrapper in scene.\n\nMake sure QuestHouseDesign scene is loaded.",
                "OK"
            );
            return;
        }

        string modeName = enableTestMode ? "Test Mode (UI Only)" : "Production Mode";
        if (developmentBuild) modeName += " + Development Build";
        
        // Set test mode (no confirmation needed - user explicitly chose via menu button)
        if (bootstrapper.testModeSimpleUI != enableTestMode)
        {
            Undo.RecordObject(bootstrapper, $"Set {modeName}");
            bootstrapper.testModeSimpleUI = enableTestMode;
            EditorUtility.SetDirty(bootstrapper);
            EditorSceneManager.MarkSceneDirty(bootstrapper.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            
            Debug.Log($"[QuestHouseBuildMenu] Switched to {modeName}");
        }

        // CRITICAL: Manually set ProductName BEFORE build (BuildNameProcessor will also do this, but we need it NOW)
        string baseProductName = PlayerSettings.productName.Replace("_TestMode", "").Replace("_Dev", "");
        string buildProductName; // This is what the APK will be named
        
        if (enableTestMode)
        {
            buildProductName = baseProductName + "_TestMode";
        }
        else if (developmentBuild)
        {
            buildProductName = baseProductName + "_Dev";
        }
        else
        {
            buildProductName = baseProductName;
        }
        
        PlayerSettings.productName = buildProductName;
        Debug.Log($"[QuestHouseBuildMenu] PRE-BUILD: Set ProductName to '{PlayerSettings.productName}'");

        // Build (and optionally install)
        // Pass the build product name so ADBTools knows what APK to look for
        if (alsoInstall)
        {
            ADBTools.BuildAndInstall(buildProductName, developmentBuild);
        }
        else
        {
            ADBTools.BuildApk(developmentBuild);
        }
        
        // CRITICAL: Restore ProductName after build
        PlayerSettings.productName = baseProductName;
        Debug.Log($"[QuestHouseBuildMenu] POST-BUILD: Restored ProductName to '{PlayerSettings.productName}'");
    }
}

