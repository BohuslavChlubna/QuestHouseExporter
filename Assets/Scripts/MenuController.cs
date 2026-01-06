using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;

public class MenuController : MonoBehaviour
{
    public MRUKRoomExporter roomExporter;
    public ViewModeController viewModeController;
    public DollHouseVisualizer dollHouseVisualizer;
    public InRoomWallVisualizer inRoomWallVisualizer;
    
    private GameObject menuCanvas;
    private TextMeshProUGUI statusText;
    private Button scanButton;
    private Button exportButton;
    private Button reloadButton;
    
    // Cached room data - snapshot of MRUK rooms at time of Show() or Reload
    private List<MRUKRoom> cachedRooms = new List<MRUKRoom>();
    
    void Start()
    {
        Debug.Log("[MenuController] Start() called (will wait for Show() call)");
    }
    
    public void Show()
    {
        Debug.Log("[MenuController] Show() called, creating UI");
        CreateMenuUI();
        
        // Cache current MRUK rooms (snapshot at startup)
        CacheCurrentRooms();
        
        // Auto-generate visualizations when menu is shown (rooms are already loaded)
        GenerateInitialVisualizations();
    }
    
    void CacheCurrentRooms()
    {
        cachedRooms.Clear();
        
        if (MRUK.Instance != null && MRUK.Instance.Rooms != null)
        {
            cachedRooms.AddRange(MRUK.Instance.Rooms);
            Debug.Log($"[MenuController] Cached {cachedRooms.Count} rooms from MRUK");
            RuntimeLogger.WriteLine($"[MenuController] Room data cached: {cachedRooms.Count} rooms");
        }
        else
        {
            Debug.LogWarning("[MenuController] No rooms to cache from MRUK");
        }
    }
    
    void GenerateInitialVisualizations()
    {
        Debug.Log("[MenuController] Generating initial visualizations");
        
        if (dollHouseVisualizer != null)
        {
            dollHouseVisualizer.GenerateDollHouse();
            Debug.Log("[MenuController] Doll house generated");
        }
        
        if (inRoomWallVisualizer != null)
        {
            inRoomWallVisualizer.GenerateWallOutlines();
            Debug.Log("[MenuController] In-room walls generated");
        }
        
        // Update status to show cached rooms are ready
        if (statusText != null)
        {
            statusText.text = cachedRooms.Count > 0 
                ? $"Ready! {cachedRooms.Count} room(s) loaded" 
                : "No rooms found";
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
        reloadButton = CreateButton("ReloadButton", "?? Reload Rooms", new Vector2(0, -250), OnReloadRoomsPressed);
        
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
        
        if (cachedRooms == null || cachedRooms.Count == 0)
        {
            statusText.text = "ERROR: No cached room data!";
            Debug.LogError("[MenuController] No cached rooms to export. Try Reload Rooms first.");
            return;
        }
        
        statusText.text = "Exporting room data...";
        
        if (roomExporter != null)
        {
            // Export from cached rooms (snapshot), not live MRUK data
            roomExporter.ExportFromCachedRooms(cachedRooms);
            statusText.text = $"Export complete! {cachedRooms.Count} room(s) exported.";
            RuntimeLogger.WriteLine($"[MenuController] Exported {cachedRooms.Count} cached rooms");
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
        Debug.Log("[MenuController] Reload Rooms button pressed");
        statusText.text = "Reloading rooms...";
        
        // Clear existing visualizations
        if (dollHouseVisualizer != null)
        {
            dollHouseVisualizer.ClearDollHouse();
        }
        
        if (inRoomWallVisualizer != null)
        {
            inRoomWallVisualizer.ClearWalls();
        }
        
        // Refresh cache with current MRUK data (new snapshot)
        CacheCurrentRooms();
        
        // Regenerate visualizations with newly cached data
        GenerateInitialVisualizations();
        
        // Update status
        statusText.text = $"Rooms reloaded! {cachedRooms.Count} room(s) found";
        
        RuntimeLogger.WriteLine($"[MenuController] Rooms reloaded and cached: {cachedRooms.Count} rooms");
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
