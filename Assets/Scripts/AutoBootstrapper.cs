using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoBootstrapper : MonoBehaviour
{
    void Awake()
    {
        // Ensure there is a singleton GameObject with MRUKRoomExporter and components
        var existing = FindFirstObjectByType<MRUKRoomExporter>();
        if (existing == null)
        {
            var go = new GameObject("QuestHouseDesign");
            
            // Core exporter
            var exporter = go.AddComponent<MRUKRoomExporter>();
            exporter.exportFolder = "QuestHouseDesign";
            exporter.exportOBJ = true;
            exporter.exportGLB = false;
            exporter.exportSVGFloorPlans = true;

            // View modes
            var viewMode = go.AddComponent<ViewModeController>();
            var dollHouse = go.AddComponent<DollHouseVisualizer>();
            viewMode.dollHouseRoot = new GameObject("DollHouseRoot");
            viewMode.dollHouseRoot.transform.SetParent(go.transform);
            dollHouse.transform.SetParent(viewMode.dollHouseRoot.transform);

            // VR Control Panel
            var vrPanel = go.AddComponent<VRControlPanel>();
            vrPanel.roomExporter = exporter;
            vrPanel.viewModeController = viewMode;

            // HTTP Server for WiFi downloads
            var httpServer = go.AddComponent<SimpleHttpServer>();

            // Runtime logger for debugging
            var logger = go.AddComponent<RuntimeLogger>();

            DontDestroyOnLoad(go);
            
            Debug.Log("QuestHouseDesign initialized successfully");
        }
    }
}
