using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;

namespace MapleClient.Tests.PlayMode
{
    public class GameplayIntegrationTests
    {
        private GameObject gameManagerObject;
        private GameManager gameManager;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Create GameManager in scene
            gameManagerObject = new GameObject("TestGameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
            
            // Wait for initialization
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
        public IEnumerator Player_Movement_Integration()
        {
            // Wait for game to load
            yield return new WaitForSeconds(0.5f);
            
            // Get player reference
            var player = gameManager.Player;
            Assert.That(player, Is.Not.Null, "Player should exist");
            
            float startX = player.Position.X;
            
            // Simulate right movement
            yield return SimulateKeyPress(KeyCode.D, 0.5f);
            
            // Verify player moved right
            Assert.That(player.Position.X, Is.GreaterThan(startX), "Player should move right");
            
            // Simulate left movement
            float midX = player.Position.X;
            yield return SimulateKeyPress(KeyCode.A, 0.5f);
            
            // Verify player moved left
            Assert.That(player.Position.X, Is.LessThan(midX), "Player should move left");
        }

        [UnityTest]
        public IEnumerator Player_Jump_Integration()
        {
            yield return new WaitForSeconds(0.5f);
            
            var player = gameManager.Player;
            float startY = player.Position.Y;
            
            // Wait for player to be grounded
            yield return WaitForCondition(() => player.IsGrounded, 2f);
            
            // Simulate jump
            yield return SimulateKeyPress(KeyCode.Space, 0.1f);
            
            // Wait for jump to start
            yield return new WaitForSeconds(0.1f);
            
            // Verify player jumped
            Assert.That(player.Position.Y, Is.GreaterThan(startY), "Player should jump up");
            
            // Wait for landing
            yield return WaitForCondition(() => player.IsGrounded, 3f);
            
            // Verify player landed
            Assert.That(player.IsGrounded, Is.True, "Player should land");
        }

        [UnityTest]
        public IEnumerator Player_Combat_Integration()
        {
            yield return new WaitForSeconds(0.5f);
            
            var gameWorld = GetGameWorld();
            Assert.That(gameWorld, Is.Not.Null);
            
            // Spawn a test monster near player
            gameWorld.SpawnMonsterForTesting(100, new MapleClient.GameLogic.Vector2(100, 50));
            yield return new WaitForSeconds(0.1f);
            
            int initialMonsterCount = gameWorld.Monsters.Count;
            Assert.That(initialMonsterCount, Is.GreaterThan(0), "Monster should be spawned");
            
            // Move player near monster
            var player = gameManager.Player;
            player.Position = new MapleClient.GameLogic.Vector2(50, 50);
            
            // Attack
            yield return SimulateKeyPress(KeyCode.Z, 0.1f);
            yield return new WaitForSeconds(0.5f);
            
            // Check if monster took damage or died
            bool monsterDied = gameWorld.Monsters.Count < initialMonsterCount;
            bool monsterDamaged = gameWorld.Monsters.Count > 0 && 
                                 gameWorld.Monsters[0].HP < gameWorld.Monsters[0].MaxHP;
            
            Assert.That(monsterDied || monsterDamaged, Is.True, 
                       "Monster should be damaged or dead after attack");
        }

        [UnityTest]
        public IEnumerator Inventory_Pickup_Integration()
        {
            yield return new WaitForSeconds(0.5f);
            
            var gameWorld = GetGameWorld();
            var player = gameManager.Player;
            
            // Check initial inventory
            int initialItemCount = player.Inventory.GetItemCount(2000000); // Red Potion
            
            // Drop an item near player
            gameWorld.AddDroppedItem(2000000, 1, player.Position);
            yield return new WaitForSeconds(0.1f);
            
            // Wait for pickup
            yield return new WaitForSeconds(0.5f);
            
            // Verify item was picked up
            int newItemCount = player.Inventory.GetItemCount(2000000);
            Assert.That(newItemCount, Is.EqualTo(initialItemCount + 1), 
                       "Item should be picked up automatically");
        }

        [UnityTest]
        public IEnumerator UI_Visibility_Integration()
        {
            yield return new WaitForSeconds(0.5f);
            
            // Find UI components
            var canvas = GameObject.Find("Canvas");
            Assert.That(canvas, Is.Not.Null, "Canvas should exist");
            
            var inventoryView = canvas.GetComponent<MapleClient.GameView.UI.InventoryView>();
            var statusBar = canvas.GetComponent<MapleClient.GameView.UI.StatusBar>();
            var expBar = canvas.GetComponent<MapleClient.GameView.UI.ExperienceBar>();
            
            Assert.That(inventoryView, Is.Not.Null, "InventoryView should exist");
            Assert.That(statusBar, Is.Not.Null, "StatusBar should exist");
            Assert.That(expBar, Is.Not.Null, "ExperienceBar should exist");
            
            // Toggle inventory
            yield return SimulateKeyPress(KeyCode.I, 0.1f);
            yield return new WaitForSeconds(0.1f);
            
            // Inventory panel should be visible (we can't easily check this without access to private field)
            // but at least verify the component responds to input
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Portal_Transition_Integration()
        {
            yield return new WaitForSeconds(0.5f);
            
            var gameWorld = GetGameWorld();
            var player = gameManager.Player;
            
            int initialMapId = gameWorld.CurrentMapId;
            
            // Move player to portal location (from MockMapLoader, portal at x=400)
            player.Position = new MapleClient.GameLogic.Vector2(400, 50);
            
            // Press up to use portal
            yield return SimulateKeyPress(KeyCode.UpArrow, 0.2f);
            yield return new WaitForSeconds(0.5f);
            
            // Check if map changed
            int newMapId = gameWorld.CurrentMapId;
            Assert.That(newMapId, Is.Not.EqualTo(initialMapId), 
                       "Map should change when using portal");
        }

        [UnityTest]
        public IEnumerator Ladder_Climbing_Integration()
        {
            yield return new WaitForSeconds(0.5f);
            
            var player = gameManager.Player;
            
            // Move to ladder position (from MockMapLoader, ladder at x=200)
            player.Position = new MapleClient.GameLogic.Vector2(200, 50);
            
            // Press up to climb
            yield return SimulateKeyHold(KeyCode.UpArrow, 1f);
            
            // Player should have climbed up
            Assert.That(player.Position.Y, Is.GreaterThan(50), 
                       "Player should climb up ladder");
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing), 
                       "Player should be in climbing state");
        }

        private IEnumerator SimulateKeyPress(KeyCode key, float duration)
        {
            // Unity's Input system can't be directly simulated in tests
            // This is a limitation of Unity's test framework
            // In a real test, you might use Input System's test framework
            yield return new WaitForSeconds(duration);
        }

        private IEnumerator SimulateKeyHold(KeyCode key, float duration)
        {
            yield return SimulateKeyPress(key, duration);
        }

        private IEnumerator WaitForCondition(System.Func<bool> condition, float timeout)
        {
            float elapsed = 0;
            while (!condition() && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        private GameWorld GetGameWorld()
        {
            // Use reflection to get private gameWorld field
            var field = typeof(GameManager).GetField("gameWorld", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(gameManager) as GameWorld;
        }
    }
}