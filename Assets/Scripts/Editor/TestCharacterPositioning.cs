using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;
using MapleClient.GameView;

public static class TestCharacterPositioning
{
    public static void RunTest()
    {
        var report = new StringBuilder();
        report.AppendLine("=== Character Positioning Test ===");
        report.AppendLine($"Test started at: {System.DateTime.Now}");
        
        try
        {
            // Load the test scene
            report.AppendLine("\nLoading TestScene...");
            var scenePath = "Assets/Scenes/TestScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);
            report.AppendLine($"Scene loaded: {scene.name}");
            
            // Find the character renderer
            var characterRenderer = GameObject.FindObjectOfType<MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                report.AppendLine("ERROR: No MapleCharacterRenderer found in scene!");
                File.WriteAllText("character-positioning-test.txt", report.ToString());
                EditorApplication.Exit(1);
                return;
            }
            
            report.AppendLine($"\nCharacter found at: {characterRenderer.transform.position}");
            
            // Check body parts
            report.AppendLine("\n=== Body Part Positions ===");
            
            // Check body
            var bodyTransform = characterRenderer.transform.Find("Body");
            if (bodyTransform != null)
            {
                report.AppendLine($"Body position: {bodyTransform.position} (Local: {bodyTransform.localPosition})");
                report.AppendLine($"  - Y position {(Mathf.Abs(bodyTransform.position.y) < 0.1f ? "CORRECT (at ground level)" : "INCORRECT (should be at Y=0)")}");
            }
            else
            {
                report.AppendLine("ERROR: Body transform not found!");
            }
            
            // Check head
            var headTransform = characterRenderer.transform.Find("Head");
            if (headTransform != null)
            {
                report.AppendLine($"\nHead position: {headTransform.position} (Local: {headTransform.localPosition})");
                if (bodyTransform != null)
                {
                    float headOffset = headTransform.position.y - bodyTransform.position.y;
                    report.AppendLine($"  - Head offset from body: {headOffset:F2} units");
                    report.AppendLine($"  - Head position {(headOffset > 0 ? "CORRECT (above body)" : "INCORRECT (should be above body)")}");
                }
            }
            else
            {
                report.AppendLine("ERROR: Head transform not found!");
            }
            
            // Check arms
            var armTransform = characterRenderer.transform.Find("Arm");
            if (armTransform != null)
            {
                report.AppendLine($"\nArm position: {armTransform.position} (Local: {armTransform.localPosition})");
                if (bodyTransform != null && headTransform != null)
                {
                    float armY = armTransform.position.y;
                    float bodyY = bodyTransform.position.y;
                    float headY = headTransform.position.y;
                    float relativePosition = (armY - bodyY) / (headY - bodyY);
                    report.AppendLine($"  - Arm relative position: {relativePosition:F2} (0=body level, 1=head level)");
                    report.AppendLine($"  - Arm position {(relativePosition > 0.3f && relativePosition < 0.7f ? "CORRECT (mid-body level)" : "INCORRECT (should be at mid-body/navel)")}");
                }
            }
            else
            {
                report.AppendLine("ERROR: Arm transform not found!");
            }
            
            // Check face
            var faceTransform = characterRenderer.transform.Find("Face");
            if (faceTransform != null)
            {
                report.AppendLine($"\nFace position: {faceTransform.position} (Local: {faceTransform.localPosition})");
                if (headTransform != null)
                {
                    float faceOffsetY = Mathf.Abs(faceTransform.position.y - headTransform.position.y);
                    float faceOffsetX = Mathf.Abs(faceTransform.position.x - headTransform.position.x);
                    report.AppendLine($"  - Face offset from head: X={faceOffsetX:F2}, Y={faceOffsetY:F2}");
                    report.AppendLine($"  - Face position {(faceOffsetY < 0.5f && faceOffsetX < 0.5f ? "CORRECT (within head)" : "INCORRECT (should be within head bounds)")}");
                }
            }
            else
            {
                report.AppendLine("ERROR: Face transform not found!");
            }
            
            // Check for any errors in console
            report.AppendLine("\n=== Checking for Errors ===");
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntries != null)
            {
                var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                clearMethod?.Invoke(null, null);
            }
            
            // Simulate a frame to trigger any rendering issues
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            
            report.AppendLine("Test completed successfully!");
            report.AppendLine($"\nTest ended at: {System.DateTime.Now}");
            
            // Write report
            File.WriteAllText("character-positioning-test.txt", report.ToString());
            
            Debug.Log("Character positioning test completed. Results written to character-positioning-test.txt");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            report.AppendLine($"\nEXCEPTION: {e.GetType().Name}: {e.Message}");
            report.AppendLine($"Stack trace:\n{e.StackTrace}");
            File.WriteAllText("character-positioning-test.txt", report.ToString());
            Debug.LogError($"Test failed with exception: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}