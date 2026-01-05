#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class SceneCleanup
{
    [MenuItem("Tools/QuestHouseDesign/Fix Scene (Remove Missing Scripts)")]
    public static void FixCurrentScene()
    {
        var go = GameObject.Find("QuestExporter");
        if (go != null)
        {
            Debug.Log("Found old QuestExporter GameObject - removing...");
            GameObject.DestroyImmediate(go);
        }

        // Remove all missing scripts from all GameObjects
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int removed = 0;
        foreach (var obj in allObjects)
        {
            var comps = obj.GetComponents<Component>();
            for (int i = comps.Length - 1; i >= 0; i--)
            {
                if (comps[i] == null)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                    removed++;
                    Debug.Log($"Removed missing script from {obj.name}");
                    break;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Scene Cleanup", $"Removed {removed} missing script references.\n\nSave the scene now (Ctrl+S).", "OK");
    }

    [MenuItem("Tools/QuestHouseDesign/Create Fresh Scene")]
    public static void CreateFreshScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("QuestHouseDesign", "Exit Play mode first.", "OK");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Create Bootstrap
        var bootstrap = new GameObject("Bootstrap");
        bootstrap.AddComponent<AutoBootstrapper>();
        
        EditorSceneManager.MarkSceneDirty(scene);
        string sceneDir = "Assets/Scenes";
        if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);
        string scenePath = Path.Combine(sceneDir, "QuestHouseDesign.unity");
        EditorSceneManager.SaveScene(scene, scenePath);
        
        EditorUtility.DisplayDialog("QuestHouseDesign", $"Fresh scene created:\n{scenePath}\n\nAdd to Build Settings if needed.", "OK");
    }
}
#endif
