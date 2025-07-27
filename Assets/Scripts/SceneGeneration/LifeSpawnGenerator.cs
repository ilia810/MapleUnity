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
        public GameObject GenerateNPCs(List<LifeData> npcs, Transform parent)
        {
            GameObject npcContainer = new GameObject("NPCs");
            npcContainer.transform.parent = parent;
            
            foreach (var npc in npcs)
            {
                CreateNPC(npc, npcContainer.transform);
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
        
        private void CreateNPC(LifeData npc, Transform parent)
        {
            GameObject npcObj = new GameObject($"NPC_{npc.Id}");
            npcObj.transform.parent = parent;
            
            // Get foothold-adjusted Y position like the C++ client does
            float adjustedY = npc.Y;
            if (FootholdManager.Instance != null)
            {
                adjustedY = FootholdManager.Instance.GetYBelow(npc.X, npc.Y);
                // C++ client subtracts 1 pixel from foothold Y
                adjustedY -= 1;
            }
            
            // Set position using the foothold-adjusted Y
            Vector3 position = CoordinateConverter.ToUnityPosition(npc.X, adjustedY, -0.5f);
            npcObj.transform.position = position;
            
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
            renderer.sortingLayerName = "NPCs";
            // Use Y position for sorting order (higher Y = further back)
            renderer.sortingOrder = -Mathf.RoundToInt(npc.Y);
            
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
                // C++ client subtracts 1 pixel from foothold Y
                adjustedY -= 1;
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
        
        private void LoadNPCSprite(string npcId, SpriteRenderer renderer)
        {
            // Try to load NPC sprite from NX data
            var nxManager = NXDataManagerSingleton.Instance;
            var (npcSprite, origin) = nxManager.GetNPCSpriteWithOrigin(npcId);
            
            if (npcSprite != null)
            {
                renderer.sprite = npcSprite;
                
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
    public class NPCBehavior : MonoBehaviour
    {
        public string npcId;
        public int footholdId;
        public int facingDirection = 1;
        
        private bool playerInRange = false;
        
        private void Start()
        {
            // Load NPC data and sprite
            LoadNPCData();
        }
        
        private void LoadNPCData()
        {
            // TODO: Load NPC sprite and data from NX files
            // For now, just log
            Debug.Log($"Loading NPC {npcId}");
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = true;
                // TODO: Show interaction prompt
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                // TODO: Hide interaction prompt
            }
        }
        
        private void Update()
        {
            if (playerInRange && Input.GetKeyDown(KeyCode.Space))
            {
                // TODO: Open NPC dialog
                Debug.Log($"Interacting with NPC {npcId}");
            }
        }
    }
    
    /// <summary>
    /// Monster spawn point component
    /// </summary>
    public class MonsterSpawnPoint : MonoBehaviour
    {
        public string monsterId;
        public int footholdId;
        public int spawnTime = 30; // seconds
        public int facingDirection = 1;
        public bool hasSpawnArea = false;
        public float spawnAreaMin;
        public float spawnAreaMax;
        
        private float nextSpawnTime;
        private GameObject currentMonster;
        
        private void Start()
        {
            // Schedule first spawn
            nextSpawnTime = Time.time + spawnTime;
        }
        
        private void Update()
        {
            // Check if we need to spawn
            if (currentMonster == null && Time.time >= nextSpawnTime)
            {
                SpawnMonster();
                nextSpawnTime = Time.time + spawnTime;
            }
        }
        
        private void SpawnMonster()
        {
            // TODO: Actually spawn monster prefab
            Debug.Log($"Spawning monster {monsterId} at {transform.position}");
            
            // Determine spawn position
            Vector3 spawnPos = transform.position;
            if (hasSpawnArea)
            {
                float x = Random.Range(spawnAreaMin, spawnAreaMax);
                spawnPos.x = x;
            }
            
            // Create placeholder
            GameObject monster = new GameObject($"Monster_{monsterId}");
            monster.transform.position = spawnPos;
            
            // Track spawned monster
            currentMonster = monster;
            
            // Set facing direction
            if (facingDirection < 0)
            {
                monster.transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Gizmo for monster spawn visualization
    /// </summary>
    public class MonsterSpawnGizmo : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            var spawn = GetComponent<MonsterSpawnPoint>();
            if (spawn == null) return;
            
            // Draw spawn point
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw spawn area if exists
            if (spawn.hasSpawnArea)
            {
                Gizmos.color = new Color(1f, 0, 0, 0.3f);
                Vector3 min = new Vector3(spawn.spawnAreaMin, transform.position.y - 0.5f, transform.position.z);
                Vector3 max = new Vector3(spawn.spawnAreaMax, transform.position.y + 0.5f, transform.position.z);
                Vector3 size = max - min;
                Vector3 center = (min + max) * 0.5f;
                Gizmos.DrawCube(center, size);
            }
            
            // Draw facing direction
            Gizmos.color = Color.yellow;
            Vector3 dir = spawn.facingDirection > 0 ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(transform.position, transform.position + dir * 0.5f);
        }
    }
    #endif
}