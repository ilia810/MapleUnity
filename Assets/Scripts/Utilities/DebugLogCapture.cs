using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MapleClient.Utilities
{
    /// <summary>
    /// Captures all Unity console output and saves it to a debug log file
    /// </summary>
    public class DebugLogCapture : MonoBehaviour
    {
        private static DebugLogCapture instance;
        private List<LogEntry> logEntries = new List<LogEntry>();
        private readonly string logFilePath = @"C:\Users\me\MapleUnity\debug-log.txt";
        private readonly object logLock = new object();
        
        private struct LogEntry
        {
            public string timestamp;
            public string message;
            public string stackTrace;
            public LogType type;
        }
        
        void Awake()
        {
            // Ensure only one instance exists
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to Unity's log message received event
            Application.logMessageReceived += HandleLog;
            
            // Clear the log file at startup
            try
            {
                File.WriteAllText(logFilePath, $"=== MapleUnity Debug Log - Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize debug log file: {e.Message}");
            }
        }
        
        void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Capture all log types (Debug, Warning, Error, Assert, Exception)
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
        
        void OnDestroy()
        {
            // Unsubscribe from log event
            Application.logMessageReceived -= HandleLog;
            
            // Save logs when the object is destroyed
            SaveLogsToFile();
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            // Save logs when application is paused (mobile)
            if (pauseStatus)
            {
                SaveLogsToFile();
            }
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            // Save logs when application loses focus
            if (!hasFocus)
            {
                SaveLogsToFile();
            }
        }
        
        void OnApplicationQuit()
        {
            // Save logs when application quits
            SaveLogsToFile();
        }
        
        private void SaveLogsToFile()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"=== MapleUnity Debug Log - Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                sb.AppendLine($"=== Total Entries: {logEntries.Count} ===");
                sb.AppendLine();
                
                lock (logLock)
                {
                    foreach (var entry in logEntries)
                    {
                        // Format the log entry
                        string prefix = entry.type switch
                        {
                            LogType.Error => "[ERROR]",
                            LogType.Assert => "[ASSERT]",
                            LogType.Warning => "[WARNING]",
                            LogType.Log => "[LOG]",
                            LogType.Exception => "[EXCEPTION]",
                            _ => "[UNKNOWN]"
                        };
                        
                        sb.AppendLine($"{entry.timestamp} {prefix} {entry.message}");
                        
                        // Include stack trace for errors and exceptions
                        if (!string.IsNullOrEmpty(entry.stackTrace) && 
                            (entry.type == LogType.Error || entry.type == LogType.Exception || entry.type == LogType.Assert))
                        {
                            sb.AppendLine("Stack Trace:");
                            sb.AppendLine(entry.stackTrace);
                        }
                        
                        sb.AppendLine(); // Empty line between entries
                    }
                }
                
                sb.AppendLine($"=== Log Saved at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                
                // Write to file
                File.WriteAllText(logFilePath, sb.ToString());
                
                // Also log to Unity console that we saved the debug log
                Debug.Log($"Debug log saved to: {logFilePath} ({logEntries.Count} entries)");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save debug log: {e.Message}");
            }
        }
        
        // Public method to manually save logs
        public static void SaveLogs()
        {
            if (instance != null)
            {
                instance.SaveLogsToFile();
            }
        }
        
        // Public method to clear logs
        public static void ClearLogs()
        {
            if (instance != null)
            {
                lock (instance.logLock)
                {
                    instance.logEntries.Clear();
                }
            }
        }
    }
}