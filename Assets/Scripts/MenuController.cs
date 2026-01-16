using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;

public class MenuController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button exportButton;
    [SerializeField] private Button reloadButton;
    [SerializeField] private Button viewModeButton;
    [SerializeField] private Button toggleDollhouseButton;
    public MRUKRoomExporter roomExporter;
    public ViewModeController viewModeController;
    public DollHouseVisualizer dollHouseVisualizer;
    public InRoomWallVisualizer inRoomWallVisualizer;
    // Nepoužívané promìnné pro dynamické UI odstranìny
    private List<RoomData> offlineRooms = new List<RoomData>();
    private int currentDollHouseIndex = 0;

    /// <summary>
    /// Cyklicky pøepíná viditelnost dollhouse objektù pod DollHouseVisualizer (volat z UI tlaèítka)
    /// </summary>
    public void ToggleDollHouse()
    {
        if (dollHouseVisualizer == null) return;
        int count = dollHouseVisualizer.transform.childCount;
        if (count == 0) return;
        currentDollHouseIndex = (currentDollHouseIndex + 1) % count;
        for (int i = 0; i < count; i++)
        {
            var child = dollHouseVisualizer.transform.GetChild(i).gameObject;
            child.SetActive(i == currentDollHouseIndex);
        }
        Debug.Log($"[MenuController] Toggled to dollhouse index: {currentDollHouseIndex}");
    }
    
    void Start()
    {
        Debug.Log($"[MenuController] Start() called (will wait for Show() call) on {gameObject.name} (ID: {GetInstanceID()})");
        Debug.Log($"[MenuController] Start() statusText: {(statusText == null ? "NULL" : statusText.gameObject.name)}");
        Show();
    }
    
    public void Show()
    {
        Debug.Log($"[MenuController] Show() called on {gameObject.name} (ID: {GetInstanceID()})");
        Debug.Log($"[MenuController] Show() statusText: {(statusText == null ? "NULL" : statusText.gameObject.name)}");

        // Load offline room data (NO MRUK dependency at startup!)
        LoadOfflineRooms();

        GenerateDollhouseBackground();
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
        Debug.Log($"[MenuController] GenerateInitialVisualizations() on {gameObject.name} (ID: {GetInstanceID()})");
        Debug.Log($"[MenuController] GenerateInitialVisualizations() statusText: {(statusText == null ? "NULL" : statusText.gameObject.name)}");
        Debug.Log("[MenuController] Production mode - Scheduling visualization generation (delayed to prevent startup hang)");
        RuntimeLogger.WriteLine("[MenuController] Production mode - Visualizations will be generated after UI is ready");
        // IMPORTANT: Delay visualization generation to prevent startup hang
        // This allows the UI to show first, then visualizations are created asynchronously
        StartCoroutine(DelayedVisualizationGeneration());
    }
    
    System.Collections.IEnumerator DelayedVisualizationGeneration()
    {
        Debug.Log("[MenuController] DelayedVisualizationGeneration START");
        // Wait for UI to be fully shown
        yield return new WaitForSeconds(0.5f);

        // Wait until visualizer komponenty jsou aktivní a povolené
        yield return new WaitUntil(() =>
            dollHouseVisualizer != null && dollHouseVisualizer.enabled);

        Debug.Log("[MenuController] Starting delayed visualization generation from offline data");

        bool success = true;
        string errorMessage = "";

        // Generate DollHouse (async per room)
        if (dollHouseVisualizer != null && dollHouseVisualizer.enabled)
        {
            Debug.Log("[MenuController] Generating doll house (async)...");
            System.Exception dollhouseEx = null;
            bool dollhouseSuccess = true;
            yield return StartCoroutine(DollhouseTryCatchWrapper(offlineRooms, ex => { dollhouseEx = ex; dollhouseSuccess = false; }));
            if (!dollhouseSuccess)
            {
                success = false;
                errorMessage = $"DollHouse error: {dollhouseEx.Message}";
                Debug.LogError($"[MenuController] DollHouse generation failed: {dollhouseEx.Message}");
                RuntimeLogger.LogException(dollhouseEx);
            }
            else
            {
                Debug.Log("[MenuController] Doll house generated (async)");
            }
        }
        else
        {
            Debug.LogWarning("[MenuController] DollHouseVisualizer is disabled or null");
        }

        // Wait a frame to prevent frame drop
        yield return null;

        // Generate InRoom walls (async per room)
        if (inRoomWallVisualizer != null && inRoomWallVisualizer.enabled)
        {
            Debug.Log("[MenuController] Generating in-room walls (async)...");
            System.Exception inroomEx = null;
            bool inroomSuccess = true;
            yield return StartCoroutine(InRoomTryCatchWrapper(offlineRooms, ex => { inroomEx = ex; inroomSuccess = false; }));
            if (!inroomSuccess)
            {
                success = false;
                errorMessage = $"InRoom error: {inroomEx.Message}";
                Debug.LogError($"[MenuController] InRoom generation failed: {inroomEx.Message}");
                RuntimeLogger.LogException(inroomEx);
            }
            else
            {
                Debug.Log("[MenuController] In-room walls generated (async)");
            }
        }
        else
        {
            Debug.LogWarning("[MenuController] InRoomWallVisualizer is disabled or null");
        }

        // Update status
        if (success)
        {
            Debug.Log("[MenuController] DelayedVisualizationGeneration END: success");
            UpdateStatusAfterVisualization();
        }
        else
        {
            Debug.Log("[MenuController] DelayedVisualizationGeneration END: error");
            if (statusText != null)
            {
                statusText.text = $"Visualization error!\n{errorMessage}\nUI still works.";
                statusText.color = Color.red;
            }
        }
    }

    // Helper coroutine wrappers for try/catch with yield
    System.Collections.IEnumerator DollhouseTryCatchWrapper(List<RoomData> rooms, System.Action<System.Exception> onError)
    {
        var enumerator = dollHouseVisualizer.GenerateDollHouseFromOfflineDataAsync(rooms);
        while (true)
        {
            try
            {
                if (!enumerator.MoveNext()) break;
            }
            catch (System.Exception ex)
            {
                onError?.Invoke(ex);
                yield break;
            }
            yield return enumerator.Current;
        }
    }

    System.Collections.IEnumerator InRoomTryCatchWrapper(List<RoomData> rooms, System.Action<System.Exception> onError)
    {
        var enumerator = inRoomWallVisualizer.GenerateWallsFromOfflineDataAsync(rooms);
        while (true)
        {
            try
            {
                if (!enumerator.MoveNext()) break;
            }
            catch (System.Exception ex)
            {
                onError?.Invoke(ex);
                yield break;
            }
            yield return enumerator.Current;
        }
    }
    
    void UpdateStatusAfterVisualization()
    {
        Debug.Log($"[MenuController] UpdateStatusAfterVisualization() called, statusText is null: {statusText == null}, obj: {(statusText != null ? statusText.gameObject.name : "NULL")}");
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
                    Debug.Log($"[MenuController] statusText set to: {statusText.text}");
                }
                else
                {
                    statusText.text = $"Ready! {offlineRooms.Count} room(s) loaded";
                    statusText.color = Color.cyan;
                    Debug.Log($"[MenuController] statusText set to: {statusText.text}");
                }
            }
            else
            {
                statusText.text = "No rooms found - Click 'Reload Rooms' to scan";
                statusText.color = new Color(1f, 0.5f, 0f);
                Debug.Log($"[MenuController] statusText set to: {statusText.text}");
            }
        }
    }

    

    public void OnScanButtonPressed()
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

    public void OnExportButtonPressed()
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

    public void OnReloadRoomsPressed()
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
    
    
    public void OnToggleViewMode()
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
        // (Dynamické otáèení canvasu bylo odstranìno, statické UI ve scénì)
    }
    
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }

    // Vygeneruje a umístí dollhouse na pozadí menu (pouze pokud není testModeSimpleUI)
    void GenerateDollhouseBackground()
    {
        if (dollHouseVisualizer != null && offlineRooms != null && offlineRooms.Count > 0)
        {
            dollHouseVisualizer.ClearDollHouse();
            dollHouseVisualizer.scale = 0.18f;
            if (dollHouseVisualizer.roomMaterial != null)
            {
                dollHouseVisualizer.roomMaterial.color = new Color(0.2f, 0.7f, 1f, 0.8f);
            }
            dollHouseVisualizer.GenerateDollHouseFromOfflineData(offlineRooms);
            // Pozice a rotace se nyní neøeší v kódu, používá se placement objekt ze scény
            Debug.Log($"[MenuController] Dollhouse generated, scale {dollHouseVisualizer.scale}");
        }
    }
}


