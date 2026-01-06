using System;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Simple initialization - checks for rooms immediately.
/// No UI, just immediate initialization.
/// </summary>
public class InitializationScreen : MonoBehaviour
{
    System.Action onInitialized;
    
    public void Show(System.Action onComplete)
    {
        onInitialized = onComplete;
        
        Debug.Log("[InitializationScreen] Checking for existing rooms...");
        
        // Check immediately - don't wait for MRUK
        if (MRUK.Instance == null)
        {
            Debug.Log("[InitializationScreen] MRUK not initialized yet - continuing anyway");
        }
        else if (MRUK.Instance.Rooms == null || MRUK.Instance.Rooms.Count == 0)
        {
            Debug.Log("[InitializationScreen] MRUK initialized but no rooms found");
        }
        else
        {
            Debug.Log($"[InitializationScreen] Found {MRUK.Instance.Rooms.Count} existing rooms");
        }
        
        // Complete initialization immediately
        CompleteInitialization();
    }
    
    void CompleteInitialization()
    {
        Debug.Log("[InitializationScreen] Initialization complete, switching to main menu");
        onInitialized?.Invoke();
    }
}

