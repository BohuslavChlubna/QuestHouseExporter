using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoBootstrapper : MonoBehaviour
{
    void Awake()
    {
        // Ensure there is a singleton GameObject with MRUKRoomExporter (new) and UI
        var existing = FindFirstObjectByType<MRUKRoomExporter>();
        if (existing == null)
        {
            var go = new GameObject("QuestHouseDesign");
            
            // New MRUK-based exporter
            var exporter = go.AddComponent<MRUKRoomExporter>();
            exporter.exportFolder = "QuestHouseDesign";
            exporter.exportOBJ = true;
            exporter.exportGLB = false;
            exporter.exportSVGFloorPlans = true;

            // View mode control
            var viewMode = go.AddComponent<ViewModeController>();
            var dollHouse = go.AddComponent<DollHouseVisualizer>();
            viewMode.dollHouseRoot = new GameObject("DollHouseRoot");
            viewMode.dollHouseRoot.transform.SetParent(go.transform);
            dollHouse.transform.SetParent(viewMode.dollHouseRoot.transform);

            // UI components
            var ui = go.AddComponent<ExporterUI>();
            var ci = go.AddComponent<ControllerInputExporter>();
            var wsp = go.AddComponent<WorldSpacePanel>();
            var cwp = go.AddComponent<ControllerWorldPointer>();

            ci.useRightController = true;
            ci.exportButtonLabel = "Primary Button (A/X) = Export";
            ci.toggleServerLabel = "Secondary Button (B/Y) = Toggle View Mode";
            
            cwp.useRight = true;
            cwp.panel = wsp;

            DontDestroyOnLoad(go);
        }
    }
}
