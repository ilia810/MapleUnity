using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;

namespace MapleClient.Tests.PlayMode
{
    public class PerformanceTests
    {
        private GameObject gameManagerObject;
        private GameManager gameManager;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            gameManagerObject = new GameObject("TestGameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gameManagerObject != null)
            {
                Object.Destroy(gameManagerObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MapLoading_Performance()
        {
            yield return new WaitForSeconds(0.5f);
            
            var gameWorld = GetGameWorld();
            
            // Measure map loading time
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Load different map
            gameWorld.LoadMap(100000001);
            
            stopwatch.Stop();
            
            // Map loading should be fast
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500), 
                       $"Map loading took {stopwatch.ElapsedMilliseconds}ms, should be under 500ms");
            
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTest]
        public IEnumerator Multiple_Monsters_Performance()
        {
            yield return new WaitForSeconds(0.5f);
            
            var gameWorld = GetGameWorld();
            
            // Spawn many monsters
            for (int i = 0; i < 20; i++)
            {
                gameWorld.SpawnMonsterForTesting(100, 
                    new MapleClient.GameLogic.Vector2(i * 50, 50));
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // Measure frame time with many monsters
            float frameTime = 0;
            int frameCount = 0;
            
            for (int i = 0; i < 60; i++) // Test 60 frames
            {
                float start = Time.realtimeSinceStartup;
                yield return null; // Wait one frame
                frameTime += Time.realtimeSinceStartup - start;
                frameCount++;
            }
            
            float avgFrameTime = frameTime / frameCount * 1000; // Convert to ms
            
            // Average frame time should be reasonable
            Assert.That(avgFrameTime, Is.LessThan(33), 
                       $"Average frame time {avgFrameTime}ms should be under 33ms (30 FPS)");
        }

        [UnityTest]
        public IEnumerator Memory_Usage_Stability()
        {
            yield return new WaitForSeconds(0.5f);
            
            long initialMemory = System.GC.GetTotalMemory(false);
            
            var gameWorld = GetGameWorld();
            
            // Perform various operations
            for (int i = 0; i < 5; i++)
            {
                // Spawn and kill monsters
                gameWorld.SpawnMonsterForTesting(100, 
                    new MapleClient.GameLogic.Vector2(100, 50));
                yield return new WaitForSeconds(0.1f);
                
                // Drop and pickup items
                gameWorld.AddDroppedItem(2000000, 1, 
                    new MapleClient.GameLogic.Vector2(100, 50));
                yield return new WaitForSeconds(0.1f);
                
                // Change maps
                gameWorld.LoadMap(i % 2 == 0 ? 100000000 : 100000001);
                yield return new WaitForSeconds(0.1f);
            }
            
            // Force garbage collection
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            long finalMemory = System.GC.GetTotalMemory(false);
            long memoryGrowth = finalMemory - initialMemory;
            
            // Memory growth should be reasonable (less than 10MB)
            Assert.That(memoryGrowth, Is.LessThan(10 * 1024 * 1024), 
                       $"Memory grew by {memoryGrowth / 1024 / 1024}MB, should be under 10MB");
        }

        private GameWorld GetGameWorld()
        {
            var field = typeof(GameManager).GetField("gameWorld", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(gameManager) as GameWorld;
        }
    }
}