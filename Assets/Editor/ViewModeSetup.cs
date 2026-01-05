using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor helper to setup ViewModeController with proper GameObjects in the scene.
/// </summary>
public class ViewModeSetup : EditorWindow
{
    [MenuItem("Tools/Setup View Mode Controller")]
    static void Setup()
    {
        // Find or create ViewModeController
        ViewModeController controller = FindFirstObjectByType<ViewModeController>();
        if (controller == null)
        {
            GameObject go = new GameObject("ViewModeController");
            controller = go.AddComponent<ViewModeController>();
        }

        // Create DollHouse Root
        if (controller.dollHouseRoot == null)
        {
            GameObject dollHouseRoot = new GameObject("DollHouseRoot");
            controller.dollHouseRoot = dollHouseRoot;
            
            // Add DollHouseVisualizer
            dollHouseRoot.AddComponent<DollHouseVisualizer>();
            
            // Create camera for doll house view
            GameObject camObj = new GameObject("DollHouseCamera");
            camObj.transform.SetParent(dollHouseRoot.transform);
            camObj.transform.localPosition = new Vector3(0, 5, -5);
            camObj.transform.localRotation = Quaternion.Euler(45, 0, 0);
            
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            cam.enabled = false;
            
            controller.dollHouseCamera = cam;
        }

        // Create InRoom Walls Root
        if (controller.inRoomWallsRoot == null)
        {
            GameObject inRoomRoot = new GameObject("InRoomWallsRoot");
            controller.inRoomWallsRoot = inRoomRoot;
            
            // Add InRoomWallVisualizer
            inRoomRoot.AddComponent<InRoomWallVisualizer>();
            inRoomRoot.SetActive(false);
        }

        // AR Passthrough Root reference (usually OVRCameraRig or similar)
        if (controller.arPassthroughRoot == null)
        {
            // Try to find OVR camera rig
            var ovrRig = FindFirstObjectByType<OVRCameraRig>();
            if (ovrRig != null)
            {
                controller.arPassthroughRoot = ovrRig.gameObject;
            }
            else
            {
                Debug.LogWarning("OVRCameraRig not found. Please assign arPassthroughRoot manually.");
            }
        }

        EditorUtility.SetDirty(controller);
        Debug.Log("ViewModeController setup complete!");
    }
}
