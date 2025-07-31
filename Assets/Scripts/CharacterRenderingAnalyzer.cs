using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class CharacterRenderingAnalyzer : MonoBehaviour
{
    private bool hasAnalyzed = false;
    
    void Start()
    {
        AnalyzeCharacterRendering();
    }
    
    void Update()
    {
        if (!hasAnalyzed && Time.time > 1f)
        {
            AnalyzeCharacterRendering();
            hasAnalyzed = true;
        }
    }
    
    void AnalyzeCharacterRendering()
    {
        var log = new StringBuilder();
        log.AppendLine("=== Character Rendering Analysis ===");
        log.AppendLine($"Time: {System.DateTime.Now}");
        log.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        
        // Find player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            // Try to find any object with PlayerView
            var playerView = FindObjectOfType<MapleClient.GameView.PlayerView>();
            if (playerView != null)
            {
                player = playerView.gameObject;
            }
        }
        
        if (player != null)
        {
            log.AppendLine($"\nPlayer Found: {player.name}");
            log.AppendLine($"Position: {player.transform.position}");
            log.AppendLine($"Rotation: {player.transform.rotation.eulerAngles}");
            log.AppendLine($"Scale: {player.transform.localScale}");
            
            // Check PlayerView
            var pv = player.GetComponent<MapleClient.GameView.PlayerView>();
            if (pv != null)
            {
                log.AppendLine($"\nPlayerView found");
                log.AppendLine($"  GameObject active: {pv.gameObject.activeInHierarchy}");
                log.AppendLine($"  Transform position: {pv.transform.position}");
            }
            
            // Find all sprite renderers
            var allSprites = player.GetComponentsInChildren<SpriteRenderer>(true);
            log.AppendLine($"\nSprite Renderers ({allSprites.Length} total):");
            
            // Group by body part type
            var headSprites = new List<SpriteRenderer>();
            var faceSprites = new List<SpriteRenderer>();
            var bodySprites = new List<SpriteRenderer>();
            var armSprites = new List<SpriteRenderer>();
            var footSprites = new List<SpriteRenderer>();
            var otherSprites = new List<SpriteRenderer>();
            
            foreach (var sr in allSprites)
            {
                var nameLower = sr.name.ToLower();
                if (nameLower.Contains("head"))
                    headSprites.Add(sr);
                else if (nameLower.Contains("face"))
                    faceSprites.Add(sr);
                else if (nameLower.Contains("body"))
                    bodySprites.Add(sr);
                else if (nameLower.Contains("arm"))
                    armSprites.Add(sr);
                else if (nameLower.Contains("foot") || nameLower.Contains("feet"))
                    footSprites.Add(sr);
                else
                    otherSprites.Add(sr);
            }
            
            // Analyze each group
            log.AppendLine($"\n--- HEAD SPRITES ({headSprites.Count}) ---");
            foreach (var sr in headSprites)
            {
                LogSpriteDetails(sr, log);
            }
            
            log.AppendLine($"\n--- FACE SPRITES ({faceSprites.Count}) ---");
            foreach (var sr in faceSprites)
            {
                LogSpriteDetails(sr, log);
            }
            
            log.AppendLine($"\n--- BODY SPRITES ({bodySprites.Count}) ---");
            foreach (var sr in bodySprites)
            {
                LogSpriteDetails(sr, log);
            }
            
            log.AppendLine($"\n--- ARM SPRITES ({armSprites.Count}) ---");
            foreach (var sr in armSprites)
            {
                LogSpriteDetails(sr, log);
            }
            
            log.AppendLine($"\n--- FOOT SPRITES ({footSprites.Count}) ---");
            foreach (var sr in footSprites)
            {
                LogSpriteDetails(sr, log);
            }
            
            log.AppendLine($"\n--- OTHER SPRITES ({otherSprites.Count}) ---");
            foreach (var sr in otherSprites)
            {
                LogSpriteDetails(sr, log);
            }
            
            // Check MapleCharacterRenderer
            var mcr = player.GetComponentInChildren<MapleClient.GameView.MapleCharacterRenderer>();
            if (mcr != null)
            {
                log.AppendLine($"\n--- MapleCharacterRenderer ---");
                log.AppendLine($"GameObject: {mcr.gameObject.name}");
                log.AppendLine($"Enabled: {mcr.enabled}");
                log.AppendLine($"Transform: {mcr.transform.position}");
            }
            
            // Check relative positions
            if (bodySprites.Count > 0 && headSprites.Count > 0)
            {
                var body = bodySprites[0];
                var head = headSprites[0];
                var relPos = head.transform.position - body.transform.position;
                log.AppendLine($"\n--- RELATIVE POSITIONS ---");
                log.AppendLine($"Head to Body offset: {relPos}");
                log.AppendLine($"Distance: {relPos.magnitude}");
            }
        }
        else
        {
            log.AppendLine("\nERROR: No player found!");
        }
        
        // Write to file
        string logPath = Path.Combine(Application.dataPath, "..", "character-rendering-analysis.log");
        File.WriteAllText(logPath, log.ToString());
        Debug.Log($"Character analysis saved to: {logPath}");
        Debug.Log(log.ToString());
        
        // Exit if in batch mode
        if (Application.isBatchMode)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
            #else
            Application.Quit();
            #endif
        }
    }
    
    void LogSpriteDetails(SpriteRenderer sr, StringBuilder log)
    {
        log.AppendLine($"  {sr.name}:");
        log.AppendLine($"    Active: {sr.gameObject.activeInHierarchy}");
        log.AppendLine($"    Enabled: {sr.enabled}");
        log.AppendLine($"    World Pos: {sr.transform.position}");
        log.AppendLine($"    Local Pos: {sr.transform.localPosition}");
        log.AppendLine($"    Rotation: {sr.transform.rotation.eulerAngles}");
        log.AppendLine($"    Scale: {sr.transform.localScale}");
        log.AppendLine($"    Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}");
        log.AppendLine($"    Order: {sr.sortingOrder}");
        log.AppendLine($"    FlipX: {sr.flipX}");
        log.AppendLine($"    Color: {sr.color}");
        
        if (sr.transform.parent != null)
        {
            log.AppendLine($"    Parent: {sr.transform.parent.name}");
        }
    }
}