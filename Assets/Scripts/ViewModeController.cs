using UnityEngine;

/// <summary>
/// Controls view mode switching between Doll House (miniature 3D view) and AR Walkthrough (passthrough).
/// </summary>
public class ViewModeController : MonoBehaviour
{
    public enum Mode { DollHouse, ARWalkthrough }

    [Header("View Mode")]
    public Mode currentMode = Mode.ARWalkthrough;

    [Header("References")]
    public GameObject dollHouseRoot;
    public Camera dollHouseCamera;
    public GameObject arPassthroughRoot;

    void Start()
    {
        ApplyMode(currentMode);
    }

    public void ToggleMode()
    {
        currentMode = currentMode == Mode.DollHouse ? Mode.ARWalkthrough : Mode.DollHouse;
        ApplyMode(currentMode);
        RuntimeLogger.WriteLine($"View mode switched to: {currentMode}");
    }

    void ApplyMode(Mode mode)
    {
        if (mode == Mode.DollHouse)
        {
            if (dollHouseRoot != null) dollHouseRoot.SetActive(true);
            if (dollHouseCamera != null) dollHouseCamera.enabled = true;
            if (arPassthroughRoot != null) arPassthroughRoot.SetActive(false);
            // TODO: Disable passthrough, enable doll house camera
        }
        else // AR Walkthrough
        {
            if (dollHouseRoot != null) dollHouseRoot.SetActive(false);
            if (dollHouseCamera != null) dollHouseCamera.enabled = false;
            if (arPassthroughRoot != null) arPassthroughRoot.SetActive(true);
            // TODO: Enable passthrough, use XR camera
        }
    }

    public void SetMode(Mode mode)
    {
        currentMode = mode;
        ApplyMode(mode);
    }
}
