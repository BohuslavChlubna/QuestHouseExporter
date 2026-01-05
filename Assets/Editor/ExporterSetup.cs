using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class ExporterSetup
{
    [MenuItem("Tools/QuestHouseDesign/Create Scene")]
    public static void CreateExporterScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("QuestHouseDesign", "Cannot create scenes while in Play mode. Exit Play mode and try again.", "OK");
            return;
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // Create Bootstrap object and add AutoBootstrapper
        var go = new GameObject("Bootstrap");
        go.AddComponent<AutoBootstrapper>();
        EditorSceneManager.MarkSceneDirty(scene);
        string sceneDir = "Assets/Scenes";
        if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);
        string scenePath = Path.Combine(sceneDir, "ExporterScene.unity");
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorUtility.DisplayDialog("QuestHouseDesign", "Scene created at " + scenePath, "OK");
    }

    [MenuItem("Tools/QuestHouseDesign/Apply Player Settings")]
    public static void ApplyPlayerSettings()
    {
        // Android settings
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.veksco.questhousedesign");
        // set company and product names
        PlayerSettings.companyName = "VeksCo";
        PlayerSettings.productName = "QuestHouseDesign";
        EditorUtility.DisplayDialog("QuestHouseDesign", "Player settings applied. Please verify in Project Settings.", "OK");
    }

    [MenuItem("Tools/QuestHouseDesign/Run Full Setup")]
    public static void RunFullSetup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("QuestHouseDesign", "Run Full Setup cannot be executed while in Play mode. Exit Play mode and try again.", "OK");
            return;
        }
        CreateExporterScene();
        ApplyPlayerSettings();
        EditorUtility.DisplayDialog("QuestHouseDesign", "Full setup completed.", "OK");
    }
}
