using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Automatically disables Burst debug info generation to keep build folder clean.
/// The _BurstDebugInformation_DoNotShip folder is only needed for debugging Burst code.
/// </summary>
public class DisableBurstDebugInfo : IPreprocessBuildWithReport
{
    public int callbackOrder => -100; // Run early
    
    public void OnPreprocessBuild(BuildReport report)
    {
        // Only for Android Release builds
        if (report.summary.platform != BuildTarget.Android)
            return;
        
        #if UNITY_2021_2_OR_NEWER && ENABLE_BURST_AOT
        // Disable Burst debug info generation for release builds
        // This prevents creation of *_BurstDebugInformation_DoNotShip folders
        Unity.Burst.BurstCompiler.Options.EnableBurstDebug = false;
        UnityEngine.Debug.Log("[DisableBurstDebugInfo] Burst debug info disabled for cleaner build output");
        #endif
    }
}
