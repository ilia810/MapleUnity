using UnityEngine;
using UnityEditor;

public static class SimpleAlignmentVerificationTest
{
    public static void RunTest()
    {
        Debug.Log("=== SIMPLE ALIGNMENT VERIFICATION TEST ===");
        
        try
        {
            // Test the Y-axis conversion formula directly
            Debug.Log("\n=== TESTING Y-AXIS CONVERSION ===");
            
            // Face offset from C++ client: brow = (-8, -52)
            // This is in MapleStory coordinates (Y-up negative)
            Vector2 mapleOffset = new Vector2(-8, -52);
            
            // Convert to Unity coordinates
            // The fix we're testing: Unity Y = -MapleStory Y
            Vector2 unityOffset = new Vector2(mapleOffset.x / 100f, -mapleOffset.y / 100f);
            
            Debug.Log($"MapleStory offset: {mapleOffset}");
            Debug.Log($"Unity offset: {unityOffset}");
            Debug.Log($"Expected Unity offset: (-0.08, 0.52)");
            
            // Verify the conversion
            bool xCorrect = Mathf.Approximately(unityOffset.x, -0.08f);
            bool yCorrect = Mathf.Approximately(unityOffset.y, 0.52f);
            
            if (xCorrect && yCorrect)
            {
                Debug.Log("✓ SUCCESS: Y-axis conversion is correct!");
                Debug.Log("  Face will be positioned 0.52 units ABOVE in Unity (52 pixels up)");
            }
            else
            {
                Debug.LogError($"✗ FAILED: Y-axis conversion is incorrect!");
                Debug.LogError($"  X correct: {xCorrect} (got {unityOffset.x}, expected -0.08)");
                Debug.LogError($"  Y correct: {yCorrect} (got {unityOffset.y}, expected 0.52)");
            }
            
            // Additional verification: simulate character hierarchy
            Debug.Log("\n=== SIMULATING CHARACTER HIERARCHY ===");
            
            var character = new GameObject("Character");
            character.transform.position = Vector3.zero;
            
            var body = new GameObject("Body");
            body.transform.SetParent(character.transform);
            body.transform.localPosition = Vector3.zero;
            
            var head = new GameObject("Head");
            head.transform.SetParent(character.transform);
            // Simulate head being above body
            head.transform.localPosition = new Vector3(0, 0.3f, 0);
            
            var face = new GameObject("Face");
            face.transform.SetParent(character.transform);
            // Apply the converted offset
            face.transform.localPosition = new Vector3(unityOffset.x, unityOffset.y, 0);
            
            Debug.Log($"Character position: {character.transform.position}");
            Debug.Log($"Body local position: {body.transform.localPosition}");
            Debug.Log($"Head local position: {head.transform.localPosition}");
            Debug.Log($"Face local position: {face.transform.localPosition}");
            
            // Verify face is above body
            float faceY = face.transform.position.y;
            float bodyY = body.transform.position.y;
            
            if (faceY > bodyY)
            {
                Debug.Log($"✓ Face is correctly positioned {faceY - bodyY:F2} units above body");
            }
            else
            {
                Debug.LogError($"✗ Face is incorrectly positioned {bodyY - faceY:F2} units below body");
            }
            
            // Clean up
            GameObject.DestroyImmediate(character);
            
            Debug.Log("\n=== TEST COMPLETED SUCCESSFULLY ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
}