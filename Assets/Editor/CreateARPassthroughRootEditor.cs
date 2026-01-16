using UnityEditor;
using UnityEngine;

public class CreateARPassthroughRootEditor
{
    [MenuItem("Tools/Create ARPassthroughRoot")]
    public static void CreateARPassthroughRoot()
    {
        if (GameObject.Find("ARPassthroughRoot") == null)
        {
            GameObject arRoot = new GameObject("ARPassthroughRoot");
            Undo.RegisterCreatedObjectUndo(arRoot, "Create ARPassthroughRoot");
            Debug.Log("ARPassthroughRoot created in scene.");
        }
        else
        {
            Debug.LogWarning("ARPassthroughRoot already exists in scene.");
        }
    }
}
