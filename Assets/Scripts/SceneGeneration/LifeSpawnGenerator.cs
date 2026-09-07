using System.Collections.Generic;
using UnityEngine;
using GameData;
using MapleClient.GameData;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates NPC and Monster spawn points from life data
    /// </summary>
    public class LifeSpawnGenerator
    {
        public GameObject GenerateNPCs(List<LifeData> npcs, Transform parent, FootholdManager footholds = null)
        {
            GameObject npcContainer = new GameObject("NPCs");
            npcContainer.transform.parent = parent;
            
            foreach (var npc in npcs)
            {
                CreateNPC(npc, npcContainer.transform, footholds ?? parent.GetComponentInParent<FootholdManager>());
            }
            
            return npcContainer;
        }
        
        public GameObject GenerateMonsterSpawns(List<LifeData> monsters, Transform parent)
        {
            GameObject monsterContainer = new GameObject("MonsterSpawns");
            monsterContainer.transform.parent = parent;
            
            foreach (var monster in monsters)
            {
                CreateMonsterSpawn(monster, monsterContainer.transform);
            }
            
            return monsterContainer;
        }
        
        private void CreateNPC(LifeData npc, Transform parent, FootholdManager footholds)
        {
            GameObject npcObj = new GameObject($"NPC_{npc.Id}");
            npcObj.transform.parent = parent;
            
            // Stationary NPCs use the settled feet position. HeavenClient's
            // initial ground-1 spawn is followed by physics; these views are static.
            float adjustedY = npc.Y;
            if (footholds != null && footholds.TryGetGroundBelow(npc.X, npc.Y, out float groundY))
                adjustedY = groundY;
            npcObj.transform.position = CoordinateConverter.ToUnityPosition(npc.X, adjustedY, -0.5f);

            // Add NPC component
            NPCBehavior behavior = npcObj.AddComponent<NPCBehavior>();
            behavior.npcId = npc.Id;
            behavior.footholdId = npc.FH;
            // In MapleStory, F (flip) value is inverted: 0 = face right, 1 = face left
            behavior.facingDirection = npc.F == 0 ? 1 : -1; // 0 = right, 1 = left
            
            // Flip sprite based on facing direction
            if (behavior.facingDirection < 0)
            {
                npcObj.transform.localScale = new Vector3(-1, 1, 1);
            }
            
            // Add collision
            BoxCollider2D collider = npcObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 1f);
            collider.isTrigger = true;
            
            // Create sprite object (like tiles do)
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.parent = npcObj.transform;
            spriteObj.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer to the sprite object
            SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
            behavior.ApplyStageOrder(footholds);
            
            // Load NPC sprite or use placeholder
            LoadNPCSprite(npc.Id, renderer);
        }
        
        private void CreateMonsterSpawn(LifeData monster, Transform parent)
        {
            GameObject spawnObj = new GameObject($"MonsterSpawn_{monster.Id}");
            spawnObj.transform.parent = parent;
            
            // Get foothold-adjusted Y position like the C++ client does
            float adjustedY = monster.Y;
            if (FootholdManager.Instance != null)
            {
                adjustedY = FootholdManager.Instance.GetYBelow(monster.X, monster.Y);
                // GetYBelow already applies the source's one-pixel spawn offset.
            }
            
            // Set position using the foothold-adjusted Y
            Vector3 position = CoordinateConverter.ToUnityPosition(monster.X, adjustedY, -0.5f);
            spawnObj.transform.position = position;
            
            // Add spawn component
            MonsterSpawnPoint spawn = spawnObj.AddComponent<MonsterSpawnPoint>();
            spawn.monsterId = monster.Id;
            spawn.footholdId = monster.FH;
            spawn.spawnTime = monster.MobTime;
            spawn.facingDirection = monster.F == 0 ? -1 : 1;
            
            // Set spawn area
            if (monster.RX0 != 0 || monster.RX1 != 0)
            {
                spawn.hasSpawnArea = true;
                spawn.spawnAreaMin = CoordinateConverter.ToUnityPosition(monster.RX0, 0).x;
                spawn.spawnAreaMax = CoordinateConverter.ToUnityPosition(monster.RX1, 0).x;
            }
            
            // Add gizmo for editor visualization
            #if UNITY_EDITOR
            spawnObj.AddComponent<MonsterSpawnGizmo>();
            #endif
        }
        
        internal static void LoadNPCSprite(string npcId, SpriteRenderer renderer)
        {
            // Try to load NPC sprite from NX data
            var nxManager = NXDataManagerSingleton.Instance;
            var (npcSprite, origin) = nxManager.GetNPCSpriteWithOrigin(npcId);
            
            if (npcSprite != null)
            {
                renderer.sprite = npcSprite;
                renderer.color = Color.white;
                
                // Apply origin offset - use the SAME logic as tiles and objects
                // C++ client: draws at pos - origin
                float offsetX = -origin.x / 100f;  // Move left by origin.x
                float offsetY = origin.y / 100f;   // Move up by origin.y (inverted due to coordinate flip)
                
                renderer.transform.localPosition = new Vector3(offsetX, offsetY, 0);
                
                Debug.Log($"NPC {npcId}: origin({origin.x},{origin.y}), offset=({offsetX},{offsetY})");
            }
            else
            {
                // Use placeholder - green square
                renderer.color = new Color(0, 1f, 0, 0.8f);
                Debug.LogWarning($"NPC sprite not found for NPC ID: {npcId}");
            }
        }
    }
    
    /// <summary>
    /// NPC behavior component
    /// </summary>

    
    /// <summary>
    /// Monster spawn point component
    /// </summary>

    
    #if UNITY_EDITOR
    /// <summary>
    /// Gizmo for monster spawn visualization
    /// </summary>

    #endif
}
