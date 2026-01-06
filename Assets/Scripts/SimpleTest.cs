using UnityEngine;

public class SimpleTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("===== SIMPLE TEST APP STARTED! =====");
        InvokeRepeating(nameof(LogHeartbeat), 1f, 1f);
    }
    
    void LogHeartbeat()
    {
        Debug.Log($"App is running... Time: {Time.time:F1}s");
    }
}
