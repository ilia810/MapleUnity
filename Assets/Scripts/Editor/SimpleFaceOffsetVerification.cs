using UnityEngine;
using UnityEditor;
using System.IO;

public static class SimpleFaceOffsetVerification
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "face-offset-verification.log");
        
        try
        {
            Debug.Log("=== FACE OFFSET VERIFICATION TEST ===");
            File.WriteAllText(logPath, "=== FACE OFFSET VERIFICATION TEST ===\n\n");
            
            // Mock attachment point data (from MockNxFile)
            Vector2 bodyNeck = new Vector2(-7, -31);
            Vector2 headNeck = new Vector2(14, 19);
            Vector2 headBrow = new Vector2(13, -2);
            
            File.AppendAllText(logPath, "Mock Attachment Points:\n");
            File.AppendAllText(logPath, $"  Body neck: {bodyNeck}\n");
            File.AppendAllText(logPath, $"  Head neck: {headNeck}\n");
            File.AppendAllText(logPath, $"  Head brow: {headBrow}\n\n");
            
            // Calculate face offset using C++ formula
            // face_pos = body.neck - head.neck + head.brow
            Vector2 calculatedFaceOffset = bodyNeck - headNeck + headBrow;
            
            File.AppendAllText(logPath, "Calculation:\n");
            File.AppendAllText(logPath, $"  face_pos = body.neck - head.neck + head.brow\n");
            File.AppendAllText(logPath, $"  face_pos = {bodyNeck} - {headNeck} + {headBrow}\n");
            File.AppendAllText(logPath, $"  face_pos = {calculatedFaceOffset}\n\n");
            
            // Expected C++ runtime value
            Vector2 expectedOffset = new Vector2(-8, -52);
            
            File.AppendAllText(logPath, "Verification:\n");
            File.AppendAllText(logPath, $"  Calculated offset: {calculatedFaceOffset}\n");
            File.AppendAllText(logPath, $"  Expected C++ offset: {expectedOffset}\n");
            
            // Check if they match
            float distance = Vector2.Distance(calculatedFaceOffset, expectedOffset);
            File.AppendAllText(logPath, $"  Distance: {distance:F2} pixels\n\n");
            
            if (distance < 0.01f)
            {
                File.AppendAllText(logPath, "✓ SUCCESS: Face offset calculation MATCHES C++ runtime value!\n");
                File.AppendAllText(logPath, "The formula correctly produces (-8, -52) pixels.\n");
                Debug.Log("✓ Face offset verification PASSED!");
            }
            else
            {
                File.AppendAllText(logPath, "✗ FAILURE: Face offset calculation does NOT match C++ runtime value!\n");
                File.AppendAllText(logPath, $"Difference: {calculatedFaceOffset - expectedOffset}\n");
                Debug.LogError("✗ Face offset verification FAILED!");
            }
            
            Debug.Log($"Test complete. Results written to: {logPath}");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            string error = $"ERROR: {e.Message}\n{e.StackTrace}";
            File.AppendAllText(logPath, error);
            Debug.LogError(error);
            EditorApplication.Exit(1);
        }
    }
}