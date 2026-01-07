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
}
