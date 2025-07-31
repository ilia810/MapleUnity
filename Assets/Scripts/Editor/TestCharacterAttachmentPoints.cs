using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;

public static class TestCharacterAttachmentPoints
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "character-attachment-test.log");
        
        try
        {
            File.WriteAllText(logPath, $"[TEST] Character Attachment Point Test - {DateTime.Now}\n\n");
            Debug.Log("[TEST] Starting character attachment point test...");
            
            // Create test scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            File.AppendAllText(logPath, "[TEST] Created new test scene\n");
            
            // Create player object
            GameObject player = new GameObject("Player");
            player.transform.position = Vector3.zero;
            File.AppendAllText(logPath, "[TEST] Created player object\n");
            
            // Add MapleCharacterRenderer
            var rendererType = Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            if (rendererType == null)
            {
                File.AppendAllText(logPath, "[ERROR] Could not find MapleCharacterRenderer type\n");
                EditorApplication.Exit(1);
                return;
            }
            
            var renderer = player.AddComponent(rendererType) as MonoBehaviour;
            File.AppendAllText(logPath, "[TEST] Added MapleCharacterRenderer component\n");
            
            // Force initialization
            EditorApplication.QueuePlayerLoopUpdate();
            
            // Wait a frame for initialization
            System.Threading.Thread.Sleep(100);
            
            // Analyze the character structure
            File.AppendAllText(logPath, "\n[TEST] === CHARACTER STRUCTURE ANALYSIS ===\n");
            
            var children = player.GetComponentsInChildren<Transform>();
            File.AppendAllText(logPath, $"Total child objects: {children.Length}\n\n");
            
            foreach (var child in children)
            {
                if (child == player.transform) continue;
                
                File.AppendAllText(logPath, $"Object: {child.name}\n");
                File.AppendAllText(logPath, $"  - Parent: {(child.parent != null ? child.parent.name : "None")}\n");
                File.AppendAllText(logPath, $"  - Local Position: {child.localPosition}\n");
                File.AppendAllText(logPath, $"  - World Position: {child.position}\n");
                
                var spriteRenderer = child.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    File.AppendAllText(logPath, $"  - Has SpriteRenderer: Yes\n");
                    File.AppendAllText(logPath, $"  - Sprite: {(spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "NULL")}\n");
                    File.AppendAllText(logPath, $"  - Sorting Order: {spriteRenderer.sortingOrder}\n");
                    File.AppendAllText(logPath, $"  - Enabled: {spriteRenderer.enabled}\n");
                }
                
                File.AppendAllText(logPath, "\n");
            }
            
            // Check attachment points specifically
            File.AppendAllText(logPath, "\n[TEST] === ATTACHMENT POINT ANALYSIS ===\n");
            
            // Look for body parts and their positions
            var bodyParts = new string[] { "Body", "Head", "Face", "Hair", "Arm", "Hand", "Leg" };
            foreach (var partName in bodyParts)
            {
                var part = FindChildByName(player.transform, partName);
                if (part != null)
                {
                    File.AppendAllText(logPath, $"\n{partName}:\n");
                    File.AppendAllText(logPath, $"  - Found at: {part.localPosition}\n");
                    File.AppendAllText(logPath, $"  - Parent: {(part.parent != null ? part.parent.name : "Root")}\n");
                    
                    // Check if it has expected children
                    if (part.childCount > 0)
                    {
                        File.AppendAllText(logPath, $"  - Children: ");
                        foreach (Transform child in part)
                        {
                            File.AppendAllText(logPath, $"{child.name} ");
                        }
                        File.AppendAllText(logPath, "\n");
                    }
                }
                else
                {
                    File.AppendAllText(logPath, $"\n{partName}: NOT FOUND\n");
                }
            }
            
            // Test setting character appearance
            File.AppendAllText(logPath, "\n[TEST] === TESTING CHARACTER APPEARANCE ===\n");
            
            try
            {
                // Use reflection to call SetCharacterAppearance
                var setAppearanceMethod = rendererType.GetMethod("SetCharacterAppearance");
                if (setAppearanceMethod != null)
                {
                    // Create test appearance
                    var appearanceType = Type.GetType("MapleClient.GameLogic.CharacterAppearance, Assembly-CSharp");
                    if (appearanceType != null)
                    {
                        var appearance = Activator.CreateInstance(appearanceType);
                        
                        // Set some basic values
                        appearanceType.GetField("skinId").SetValue(appearance, 0);
                        appearanceType.GetField("faceId").SetValue(appearance, 20000);
                        appearanceType.GetField("hairId").SetValue(appearance, 30000);
                        
                        setAppearanceMethod.Invoke(renderer, new object[] { appearance });
                        File.AppendAllText(logPath, "[TEST] Called SetCharacterAppearance successfully\n");
                        
                        // Wait for update
                        EditorApplication.QueuePlayerLoopUpdate();
                        System.Threading.Thread.Sleep(100);
                        
                        // Re-analyze structure
                        File.AppendAllText(logPath, "\n[TEST] === POST-APPEARANCE STRUCTURE ===\n");
                        
                        var postChildren = player.GetComponentsInChildren<Transform>();
                        File.AppendAllText(logPath, $"Total child objects after appearance: {postChildren.Length}\n");
                        
                        foreach (var child in postChildren)
                        {
                            if (child == player.transform) continue;
                            
                            var sr = child.GetComponent<SpriteRenderer>();
                            if (sr != null && sr.sprite != null)
                            {
                                File.AppendAllText(logPath, $"{child.name}: {sr.sprite.name} at {child.localPosition}\n");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                File.AppendAllText(logPath, $"[ERROR] Failed to set appearance: {e.Message}\n");
            }
            
            File.AppendAllText(logPath, "\n[TEST] Test completed successfully!\n");
            Debug.Log("[TEST] Test completed successfully! Check character-attachment-test.log");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"\n[ERROR] Test failed: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"[TEST] Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains(name))
                return child;
        }
        return null;
    }
}