using UnityEngine;
using UnityEditor;

/// <summary>
/// Simple editor window for entering Quest IP address for wireless ADB connection.
/// Shows current connection status.
/// </summary>
public class WirelessADBWindow : EditorWindow
{
    string ipAddress = "";
    string connectionStatus = "";
    
    public static void ShowWindow(string defaultIP)
    {
        var window = GetWindow<WirelessADBWindow>("Connect to Quest");
        window.ipAddress = defaultIP ?? "";
        window.minSize = new Vector2(400, 180);
        window.maxSize = new Vector2(400, 180);
        window.UpdateConnectionStatus();
        window.Show();
    }
    
    void UpdateConnectionStatus()
    {
        // Check current connection status
        var devices = ADBTools.RunAdbCommand("devices");
        
        if (!string.IsNullOrEmpty(devices))
        {
            if (devices.Contains(":5555") && devices.Contains("device"))
            {
                // Extract IP from devices list
                var lines = devices.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains(":5555") && line.Contains("device"))
                    {
                        var parts = line.Split(new[] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            string connectedIP = parts[0].Replace(":5555", "");
                            connectionStatus = $"[OK] Connected: {connectedIP}";
                            return;
                        }
                    }
                }
                connectionStatus = "[OK] Wireless connected";
            }
            else if (devices.Contains("device") && !devices.Contains("unauthorized"))
            {
                connectionStatus = "[USB] USB connected (wireless not active)";
            }
            else
            {
                connectionStatus = "[WARN] Not connected";
            }
        }
        else
        {
            connectionStatus = "[FAIL] ADB not available";
        }
    }
    
    void OnGUI()
    {
        GUILayout.Space(10);
        
        // Show connection status
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel, GUILayout.Width(60));
        
        GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
        if (connectionStatus.Contains("[OK]"))
            statusStyle.normal.textColor = Color.green;
        else if (connectionStatus.Contains("[USB]"))
            statusStyle.normal.textColor = Color.cyan;
        else
            statusStyle.normal.textColor = Color.red;
            
        EditorGUILayout.LabelField(connectionStatus, statusStyle);
        
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            UpdateConnectionStatus();
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "Find Quest IP address:\n" +
            "Settings ? WiFi ? Advanced ? IP Address\n\n" +
            "Example: 192.168.1.100", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("IP Address:", GUILayout.Width(80));
        ipAddress = EditorGUILayout.TextField(ipAddress);
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Connect", GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(ipAddress))
            {
                ADBTools.ConnectToQuestIP(ipAddress);
                UpdateConnectionStatus();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid IP", "Please enter a valid IP address.", "OK");
            }
        }
        
        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            Close();
        }
        
        EditorGUILayout.EndHorizontal();
    }
}
