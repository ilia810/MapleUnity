using UnityEngine;
using System;
using System.Collections.Generic;

namespace MapleClient.GameView.Debugging
{
    /// <summary>
    /// Captures console output with [FOOTHOLD_COLLISION] prefix and logs it to Unity's Debug.Log
    /// This allows GameLogic layer to log without referencing UnityEngine
    /// </summary>
    public class FootholdDebugLogger : MonoBehaviour
    {
        private static FootholdDebugLogger instance;
        private readonly Queue<string> pendingLogs = new Queue<string>();
        private System.IO.StringWriter consoleCapture;
        private System.IO.TextWriter originalConsole;
        
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Capture console output
            originalConsole = Console.Out;
            consoleCapture = new System.IO.StringWriter();
            var multiWriter = new MultiTextWriter(originalConsole, consoleCapture);
            Console.SetOut(multiWriter);
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                // Restore original console
                Console.SetOut(originalConsole);
                consoleCapture?.Dispose();
                instance = null;
            }
        }
        
        private void Update()
        {
            // Check for new console output
            var output = consoleCapture.ToString();
            if (!string.IsNullOrEmpty(output))
            {
                // Clear the buffer
                consoleCapture.GetStringBuilder().Clear();
                
                // Process each line
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("[FOOTHOLD_COLLISION]"))
                    {
                        // Log to Unity console
                        if (line.Contains("WARNING"))
                        {
                            Debug.LogWarning(line);
                        }
                        else
                        {
                            Debug.Log(line);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Custom TextWriter that writes to multiple destinations
        /// </summary>
        private class MultiTextWriter : System.IO.TextWriter
        {
            private readonly System.IO.TextWriter[] writers;
            
            public MultiTextWriter(params System.IO.TextWriter[] writers)
            {
                this.writers = writers;
            }
            
            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
            
            public override void Write(char value)
            {
                foreach (var writer in writers)
                {
                    writer.Write(value);
                }
            }
            
            public override void WriteLine(string value)
            {
                foreach (var writer in writers)
                {
                    writer.WriteLine(value);
                }
            }
            
            public override void Flush()
            {
                foreach (var writer in writers)
                {
                    writer.Flush();
                }
            }
        }
    }
}