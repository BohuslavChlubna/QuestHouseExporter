using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor menu for quick Test Mode toggle
/// Menu: Tools/Quest House Design/Toggle Test Mode
/// </summary>
public class TestModeToggle : EditorWindow
{
    private static AutoBootstrapper FindBootstrapper()
    {
        // Try to find in current scene
        var bootstrapper = Object.FindFirstObjectByType<AutoBootstrapper>();
        
        if (bootstrapper == null)
        {
            Debug.LogWarning("[TestModeToggle] AutoBootstrapper not found in current scene!");
        }
        
        return bootstrapper;
    }

    [MenuItem("Tools/Quest House Design/Toggle Test Mode")]
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

        // Toggle the test mode
        bool newState = !bootstrapper.testModeSimpleUI;
        bootstrapper.testModeSimpleUI = newState;
        
        // Mark scene as dirty
        EditorUtility.SetDirty(bootstrapper);
        EditorSceneManager.MarkSceneDirty(bootstrapper.gameObject.scene);
        
        // Show notification
        string message = newState 
            ? "? TEST MODE ENABLED\n\nBuild will be named: " + PlayerSettings.productName + "_TestMode.apk"
            : "? TEST MODE DISABLED\n\nBuild will be named: " + PlayerSettings.productName + ".apk";
        
        EditorUtility.DisplayDialog("Test Mode Toggle", message, "OK");
        
        Debug.Log($"[TestModeToggle] Test Mode {(newState ? "ENABLED" : "DISABLED")}");
    }

    [MenuItem("Tools/Quest House Design/Show Test Mode Status")]
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
        string apkName = isTestMode 
            ? PlayerSettings.productName + "_TestMode.apk" 
            : PlayerSettings.productName + ".apk";
        
        string message = isTestMode
            ? $"? TEST MODE ACTIVE\n\nAPK Name: {apkName}\n\nVisualizations: DISABLED\nUI Only: YES"
            : $"? PRODUCTION MODE\n\nAPK Name: {apkName}\n\nVisualizations: ENABLED\nFull functionality: YES";
        
        EditorUtility.DisplayDialog("Test Mode Status", message, "OK");
    }

    [MenuItem("Tools/Quest House Design/Build Info")]
    public static void ShowBuildInfo()
    {
        var bootstrapper = FindBootstrapper();
        
        string testModeStatus = bootstrapper != null && bootstrapper.testModeSimpleUI 
            ? "ENABLED (UI only)" 
            : "DISABLED (Full app)";
        
        string apkName = bootstrapper != null && bootstrapper.testModeSimpleUI
            ? PlayerSettings.productName + "_TestMode.apk"
            : PlayerSettings.productName + ".apk";
        
        string info = $"Product Name: {PlayerSettings.productName}\n" +
                     $"APK Name: {apkName}\n" +
                     $"Bundle ID: {PlayerSettings.applicationIdentifier}\n" +
                     $"Version: {PlayerSettings.bundleVersion}\n" +
                     $"Build Number: {PlayerSettings.Android.bundleVersionCode}\n" +
                     $"\nTest Mode: {testModeStatus}";
        
        EditorUtility.DisplayDialog("Build Information", info, "OK");
    }
    
    [MenuItem("Tools/Quest House Design/Test Build Name Processor")]
    public static void TestBuildNameProcessor()
    {
        var bootstrapper = FindBootstrapper();
        
        if (bootstrapper == null)
        {
            EditorUtility.DisplayDialog(
                "Test Failed",
                "AutoBootstrapper not found in scene!",
                "OK"
            );
            return;
        }
        
        bool isTestMode = bootstrapper.testModeSimpleUI;
        string currentProductName = PlayerSettings.productName;
        
        // Remove suffix if exists
        string baseName = currentProductName.EndsWith("_TestMode") 
            ? currentProductName.Replace("_TestMode", "") 
            : currentProductName;
        
        string expectedAPKName = isTestMode 
            ? $"{baseName}_TestMode.apk" 
            : $"{baseName}.apk";
        
        string message = $"Build Name Processor Test:\n\n" +
                        $"Current Product Name: {currentProductName}\n" +
                        $"Test Mode Enabled: {isTestMode}\n" +
                        $"Expected APK Name: {expectedAPKName}\n\n" +
                        $"The BuildNameProcessor will:\n" +
                        $"1. Detect testModeSimpleUI = {isTestMode}\n" +
                        $"2. {(isTestMode ? "Add '_TestMode' suffix" : "Use original name")}\n" +
                        $"3. Set ProductName during build\n" +
                        $"4. After build, restore to: {baseName}";
        
        EditorUtility.DisplayDialog("Build Name Processor Test", message, "OK");
        
        Debug.Log($"[TestModeToggle] Build Name Processor Test:");
        Debug.Log($"  Current: {currentProductName}");
        Debug.Log($"  Test Mode: {isTestMode}");
        Debug.Log($"  Expected APK: {expectedAPKName}");
    }
    
    [MenuItem("Tools/Quest House Design/Open Build Folder")]
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
                Debug.Log($"[TestModeToggle] Created build folder: {buildPath}");
            }
            else
            {
                return;
            }
        }
        
        // Open folder in Windows Explorer
        EditorUtility.RevealInFinder(buildPath);
        Debug.Log($"[TestModeToggle] Opened build folder: {buildPath}");
    }
    
    [MenuItem("Tools/Quest House Design/Show Build Files Info")]
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
            
            sb.AppendLine($"?? {fileName}");
            sb.AppendLine($"   Size: {sizeInMB} MB ({fileInfo.Length:N0} bytes)");
            sb.AppendLine($"   Modified: {lastModified}");
            sb.AppendLine();
        }
        
        // Add button to open folder
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
            Debug.Log($"[TestModeToggle] Copied to clipboard: {buildPath}");
        }
    }
}


