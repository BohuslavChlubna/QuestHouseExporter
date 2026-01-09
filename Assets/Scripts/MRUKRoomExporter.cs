using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Main exporter using MRUK (Mixed Reality Utility Kit) to access Quest Scene API rooms.
/// Exports room meshes, floor plans (SVG), and metadata (JSON/CSV/Excel).
/// </summary>
public class MRUKRoomExporter : MonoBehaviour
{
    [Header("Export Settings")]
    public string exportFolder = "Export";
    public bool exportOBJ = true;
    public bool exportGLB = false;
    public bool exportUnifiedGLTF = true;
    public bool exportSVGFloorPlans = true;
    public bool exportDetailedExcel = true;

    /// <summary>
    /// Export from offline RoomData (NO MRUK dependency!)
    /// Performs same exports as PerformExport but from offline data.
    /// </summary>
    public void ExportFromOfflineRooms(List<RoomData> offlineRooms)
    {
        if (offlineRooms == null || offlineRooms.Count == 0)
        {
            RuntimeLogger.WriteLine("ERROR: No offline rooms to export.");
            Debug.LogError("ExportFromOfflineRooms: offlineRooms is null or empty.");
            return;
        }
        
        try
        {
            // Vytvoøení složky Export/<timestamp>/
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string basePath = Path.Combine(Application.persistentDataPath, exportFolder, timestamp);
            Directory.CreateDirectory(basePath);

            RuntimeLogger.Init(Path.Combine(exportFolder, timestamp));
            RuntimeLogger.WriteLine("=== MRUKRoomExporter.ExportFromOfflineRooms started ===");
            RuntimeLogger.WriteLine($"Exporting {offlineRooms.Count} offline rooms");

            // Group rooms by floor level
            var floorGroups = GroupRoomsByFloorOffline(offlineRooms);
            RuntimeLogger.WriteLine($"Detected {floorGroups.Count} floor levels");

            List<Dictionary<string, object>> jsonList = new List<Dictionary<string, object>>();
            int roomIndex = 0;

            foreach (var floorKvp in floorGroups.OrderBy(kv => kv.Key))
            {
                int floorLevel = floorKvp.Key;
                var floorRooms = floorKvp.Value;
                RuntimeLogger.WriteLine($"Processing floor {floorLevel} with {floorRooms.Count} rooms");

                foreach (var room in floorRooms)
                {
                    roomIndex++;
                    string roomName = room.roomName ?? $"Room_{roomIndex}";
                    RuntimeLogger.WriteLine($"  Exporting room: {roomName}");

                    var roomData = ExportRoomFromOfflineData(room, roomName, floorLevel, basePath);
                    if (roomData != null) jsonList.Add(roomData);
                }

                // Export SVG floor plan for this floor
                if (exportSVGFloorPlans)
                {
                    string svgPath = Path.Combine(basePath, $"Floor_{floorLevel}_plan.svg");
                    SVGFloorPlanGenerator.GenerateFloorPlanFromOfflineData(floorRooms, svgPath, floorLevel);
                    RuntimeLogger.WriteLine($"  Exported SVG floor plan: {svgPath}");
                }
            }

            // Export unified GLTF/OBJ house model
            if (exportUnifiedGLTF || exportOBJ)
            {
                string modelPath = Path.Combine(basePath, exportOBJ ? "UnifiedHouse.obj" : "UnifiedHouse.gltf");
                GLTFHouseExporter.ExportUnifiedHouseFromOfflineData(offlineRooms, modelPath, exportOBJ);
                RuntimeLogger.WriteLine($"Exported unified house model: {modelPath}");
            }

            // Export detailed Excel data (novì pouze jeden .xlsx soubor)
            if (exportDetailedExcel)
            {
                DetailedExcelExporter.ExportToExcelXLSX(offlineRooms, basePath);
                RuntimeLogger.WriteLine($"Exported detailed Excel data to: {basePath}");
            }

            // Export summary JSON (ponecháme pro kontrolu)
            ExportSummaryJSON(basePath, jsonList);

            // Save raw offline room data as well
            string offlineJsonPath = Path.Combine(basePath, "offline_rooms_raw.json");
            var wrapper = new RoomDataListWrapper { rooms = offlineRooms };
            string rawJson = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(offlineJsonPath, rawJson);

            RuntimeLogger.WriteLine($"Export completed: {jsonList.Count} rooms exported to {basePath}");
            Debug.Log($"[MRUKRoomExporter] Export complete: {basePath}");
            RuntimeLogger.WriteLine("=== Export completed successfully ===");
        }
        catch (Exception ex)
        {
            RuntimeLogger.WriteLine($"ERROR during offline export: {ex.Message}");
            RuntimeLogger.LogException(ex);
            Debug.LogError($"ExportFromOfflineRooms failed: {ex}");
        }
    }
    
    Dictionary<int, List<RoomData>> GroupRoomsByFloorOffline(List<RoomData> rooms)
    {
        var groups = new Dictionary<int, List<RoomData>>();
        foreach (var room in rooms)
        {
            float floorY = 0f;
            if (room.floorBoundary != null && room.floorBoundary.Count > 0)
            {
                floorY = room.floorBoundary[0].y;
            }
            
            int floorLevel = Mathf.RoundToInt(floorY / 3.0f);
            if (!groups.ContainsKey(floorLevel)) groups[floorLevel] = new List<RoomData>();
            groups[floorLevel].Add(room);
        }
        return groups;
    }
    
    Dictionary<string, object> ExportRoomFromOfflineData(RoomData room, string roomName, int floorLevel, string basePath)
    {
        var roomData = new Dictionary<string, object>();
        roomData["name"] = roomName;
        roomData["floor_level"] = floorLevel;
        
        // Calculate floor area
        float area = CalculateFloorArea(room.floorBoundary);
        roomData["area_m2"] = area;
        
        // Count windows and doors from anchors
        int windowCount = 0;
        int doorCount = 0;
        List<object> windows = new List<object>();
        List<object> doors = new List<object>();
        
        if (room.anchors != null)
        {
            foreach (var anchor in room.anchors)
            {
                string anchorType = anchor.anchorType.ToUpper();
                if (anchorType.Contains("WINDOW"))
                {
                    windowCount++;
                    windows.Add(new Dictionary<string, object>
                    {
                        { "position", $"{anchor.position.x:F2},{anchor.position.y:F2},{anchor.position.z:F2}" },
                        { "size", $"{anchor.scale.x:F2}x{anchor.scale.y:F2}" }
                    });
                }
                else if (anchorType.Contains("DOOR"))
                {
                    doorCount++;
                    doors.Add(new Dictionary<string, object>
                    {
                        { "position", $"{anchor.position.x:F2},{anchor.position.y:F2},{anchor.position.z:F2}" },
                        { "size", $"{anchor.scale.x:F2}x{anchor.scale.y:F2}" }
                    });
                }
            }
        }
        
        roomData["windows"] = windows;
        roomData["doors"] = doors;
        roomData["num_walls"] = room.walls != null ? room.walls.Count : 0;
        roomData["ceiling_height"] = room.ceilingHeight;
        
        // Export individual room OBJ if enabled
        if (exportOBJ)
        {
            string objPath = Path.Combine(basePath, $"{roomName}_Floor{floorLevel}.obj");
            ExportRoomAsOBJ(room, objPath);
            RuntimeLogger.WriteLine($"    Exported OBJ: {objPath}");
        }
        
        RuntimeLogger.WriteLine($"    Room {roomName}: floor={floorLevel}, area={area:F2}m?, walls={room.walls?.Count ?? 0}, windows={windowCount}, doors={doorCount}");
        return roomData;
    }
    
    float CalculateFloorArea(List<Vector3> boundary)
    {
        if (boundary == null || boundary.Count < 3) return 0f;
        
        // Shoelace formula for polygon area
        float area = 0f;
        int n = boundary.Count;
        
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += boundary[i].x * boundary[j].z;
            area -= boundary[j].x * boundary[i].z;
        }
        
        return Mathf.Abs(area) / 2f;
    }
    
    void ExportRoomAsOBJ(RoomData room, string path)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# Unity OBJ Export - {room.roomName}");
            sb.AppendLine($"# Generated: {System.DateTime.Now}");
            sb.AppendLine();
            
            int vertexOffset = 1; // OBJ indices start at 1
            
            // Export floor
            if (room.floorBoundary != null && room.floorBoundary.Count > 0)
            {
                sb.AppendLine("# Floor");
                sb.AppendLine("g Floor");
                
                foreach (var v in room.floorBoundary)
                {
                    sb.AppendLine($"v {v.x:F6} {v.y:F6} {v.z:F6}");
                }
                
                sb.AppendLine("vn 0.000000 1.000000 0.000000");
                
                // Triangulate floor (fan from first vertex)
                for (int i = 0; i < room.floorBoundary.Count - 2; i++)
                {
                    sb.AppendLine($"f {vertexOffset}//{vertexOffset} {vertexOffset + i + 1}//{vertexOffset} {vertexOffset + i + 2}//{vertexOffset}");
                }
                
                vertexOffset += room.floorBoundary.Count;
            }
            
            // Export walls
            if (room.walls != null && room.walls.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("# Walls");
                sb.AppendLine("g Walls");
                
                foreach (var wall in room.walls)
                {
                    Vector3 bottom1 = wall.start;
                    Vector3 bottom2 = wall.end;
                    Vector3 top1 = wall.start + Vector3.up * wall.height;
                    Vector3 top2 = wall.end + Vector3.up * wall.height;
                    
                    sb.AppendLine($"v {bottom1.x:F6} {bottom1.y:F6} {bottom1.z:F6}");
                    sb.AppendLine($"v {bottom2.x:F6} {bottom2.y:F6} {bottom2.z:F6}");
                    sb.AppendLine($"v {top2.x:F6} {top2.y:F6} {top2.z:F6}");
                    sb.AppendLine($"v {top1.x:F6} {top1.y:F6} {top1.z:F6}");
                    
                    // Calculate normal
                    Vector3 dir = (bottom2 - bottom1).normalized;
                    Vector3 normal = Vector3.Cross(Vector3.up, dir).normalized;
                    sb.AppendLine($"vn {normal.x:F6} {normal.y:F6} {normal.z:F6}");
                    
                    int v = vertexOffset;
                    sb.AppendLine($"f {v}//{v} {v+1}//{v} {v+2}//{v}");
                    sb.AppendLine($"f {v}//{v} {v+2}//{v} {v+3}//{v}");
                    
                    vertexOffset += 4;
                }
            }
            
            // Export ceiling (sloped or flat)
            if (room.hasSlopedCeiling && room.ceilingBoundary != null && room.ceilingBoundary.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("# Ceiling (Sloped)");
                sb.AppendLine("g Ceiling_Sloped");
                
                foreach (var v in room.ceilingBoundary)
                {
                    sb.AppendLine($"v {v.x:F6} {v.y:F6} {v.z:F6}");
                }
                
                sb.AppendLine("vn 0.000000 -1.000000 0.000000"); // Down-facing normal
                
                // Triangulate ceiling (reversed winding)
                for (int i = 0; i < room.ceilingBoundary.Count - 2; i++)
                {
                    sb.AppendLine($"f {vertexOffset}//{vertexOffset} {vertexOffset + i + 2}//{vertexOffset} {vertexOffset + i + 1}//{vertexOffset}");
                }
                
                vertexOffset += room.ceilingBoundary.Count;
            }
            
            File.WriteAllText(path, sb.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to export room OBJ: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Export from cached room list (snapshot from MenuController).
    /// This ensures exported data matches what user sees in visualizations.
    /// </summary>
    public void ExportFromCachedRooms(List<MRUKRoom> cachedRooms)
    {
        if (cachedRooms == null || cachedRooms.Count == 0)
        {
            RuntimeLogger.WriteLine("ERROR: No cached rooms to export.");
            Debug.LogError("ExportFromCachedRooms: cachedRooms is null or empty.");
            return;
        }

        PerformExport(cachedRooms);
    }

    /// <summary>
    /// Legacy method - exports directly from MRUK.Instance.Rooms.
    /// Kept for backwards compatibility.
    /// </summary>
    public void ExportAll()
    {
        if (MRUK.Instance == null)
        {
            RuntimeLogger.WriteLine("ERROR: MRUK.Instance is null. Ensure MRUK is initialized in the scene.");
            Debug.LogError("MRUK.Instance is null. Add MRUK component to scene.");
            return;
        }

        var rooms = MRUK.Instance.Rooms;
        if (rooms == null || rooms.Count == 0)
        {
            RuntimeLogger.WriteLine("No rooms found in MRUK.Instance.Rooms. User may need to scan space first.");
            Debug.LogWarning("No rooms found. Scan the space in Quest Settings first.");
            return;
        }

        PerformExport(rooms);
    }

    /// <summary>
    /// Core export logic - works with any list of MRUKRooms.
    /// </summary>
    void PerformExport(List<MRUKRoom> rooms)
    {
        try
        {

            // Vytvoøení složky Export/<timestamp>/
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string basePath = Path.Combine(Application.persistentDataPath, exportFolder, timestamp);
            Directory.CreateDirectory(basePath);

            RuntimeLogger.Init(Path.Combine(exportFolder, timestamp));
            RuntimeLogger.WriteLine("=== MRUKRoomExporter.PerformExport started ===");

            RuntimeLogger.WriteLine($"Found {rooms.Count} rooms to export");

            // Group rooms by floor level (based on floor anchor Y position)
            var floorGroups = GroupRoomsByFloor(rooms);
            RuntimeLogger.WriteLine($"Detected {floorGroups.Count} floor levels");

            List<Dictionary<string, object>> jsonList = new List<Dictionary<string, object>>();
            int roomIndex = 0;

            foreach (var floorKvp in floorGroups.OrderBy(kv => kv.Key))
            {
                int floorLevel = floorKvp.Key;
                var floorRooms = floorKvp.Value;
                RuntimeLogger.WriteLine($"Processing floor {floorLevel} with {floorRooms.Count} rooms");

                foreach (var room in floorRooms)
                {
                    roomIndex++;
                    string roomName = room.name ?? $"Room_{roomIndex}";
                    RuntimeLogger.WriteLine($"  Exporting room: {roomName}");

                    var roomData = ExportRoom(room, roomName, floorLevel, basePath);
                    if (roomData != null) jsonList.Add(roomData);
                }

            // Export SVG floor plan for this floor
                if (exportSVGFloorPlans)
                {
                    string svgPath = Path.Combine(basePath, $"Floor_{floorLevel}_plan.svg");
                    SVGFloorPlanGenerator.GenerateFloorPlan(floorRooms, svgPath, floorLevel);
                    RuntimeLogger.WriteLine($"  Exported SVG floor plan: {svgPath}");
                }
            }

            // Export unified GLTF house model
            if (exportUnifiedGLTF)
            {
                string gltfPath = Path.Combine(basePath, "UnifiedHouse.gltf");
                GLTFHouseExporter.ExportUnifiedHouse(rooms, gltfPath);
                RuntimeLogger.WriteLine($"Exported unified GLTF house: {gltfPath}");
            }


            // Excel export se provádí pouze z offline dat, nikoliv z MRUKRoom

            // Export summary JSON (ponecháme pro kontrolu)
            ExportSummaryJSON(basePath, jsonList);

            RuntimeLogger.WriteLine($"Export completed: {jsonList.Count} rooms exported to {basePath}");
            Debug.Log($"Exported {jsonList.Count} rooms to {basePath}");

            RuntimeLogger.WriteLine("=== Export completed successfully ===");
        }
        catch (Exception ex)
        {
            RuntimeLogger.LogException(ex);
            Debug.LogError("Export failed: " + ex.Message);
        }
    }

    Dictionary<int, List<MRUKRoom>> GroupRoomsByFloor(List<MRUKRoom> rooms)
    {
        var groups = new Dictionary<int, List<MRUKRoom>>();
        foreach (var room in rooms)
        {
            float floorY = room.FloorAnchor != null ? room.FloorAnchor.transform.position.y : 0f;
            int floorLevel = Mathf.RoundToInt(floorY / 3.0f); // assume ~3m per floor
            if (!groups.ContainsKey(floorLevel)) groups[floorLevel] = new List<MRUKRoom>();
            groups[floorLevel].Add(room);
        }
        return groups;
    }

    Dictionary<string, object> ExportRoom(MRUKRoom room, string roomName, int floorLevel, string basePath)
    {
        var roomData = new Dictionary<string, object>();
        roomData["name"] = roomName;
        roomData["floor_level"] = floorLevel;

        // TODO: Extract mesh from room anchors, detect windows/doors, calculate area
        // For now, placeholder data
        roomData["area_m2"] = 0f;
        roomData["windows"] = new List<object>();
        roomData["doors"] = new List<object>();

        RuntimeLogger.WriteLine($"    Room {roomName}: floor={floorLevel}, area=0 (TODO)");
        return roomData;
    }

    void ExportSummaryCSV(string basePath, List<Dictionary<string, object>> rooms)
    {
        var lines = new List<string>();
        lines.Add("RoomName,FloorLevel,AreaM2,NumWindows,NumDoors");
        foreach (var r in rooms)
        {
            string name = r.ContainsKey("name") ? r["name"].ToString() : "";
            int floor = r.ContainsKey("floor_level") ? Convert.ToInt32(r["floor_level"]) : 0;
            float area = r.ContainsKey("area_m2") ? Convert.ToSingle(r["area_m2"]) : 0f;
            int numW = 0, numD = 0;
            lines.Add($"{name},{floor},{area:F2},{numW},{numD}");
        }
        File.WriteAllLines(Path.Combine(basePath, "rooms_summary.csv"), lines);
    }

    void ExportSummaryJSON(string basePath, List<Dictionary<string, object>> rooms)
    {
        string json = MiniJSON.Serialize(rooms);
        File.WriteAllText(Path.Combine(basePath, "rooms_summary.json"), json);
    }
}


