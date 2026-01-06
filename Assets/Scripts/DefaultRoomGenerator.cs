using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Generates a default test room (4x5m rectangle with door and window) for testing without scanning.
/// </summary>
public class DefaultRoomGenerator : MonoBehaviour
{
    public static MRUKRoom GenerateDefaultRoom()
    {
        Debug.Log("[DefaultRoomGenerator] Creating default test room...");
        
        var roomObj = new GameObject("DefaultTestRoom");
        var room = roomObj.AddComponent<MRUKRoom>();
        
        // Create floor anchor (4x5 meters rectangle)
        CreateFloorAnchor(room, roomObj);
        
        // Create 4 walls
        CreateWalls(room, roomObj);
        
        // Create ceiling
        CreateCeilingAnchor(room, roomObj);
        
        // Create door on one wall
        CreateDoor(room, roomObj);
        
        // Create window on opposite wall
        CreateWindow(room, roomObj);
        
        Debug.Log("[DefaultRoomGenerator] Default room created successfully");
        RuntimeLogger.WriteLine("[DefaultRoomGenerator] Created default 4x5m test room with door and window");
        
        return room;
    }
    
    static void CreateFloorAnchor(MRUKRoom room, GameObject roomObj)
    {
        var floorObj = new GameObject("Floor");
        floorObj.transform.SetParent(roomObj.transform);
        floorObj.transform.localPosition = Vector3.zero;
        
        var anchor = floorObj.AddComponent<MRUKAnchor>();
        
        // Set boundary for 4x5 meter rectangle
        var boundary = new List<Vector2>
        {
            new Vector2(-2f, -2.5f),  // bottom-left
            new Vector2(2f, -2.5f),   // bottom-right
            new Vector2(2f, 2.5f),    // top-right
            new Vector2(-2f, 2.5f)    // top-left
        };
        
        // Use reflection to set the boundary (MRUK API may vary)
        var field = typeof(MRUKAnchor).GetField("_planeBoundary2D", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(anchor, boundary);
        }
        
        // Set plane rect
        var planeRect = new Rect(-2f, -2.5f, 4f, 5f);
        var rectField = typeof(MRUKAnchor).GetField("_planeRect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rectField != null)
        {
            rectField.SetValue(anchor, planeRect);
        }
        
        // Set label
        var labelField = typeof(MRUKAnchor).GetField("_label", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (labelField != null)
        {
            labelField.SetValue(anchor, MRUKAnchor.SceneLabels.FLOOR);
        }
        
        // Assign to room
        var roomFloorField = typeof(MRUKRoom).GetField("FloorAnchor", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (roomFloorField != null)
        {
            roomFloorField.SetValue(room, anchor);
        }
    }
    
    static void CreateCeilingAnchor(MRUKRoom room, GameObject roomObj)
    {
        var ceilingObj = new GameObject("Ceiling");
        ceilingObj.transform.SetParent(roomObj.transform);
        ceilingObj.transform.localPosition = new Vector3(0, 2.5f, 0); // 2.5m height
        
        var anchor = ceilingObj.AddComponent<MRUKAnchor>();
        
        // Same boundary as floor
        var boundary = new List<Vector2>
        {
            new Vector2(-2f, -2.5f),
            new Vector2(2f, -2.5f),
            new Vector2(2f, 2.5f),
            new Vector2(-2f, 2.5f)
        };
        
        var field = typeof(MRUKAnchor).GetField("_planeBoundary2D", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(anchor, boundary);
        }
        
        var planeRect = new Rect(-2f, -2.5f, 4f, 5f);
        var rectField = typeof(MRUKAnchor).GetField("_planeRect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rectField != null)
        {
            rectField.SetValue(anchor, planeRect);
        }
        
        var labelField = typeof(MRUKAnchor).GetField("_label", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (labelField != null)
        {
            labelField.SetValue(anchor, MRUKAnchor.SceneLabels.CEILING);
        }
        
        var roomCeilingField = typeof(MRUKRoom).GetField("CeilingAnchor", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (roomCeilingField != null)
        {
            roomCeilingField.SetValue(room, anchor);
        }
    }
    
    static void CreateWalls(MRUKRoom room, GameObject roomObj)
    {
        // Wall positions and orientations for 4x5m room
        Vector3[] wallPositions = new Vector3[]
        {
            new Vector3(0, 1.25f, -2.5f),   // South wall (width 4m)
            new Vector3(2f, 1.25f, 0),      // East wall (width 5m)
            new Vector3(0, 1.25f, 2.5f),    // North wall (width 4m)
            new Vector3(-2f, 1.25f, 0)      // West wall (width 5m)
        };
        
        Quaternion[] wallRotations = new Quaternion[]
        {
            Quaternion.Euler(0, 0, 0),      // South
            Quaternion.Euler(0, 90, 0),     // East
            Quaternion.Euler(0, 180, 0),    // North
            Quaternion.Euler(0, 270, 0)     // West
        };
        
        float[] wallWidths = new float[] { 4f, 5f, 4f, 5f };
        float wallHeight = 2.5f;
        
        var wallAnchors = new List<MRUKAnchor>();
        
        for (int i = 0; i < 4; i++)
        {
            var wallObj = new GameObject($"Wall_{i}");
            wallObj.transform.SetParent(roomObj.transform);
            wallObj.transform.localPosition = wallPositions[i];
            wallObj.transform.localRotation = wallRotations[i];
            
            var anchor = wallObj.AddComponent<MRUKAnchor>();
            
            // Wall boundary (rectangle)
            float halfWidth = wallWidths[i] / 2f;
            float halfHeight = wallHeight / 2f;
            
            var boundary = new List<Vector2>
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(halfWidth, -halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(-halfWidth, halfHeight)
            };
            
            var field = typeof(MRUKAnchor).GetField("_planeBoundary2D", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(anchor, boundary);
            }
            
            var planeRect = new Rect(-halfWidth, -halfHeight, wallWidths[i], wallHeight);
            var rectField = typeof(MRUKAnchor).GetField("_planeRect", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rectField != null)
            {
                rectField.SetValue(anchor, planeRect);
            }
            
            var labelField = typeof(MRUKAnchor).GetField("_label", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (labelField != null)
            {
                labelField.SetValue(anchor, MRUKAnchor.SceneLabels.WALL_FACE);
            }
            
            wallAnchors.Add(anchor);
        }
        
        // Assign walls to room
        var roomWallsField = typeof(MRUKRoom).GetField("WallAnchors", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (roomWallsField != null)
        {
            roomWallsField.SetValue(room, wallAnchors);
        }
    }
    
    static void CreateDoor(MRUKRoom room, GameObject roomObj)
    {
        var doorObj = new GameObject("Door");
        doorObj.transform.SetParent(roomObj.transform);
        doorObj.transform.localPosition = new Vector3(-1f, 1f, -2.5f); // On south wall, left side
        doorObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
        
        var anchor = doorObj.AddComponent<MRUKAnchor>();
        
        // Door: 0.9m wide, 2m tall
        var boundary = new List<Vector2>
        {
            new Vector2(-0.45f, 0),
            new Vector2(0.45f, 0),
            new Vector2(0.45f, 2f),
            new Vector2(-0.45f, 2f)
        };
        
        var field = typeof(MRUKAnchor).GetField("_planeBoundary2D", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(anchor, boundary);
        }
        
        var planeRect = new Rect(-0.45f, 0, 0.9f, 2f);
        var rectField = typeof(MRUKAnchor).GetField("_planeRect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rectField != null)
        {
            rectField.SetValue(anchor, planeRect);
        }
        
        var labelField = typeof(MRUKAnchor).GetField("_label", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (labelField != null)
        {
            labelField.SetValue(anchor, MRUKAnchor.SceneLabels.DOOR_FRAME);
        }
    }
    
    static void CreateWindow(MRUKRoom room, GameObject roomObj)
    {
        var windowObj = new GameObject("Window");
        windowObj.transform.SetParent(roomObj.transform);
        windowObj.transform.localPosition = new Vector3(0, 1.5f, 2.5f); // On north wall, centered, higher up
        windowObj.transform.localRotation = Quaternion.Euler(0, 180, 0);
        
        var anchor = windowObj.AddComponent<MRUKAnchor>();
        
        // Window: 1.2m wide, 1m tall
        var boundary = new List<Vector2>
        {
            new Vector2(-0.6f, -0.5f),
            new Vector2(0.6f, -0.5f),
            new Vector2(0.6f, 0.5f),
            new Vector2(-0.6f, 0.5f)
        };
        
        var field = typeof(MRUKAnchor).GetField("_planeBoundary2D", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(anchor, boundary);
        }
        
        var planeRect = new Rect(-0.6f, -0.5f, 1.2f, 1f);
        var rectField = typeof(MRUKAnchor).GetField("_planeRect", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rectField != null)
        {
            rectField.SetValue(anchor, planeRect);
        }
        
        var labelField = typeof(MRUKAnchor).GetField("_label", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (labelField != null)
        {
            labelField.SetValue(anchor, MRUKAnchor.SceneLabels.WINDOW_FRAME);
        }
    }
}
