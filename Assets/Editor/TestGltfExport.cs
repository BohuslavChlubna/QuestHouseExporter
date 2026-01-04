using UnityEditor;
using UnityEngine;
using System.IO;

public static class TestGltfExport
{
    [MenuItem("Tools/QuestExporter/Test GLB Export from Selected")]
    public static void ExportSelected()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("Select a GameObject with MeshFilter first.");
            return;
        }
        var mf = Selection.activeGameObject.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("Selected GameObject has no MeshFilter or mesh.");
            return;
        }
        string dir = Path.Combine(Application.dataPath, "../TestExports");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, mf.sharedMesh.name + ".glb");
        Debug.Log("Exporting mesh '" + mf.sharedMesh.name + "' to " + path);
        try
        {
            // Call SimpleGltfExporter directly to see detailed errors
            bool ok = SimpleGltfExporter.Export(mf.sharedMesh, path);
            Debug.Log("SimpleGltfExporter.Export returned: " + ok);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception from SimpleGltfExporter.Export: " + ex.ToString());
            if (ex.InnerException != null) Debug.LogError("Inner: " + ex.InnerException.ToString());
        }
    }
}
