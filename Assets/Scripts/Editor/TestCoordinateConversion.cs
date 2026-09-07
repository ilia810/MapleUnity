using UnityEngine;
using UnityEditor;
using MapleClient.GameData;

public static class TestCoordinateConversion
{
    [MenuItem("MapleUnity/Test/Test Coordinate Conversion")]
    public static void RunTest()
    {
        Debug.Log("=== COORDINATE CONVERSION TEST ===");
        
        // Test the mock attachment points
        var mockFile = new MockNxFile();
        
        Debug.Log("\nExpected C++ Runtime Values:");
        Debug.Log("Body neck: (-7, -31)");
        Debug.Log("Head neck: (14, 19)");
        Debug.Log("Head brow: (13, -2)");
        
        Debug.Log("\nC++ Formula:");
        Debug.Log("face_pos = body.neck - head.neck + head.brow");
        Debug.Log("face_pos = (-7, -31) - (14, 19) + (13, -2)");
        Debug.Log("face_pos = (-8, -52)");
        
        Debug.Log("\n=== UNITY CONVERSION ===");
        
        // Unity conversion (divide by 100, flip Y)
        Vector2 bodyNeck = new Vector2(-7, -31);
        Vector2 headNeck = new Vector2(14, 19);
        Vector2 headBrow = new Vector2(13, -2);
        
        // Calculate head position
        Vector2 headPos = bodyNeck - headNeck;
        Debug.Log($"\nHead position (pixels): {headPos}");
        
        // Convert to Unity units
        Vector3 headPosUnity = new Vector3(
            headPos.x / 100f,
            -headPos.y / 100f,  // Flip Y
            0
        );
        Debug.Log($"Head position (Unity): {headPosUnity}");
        
        // Calculate face position
        Vector2 facePos = bodyNeck - headNeck + headBrow;
        Debug.Log($"\nFace position (pixels): {facePos}");
        
        // Convert to Unity units
        Vector3 facePosUnity = new Vector3(
            facePos.x / 100f,
            -facePos.y / 100f,  // Flip Y
            0
        );
        Debug.Log($"Face position (Unity): {facePosUnity}");
        
        // Alternative: Face relative to head
        Vector3 faceRelativeToHead = new Vector3(
            headBrow.x / 100f,
            -headBrow.y / 100f,
            0
        );
        Debug.Log($"\nFace relative to head (Unity): {faceRelativeToHead}");
        
        // Check the body offset issue
        Debug.Log("\n=== BODY OFFSET ISSUE ===");
        Vector2 bodyNavel = new Vector2(0, 0);  // Should be at origin
        Vector3 bodyOffset = new Vector3(
            -bodyNavel.x / 100f,
            bodyNavel.y / 100f,  // This might be the issue!
            0
        );
        Debug.Log($"Body offset calculation: {bodyOffset}");
        Debug.Log("Note: If body.navel is (0,0), body offset should be (0,0,0)");
        
        // The real issue might be here
        Debug.Log("\n=== POTENTIAL ISSUE ===");
        Debug.Log("In ApplyAttachmentOffsets line 405:");
        Debug.Log("bodyNavel.y / 100f should probably be -bodyNavel.y / 100f");
        Debug.Log("Because Unity Y-up means we need to negate MapleStory Y values");
    }
}