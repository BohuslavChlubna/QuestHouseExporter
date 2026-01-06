using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoBootstrapper : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[AutoBootstrapper] Awake() called");
        
        var existing = FindFirstObjectByType<MRUKRoomExporter>();
        if (existing == null)
        {
            Debug.Log("[AutoBootstrapper] Creating QuestHouseDesign GameObject...");
            var go = new GameObject("QuestHouseDesign");
            
            Debug.Log("[AutoBootstrapper] Adding MRUKRoomExporter...");
            var exporter = go.AddComponent<MRUKRoomExporter>();
            exporter.exportFolder = "QuestHouseDesign";
            exporter.exportOBJ = true;
            exporter.exportGLB = false;
            exporter.exportSVGFloorPlans = true;

            Debug.Log("[AutoBootstrapper] Adding ViewModeController and DollHouseVisualizer...");
            var dollHouseRoot = new GameObject("DollHouseRoot");
            dollHouseRoot.transform.SetParent(go.transform);
            var dollHouse = dollHouseRoot.AddComponent<DollHouseVisualizer>();
            
            var inRoomRoot = new GameObject("InRoomRoot");
            inRoomRoot.transform.SetParent(go.transform);
            var inRoomViz = inRoomRoot.AddComponent<InRoomWallVisualizer>();
            
            var viewMode = go.AddComponent<ViewModeController>();
            viewMode.dollHouseRoot = dollHouseRoot;
            viewMode.inRoomWallsRoot = inRoomRoot;

            Debug.Log("[AutoBootstrapper] Adding MenuController (will be shown after init)...");
            var menuController = go.AddComponent<MenuController>();
            menuController.roomExporter = exporter;
            menuController.viewModeController = viewMode;
            menuController.dollHouseVisualizer = dollHouse;
            menuController.inRoomWallVisualizer = inRoomViz;
            menuController.enabled = false;

            Debug.Log("[AutoBootstrapper] Adding InitializationScreen...");
            var initScreen = go.AddComponent<InitializationScreen>();
            initScreen.Show(() => {
                Debug.Log("[AutoBootstrapper] Initialization complete, enabling MenuController");
                menuController.enabled = true;
                menuController.Show();
            });

            DontDestroyOnLoad(go);
            
            Debug.Log("[AutoBootstrapper] QuestHouseDesign initialized successfully!");
        }
        else
        {
            Debug.Log("[AutoBootstrapper] QuestHouseDesign already exists, skipping initialization");
        }
    }
}
