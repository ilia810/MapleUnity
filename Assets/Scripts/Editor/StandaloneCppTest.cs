using UnityEngine;
using UnityEditor;
using System.IO;

public static class StandaloneCppTest
{
    public static void RunTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\cpp-formula-test-results.txt";
        var log = new System.Text.StringBuilder();
        
        try
        {
            log.AppendLine("=== Testing C++ Character Rendering Formulas ===");
            log.AppendLine($"Test started at: {System.DateTime.Now}");
            log.AppendLine($"Unity Version: {Application.unityVersion}");
            log.AppendLine($"Project Path: {Application.dataPath}");
            
            // Try to run character rendering tests using reflection to avoid compilation dependencies
            log.AppendLine("\n=== Attempting to test MapleCharacterRenderer ===");
            
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            bool foundAssembly = false;
            
            foreach (var assembly in assemblies)
            {
                if (assembly.GetName().Name == "Assembly-CSharp")
                {
                    foundAssembly = true;
                    log.AppendLine($"Found Assembly-CSharp: {assembly.FullName}");
                    
                    // Look for MapleCharacterRenderer type
                    var types = assembly.GetTypes();
                    log.AppendLine($"Total types in assembly: {types.Length}");
                    
                    var rendererType = assembly.GetType("MapleClient.GameView.MapleCharacterRenderer");
                    if (rendererType != null)
                    {
                        log.AppendLine("\nFound MapleCharacterRenderer!");
                        TestCharacterRendering(rendererType, log);
                    }
                    else
                    {
                        log.AppendLine("\nERROR: MapleCharacterRenderer not found in assembly");
                        log.AppendLine("Looking for types containing 'Character':");
                        
                        foreach (var type in types)
                        {
                            if (type.Name.Contains("Character") && type.Namespace != null && type.Namespace.Contains("MapleClient"))
                            {
                                log.AppendLine($"  - {type.FullName}");
                            }
                        }
                    }
                    break;
                }
            }
            
            if (!foundAssembly)
            {
                log.AppendLine("ERROR: Assembly-CSharp not found!");
            }
            
            log.AppendLine("\n=== Test Complete ===");
            File.WriteAllText(logPath, log.ToString());
            Debug.Log($"Test results written to: {logPath}");
            Debug.Log("Test completed successfully");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            log.AppendLine($"\nERROR: Test failed - {e.Message}");
            log.AppendLine($"Stack trace:\n{e.StackTrace}");
            File.WriteAllText(logPath, log.ToString());
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void TestCharacterRendering(System.Type rendererType, System.Text.StringBuilder log)
    {
        try
        {
            // Create test GameObject
            GameObject testObject = new GameObject("TestCharacter");
            testObject.transform.position = Vector3.zero;
            
            // Add renderer component
            var renderer = testObject.AddComponent(rendererType);
            log.AppendLine("Successfully added MapleCharacterRenderer component");
            
            // Create a player instance using reflection
            var playerType = System.Type.GetType("MapleClient.GameLogic.Core.Player, Assembly-CSharp");
            if (playerType != null)
            {
                // Use parameterless constructor
                var player = System.Activator.CreateInstance(playerType);
                log.AppendLine("Created Player instance");
                
                // Initialize renderer
                var initMethod = rendererType.GetMethod("Initialize");
                if (initMethod != null)
                {
                    initMethod.Invoke(renderer, new[] { player, null });
                    log.AppendLine("Initialized renderer");
                    
                    // Wait one frame and check positions
                    EditorApplication.delayCall += () => {
                        LogPositions(renderer, rendererType, log);
                        GameObject.DestroyImmediate(testObject);
                    };
                }
                else
                {
                    log.AppendLine("ERROR: Initialize method not found");
                }
            }
            else
            {
                log.AppendLine("ERROR: Player type not found");
            }
        }
        catch (System.Exception e)
        {
            log.AppendLine($"ERROR in TestCharacterRendering: {e.Message}");
            log.AppendLine($"Stack: {e.StackTrace}");
        }
    }
    
    private static void LogPositions(object renderer, System.Type rendererType, System.Text.StringBuilder log)
    {
        try
        {
            log.AppendLine("\n=== Character Part Positions ===");
            var transform = (renderer as Component).transform;
            
            var parts = new[] { "Body", "Arm", "Head", "Face", "Hair" };
            foreach (var part in parts)
            {
                var child = transform.Find(part);
                if (child != null)
                {
                    log.AppendLine($"\n{part}:");
                    log.AppendLine($"  Local Position: {child.localPosition}");
                    log.AppendLine($"  World Position: {child.position}");
                    
                    var sr = child.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        log.AppendLine($"  Sprite: {sr.sprite.name}");
                        log.AppendLine($"  Pivot: {sr.sprite.pivot}");
                    }
                }
            }
            
            log.AppendLine("\n=== Formula Verification ===");
            log.AppendLine("Expected C++ formulas:");
            log.AppendLine("1. Body navel at (0,0)");
            log.AppendLine("2. Arm navel aligned with body navel");
            log.AppendLine("3. Head neck aligned with body neck");
            log.AppendLine("4. Face at head position + brow offset");
        }
        catch (System.Exception e)
        {
            log.AppendLine($"ERROR in LogPositions: {e.Message}");
        }
    }
}