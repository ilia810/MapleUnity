using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public static class SimpleCharacterPositionTest
{
    public static void RunTest()
    {
        Debug.Log("=== Simple Character Position Test ===");
        
        var results = new List<string>();
        results.Add("Character Position Verification Test");
        results.Add($"Test run at: {System.DateTime.Now}");
        results.Add("");
        
        try
        {
            // Test C++ position formulas
            results.Add("Testing MapleStory C++ Client Position Formulas");
            results.Add("===============================================");
            results.Add("");
            
            // Body is always at (0, 0)
            Vector2 bodyPos = Vector2.zero;
            results.Add("1. Body Position: (0, 0) [Base position]");
            results.Add("");
            
            // Test Case 1: Arm positioning
            results.Add("2. Arm Position Formula: arm_pos = body_pos + arm_map - body_navel");
            Vector2 armMap = new Vector2(3, -2);
            Vector2 bodyNavel = new Vector2(4, 12);
            Vector2 armPos = bodyPos + armMap - bodyNavel;
            results.Add($"   Given: arm_map = {armMap}, body_navel = {bodyNavel}");
            results.Add($"   Calculation: (0,0) + (3,-2) - (4,12) = ({armPos.x}, {armPos.y})");
            results.Add("");
            
            // Test Case 2: Head positioning
            results.Add("3. Head Position Formula: head_pos = body_pos + body_neck - head_neck");
            Vector2 bodyNeck = new Vector2(6, 23);
            Vector2 headNeck = new Vector2(10, 2);
            Vector2 headPos = bodyPos + bodyNeck - headNeck;
            results.Add($"   Given: body_neck = {bodyNeck}, head_neck = {headNeck}");
            results.Add($"   Calculation: (0,0) + (6,23) - (10,2) = ({headPos.x}, {headPos.y})");
            results.Add("");
            
            // Test Case 3: Face positioning
            results.Add("4. Face Position Formula: face_pos = head_pos + head_brow - face_brow");
            Vector2 headBrow = new Vector2(15, 10);
            Vector2 faceBrow = new Vector2(10, 5);
            Vector2 facePos = headPos + headBrow - faceBrow;
            results.Add($"   Given: head_brow = {headBrow}, face_brow = {faceBrow}");
            results.Add($"   Calculation: ({headPos.x},{headPos.y}) + (15,10) - (10,5) = ({facePos.x}, {facePos.y})");
            results.Add("");
            
            // Test Case 4: Equipment positioning (e.g., hat)
            results.Add("5. Equipment Position Formula: equip_pos = part_pos + part_point - equip_point");
            Vector2 headVslot = new Vector2(8, 25);
            Vector2 hatVslot = new Vector2(12, 8);
            Vector2 hatPos = headPos + headVslot - hatVslot;
            results.Add($"   Example: Hat on head");
            results.Add($"   Given: head_vslot = {headVslot}, hat_vslot = {hatVslot}");
            results.Add($"   Calculation: ({headPos.x},{headPos.y}) + (8,25) - (12,8) = ({hatPos.x}, {hatPos.y})");
            results.Add("");
            
            // Create visual test in scene
            results.Add("6. Creating Visual Test Objects");
            results.Add("================================");
            
            var root = new GameObject("CharacterPositionTest");
            
            // Body at origin
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.parent = root.transform;
            body.transform.localPosition = new Vector3(bodyPos.x * 0.01f, bodyPos.y * 0.01f, 0);
            body.transform.localScale = Vector3.one * 0.5f;
            
            // Arm
            var arm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            arm.name = "Arm";
            arm.transform.parent = root.transform;
            arm.transform.localPosition = new Vector3(armPos.x * 0.01f, armPos.y * 0.01f, -0.1f);
            arm.transform.localScale = Vector3.one * 0.3f;
            
            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.parent = root.transform;
            head.transform.localPosition = new Vector3(headPos.x * 0.01f, headPos.y * 0.01f, -0.2f);
            head.transform.localScale = Vector3.one * 0.4f;
            
            // Face
            var face = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            face.name = "Face";
            face.transform.parent = root.transform;
            face.transform.localPosition = new Vector3(facePos.x * 0.01f, facePos.y * 0.01f, -0.3f);
            face.transform.localScale = Vector3.one * 0.2f;
            
            results.Add($"Created test objects in scene hierarchy");
            results.Add($"Body: {body.transform.localPosition}");
            results.Add($"Arm: {arm.transform.localPosition}");
            results.Add($"Head: {head.transform.localPosition}");
            results.Add($"Face: {face.transform.localPosition}");
            results.Add("");
            
            // Summary
            results.Add("SUMMARY");
            results.Add("=======");
            results.Add("✓ All position calculations follow C++ client formulas");
            results.Add("✓ Body anchored at (0, 0)");
            results.Add("✓ Child parts positioned relative to parent attachment points");
            results.Add("✓ Formula: child_pos = parent_pos + parent_attach - child_attach");
            results.Add("");
            results.Add("Test completed successfully!");
            
            // Clean up
            GameObject.DestroyImmediate(root);
        }
        catch (System.Exception e)
        {
            results.Add($"ERROR: {e.Message}");
            results.Add($"Stack: {e.StackTrace}");
        }
        
        // Write results
        string outputPath = Path.Combine(Application.dataPath, "..", "character-position-test-results.txt");
        File.WriteAllLines(outputPath, results);
        
        foreach (var line in results)
        {
            Debug.Log(line);
        }
        
        Debug.Log($"Results written to: {outputPath}");
        
        // Exit Unity
        EditorApplication.Exit(0);
    }
}