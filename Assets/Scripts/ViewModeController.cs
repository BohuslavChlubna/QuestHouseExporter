using UnityEngine;

/// <summary>
/// Controls view mode switching between Doll House (miniature 3D view), In-Room (full scale with wall outlines), and AR Walkthrough (passthrough).
/// </summary>
public class ViewModeController : MonoBehaviour
{
    public enum Mode { DollHouse, InRoom, ARWalkthrough }

    [Header("View Mode")]
    public Mode currentMode = Mode.ARWalkthrough;

    [Header("References")]
    public GameObject dollHouseRoot;
    public Camera dollHouseCamera;
    public GameObject inRoomWallsRoot;
    public GameObject arPassthroughRoot;
    
    DollHouseVisualizer dollHouseVisualizer;
    InRoomWallVisualizer inRoomWallVisualizer;

    void Start()
    {
        // Get or create visualizer components
        if (dollHouseRoot != null)
        {
            dollHouseVisualizer = dollHouseRoot.GetComponent<DollHouseVisualizer>();
            if (dollHouseVisualizer == null)
                dollHouseVisualizer = dollHouseRoot.AddComponent<DollHouseVisualizer>();
        }

        if (inRoomWallsRoot != null)
        {
            inRoomWallVisualizer = inRoomWallsRoot.GetComponent<InRoomWallVisualizer>();
            if (inRoomWallVisualizer == null)
                inRoomWallVisualizer = inRoomWallsRoot.AddComponent<InRoomWallVisualizer>();
            inRoomWallsRoot.SetActive(false); // Always start hidden
        }

        ApplyMode(currentMode);
    }

    public void ToggleMode()
    {
        // Cycle through modes: ARWalkthrough -> InRoom -> DollHouse -> ARWalkthrough
        switch (currentMode)
        {
            case Mode.ARWalkthrough:
                currentMode = Mode.InRoom;
                break;
            case Mode.InRoom:
                currentMode = Mode.DollHouse;
                break;
            case Mode.DollHouse:
                currentMode = Mode.ARWalkthrough;
                break;
        }
        ApplyMode(currentMode);
        RuntimeLogger.WriteLine($"View mode switched to: {currentMode}");
        Debug.Log($"View mode: {currentMode}");
    }

    void ApplyMode(Mode mode)
    {
        switch (mode)
        {
            case Mode.DollHouse:
                // Show miniature doll house view from above
                if (dollHouseRoot != null) 
                {
                    dollHouseRoot.SetActive(true);
                    if (dollHouseVisualizer != null)
                        dollHouseVisualizer.GenerateDollHouse();
                }
                if (dollHouseCamera != null) dollHouseCamera.enabled = true;
                if (inRoomWallsRoot != null) inRoomWallsRoot.SetActive(false);
                if (arPassthroughRoot != null) arPassthroughRoot.SetActive(true);
                break;
                
            case Mode.InRoom:
                // Show full-scale wall outlines (like LayoutXR)
                if (dollHouseRoot != null) dollHouseRoot.SetActive(false);
                if (dollHouseCamera != null) dollHouseCamera.enabled = false;
                if (inRoomWallsRoot != null) 
                {
                    inRoomWallsRoot.SetActive(true);
                    if (inRoomWallVisualizer != null)
                        inRoomWallVisualizer.GenerateWallOutlines();
                }
                if (arPassthroughRoot != null) arPassthroughRoot.SetActive(true);
                break;
                
            case Mode.ARWalkthrough:
                // Standard AR passthrough mode
                if (dollHouseRoot != null) dollHouseRoot.SetActive(false);
                if (dollHouseCamera != null) dollHouseCamera.enabled = false;
                if (inRoomWallsRoot != null) inRoomWallsRoot.SetActive(false);
                if (arPassthroughRoot != null) arPassthroughRoot.SetActive(true);
                break;
        }
    }

    public void SetMode(Mode mode)
    {
        currentMode = mode;
        ApplyMode(mode);
    }
}
