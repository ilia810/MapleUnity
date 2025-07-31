using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using GameData;
using System.Linq;

public class CheckFaceStructure : MonoBehaviour
{
    [MenuItem("MapleUnity/Tests/Check Face 20000 Path Structure")]
    public static void RunCheck()
    {
        Debug.Log("[CHECK_FACE_STRUCTURE] Starting check...");
        
        // Initialize NX data if not already loaded
        var singleton = NXDataManagerSingleton.Instance;
        if (singleton == null || singleton.DataManager == null)
        {
            Debug.LogError("NXDataManager not initialized!");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        
        var loader = NXAssetLoader.Instance;
        var charFile = loader.GetNxFile("character");
        
        if (charFile == null || charFile.Root == null)
        {
            Debug.LogError("Character NX file not loaded!");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        
        // Check the exact path that LoadFace is using
        var faceId = 20000;
        var expression = "default";
        
        Debug.Log($"\n=== Checking paths for face {faceId}, expression '{expression}' ===");
        
        // Path that LoadFace is currently using
        var currentPath = $"Face/{faceId:D8}.img/{expression}/0";
        Debug.Log($"\nCurrent LoadFace path: {currentPath}");
        var node1 = charFile.GetNode(currentPath);
        Debug.Log($"Node exists at current path: {node1 != null}");
        
        // Alternative path with 'face' subdirectory
        var altPath = $"Face/{faceId:D8}.img/{expression}/face";
        Debug.Log($"\nAlternative path: {altPath}");
        var node2 = charFile.GetNode(altPath);
        Debug.Log($"Node exists at alternative path: {node2 != null}");
        if (node2 != null)
        {
            Debug.Log($"  - Node type: {node2.GetType().Name}");
            Debug.Log($"  - Has value: {node2.Value != null}");
            Debug.Log($"  - Value type: {node2.Value?.GetType().Name ?? "null"}");
            
            if (node2.Value is byte[] bytes)
            {
                Debug.Log($"  - Byte array length: {bytes.Length}");
                // Check if it's a PNG
                if (bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    Debug.Log($"  - IS A PNG IMAGE!");
                }
            }
        }
        
        // Check what's directly under the expression
        var exprPath = $"Face/{faceId:D8}.img/{expression}";
        Debug.Log($"\nExpression node path: {exprPath}");
        var exprNode = charFile.GetNode(exprPath);
        if (exprNode != null)
        {
            Debug.Log("Children of expression node:");
            foreach (var child in exprNode.Children)
            {
                Debug.Log($"  - {child.Name}: {child.Value?.GetType().Name ?? "Container"}");
                
                // If it's a container, show its children
                if (child.Value == null && child.Children.Any())
                {
                    foreach (var subchild in child.Children.Take(5))
                    {
                        Debug.Log($"    - {subchild.Name}: {subchild.Value?.GetType().Name ?? "Container"}");
                    }
                }
            }
            
            // Check if the expression node itself has a value
            Debug.Log($"\nExpression node value type: {exprNode.Value?.GetType().Name ?? "null"}");
            if (exprNode.Value is byte[] exprBytes)
            {
                Debug.Log($"Expression node IS a byte array with length: {exprBytes.Length}");
                if (exprBytes.Length > 4 && exprBytes[0] == 0x89 && exprBytes[1] == 0x50 && exprBytes[2] == 0x4E && exprBytes[3] == 0x47)
                {
                    Debug.Log("Expression node itself IS A PNG IMAGE!");
                }
            }
        }
        
        // Try to manually load the sprite using different approaches
        Debug.Log("\n=== Trying manual sprite loading ===");
        
        // Try loading from the face subdirectory
        if (node2 != null)
        {
            try
            {
                var sprite = SpriteLoader.LoadSprite(node2, $"face/{faceId}/{expression}");
                if (sprite != null)
                {
                    Debug.Log($"SUCCESS loading from 'face' subdirectory!");
                    Debug.Log($"  - Sprite name: {sprite.name}");
                    Debug.Log($"  - Sprite size: {sprite.rect.width}x{sprite.rect.height}");
                }
                else
                {
                    Debug.Log("Failed to load sprite from 'face' subdirectory");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception loading from 'face' subdirectory: {e.Message}");
            }
        }
        
        // Try loading from the expression node directly
        if (exprNode != null && exprNode.Value is byte[])
        {
            try
            {
                var sprite = SpriteLoader.LoadSprite(exprNode, $"face/{faceId}/{expression}");
                if (sprite != null)
                {
                    Debug.Log($"SUCCESS loading from expression node directly!");
                    Debug.Log($"  - Sprite name: {sprite.name}");
                    Debug.Log($"  - Sprite size: {sprite.rect.width}x{sprite.rect.height}");
                }
                else
                {
                    Debug.Log("Failed to load sprite from expression node directly");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception loading from expression node: {e.Message}");
            }
        }
        
        Debug.Log("\n[CHECK_FACE_STRUCTURE] Check complete!");
        
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
}