using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Modern Input System controller handler for Quest.
/// Uses Input Actions for button presses and XR tracking.
/// </summary>
[RequireComponent(typeof(MRUKRoomExporter))]
public class ControllerInputExporter : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionAsset inputActions;
    
    [Header("Settings")]
    public bool useRightController = true;
    public bool enableLegacyUI = false;
    
    public string exportButtonLabel = "Primary Button (A) = Export";
    public string toggleModeLabel = "Secondary Button (B) = Toggle View Mode";

    MRUKRoomExporter exporter;
    ViewModeController viewMode;
    
    InputAction primaryButtonAction;
    InputAction secondaryButtonAction;
    InputAction positionAction;
    InputAction rotationAction;

    void Start()
    {
        exporter = GetComponent<MRUKRoomExporter>();
        viewMode = GetComponent<ViewModeController>();
        
        // Load Input Actions if not assigned
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("QuestInputActions");
            if (inputActions == null)
            {
                Debug.LogError("QuestInputActions not found! Create it at Assets/Resources/QuestInputActions.inputactions");
                return;
            }
        }

        // Get action map
        var actionMap = inputActions.FindActionMap("XR Right Controller");
        if (actionMap == null)
        {
            Debug.LogError("Action Map 'XR Right Controller' not found!");
            return;
        }

        // Get actions
        primaryButtonAction = actionMap.FindAction("Primary Button");
        secondaryButtonAction = actionMap.FindAction("Secondary Button");
        positionAction = actionMap.FindAction("Position");
        rotationAction = actionMap.FindAction("Rotation");

        // Subscribe to button events
        if (primaryButtonAction != null)
        {
            primaryButtonAction.performed += OnPrimaryButtonPressed;
            primaryButtonAction.Enable();
        }
        
        if (secondaryButtonAction != null)
        {
            secondaryButtonAction.performed += OnSecondaryButtonPressed;
            secondaryButtonAction.Enable();
        }

        if (positionAction != null) positionAction.Enable();
        if (rotationAction != null) rotationAction.Enable();

        if (!enableLegacyUI)
        {
            // Modern Input System active
        }
    }

    void OnDestroy()
    {
        // Unsubscribe
        if (primaryButtonAction != null)
        {
            primaryButtonAction.performed -= OnPrimaryButtonPressed;
            primaryButtonAction.Disable();
        }
        
        if (secondaryButtonAction != null)
        {
            secondaryButtonAction.performed -= OnSecondaryButtonPressed;
            secondaryButtonAction.Disable();
        }

        if (positionAction != null) positionAction.Disable();
        if (rotationAction != null) rotationAction.Disable();
    }

    void OnPrimaryButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Primary button pressed - Exporting...");
        if (exporter != null)
        {
            exporter.ExportAll();
            SendHaptic(0.1f, 0.2f);
        }
    }

    void OnSecondaryButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Secondary button pressed - Toggling view mode...");
        if (viewMode != null)
        {
            viewMode.ToggleMode();
            SendHaptic(0.05f, 0.15f);
        }
    }

    void SendHaptic(float amplitude, float duration)
    {
        // Modern Input System haptics via XR HMD
        var hmd = InputSystem.GetDevice<XRHMD>();
        if (hmd != null)
        {
            // Note: Haptics API varies by device
            // For Quest, Meta XR SDK provides better haptics support
            Debug.Log($"Haptic feedback: amplitude={amplitude}, duration={duration}");
        }
    }

    void OnGUI()
    {
        if (!enableLegacyUI) return;
        
        GUI.color = Color.white;
        GUI.Box(new Rect(10, 10, 450, 70), "Controller Input (Modern Input System)");
        GUI.Label(new Rect(20, 30, 430, 20), exportButtonLabel);
        GUI.Label(new Rect(20, 50, 430, 20), toggleModeLabel);
    }
}
