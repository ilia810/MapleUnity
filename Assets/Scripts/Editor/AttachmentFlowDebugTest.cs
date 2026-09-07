using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

public static class AttachmentFlowDebugTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== ATTACHMENT FLOW DEBUG TEST ===");
            
            // Create test scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Initialize mock data
            var mockFileType = System.Type.GetType("MockNxFile, Assembly-CSharp");
            var loaderType = System.Type.GetType("MapleClient.GameData.NXAssetLoader, Assembly-CSharp");
            
            if (mockFileType != null && loaderType != null)
            {
                var mockFile = System.Activator.CreateInstance(mockFileType);
                var instanceProp = loaderType.GetProperty("Instance");
                if (instanceProp != null)
                {
                    var loader = instanceProp.GetValue(null);
                    var registerMethod = loaderType.GetMethod("RegisterNxFile");
                    if (registerMethod != null)
                    {
                        registerMethod.Invoke(loader, new object[] { "character", mockFile });
                        Debug.Log("Registered mock NX file");
                    }
                }
            }
            
            // Create character
            var characterObj = new GameObject("TestCharacter");
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            
            if (rendererType == null)
            {
                Debug.LogError("MapleCharacterRenderer not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            var renderer = characterObj.AddComponent(rendererType) as Component;
            
            // Set appearance
            var setAppearanceMethod = rendererType.GetMethod("SetCharacterAppearance");
            if (setAppearanceMethod != null)
            {
                setAppearanceMethod.Invoke(renderer, new object[] { 0, 20000, 30000 });
                Debug.Log("Set character appearance");
            }
            
            // Force Start
            renderer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            
            // Wait a bit for initialization
            System.Threading.Thread.Sleep(1000);
            
            // Get attachment points
            FieldInfo attachmentField = rendererType.GetField("currentAttachmentPoints", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (attachmentField != null)
            {
                var attachmentPoints = attachmentField.GetValue(renderer) as Dictionary<string, Vector2>;
                if (attachmentPoints != null)
                {
                    Debug.Log($"\n=== ATTACHMENT POINTS ({attachmentPoints.Count} total) ===");
                    
                    // Check required points
                    string[] required = {
                        "body.map.neck", "body.neck",
                        "body.map.navel", "body.navel",
                        "head.map.neck", "head.neck",
                        "head.map.brow", "head.brow"
                    };
                    
                    foreach (var key in required)
                    {
                        if (attachmentPoints.ContainsKey(key))
                        {
                            Debug.Log($"âœ“ Found {key}: {attachmentPoints[key]}");
                        }
                        else
                        {
                            Debug.LogWarning($"âœ— Missing {key}");
                        }
                    }
                    
                    // Calculate expected face offset
                    Vector2 bodyNeck = Vector2.zero;
                    Vector2 headNeck = Vector2.zero;
                    Vector2 headBrow = Vector2.zero;
                    
                    if (attachmentPoints.TryGetValue("body.map.neck", out bodyNeck) || 
                        attachmentPoints.TryGetValue("body.neck", out bodyNeck))
                    {
                        Debug.Log($"Using body neck: {bodyNeck}");
                    }
                    
                    if (attachmentPoints.TryGetValue("head.map.neck", out headNeck) || 
                        attachmentPoints.TryGetValue("head.neck", out headNeck))
                    {
                        Debug.Log($"Using head neck: {headNeck}");
                    }
                    
                    if (attachmentPoints.TryGetValue("head.map.brow", out headBrow) || 
                        attachmentPoints.TryGetValue("head.brow", out headBrow))
                    {
                        Debug.Log($"Using head brow: {headBrow}");
                    }
                    
                    Vector2 expectedFaceOffset = bodyNeck - headNeck + headBrow;
                    Debug.Log($"\nExpected face offset: {expectedFaceOffset}");
                    Debug.Log($"Calculation: body.neck({bodyNeck}) - head.neck({headNeck}) + head.brow({headBrow})");
                }
            }
            
            // Check actual positions
            Debug.Log("\n=== ACTUAL CHILD POSITIONS ===");
            Transform body = null, head = null, face = null;
            
            foreach (Transform child in characterObj.transform)
            {
                Debug.Log($"{child.name}: localPosition={child.localPosition}");
                
                if (child.name == "Body") body = child;
                else if (child.name == "Head") head = child;
                else if (child.name == "Face") face = child;
            }
            
            if (head != null && face != null)
            {
                float distance = Vector3.Distance(head.position, face.position);
                Debug.Log($"\nHead-Face distance: {distance:F4}");
                
                if (distance < 0.001f)
                {
                    Debug.LogError("ERROR: Face and Head are at the same position!");
                    Debug.Log($"Head world pos: {head.position}");
                    Debug.Log($"Face world pos: {face.position}");
                    Debug.Log($"Head local pos: {head.localPosition}");
                    Debug.Log($"Face local pos: {face.localPosition}");
                }
                else
                {
                    Debug.Log("SUCCESS: Face and Head are at different positions!");
                }
            }
            
            // Force update and check again
            renderer.SendMessage("UpdateSprites", SendMessageOptions.DontRequireReceiver);
            System.Threading.Thread.Sleep(500);
            
            Debug.Log("\n=== POSITIONS AFTER UPDATE ===");
            foreach (Transform child in characterObj.transform)
            {
                Debug.Log($"{child.name}: localPosition={child.localPosition}");
            }
            
            Debug.Log("\n=== TEST COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}
