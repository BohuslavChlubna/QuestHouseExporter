using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;

public class MenuController : MonoBehaviour
{
    [Header("TEST MODE - Disable visualizations for debugging")]
    public bool testModeSimpleUI = false; // Set TRUE to test just UI without dollhouse
    
    public MRUKRoomExporter roomExporter;
    public ViewModeController viewModeController;
    public DollHouseVisualizer dollHouseVisualizer;
    public InRoomWallVisualizer inRoomWallVisualizer;
    
    private GameObject menuCanvas;
    private TextMeshProUGUI statusText;
    private Button scanButton;
    private Button exportButton;
    private Button reloadButton;
    
    // Offline cached room data from RoomDataStorage
    private List<RoomData> offlineRooms = new List<RoomData>();
    
    void Start()
    {
        Debug.Log("[MenuController] Start() called (will wait for Show() call)");
    }
    
    public void Show()
    {
        Debug.Log("[MenuController] Show() called, creating UI");
        CreateMenuUI();
        
        // Load offline room data (NO MRUK dependency at startup!)
        LoadOfflineRooms();
        
        // Auto-generate visualizations from offline data
        GenerateInitialVisualizations();
    }
    
    void LoadOfflineRooms()
    {
        offlineRooms.Clear();
        
        if (RoomDataStorage.Instance != null)
        {
            offlineRooms = RoomDataStorage.Instance.GetCachedRooms();
            Debug.Log($"[MenuController] Loaded {offlineRooms.Count} offline rooms");
            RuntimeLogger.WriteLine($"[MenuController] Loaded {offlineRooms.Count} room(s) from offline cache");
        }
        else
        {
            Debug.LogError("[MenuController] RoomDataStorage not found!");
        }
    }
    
    void GenerateInitialVisualizations()
    {
        if (testModeSimpleUI)
        {
            Debug.Log("[MenuController] TEST MODE - Skipping visualizations");
            RuntimeLogger.WriteLine("[MenuController] Running in TEST MODE - UI only, no visualizations");
            return;
        }
        
        Debug.Log("[MenuController] Production mode - Scheduling visualization generation (delayed to prevent startup hang)");
        RuntimeLogger.WriteLine("[MenuController] Production mode - Visualizations will be generated after UI is ready");
        
        // IMPORTANT: Delay visualization generation to prevent startup hang
        // This allows the UI to show first, then visualizations are created asynchronously
        StartCoroutine(DelayedVisualizationGeneration());
    }
    
    System.Collections.IEnumerator DelayedVisualizationGeneration()
    {
        // Wait for UI to be fully shown
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[MenuController] Starting delayed visualization generation from offline data");
        
        bool success = true;
        string errorMessage = "";
        
        // Generate DollHouse
        if (dollHouseVisualizer != null && dollHouseVisualizer.enabled)
        {
            try
            {
                Debug.Log("[MenuController] Generating doll house...");
                dollHouseVisualizer.GenerateDollHouseFromOfflineData(offlineRooms);
                Debug.Log("[MenuController] Doll house generated");
            }
            catch (System.Exception ex)
            {
                success = false;
                errorMessage = $"DollHouse error: {ex.Message}";
                Debug.LogError($"[MenuController] DollHouse generation failed: {ex.Message}");
                RuntimeLogger.LogException(ex);
            }
        }
        else
        {
            Debug.LogWarning("[MenuController] DollHouseVisualizer is disabled or null");
        }
        
        // Wait a frame to prevent frame drop
        yield return null;
        
        // Generate InRoom walls
        if (inRoomWallVisualizer != null && inRoomWallVisualizer.enabled)
        {
            try
            {
                Debug.Log("[MenuController] Generating in-room walls...");
                inRoomWallVisualizer.GenerateWallsFromOfflineData(offlineRooms);
                Debug.Log("[MenuController] In-room walls generated");
            }
            catch (System.Exception ex)
            {
                success = false;
                errorMessage = $"InRoom error: {ex.Message}";
                Debug.LogError($"[MenuController] InRoom generation failed: {ex.Message}");
                RuntimeLogger.LogException(ex);
            }
        }
        else
        {
            Debug.LogWarning("[MenuController] InRoomWallVisualizer is disabled or null");
        }
        
        // Update status
        if (success)
        {
            UpdateStatusAfterVisualization();
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = $"Visualization error!\n{errorMessage}\nUI still works.";
                statusText.color = Color.red;
            }
        }
    }
    
    void UpdateStatusAfterVisualization()
    {
        if (statusText != null)
        {
            if (offlineRooms.Count > 0)
            {
                bool isDefaultRoom = offlineRooms.Count == 1 && 
                                    offlineRooms[0].roomId == "default_test_room";
                
                if (isDefaultRoom)
                {
                    statusText.text = "Using test room (4x5m)\nClick 'Reload Rooms' to scan real rooms";
                    statusText.color = new Color(1f, 0.8f, 0.2f);
                }
                else
                {
                    statusText.text = $"Ready! {offlineRooms.Count} room(s) loaded";
                    statusText.color = Color.cyan;
                }
            }
            else
            {
                statusText.text = "No rooms found - Click 'Reload Rooms' to scan";
                statusText.color = new Color(1f, 0.5f, 0f);
            }
        }
    }

    void CreateMenuUI()
    {
        // Create Canvas
        menuCanvas = new GameObject("MenuCanvas");
        menuCanvas.transform.SetParent(transform);
        
        var canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        var canvasScaler = menuCanvas.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 10;
        
        menuCanvas.AddComponent<GraphicRaycaster>();
        
        // Position canvas in front of user
        menuCanvas.transform.localPosition = new Vector3(0, 1.5f, 2f);
        menuCanvas.transform.localRotation = Quaternion.identity;
        
        var rectTransform = menuCanvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(800, 700);
        rectTransform.localScale = Vector3.one * 0.001f;
        
        // Background Panel
        var bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(menuCanvas.transform, false);
        
        var bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        var bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Title Text
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(menuCanvas.transform, false);
        
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Quest House Design";
        titleText.fontSize = 48;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -50);
        titleRect.sizeDelta = new Vector2(700, 80);
        
        // Status Text
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(menuCanvas.transform, false);
        
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Initializing...";
        statusText.fontSize = 32;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.cyan;
        
        var statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0, 50);
        statusRect.sizeDelta = new Vector2(700, 60);
        
        // Scan Button
        scanButton = CreateButton("ScanButton", "Request Room Scan", new Vector2(0, -50), OnScanButtonPressed);
        
        // Export Button
        exportButton = CreateButton("ExportButton", "Export Room Data", new Vector2(0, -150), OnExportButtonPressed);
        exportButton.interactable = true;
        
        // Reload Rooms Button
        reloadButton = CreateButton("ReloadButton", "Reload Rooms", new Vector2(0, -250), OnReloadRoomsPressed);
        
        // View Mode Toggle Button
        if (viewModeController != null)
        {
            CreateButton("ViewModeButton", "Toggle View Mode", new Vector2(0, -350), OnToggleViewMode);
        }
        
        Debug.Log("[MenuController] UI created successfully");
    }

    Button CreateButton(string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(menuCanvas.transform, false);
        
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(600, 80);
        
        // Add Image component with explicit null sprite
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        buttonImage.sprite = null; // Unity creates default white sprite
        
        // Add Button component after Image
        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // Button Text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 36;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        button.onClick.AddListener(onClick);
        
        return button;
    }

    void OnScanButtonPressed()
    {
        Debug.Log("[MenuController] Scan button pressed - requesting room scan");
        statusText.text = "Requesting room scan...";
        
        if (roomExporter != null)
        {
            // Trigger room scan via MRUK
            // This will request the Meta Quest Scene API to provide room data
            StartCoroutine(RequestRoomScan());
        }
        else
        {
            Debug.LogError("[MenuController] RoomExporter is null!");
            statusText.text = "ERROR: Room exporter not found";
        }
    }

    void OnExportButtonPressed()
    {
        Debug.Log("[MenuController] Export button pressed");
        
        if (offlineRooms == null || offlineRooms.Count == 0)
        {
            statusText.text = "ERROR: No room data!";
            Debug.LogError("[MenuController] No rooms to export. Try Reload Rooms first.");
            return;
        }
        
        statusText.text = "Exporting room data...";
        
        if (roomExporter != null)
        {
            // Export from offline data (not MRUK)
            roomExporter.ExportFromOfflineRooms(offlineRooms);
            statusText.text = $"Export complete! {offlineRooms.Count} room(s) exported.";
            RuntimeLogger.WriteLine($"[MenuController] Exported {offlineRooms.Count} offline rooms");
        }
        else
        {
            Debug.LogError("[MenuController] RoomExporter is null!");
            statusText.text = "ERROR: Room exporter not found";
        }
    }

    System.Collections.IEnumerator RequestRoomScan()
    {
        // Wait a frame for the request to process
        yield return null;
        
        // TODO: Integrate with MRUK Scene API to request room scan
        // For now, just enable export button as if scan completed
        statusText.text = "Room scan requested!\nUse Quest menu to scan your room.";
        
        // Enable export button after "scan" (in real implementation, this would be triggered by MRUK callback)
        yield return new WaitForSeconds(2f);
        
        exportButton.interactable = true;
        statusText.text = "Room data loaded. Ready to export.";
    }

    void OnReloadRoomsPressed()
    {
        Debug.Log("[MenuController] Reload Rooms button pressed - NOW INITIALIZING MRUK!");
        statusText.text = "Scanning rooms via MRUK...";
        
        // THIS IS THE ONLY PLACE WHERE WE ACCESS MRUK!
        StartCoroutine(ReloadRoomsFromMRUK());
    }
    
    System.Collections.IEnumerator ReloadRoomsFromMRUK()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Check if MRUK is ready
        if (MRUK.Instance == null)
        {
            statusText.text = "MRUK not initialized!\nScan rooms in Quest settings first";
            statusText.color = Color.red;
            RuntimeLogger.WriteLine("[MenuController] MRUK not found - cannot reload rooms");
            yield break;
        }
        
        if (MRUK.Instance.Rooms == null || MRUK.Instance.Rooms.Count == 0)
        {
            statusText.text = "No rooms scanned!\nUse Quest settings to scan your room";
            statusText.color = Color.red;
            RuntimeLogger.WriteLine("[MenuController] MRUK has no rooms - user needs to scan");
            yield break;
        }
        
        // Convert MRUK rooms to RoomData
        List<RoomData> newRooms = ConvertMRUKRoomsToOfflineData(MRUK.Instance.Rooms);
        
        // Save to persistent storage
        if (RoomDataStorage.Instance != null)
        {
            RoomDataStorage.Instance.SaveRooms(newRooms);
        }
        
        // Update offline cache
        offlineRooms = newRooms;
        
        // Clear and regenerate visualizations
        if (dollHouseVisualizer != null)
        {
            dollHouseVisualizer.ClearDollHouse();
            dollHouseVisualizer.GenerateDollHouseFromOfflineData(offlineRooms);
        }
        
        if (inRoomWallVisualizer != null)
        {
            inRoomWallVisualizer.ClearWalls();
            inRoomWallVisualizer.GenerateWallsFromOfflineData(offlineRooms);
        }
        
        statusText.text = $"Rooms reloaded! {offlineRooms.Count} room(s) scanned";
        statusText.color = Color.green;
        
        RuntimeLogger.WriteLine($"[MenuController] Successfully reloaded {offlineRooms.Count} rooms from MRUK");
    }
    
    
    List<RoomData> ConvertMRUKRoomsToOfflineData(List<MRUKRoom> mrukRooms)
    {
        var result = new List<RoomData>();
        
        foreach (var mrukRoom in mrukRooms)
        {
            var roomData = new RoomData
            {
                roomId = mrukRoom.Anchor != null ? mrukRoom.Anchor.Uuid.ToString() : System.Guid.NewGuid().ToString(),
                roomName = mrukRoom.name ?? "Unnamed Room",
                ceilingHeight = 2.5f
            };
            
            // Get ceiling height
            if (mrukRoom.CeilingAnchor != null && mrukRoom.FloorAnchor != null)
            {
                roomData.ceilingHeight = Mathf.Abs(
                    mrukRoom.CeilingAnchor.transform.position.y - 
                    mrukRoom.FloorAnchor.transform.position.y
                );
                
                // NEW: Check for sloped ceiling (FW83+)
                if (mrukRoom.CeilingAnchor.PlaneBoundary2D != null && mrukRoom.CeilingAnchor.PlaneBoundary2D.Count > 0)
                {
                    roomData.hasSlopedCeiling = true;
                    roomData.ceilingBoundary = new List<Vector3>();
                    
                    Vector3 ceilingPos = mrukRoom.CeilingAnchor.transform.position;
                    Quaternion ceilingRot = mrukRoom.CeilingAnchor.transform.rotation;
                    
                    foreach (var pt2D in mrukRoom.CeilingAnchor.PlaneBoundary2D)
                    {
                        // Convert to world coordinates
                        Vector3 worldPos = ceilingPos + ceilingRot * new Vector3(pt2D.x, 0, pt2D.y);
                        roomData.ceilingBoundary.Add(worldPos);
                    }
                    
                    RuntimeLogger.WriteLine($"  Detected SLOPED ceiling in {roomData.roomName} with {roomData.ceilingBoundary.Count} vertices");
                }
            }
            
            // Convert floor boundary
            if (mrukRoom.FloorAnchor != null && mrukRoom.FloorAnchor.PlaneBoundary2D != null)
            {
                var boundary2D = mrukRoom.FloorAnchor.PlaneBoundary2D;
                Vector3 roomPos = mrukRoom.FloorAnchor.transform.position;
                Quaternion roomRot = mrukRoom.FloorAnchor.transform.rotation;
                
                roomData.floorBoundary = new List<Vector3>();
                foreach (var pt2D in boundary2D)
                {
                    Vector3 worldPos = roomPos + roomRot * new Vector3(pt2D.x, 0, pt2D.y);
                    roomData.floorBoundary.Add(worldPos);
                }
                
                // Create walls from boundary
                roomData.walls = new List<WallData>();
                for (int i = 0; i < boundary2D.Count; i++)
                {
                    Vector2 p1 = boundary2D[i];
                    Vector2 p2 = boundary2D[(i + 1) % boundary2D.Count];
                    
                    Vector3 start = roomPos + roomRot * new Vector3(p1.x, 0, p1.y);
                    Vector3 end = roomPos + roomRot * new Vector3(p2.x, 0, p2.y);
                    
                    var wall = new WallData
                    {
                        start = start,
                        end = end,
                        height = roomData.ceilingHeight,
                        attachedAnchors = new List<AnchorData>()
                    };
                    
                    roomData.walls.Add(wall);
                }
            }
            
            // Convert anchors (windows, doors, furniture)
            roomData.anchors = new List<AnchorData>();
            foreach (var anchor in mrukRoom.Anchors)
            {
                if (anchor == mrukRoom.FloorAnchor || anchor == mrukRoom.CeilingAnchor)
                    continue;
                
                var anchorData = new AnchorData
                {
                    anchorType = anchor.Label.ToString(),
                    position = anchor.transform.position,
                    rotation = anchor.transform.rotation,
                    scale = anchor.VolumeBounds.HasValue ? anchor.VolumeBounds.Value.size : Vector3.one * 0.5f
                };
                
                roomData.anchors.Add(anchorData);
                
                // Attach anchor to closest wall
                AttachAnchorToWall(anchorData, roomData.walls);
            }
            
            result.Add(roomData);
            RuntimeLogger.WriteLine($"Converted room: {roomData.roomName} - {roomData.walls.Count} walls, {roomData.anchors.Count} anchors");
        }
        
        return result;
    }
    
    void AttachAnchorToWall(AnchorData anchor, List<WallData> walls)
    {
        if (walls == null || walls.Count == 0) return;
        
        // Find closest wall
        float minDist = float.MaxValue;
        WallData closestWall = null;
        
        foreach (var wall in walls)
        {
            Vector3 wallMid = (wall.start + wall.end) * 0.5f;
            float dist = Vector3.Distance(anchor.position, wallMid);
            
            if (dist < minDist)
            {
                minDist = dist;
                closestWall = wall;
            }
        }
        
        if (closestWall != null)
        {
            closestWall.attachedAnchors.Add(anchor);
        }
    }
    
    
    void OnToggleViewMode()
    {
        Debug.Log("[MenuController] Toggle view mode pressed");
        if (viewModeController != null)
        {
            viewModeController.ToggleMode();
            statusText.text = $"View mode: {viewModeController.currentMode}";
        }
    }

    void Update()
    {
        // Make canvas always face the camera
        if (Camera.main != null && menuCanvas != null)
        {
            menuCanvas.transform.LookAt(Camera.main.transform);
            menuCanvas.transform.Rotate(0, 180, 0); // Flip to face user
        }
    }
}


