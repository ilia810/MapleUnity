using UnityEngine;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public class CharacterRenderingDebugger : MonoBehaviour
{
    private float lastLogTime = 0f;
    private const float LOG_INTERVAL = 2f; // Log every 2 seconds
    
    void Start()
    {
        Debug.Log("[CharacterRenderingDebugger] Started monitoring character rendering");
        LogCharacterState();
    }
    
    void Update()
    {
        if (Time.time - lastLogTime > LOG_INTERVAL)
        {
            LogCharacterState();
            lastLogTime = Time.time;
        }
    }
    
    void LogCharacterState()
    {
        var log = new StringBuilder();
        log.AppendLine("\n=== Character Rendering State ===");
        log.AppendLine($"Time: {Time.time:F2}");
        
        // Find MapleCharacterRenderer
        var renderer = GetComponentInChildren<MapleClient.GameView.MapleCharacterRenderer>();
        if (renderer == null)
        {
            renderer = FindObjectOfType<MapleClient.GameView.MapleCharacterRenderer>();
        }
        
        if (renderer != null)
        {
            log.AppendLine($"Found MapleCharacterRenderer on: {renderer.gameObject.name}");
            
            // Use reflection to get private fields
            var type = renderer.GetType();
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            
            // Get current head attach point
            var headAttachField = type.GetField("currentHeadAttachPoint", bindingFlags);
            if (headAttachField != null)
            {
                var headAttach = headAttachField.GetValue(renderer);
                log.AppendLine($"Current Head Attach Point: {headAttach}");
            }
            
            // Get body part renderers
            var bodyPartsField = type.GetField("bodyPartRenderers", bindingFlags);
            if (bodyPartsField != null)
            {
                var bodyParts = bodyPartsField.GetValue(renderer) as Dictionary<string, SpriteRenderer>;
                if (bodyParts != null)
                {
                    log.AppendLine($"\nBody Part Status:");
                    
                    // Check specific parts
                    string[] importantParts = { "Body", "Head", "Face", "Arm" };
                    foreach (string partName in importantParts)
                    {
                        if (bodyParts.TryGetValue(partName, out SpriteRenderer sr) && sr != null)
                        {
                            log.AppendLine($"  {partName}:");
                            log.AppendLine($"    - World Pos: {sr.transform.position}");
                            log.AppendLine($"    - Local Pos: {sr.transform.localPosition}");
                            log.AppendLine($"    - Sprite: {(sr.sprite ? sr.sprite.name : "NULL")}");
                            log.AppendLine($"    - Active: {sr.gameObject.activeInHierarchy}");
                            log.AppendLine($"    - Sorting Order: {sr.sortingOrder}");
                            
                            if (partName == "Head" && bodyParts.TryGetValue("Body", out SpriteRenderer bodyRend) && bodyRend != null)
                            {
                                var offset = sr.transform.position - bodyRend.transform.position;
                                log.AppendLine($"    - Offset from Body: {offset}");
                            }
                        }
                    }
                }
            }
            
            // Check current animation
            var currentAnimField = type.GetField("currentAnimation", bindingFlags);
            if (currentAnimField != null)
            {
                var currentAnim = currentAnimField.GetValue(renderer) as string;
                log.AppendLine($"\nCurrent Animation: {currentAnim}");
            }
            
            // Check state
            var currentStateField = type.GetField("currentState", bindingFlags);
            if (currentStateField != null)
            {
                var currentState = currentStateField.GetValue(renderer);
                log.AppendLine($"Current State: {currentState}");
            }
            
            // Check frame
            var currentFrameField = type.GetField("currentFrame", bindingFlags);
            if (currentFrameField != null)
            {
                var currentFrame = (int)currentFrameField.GetValue(renderer);
                log.AppendLine($"Current Frame: {currentFrame}");
            }
        }
        else
        {
            log.AppendLine("No MapleCharacterRenderer found!");
        }
        
        // Check PlayerView
        var playerView = GetComponent<MapleClient.GameView.PlayerView>();
        if (playerView == null)
        {
            playerView = FindObjectOfType<MapleClient.GameView.PlayerView>();
        }
        
        if (playerView != null)
        {
            log.AppendLine($"\nPlayerView:");
            log.AppendLine($"  - Position: {playerView.transform.position}");
            // PlayerView doesn't have public facingRight property
        }
        
        Debug.Log(log.ToString());
        
        // Save to file periodically
        if (Application.isBatchMode || Time.time < 5f)
        {
            string logPath = Path.Combine(Application.dataPath, "..", "character-rendering-runtime.log");
            File.AppendAllText(logPath, log.ToString());
        }
    }
}