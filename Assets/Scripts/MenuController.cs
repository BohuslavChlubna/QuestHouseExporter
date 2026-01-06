using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    public MRUKRoomExporter roomExporter;
    
    private GameObject menuCanvas;
    private TextMeshProUGUI statusText;
    private Button scanButton;
    private Button exportButton;
    
    void Start()
    {
        Debug.Log("[MenuController] Creating UI...");
        CreateMenuUI();
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
        rectTransform.sizeDelta = new Vector2(800, 600);
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
        statusText.text = "Ready to scan room";
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
        exportButton.interactable = false; // Disabled until room is scanned
        
        Debug.Log("[MenuController] UI created successfully");
    }

    Button CreateButton(string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(menuCanvas.transform, false);
        
        var button = buttonObj.AddComponent<Button>();
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        
        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(600, 80);
        
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
        statusText.text = "Exporting room data...";
        
        if (roomExporter != null)
        {
            roomExporter.ExportAll();
            statusText.text = "Export complete! Check logs.";
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
