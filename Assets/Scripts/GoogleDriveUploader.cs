using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public class GoogleDriveUploader : MonoBehaviour
{
    [Tooltip("OAuth2 access token (Bearer). You can paste a token obtained from OAuth playground or your flow.")]
    public string accessToken;

    [Tooltip("Optional Drive folder ID to upload into. If empty, files will be uploaded to root.")]
    public string folderId;

    [Tooltip("If true, uploader will automatically upload after export when token is present.")]
    public bool autoUpload = false;

    public void StartUploadDirectory(string directory)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogWarning("GoogleDriveUploader: no access token provided");
            return;
        }
        if (!Directory.Exists(directory))
        {
            Debug.LogWarning("GoogleDriveUploader: directory does not exist: " + directory);
            return;
        }
        StartCoroutine(UploadDirectoryCoroutine(directory));
    }

    public IEnumerator UploadDirectoryCoroutine(string directory)
    {
        var files = Directory.GetFiles(directory);
        Debug.Log($"GoogleDriveUploader: uploading {files.Length} files from {directory}");
        foreach (var f in files)
        {
            yield return UploadFileCoroutine(f);
        }
        Debug.Log("GoogleDriveUploader: upload finished");
    }

    IEnumerator UploadFileCoroutine(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        byte[] fileBytes = File.ReadAllBytes(filePath);

        string boundary = "-------UnityDriveBoundary" + DateTime.Now.Ticks.ToString("x");
        var meta = new Dictionary<string, object>();
        meta["name"] = fileName;
        if (!string.IsNullOrEmpty(folderId)) meta["parents"] = new string[] { folderId };
        string metaJson = MiniJSON.Serialize(meta);

        var header = "--" + boundary + "\r\n" +
                     "Content-Type: application/json; charset=UTF-8\r\n\r\n" +
                     metaJson + "\r\n" +
                     "--" + boundary + "\r\n" +
                     "Content-Type: application/octet-stream\r\n\r\n";
        var footer = "\r\n--" + boundary + "--\r\n";

        byte[] headerBytes = Encoding.UTF8.GetBytes(header);
        byte[] footerBytes = Encoding.UTF8.GetBytes(footer);

        byte[] body = new byte[headerBytes.Length + fileBytes.Length + footerBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, body, 0, headerBytes.Length);
        Buffer.BlockCopy(fileBytes, 0, body, headerBytes.Length, fileBytes.Length);
        Buffer.BlockCopy(footerBytes, 0, body, headerBytes.Length + fileBytes.Length, footerBytes.Length);

        string url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
        var uw = new UnityWebRequest(url, "POST");
        uw.uploadHandler = new UploadHandlerRaw(body);
        uw.downloadHandler = new DownloadHandlerBuffer();
        uw.SetRequestHeader("Authorization", "Bearer " + accessToken);
        uw.SetRequestHeader("Content-Type", "multipart/related; boundary=" + boundary);

        yield return uw.SendWebRequest();

        if (uw.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GoogleDriveUploader: failed to upload {fileName}: {uw.error} | {uw.downloadHandler.text}");
        }
        else
        {
            Debug.Log($"GoogleDriveUploader: uploaded {fileName}: {uw.downloadHandler.text}");
        }
    }
}
