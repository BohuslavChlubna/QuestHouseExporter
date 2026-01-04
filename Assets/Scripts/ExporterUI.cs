using UnityEngine;

[RequireComponent(typeof(ScannerExporter))]
public class ExporterUI : MonoBehaviour
{
    ScannerExporter exporter;
    void Start() => exporter = GetComponent<ScannerExporter>();

    void OnGUI()
    {
        var rect = new Rect(10, Screen.height - 110, 220, 100);
        GUI.Box(rect, "Exporter");
        if (GUI.Button(new Rect(20, Screen.height - 90, 200, 30), "Export")) exporter.ExportAll();
        if (GUI.Button(new Rect(20, Screen.height - 50, 200, 30), exporter.runHttpServer ? "Stop Server" : "Start Server"))
        {
            if (exporter.runHttpServer) exporter.StopHttpServer(); else exporter.StartHttpServer();
            exporter.runHttpServer = !exporter.runHttpServer;
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
