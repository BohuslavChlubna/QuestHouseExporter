using System;
using System.Net;
using System.Threading;
using System.IO;
using UnityEngine;

public class SimpleHttpServer
{
    private readonly string basePath;
    private readonly int port;
    private HttpListener listener;
    private Thread listenerThread;
    private bool running;

    public SimpleHttpServer(string basePath, int port)
    {
        this.basePath = basePath;
        this.port = port;
    }

    public void Start()
    {
        if (running) return;
        listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");
        running = true;
        listener.Start();
        listenerThread = new Thread(ListenLoop);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    public void Stop()
    {
        running = false;
        try { listener?.Stop(); } catch { }
        try { listenerThread?.Join(500); } catch { }
        listener = null;
    }

    void ListenLoop()
    {
        while (running)
        {
            try
            {
                var ctx = listener.GetContext();
                ThreadPool.QueueUserWorkItem((o) => HandleRequest(ctx));
            }
            catch (Exception ex)
            {
                Debug.Log("HTTP server error: " + ex.Message);
            }
        }
    }

    void HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            string urlPath = ctx.Request.Url.LocalPath.TrimStart('/');
            if (string.IsNullOrEmpty(urlPath)) urlPath = "index.html";
            string filePath = Path.Combine(basePath, urlPath);
            if (!File.Exists(filePath))
            {
                // try directory listing
                if (Directory.Exists(Path.Combine(basePath, urlPath)))
                {
                    var entries = Directory.GetFiles(Path.Combine(basePath, urlPath));
                    string html = "<html><body>";
                    foreach (var e in entries)
                        html += $"<a href=\"/{Path.GetFileName(e)}\">{Path.GetFileName(e)}</a><br/>";
                    html += "</body></html>";
                    var data = System.Text.Encoding.UTF8.GetBytes(html);
                    ctx.Response.ContentType = "text/html";
                    ctx.Response.OutputStream.Write(data, 0, data.Length);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                }
                ctx.Response.Close();
                return;
            }

            string mime = "application/octet-stream";
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".obj") mime = "text/plain";
            if (ext == ".glb") mime = "model/gltf-binary";
            if (ext == ".json") mime = "application/json";
            if (ext == ".csv") mime = "text/csv";

            byte[] bytes = File.ReadAllBytes(filePath);
            ctx.Response.ContentType = mime;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Debug.Log("HTTP handler error: " + ex.Message);
        }
        finally
        {
            try { ctx.Response.Close(); } catch { }
        }
    }
}
