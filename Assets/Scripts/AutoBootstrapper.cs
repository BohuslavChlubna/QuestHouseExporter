using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoBootstrapper : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[AutoBootstrapper] Awake() called");
        
        // Ensure there is a singleton GameObject with MRUKRoomExporter and components
        var existing = FindFirstObjectByType<MRUKRoomExporter>();
        if (existing == null)
        {
            Debug.Log("[AutoBootstrapper] Creating QuestHouseDesign GameObject...");
            var go = new GameObject("QuestHouseDesign");
            
            Debug.Log("[AutoBootstrapper] Adding MRUKRoomExporter...");
            // Core exporter
            var exporter = go.AddComponent<MRUKRoomExporter>();
            exporter.exportFolder = "QuestHouseDesign";
            exporter.exportOBJ = true;
            exporter.exportGLB = false;
            exporter.exportSVGFloorPlans = true;

            // TODO: Re-enable these when we have proper shader support
            // Debug.Log("[AutoBootstrapper] Adding ViewModeController and DollHouseVisualizer...");
            // // View modes
            // var viewMode = go.AddComponent<ViewModeController>();
            // var dollHouse = go.AddComponent<DollHouseVisualizer>();
            // viewMode.dollHouseRoot = new GameObject("DollHouseRoot");
            // viewMode.dollHouseRoot.transform.SetParent(go.transform);
            // dollHouse.transform.SetParent(viewMode.dollHouseRoot.transform);

            // Debug.Log("[AutoBootstrapper] Adding VRControlPanel...");
            // // VR Control Panel
            // var vrPanel = go.AddComponent<VRControlPanel>();
            // vrPanel.roomExporter = exporter;
            // vrPanel.viewModeController = viewMode;

            Debug.Log("[AutoBootstrapper] Adding MenuController (simple UI)...");
            // Simple UI Menu
            var menuController = go.AddComponent<MenuController>();
            menuController.roomExporter = exporter;

            // Note: SimpleHttpServer and RuntimeLogger are not MonoBehaviour components
            // They are initialized automatically when needed (RuntimeLogger in MRUKRoomExporter)

            DontDestroyOnLoad(go);
            
            Debug.Log("[AutoBootstrapper] QuestHouseDesign initialized successfully!");
        }
        else
        {
            Debug.Log("[AutoBootstrapper] QuestHouseDesign already exists, skipping initialization");
        }
    }
}
