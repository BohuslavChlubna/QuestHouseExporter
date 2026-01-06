using System;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Simple initialization - checks for rooms and creates default test room if none exist.
/// No UI, just immediate initialization.
/// </summary>
public class InitializationScreen : MonoBehaviour
{
    System.Action onInitialized;
    
    public void Show(System.Action onComplete)
    {
        onInitialized = onComplete;
        
        // Check for rooms immediately, no waiting
        Debug.Log("[InitializationScreen] Checking for existing rooms...");
        
        // Wait a bit for MRUK to potentially initialize
        StartCoroutine(WaitForMRUKOrTimeout());
    }
    
    System.Collections.IEnumerator WaitForMRUKOrTimeout()
    {
        float waitTime = 0f;
        float maxWait = 2f; // Wait max 2 seconds for MRUK
        
        // Wait for MRUK to initialize or timeout
        while (waitTime < maxWait)
        {
            if (MRUK.Instance != null)
            {
                Debug.Log("[InitializationScreen] MRUK initialized!");
                break;
            }
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        // Check if we have rooms
        if (MRUK.Instance == null || MRUK.Instance.Rooms == null || MRUK.Instance.Rooms.Count == 0)
        {
            Debug.Log("[InitializationScreen] No rooms found and MRUK not ready - continuing without visualization");
            // Don't create default room if MRUK isn't ready - it won't work anyway
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

