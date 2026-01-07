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
        Debug.Log("[BuildNameProcessor] Scanning build scenes for AutoBootstrapper...");
        
        // Get all scenes in build settings
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogWarning("[BuildNameProcessor] No scenes in build settings!");
            return false;
        }
        
        Debug.Log($"[BuildNameProcessor] Found {scenes.Length} scene(s) in build settings");

        // Save currently open scenes
        Scene[] openScenes = new Scene[SceneManager.sceneCount];
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            openScenes[i] = SceneManager.GetSceneAt(i);
        }

        bool testModeFound = false;
        bool bootstrapperFound = false;

        // Check each build scene
        for (int i = 0; i < scenes.Length; i++)
        {
            var sceneSettings = scenes[i];
            
            if (!sceneSettings.enabled)
            {
                Debug.Log($"[BuildNameProcessor] Scene {i}: '{sceneSettings.path}' - DISABLED, skipping");
                continue;
            }
            
            Debug.Log($"[BuildNameProcessor] Scene {i}: '{sceneSettings.path}' - checking...");

            try
            {
                // Load scene temporarily
                Scene scene = EditorSceneManager.OpenScene(sceneSettings.path, OpenSceneMode.Single);
                
                // Find AutoBootstrapper
                var rootObjects = scene.GetRootGameObjects();
                Debug.Log($"[BuildNameProcessor]   Searching in {rootObjects.Length} root GameObject(s)...");
                
                foreach (var rootObj in rootObjects)
                {
                    var bootstrapper = rootObj.GetComponentInChildren<AutoBootstrapper>(true);
                    if (bootstrapper != null)
                    {
                        bootstrapperFound = true;
                        testModeFound = bootstrapper.testModeSimpleUI;
                        Debug.Log($"[BuildNameProcessor]   ? Found AutoBootstrapper in '{rootObj.name}'");
                        Debug.Log($"[BuildNameProcessor]   testModeSimpleUI = {testModeFound}");
                        break; // Found bootstrapper, stop searching in this scene
                    }
                }

                if (bootstrapperFound)
                {
                    Debug.Log($"[BuildNameProcessor] Bootstrapper found, stopping scene scan");
                    break; // Found bootstrapper in a scene, stop checking other scenes
                }
                else
                {
                    Debug.Log($"[BuildNameProcessor]   No AutoBootstrapper found in this scene");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildNameProcessor] Could not load scene {sceneSettings.path}: {ex.Message}");
            }
        }

        // Restore original scenes
        Debug.Log($"[BuildNameProcessor] Restoring {openScenes.Length} originally open scene(s)...");
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
                Debug.Log($"[BuildNameProcessor] ? Scenes restored");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildNameProcessor] Could not restore scenes: {ex.Message}");
            }
        }

        if (!bootstrapperFound)
        {
            Debug.LogWarning("[BuildNameProcessor] ? AutoBootstrapper NOT FOUND in any build scene!");
            Debug.LogWarning("[BuildNameProcessor] Defaulting to PRODUCTION mode (no _TestMode suffix)");
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

