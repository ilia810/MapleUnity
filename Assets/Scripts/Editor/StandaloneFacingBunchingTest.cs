using UnityEngine;
using UnityEditor;
using System.IO;

public static class StandaloneFacingBunchingTest
{
    [MenuItem("MapleUnity/Debug/Standalone Facing Bunching Test")]
    public static void RunMenuItem()
    {
        Run();
    }
    
    public static void Run()
    {
        try
        {
            Debug.Log("=== STANDALONE FACING BUNCHING TEST ===");
            
            var output = new System.Text.StringBuilder();
            output.AppendLine("=== FACING AND BUNCHING ISSUE ANALYSIS ===");
            output.AppendLine($"Time: {System.DateTime.Now}");
            output.AppendLine("\nKnown Issues from C++ Client Analysis:");
            output.AppendLine("1. Sprites face LEFT by default in the NX data");
            output.AppendLine("2. flip=true means facing LEFT (confusing but confirmed)");
            output.AppendLine("3. flip=false means facing RIGHT");
            output.AppendLine("4. Unity needs to scale.x = -1 when flip=true to flip the left-facing sprite");
            
            // Create test scene
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, 
                UnityEditor.SceneManagement.NewSceneMode.Single);
            
            // Create character
            var characterObj = new GameObject("TestCharacter");
            
            // Add some test child objects to simulate body parts
            var body = new GameObject("Body");
            body.transform.parent = characterObj.transform;
            body.transform.localPosition = Vector3.zero;
            
            var head = new GameObject("Head");
            head.transform.parent = characterObj.transform;
            head.transform.localPosition = new Vector3(-0.21f, 0.5f, 0); // Expected offset
            
            var face = new GameObject("Face");
            face.transform.parent = characterObj.transform;
            face.transform.localPosition = new Vector3(-0.21f, 0.5f, 0); // Should be same as head
            
            // Analyze positions
            output.AppendLine("\n=== INITIAL POSITIONS ===");
            output.AppendLine($"Body: {body.transform.localPosition}");
            output.AppendLine($"Head: {head.transform.localPosition}");
            output.AppendLine($"Face: {face.transform.localPosition}");
            
            // Check for bunching
            float headBodyDistance = Vector3.Distance(head.transform.position, body.transform.position);
            float headFaceDistance = Vector3.Distance(head.transform.position, face.transform.position);
            
            output.AppendLine($"\nDistances:");
            output.AppendLine($"Head-Body: {headBodyDistance:F3}");
            output.AppendLine($"Head-Face: {headFaceDistance:F3}");
            
            if (headFaceDistance < 0.01f)
            {
                output.AppendLine("WARNING: Head and Face are at the same position (bunched up)!");
            }
            
            // Simulate facing right (flip=false, scale.x=1)
            output.AppendLine("\n=== FACING RIGHT (flip=false) ===");
            characterObj.transform.localScale = new Vector3(1, 1, 1);
            output.AppendLine($"Character scale: {characterObj.transform.localScale}");
            output.AppendLine("Expected: scale.x = 1 (no flip needed, sprite faces left naturally)");
            
            // Simulate facing left (flip=true, scale.x=-1)
            output.AppendLine("\n=== FACING LEFT (flip=true) ===");
            characterObj.transform.localScale = new Vector3(-1, 1, 1);
            output.AppendLine($"Character scale: {characterObj.transform.localScale}");
            output.AppendLine("Expected: scale.x = -1 (flip the left-facing sprite)");
            
            // Check MapleCharacterRenderer if it exists
            output.AppendLine("\n=== CHECKING MAPLECHARACTERRENDERER ===");
            
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, GameView");
            if (rendererType != null)
            {
                output.AppendLine("MapleCharacterRenderer type found!");
                
                // Check OnVelocityChanged method
                var method = rendererType.GetMethod("OnVelocityChanged");
                if (method != null)
                {
                    output.AppendLine("OnVelocityChanged method found");
                    output.AppendLine("This method should:");
                    output.AppendLine("- Set flip=false when velocity.x > 0 (moving right)");
                    output.AppendLine("- Set flip=true when velocity.x < 0 (moving left)");
                    output.AppendLine("- Update transform.localScale.x based on flip state");
                }
            }
            else
            {
                output.AppendLine("MapleCharacterRenderer type not found - may have compilation errors");
            }
            
            // Recommendations
            output.AppendLine("\n=== RECOMMENDATIONS ===");
            output.AppendLine("1. Fix flip logic inversion:");
            output.AppendLine("   - When moving RIGHT (velocity.x > 0): set flip=false, scale.x=1");
            output.AppendLine("   - When moving LEFT (velocity.x < 0): set flip=true, scale.x=-1");
            output.AppendLine("\n2. Fix bunching issue:");
            output.AppendLine("   - Ensure attachment points are applied correctly");
            output.AppendLine("   - Face should have its own offset from head, not be at the same position");
            output.AppendLine("   - Check that origins/pivots are being applied");
            
            // Write results
            File.WriteAllText("facing-bunching-analysis.txt", output.ToString());
            Debug.Log($"Analysis complete. Results written to facing-bunching-analysis.txt");
            
            // Clean up
            GameObject.DestroyImmediate(characterObj);
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e}");
            File.WriteAllText("facing-bunching-error.txt", e.ToString());
            EditorApplication.Exit(1);
        }
    }
}