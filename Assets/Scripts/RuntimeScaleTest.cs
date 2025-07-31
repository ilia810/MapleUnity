using UnityEngine;
using System.Collections;
using System.IO;
using System;

namespace MapleClient
{
    public class RuntimeScaleTest : MonoBehaviour
    {
        private bool testStarted = false;
        private string testReport = "";
        
        void Start()
        {
            if (!testStarted)
            {
                testStarted = true;
                StartCoroutine(RunScaleTest());
            }
        }
        
        IEnumerator RunScaleTest()
        {
            testReport = "=== Runtime Scale-Based Flipping Test ===\n";
            testReport += $"Started at: {DateTime.Now}\n\n";
            
            // Wait a frame for scene to initialize
            yield return null;
            
            // Find MapleCharacterRenderer
            var renderers = FindObjectsOfType<MonoBehaviour>();
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
                testReport += "ERROR: Could not find MapleCharacterRenderer\n";
                SaveReport();
                yield break;
            }
            
            testReport += "Found MapleCharacterRenderer\n\n";
            
            // Test scale-based flipping
            testReport += "=== Testing Scale-Based Flipping ===\n";
            
            var transform = characterRenderer.transform;
            var initialScale = transform.localScale;
            testReport += $"Initial scale: {initialScale}\n";
            
            // Get SetFacingDirection method
            var setFacingMethod = characterRenderer.GetType().GetMethod("SetFacingDirection");
            if (setFacingMethod != null)
            {
                // Test facing right
                setFacingMethod.Invoke(characterRenderer, new object[] { true });
                yield return new WaitForSeconds(0.1f);
                
                var rightScale = transform.localScale;
                testReport += $"\nFacing right:\n";
                testReport += $"  Scale: {rightScale}\n";
                testReport += $"  X > 0: {rightScale.x > 0} (Expected: true)\n";
                
                // Check sprites
                var sprites = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
                var visibleRight = 0;
                foreach (var sprite in sprites)
                {
                    if (sprite.enabled && sprite.sprite != null)
                    {
                        visibleRight++;
                        if (sprite.name.Contains("arm") || sprite.name.Contains("face"))
                        {
                            testReport += $"  {sprite.name}: pos={sprite.transform.localPosition}, flipX={sprite.flipX}\n";
                        }
                    }
                }
                testReport += $"  Visible sprites: {visibleRight}\n";
                
                // Test facing left
                setFacingMethod.Invoke(characterRenderer, new object[] { false });
                yield return new WaitForSeconds(0.1f);
                
                var leftScale = transform.localScale;
                testReport += $"\nFacing left:\n";
                testReport += $"  Scale: {leftScale}\n";
                testReport += $"  X < 0: {leftScale.x < 0} (Expected: true)\n";
                
                // Check sprites again
                var visibleLeft = 0;
                foreach (var sprite in sprites)
                {
                    if (sprite.enabled && sprite.sprite != null)
                    {
                        visibleLeft++;
                        if (sprite.name.Contains("arm") || sprite.name.Contains("face"))
                        {
                            testReport += $"  {sprite.name}: pos={sprite.transform.localPosition}, flipX={sprite.flipX}\n";
                        }
                    }
                }
                testReport += $"  Visible sprites: {visibleLeft}\n";
                
                // Verify scale flipping
                bool scaleFlippingWorks = rightScale.x > 0 && leftScale.x < 0;
                testReport += $"\nScale-based flipping: {(scaleFlippingWorks ? "WORKING" : "NOT WORKING")}\n";
                
                // Test animations
                testReport += "\n=== Testing Animations ===\n";
                var playAnimMethod = characterRenderer.GetType().GetMethod("PlayAnimation");
                if (playAnimMethod != null)
                {
                    string[] animations = { "stand", "walk", "crouch" };
                    foreach (var anim in animations)
                    {
                        playAnimMethod.Invoke(characterRenderer, new object[] { anim });
                        yield return new WaitForSeconds(0.2f);
                        
                        testReport += $"\nAnimation '{anim}':\n";
                        testReport += $"  Scale: {transform.localScale}\n";
                        
                        var activeSprites = 0;
                        foreach (var sprite in sprites)
                        {
                            if (sprite.enabled && sprite.sprite != null)
                            {
                                activeSprites++;
                            }
                        }
                        testReport += $"  Active sprites: {activeSprites}\n";
                    }
                }
            }
            else
            {
                testReport += "ERROR: SetFacingDirection method not found\n";
            }
            
            testReport += "\n=== Test Completed ===\n";
            SaveReport();
            
            // In editor, quit
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        
        void SaveReport()
        {
            string path = Path.Combine(Application.dataPath, "..", "runtime-scale-test-report.txt");
            File.WriteAllText(path, testReport);
            Debug.Log($"Test report saved to: {path}");
            Debug.Log(testReport);
        }
    }
}