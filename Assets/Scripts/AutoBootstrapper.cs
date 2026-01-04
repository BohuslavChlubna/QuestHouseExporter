using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoBootstrapper : MonoBehaviour
{
    void Awake()
    {
        // Ensure there is a singleton GameObject with ScannerExporter and ExporterUI
        var existing = FindObjectOfType<ScannerExporter>();
        if (existing == null)
        {
            var go = new GameObject("QuestExporter");
            var se = go.AddComponent<ScannerExporter>();
            var ui = go.AddComponent<ExporterUI>();
            var ci = go.AddComponent<ControllerInputExporter>();
            var wsp = go.AddComponent<WorldSpacePanel>();
            var cwp = go.AddComponent<ControllerWorldPointer>();
            // default settings
            se.exportFolder = "QuestHouseExport";
            se.exportOBJ = true;
            se.exportGLB = true;
            se.runHttpServer = false;

            ci.useRightController = true;
            ci.exportButtonLabel = "Primary Button (A/X) = Export";
            ci.toggleServerLabel = "Secondary Button (B/Y) = Toggle Server";
            // connect world space components
            cwp.useRight = true;
            cwp.panel = wsp;

            DontDestroyOnLoad(go);
        }
    }
}
