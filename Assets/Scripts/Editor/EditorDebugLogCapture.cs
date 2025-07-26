using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace MapleClient.Editor
{
    /// <summary>
    /// Captures all Unity console output and saves it to a debug log file when exiting play mode
    /// </summary>
    [InitializeOnLoad]
    public static class EditorDebugLogCapture
    {
        private static List<LogEntry> logEntries = new List<LogEntry>();
        private static readonly string logFilePath = @"C:\Users\me\MapleUnity\debug-log.txt";
        private static readonly object logLock = new object();
        private static bool isCapturing = false;
        
        private struct LogEntry
        {
            public string timestamp;
            public string message;
            public string stackTrace;
            public LogType type;
        }
        
        static EditorDebugLogCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    StartCapturing();
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    SaveLogs();
                    break;
            }
        }
        
        private static void StartCapturing()
        {
            if (!isCapturing)
            {
                Application.logMessageReceived += HandleLog;
                isCapturing = true;
                logEntries.Clear();
                Debug.Log("[EditorDebugLogCapture] Started capturing logs");
            }
        }
        
        private static void StopCapturing()
        {
            if (isCapturing)
            {
                Application.logMessageReceived -= HandleLog;
                isCapturing = false;
                Debug.Log("[EditorDebugLogCapture] Stopped capturing logs");
            }
        }
        
        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            lock (logLock)
            {
                logEntries.Add(new LogEntry
                {
                    timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                    message = logString,
                    stackTrace = stackTrace,
                    type = type
                });
            }
        }
        
        private static void SaveLogs()
        {
            try
            {
                StopCapturing();
                
                lock (logLock)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"=== Unity Debug Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                    sb.AppendLine($"Total entries: {logEntries.Count}");
                    sb.AppendLine();
                    
                    foreach (var entry in logEntries)
                    {
                        sb.AppendLine($"[{entry.timestamp}] [{entry.type}] {entry.message}");
                        if (!string.IsNullOrEmpty(entry.stackTrace) && entry.type == LogType.Error || entry.type == LogType.Exception)
                        {
                            sb.AppendLine("Stack Trace:");
                            sb.AppendLine(entry.stackTrace);
                        }
                        sb.AppendLine();
                    }
                    
                    File.WriteAllText(logFilePath, sb.ToString());
                    Debug.Log($"[EditorDebugLogCapture] Saved {logEntries.Count} log entries to {logFilePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EditorDebugLogCapture] Failed to save logs: {e.Message}");
            }
        }
    }
}