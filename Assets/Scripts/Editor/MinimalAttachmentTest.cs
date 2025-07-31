using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public static class MinimalAttachmentTest
{
    public static void RunTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\attachment-test-results.log";
        
        try
        {
            File.WriteAllText(logPath, "=== MINIMAL ATTACHMENT TEST ===\n");
            File.AppendAllText(logPath, $"Started at: {System.DateTime.Now}\n");
            File.AppendAllText(logPath, $"Unity Version: {Application.unityVersion}\n\n");
            
            // Try to use NXAssetLoader via reflection to avoid compilation issues
            var loaderType = System.Type.GetType("MapleClient.GameData.NXAssetLoader, Assembly-CSharp");
            if (loaderType == null)
            {
                File.AppendAllText(logPath, "ERROR: Could not find NXAssetLoader type\n");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get Instance property
            var instanceProp = loaderType.GetProperty("Instance");
            if (instanceProp == null)
            {
                File.AppendAllText(logPath, "ERROR: Could not find Instance property\n");
                EditorApplication.Exit(1);
                return;
            }
            
            var loader = instanceProp.GetValue(null);
            if (loader == null)
            {
                File.AppendAllText(logPath, "ERROR: NXAssetLoader.Instance is null\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(logPath, "Successfully got NXAssetLoader instance\n");
            
            // Get LoadCharacterBodyParts method
            var loadMethod = loaderType.GetMethod("LoadCharacterBodyParts");
            if (loadMethod == null)
            {
                File.AppendAllText(logPath, "ERROR: Could not find LoadCharacterBodyParts method\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(logPath, "Found LoadCharacterBodyParts method\n");
            
            // Prepare parameters
            object[] parameters = new object[] { 0, "stand1", 0, null };
            
            // Call method
            var result = loadMethod.Invoke(loader, parameters);
            var attachmentPoints = parameters[3] as Dictionary<string, Vector2>;
            
            if (result == null)
            {
                File.AppendAllText(logPath, "ERROR: LoadCharacterBodyParts returned null\n");
                EditorApplication.Exit(1);
                return;
            }
            
            var bodyParts = result as Dictionary<string, Sprite>;
            File.AppendAllText(logPath, $"\nLoaded {bodyParts?.Count ?? 0} body parts\n");
            
            if (bodyParts != null)
            {
                foreach (var part in bodyParts.Keys)
                {
                    File.AppendAllText(logPath, $"  - {part}\n");
                }
            }
            
            File.AppendAllText(logPath, $"\nFound {attachmentPoints?.Count ?? 0} attachment points\n");
            if (attachmentPoints != null)
            {
                foreach (var attachment in attachmentPoints)
                {
                    File.AppendAllText(logPath, $"  {attachment.Key}: {attachment.Value} (Unity: {attachment.Value / 100f})\n");
                }
            }
            
            // Test character creation
            File.AppendAllText(logPath, "\n=== Testing Character Creation ===\n");
            
            var testChar = new GameObject("TestCharacter");
            testChar.transform.position = Vector3.zero;
            
            // Get MapleCharacterRenderer type
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            if (rendererType == null)
            {
                File.AppendAllText(logPath, "ERROR: Could not find MapleCharacterRenderer type\n");
                GameObject.DestroyImmediate(testChar);
                EditorApplication.Exit(1);
                return;
            }
            
            var renderer = testChar.AddComponent(rendererType);
            File.AppendAllText(logPath, "Added MapleCharacterRenderer component\n");
            
            // Wait a moment for initialization
            EditorApplication.delayCall += () => {
                try
                {
                    File.AppendAllText(logPath, "\n=== Analyzing Sprite Positions ===\n");
                    
                    // Find body parts
                    var body = testChar.transform.Find("body");
                    var arm = testChar.transform.Find("arm");
                    var head = testChar.transform.Find("head");
                    var face = testChar.transform.Find("face") ?? testChar.transform.Find("head/face");
                    
                    if (body != null)
                        File.AppendAllText(logPath, $"Body position: {body.localPosition}\n");
                    else
                        File.AppendAllText(logPath, "Body not found!\n");
                        
                    if (arm != null)
                    {
                        File.AppendAllText(logPath, $"Arm position: {arm.localPosition}\n");
                        if (arm.localPosition.y < 0.1f)
                        {
                            File.AppendAllText(logPath, "ERROR: Arm is too low! Expected Y ~= 0.20\n");
                        }
                    }
                    else
                        File.AppendAllText(logPath, "Arm not found!\n");
                        
                    if (head != null)
                    {
                        File.AppendAllText(logPath, $"Head position: {head.localPosition}\n");
                        if (head.localPosition.y < 0.2f)
                        {
                            File.AppendAllText(logPath, "ERROR: Head is too low! Expected Y >= 0.28\n");
                        }
                    }
                    else
                        File.AppendAllText(logPath, "Head not found!\n");
                        
                    if (face != null)
                        File.AppendAllText(logPath, $"Face position: {face.position} (world)\n");
                    else
                        File.AppendAllText(logPath, "Face not found!\n");
                    
                    File.AppendAllText(logPath, "\n=== Expected vs Actual Positions ===\n");
                    File.AppendAllText(logPath, "Expected: Body at Y=0.00, Arm at Y=0.20, Head at Y=0.28+\n");
                    File.AppendAllText(logPath, $"Actual: Body={body?.localPosition.y ?? -1:F2}, Arm={arm?.localPosition.y ?? -1:F2}, Head={head?.localPosition.y ?? -1:F2}\n");
                    
                    GameObject.DestroyImmediate(testChar);
                    File.AppendAllText(logPath, "\n=== TEST COMPLETE ===\n");
                    EditorApplication.Exit(0);
                }
                catch (System.Exception e)
                {
                    File.AppendAllText(logPath, $"\nERROR in analysis: {e.Message}\n{e.StackTrace}\n");
                    EditorApplication.Exit(1);
                }
            };
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nERROR: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
}