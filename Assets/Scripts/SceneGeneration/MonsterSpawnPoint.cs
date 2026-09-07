using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
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
}
