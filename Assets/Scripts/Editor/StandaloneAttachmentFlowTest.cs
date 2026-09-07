using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public static class StandaloneAttachmentFlowTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== STANDALONE ATTACHMENT FLOW TEST ===");
            
            // Create test scene
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, 
                UnityEditor.SceneManagement.NewSceneMode.Single);
            
            // Try to get the types we need
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            var mockFileType = System.Type.GetType("MockNxFile, Assembly-CSharp");
            var loaderType = System.Type.GetType("MapleClient.GameData.NXAssetLoader, Assembly-CSharp");
            
            if (rendererType == null)
            {
                Debug.LogError("Could not find MapleCharacterRenderer type!");
                Debug.Log("Trying without namespace...");
                rendererType = System.Type.GetType("MapleCharacterRenderer, Assembly-CSharp");
            }
            
            if (rendererType == null)
            {
                Debug.LogError("Still could not find MapleCharacterRenderer!");
                
                // List all types to debug
                Debug.Log("=== Listing all types in Assembly-CSharp ===");
                var assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
                if (assembly != null)
                {
                    var types = assembly.GetTypes();
                    int count = 0;
                    foreach (var t in types)
                    {
                        if (t.Name.Contains("Character") || t.Name.Contains("Renderer"))
                        {
                            Debug.Log($"Found: {t.FullName}");
                            count++;
                        }
                    }
                    Debug.Log($"Total character/renderer types found: {count}");
                }
                
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log($"Found renderer type: {rendererType.FullName}");
            
            // Create character object
            var characterObj = new GameObject("TestCharacter");
            var renderer = characterObj.AddComponent(rendererType) as Component;
            
            if (renderer == null)
            {
                Debug.LogError("Failed to add MapleCharacterRenderer component!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log("Successfully created character with renderer");
            
            // Try to set up mock data
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
            
            // Set character appearance
            var setAppearanceMethod = rendererType.GetMethod("SetCharacterAppearance");
            if (setAppearanceMethod != null)
            {
                setAppearanceMethod.Invoke(renderer, new object[] { 0, 20000, 30000 });
                Debug.Log("Set character appearance: skin=0, face=20000, hair=30000");
            }
            
            // Force Start
            renderer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            
            // Check attachment points
            FieldInfo attachmentField = rendererType.GetField("currentAttachmentPoints", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (attachmentField != null)
            {
                var attachmentPoints = attachmentField.GetValue(renderer) as Dictionary<string, Vector2>;
                if (attachmentPoints != null)
                {
                    Debug.Log($"\n=== ATTACHMENT POINTS ({attachmentPoints.Count} total) ===");
                    foreach (var kvp in attachmentPoints)
                    {
                        Debug.Log($"  {kvp.Key}: {kvp.Value}");
                    }
                }
            }
            
            // Check child positions
            Debug.Log("\n=== CHILD POSITIONS ===");
            foreach (Transform child in characterObj.transform)
            {
                Debug.Log($"{child.name}: localPosition={child.localPosition}, worldPosition={child.position}");
            }
            
            // Force update
            renderer.SendMessage("UpdateSprites", SendMessageOptions.DontRequireReceiver);
            
            // Wait a bit
            System.Threading.Thread.Sleep(1000);
            
            // Check positions again
            Debug.Log("\n=== CHILD POSITIONS AFTER UPDATE ===");
            foreach (Transform child in characterObj.transform)
            {
                Debug.Log($"{child.name}: localPosition={child.localPosition}, worldPosition={child.position}");
                
                // Check if face and head are at same position
                if (child.name == "Face")
                {
                    var head = characterObj.transform.Find("Head");
                    if (head != null)
                    {
                        float distance = Vector3.Distance(head.position, child.position);
                        Debug.Log($"Head-Face distance: {distance:F4}");
                        if (distance < 0.001f)
                        {
                            Debug.LogError("ERROR: Face and Head are at the same position!");
                        }
                    }
                }
            }
            
            Debug.Log("=== TEST COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}