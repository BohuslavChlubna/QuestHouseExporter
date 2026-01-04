using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public class GoogleDeviceAuth : MonoBehaviour
{
    [Tooltip("OAuth client ID for OAuth 2.0 Client (TVs and Limited-input devices)")]
    public string clientId;
    [Tooltip("Space separated OAuth scopes e.g. 'https://www.googleapis.com/auth/drive.file'")]
    public string scope = "https://www.googleapis.com/auth/drive.file";

    [Tooltip("Link to GoogleDriveUploader component to set access token upon success")]
    public GoogleDriveUploader driveUploader;

    [Tooltip("If true, start device auth automatically when this component starts")]
    public bool autoStart = false;

    [NonSerialized]
    public string userCode;
    [NonSerialized]
    public string verificationUrl;
    [NonSerialized]
    public string deviceCode;
    [NonSerialized]
    public int interval = 5;
    [NonSerialized]
    public string statusMessage = "";

    Coroutine pollCoroutine;

    public void StartDeviceAuth()
    {
        if (string.IsNullOrEmpty(clientId))
        {
            statusMessage = "Missing clientId - create OAuth client and set clientId.";
            Debug.LogWarning(statusMessage);
            return;
        }
        StartCoroutine(RequestDeviceCode());
    }

    void Start()
    {
        if (autoStart)
        {
            StartDeviceAuth();
        }
    }

    IEnumerator RequestDeviceCode()
    {
        statusMessage = "Requesting device code...";
        var form = new WWWForm();
        form.AddField("client_id", clientId);
        form.AddField("scope", scope);

        using (var www = UnityWebRequest.Post("https://oauth2.googleapis.com/device/code", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                statusMessage = "Device code request failed: " + www.error;
                Debug.LogError(statusMessage + " | " + www.downloadHandler.text);
                yield break;
            }
            var json = www.downloadHandler.text;
            var dict = MiniJSON.Deserialize(json) as Dictionary<string, object>;
            if (dict == null)
            {
                statusMessage = "Invalid response for device code";
                yield break;
            }
            deviceCode = dict.ContainsKey("device_code") ? dict["device_code"].ToString() : null;
            userCode = dict.ContainsKey("user_code") ? dict["user_code"].ToString() : null;
            verificationUrl = dict.ContainsKey("verification_url") ? dict["verification_url"].ToString() : dict.ContainsKey("verification_uri") ? dict["verification_uri"].ToString() : null;
            if (dict.ContainsKey("interval")) interval = Convert.ToInt32(dict["interval"]);
            statusMessage = $"Open {verificationUrl} and enter code: {userCode}";
            Debug.Log(statusMessage);

            if (pollCoroutine != null) StopCoroutine(pollCoroutine);
            pollCoroutine = StartCoroutine(PollForToken());
        }
    }

    IEnumerator PollForToken()
    {
        statusMessage = "Waiting for user authorization...";
        while (true)
        {
            yield return new WaitForSeconds(interval);

            var form = new WWWForm();
            form.AddField("client_id", clientId);
            form.AddField("device_code", deviceCode);
            form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:device_code");

            using (var www = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form))
            {
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    // network error or such - continue polling
                    Debug.LogWarning("Poll error: " + www.error + " | " + www.downloadHandler.text);
                    continue;
                }
                var text = www.downloadHandler.text;
                var dict = MiniJSON.Deserialize(text) as Dictionary<string, object>;
                if (dict == null)
                {
                    Debug.LogWarning("Invalid token response: " + text);
                    continue;
                }
                if (dict.ContainsKey("error"))
                {
                    var err = dict["error"].ToString();
                    if (err == "authorization_pending")
                    {
                        // keep waiting
                        continue;
                    }
                    else if (err == "slow_down")
                    {
                        interval += 5;
                        continue;
                    }
                    else
                    {
                        statusMessage = "Authorization failed: " + err;
                        Debug.LogError(statusMessage + " | " + text);
                        yield break;
                    }
                }
                // success
                if (dict.ContainsKey("access_token"))
                {
                    string at = dict["access_token"].ToString();
                    string rt = dict.ContainsKey("refresh_token") ? dict["refresh_token"].ToString() : null;
                    int expiresIn = dict.ContainsKey("expires_in") ? Convert.ToInt32(dict["expires_in"]) : 3600;

                    statusMessage = "Authorized";
                    Debug.Log("Device authorized, access token obtained.");

                    // set token to driveUploader if present
                    if (driveUploader != null)
                    {
                        driveUploader.accessToken = at;
                    }
                    PlayerPrefs.SetString("GD_access_token", at);
                    if (!string.IsNullOrEmpty(rt)) PlayerPrefs.SetString("GD_refresh_token", rt);
                    PlayerPrefs.SetInt("GD_expires_at", (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds) + expiresIn);
                    PlayerPrefs.Save();

                    yield break;
                }
            }
        }
    }

    void OnGUI()
    {
        if (!string.IsNullOrEmpty(userCode))
        {
            GUI.Box(new Rect(10, 70, 420, 80), "Sign in to Google Drive");
            GUI.Label(new Rect(20, 90, 400, 20), "Visit: " + verificationUrl);
            GUI.Label(new Rect(20, 110, 400, 20), "Code: " + userCode);
            if (GUI.Button(new Rect(20, 135, 200, 20), "Open verification URL"))
            {
                Application.OpenURL(verificationUrl);
            }
        }
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUI.Label(new Rect(20, 160, 600, 20), statusMessage);
        }
    }
}
