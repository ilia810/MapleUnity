using UnityEngine;
using System.IO;
using System.Collections;
using MapleClient.GameView;

public class RuntimeCharacterTest : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(TestCharacterPositioning());
    }
    
    IEnumerator TestCharacterPositioning()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "runtime-character-test.txt");
        
        File.WriteAllText(logPath, "=== Runtime Character Test Started ===\n");
        File.AppendAllText(logPath, $"Time: {System.DateTime.Now}\n\n");
        
        // Find character renderer
        var characterRenderer = FindObjectOfType<MapleCharacterRenderer>();
        if (characterRenderer == null)
        {
            File.AppendAllText(logPath, "ERROR: No MapleCharacterRenderer found in scene!\n");
            
            // Try to create one
            File.AppendAllText(logPath, "Creating MapleCharacterRenderer...\n");
            GameObject charObj = new GameObject("TestCharacter");
            characterRenderer = charObj.AddComponent<MapleCharacterRenderer>();
            
            // Wait a frame for initialization
            yield return null;
        }
        
        File.AppendAllText(logPath, "Character renderer found/created\n\n");
        
        // Log character hierarchy
        File.AppendAllText(logPath, "=== Character Hierarchy ===\n");
        LogTransformHierarchy(characterRenderer.transform, logPath, 0);
        
        // Wait for sprites to load
        yield return new WaitForSeconds(0.5f);
        
        try
        {
            
            // Check body parts
            File.AppendAllText(logPath, "\n=== Body Part Analysis ===\n");
            
            // Check body
            Transform bodyTransform = characterRenderer.transform.Find("Body");
            if (bodyTransform != null)
            {
                var spriteRenderer = bodyTransform.GetComponent<SpriteRenderer>();
                File.AppendAllText(logPath, $"Body:\n");
                File.AppendAllText(logPath, $"  Position: {bodyTransform.position} (Local: {bodyTransform.localPosition})\n");
                File.AppendAllText(logPath, $"  Has Sprite: {(spriteRenderer != null && spriteRenderer.sprite != null)}\n");
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    File.AppendAllText(logPath, $"  Sprite: {spriteRenderer.sprite.name}\n");
                    File.AppendAllText(logPath, $"  Sprite Bounds: {spriteRenderer.sprite.bounds}\n");
                    File.AppendAllText(logPath, $"  Sprite Pivot: {spriteRenderer.sprite.pivot}\n");
                }
                File.AppendAllText(logPath, $"  Y Position Check: {(Mathf.Abs(bodyTransform.position.y) < 0.1f ? "CORRECT (at ground)" : "INCORRECT (should be at Y=0)")}\n");
            }
            else
            {
                File.AppendAllText(logPath, "Body: NOT FOUND\n");
            }
            
            // Check head
            Transform headTransform = characterRenderer.transform.Find("Head");
            if (headTransform != null)
            {
                var spriteRenderer = headTransform.GetComponent<SpriteRenderer>();
                File.AppendAllText(logPath, $"\nHead:\n");
                File.AppendAllText(logPath, $"  Position: {headTransform.position} (Local: {headTransform.localPosition})\n");
                File.AppendAllText(logPath, $"  Has Sprite: {(spriteRenderer != null && spriteRenderer.sprite != null)}\n");
                if (bodyTransform != null)
                {
                    float headOffset = headTransform.position.y - bodyTransform.position.y;
                    File.AppendAllText(logPath, $"  Offset from body: {headOffset:F3} units\n");
                    File.AppendAllText(logPath, $"  Position Check: {(headOffset > 0 ? "CORRECT (above body)" : "INCORRECT (should be above body)")}\n");
                }
            }
            else
            {
                File.AppendAllText(logPath, "Head: NOT FOUND\n");
            }
            
            // Check arm
            Transform armTransform = characterRenderer.transform.Find("Arm");
            if (armTransform != null)
            {
                var spriteRenderer = armTransform.GetComponent<SpriteRenderer>();
                File.AppendAllText(logPath, $"\nArm:\n");
                File.AppendAllText(logPath, $"  Position: {armTransform.position} (Local: {armTransform.localPosition})\n");
                File.AppendAllText(logPath, $"  Has Sprite: {(spriteRenderer != null && spriteRenderer.sprite != null)}\n");
                if (bodyTransform != null && headTransform != null)
                {
                    float armY = armTransform.position.y;
                    float bodyY = bodyTransform.position.y;
                    float headY = headTransform.position.y;
                    float relativePosition = (armY - bodyY) / (headY - bodyY);
                    File.AppendAllText(logPath, $"  Relative position: {relativePosition:F3} (0=body level, 1=head level)\n");
                    File.AppendAllText(logPath, $"  Position Check: {(relativePosition > 0.3f && relativePosition < 0.7f ? "CORRECT (mid-body)" : "INCORRECT (should be at mid-body/navel)")}\n");
                }
            }
            else
            {
                File.AppendAllText(logPath, "Arm: NOT FOUND\n");
            }
            
            // Check face
            Transform faceTransform = characterRenderer.transform.Find("Face");
            if (faceTransform != null)
            {
                var spriteRenderer = faceTransform.GetComponent<SpriteRenderer>();
                File.AppendAllText(logPath, $"\nFace:\n");
                File.AppendAllText(logPath, $"  Position: {faceTransform.position} (Local: {faceTransform.localPosition})\n");
                File.AppendAllText(logPath, $"  Has Sprite: {(spriteRenderer != null && spriteRenderer.sprite != null)}\n");
                if (headTransform != null)
                {
                    float faceOffsetX = Mathf.Abs(faceTransform.position.x - headTransform.position.x);
                    float faceOffsetY = Mathf.Abs(faceTransform.position.y - headTransform.position.y);
                    File.AppendAllText(logPath, $"  Offset from head: X={faceOffsetX:F3}, Y={faceOffsetY:F3}\n");
                    File.AppendAllText(logPath, $"  Position Check: {(faceOffsetX < 0.5f && faceOffsetY < 0.5f ? "CORRECT (within head)" : "INCORRECT (should be within head bounds)")}\n");
                }
            }
            else
            {
                File.AppendAllText(logPath, "Face: NOT FOUND\n");
            }
            
            File.AppendAllText(logPath, "\n=== Test Completed ===\n");
            Debug.Log("Runtime character test completed. Results written to runtime-character-test.txt");
            
            // Quit if in batch mode
            if (Application.isBatchMode)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(0);
                #endif
            }
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nERROR: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"Test failed: {e.Message}");
            
            if (Application.isBatchMode)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(1);
                #endif
            }
        }
    }
    
    void LogTransformHierarchy(Transform transform, string logPath, int depth)
    {
        string indent = new string(' ', depth * 2);
        var spriteRenderer = transform.GetComponent<SpriteRenderer>();
        string spriteInfo = "";
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            spriteInfo = $" [Sprite: {spriteRenderer.sprite.name}]";
        }
        
        File.AppendAllText(logPath, $"{indent}{transform.name} - Pos: {transform.localPosition}, World: {transform.position}{spriteInfo}\n");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            LogTransformHierarchy(transform.GetChild(i), logPath, depth + 1);
        }
    }
}