using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to setup VR Control Panel with controller attachment.
/// </summary>
public class VRControlPanelSetup : EditorWindow
{
    [MenuItem("Tools/Setup VR Control Panel")]
    static void Setup()
    {
        // Find or create controller parent
        GameObject controllerParent = GameObject.Find("RightControllerAnchor");
        if (controllerParent == null)
        {
            // Try to find OVR rig
            var ovrRig = FindFirstObjectByType<OVRCameraRig>();
            if (ovrRig != null)
            {
                controllerParent = ovrRig.rightControllerAnchor?.gameObject;
            }
        }
        
        if (controllerParent == null)
        {
            // Create standalone controller tracker
            controllerParent = new GameObject("RightControllerTracker");
            Debug.LogWarning("OVRCameraRig not found. Created standalone tracker. Assign to controller anchor manually.");
        }
        
        // Add VRControlPanel component
        var panel = controllerParent.GetComponent<VRControlPanel>();
        if (panel == null)
        {
            panel = controllerParent.AddComponent<VRControlPanel>();
        }
        
        // Find references
        panel.roomExporter = FindFirstObjectByType<MRUKRoomExporter>();
        panel.viewModeController = FindFirstObjectByType<ViewModeController>();
        
        // Disable legacy UI components
        var exporterUI = FindFirstObjectByType<ExporterUI>();
        if (exporterUI != null)
        {
            exporterUI.enableLegacyUI = false;
            Debug.Log("Disabled legacy ExporterUI");
        }
        
        var controllerInput = FindFirstObjectByType<ControllerInputExporter>();
        if (controllerInput != null)
        {
            controllerInput.enableLegacyUI = false;
            Debug.Log("Disabled legacy ControllerInputExporter UI");
        }
        
        EditorUtility.SetDirty(panel);
        if (exporterUI != null) EditorUtility.SetDirty(exporterUI);
        if (controllerInput != null) EditorUtility.SetDirty(controllerInput);
        
        Debug.Log("VR Control Panel setup complete! Panel attached to: " + controllerParent.name);
        Debug.Log("Panel will appear on your controller at runtime with physical 3D buttons.");
    }
}
