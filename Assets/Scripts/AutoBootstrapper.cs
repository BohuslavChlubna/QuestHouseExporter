using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AutoBootstrapper : MonoBehaviour
{
    [Header("TEST MODE - Simple UI only")]
    public bool testModeSimpleUI = false; // Enable to test UI without visualizations
    
    void Awake()
    {
        Debug.Log("[AutoBootstrapper] Awake() called");
        
        if (testModeSimpleUI)
        {
            Debug.LogWarning("[AutoBootstrapper] TEST MODE ENABLED - UI only, no visualizations!");
            RuntimeLogger.WriteLine("[AutoBootstrapper] Running in TEST MODE");
        }
        
        var existing = FindFirstObjectByType<MRUKRoomExporter>();
        if (existing == null)
        {
            Debug.Log("[AutoBootstrapper] Creating QuestHouseDesign GameObject...");
            var go = new GameObject("QuestHouseDesign");
            
            // Add RoomDataStorage FIRST - NO MRUK dependency!
            Debug.Log("[AutoBootstrapper] Adding RoomDataStorage (offline mode)...");
            go.AddComponent<RoomDataStorage>();
            
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
            menuController.testModeSimpleUI = testModeSimpleUI; // Pass test mode flag
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
