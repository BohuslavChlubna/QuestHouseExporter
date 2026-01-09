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
    // CSV export only, no dependencies
public static void ExportToCsv(List<global::RoomData> offlineRooms, string outputPath)
    {
        try
        {
            var sb = new StringBuilder();
            // Header
            sb.AppendLine("Room Name,Floor Level,Floor Area (m2),Ceiling Height (m),Num Walls,Num Windows,Num Doors,Windows (WxH, pos),Doors (WxH, pos)");

            foreach (var offlineRoom in offlineRooms)
            {
                string roomName = EscapeCsv(offlineRoom.roomName ?? "Unknown Room");
                float floorY = 0f;
                if (offlineRoom.floorBoundary != null && offlineRoom.floorBoundary.Count > 0)
                    floorY = offlineRoom.floorBoundary[0].y;
                int floorLevel = Mathf.RoundToInt(floorY / 3.0f);
                float ceilingHeight = offlineRoom.ceilingHeight;
                float floorArea = (offlineRoom.floorBoundary != null) ? CalculatePolygonArea(offlineRoom.floorBoundary) : 0f;
                int numWalls = offlineRoom.walls != null ? offlineRoom.walls.Count : 0;

                // Windows/Doors
                var windows = new List<string>();
                var doors = new List<string>();
                int numWindows = 0, numDoors = 0;
                if (offlineRoom.anchors != null)
                {
                    foreach (var anchor in offlineRoom.anchors)
                    {
                        string anchorType = anchor.anchorType.ToUpper();
                        if (anchorType.Contains("WINDOW"))
                        {
                            windows.Add($"{anchor.scale.x:F2}x{anchor.scale.y:F2} ({anchor.position.x:F2},{anchor.position.y:F2},{anchor.position.z:F2})");
                            numWindows++;
                        }
                        else if (anchorType.Contains("DOOR"))
                        {
                            doors.Add($"{anchor.scale.x:F2}x{anchor.scale.y:F2} ({anchor.position.x:F2},{anchor.position.y:F2},{anchor.position.z:F2})");
                            numDoors++;
                        }
                    }
                }

                sb.AppendLine($"{roomName},{floorLevel},{floorArea:F2},{ceilingHeight:F2},{numWalls},{numWindows},{numDoors}," +
                    $"{EscapeCsv(string.Join("; ", windows))},{EscapeCsv(string.Join("; ", doors))}");
            }

            string csvPath = Path.Combine(outputPath, "rooms_export.csv");
            File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
            RuntimeLogger.WriteLine($"Exported CSV file: {csvPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("CSV export failed: " + ex.Message);
            RuntimeLogger.WriteLine("CSV export failed: " + ex.Message);
        }
    }

    static string EscapeCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains(";"))
            return '"' + s.Replace("\"", "\"\"") + '"';
        return s;
    }
    static float CalculatePolygonArea(List<Vector3> points)
    {
        if (points == null || points.Count < 3) return 0f;
        float area = 0f;
        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += points[i].x * points[j].z;
            area -= points[j].x * points[i].z;
        }
        return Mathf.Abs(area) / 2f;
    }

    // Veøejná metoda pro kompatibilitu s MRUKRoomExporter.cs
    public static void ExportToExcelXLSX(List<global::RoomData> offlineRooms, string outputPath)
    {
        ExportToCsv(offlineRooms, outputPath);
    }
}
