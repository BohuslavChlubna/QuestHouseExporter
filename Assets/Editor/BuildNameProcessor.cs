using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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

        // Find AutoBootstrapper in scene
        var bootstrapper = Object.FindFirstObjectByType<AutoBootstrapper>();
        
        if (bootstrapper == null)
        {
            Debug.LogWarning("[BuildNameProcessor] AutoBootstrapper not found in scene - using default build name");
            return;
        }

        // Get current product name
        string baseProductName = PlayerSettings.productName;
        
        // Remove any existing "_TestMode" suffix first
        if (baseProductName.EndsWith("_TestMode"))
        {
            baseProductName = baseProductName.Replace("_TestMode", "");
        }

        // Check if test mode is enabled
        bool isTestMode = bootstrapper.testModeSimpleUI;
        
        if (isTestMode)
        {
            // Add test mode suffix
            PlayerSettings.productName = baseProductName + "_TestMode";
            Debug.Log($"[BuildNameProcessor] TEST MODE BUILD - APK name: {PlayerSettings.productName}.apk");
        }
        else
        {
            // Normal build - ensure no suffix
            PlayerSettings.productName = baseProductName;
            Debug.Log($"[BuildNameProcessor] PRODUCTION BUILD - APK name: {PlayerSettings.productName}.apk");
        }
        
        // Also update bundle version code for easier identification
        int currentBuildNumber = PlayerSettings.Android.bundleVersionCode;
        Debug.Log($"[BuildNameProcessor] Build version: {PlayerSettings.bundleVersion} ({currentBuildNumber})");
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

        // Restore original name (remove _TestMode suffix)
        string currentName = PlayerSettings.productName;
        if (currentName.EndsWith("_TestMode"))
        {
            PlayerSettings.productName = currentName.Replace("_TestMode", "");
            Debug.Log($"[BuildNamePostProcessor] Restored product name to: {PlayerSettings.productName}");
        }
    }
}
