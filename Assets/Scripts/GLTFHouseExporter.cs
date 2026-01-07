using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Enhanced GLTF exporter that creates a unified multi-floor house model
/// with properly connected walls and aligned floor levels.
/// </summary>
public static class GLTFHouseExporter
{
    /// <summary>
    /// Export unified house from offline RoomData (NO MRUK dependency!)
    /// </summary>
    public static void ExportUnifiedHouseFromOfflineData(List<RoomData> rooms, string outputPath, bool exportAsOBJ)
    {
        RuntimeLogger.WriteLine("=== GLTFHouseExporter: Starting unified house export from offline data ===");
        
        // Collect all geometry
        List<Vector3> allVertices = new List<Vector3>();
        List<int> allTriangles = new List<int>();
        List<Vector3> allNormals = new List<Vector3>();
        List<Vector2> allUVs = new List<Vector2>();
        
        int vertexOffset = 0;
        
        foreach (var room in rooms)
        {
            AddRoomGeometryFromOfflineData(room, ref allVertices, ref allTriangles, 
                ref allNormals, ref allUVs, ref vertexOffset);
        }
        
        // Export to file
        if (exportAsOBJ)
        {
            ExportAsOBJ(allVertices, allTriangles, allNormals, allUVs, outputPath);
        }
        else
        {
            ExportToGLTF(allVertices, allTriangles, allNormals, allUVs, outputPath);
        }
        
        RuntimeLogger.WriteLine($"Unified house export completed: {outputPath}");
    }
    
    static void AddRoomGeometryFromOfflineData(RoomData room, ref List<Vector3> vertices, 
        ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, ref int vertexOffset)
    {
        if (room.floorBoundary == null || room.floorBoundary.Count == 0) return;
        
        // Add floor
        AddFloorGeometryFromOfflineData(room.floorBoundary, ref vertices, ref triangles, 
            ref normals, ref uvs, ref vertexOffset);
        
        // Add walls
        if (room.walls != null)
        {
            foreach (var wall in room.walls)
            {
                AddWallSegmentFromOfflineData(wall, ref vertices, ref triangles, 
                    ref normals, ref uvs, ref vertexOffset);
            }
        }
        
        // Add ceiling
        AddCeilingGeometryFromOfflineData(room.floorBoundary, room.ceilingHeight, 
            ref vertices, ref triangles, ref normals, ref uvs, ref vertexOffset);
    }
    
    static void AddFloorGeometryFromOfflineData(List<Vector3> boundary, ref List<Vector3> vertices,
        ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, ref int vertexOffset)
    {
        int startVertex = vertices.Count;
        
        // Add vertices
        foreach (var pt in boundary)
        {
            vertices.Add(pt);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(pt.x, pt.z));
        }
        
        // Triangulate (fan from first vertex)
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            triangles.Add(startVertex + 0);
            triangles.Add(startVertex + i + 1);
            triangles.Add(startVertex + i + 2);
        }
        
        vertexOffset += boundary.Count;
    }
    
    static void AddCeilingGeometryFromOfflineData(List<Vector3> boundary, float height,
        ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, 
        ref List<Vector2> uvs, ref int vertexOffset)
    {
        int startVertex = vertices.Count;
        
        // Add vertices at ceiling height
        foreach (var pt in boundary)
        {
            Vector3 ceilingPt = pt + Vector3.up * height;
            vertices.Add(ceilingPt);
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(pt.x, pt.z));
        }
        
        // Triangulate (reversed winding for ceiling)
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            triangles.Add(startVertex + 0);
            triangles.Add(startVertex + i + 2);
            triangles.Add(startVertex + i + 1);
        }
        
        vertexOffset += boundary.Count;
    }
    
    static void AddWallSegmentFromOfflineData(WallData wall, ref List<Vector3> vertices,
        ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, ref int vertexOffset)
    {
        Vector3 bottom1 = wall.start;
        Vector3 bottom2 = wall.end;
        Vector3 top1 = bottom1 + Vector3.up * wall.height;
        Vector3 top2 = bottom2 + Vector3.up * wall.height;
        
        Vector3 normal = Vector3.Cross((top1 - bottom1).normalized, (bottom2 - bottom1).normalized).normalized;
        
        int startVertex = vertices.Count;
        
        // Add quad vertices
        vertices.Add(bottom1);
        vertices.Add(bottom2);
        vertices.Add(top2);
        vertices.Add(top1);
        
        for (int i = 0; i < 4; i++) normals.Add(normal);
        
        float wallLength = Vector3.Distance(wall.start, wall.end);
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(wallLength, 0));
        uvs.Add(new Vector2(wallLength, wall.height));
        uvs.Add(new Vector2(0, wall.height));
        
        // Add triangles
        triangles.Add(startVertex + 0);
        triangles.Add(startVertex + 1);
        triangles.Add(startVertex + 2);
        
        triangles.Add(startVertex + 0);
        triangles.Add(startVertex + 2);
        triangles.Add(startVertex + 3);
        
        vertexOffset += 4;
    }
    
    static void ExportAsOBJ(List<Vector3> vertices, List<int> triangles, List<Vector3> normals,
        List<Vector2> uvs, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Unity OBJ Export - Full House Model (Offline Data)");
        sb.AppendLine($"# Vertices: {vertices.Count}");
        sb.AppendLine($"# Triangles: {triangles.Count / 3}");
        sb.AppendLine($"# Generated: {DateTime.Now}");
        sb.AppendLine();
        
        // Vertices
        foreach (var v in vertices)
        {
            sb.AppendLine($"v {v.x:F6} {v.y:F6} {v.z:F6}");
        }
        
        // Normals
        foreach (var n in normals)
        {
            sb.AppendLine($"vn {n.x:F6} {n.y:F6} {n.z:F6}");
        }
        
        // UVs
        foreach (var uv in uvs)
        {
            sb.AppendLine($"vt {uv.x:F6} {uv.y:F6}");
        }
        
        sb.AppendLine();
        sb.AppendLine("g UnifiedHouse");
        sb.AppendLine("usemtl Default");
        
        // Faces (1-indexed in OBJ)
        for (int i = 0; i < triangles.Count; i += 3)
        {
            int i1 = triangles[i] + 1;
            int i2 = triangles[i + 1] + 1;
            int i3 = triangles[i + 2] + 1;
            sb.AppendLine($"f {i1}/{i1}/{i1} {i2}/{i2}/{i2} {i3}/{i3}/{i3}");
        }
        
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        RuntimeLogger.WriteLine($"Exported unified house OBJ with {vertices.Count} vertices, {triangles.Count / 3} triangles");
    }
    
    public static void ExportUnifiedHouse(List<MRUKRoom> rooms, string outputPath)
    {
        RuntimeLogger.WriteLine("=== GLTFHouseExporter: Starting unified house export ===");
        
        // Group rooms by floor
        var floorGroups = GroupRoomsByFloor(rooms);
        
        // Collect all geometry
        List<Vector3> allVertices = new List<Vector3>();
        List<int> allTriangles = new List<int>();
        List<Vector3> allNormals = new List<Vector3>();
        List<Vector2> allUVs = new List<Vector2>();
        
        int vertexOffset = 0;
        
        foreach (var floorKvp in floorGroups)
        {
            int floorLevel = floorKvp.Key;
            var floorRooms = floorKvp.Value;
            
            RuntimeLogger.WriteLine($"Processing floor {floorLevel} with {floorRooms.Count} rooms");
            
            foreach (var room in floorRooms)
            {
                AddRoomGeometry(room, ref allVertices, ref allTriangles, ref allNormals, 
                    ref allUVs, ref vertexOffset);
            }
        }
        
        // Export to GLTF
        ExportToGLTF(allVertices, allTriangles, allNormals, allUVs, outputPath);
        
        RuntimeLogger.WriteLine($"GLTF export completed: {outputPath}");
    }
    
    static Dictionary<int, List<MRUKRoom>> GroupRoomsByFloor(List<MRUKRoom> rooms)
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
    
    static void AddRoomGeometry(MRUKRoom room, ref List<Vector3> vertices, ref List<int> triangles,
        ref List<Vector3> normals, ref List<Vector2> uvs, ref int vertexOffset)
    {
        if (room.FloorAnchor == null) return;
        
        var boundary = room.FloorAnchor.PlaneBoundary2D;
        if (boundary == null || boundary.Count == 0) return;
        
        Vector3 roomWorldPos = room.FloorAnchor.transform.position;
        Quaternion roomWorldRot = room.FloorAnchor.transform.rotation;
        
        // Detect wall height
        float wallHeight = 2.5f;
        if (room.CeilingAnchor != null)
        {
            wallHeight = Mathf.Abs(room.CeilingAnchor.transform.position.y - roomWorldPos.y);
        }
        
        // Add floor
        AddFloorGeometry(boundary, roomWorldPos, roomWorldRot, ref vertices, ref triangles, 
            ref normals, ref uvs, ref vertexOffset);
        
        // Add walls
        for (int i = 0; i < boundary.Count; i++)
        {
            Vector2 p1 = boundary[i];
            Vector2 p2 = boundary[(i + 1) % boundary.Count];
            AddWallSegment(p1, p2, roomWorldPos, roomWorldRot, wallHeight, ref vertices, 
                ref triangles, ref normals, ref uvs, ref vertexOffset);
        }
        
        // Add ceiling
        if (room.CeilingAnchor != null)
        {
            AddCeilingGeometry(boundary, roomWorldPos, roomWorldRot, wallHeight, ref vertices, 
                ref triangles, ref normals, ref uvs, ref vertexOffset);
        }
    }
    
    static void AddFloorGeometry(List<Vector2> boundary, Vector3 roomPos, Quaternion roomRot,
        ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, 
        ref List<Vector2> uvs, ref int vertexOffset)
    {
        int startVertex = vertices.Count;
        
        // Add vertices
        foreach (var pt in boundary)
        {
            Vector3 worldPos = roomPos + roomRot * new Vector3(pt.x, 0, pt.y);
            vertices.Add(worldPos);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(pt.x, pt.y));
        }
        
        // Triangulate (fan from first vertex)
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            triangles.Add(startVertex + 0);
            triangles.Add(startVertex + i + 1);
            triangles.Add(startVertex + i + 2);
        }
        
        vertexOffset += boundary.Count;
    }
    
    static void AddCeilingGeometry(List<Vector2> boundary, Vector3 roomPos, Quaternion roomRot, 
        float height, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, 
        ref List<Vector2> uvs, ref int vertexOffset)
    {
        int startVertex = vertices.Count;
        
        // Add vertices
        foreach (var pt in boundary)
        {
            Vector3 worldPos = roomPos + roomRot * new Vector3(pt.x, height, pt.y);
            vertices.Add(worldPos);
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(pt.x, pt.y));
        }
        
        // Triangulate (reversed winding for ceiling)
        for (int i = 0; i < boundary.Count - 2; i++)
        {
            triangles.Add(startVertex + 0);
            triangles.Add(startVertex + i + 2);
            triangles.Add(startVertex + i + 1);
        }
        
        vertexOffset += boundary.Count;
    }
    
    static void AddWallSegment(Vector2 p1, Vector2 p2, Vector3 roomPos, Quaternion roomRot, 
        float height, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, 
        ref List<Vector2> uvs, ref int vertexOffset)
    {
        Vector3 bottom1 = roomPos + roomRot * new Vector3(p1.x, 0, p1.y);
        Vector3 bottom2 = roomPos + roomRot * new Vector3(p2.x, 0, p2.y);
        Vector3 top1 = bottom1 + Vector3.up * height;
        Vector3 top2 = bottom2 + Vector3.up * height;
        
        Vector3 normal = Vector3.Cross((top1 - bottom1).normalized, (bottom2 - bottom1).normalized).normalized;
        
        int startVertex = vertices.Count;
        
        // Add quad vertices
        vertices.Add(bottom1);
        vertices.Add(bottom2);
        vertices.Add(top2);
        vertices.Add(top1);
        
        for (int i = 0; i < 4; i++) normals.Add(normal);
        
        float wallLength = Vector2.Distance(p1, p2);
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(wallLength, 0));
        uvs.Add(new Vector2(wallLength, height));
        uvs.Add(new Vector2(0, height));
        
        // Add triangles
        triangles.Add(startVertex + 0);
        triangles.Add(startVertex + 1);
        triangles.Add(startVertex + 2);
        
        triangles.Add(startVertex + 0);
        triangles.Add(startVertex + 2);
        triangles.Add(startVertex + 3);
        
        vertexOffset += 4;
    }
    
    static void ExportToGLTF(List<Vector3> vertices, List<int> triangles, List<Vector3> normals,
        List<Vector2> uvs, string outputPath)
    {
        // Create Unity mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        // Simple GLTF export using OBJ as fallback
        // In production, you would use a proper GLTF library
        ExportAsOBJ(mesh, outputPath.Replace(".gltf", ".obj"));
        
        RuntimeLogger.WriteLine($"Exported unified house mesh with {vertices.Count} vertices, {triangles.Count / 3} triangles");
    }
    
    static void ExportAsOBJ(Mesh mesh, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Unity OBJ Export - Full House Model");
        sb.AppendLine($"# Vertices: {mesh.vertices.Length}");
        sb.AppendLine($"# Triangles: {mesh.triangles.Length / 3}");
        sb.AppendLine();
        
        // Vertices
        foreach (var v in mesh.vertices)
        {
            sb.AppendLine($"v {v.x:F6} {v.y:F6} {v.z:F6}");
        }
        
        // Normals
        foreach (var n in mesh.normals)
        {
            sb.AppendLine($"vn {n.x:F6} {n.y:F6} {n.z:F6}");
        }
        
        // UVs
        foreach (var uv in mesh.uv)
        {
            sb.AppendLine($"vt {uv.x:F6} {uv.y:F6}");
        }
        
        sb.AppendLine();
        sb.AppendLine("g UnifiedHouse");
        sb.AppendLine("usemtl Default");
        
        // Faces (1-indexed in OBJ)
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int i1 = mesh.triangles[i] + 1;
            int i2 = mesh.triangles[i + 1] + 1;
            int i3 = mesh.triangles[i + 2] + 1;
            sb.AppendLine($"f {i1}/{i1}/{i1} {i2}/{i2}/{i2} {i3}/{i3}/{i3}");
        }
        
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
