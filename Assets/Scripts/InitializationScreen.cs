using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Initialization screen shown on app startup. Waits for MRUK room scan before allowing app usage.
/// </summary>
public class InitializationScreen : MonoBehaviour
{
    Canvas canvas;
    TextMeshProUGUI statusText;
    Button scanButton;
    System.Action onInitialized;
    float timeoutDuration = 10f; // Wait 10 seconds before allowing manual skip
    float elapsedTime = 0f;
    bool canSkip = false;
    
    public void Show(System.Action onComplete)
    {
        onInitialized = onComplete;
        elapsedTime = 0f;
        canSkip = false;
        CreateUI();
        InvokeRepeating(nameof(CheckMRUKStatus), 0.5f, 0.5f);
    }
    
    void CreateUI()
    {
        // Ensure EventSystem exists for UI interaction
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[InitializationScreen] Created EventSystem for UI interaction");
        }
        
        var canvasObj = new GameObject("InitCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        // Add GraphicRaycaster for button interaction
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        var rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 500);
        rect.localPosition = new Vector3(0, 1.5f, 2);
        rect.localScale = Vector3.one * 0.001f;
        
        var bg = canvasObj.AddComponent<Image>();
        var shader = Shader.Find("QuestHouse/UnlitColor");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color"); // Fallback
        }
        if (shader != null)
        {
            bg.material = new Material(shader);
        }
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        
        var textObj = new GameObject("StatusText");
        textObj.transform.SetParent(canvasObj.transform, false);
        statusText = textObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "?? Quest House Exporter\n\nWaiting for room scan...\n\nPlease scan your room first";
        statusText.fontSize = 32;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.3f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);
        
        // Create "Start Room Scan" button
        CreateScanButton(canvasObj);
        
        // Create "Skip" button (initially hidden)
        CreateSkipButton(canvasObj);
    }
    
    void CreateScanButton(GameObject canvasObj)
    {
        var buttonObj = new GameObject("ScanButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.15f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.15f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(600, 80);
        
        // Add Image component with null sprite (Unity will handle it)
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1.0f, 1f);
        buttonImage.sprite = null; // Explicitly set to null - Unity creates default white sprite
        
        // Add Button component after Image
        scanButton = buttonObj.AddComponent<Button>();
        scanButton.targetGraphic = buttonImage;
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Start Room Scan";
        buttonText.fontSize = 36;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        scanButton.onClick.AddListener(OnScanButtonPressed);
    }
    
    Button skipButton;
    void CreateSkipButton(GameObject canvasObj)
    {
        var buttonObj = new GameObject("SkipButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        buttonObj.SetActive(false); // Hidden by default
        
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.05f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.05f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(400, 60);
        
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        buttonImage.sprite = null;
        
        skipButton = buttonObj.AddComponent<Button>();
        skipButton.targetGraphic = buttonImage;
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Continue Without Scan";
        buttonText.fontSize = 24;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        skipButton.onClick.AddListener(OnSkipButtonPressed);
    }
    
    void OnSkipButtonPressed()
    {
        Debug.Log("[InitializationScreen] User chose to continue without room scan");
        statusText.text = "?? No room scan available\n\nApp will start without room data\n\nYou can scan later via Settings";
        CancelInvoke(nameof(CheckMRUKStatus));
        Invoke(nameof(CompleteInitialization), 1.5f);
    }
    
    void OnScanButtonPressed()
    {
        Debug.Log("[InitializationScreen] Scan button pressed, requesting room capture");
        statusText.text = "?? Quest House Exporter\n\nRequesting room capture...\n\nFollow Quest prompts to scan your room";
        
        // Request room capture via OVRSceneManager
        #if UNITY_ANDROID && !UNITY_EDITOR
        var sceneManager = FindFirstObjectByType<OVRSceneManager>();
        if (sceneManager != null)
        {
            Debug.Log("[InitializationScreen] Found OVRSceneManager, requesting scene capture");
            // This will trigger the Quest's built-in room setup flow
            sceneManager.RequestSceneCapture();
        }
        else
        {
            Debug.LogWarning("[InitializationScreen] OVRSceneManager not found in scene");
            statusText.text = "?? Scene Manager not found\n\nPlease set up room manually via Quest settings";
        }
        #else
        Debug.Log("[InitializationScreen] Room scan only works on Quest device");
        statusText.text = "?? Room scan only available on Quest\n\nIn editor: rooms will load automatically if available";
        #endif
    }
    
    void CheckMRUKStatus()
    {
        if (MRUK.Instance != null && MRUK.Instance.Rooms != null && MRUK.Instance.Rooms.Count > 0)
        {
            statusText.text = $"? Room scan detected!\n{MRUK.Instance.Rooms.Count} room(s) found\n\nInitializing...";
            RuntimeLogger.WriteLine($"InitializationScreen: Detected {MRUK.Instance.Rooms.Count} rooms");
            
            // Hide both buttons when rooms are found
            if (scanButton != null)
            {
                scanButton.gameObject.SetActive(false);
            }
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false);
            }
            
            CancelInvoke(nameof(CheckMRUKStatus));
            Invoke(nameof(CompleteInitialization), 1f);
        }
        else
        {
            elapsedTime += 0.5f;
            
            // After timeout, show skip button
            if (elapsedTime >= timeoutDuration && !canSkip)
            {
                canSkip = true;
                if (skipButton != null)
                {
                    skipButton.gameObject.SetActive(true);
                }
                statusText.text = "? No room scan detected\n\nYou can:\n• Start a new room scan below\n• Continue without scan";
                Debug.Log("[InitializationScreen] Timeout reached - showing skip option");
            }
            else if (!canSkip)
            {
                Debug.Log($"[InitializationScreen] Still waiting for MRUK rooms... ({elapsedTime:F1}s elapsed)");
            }
        }
    }
    
    void CompleteInitialization()
    {
        Debug.Log("[InitializationScreen] Initialization complete, switching to main menu");
        onInitialized?.Invoke();
        if (canvas != null && canvas.gameObject != null)
        {
            Destroy(canvas.gameObject);
        }
    }
    
    void Update()
    {
        if (canvas != null && Camera.main != null)
        {
            canvas.transform.LookAt(Camera.main.transform);
            canvas.transform.Rotate(0, 180, 0);
        }
    }
}
