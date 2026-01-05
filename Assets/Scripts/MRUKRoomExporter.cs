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
    public string exportFolder = "QuestHouseDesign";
    public bool exportOBJ = true;
    public bool exportGLB = false;
    public bool exportUnifiedGLTF = true;
    public bool exportSVGFloorPlans = true;
    public bool exportDetailedExcel = true;

    [Header("Google Drive Upload")]
    public bool enableDriveUpload = false;
    public GoogleDriveUploader driveUploader;

    public void ExportAll()
    {
        try
        {
            RuntimeLogger.Init(exportFolder);
            RuntimeLogger.WriteLine("=== MRUKRoomExporter.ExportAll started ===");

            string basePath = Path.Combine(Application.persistentDataPath, exportFolder);
            Directory.CreateDirectory(basePath);

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

            RuntimeLogger.WriteLine($"Found {rooms.Count} rooms in MRUK");

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

            // Export detailed Excel data
            if (exportDetailedExcel)
            {
                DetailedExcelExporter.ExportToExcel(rooms, basePath);
                RuntimeLogger.WriteLine($"Exported detailed Excel data to: {basePath}");
            }

            // Export summary files
            ExportSummaryCSV(basePath, jsonList);
            ExportSummaryJSON(basePath, jsonList);

            RuntimeLogger.WriteLine($"Export completed: {jsonList.Count} rooms exported to {basePath}");
            Debug.Log($"Exported {jsonList.Count} rooms to {basePath}");

            if (enableDriveUpload && driveUploader != null && driveUploader.autoUpload)
            {
                driveUploader.StartUploadDirectory(basePath);
            }
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
