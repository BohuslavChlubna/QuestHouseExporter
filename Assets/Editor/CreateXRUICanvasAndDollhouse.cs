using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CreateXRUICanvasAndDollhouse : MonoBehaviour
{
    [MenuItem("Tools/Quest House Design/Create XR UI Canvas + Dollhouse", false, 200)]
    public static void CreateCanvasAndDollhouse()
    {
        // === XR UI CANVAS + MENU CONTROLLER ===
        var menuRoot = new GameObject("MenuRoot");
        Undo.RegisterCreatedObjectUndo(menuRoot, "Create MenuRoot");
        var menuController = menuRoot.AddComponent<MenuController>();

        var canvasGO = new GameObject("MenuCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create XR UI Canvas");
        canvasGO.transform.SetParent(menuRoot.transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        canvasGO.AddComponent<GraphicRaycaster>();
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            canvasGO.layer = uiLayer;
        else
            Debug.LogWarning("Layer 'UI' not found! Canvas will stay on Default layer.");
        var rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1200, 1000);
        rect.localScale = Vector3.one * 0.002f;
        canvasGO.transform.position = new Vector3(0, 1.5f, 3f);
        canvasGO.transform.rotation = Quaternion.identity;

        // Background Panel
        var bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        var bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title Text
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvasGO.transform, false);
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
        statusObj.transform.SetParent(canvasGO.transform, false);
        var statusText = statusObj.AddComponent<TextMeshProUGUI>();
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
        var scanButton = CreateButton("ScanButton", "Request Room Scan", new Vector2(0, -50), canvasGO.transform);
        // Export Button
        var exportButton = CreateButton("ExportButton", "Export Room Data", new Vector2(0, -150), canvasGO.transform);
        // Reload Rooms Button
        var reloadButton = CreateButton("ReloadButton", "Reload Rooms", new Vector2(0, -250), canvasGO.transform);
        // View Mode Toggle Button
        var toggleViewButton = CreateButton("ViewModeButton", "Toggle View Mode", new Vector2(0, -350), canvasGO.transform);

        // Napojení na MenuController (pøiøazení referencí) - nastavte ruènì v editoru pokud používáte
        menuController.dollHouseVisualizer = null;
        menuController.inRoomWallVisualizer = null;
        menuController.viewModeController = null;
        menuController.roomExporter = null;
        // Pokud používáte propojení UI prvkù, nastavte je ruènì v editoru nebo upravte MenuController
        scanButton.GetComponent<Button>().onClick.AddListener(menuController.OnScanButtonPressed);
        exportButton.GetComponent<Button>().onClick.AddListener(menuController.OnExportButtonPressed);
        reloadButton.GetComponent<Button>().onClick.AddListener(menuController.OnReloadRoomsPressed);
        toggleViewButton.GetComponent<Button>().onClick.AddListener(menuController.OnToggleViewMode);

        // === DOLLHOUSE ===
        var dollhouseGO = new GameObject("DollHouseRoot");
        Undo.RegisterCreatedObjectUndo(dollhouseGO, "Create DollHouseRoot");
        var dollhouse = dollhouseGO.AddComponent<DollHouseVisualizer>();
        // Posunutí dollhouse ještì níž a dál od menu
        dollhouseGO.transform.position = new Vector3(0, 0.7f, 6.0f); // ještì níž a dál
        dollhouseGO.transform.rotation = Quaternion.identity;
        dollhouse.scale = 0.18f; // menší mìøítko
        // Optionally, you can assign a material here if needed

        // === DEFAULT DOLLHOUSE DATA ===
        // Vytvoøení místnosti (4x5m) se šikmým stropem, dvìma okny a jednìmi dveømi
        float width = 4f;
        float length = 5f;
        float wallHeight = 2.5f;
        float ceilingSlope = 0.5f;
        var floor = new List<Vector3>
        {
            new Vector3(-width/2, 0, -length/2),
            new Vector3(width/2, 0, -length/2),
            new Vector3(width/2, 0, length/2),
            new Vector3(-width/2, 0, length/2)
        };
        var ceiling = new List<Vector3>
        {
            new Vector3(-width/2, wallHeight, -length/2),
            new Vector3(width/2, wallHeight + ceilingSlope, -length/2),
            new Vector3(width/2, wallHeight + ceilingSlope, length/2),
            new Vector3(-width/2, wallHeight, length/2)
        };
        var walls = new List<WallData>
        {
            new WallData { start = floor[0], end = floor[1], height = wallHeight }, // Front
            new WallData { start = floor[1], end = floor[2], height = wallHeight + ceilingSlope }, // Right
            new WallData { start = floor[2], end = floor[3], height = wallHeight }, // Back
            new WallData { start = floor[3], end = floor[0], height = wallHeight }  // Left
        };
        // Door (pøední stìna, støed)
        walls[0].attachedAnchors.Add(new AnchorData
        {
            anchorType = "DOOR",
            position = new Vector3(0, 1.0f, -length/2),
            scale = new Vector3(0.9f, 2.0f, 0.1f),
            rotation = Quaternion.identity
        });
        // Window 1 (zadní stìna, vlevo)
        walls[2].attachedAnchors.Add(new AnchorData
        {
            anchorType = "WINDOW",
            position = new Vector3(-width/4, 1.5f, length/2),
            scale = new Vector3(1.2f, 1.0f, 0.1f),
            rotation = Quaternion.identity
        });
        // Window 2 (zadní stìna, vpravo)
        walls[2].attachedAnchors.Add(new AnchorData
        {
            anchorType = "WINDOW",
            position = new Vector3(width/4, 1.5f, length/2),
            scale = new Vector3(1.2f, 1.0f, 0.1f),
            rotation = Quaternion.identity
        });
        var defaultRoom = new RoomData
        {
            roomId = "default_test_room",
            roomName = "Default 4x5m Room (Sloped Ceiling)",
            ceilingHeight = wallHeight,
            floorBoundary = floor,
            ceilingBoundary = ceiling,
            walls = walls,
            anchors = new List<AnchorData>(),
            hasSlopedCeiling = true
        };
        var rooms = new List<RoomData> { defaultRoom };
        dollhouse.GenerateDollHouseFromOfflineData(rooms);

        Selection.activeGameObject = menuRoot;
        Debug.Log("[CreateXRUICanvasAndDollhouse] XR UI Canvas, MenuController a Dollhouse byly vytvoøeny ve scénì.");
    }

    private static GameObject CreateButton(string name, string buttonText, Vector2 anchoredPosition, Transform parent)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(600, 80);

        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        buttonImage.sprite = null;

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        var buttonTextComp = textObj.AddComponent<TextMeshProUGUI>();
        buttonTextComp.text = buttonText;
        buttonTextComp.fontSize = 36;
        buttonTextComp.alignment = TextAlignmentOptions.Center;
        buttonTextComp.color = Color.white;

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObj;
    }
}
