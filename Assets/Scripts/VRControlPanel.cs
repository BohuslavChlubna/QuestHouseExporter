using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// 3D VR control panel with physical buttons attached to controller.
/// Uses modern Input System for controller tracking and input.
/// </summary>
public class VRControlPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    public bool attachToRightController = true;
    public Vector3 panelOffset = new Vector3(0.05f, 0.02f, 0.08f);
    public Vector3 panelRotation = new Vector3(-45f, 0f, 0f);
    public Vector2 panelSize = new Vector2(0.12f, 0.16f);
    
    [Header("Button Colors")]
    public Color buttonNormalColor = new Color(0.2f, 0.3f, 0.4f);
    public Color buttonHoverColor = new Color(0.3f, 0.5f, 0.7f);
    public Color buttonPressColor = new Color(0.5f, 0.7f, 1.0f);
    
    [Header("References")]
    public MRUKRoomExporter roomExporter;
    public ViewModeController viewModeController;
    public InputActionAsset inputActions;
    
    class VRButton
    {
        public GameObject gameObject;
        public MeshRenderer meshRenderer;
        public Material material;
        public TextMesh label;
        public System.Action onPress;
        public Vector3 localPosition;
        public bool isPressed;
    }
    
    List<VRButton> buttons = new List<VRButton>();
    GameObject panelRoot;
    
    InputAction triggerAction;
    InputAction positionAction;
    InputAction rotationAction;
    
    void Start()
    {
        // Find references if not set
        if (roomExporter == null) roomExporter = FindFirstObjectByType<MRUKRoomExporter>();
        if (viewModeController == null) viewModeController = FindFirstObjectByType<ViewModeController>();
        
        // Load Input Actions
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("QuestInputActions");
        }

        if (inputActions != null)
        {
            var actionMap = inputActions.FindActionMap("XR Right Controller");
            if (actionMap != null)
            {
                triggerAction = actionMap.FindAction("Trigger");
                positionAction = actionMap.FindAction("Position");
                rotationAction = actionMap.FindAction("Rotation");

                if (triggerAction != null) triggerAction.Enable();
                if (positionAction != null) positionAction.Enable();
                if (rotationAction != null) rotationAction.Enable();
            }
        }
        
        CreatePanel();
    }
    
    void OnDestroy()
    {
        if (triggerAction != null) triggerAction.Disable();
        if (positionAction != null) positionAction.Disable();
        if (rotationAction != null) rotationAction.Disable();
    }
    
    void CreatePanel()
    {
        panelRoot = new GameObject("VRControlPanel");
        panelRoot.transform.SetParent(transform);
        panelRoot.transform.localPosition = panelOffset;
        panelRoot.transform.localRotation = Quaternion.Euler(panelRotation);
        
        // Create background
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
        background.name = "PanelBackground";
        background.transform.SetParent(panelRoot.transform);
        background.transform.localPosition = Vector3.zero;
        background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 0.002f);
        background.transform.localRotation = Quaternion.identity;
        
        var bgMat = new Material(Shader.Find("Standard"));
        bgMat.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        bgMat.SetFloat("_Mode", 3); // Transparent
        bgMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bgMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bgMat.SetInt("_ZWrite", 0);
        bgMat.EnableKeyword("_ALPHABLEND_ON");
        bgMat.renderQueue = 3000;
        background.GetComponent<MeshRenderer>().material = bgMat;
        Destroy(background.GetComponent<Collider>());
        
        // Create buttons
        float buttonHeight = 0.025f;
        float buttonWidth = panelSize.x * 0.85f;
        float spacing = 0.005f;
        float startY = panelSize.y / 2f - 0.015f;
        
        CreateButton("Export Full House", new Vector3(0, startY - (buttonHeight + spacing) * 0, 0.002f), 
            new Vector2(buttonWidth, buttonHeight), OnExportFullHouse);
        
        CreateButton("Toggle View Mode", new Vector3(0, startY - (buttonHeight + spacing) * 1, 0.002f), 
            new Vector2(buttonWidth, buttonHeight), OnToggleViewMode);
        
        CreateButton("Export SVG Plans", new Vector3(0, startY - (buttonHeight + spacing) * 2, 0.002f), 
            new Vector2(buttonWidth, buttonHeight), OnExportSVGPlans);
        
        CreateButton("Export Excel", new Vector3(0, startY - (buttonHeight + spacing) * 3, 0.002f), 
            new Vector2(buttonWidth, buttonHeight), OnExportExcel);
        
        CreateButton("Upload to Drive", new Vector3(0, startY - (buttonHeight + spacing) * 4, 0.002f), 
            new Vector2(buttonWidth, buttonHeight), OnUploadToDrive);
    }
    
    void CreateButton(string labelText, Vector3 position, Vector2 size, System.Action onPress)
    {
        var btn = new VRButton();
        btn.localPosition = position;
        btn.onPress = onPress;
        
        // Button mesh
        btn.gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btn.gameObject.name = "Btn_" + labelText.Replace(" ", "");
        btn.gameObject.transform.SetParent(panelRoot.transform);
        btn.gameObject.transform.localPosition = position;
        btn.gameObject.transform.localScale = new Vector3(size.x, size.y, 0.003f);
        btn.gameObject.transform.localRotation = Quaternion.identity;
        
        // Button material
        btn.material = new Material(Shader.Find("Standard"));
        btn.material.color = buttonNormalColor;
        btn.meshRenderer = btn.gameObject.GetComponent<MeshRenderer>();
        btn.meshRenderer.material = btn.material;
        
        // Make collider trigger
        var collider = btn.gameObject.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btn.gameObject.transform);
        labelObj.transform.localPosition = new Vector3(0, 0, -0.6f);
        labelObj.transform.localRotation = Quaternion.identity;
        labelObj.transform.localScale = Vector3.one;
        
        btn.label = labelObj.AddComponent<TextMesh>();
        btn.label.text = labelText;
        btn.label.fontSize = 32;
        btn.label.characterSize = 0.0015f;
        btn.label.anchor = TextAnchor.MiddleCenter;
        btn.label.alignment = TextAlignment.Center;
        btn.label.color = Color.white;
        
        buttons.Add(btn);
    }
    
    void Update()
    {
        // Update panel position relative to controller
        UpdatePanelPosition();
        
        // Check for button interactions
        bool triggerPressed = triggerAction != null && triggerAction.ReadValue<float>() > 0.5f;
        CheckButtonInteractions(triggerPressed);
    }
    
    void UpdatePanelPosition()
    {
        if (positionAction == null || rotationAction == null) return;

        Vector3 position = positionAction.ReadValue<Vector3>();
        Quaternion rotation = rotationAction.ReadValue<Quaternion>();

        if (position != Vector3.zero) // Valid tracking data
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
    
    void CheckButtonInteractions(bool triggerPressed)
    {
        // Simple ray from controller forward
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        VRButton hitButton = null;
        
        if (Physics.Raycast(ray, out hit, 0.5f))
        {
            foreach (var btn in buttons)
            {
                if (hit.collider != null && hit.collider.gameObject == btn.gameObject)
                {
                    hitButton = btn;
                    break;
                }
            }
        }
        
        // Update button states
        foreach (var btn in buttons)
        {
            if (btn == hitButton)
            {
                // Hover
                btn.material.color = buttonHoverColor;
                
                // Press
                if (triggerPressed && !btn.isPressed)
                {
                    btn.isPressed = true;
                    btn.material.color = buttonPressColor;
                    btn.onPress?.Invoke();
                    SendHapticFeedback(0.3f, 0.1f);
                    
                    // Reset after delay
                    StartCoroutine(ResetButtonAfterDelay(btn, 0.2f));
                }
            }
            else if (!btn.isPressed)
            {
                btn.material.color = buttonNormalColor;
            }
        }
    }
    
    System.Collections.IEnumerator ResetButtonAfterDelay(VRButton btn, float delay)
    {
        yield return new WaitForSeconds(delay);
        btn.isPressed = false;
        btn.material.color = buttonNormalColor;
    }
    
    void SendHapticFeedback(float amplitude, float duration)
    {
        // Modern Input System haptics
        var hmd = InputSystem.GetDevice<XRHMD>();
        if (hmd != null)
        {
            // Note: For Quest-specific haptics, use Meta XR SDK's OVRInput.SetControllerVibration()
            UnityEngine.Debug.Log($"Haptic feedback: {amplitude}@{duration}s");
        }
    }
    
    // Button actions
    void OnExportFullHouse()
    {
        Debug.Log("Exporting full house GLTF...");
        if (roomExporter != null)
        {
            roomExporter.ExportAll();
        }
    }
    
    void OnToggleViewMode()
    {
        Debug.Log("Toggling view mode...");
        if (viewModeController != null)
        {
            viewModeController.ToggleMode();
        }
    }
    
    void OnExportSVGPlans()
    {
        Debug.Log("Exporting SVG floor plans...");
        if (roomExporter != null)
        {
            roomExporter.exportSVGFloorPlans = true;
            roomExporter.ExportAll();
        }
    }
    
    void OnExportExcel()
    {
        Debug.Log("Exporting Excel data...");
        if (roomExporter != null)
        {
            roomExporter.ExportAll();
        }
    }
    
    void OnUploadToDrive()
    {
        Debug.Log("Upload to Google Drive feature removed - use HTTP server for WiFi downloads instead");
        RuntimeLogger.WriteLine("Google Drive upload is no longer available. Use SimpleHttpServer for WiFi downloads.");
    }
}
