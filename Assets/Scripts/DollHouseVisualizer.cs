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
        if (roomMaterial == null)
        {
            var shader = Shader.Find("QuestHouse/UnlitColor") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogError("[DollHouseVisualizer] Could not find Unlit/Color shader! Make sure shaders are included in build.");
                return;
            }
            roomMaterial = new Material(shader);
            roomMaterial.color = new Color(0.8f, 0.8f, 0.9f, 0.5f);
        }
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
        
        visualizedRooms.Add(go);
        Debug.Log($"[DollHouseVisualizer] Created room: {room.roomName} on floor {floorLevel}");
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
        Material wallMat = new Material(roomMaterial);
        wallMat.color = new Color(0.6f, 0.6f, 0.7f, 0.8f); // Slightly different color for walls
        mr.material = wallMat;
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
}

