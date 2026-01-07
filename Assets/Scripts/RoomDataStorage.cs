using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Offline storage for room data - independent of MRUK
/// Stores serialized room geometry for visualization and export
/// </summary>
[Serializable]
public class RoomData
{
    public string roomId;
    public string roomName;
    public List<Vector3> floorBoundary = new List<Vector3>();
    public List<Vector3> ceilingBoundary = new List<Vector3>(); // NEW: Support for sloped ceilings (FW83+)
    public List<WallData> walls = new List<WallData>();
    public List<AnchorData> anchors = new List<AnchorData>();
    public float ceilingHeight = 2.5f; // Fallback for flat ceilings
    public bool hasSlopedCeiling = false; // NEW: Flag to indicate sloped ceiling
}

[Serializable]
public class WallData
{
    public Vector3 start;
    public Vector3 end;
    public float height;
    public List<AnchorData> attachedAnchors = new List<AnchorData>();
}

[Serializable]
public class AnchorData
{
    public string anchorType; // "DOOR", "WINDOW", "TABLE", etc.
    public Vector3 position;
    public Vector3 scale;
    public Quaternion rotation;
}

/// <summary>
/// Manages persistent storage of room data
/// Saves to Application.persistentDataPath as JSON
/// </summary>
public class RoomDataStorage : MonoBehaviour
{
    static RoomDataStorage instance;
    public static RoomDataStorage Instance => instance;
    
    List<RoomData> cachedRooms = new List<RoomData>();
    string saveFilePath;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            saveFilePath = System.IO.Path.Combine(Application.persistentDataPath, "cached_rooms.json");
            LoadFromDisk();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public List<RoomData> GetCachedRooms()
    {
        if (cachedRooms.Count == 0)
        {
            Debug.Log("[RoomDataStorage] No cached rooms, creating default 4x5m room");
            CreateDefaultRoom();
        }
        return new List<RoomData>(cachedRooms);
    }
    
    public void SaveRooms(List<RoomData> rooms)
    {
        cachedRooms = new List<RoomData>(rooms);
        SaveToDisk();
        Debug.Log($"[RoomDataStorage] Saved {rooms.Count} rooms to disk");
    }
    
    void CreateDefaultRoom()
    {
        var defaultRoom = new RoomData
        {
            roomId = "default_test_room",
            roomName = "Default 4x5m Room",
            ceilingHeight = 2.5f
        };
        
        // Floor boundary (4x5 meters)
        defaultRoom.floorBoundary = new List<Vector3>
        {
            new Vector3(-2f, 0f, -2.5f),  // bottom-left
            new Vector3(2f, 0f, -2.5f),   // bottom-right
            new Vector3(2f, 0f, 2.5f),    // top-right
            new Vector3(-2f, 0f, 2.5f)    // top-left
        };
        
        // Walls
        defaultRoom.walls = new List<WallData>
        {
            new WallData { start = new Vector3(-2f, 0f, -2.5f), end = new Vector3(2f, 0f, -2.5f), height = 2.5f },  // Front
            new WallData { start = new Vector3(2f, 0f, -2.5f), end = new Vector3(2f, 0f, 2.5f), height = 2.5f },    // Right
            new WallData { start = new Vector3(2f, 0f, 2.5f), end = new Vector3(-2f, 0f, 2.5f), height = 2.5f },    // Back
            new WallData { start = new Vector3(-2f, 0f, 2.5f), end = new Vector3(-2f, 0f, -2.5f), height = 2.5f }   // Left
        };
        
        // Add door on front wall
        defaultRoom.walls[0].attachedAnchors.Add(new AnchorData
        {
            anchorType = "DOOR",
            position = new Vector3(0f, 0f, -2.5f),
            scale = new Vector3(0.9f, 2.0f, 0.1f),
            rotation = Quaternion.identity
        });
        
        // Add window on back wall
        defaultRoom.walls[2].attachedAnchors.Add(new AnchorData
        {
            anchorType = "WINDOW",
            position = new Vector3(0f, 1.5f, 2.5f),
            scale = new Vector3(1.2f, 1.0f, 0.1f),
            rotation = Quaternion.identity
        });
        
        cachedRooms.Add(defaultRoom);
        SaveToDisk();
        
        Debug.Log("[RoomDataStorage] Created default 4x5m test room");
        RuntimeLogger.WriteLine("[RoomDataStorage] No saved data found - created default test room (4x5m, 1 door, 1 window)");
    }
    
    void LoadFromDisk()
    {
        if (System.IO.File.Exists(saveFilePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(saveFilePath);
                var wrapper = JsonUtility.FromJson<RoomDataListWrapper>(json);
                cachedRooms = wrapper.rooms;
                Debug.Log($"[RoomDataStorage] Loaded {cachedRooms.Count} rooms from disk");
                RuntimeLogger.WriteLine($"[RoomDataStorage] Loaded {cachedRooms.Count} cached room(s) from previous session");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomDataStorage] Failed to load: {ex.Message}");
                cachedRooms.Clear();
            }
        }
        else
        {
            Debug.Log("[RoomDataStorage] No save file found");
        }
    }
    
    void SaveToDisk()
    {
        try
        {
            var wrapper = new RoomDataListWrapper { rooms = cachedRooms };
            string json = JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(saveFilePath, json);
            Debug.Log($"[RoomDataStorage] Saved to: {saveFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RoomDataStorage] Failed to save: {ex.Message}");
        }
    }
}

[Serializable]
class RoomDataListWrapper
{
    public List<RoomData> rooms = new List<RoomData>();
}
