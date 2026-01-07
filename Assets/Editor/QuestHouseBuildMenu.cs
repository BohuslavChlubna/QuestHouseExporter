using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Quest House Design - Build Menu
/// Provides quick access to build, test mode toggle, and build utilities
/// </summary>
public class QuestHouseBuildMenu : EditorWindow
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
    // BUILD MENU - Main build commands
    // ============================================================
    
    [MenuItem("Tools/Quest House Design/Build Production APK", false, 1)]
    public static void BuildProductionAPK()
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

        // Ensure Test Mode is OFF
        if (bootstrapper.testModeSimpleUI)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Test Mode is ON",
                "Test Mode is currently enabled.\n\nProduction build requires Test Mode to be OFF.\n\nDisable Test Mode and continue?",
                "Yes, Disable and Build",
                "Cancel"
            );
            
            if (!confirm) return;
            
            // Disable test mode
            Undo.RecordObject(bootstrapper, "Disable Test Mode");
            bootstrapper.testModeSimpleUI = false;
            EditorUtility.SetDirty(bootstrapper);
            EditorSceneManager.MarkSceneDirty(bootstrapper.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            
            Debug.Log("[QuestHouseBuildMenu] Test Mode disabled for Production build");
        }

        // Call ADBTools build
        ADBTools.BuildApk();
    }
    
    [MenuItem("Tools/Quest House Design/Build Test APK (UI Only)", false, 2)]
    public static void BuildTestAPK()
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

        // Ensure Test Mode is ON
        if (!bootstrapper.testModeSimpleUI)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Test Mode is OFF",
                "Test Mode is currently disabled.\n\nTest build requires Test Mode to be ON (UI only, no visualizations).\n\nEnable Test Mode and continue?",
                "Yes, Enable and Build",
                "Cancel"
            );
            
            if (!confirm) return;
            
            // Enable test mode
            Undo.RecordObject(bootstrapper, "Enable Test Mode");
            bootstrapper.testModeSimpleUI = true;
            EditorUtility.SetDirty(bootstrapper);
            EditorSceneManager.MarkSceneDirty(bootstrapper.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            
            Debug.Log("[QuestHouseBuildMenu] Test Mode enabled for Test build");
        }

        // Call ADBTools build
        ADBTools.BuildApk();
    }

    [MenuItem("Tools/Quest House Design/Build and Install on Quest", false, 3)]
    public static void BuildAndInstall()
    {
        // Just call ADBTools - it respects current Test Mode state
        ADBTools.BuildAndInstall();
    }

    // ============================================================
    // SEPARATOR
    // ============================================================
    [MenuItem("Tools/Quest House Design/ ", false, 10)]
    static void Separator1() { }

    // ============================================================
    // TEST MODE TOGGLE
    // ============================================================
    
    [MenuItem("Tools/Quest House Design/Toggle Test Mode", false, 11)]
    public static void ToggleTestMode()
    {
        var bootstrapper = FindBootstrapper();
        
        if (bootstrapper == null)
        {
            EditorUtility.DisplayDialog(
                "AutoBootstrapper Not Found",
                "Cannot find AutoBootstrapper component in the scene.\n\nPlease make sure the scene is loaded.",
                "OK"
            );
            return;
        }

        bool newState = !bootstrapper.testModeSimpleUI;
        
        Undo.RecordObject(bootstrapper, "Toggle Test Mode");
        bootstrapper.testModeSimpleUI = newState;
        EditorUtility.SetDirty(bootstrapper);
        EditorSceneManager.MarkSceneDirty(bootstrapper.gameObject.scene);
        
        string baseName = PlayerSettings.productName.Replace("_TestMode", "");
        string apkName = newState ? $"{baseName}_TestMode.apk" : $"{baseName}.apk";
        
        string message = newState 
            ? $"? TEST MODE ENABLED\n\nNext build will be:\n{apkName}\n\nFeatures:\n• UI only\n• No visualizations\n• Faster iteration"
            : $"? PRODUCTION MODE\n\nNext build will be:\n{apkName}\n\nFeatures:\n• Full functionality\n• DollHouse + InRoom views\n• All visualizations";
        
        EditorUtility.DisplayDialog("Test Mode Toggle", message, "OK");
        
        Debug.Log($"[QuestHouseBuildMenu] Test Mode {(newState ? "ENABLED" : "DISABLED")}");
    }

    [MenuItem("Tools/Quest House Design/Current Build Mode Status", false, 12)]
    public static void ShowTestModeStatus()
    {
        var bootstrapper = FindBootstrapper();
        
        if (bootstrapper == null)
        {
            EditorUtility.DisplayDialog(
                "AutoBootstrapper Not Found",
                "Cannot find AutoBootstrapper component in the scene.",
                "OK"
            );
            return;
        }

        bool isTestMode = bootstrapper.testModeSimpleUI;
        string baseName = PlayerSettings.productName.Replace("_TestMode", "");
        string apkName = isTestMode ? $"{baseName}_TestMode.apk" : $"{baseName}.apk";
        
        string mode = isTestMode ? "? TEST MODE" : "? PRODUCTION MODE";
        string features = isTestMode 
            ? "• UI only\n• No visualizations\n• Faster build & iteration\n• Smaller APK size"
            : "• Full functionality\n• DollHouse visualization\n• InRoom wall view\n• All features enabled";
        
        string message = $"{mode}\n\nNext APK Name:\n{apkName}\n\nFeatures:\n{features}";
        
        EditorUtility.DisplayDialog("Current Build Mode", message, "OK");
    }

    // ============================================================
    // SEPARATOR
    // ============================================================
    [MenuItem("Tools/Quest House Design/  ", false, 20)]
    static void Separator2() { }

    // ============================================================
    // BUILD UTILITIES
    // ============================================================
    
    [MenuItem("Tools/Quest House Design/Open Build Folder", false, 21)]
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
        Debug.Log($"[QuestHouseBuildMenu] Opened build folder: {buildPath}");
    }
    
    [MenuItem("Tools/Quest House Design/Show Build Files Info", false, 22)]
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
            
            // Mark test builds
            string prefix = fileName.Contains("_TestMode") ? "?? TEST: " : "?? PROD: ";
            
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
        
        if (result == 0) // Open Folder
        {
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (result == 2) // Copy Path
        {
            EditorGUIUtility.systemCopyBuffer = buildPath;
            Debug.Log($"[QuestHouseBuildMenu] Copied to clipboard: {buildPath}");
        }
    }

    [MenuItem("Tools/Quest House Design/Build Information", false, 23)]
    public static void ShowBuildInfo()
    {
        var bootstrapper = FindBootstrapper();
        
        bool isTestMode = bootstrapper != null && bootstrapper.testModeSimpleUI;
        string baseName = PlayerSettings.productName.Replace("_TestMode", "");
        string nextAPKName = isTestMode ? $"{baseName}_TestMode.apk" : $"{baseName}.apk";
        string mode = isTestMode ? "TEST MODE (UI only)" : "PRODUCTION MODE (Full app)";
        
        string info = $"Current Build Configuration:\n\n" +
                     $"Mode: {mode}\n" +
                     $"Next APK: {nextAPKName}\n\n" +
                     $"Project Settings:\n" +
                     $"Product Name: {PlayerSettings.productName}\n" +
                     $"Bundle ID: {PlayerSettings.applicationIdentifier}\n" +
                     $"Version: {PlayerSettings.bundleVersion}\n" +
                     $"Build Number: {PlayerSettings.Android.bundleVersionCode}";
        
        EditorUtility.DisplayDialog("Build Information", info, "OK");
    }
}



