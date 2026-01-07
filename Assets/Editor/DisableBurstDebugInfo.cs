using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Automatically disables Burst debug info generation to keep build folder clean.
/// The _BurstDebugInformation_DoNotShip folder is only needed for debugging Burst code.
/// </summary>
[InitializeOnLoad]
public class DisableBurstDebugInfo : IPreprocessBuildWithReport
{
    public int callbackOrder => -100; // Run early
    
    // Static constructor runs when Unity loads (Editor startup)
    static DisableBurstDebugInfo()
    {
        // Disable Burst debug info PERMANENTLY in Editor
        #if UNITY_2021_2_OR_NEWER
        try
        {
            // Access Burst compiler options via reflection (works in all Unity versions)
            var burstCompilerType = System.Type.GetType("Unity.Burst.BurstCompiler, Unity.Burst");
            if (burstCompilerType != null)
            {
                var optionsProperty = burstCompilerType.GetProperty("Options");
                if (optionsProperty != null)
                {
                    var options = optionsProperty.GetValue(null);
                    if (options != null)
                    {
                        var enableDebugField = options.GetType().GetProperty("EnableBurstDebug");
                        if (enableDebugField != null)
                        {
                            enableDebugField.SetValue(options, false);
                            UnityEngine.Debug.Log("[DisableBurstDebugInfo] Burst debug info disabled globally (Editor startup)");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[DisableBurstDebugInfo] Could not disable Burst debug: {ex.Message}");
        }
        #endif
    }
    
    public void OnPreprocessBuild(BuildReport report)
    {
        // Only for Android Release builds
        if (report.summary.platform != BuildTarget.Android)
            return;
        
        #if UNITY_2021_2_OR_NEWER
        // Double-check: Disable Burst debug info before build
        try
        {
            var burstCompilerType = System.Type.GetType("Unity.Burst.BurstCompiler, Unity.Burst");
            if (burstCompilerType != null)
            {
                var optionsProperty = burstCompilerType.GetProperty("Options");
                if (optionsProperty != null)
                {
                    var options = optionsProperty.GetValue(null);
                    if (options != null)
                    {
                        var enableDebugField = options.GetType().GetProperty("EnableBurstDebug");
                        if (enableDebugField != null)
                        {
                            enableDebugField.SetValue(options, false);
                            UnityEngine.Debug.Log("[DisableBurstDebugInfo] Burst debug info disabled for build");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[DisableBurstDebugInfo] Build-time disable failed: {ex.Message}");
        }
        #endif
    }
}

