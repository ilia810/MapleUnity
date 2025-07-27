using UnityEngine;
using UnityEditor;
using MapleClient.SceneGeneration;
using System.Linq;

namespace MapleClient.Editor
{
    public class TestNPCAlignment
    {
        [MenuItem("MapleUnity/Test/Debug NPC Alignment")]
        public static void DebugNPCAlignment()
        {
            Debug.Log("=== DEBUGGING NPC ALIGNMENT ===");
            
            var npcs = Object.FindObjectsOfType<NPCBehavior>();
            Debug.Log($"Found {npcs.Length} NPCs in scene");
            
            foreach (var npc in npcs)
            {
                Debug.Log($"\nNPC {npc.npcId}:");
                Debug.Log($"  Position: {npc.transform.position}");
                Debug.Log($"  Foothold ID: {npc.footholdId}");
                
                // Check sprite renderer
                var renderer = npc.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    var sprite = renderer.sprite;
                    Debug.Log($"  Sprite: {sprite.name}");
                    Debug.Log($"  Sprite size: {sprite.rect.width}x{sprite.rect.height} pixels");
                    Debug.Log($"  Sprite bounds: {sprite.bounds.size} units");
                    Debug.Log($"  Sprite pivot: {sprite.pivot} (normalized: {sprite.pivot.x/sprite.rect.width:F2},{sprite.pivot.y/sprite.rect.height:F2})");
                    Debug.Log($"  Renderer offset: {renderer.transform.localPosition}");
                    
                    // Calculate where the bottom of the sprite actually is
                    Vector3 npcWorldPos = npc.transform.position;
                    Vector3 spriteOffset = renderer.transform.localPosition;
                    float spriteHeight = sprite.bounds.size.y;
                    
                    // With top-left pivot, the bottom of the sprite is at:
                    float spriteBottomY = npcWorldPos.y + spriteOffset.y - spriteHeight;
                    Debug.Log($"  Sprite bottom Y: {spriteBottomY:F2}");
                    
                    // Check foothold Y at this position
                    if (FootholdManager.Instance != null)
                    {
                        float footholdY = FootholdManager.Instance.GetYBelow(npcWorldPos.x, npcWorldPos.y + 10f);
                        Debug.Log($"  Foothold Y: {footholdY:F2}");
                        Debug.Log($"  NPC Y position: {npcWorldPos.y:F2}");
                        
                        // Expected: NPC Y should be foothold Y - 1 pixel (0.01 units)
                        float expectedNpcY = CoordinateConverter.ToUnityPosition(0, footholdY - 1, 0).y;
                        float positionDiff = npcWorldPos.y - expectedNpcY;
                        
                        if (Mathf.Abs(positionDiff) > 0.01f)
                        {
                            Debug.LogWarning($"  POSITION ERROR: NPC Y is off by {positionDiff:F2} units");
                        }
                        
                        // Check sprite bottom
                        float gap = spriteBottomY - (footholdY * 0.01f); // Convert foothold to Unity units
                        if (Mathf.Abs(gap) > 0.02f) // Allow 2 pixel tolerance
                        {
                            Debug.LogWarning($"  SPRITE GAP: Bottom is {gap:F2} units {(gap > 0 ? "above" : "below")} the foothold!");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"  No sprite renderer found!");
                }
            }
        }
        
        [MenuItem("MapleUnity/Test/Fix NPC Alignment")]
        public static void FixNPCAlignment()
        {
            Debug.Log("=== FIXING NPC ALIGNMENT ===");
            
            var npcs = Object.FindObjectsOfType<NPCBehavior>();
            int fixedCount = 0;
            
            foreach (var npc in npcs)
            {
                var renderer = npc.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null && FootholdManager.Instance != null)
                {
                    var sprite = renderer.sprite;
                    Vector3 npcPos = npc.transform.position;
                    Vector3 spriteOffset = renderer.transform.localPosition;
                    float spriteHeight = sprite.bounds.size.y;
                    
                    // Calculate current bottom position
                    float currentBottomY = npcPos.y + spriteOffset.y - spriteHeight;
                    
                    // Get foothold Y
                    float footholdY = FootholdManager.Instance.GetYBelow(npcPos.x, npcPos.y + 10f);
                    
                    // Calculate adjustment needed
                    float adjustment = footholdY - currentBottomY;
                    
                    if (Mathf.Abs(adjustment) > 0.01f)
                    {
                        // Adjust NPC position
                        npc.transform.position = new Vector3(npcPos.x, npcPos.y + adjustment, npcPos.z);
                        Debug.Log($"Adjusted NPC {npc.npcId} by {adjustment:F2} units");
                        fixedCount++;
                    }
                }
            }
            
            Debug.Log($"Fixed {fixedCount} NPCs");
            
            if (fixedCount > 0)
            {
                EditorUtility.SetDirty(Object.FindObjectOfType<NPCBehavior>().transform.root.gameObject);
            }
        }
    }
}