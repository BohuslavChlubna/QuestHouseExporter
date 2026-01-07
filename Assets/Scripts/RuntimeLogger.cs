using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class RuntimeLogger
{
    static string logPath;
    static object sync = new object();

    public static void Init(string exportFolder)
    {
        try
        {
            var basePath = Path.Combine(Application.persistentDataPath, exportFolder);
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            logPath = Path.Combine(basePath, "export_log.txt");
            // start new log with header
            WriteLine($"=== Export Log started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        }
        catch (Exception ex)
        {
            Debug.LogError("RuntimeLogger.Init failed: " + ex.Message);
        }
    }

    public static void WriteLine(string line)
    {
        try
        {
            if (string.IsNullOrEmpty(logPath))
            {
                // fallback to a basic path
                var basePath = Path.Combine(Application.persistentDataPath, "QuestHouseDesign");
                if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
                logPath = Path.Combine(basePath, "export_log.txt");
            }
            lock (sync)
            {
                using (var sw = new StreamWriter(logPath, true, Encoding.UTF8))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("RuntimeLogger.WriteLine failed: " + ex.Message);
        }
    }

    public static void LogException(Exception ex)
    {
        WriteLine("EXCEPTION: " + ex.ToString());
    }
    
    public static void ClearLog()
    {
        try
        {
            if (string.IsNullOrEmpty(logPath))
            {
                var basePath = Path.Combine(Application.persistentDataPath, "QuestHouseDesign");
                if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
                logPath = Path.Combine(basePath, "export_log.txt");
            }
            lock (sync)
            {
                // Overwrite file (not append)
                using (var sw = new StreamWriter(logPath, false, Encoding.UTF8))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === LOG CLEARED ===");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("RuntimeLogger.ClearLog failed: " + ex.Message);
        }
    }
