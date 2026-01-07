using UnityEngine;
using System.Text;

/// <summary>
/// Diagnostic script to log startup issues and prevent hangs
/// Runs BEFORE AutoBootstrapper
/// </summary>
[DefaultExecutionOrder(-200)]
public class StartupDiagnostics : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("=== STARTUP DIAGNOSTICS BEGIN ===");
        RuntimeLogger.WriteLine("=== STARTUP DIAGNOSTICS ===");
        
        var sb = new StringBuilder();
        sb.AppendLine("System Information:");
        sb.AppendLine($"  Platform: {Application.platform}");
        sb.AppendLine($"  Unity Version: {Application.unityVersion}");
        sb.AppendLine($"  Device Model: {SystemInfo.deviceModel}");
        sb.AppendLine($"  OS: {SystemInfo.operatingSystem}");
        sb.AppendLine($"  Graphics: {SystemInfo.graphicsDeviceName}");
        
        Debug.Log(sb.ToString());
        RuntimeLogger.WriteLine(sb.ToString());
        
        // Check for critical shaders
        CheckShaderAvailability();
        
        // Check MRUK availability (should NOT block!)
        CheckMRUKStatus();
        
        Debug.Log("=== STARTUP DIAGNOSTICS COMPLETE ===");
        RuntimeLogger.WriteLine("=== DIAGNOSTICS COMPLETE - Proceeding to AutoBootstrapper ===");
    }
    
    void CheckShaderAvailability()
    {
        Debug.Log("[Diagnostics] Checking shader availability...");
        
        string[] criticalShaders = new string[]
        {
            "QuestHouse/UnlitColor",
            "Unlit/Color",
            "Standard"
        };
        
        bool foundShader = false;
        foreach (var shaderName in criticalShaders)
        {
            var shader = Shader.Find(shaderName);
            if (shader != null)
            {
                Debug.Log($"[Diagnostics] ? Found shader: {shaderName}");
                RuntimeLogger.WriteLine($"Shader available: {shaderName}");
                foundShader = true;
            }
            else
            {
                Debug.LogWarning($"[Diagnostics] ? Missing shader: {shaderName}");
            }
        }
        
        if (!foundShader)
        {
            Debug.LogError("[Diagnostics] CRITICAL: No shaders found! Visualizations will fail!");
            RuntimeLogger.WriteLine("ERROR: No shaders available - visualizations will be disabled");
        }
    }
    
    void CheckMRUKStatus()
    {
        Debug.Log("[Diagnostics] Checking MRUK status (non-blocking)...");
        
        try
        {
            // Check if MRUK class exists (without instantiating)
            var mrukType = System.Type.GetType("Meta.XR.MRUtilityKit.MRUK, Meta.XR.MRUtilityKit");
            if (mrukType != null)
            {
                Debug.Log("[Diagnostics] MRUK library found");
                RuntimeLogger.WriteLine("MRUK library: Available");
            }
            else
            {
                Debug.LogWarning("[Diagnostics] MRUK library not found");
                RuntimeLogger.WriteLine("MRUK library: Not available");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Diagnostics] MRUK check failed (non-critical): {ex.Message}");
        }
        
        // IMPORTANT: Do NOT access MRUK.Instance here - it can block!
        Debug.Log("[Diagnostics] Note: MRUK.Instance will only be accessed on user demand (Reload Rooms button)");
    }
}
