using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Tests.Fakes;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class CombatTests
    {
        private Player player;
        private List<Monster> monsters;
        private GameWorld gameWorld;
        private FakeMapLoader mapLoader;

        [SetUp]
        public void Setup()
        {
            player = new Player();
            player.Position = new Vector2(0, 0);
            
            monsters = new List<Monster>();
            
            // Create test monsters
            var snailTemplate = new MonsterTemplate
            {
                MonsterId = 100100,
                Name = "Snail",
                MaxHP = 100,
                PhysicalDefense = 5
            };
            
            // Monster in range
            monsters.Add(new Monster(snailTemplate, new Vector2(50, 0)));
            
            // Monster out of range
            monsters.Add(new Monster(snailTemplate, new Vector2(200, 0)));
            
            // Set up game world
            mapLoader = new FakeMapLoader();
            var testMap = new MapData { MapId = 100000000 };
            mapLoader.AddMap(100000000, testMap);
            
            gameWorld = new GameWorld(new FakeInputProvider(), mapLoader);
            gameWorld.LoadMap(100000000);
        }

        [Test]
        public void Player_BasicAttack_DamagesMonsterInRange()
        {
            // Arrange
            var monsterInRange = monsters[0];
            var initialHP = monsterInRange.HP;
            
            // Act
            var combat = new Combat();
            var hitMonsters = combat.PerformBasicAttack(player, monsters, 100); // 100 pixel range
            
            // Assert
            Assert.That(hitMonsters.Count, Is.EqualTo(1));
            Assert.That(hitMonsters[0], Is.EqualTo(monsterInRange));
            Assert.That(monsterInRange.HP, Is.LessThan(initialHP));
        }

        [Test]
        public void Player_BasicAttack_DoesNotDamageMonsterOutOfRange()
        {
            // Arrange
            var monsterOutOfRange = monsters[1];
            var initialHP = monsterOutOfRange.HP;
            
            // Act
            var combat = new Combat();
            var hitMonsters = combat.PerformBasicAttack(player, monsters, 100); // 100 pixel range
            
            // Assert
            Assert.That(monsterOutOfRange.HP, Is.EqualTo(initialHP));
        }

        [Test]
        public void Player_BasicAttack_CanKillMonster()
        {
            // Arrange
            var monster = monsters[0];
            monster.TakeDamage(90); // Reduce to 10 HP
            
            // Act
            var combat = new Combat();
            combat.PerformBasicAttack(player, monsters, 100);
            
            // Assert
            Assert.That(monster.IsDead, Is.True);
            Assert.That(monster.HP, Is.EqualTo(0));
        }

        [Test]
        public void DamageCalculation_UsesPlayerStats()
        {
            // Arrange
            player.SetBaseDamage(50);
            
            // Act
            var combat = new Combat();
            var damage = combat.CalculatePhysicalDamage(player, monsters[0]);
            
            // Assert
            Assert.That(damage, Is.GreaterThan(0));
            Assert.That(damage, Is.LessThanOrEqualTo(100)); // Max damage with variance
        }

        [Test]
        public void Player_Attack_TriggersCooldown()
        {
            // Arrange
            var combat = new Combat();
            
            // Act
            var canAttack1 = combat.CanPlayerAttack(player);
            combat.PerformBasicAttack(player, monsters, 100);
            var canAttack2 = combat.CanPlayerAttack(player);
            
            // Assert
            Assert.That(canAttack1, Is.True);
            Assert.That(canAttack2, Is.False); // Should be on cooldown
        }
    }
}