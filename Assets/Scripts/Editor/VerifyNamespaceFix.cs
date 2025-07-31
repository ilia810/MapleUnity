using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

public static class VerifyNamespaceFix
{
    public static void RunVerification()
    {
        string logPath = @"C:\Users\me\MapleUnity\namespace-verification.log";
        
        try
        {
            File.WriteAllText(logPath, "=== Namespace Fix Verification ===\n");
            File.AppendAllText(logPath, $"Timestamp: {DateTime.Now}\n");
            File.AppendAllText(logPath, $"Unity Version: {Application.unityVersion}\n\n");
            
            Debug.Log("=== Verifying RuntimeCharacterTest Namespace Fix ===");
            
            // Check all loaded assemblies for our types
            File.AppendAllText(logPath, "Searching all assemblies for types...\n");
            
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            File.AppendAllText(logPath, $"Total assemblies loaded: {assemblies.Length}\n");
            
            bool foundRuntimeTest = false;
            bool foundMapleCharRenderer = false;
            bool foundPlayerView = false;
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name == "RuntimeCharacterTest")
                        {
                            foundRuntimeTest = true;
                            File.AppendAllText(logPath, $"SUCCESS: Found RuntimeCharacterTest in assembly {assembly.GetName().Name}\n");
                            File.AppendAllText(logPath, $"  Full name: {type.FullName}\n");
                            File.AppendAllText(logPath, $"  Namespace: {type.Namespace ?? "none"}\n");
                            Debug.Log($"SUCCESS: RuntimeCharacterTest found in {assembly.GetName().Name}");
                        }
                        else if (type.FullName == "MapleClient.GameView.MapleCharacterRenderer")
                        {
                            foundMapleCharRenderer = true;
                            File.AppendAllText(logPath, $"SUCCESS: Found MapleClient.GameView.MapleCharacterRenderer in assembly {assembly.GetName().Name}\n");
                            Debug.Log($"SUCCESS: MapleClient.GameView.MapleCharacterRenderer found");
                        }
                        else if (type.FullName == "MapleClient.GameView.PlayerView")
                        {
                            foundPlayerView = true;
                            File.AppendAllText(logPath, $"SUCCESS: Found MapleClient.GameView.PlayerView in assembly {assembly.GetName().Name}\n");
                            Debug.Log($"SUCCESS: MapleClient.GameView.PlayerView found");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    // Some assemblies might not be loadable
                    File.AppendAllText(logPath, $"Could not load types from assembly {assembly.GetName().Name}: {ex.Message}\n");
                }
            }
            
            if (!foundRuntimeTest)
            {
                File.AppendAllText(logPath, "ERROR: RuntimeCharacterTest type not found in any assembly\n");
                Debug.LogError("ERROR: RuntimeCharacterTest type not found");
            }
            
            if (!foundMapleCharRenderer)
            {
                File.AppendAllText(logPath, "ERROR: MapleClient.GameView.MapleCharacterRenderer not found in any assembly\n");
                Debug.LogError("ERROR: MapleClient.GameView.MapleCharacterRenderer not found");
            }
            
            if (!foundPlayerView)
            {
                File.AppendAllText(logPath, "ERROR: MapleClient.GameView.PlayerView not found in any assembly\n");
                Debug.LogError("ERROR: MapleClient.GameView.PlayerView not found");
            }
            
            // Check for compilation errors
            File.AppendAllText(logPath, "\nChecking for compilation errors...\n");
            
            var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
            if (logEntries != null)
            {
                var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var getCountMethod = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                
                if (clearMethod != null && getCountMethod != null)
                {
                    clearMethod.Invoke(null, null);
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                    
                    // Give it a moment
                    System.Threading.Thread.Sleep(1000);
                    
                    int errorCount = (int)getCountMethod.Invoke(null, null);
                    File.AppendAllText(logPath, $"Compilation error count: {errorCount}\n");
                    
                    if (errorCount == 0)
                    {
                        File.AppendAllText(logPath, "SUCCESS: No compilation errors detected!\n");
                        Debug.Log("SUCCESS: No compilation errors!");
                    }
                    else
                    {
                        File.AppendAllText(logPath, $"WARNING: {errorCount} compilation errors detected\n");
                        Debug.LogWarning($"WARNING: {errorCount} compilation errors detected");
                    }
                }
            }
            
            // Summary
            File.AppendAllText(logPath, "\n=== VERIFICATION SUMMARY ===\n");
            if (foundRuntimeTest && foundMapleCharRenderer && foundPlayerView)
            {
                File.AppendAllText(logPath, "RESULT: Namespace fix appears to be working correctly!\n");
                File.AppendAllText(logPath, "All expected types are loading successfully.\n");
                Debug.Log("VERIFICATION PASSED: Namespace fix is working correctly!");
                EditorApplication.Exit(0);
            }
            else
            {
                File.AppendAllText(logPath, "RESULT: Some types are not loading correctly.\n");
                File.AppendAllText(logPath, "The namespace fix may not be complete.\n");
                Debug.LogError("VERIFICATION FAILED: Some types are not loading");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"\nEXCEPTION: {e.Message}\n");
            File.AppendAllText(logPath, $"Stack trace: {e.StackTrace}\n");
            Debug.LogError($"Verification failed with exception: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}