#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MetaXRAutoFix
{
    [MenuItem("Tools/QuestHouseDesign/Fix Meta XR Settings")]
    public static void FixMetaXRSettings()
    {
        bool changed = false;

        // 1. Set Color Space to Linear
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("? Set Color Space to Linear");
            changed = true;
        }

        // 2. Set Graphics API to Vulkan (remove OpenGLES3 if present)
        var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
        if (graphicsAPIs.Length != 1 || graphicsAPIs[0] != UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
        {
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            Debug.Log("? Set Graphics API to Vulkan only");
            changed = true;
        }

        // 3. Set Texture Compression to ASTC
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        Debug.Log("? Set Texture Compression to ASTC");
        changed = true;

        // 4. Disable Auto Graphics API (already set above by SetGraphicsAPIs)
        if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android))
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            Debug.Log("? Disabled Auto Graphics API");
            changed = true;
        }

        // 5. Set Android Target API Level to at least 32 (Android 12)
        if (PlayerSettings.Android.targetSdkVersion < AndroidSdkVersions.AndroidApiLevel32)
        {
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            Debug.Log("? Set Target API Level to 32 (Android 12)");
            changed = true;
        }

        // 6. Set Stereo Rendering Mode to Multiview
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
        Debug.Log("? Set Stereo Rendering to Multiview (Instancing)");
        changed = true;

        // 7. Disable Multithreaded Rendering (recommended for Quest)
        PlayerSettings.MTRendering = false;
        Debug.Log("? Disabled Multithreaded Rendering");
        changed = true;

        // 8. Set Active Input Handling to Input Manager only (avoid "Both" on Android)
        #if UNITY_2020_1_OR_NEWER
        PlayerSettings.SetPropertyInt("ActiveInputHandler", 0, BuildTargetGroup.Android); // 0 = Input Manager only
        Debug.Log("? Set Active Input Handling to Input Manager only");
        changed = true;
        #endif

        if (changed)
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Meta XR Settings Fixed", 
                "All Meta XR Project Setup requirements have been applied.\n\n" +
                "Check Edit ? Project Settings ? Meta XR to verify.\n\n" +
                "You may need to restart Unity for all changes to take effect.", 
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Meta XR Settings", 
                "All settings are already correct!", 
                "OK");
        }
    }
}
#endif
