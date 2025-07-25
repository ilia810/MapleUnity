using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class MonsterTests
    {
        private Monster monster;
        private MonsterTemplate testTemplate;

        [SetUp]
        public void Setup()
        {
            testTemplate = new MonsterTemplate
            {
                MonsterId = 100100,
                Name = "Snail",
                MaxHP = 100,
                MaxMP = 0,
                Level = 1,
                Exp = 10,
                PhysicalDamage = 15,
                MagicDamage = 0,
                PhysicalDefense = 5,
                MagicDefense = 5,
                Accuracy = 10,
                Avoidability = 5,
                Speed = 20
            };
            
            monster = new Monster(testTemplate, new Vector2(100, 0));
        }

        [Test]
        public void Monster_InitializedWithTemplate_HasCorrectStats()
        {
            Assert.That(monster.MonsterId, Is.EqualTo(100100));
            Assert.That(monster.Name, Is.EqualTo("Snail"));
            Assert.That(monster.HP, Is.EqualTo(100));
            Assert.That(monster.MaxHP, Is.EqualTo(100));
            Assert.That(monster.Position.X, Is.EqualTo(100));
            Assert.That(monster.Position.Y, Is.EqualTo(0));
            Assert.That(monster.IsDead, Is.False);
        }

        [Test]
        public void TakeDamage_ReducesHP()
        {
            // Act
            monster.TakeDamage(30);

            // Assert
            Assert.That(monster.HP, Is.EqualTo(70));
            Assert.That(monster.IsDead, Is.False);
        }

        [Test]
        public void TakeDamage_WhenDamageExceedsHP_Dies()
        {
            // Act
            monster.TakeDamage(150);

            // Assert
            Assert.That(monster.HP, Is.EqualTo(0));
            Assert.That(monster.IsDead, Is.True);
        }

        [Test]
        public void TakeDamage_WhenAlreadyDead_DoesNothing()
        {
            // Arrange
            monster.TakeDamage(150); // Kill it
            
            // Act
            monster.TakeDamage(50); // Try to damage again

            // Assert
            Assert.That(monster.HP, Is.EqualTo(0));
            Assert.That(monster.IsDead, Is.True);
        }

        [Test]
        public void Update_WhenAlive_CanMove()
        {
            // Arrange
            var initialX = monster.Position.X;
            monster.SetMovementPattern(MovementPattern.Patrol, 50);

            // Act
            monster.Update(1.0f);

            // Assert
            Assert.That(monster.Position.X, Is.Not.EqualTo(initialX));
        }

        [Test]
        public void Update_WhenDead_DoesNotMove()
        {
            // Arrange
            monster.TakeDamage(150); // Kill it
            var deathPosition = monster.Position;
            monster.SetMovementPattern(MovementPattern.Patrol, 50);

            // Act
            monster.Update(1.0f);

            // Assert
            Assert.That(monster.Position, Is.EqualTo(deathPosition));
        }
    }
}