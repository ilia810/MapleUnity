using System;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Core
{
    public class Monster
    {
        private readonly MonsterTemplate template;
        private static readonly Random random = new Random();
        private Vector2 position;
        private Vector2 velocity;
        private MovementPattern movementPattern;
        private float patrolRange;
        private float patrolOriginX;
        private bool movingRight = true;

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
            this.position = spawnPosition;
            this.patrolOriginX = spawnPosition.X;
            this.HP = template.MaxHP;
            this.IsDead = false;
            this.movementPattern = MovementPattern.Stationary;
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

            switch (movementPattern)
            {
                case MovementPattern.Patrol:
                    UpdatePatrolMovement(deltaTime);
                    break;
                case MovementPattern.Stationary:
                default:
                    velocity = Vector2.Zero;
                    break;
            }

            // Update position
            position += velocity * deltaTime;
        }

        private void UpdatePatrolMovement(float deltaTime)
        {
            var speed = template.Speed;
            
            // Check if we've reached patrol boundary
            if (movingRight && position.X >= patrolOriginX + patrolRange)
            {
                movingRight = false;
            }
            else if (!movingRight && position.X <= patrolOriginX - patrolRange)
            {
                movingRight = true;
            }

            // Set velocity based on direction
            velocity = new Vector2(movingRight ? speed : -speed, 0);
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