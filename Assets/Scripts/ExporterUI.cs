using UnityEngine;

/// <summary>
/// Legacy 2D UI for exporter - deprecated in favor of VRControlPanel.
/// Set enableLegacyUI to true to show this UI.
/// </summary>
[RequireComponent(typeof(MRUKRoomExporter))]
public class ExporterUI : MonoBehaviour
{
    public bool enableLegacyUI = false;
    
    MRUKRoomExporter exporter;
    void Start() => exporter = GetComponent<MRUKRoomExporter>();

    void OnGUI()
    {
        if (!enableLegacyUI) return; // Skip if legacy UI disabled
        var rect = new Rect(10, Screen.height - 110, 220, 100);
        GUI.Box(rect, "Exporter");
        if (GUI.Button(new Rect(20, Screen.height - 90, 200, 30), "Export")) exporter.ExportAll();
        // HTTP server removed - use view mode toggle instead
        var vm = GetComponent<ViewModeController>();
        if (vm != null && GUI.Button(new Rect(20, Screen.height - 50, 200, 30), "Toggle View Mode"))
        {
            vm.ToggleMode();
        }
        // Google Drive upload button
        if (exporter.enableDriveUpload && exporter.driveUploader != null)
        {
            if (GUI.Button(new Rect(20, Screen.height - 130, 200, 20), "Upload Last Export to Drive"))
            {
                string basePath = System.IO.Path.Combine(Application.persistentDataPath, exporter.exportFolder);
                exporter.driveUploader.StartUploadDirectory(basePath);
            }
        }

        // Device auth controls
        var auth = exporter.GetComponent<GoogleDeviceAuth>();
        if (auth != null)
        {
            if (GUI.Button(new Rect(20, Screen.height - 160, 200, 20), "Start Google Device Auth"))
            {
                auth.StartDeviceAuth();
            }
            if (!string.IsNullOrEmpty(auth.userCode))
            {
                GUI.Label(new Rect(20, Screen.height - 185, 400, 20), "Code: " + auth.userCode);
            }
        }
    }
}
