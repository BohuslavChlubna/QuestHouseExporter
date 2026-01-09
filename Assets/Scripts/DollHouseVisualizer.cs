using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Visualizes MRUK rooms as a miniature doll house (scaled down, stacked by floor).
/// </summary>
public class DollHouseVisualizer : MonoBehaviour
{
    [Header("Doll House Settings")]
    public float scale = 0.1f; // 1:10 scale
    public float floorSpacing = 0.5f; // vertical spacing between floors in doll house units
    public Material roomMaterial;

    List<GameObject> visualizedRooms = new List<GameObject>();

    void Start()
    {
        Debug.Log("[DollHouseVisualizer] Start() - Initializing materials...");
        
        if (roomMaterial == null)
        {
            // Try custom shader first
            var shader = Shader.Find("QuestHouse/UnlitColor");
            if (shader == null)
            {
                Debug.LogWarning("[DollHouseVisualizer] Custom shader 'QuestHouse/UnlitColor' not found, trying fallback...");
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                // Last resort - try Standard shader
                Debug.LogWarning("[DollHouseVisualizer] 'Unlit/Color' not found, trying Standard...");
                shader = Shader.Find("Standard");
            }
            
            if (shader == null)
            {
                Debug.LogError("[DollHouseVisualizer] CRITICAL: No shaders found! Cannot create materials. Doll house visualization will NOT work.");
                RuntimeLogger.WriteLine("ERROR: DollHouseVisualizer - No shaders available");
                enabled = false; // Disable this component to prevent further issues
                return;
            }
            
            Debug.Log($"[DollHouseVisualizer] Using shader: {shader.name}");
            RuntimeLogger.WriteLine($"DollHouseVisualizer shader: {shader.name}");
            
            roomMaterial = new Material(shader);
            roomMaterial.color = new Color(0.8f, 0.8f, 0.9f, 0.5f);
            
            // Enable transparency if using Standard shader
            if (shader.name == "Standard")
            {
                roomMaterial.SetFloat("_Mode", 3); // Transparent mode
                roomMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                roomMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                roomMaterial.SetInt("_ZWrite", 0);
                roomMaterial.DisableKeyword("_ALPHATEST_ON");
                roomMaterial.EnableKeyword("_ALPHABLEND_ON");
                roomMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                roomMaterial.renderQueue = 3000;
            }
        }
        
        Debug.Log("[DollHouseVisualizer] Material initialization complete");
    }

    /// <summary>
    /// Generate dollhouse from offline RoomData (NO MRUK dependency!)
    /// </summary>
    public void GenerateDollHouseFromOfflineData(List<RoomData> rooms)
    {
        Debug.Log($"[DollHouseVisualizer] Generating from {rooms.Count} offline rooms");
        ClearDollHouse();
        
        // Group rooms by floor level
        var floorGroups = GroupRoomsByFloorOffline(rooms);
        Debug.Log($"[DollHouseVisualizer] Detected {floorGroups.Count} floor levels");
        
        foreach (var floorKvp in floorGroups)
        {
            int floorLevel = floorKvp.Key;
            var floorRooms = floorKvp.Value;
            
            Debug.Log($"[DollHouseVisualizer] Processing floor {floorLevel} with {floorRooms.Count} rooms");
            
            foreach (var room in floorRooms)
            {
                CreateRoomVisualizationFromOfflineData(room, floorLevel);
            }
        }
        
        RuntimeLogger.WriteLine($"Doll house generated from offline data: {visualizedRooms.Count} rooms across {floorGroups.Count} floors");
    }
    
    Dictionary<int, List<RoomData>> GroupRoomsByFloorOffline(List<RoomData> rooms)
    {
        var groups = new Dictionary<int, List<RoomData>>();
        
        foreach (var room in rooms)
        {
            // Detect floor level from room's Y position
            float floorY = 0f;
            if (room.floorBoundary != null && room.floorBoundary.Count > 0)
            {
                floorY = room.floorBoundary[0].y;
            }
            
            int floorLevel = Mathf.RoundToInt(floorY / 3.0f); // assume ~3m per floor
            
            if (!groups.ContainsKey(floorLevel))
                groups[floorLevel] = new List<RoomData>();
            
            groups[floorLevel].Add(room);
        }
        
        return groups;
    }
    
    void CreateRoomVisualizationFromOfflineData(RoomData room, int floorLevel)
    {
        if (room.floorBoundary == null || room.floorBoundary.Count < 3)
        {
            Debug.LogWarning($"[DollHouseVisualizer] Room {room.roomName} has no valid floor boundary");
            return;
        }
        
        var go = new GameObject($"DH_{room.roomName}");
        go.transform.SetParent(transform, false);
        
        // Calculate room center
        Vector3 roomCenter = Vector3.zero;
        foreach (var pt in room.floorBoundary)
        {
            roomCenter += pt;
        }
        roomCenter /= room.floorBoundary.Count;
        
        // Position scaled down, offset by floor level
        Vector3 scaledCenter = roomCenter * scale;
        scaledCenter.y = floorLevel * floorSpacing;
        go.transform.localPosition = scaledCenter;
        go.transform.localScale = Vector3.one * scale;
        
        // Create floor mesh from boundary
        Mesh floorMesh = CreateFloorMeshFromOfflineData(room.floorBoundary, roomCenter);
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = floorMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = roomMaterial;
        
        // Create walls
        if (room.walls != null && room.walls.Count > 0)
        {
            foreach (var wall in room.walls)
            {
                CreateWallVisualization(wall, go.transform, roomCenter, floorLevel);
            }
        }
        
        // Create ceiling (sloped or flat)
        if (room.hasSlopedCeiling && room.ceilingBoundary != null && room.ceilingBoundary.Count > 0)
        {
            CreateSlopedCeilingVisualization(room.ceilingBoundary, go.transform, roomCenter);
        }
        
        visualizedRooms.Add(go);
        Debug.Log($"[DollHouseVisualizer] Created room: {room.roomName} on floor {floorLevel}");
    }
    
    void CreateSlopedCeilingVisualization(List<Vector3> ceilingBoundary, Transform parent, Vector3 roomCenter)
    {
        var ceilingObj = new GameObject("Ceiling_Sloped");
        ceilingObj.transform.SetParent(parent, false);

        // Convert to local coordinates
        Vector3[] localVerts = new Vector3[ceilingBoundary.Count];
        for (int i = 0; i < ceilingBoundary.Count; i++)
        {
            localVerts[i] = ceilingBoundary[i] - roomCenter;
        }

        Mesh ceilingMesh = new Mesh();
        ceilingMesh.vertices = localVerts;

        // Triangulate (reversed for ceiling)
        int[] tris = new int[(ceilingBoundary.Count - 2) * 3];
        for (int i = 0; i < ceilingBoundary.Count - 2; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 2] = i + 1;
            tris[i * 3 + 1] = i + 2;
        }
        ceilingMesh.triangles = tris;
        ceilingMesh.RecalculateNormals();

        var mf = ceilingObj.AddComponent<MeshFilter>();
        mf.mesh = ceilingMesh;

        var mr = ceilingObj.AddComponent<MeshRenderer>();
        mr.material = roomMaterial;
        // Use MaterialPropertyBlock for color
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", new Color(0.7f, 0.7f, 0.9f, 0.6f));
        mr.SetPropertyBlock(mpb);
    }
    
    void CreateWallVisualization(WallData wall, Transform parent, Vector3 roomCenter, int floorLevel)
    {
        var wallObj = new GameObject("Wall");
        wallObj.transform.SetParent(parent, false);

        // Convert to local coordinates (relative to room center)
        Vector3 localStart = (wall.start - roomCenter);
        Vector3 localEnd = (wall.end - roomCenter);

        // Create wall quad
        Vector3 bottom1 = localStart;
        Vector3 bottom2 = localEnd;
        Vector3 top1 = localStart + Vector3.up * wall.height;
        Vector3 top2 = localEnd + Vector3.up * wall.height;

        Mesh wallMesh = new Mesh();
        wallMesh.vertices = new Vector3[] { bottom1, bottom2, top2, top1 };
        wallMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        wallMesh.RecalculateNormals();

        var mf = wallObj.AddComponent<MeshFilter>();
        mf.mesh = wallMesh;

        var mr = wallObj.AddComponent<MeshRenderer>();
        mr.material = roomMaterial;
        // Use MaterialPropertyBlock for color
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", new Color(0.6f, 0.6f, 0.7f, 0.8f));
        mr.SetPropertyBlock(mpb);
    }
    
    Mesh CreateFloorMeshFromOfflineData(List<Vector3> boundary, Vector3 center)
    {
        Mesh mesh = new Mesh();
        
        // Convert to local coordinates (relative to center)
        Vector3[] verts = new Vector3[boundary.Count];
        for (int i = 0; i < boundary.Count; i++)
        {
            verts[i] = boundary[i] - center;
            verts[i].y = 0; // Flatten to floor level
        }
        mesh.vertices = verts;
        
        // Simple triangulation (fan from first vertex)
        int[] tris = new int[(boundary.Count - 2) * 3];
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }
    
    public void GenerateDollHouse()
    {
        ClearDollHouse();

        if (MRUK.Instance == null || MRUK.Instance.Rooms == null)
        {
            Debug.LogWarning("MRUK not initialized. Cannot generate doll house.");
            return;
        }

        var rooms = MRUK.Instance.Rooms;
        var floorGroups = GroupRoomsByFloor(rooms);

        foreach (var floorKvp in floorGroups)
        {
            int floorLevel = floorKvp.Key;
            var floorRooms = floorKvp.Value;

            foreach (var room in floorRooms)
            {
                CreateRoomVisualization(room, floorLevel);
            }
        }

        RuntimeLogger.WriteLine($"Doll house generated with {visualizedRooms.Count} rooms");
    }

    void CreateRoomVisualization(MRUKRoom room, int floorLevel)
    {
        if (room.FloorAnchor == null) return;

        var go = new GameObject($"DH_{room.name}");
        go.transform.SetParent(transform, false);

        // Position: scale down and offset by floor level
        Vector3 pos = room.FloorAnchor.transform.position * scale;
        pos.y = floorLevel * floorSpacing;
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;

        // Create floor mesh
        var boundary = room.FloorAnchor.PlaneBoundary2D;
        if (boundary != null && boundary.Count > 0)
        {
            Mesh mesh = CreateFloorMesh(boundary);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = roomMaterial;
        }

        visualizedRooms.Add(go);
    }

    Mesh CreateFloorMesh(List<Vector2> boundary)
    {
        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[boundary.Count];
        for (int i = 0; i < boundary.Count; i++)
        {
            verts[i] = new Vector3(boundary[i].x, 0, boundary[i].y);
        }
        mesh.vertices = verts;

        // Simple triangulation (fan from first vertex)
        int[] tris = new int[(boundary.Count - 2) * 3];
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    Dictionary<int, List<MRUKRoom>> GroupRoomsByFloor(List<MRUKRoom> rooms)
    {
        var groups = new Dictionary<int, List<MRUKRoom>>();
        foreach (var room in rooms)
        {
            float floorY = room.FloorAnchor != null ? room.FloorAnchor.transform.position.y : 0f;
            int floorLevel = Mathf.RoundToInt(floorY / 3.0f);
            if (!groups.ContainsKey(floorLevel)) groups[floorLevel] = new List<MRUKRoom>();
            groups[floorLevel].Add(room);
        }
        return groups;
    }

    public void ClearDollHouse()
    {
        foreach (var go in visualizedRooms)
        {
            if (go != null) Destroy(go);
        }
        visualizedRooms.Clear();
    }

    /// <summary>
    /// Asynchronous version: generates dollhouse from offline RoomData, one room per frame.
    /// </summary>
    public System.Collections.IEnumerator GenerateDollHouseFromOfflineDataAsync(List<RoomData> rooms)
    {
        Debug.Log($"[DollHouseVisualizer] (Async) Generating from {rooms.Count} offline rooms");
        ClearDollHouse();

        var floorGroups = GroupRoomsByFloorOffline(rooms);
        Debug.Log($"[DollHouseVisualizer] (Async) Detected {floorGroups.Count} floor levels");

        foreach (var floorKvp in floorGroups)
        {
            int floorLevel = floorKvp.Key;
            var floorRooms = floorKvp.Value;
            Debug.Log($"[DollHouseVisualizer] (Async) Processing floor {floorLevel} with {floorRooms.Count} rooms");
            foreach (var room in floorRooms)
            {
                CreateRoomVisualizationFromOfflineData(room, floorLevel);
                yield return null; // Wait one frame per room
            }
        }

        RuntimeLogger.WriteLine($"Doll house generated from offline data (async): {visualizedRooms.Count} rooms across {floorGroups.Count} floors");
    }
}

