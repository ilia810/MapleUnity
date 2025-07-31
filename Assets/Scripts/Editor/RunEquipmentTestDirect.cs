using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class RunEquipmentTestDirect  
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "equipment-test-direct.log");
        
        try
        {
            File.WriteAllText(logPath, $"[DIRECT_TEST] Starting test at {DateTime.Now}\n");
            
            // Call the original test method directly
            TestCharacterWithEquipment.RunTest();
            
            File.AppendAllText(logPath, "[DIRECT_TEST] Test called successfully\n");
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"[DIRECT_TEST] ERROR: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
}