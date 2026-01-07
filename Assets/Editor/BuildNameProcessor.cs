using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Automatically modifies APK name based on AutoBootstrapper settings
/// Adds "_TestMode" suffix when testModeSimpleUI is enabled
/// </summary>
public class BuildNameProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // Only for Android builds
        if (report.summary.platform != BuildTarget.Android)
            return;

        Debug.Log("[BuildNameProcessor] Starting pre-build processing...");

        // Try to find AutoBootstrapper in build scenes
        bool isTestMode = FindTestModeInBuildScenes();
        
        // Get current product name
        string baseProductName = PlayerSettings.productName;
        
        // Remove any existing "_TestMode" suffix first
        if (baseProductName.EndsWith("_TestMode"))
        {
            baseProductName = baseProductName.Replace("_TestMode", "");
        }

        if (isTestMode)
        {
            // Add test mode suffix to product name
            PlayerSettings.productName = baseProductName + "_TestMode";
            Debug.Log($"[BuildNameProcessor] ? TEST MODE BUILD");
            Debug.Log($"[BuildNameProcessor]   Product Name: {PlayerSettings.productName}");
            Debug.Log($"[BuildNameProcessor]   APK will be named: {PlayerSettings.productName}.apk");
        }
        else
        {
            // Normal build - ensure no suffix
            PlayerSettings.productName = baseProductName;
            Debug.Log($"[BuildNameProcessor] ? PRODUCTION BUILD");
            Debug.Log($"[BuildNameProcessor]   Product Name: {PlayerSettings.productName}");
            Debug.Log($"[BuildNameProcessor]   APK will be named: {PlayerSettings.productName}.apk");
        }
        
        // Log bundle info
        Debug.Log($"[BuildNameProcessor]   Bundle ID: {PlayerSettings.applicationIdentifier}");
        Debug.Log($"[BuildNameProcessor]   Version: {PlayerSettings.bundleVersion} (code: {PlayerSettings.Android.bundleVersionCode})");
    }
    
    bool FindTestModeInBuildScenes()
    {
        // Get all scenes in build settings
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogWarning("[BuildNameProcessor] No scenes in build settings!");
            return false;
        }

        // Save currently open scenes
        Scene[] openScenes = new Scene[SceneManager.sceneCount];
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            openScenes[i] = SceneManager.GetSceneAt(i);
        }

        bool testModeFound = false;

        // Check each build scene
        foreach (var sceneSettings in scenes)
        {
            if (!sceneSettings.enabled)
                continue;

            try
            {
                // Load scene temporarily
                Scene scene = EditorSceneManager.OpenScene(sceneSettings.path, OpenSceneMode.Single);
                
                // Find AutoBootstrapper
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    var bootstrapper = rootObj.GetComponentInChildren<AutoBootstrapper>(true);
                    if (bootstrapper != null)
                    {
                        testModeFound = bootstrapper.testModeSimpleUI;
                        Debug.Log($"[BuildNameProcessor] Found AutoBootstrapper in scene '{scene.name}'");
                        Debug.Log($"[BuildNameProcessor]   testModeSimpleUI = {testModeFound}");
                        break;
                    }
                }

                if (testModeFound)
                    break;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildNameProcessor] Could not load scene {sceneSettings.path}: {ex.Message}");
            }
        }

        // Restore original scenes
        if (openScenes.Length > 0 && openScenes[0].IsValid())
        {
            try
            {
                EditorSceneManager.OpenScene(openScenes[0].path, OpenSceneMode.Single);
                for (int i = 1; i < openScenes.Length; i++)
                {
                    if (openScenes[i].IsValid())
                    {
                        EditorSceneManager.OpenScene(openScenes[i].path, OpenSceneMode.Additive);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildNameProcessor] Could not restore scenes: {ex.Message}");
            }
        }

        if (!testModeFound)
        {
            Debug.LogWarning("[BuildNameProcessor] AutoBootstrapper not found in any build scene - defaulting to PRODUCTION mode");
        }

        return testModeFound;
    }
}

/// <summary>
/// Post-build processor to restore original product name
/// </summary>
public class BuildNamePostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        Debug.Log("[BuildNamePostProcessor] Post-build processing...");

        // Restore original name (remove _TestMode suffix)
        string currentName = PlayerSettings.productName;
        if (currentName.EndsWith("_TestMode"))
        {
            string restoredName = currentName.Replace("_TestMode", "");
            PlayerSettings.productName = restoredName;
            Debug.Log($"[BuildNamePostProcessor] ? Restored product name: '{currentName}' ? '{restoredName}'");
        }
        else
        {
            Debug.Log($"[BuildNamePostProcessor] Product name unchanged: '{currentName}'");
        }
        
        // Log final APK path if available
        if (report.summary.platform == BuildTarget.Android && !string.IsNullOrEmpty(report.summary.outputPath))
        {
            Debug.Log($"[BuildNamePostProcessor] APK output: {report.summary.outputPath}");
        }
    }
}

