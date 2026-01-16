using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Quest House Design - Simplified Build Menu
/// Build buttons for Production and Test APK (with optional install)
/// </summary>
public class QuestHouseBuildMenu
{

    // ============================================================
    // PRODUCTION BUILD
    // ============================================================

    [MenuItem("Tools/Quest House Design/Build Production APK", false, 1)]
    public static void BuildProductionAPK()
    {
        SetTestModeAndBuild(false, false, false);
    }
    
    [MenuItem("Tools/Quest House Design/Build Production APK and Install", false, 2)]
    public static void BuildProductionAndInstall()
    {
        SetTestModeAndBuild(false, true, false);
    }

    // ============================================================
    // DEVELOPMENT BUILD (for debugging)
    // ============================================================
    

    // ============================================================
    // TEST BUILD (UI ONLY)
    // ============================================================
    

    // ============================================================
    // UTILITIES
    // ============================================================
    
    [MenuItem("Tools/Quest House Design/Uninstall from Quest", false, 50)]
    public static void UninstallFromQuest()
    {
        ADBTools.UninstallApp();
    }
    
    [MenuItem("Tools/Quest House Design/Pull Exports from Quest", false, 51)]
    public static void PullExportsFromQuest()
    {
        ADBTools.PullExports();
    }

    [MenuItem("Tools/Quest House Design/Open Build Folder", false, 100)]
    public static void OpenBuildFolder()
    {
        string buildPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Builds", "Android");
        buildPath = System.IO.Path.GetFullPath(buildPath);
        
        if (!System.IO.Directory.Exists(buildPath))
        {
            bool create = EditorUtility.DisplayDialog(
                "Build Folder Not Found",
                $"Build folder does not exist:\n{buildPath}\n\nDo you want to create it?",
                "Create",
                "Cancel"
            );
            
            if (create)
            {
                System.IO.Directory.CreateDirectory(buildPath);
                Debug.Log($"[QuestHouseBuildMenu] Created build folder: {buildPath}");
            }
            else
            {
                return;
            }
        }
        
        EditorUtility.RevealInFinder(buildPath);
    }
    

    // ============================================================
    // INTERNAL HELPER
    // ============================================================

    private static void SetTestModeAndBuild(bool enableTestMode, bool alsoInstall, bool developmentBuild = false)
    {
        // Always save all open scenes before build to prevent data loss
        EditorSceneManager.SaveOpenScenes();

        string modeName = enableTestMode ? "Test Mode (UI Only)" : "Production Mode";
        if (developmentBuild) modeName += " + Development Build";

        // CRITICAL: Manually set ProductName BEFORE build (BuildNameProcessor will also do this, but we need it NOW)
        string baseProductName = PlayerSettings.productName.Replace("_TestMode", "").Replace("_Dev", "");
        string buildProductName; // This is what the APK will be named

        if (enableTestMode)
        {
            buildProductName = baseProductName + "_TestMode";
        }
        else if (developmentBuild)
        {
            buildProductName = baseProductName + "_Dev";
        }
        else
        {
            buildProductName = baseProductName;
        }

        PlayerSettings.productName = buildProductName;
        Debug.Log($"[QuestHouseBuildMenu] PRE-BUILD: Set ProductName to '{PlayerSettings.productName}'");

        // Build (and optionally install)
        // Pass the build product name so ADBTools knows what APK to look for
        if (alsoInstall)
        {
            ADBTools.BuildAndInstall(buildProductName, developmentBuild);
        }
        else
        {
            ADBTools.BuildApk(developmentBuild);
        }

        // CRITICAL: Restore ProductName after build
        PlayerSettings.productName = baseProductName;
        Debug.Log($"[QuestHouseBuildMenu] POST-BUILD: Restored ProductName to '{PlayerSettings.productName}'");
    }
}

