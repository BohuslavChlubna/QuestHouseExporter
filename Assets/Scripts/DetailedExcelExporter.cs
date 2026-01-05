using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Exports detailed room data to Excel-compatible CSV format.
/// Includes room names from Quest scans, wall dimensions, ceiling heights, windows, and doors.
/// </summary>
public static class DetailedExcelExporter
{
    public class RoomData
    {
        public string roomName;
        public int floorLevel;
        public float floorArea;
        public float ceilingHeight;
        public List<WallData> walls = new List<WallData>();
        public List<WindowData> windows = new List<WindowData>();
        public List<DoorData> doors = new List<DoorData>();
    }
    
    public class WallData
    {
        public float length;
        public float height;
        public Vector3 startPos;
        public Vector3 endPos;
    }
    
    public class WindowData
    {
        public float width;
        public float height;
        public Vector3 position;
        public string wallSide;
    }
    
    public class DoorData
    {
        public float width;
        public float height;
        public Vector3 position;
        public string wallSide;
    }
    
    public static void ExportToExcel(List<MRUKRoom> rooms, string outputPath)
    {
        RuntimeLogger.WriteLine("=== DetailedExcelExporter: Starting Excel export ===");
        
        List<RoomData> allRoomData = new List<RoomData>();
        
        foreach (var room in rooms)
        {
            var roomData = ProcessRoom(room);
            if (roomData != null)
            {
                allRoomData.Add(roomData);
                RuntimeLogger.WriteLine($"Processed room: {roomData.roomName} with {roomData.walls.Count} walls, " +
                    $"{roomData.windows.Count} windows, {roomData.doors.Count} doors");
            }
        }
        
        // Export main summary sheet
        ExportSummarySheet(allRoomData, Path.Combine(Path.GetDirectoryName(outputPath), "rooms_summary.csv"));
        
        // Export detailed walls sheet
        ExportWallsSheet(allRoomData, Path.Combine(Path.GetDirectoryName(outputPath), "walls_details.csv"));
        
        // Export windows and doors sheet
        ExportOpeningsSheet(allRoomData, Path.Combine(Path.GetDirectoryName(outputPath), "openings_details.csv"));
        
        RuntimeLogger.WriteLine($"Excel export completed: {allRoomData.Count} rooms processed");
    }
    
    static RoomData ProcessRoom(MRUKRoom room)
    {
        if (room.FloorAnchor == null) return null;
        
        var data = new RoomData();
        
        // Get room name from Quest scan (Unity GameObject name)
        data.roomName = room.name ?? room.gameObject.name ?? "Unknown Room";
        
        // Determine floor level
        float floorY = room.FloorAnchor.transform.position.y;
        data.floorLevel = Mathf.RoundToInt(floorY / 3.0f);
        
        // Calculate ceiling height
        data.ceilingHeight = 2.5f; // default
        if (room.CeilingAnchor != null)
        {
            data.ceilingHeight = Mathf.Abs(room.CeilingAnchor.transform.position.y - floorY);
        }
        
        // Get room boundary
        var boundary = room.FloorAnchor.PlaneBoundary2D;
        if (boundary != null && boundary.Count > 0)
        {
            // Calculate floor area
            data.floorArea = CalculatePolygonArea(boundary);
            
            // Process walls
            Vector3 roomPos = room.FloorAnchor.transform.position;
            Quaternion roomRot = room.FloorAnchor.transform.rotation;
            
            for (int i = 0; i < boundary.Count; i++)
            {
                Vector2 p1 = boundary[i];
                Vector2 p2 = boundary[(i + 1) % boundary.Count];
                
                var wall = new WallData();
                wall.startPos = roomPos + roomRot * new Vector3(p1.x, 0, p1.y);
                wall.endPos = roomPos + roomRot * new Vector3(p2.x, 0, p2.y);
                wall.length = Vector2.Distance(p1, p2);
                wall.height = data.ceilingHeight;
                
                data.walls.Add(wall);
            }
        }
        
        // Process windows and doors from MRUK anchors
        ProcessRoomAnchors(room, data);
        
        return data;
    }
    
    static void ProcessRoomAnchors(MRUKRoom room, RoomData data)
    {
        // Get all anchors in room
        var anchors = room.GetComponentsInChildren<MRUKAnchor>();
        
        foreach (var anchor in anchors)
        {
            if (anchor == null) continue;
            
            // Check anchor label/type
            string label = anchor.Label?.ToString() ?? "";
            
            if (label.Contains("WINDOW") || label.Contains("Window"))
            {
                var window = new WindowData();
                window.position = anchor.transform.position;
                
                // Try to get dimensions from VolumeBounds
                if (anchor.VolumeBounds.HasValue)
                {
                    var bounds = anchor.VolumeBounds.Value;
                    window.width = bounds.size.x;
                    window.height = bounds.size.y;
                }
                else
                {
                    window.width = 1.0f;
                    window.height = 1.2f;
                }
                
                window.wallSide = DetermineWallSide(window.position, data);
                data.windows.Add(window);
            }
            else if (label.Contains("DOOR") || label.Contains("Door"))
            {
                var door = new DoorData();
                door.position = anchor.transform.position;
                
                if (anchor.VolumeBounds.HasValue)
                {
                    var bounds = anchor.VolumeBounds.Value;
                    door.width = bounds.size.x;
                    door.height = bounds.size.y;
                }
                else
                {
                    door.width = 0.9f;
                    door.height = 2.1f;
                }
                
                door.wallSide = DetermineWallSide(door.position, data);
                data.doors.Add(door);
            }
        }
    }
    
    static string DetermineWallSide(Vector3 position, RoomData room)
    {
        // Simple cardinal direction determination
        if (room.walls.Count == 0) return "Unknown";
        
        // Find closest wall
        float minDist = float.MaxValue;
        int closestWall = 0;
        
        for (int i = 0; i < room.walls.Count; i++)
        {
            Vector3 wallMid = (room.walls[i].startPos + room.walls[i].endPos) * 0.5f;
            float dist = Vector3.Distance(position, wallMid);
            if (dist < minDist)
            {
                minDist = dist;
                closestWall = i;
            }
        }
        
        return $"Wall_{closestWall + 1}";
    }
    
    static float CalculatePolygonArea(List<Vector2> points)
    {
        float area = 0f;
        int n = points.Count;
        
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += points[i].x * points[j].y;
            area -= points[j].x * points[i].y;
        }
        
        return Mathf.Abs(area) / 2f;
    }
    
    static void ExportSummarySheet(List<RoomData> rooms, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Room Name,Floor Level,Floor Area (m?),Ceiling Height (m),Num Walls,Num Windows,Num Doors");
        
        foreach (var room in rooms)
        {
            sb.AppendLine($"\"{room.roomName}\",{room.floorLevel},{room.floorArea:F2},{room.ceilingHeight:F2}," +
                $"{room.walls.Count},{room.windows.Count},{room.doors.Count}");
        }
        
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        RuntimeLogger.WriteLine($"Exported summary sheet: {path}");
    }
    
    static void ExportWallsSheet(List<RoomData> rooms, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Room Name,Wall Number,Length (m),Height (m),Start X,Start Y,Start Z,End X,End Y,End Z");
        
        foreach (var room in rooms)
        {
            for (int i = 0; i < room.walls.Count; i++)
            {
                var wall = room.walls[i];
                sb.AppendLine($"\"{room.roomName}\",{i + 1},{wall.length:F2},{wall.height:F2}," +
                    $"{wall.startPos.x:F2},{wall.startPos.y:F2},{wall.startPos.z:F2}," +
                    $"{wall.endPos.x:F2},{wall.endPos.y:F2},{wall.endPos.z:F2}");
            }
        }
        
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        RuntimeLogger.WriteLine($"Exported walls sheet: {path}");
    }
    
    static void ExportOpeningsSheet(List<RoomData> rooms, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Room Name,Type,Width (m),Height (m),Wall Side,Position X,Position Y,Position Z");
        
        foreach (var room in rooms)
        {
            foreach (var window in room.windows)
            {
                sb.AppendLine($"\"{room.roomName}\",Window,{window.width:F2},{window.height:F2}," +
                    $"\"{window.wallSide}\",{window.position.x:F2},{window.position.y:F2},{window.position.z:F2}");
            }
            
            foreach (var door in room.doors)
            {
                sb.AppendLine($"\"{room.roomName}\",Door,{door.width:F2},{door.height:F2}," +
                    $"\"{door.wallSide}\",{door.position.x:F2},{door.position.y:F2},{door.position.z:F2}");
            }
        }
        
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        RuntimeLogger.WriteLine($"Exported openings sheet: {path}");
    }
}
