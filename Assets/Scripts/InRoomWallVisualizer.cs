using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Visualizes room walls as wireframe outlines in full scale (like LayoutXR).
/// User sees room boundaries from inside as if walking through the space.
/// </summary>
public class InRoomWallVisualizer : MonoBehaviour
{
    [Header("Wall Visualization Settings")]
    public Material wallMaterial;
    public Color wallColor = new Color(0.2f, 0.6f, 1.0f, 0.8f); // bright blue
    public float wallLineWidth = 0.02f; // thickness of wall lines in meters
    public float wallHeight = 2.5f; // default wall height if not detected

    [Header("Advanced Settings")]
    public bool showCeiling = false;
    public bool showFloor = true;
    public float floorOpacity = 0.1f;

    List<GameObject> visualizedWalls = new List<GameObject>();

    void Start()
    {
        if (wallMaterial == null)
        {
            wallMaterial = new Material(Shader.Find("Standard"));
            wallMaterial.SetFloat("_Mode", 3); // Transparent
            wallMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            wallMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            wallMaterial.SetInt("_ZWrite", 0);
            wallMaterial.DisableKeyword("_ALPHATEST_ON");
            wallMaterial.EnableKeyword("_ALPHABLEND_ON");
            wallMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            wallMaterial.renderQueue = 3000;
        }
    }

    public void GenerateWallOutlines()
    {
        ClearWalls();

        if (MRUK.Instance == null || MRUK.Instance.Rooms == null)
        {
            Debug.LogWarning("MRUK not initialized. Cannot generate wall outlines.");
            return;
        }

        var rooms = MRUK.Instance.Rooms;
        int wallCount = 0;

        foreach (var room in rooms)
        {
            wallCount += CreateRoomWallOutlines(room);
        }

        RuntimeLogger.WriteLine($"In-Room walls generated: {wallCount} walls in {rooms.Count} rooms");
    }

    int CreateRoomWallOutlines(MRUKRoom room)
    {
        if (room.FloorAnchor == null) return 0;

        var boundary = room.FloorAnchor.PlaneBoundary2D;
        if (boundary == null || boundary.Count == 0) return 0;

        Vector3 roomWorldPos = room.FloorAnchor.transform.position;
        Quaternion roomWorldRot = room.FloorAnchor.transform.rotation;

        int wallCount = 0;

        // Detect ceiling height if available
        float detectedHeight = wallHeight;
        if (room.CeilingAnchor != null)
        {
            detectedHeight = Mathf.Abs(room.CeilingAnchor.transform.position.y - roomWorldPos.y);
        }

        // Create vertical wall outlines for each edge
        for (int i = 0; i < boundary.Count; i++)
        {
            Vector2 p1 = boundary[i];
            Vector2 p2 = boundary[(i + 1) % boundary.Count];

            CreateWallSegment(p1, p2, roomWorldPos, roomWorldRot, detectedHeight);
            wallCount++;
        }

        // Optional: Create floor visualization
        if (showFloor)
        {
            CreateFloorOutline(boundary, roomWorldPos, roomWorldRot);
        }

        // Optional: Create ceiling visualization
        if (showCeiling && room.CeilingAnchor != null)
        {
            CreateCeilingOutline(boundary, roomWorldPos, roomWorldRot, detectedHeight);
        }

        return wallCount;
    }

    void CreateWallSegment(Vector2 p1, Vector2 p2, Vector3 roomPos, Quaternion roomRot, float height)
    {
        GameObject wallObj = new GameObject("Wall_Segment");
        wallObj.transform.SetParent(transform, false);

        // Convert 2D boundary points to 3D world space
        Vector3 bottom1 = roomPos + roomRot * new Vector3(p1.x, 0, p1.y);
        Vector3 bottom2 = roomPos + roomRot * new Vector3(p2.x, 0, p2.y);
        Vector3 top1 = bottom1 + Vector3.up * height;
        Vector3 top2 = bottom2 + Vector3.up * height;

        // Create wall quad mesh
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[]
        {
            bottom1, bottom2, top2, top1, // Front face
            bottom2, bottom1, top1, top2  // Back face (for double-sided rendering)
        };

        int[] triangles = new int[]
        {
            0, 2, 1, 0, 3, 2, // Front
            4, 6, 5, 4, 7, 6  // Back
        };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        var mf = wallObj.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mr = wallObj.AddComponent<MeshRenderer>();
        Material mat = new Material(wallMaterial);
        mat.color = wallColor;
        mr.material = mat;

        visualizedWalls.Add(wallObj);
    }

    void CreateFloorOutline(List<Vector2> boundary, Vector3 roomPos, Quaternion roomRot)
    {
        GameObject floorObj = new GameObject("Floor_Outline");
        floorObj.transform.SetParent(transform, false);

        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[boundary.Count];
        
        for (int i = 0; i < boundary.Count; i++)
        {
            Vector3 worldPos = roomPos + roomRot * new Vector3(boundary[i].x, 0, boundary[i].y);
            verts[i] = worldPos;
        }
        
        mesh.vertices = verts;

        // Triangulate
        int[] tris = new int[(boundary.Count - 2) * 3];
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        var mf = floorObj.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mr = floorObj.AddComponent<MeshRenderer>();
        Material mat = new Material(wallMaterial);
        mat.color = new Color(wallColor.r, wallColor.g, wallColor.b, floorOpacity);
        mr.material = mat;

        visualizedWalls.Add(floorObj);
    }

    void CreateCeilingOutline(List<Vector2> boundary, Vector3 roomPos, Quaternion roomRot, float height)
    {
        GameObject ceilingObj = new GameObject("Ceiling_Outline");
        ceilingObj.transform.SetParent(transform, false);

        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[boundary.Count];
        
        for (int i = 0; i < boundary.Count; i++)
        {
            Vector3 worldPos = roomPos + roomRot * new Vector3(boundary[i].x, height, boundary[i].y);
            verts[i] = worldPos;
        }
        
        mesh.vertices = verts;

        // Triangulate (reversed winding for ceiling)
        int[] tris = new int[(boundary.Count - 2) * 3];
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 2] = i + 1;
            tris[i * 3 + 1] = i + 2;
        }
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        var mf = ceilingObj.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mr = ceilingObj.AddComponent<MeshRenderer>();
        Material mat = new Material(wallMaterial);
        mat.color = new Color(wallColor.r, wallColor.g, wallColor.b, floorOpacity);
        mr.material = mat;

        visualizedWalls.Add(ceilingObj);
    }

    public void ClearWalls()
    {
        foreach (var wall in visualizedWalls)
        {
            if (wall != null) Destroy(wall);
        }
        visualizedWalls.Clear();
    }

    void OnDisable()
    {
        ClearWalls();
    }
}
