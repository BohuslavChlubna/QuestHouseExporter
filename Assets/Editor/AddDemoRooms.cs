using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class AddDemoRooms
{
    [MenuItem("Tools/QuestExporter/Add Demo Rooms")]
    public static void AddRooms()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("QuestExporter", "Exit Play mode to add demo rooms.", "OK");
            return;
        }

        // Create root
        var rootName = "DemoRooms";
        GameObject root = GameObject.Find(rootName);
        if (root != null)
        {
            if (!EditorUtility.DisplayDialog("QuestExporter", "DemoRooms already exists. Remove and recreate?", "Yes", "No"))
                return;
            GameObject.DestroyImmediate(root);
        }

        root = new GameObject(rootName);

        // Create a few simple rooms
        CreateRoom(root.transform, "Living Room", new Vector3(4f, 2.5f, 5f), new Vector3(0, 0, 0));
        CreateRoom(root.transform, "Kitchen", new Vector3(3.5f, 2.5f, 3.5f), new Vector3(5f, 0, 0));
        CreateRoom(root.transform, "Bedroom", new Vector3(3.5f, 2.5f, 4f), new Vector3(0, 0, 6f));

        // Try to assign to ScannerExporter.roomsRoot if present
        var se = Object.FindObjectOfType<ScannerExporter>();
        if (se != null)
        {
            se.roomsRoot = root.transform;
            EditorUtility.SetDirty(se);
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("QuestExporter", "Demo rooms created under '" + rootName + "' and assigned to ScannerExporter.roomsRoot.", "OK");
    }

    static void CreateRoom(Transform parent, string roomName, Vector3 size, Vector3 position)
    {
        var room = new GameObject(MakeSafeName(roomName));
        room.transform.SetParent(parent);
        room.transform.localPosition = position;
        var meta = room.AddComponent<RoomMetadata>();
        meta.roomName = roomName;

        // Create a simple volume mesh (cube) as placeholder for room mesh
        var meshGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        meshGO.name = "RoomMesh";
        meshGO.transform.SetParent(room.transform);
        meshGO.transform.localPosition = new Vector3(0, size.y / 2f, 0);
        meshGO.transform.localScale = size;

        // Make the mesh a child and remove collider to avoid physics
        var meshCol = meshGO.GetComponent<Collider>();
        if (meshCol != null) GameObject.DestroyImmediate(meshCol);

        // Add a window placeholder on the +X wall (thin pane flush with the wall)
        float winThickness = 0.01f;
        var window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "Window_1";
        window.transform.SetParent(room.transform);
        window.transform.localScale = new Vector3(1.2f, 1.0f, winThickness);
        window.transform.localPosition = new Vector3(size.x / 2f - winThickness / 2f, 1.2f, 0f);
        window.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // face outward on X wall
        if (window.GetComponent<Collider>() != null) GameObject.DestroyImmediate(window.GetComponent<Collider>());

        // Add a second window on the +Z wall
        var window2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window2.name = "Window_2";
        window2.transform.SetParent(room.transform);
        window2.transform.localScale = new Vector3(1.0f, 0.8f, winThickness);
        window2.transform.localPosition = new Vector3(0f, 1.3f, size.z / 2f - winThickness / 2f);
        window2.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // face outward on Z wall
        if (window2.GetComponent<Collider>() != null) GameObject.DestroyImmediate(window2.GetComponent<Collider>());

        // Add a door placeholder on the -Z wall (thin side along Z, no rotation needed)
        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door_Main";
        door.transform.SetParent(room.transform);
        door.transform.localScale = new Vector3(0.9f, 2.0f, 0.05f);
        door.transform.localPosition = new Vector3(0f, 1.0f, -size.z / 2f + 0.025f);
        door.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (door.GetComponent<Collider>() != null) GameObject.DestroyImmediate(door.GetComponent<Collider>());

        // Color the mesh to visually distinguish
        var mr = meshGO.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.8f, 0.8f, 0.8f) };
        var wm = window.GetComponent<MeshRenderer>();
        var wm2 = window2.GetComponent<MeshRenderer>();
        if (wm != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            var col = new Color(0.3f, 0.6f, 1f, 0.45f);
            mat.color = col;
            // make transparent
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            wm.sharedMaterial = mat;
        }
        if (wm2 != null)
        {
            var mat2 = new Material(Shader.Find("Standard"));
            var col2 = new Color(0.3f, 0.6f, 1f, 0.45f);
            mat2.color = col2;
            mat2.SetFloat("_Mode", 3);
            mat2.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat2.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat2.SetInt("_ZWrite", 0);
            mat2.DisableKeyword("_ALPHATEST_ON");
            mat2.EnableKeyword("_ALPHABLEND_ON");
            mat2.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat2.renderQueue = 3000;
            wm2.sharedMaterial = mat2;
        }
        var dm = door.GetComponent<MeshRenderer>();
        if (dm != null) dm.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.55f, 0.27f, 0.07f) };
    }

    static string MakeSafeName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }
}
