using UnityEngine;
using UnityEditor;
using System.IO;

public static class StandaloneEquipmentTest
{
    public static void RunStandaloneTest()
    {
        string logPath = Path.Combine(Application.dataPath, "../equipment_test_results.txt");
        System.Text.StringBuilder results = new System.Text.StringBuilder();
        
        try
        {
            results.AppendLine("=== Equipment Loading Test Results ===");
            results.AppendLine($"Time: {System.DateTime.Now}");
            results.AppendLine();
            
            // Run the actual test and capture output
            var logHandler = new TestLogHandler(results);
            Debug.unityLogger.logHandler = logHandler;
            
            // Execute the test
            TestEquipmentLoading.RunTest();
            
            // Write results to file
            File.WriteAllText(logPath, results.ToString());
            
            Debug.unityLogger.logHandler = Debug.unityLogger.logHandler; // Restore default
            Debug.Log($"Test results written to: {logPath}");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            results.AppendLine($"\nFATAL ERROR: {e.Message}");
            results.AppendLine($"Stack trace: {e.StackTrace}");
            File.WriteAllText(logPath, results.ToString());
            EditorApplication.Exit(1);
        }
    }
    
    private class TestLogHandler : ILogHandler
    {
        private System.Text.StringBuilder _output;
        private ILogHandler _defaultHandler;
        
        public TestLogHandler(System.Text.StringBuilder output)
        {
            _output = output;
            _defaultHandler = Debug.unityLogger.logHandler;
        }
        
        public void LogException(System.Exception exception, Object context)
        {
            _output.AppendLine($"[EXCEPTION] {exception.Message}");
            _output.AppendLine($"Stack trace: {exception.StackTrace}");
            _defaultHandler.LogException(exception, context);
        }
        
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            string message = string.Format(format, args);
            _output.AppendLine($"[{logType}] {message}");
            _defaultHandler.LogFormat(logType, context, format, args);
        }
    }
}