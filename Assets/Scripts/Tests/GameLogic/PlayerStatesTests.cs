using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;

namespace MapleClient.Tests.GameLogic
{
    public class PlayerStatesTests
    {
        private Player player;

        [SetUp]
        public void Setup()
        {
            player = new Player();
        }

        [Test]
        public void Player_InitialState_IsStanding()
        {
            Assert.That(player.State, Is.EqualTo(PlayerState.Standing));
        }

        [Test]
        public void Crouch_WhenGrounded_ChangesStateToCrouching()
        {
            // Arrange
            player.IsGrounded = true;

            // Act
            player.Crouch(true);

            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Crouching));
        }

        [Test]
        public void Crouch_WhenNotGrounded_DoesNotChangeToCrouching()
        {
            // Arrange
            player.IsGrounded = false;

            // Act
            player.Crouch(true);

            // Assert
            Assert.That(player.State, Is.Not.EqualTo(PlayerState.Crouching));
        }

        [Test]
        public void StopCrouch_WhenCrouching_ReturnsToStanding()
        {
            // Arrange
            player.IsGrounded = true;
            player.Crouch(true);
            Assert.That(player.State, Is.EqualTo(PlayerState.Crouching));

            // Act
            player.Crouch(false);

            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Standing));
        }

        [Test]
        public void Movement_WhenCrouching_PreventedHorizontally()
        {
            // Arrange
            player.IsGrounded = true;
            player.Position = new Vector2(100, 100);
            player.Crouch(true);

            // Act
            player.MoveLeft(true);
            player.UpdatePhysics(0.1f, null);

            // Assert
            Assert.That(player.Position.X, Is.EqualTo(100)); // No movement
        }

        [Test]
        public void Jump_WhenCrouching_DoesNothing()
        {
            // Arrange
            player.IsGrounded = true;
            player.Crouch(true);

            // Act
            player.Jump();

            // Assert
            Assert.That(player.IsJumping, Is.False);
            Assert.That(player.Velocity.Y, Is.EqualTo(0));
        }

        [Test]
        public void ClimbLadder_WhenAtLadder_ChangesToClimbing()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 100); // At ladder position
            player.IsGrounded = true;

            // Act
            player.StartClimbing(ladderInfo);

            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing));
            Assert.That(player.IsGrounded, Is.False); // No longer affected by gravity
        }

        [Test]
        public void ClimbUp_WhenClimbing_MovesPlayerUp()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 100);
            player.StartClimbing(ladderInfo);
            var initialY = player.Position.Y;

            // Act
            player.ClimbUp(true);
            player.UpdatePhysics(0.1f, null);

            // Assert
            Assert.That(player.Position.Y, Is.GreaterThan(initialY));
        }

        [Test]
        public void ClimbDown_WhenClimbing_MovesPlayerDown()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 150);
            player.StartClimbing(ladderInfo);
            var initialY = player.Position.Y;

            // Act
            player.ClimbDown(true);
            player.UpdatePhysics(0.1f, null);

            // Assert
            Assert.That(player.Position.Y, Is.LessThan(initialY));
        }

        [Test]
        public void StopClimbing_ReturnsToNormalState()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 100);
            player.StartClimbing(ladderInfo);

            // Act
            player.StopClimbing();

            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Standing));
            Assert.That(player.IsGrounded, Is.False); // Should fall if not on ground
        }

        [Test]
        public void ClimbBeyondLadderTop_StopsAtTop()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 195); // Near top
            player.StartClimbing(ladderInfo);

            // Act
            player.ClimbUp(true);
            player.UpdatePhysics(1.0f, null); // Large delta to try going past

            // Assert
            Assert.That(player.Position.Y, Is.LessThanOrEqualTo(200));
        }

        [Test]
        public void ClimbBeyondLadderBottom_StopsAtBottom()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 55); // Near bottom
            player.StartClimbing(ladderInfo);

            // Act
            player.ClimbDown(true);
            player.UpdatePhysics(1.0f, null); // Large delta to try going past

            // Assert
            Assert.That(player.Position.Y, Is.GreaterThanOrEqualTo(50));
        }

        [Test]
        public void HorizontalInput_WhenClimbing_DoesNotMoveHorizontally()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 100);
            player.StartClimbing(ladderInfo);
            var initialX = player.Position.X;

            // Act
            player.MoveLeft(true);
            player.UpdatePhysics(0.1f, null);

            // Assert
            Assert.That(player.Position.X, Is.EqualTo(initialX));
        }

        [Test]
        public void Jump_WhenClimbing_ExitsClimbingAndJumps()
        {
            // Arrange
            var ladderInfo = new LadderInfo { X = 100, Y1 = 50, Y2 = 200 };
            player.Position = new Vector2(100, 100);
            player.StartClimbing(ladderInfo);

            // Act
            player.Jump();

            // Assert
            Assert.That(player.State, Is.Not.EqualTo(PlayerState.Climbing));
            Assert.That(player.Velocity.Y, Is.GreaterThan(0)); // Jumping velocity
        }
    }
}