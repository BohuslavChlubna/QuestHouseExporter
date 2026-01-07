using System;
using UnityEngine;

/// <summary>
/// Simple initialization - NO MRUK checks, uses offline RoomDataStorage
/// </summary>
public class InitializationScreen : MonoBehaviour
{
    System.Action onInitialized;
    
    public void Show(System.Action onComplete)
    {
        onInitialized = onComplete;
        
        Debug.Log("[InitializationScreen] Initializing with offline room data...");
        
        // Check if RoomDataStorage is ready (not MRUK!)
        if (RoomDataStorage.Instance != null)
        {
            var rooms = RoomDataStorage.Instance.GetCachedRooms();
            Debug.Log($"[InitializationScreen] Loaded {rooms.Count} offline room(s)");
        }
        else
        {
            Debug.LogWarning("[InitializationScreen] RoomDataStorage not ready yet");
        }
        
        // Complete initialization immediately (no MRUK wait!)
        CompleteInitialization();
    }
    
    void CompleteInitialization()
    {
        Debug.Log("[InitializationScreen] Initialization complete, switching to main menu");
        onInitialized?.Invoke();
    }
}

