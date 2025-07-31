using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using GameData;
using System.Linq;

public class ExploreFace20000 : MonoBehaviour
{
    [MenuItem("MapleUnity/Tests/Explore Face 20000 Structure")]
    public static void RunExploration()
    {
        Debug.Log("[EXPLORE_FACE_20000] Starting exploration of face ID 20000...");
        
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
        
        // Look for face ID 20000 (00020000.img)
        Debug.Log("\n=== Exploring Face ID 20000 (00020000.img) ===");
        
        var faceNode = charFile.GetNode("Face/00020000.img");
        if (faceNode == null)
        {
            Debug.LogError("Face ID 00020000.img not found!");
            
            // Try alternative paths
            Debug.Log("Trying alternative paths...");
            faceNode = charFile.Root["Face"]?["00020000.img"];
            if (faceNode == null)
            {
                Debug.LogError("Still not found via alternative path!");
                
                // List what's under Face node
                var faceRoot = charFile.GetNode("Face");
                if (faceRoot != null)
                {
                    Debug.Log("Face node children (first 5):");
                    foreach (var child in faceRoot.Children.Take(5))
                    {
                        Debug.Log($"  - {child.Name}");
                    }
                }
                
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
        }
        
        Debug.Log($"Found face node! Type: {faceNode.GetType().Name}");
        Debug.Log($"Has children: {faceNode.Children.Any()}");
        Debug.Log($"Child count: {faceNode.Children.Count()}");
        
        // List all expressions
        Debug.Log("\nAll expressions found:");
        foreach (var expr in faceNode.Children.OrderBy(c => c.Name))
        {
            Debug.Log($"  Expression: {expr.Name}");
            
            // Check if it has a canvas
            var canvas = expr["canvas"];
            if (canvas != null)
            {
                Debug.Log($"    - Has canvas");
                
                // Check for _inlink
                var inlink = canvas["_inlink"];
                if (inlink != null)
                {
                    Debug.Log($"    - Canvas has _inlink: {inlink.Value}");
                }
                
                // Check for direct image
                var canvasValue = canvas.Value;
                if (canvasValue != null)
                {
                    Debug.Log($"    - Canvas value type: {canvasValue.GetType().Name}");
                }
            }
            
            // Check for face subdirectory
            var face = expr["face"];
            if (face != null)
            {
                Debug.Log($"    - Has face subdirectory");
                
                // Check what's inside face
                foreach (var faceChild in face.Children.Take(3))
                {
                    Debug.Log($"      - {faceChild.Name}: {faceChild.Value?.GetType().Name ?? "Container"}");
                }
            }
            
            // Check for direct image at expression level
            var exprValue = expr.Value;
            if (exprValue != null)
            {
                Debug.Log($"    - Expression value type: {exprValue.GetType().Name}");
            }
        }
        
        // Try to load the default expression specifically
        Debug.Log("\n=== Testing default expression loading ===");
        var defaultExpr = faceNode["default"];
        if (defaultExpr != null)
        {
            Debug.Log("Found default expression");
            
            // Try different paths to find the actual sprite
            string[] pathsToTry = {
                "face",
                "face/canvas",
                "canvas",
                "0",
                "face/0"
            };
            
            foreach (var path in pathsToTry)
            {
                var node = defaultExpr.GetNode(path);
                if (node != null)
                {
                    Debug.Log($"  Path '{path}' exists:");
                    Debug.Log($"    - Type: {node.GetType().Name}");
                    Debug.Log($"    - Has value: {node.Value != null}");
                    Debug.Log($"    - Value type: {node.Value?.GetType().Name ?? "null"}");
                    Debug.Log($"    - Children: {node.Children.Count()}");
                    
                    if (node.Value != null && node.Value.GetType().Name.Contains("Bitmap"))
                    {
                        Debug.Log($"    - FOUND BITMAP at path: {path}");
                    }
                }
            }
            
            // Try to load using NXAssetLoader
            Debug.Log("\nTrying to load with NXAssetLoader.LoadFace(20000, \"default\"):");
            try
            {
                var sprite = loader.LoadFace(20000, "default");
                if (sprite != null)
                {
                    Debug.Log("SUCCESS: Sprite loaded!");
                    Debug.Log($"  - Sprite name: {sprite.name}");
                    Debug.Log($"  - Sprite size: {sprite.rect.width}x{sprite.rect.height}");
                }
                else
                {
                    Debug.LogError("FAILED: LoadFace returned null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EXCEPTION in LoadFace: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        Debug.Log("\n[EXPLORE_FACE_20000] Exploration complete!");
        
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
}