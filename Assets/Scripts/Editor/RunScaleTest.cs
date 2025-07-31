using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class RunScaleTest
{
    [MenuItem("MapleUnity/Test Scale-Based Flipping")]
    public static void TestScaleFlipping()
    {
        RunTest();
    }
    
    public static void RunTest()
    {
        Debug.Log("=== Scale-Based Flipping Test Starting ===");
        string report = "";
        
        try
        {
            // Load the main scene
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            if (scene == null)
            {
                report += "ERROR: Could not load MainScene\n";
                File.WriteAllText("scale-test-report.txt", report);
                EditorApplication.Exit(1);
                return;
            }
            
            report += "Scene loaded successfully\n\n";
            
            // Find the MapleCharacterRenderer in the scene
            var renderers = GameObject.FindObjectsOfType<MonoBehaviour>();
            MonoBehaviour characterRenderer = null;
            
            foreach (var renderer in renderers)
            {
                if (renderer.GetType().Name == "MapleCharacterRenderer")
                {
                    characterRenderer = renderer;
                    break;
                }
            }
            
            if (characterRenderer == null)
            {
                report += "ERROR: Could not find MapleCharacterRenderer in scene\n";
                File.WriteAllText("scale-test-report.txt", report);
                EditorApplication.Exit(1);
                return;
            }
            
            report += "Found MapleCharacterRenderer\n\n";
            
            // Test scale-based flipping
            report += "=== Testing Scale-Based Flipping ===\n";
            
            var transform = characterRenderer.transform;
            var initialScale = transform.localScale;
            report += $"Initial scale: {initialScale}\n";
            
            // Get SetFacingDirection method
            var setFacingMethod = characterRenderer.GetType().GetMethod("SetFacingDirection");
            if (setFacingMethod != null)
            {
                // Test facing right
                setFacingMethod.Invoke(characterRenderer, new object[] { true });
                var rightScale = transform.localScale;
                report += $"Facing right scale: {rightScale}\n";
                report += $"  X > 0: {rightScale.x > 0} (Expected: true)\n";
                
                // Test facing left
                setFacingMethod.Invoke(characterRenderer, new object[] { false });
                var leftScale = transform.localScale;
                report += $"Facing left scale: {leftScale}\n";
                report += $"  X < 0: {leftScale.x < 0} (Expected: true)\n";
                
                // Check if only X changes
                bool onlyXChanges = Mathf.Abs(rightScale.y - leftScale.y) < 0.001f && 
                                   Mathf.Abs(rightScale.z - leftScale.z) < 0.001f;
                report += $"  Only X scale changes: {onlyXChanges} (Expected: true)\n";
                
                report += "\nScale-based flipping is " + 
                         (rightScale.x > 0 && leftScale.x < 0 && onlyXChanges ? "WORKING CORRECTLY" : "NOT WORKING") + "\n";
            }
            else
            {
                report += "ERROR: SetFacingDirection method not found\n";
            }
            
            // Check sprite renderers
            report += "\n=== Checking Sprite Renderers ===\n";
            var sprites = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
            report += $"Total sprite renderers: {sprites.Length}\n";
            
            // Group by type
            int bodyCount = 0, headCount = 0, faceCount = 0, armCount = 0, equipCount = 0;
            
            foreach (var sprite in sprites)
            {
                if (sprite.name.Contains("body")) bodyCount++;
                else if (sprite.name.Contains("head")) headCount++;
                else if (sprite.name.Contains("face")) faceCount++;
                else if (sprite.name.Contains("arm")) armCount++;
                else if (sprite.name.Contains("coat") || sprite.name.Contains("mail")) equipCount++;
                
                if (sprite.name.Contains("arm") || sprite.name.Contains("face"))
                {
                    report += $"\n{sprite.name}:\n";
                    report += $"  Position: {sprite.transform.localPosition}\n";
                    report += $"  Sorting Order: {sprite.sortingOrder}\n";
                    report += $"  Flip X: {sprite.flipX}\n";
                }
            }
            
            report += $"\nSprite counts - Body: {bodyCount}, Head: {headCount}, Face: {faceCount}, Arm: {armCount}, Equipment: {equipCount}\n";
            
            // Test animations
            report += "\n=== Testing Animations ===\n";
            var playAnimMethod = characterRenderer.GetType().GetMethod("PlayAnimation");
            if (playAnimMethod != null)
            {
                string[] animations = { "stand", "walk", "crouch" };
                foreach (var anim in animations)
                {
                    playAnimMethod.Invoke(characterRenderer, new object[] { anim });
                    report += $"\nAnimation '{anim}':\n";
                    report += $"  Scale: {transform.localScale}\n";
                    
                    var activeSprites = 0;
                    foreach (var sprite in sprites)
                    {
                        if (sprite.enabled && sprite.sprite != null) activeSprites++;
                    }
                    report += $"  Active sprites: {activeSprites}\n";
                }
            }
            
            report += "\n=== Test Completed Successfully ===\n";
            
            File.WriteAllText("scale-test-report.txt", report);
            Debug.Log("Test completed. Report written to scale-test-report.txt");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            report += $"\nEXCEPTION: {e.Message}\n{e.StackTrace}\n";
            File.WriteAllText("scale-test-report.txt", report);
            Debug.LogError($"Test failed: {e}");
            EditorApplication.Exit(1);
        }
    }
}