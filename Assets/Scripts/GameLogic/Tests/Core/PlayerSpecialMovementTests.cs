using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Tests.Core
{
    [TestFixture]
    public class PlayerSpecialMovementTests
    {
        private Player player;
        private MapData mapData;

        [SetUp]
        public void SetUp()
        {
            player = new Player();
            mapData = new MapData();
            mapData.Ladders = new List<LadderInfo>();
            mapData.Platforms = new List<Platform>();
        }

        #region Ladder/Rope Climbing Tests

        [Test]
        public void Player_NearLadder_UpKeyStartsClimbing()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            mapData.Ladders.Add(ladder);
            player.Position = new Vector2(5f, 2f); // On ladder
            player.IsGrounded = true;

            // Act
            player.TryStartClimbing(mapData, true); // Up pressed

            // Assert
            Assert.AreEqual(PlayerState.Climbing, player.State);
            Assert.AreEqual(5f, player.Position.X); // Snapped to ladder X
            Assert.IsFalse(player.IsGrounded);
            Assert.AreEqual(Vector2.Zero, player.Velocity);
        }

        [Test]
        public void Player_NearLadder_DownKeyStartsClimbing()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            mapData.Ladders.Add(ladder);
            player.Position = new Vector2(5f, 2f); // On ladder
            player.IsGrounded = true;

            // Act
            player.TryStartClimbing(mapData, false); // Down pressed

            // Assert
            Assert.AreEqual(PlayerState.Climbing, player.State);
        }

        [Test]
        public void Player_NotNearLadder_UpKeyDoesNotStartClimbing()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            mapData.Ladders.Add(ladder);
            player.Position = new Vector2(20f, 2f); // Far from ladder
            player.IsGrounded = true;

            // Act
            player.TryStartClimbing(mapData, true);

            // Assert
            Assert.AreNotEqual(PlayerState.Climbing, player.State);
        }

        [Test]
        public void Player_Climbing_GravityDisabled()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            player.StartClimbing(ladder);

            // Assert
            Assert.IsFalse(player.UseGravity);
        }

        [Test]
        public void Player_Climbing_UpKeyMovesUp()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            player.StartClimbing(ladder);
            player.Position = new Vector2(5f, 5f);

            // Act
            player.ClimbUp(true);

            // Assert
            Assert.AreEqual(MaplePhysics.ClimbSpeed, player.Velocity.Y);
            Assert.AreEqual(0f, player.Velocity.X);
        }

        [Test]
        public void Player_Climbing_DownKeyMovesDown()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            player.StartClimbing(ladder);
            player.Position = new Vector2(5f, 5f);

            // Act
            player.ClimbDown(true);

            // Assert
            Assert.AreEqual(-MaplePhysics.ClimbSpeed, player.Velocity.Y);
            Assert.AreEqual(0f, player.Velocity.X);
        }

        [Test]
        public void Player_Climbing_JumpOffLadderHorizontally()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            player.StartClimbing(ladder);
            player.Position = new Vector2(5f, 5f);
            player.MoveLeft(true); // Holding left

            // Act
            player.Jump();

            // Assert
            Assert.AreEqual(PlayerState.Jumping, player.State);
            Assert.IsTrue(player.Velocity.Y > 0); // Jumping up
            Assert.IsTrue(player.Velocity.X < 0); // Moving left
            Assert.IsNull(player.GetCurrentLadder());
        }

        [Test]
        public void Player_Climbing_ReleaseBothKeysStopsMovement()
        {
            // Arrange
            var ladder = new LadderInfo { X = 5f, Y1 = 0f, Y2 = 10f };
            player.StartClimbing(ladder);
            player.ClimbUp(true);

            // Act
            player.ClimbUp(false);
            player.ClimbDown(false);

            // Assert
            Assert.AreEqual(Vector2.Zero, player.Velocity);
        }

        #endregion

        #region Double Jump Tests

        [Test]
        public void Player_WithDoubleJumpSkill_CanDoubleJump()
        {
            // Arrange
            player.IsGrounded = true;
            player.EnableDoubleJump(true); // Enable double jump skill

            // Act - First jump
            player.Jump();
            player.ReleaseJump();
            player.UpdatePhysics(0.1f, mapData); // Let player go airborne
            
            // Act - Second jump
            player.Jump();

            // Assert
            Assert.AreEqual(PlayerState.DoubleJumping, player.State);
            Assert.IsTrue(player.Velocity.Y > 0);
            Assert.AreEqual(1, player.GetJumpCount()); // Used double jump
        }

        [Test]
        public void Player_WithoutDoubleJumpSkill_CannotDoubleJump()
        {
            // Arrange
            player.IsGrounded = true;
            player.EnableDoubleJump(false); // No double jump skill

            // Act - First jump
            player.Jump();
            player.ReleaseJump();
            player.UpdatePhysics(0.1f, mapData); // Let player go airborne
            
            // Act - Try second jump
            player.Jump();

            // Assert
            Assert.AreEqual(PlayerState.Jumping, player.State); // Still in first jump
            Assert.AreEqual(0, player.GetJumpCount()); // No double jump used
        }

        [Test]
        public void Player_DoubleJump_HasReducedPower()
        {
            // Arrange
            player.IsGrounded = true;
            player.EnableDoubleJump(true);
            
            // Act - First jump
            player.Jump();
            float firstJumpVelocity = player.Velocity.Y;
            player.ReleaseJump();
            player.UpdatePhysics(0.1f, mapData);
            
            // Act - Second jump
            player.Jump();
            float secondJumpVelocity = player.Velocity.Y;

            // Assert
            Assert.AreEqual(firstJumpVelocity * MaplePhysics.DoubleJumpModifier, 
                           secondJumpVelocity, 0.01f);
        }

        [Test]
        public void Player_Landing_ResetsDoubleJump()
        {
            // Arrange
            player.EnableDoubleJump(true);
            player.IsGrounded = false;
            player.SetJumpCount(1); // Used double jump

            // Act - Land on platform
            var platform = new Platform(0, 0, 100, 0);
            mapData.Platforms.Add(platform);
            player.Position = new Vector2(50f, 1f);
            player.Velocity = new Vector2(0, -1f);
            player.UpdatePhysics(0.016f, mapData);

            // Assert
            Assert.IsTrue(player.IsGrounded);
            Assert.AreEqual(0, player.GetJumpCount()); // Reset
        }

        #endregion

        #region Flash Jump Tests

        [Test]
        public void Player_WithFlashJump_CanFlashJump()
        {
            // Arrange
            player.IsGrounded = false; // In air
            player.EnableFlashJump(true);
            player.MoveRight(true); // Direction for flash jump

            // Act
            player.FlashJump();

            // Assert
            Assert.AreEqual(PlayerState.FlashJumping, player.State);
            Assert.IsTrue(player.Velocity.X > player.GetWalkSpeed()); // Boosted horizontal speed
            Assert.IsTrue(player.Velocity.Y > 0); // Small upward boost
        }

        [Test]
        public void Player_FlashJump_TeleportsHorizontally()
        {
            // Arrange
            player.Position = new Vector2(5f, 5f);
            player.IsGrounded = false;
            player.EnableFlashJump(true);
            player.MoveRight(true);
            float startX = player.Position.X;

            // Act
            player.FlashJump();

            // Assert
            Assert.IsTrue(player.Position.X > startX + 1f); // Teleported right
            Assert.AreEqual(PlayerState.FlashJumping, player.State);
        }

        [Test]
        public void Player_FlashJump_RequiresDirection()
        {
            // Arrange
            player.IsGrounded = false;
            player.EnableFlashJump(true);
            // No direction input

            // Act
            player.FlashJump();

            // Assert
            Assert.AreNotEqual(PlayerState.FlashJumping, player.State);
        }

        [Test]
        public void Player_FlashJump_HasCooldown()
        {
            // Arrange
            player.IsGrounded = false;
            player.EnableFlashJump(true);
            player.MoveRight(true);

            // Act - First flash jump
            player.FlashJump();
            Assert.AreEqual(PlayerState.FlashJumping, player.State);

            // Act - Try immediate second flash jump
            player.FlashJump();

            // Assert - Should fail due to cooldown
            Assert.IsFalse(player.CanFlashJump());
        }

        #endregion

        #region Environmental Effects Tests

        [Test]
        public void Player_OnIce_HasReducedFriction()
        {
            // Arrange
            var icePlatform = new Platform(0, 0, 100, 0) { IsSlippery = true };
            mapData.Platforms.Add(icePlatform);
            player.Position = new Vector2(50f, 0.5f);
            player.IsGrounded = true;
            player.Velocity = new Vector2(2f, 0); // Moving right

            // Act - Stop input but should slide
            player.MoveRight(false);
            float velocityBefore = player.Velocity.X;
            player.UpdatePhysics(0.1f, mapData);
            float velocityAfter = player.Velocity.X;

            // Assert - Velocity decreases slower than normal
            float velocityChange = velocityBefore - velocityAfter;
            Assert.IsTrue(velocityChange < MaplePhysics.WalkFriction * 0.1f * 0.5f); // Less friction
            Assert.IsTrue(velocityAfter > 0); // Still sliding
        }

        [Test]
        public void Player_OnConveyorBelt_GetsAdditionalVelocity()
        {
            // Arrange
            var conveyorPlatform = new Platform(0, 0, 100, 0) 
            { 
                IsConveyor = true,
                ConveyorSpeed = 1f // Moving right at 1 unit/s
            };
            mapData.Platforms.Add(conveyorPlatform);
            player.Position = new Vector2(50f, 0.5f);
            player.IsGrounded = true;
            player.Velocity = new Vector2(0, 0); // Not moving

            // Act
            player.UpdatePhysics(0.1f, mapData);

            // Assert
            Assert.AreEqual(1f, player.Velocity.X, 0.1f); // Moving with conveyor
        }

        [Test]
        public void Player_Swimming_HasDifferentPhysics()
        {
            // Arrange
            mapData.IsUnderwater = true;
            player.Position = new Vector2(5f, 5f);
            player.IsGrounded = false;

            // Act - Jump underwater
            player.Jump();
            float underwaterJumpVelocity = player.Velocity.Y;

            // Reset and test normal jump
            mapData.IsUnderwater = false;
            player.Velocity = Vector2.Zero;
            player.IsGrounded = true;
            player.Jump();
            float normalJumpVelocity = player.Velocity.Y;

            // Assert
            Assert.AreEqual(MaplePhysics.SwimJump / 100f, underwaterJumpVelocity, 0.01f);
            Assert.IsTrue(underwaterJumpVelocity < normalJumpVelocity);
        }

        [Test]
        public void Player_Swimming_HasSlowerGravity()
        {
            // Arrange
            mapData.IsUnderwater = true;
            player.Position = new Vector2(5f, 5f);
            player.IsGrounded = false;
            player.Velocity = new Vector2(0, 0);

            // Act
            player.UpdatePhysics(0.1f, mapData);

            // Assert
            float expectedVelocity = -MaplePhysics.SwimGravity / 100f * 0.1f;
            Assert.AreEqual(expectedVelocity, player.Velocity.Y, 0.01f);
        }

        #endregion

        #region Movement Modifier Tests

        [Test]
        public void Player_WithSpeedBoost_MoveFaster()
        {
            // Arrange
            player.IsGrounded = true;
            var speedModifier = new SpeedModifier(1.5f, 10f); // 50% speed boost for 10 seconds
            player.AddMovementModifier(speedModifier);

            // Act
            player.MoveRight(true);
            player.UpdatePhysics(0.016f, mapData);

            // Assert
            float expectedSpeed = player.GetWalkSpeed() * 1.5f;
            Assert.AreEqual(expectedSpeed, player.GetModifiedWalkSpeed(), 0.01f);
        }

        [Test]
        public void Player_ModifierExpires_SpeedReturnsToNormal()
        {
            // Arrange
            player.IsGrounded = true;
            var speedModifier = new SpeedModifier(1.5f, 0.1f); // Short duration
            player.AddMovementModifier(speedModifier);

            // Act - Let modifier expire
            player.UpdateMovementModifiers(0.2f);

            // Assert
            Assert.AreEqual(player.GetWalkSpeed(), player.GetModifiedWalkSpeed());
            Assert.IsFalse(player.HasActiveMovementModifier());
        }

        [Test]
        public void Player_MultipleModifiers_Stack()
        {
            // Arrange
            player.IsGrounded = true;
            var speedBoost = new SpeedModifier(1.5f, 10f); // 50% boost
            var speedBoost2 = new SpeedModifier(1.2f, 10f); // 20% boost
            player.AddMovementModifier(speedBoost);
            player.AddMovementModifier(speedBoost2);

            // Assert
            float expectedSpeed = player.GetWalkSpeed() * 1.5f * 1.2f;
            Assert.AreEqual(expectedSpeed, player.GetModifiedWalkSpeed(), 0.01f);
        }

        #endregion
    }
}