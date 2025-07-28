using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public class Monster : IPhysicsObject
    {
        private readonly MonsterTemplate template;
        private static readonly Random random = new Random();
        private Vector2 position;
        private Vector2 velocity;
        private MovementPattern movementPattern;
        private float patrolRange;
        private float patrolOriginX;
        private bool movingRight = true;
        private bool isGrounded = false;
        
        // IPhysicsObject implementation
        private static int nextPhysicsId = 1000; // Start at 1000 to avoid conflicts with players
        private int physicsId;
        
        public int PhysicsId => physicsId;
        public bool UseGravity => true; // Monsters are affected by gravity
        public bool IsPhysicsActive => !IsDead; // Only active when alive
        public Vector2 Velocity 
        { 
            get => velocity; 
            set => velocity = value; 
        }

        public int Id { get; set; }
        public int MonsterId => template.MonsterId;
        public string Name => template.Name;
        public int HP { get; private set; }
        public int MaxHP => template.MaxHP;
        public Vector2 Position 
        { 
            get => position; 
            set => position = value; 
        }
        public bool IsDead { get; private set; }
        public MonsterTemplate Template => template;
        
        public event Action<Monster> Died;
        public event Action<Monster, int> DamageTaken;
        public event Action<int, int, Vector2> ItemDropped;

        public Monster(MonsterTemplate template, Vector2 spawnPosition)
        {
            this.template = template ?? throw new ArgumentNullException(nameof(template));
            this.physicsId = nextPhysicsId++;
            this.position = spawnPosition;
            this.patrolOriginX = spawnPosition.X;
            this.HP = template.MaxHP;
            this.IsDead = false;
            this.movementPattern = MovementPattern.Stationary;
            this.velocity = Vector2.Zero;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead)
                return;

            var actualDamage = Math.Max(0, damage);
            HP = Math.Max(0, HP - actualDamage);

            DamageTaken?.Invoke(this, actualDamage);

            if (HP <= 0)
            {
                HP = 0;
                IsDead = true;
                velocity = Vector2.Zero;
                
                // Drop items
                if (template.DropTable != null)
                {
                    foreach (var dropInfo in template.DropTable)
                    {
                        if (random.NextDouble() <= dropInfo.DropRate)
                        {
                            ItemDropped?.Invoke(dropInfo.ItemId, dropInfo.Quantity, position);
                        }
                    }
                }
                
                Died?.Invoke(this);
            }
        }

        public void SetMovementPattern(MovementPattern pattern, float range = 0)
        {
            movementPattern = pattern;
            patrolRange = range;
            patrolOriginX = position.X;
        }

        public void Update(float deltaTime)
        {
            if (IsDead)
                return;

            // Movement pattern updates velocity but doesn't apply physics
            UpdateMovementPattern();
        }
        
        // IPhysicsObject implementation
        public void UpdatePhysics(float fixedDeltaTime, MapData mapData)
        {
            if (IsDead)
                return;
            
            // Apply gravity if not grounded
            if (!isGrounded && UseGravity)
            {
                velocity = new Vector2(velocity.X, MaplePhysics.ApplyGravity(velocity.Y, fixedDeltaTime));
            }
            
            // Update position
            var newPosition = position + velocity * fixedDeltaTime;
            
            // Simple ground collision (monsters don't use platforms for now)
            // This will be expanded later for proper platform collision
            if (mapData != null && velocity.Y <= 0)
            {
                // For now, just check if we're below spawn height
                if (newPosition.Y <= patrolOriginX)
                {
                    newPosition = new Vector2(newPosition.X, patrolOriginX);
                    velocity = new Vector2(velocity.X, 0);
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                }
            }
            
            position = newPosition;
        }
        
        public void OnTerrainCollision(Vector2 collisionPoint, Vector2 collisionNormal)
        {
            // Handle terrain collision
            if (collisionNormal.Y > 0.7f) // Ground collision
            {
                isGrounded = true;
                velocity = new Vector2(velocity.X, 0);
            }
        }
        
        private void UpdateMovementPattern()
        {
            switch (movementPattern)
            {
                case MovementPattern.Patrol:
                    UpdatePatrolMovement();
                    break;
                case MovementPattern.Stationary:
                default:
                    velocity = new Vector2(0, velocity.Y); // Keep Y velocity for gravity
                    break;
            }
        }

        private void UpdatePatrolMovement()
        {
            var speed = template.Speed / 100f; // Convert pixels/s to units/s
            
            // Check if we've reached patrol boundary
            if (movingRight && position.X >= patrolOriginX + patrolRange)
            {
                movingRight = false;
            }
            else if (!movingRight && position.X <= patrolOriginX - patrolRange)
            {
                movingRight = true;
            }

            // Set horizontal velocity based on direction, preserve Y velocity
            velocity = new Vector2(movingRight ? speed : -speed, velocity.Y);
        }
    }

    public enum MovementPattern
    {
        Stationary,
        Patrol,
        Chase,
        Random
    }
}