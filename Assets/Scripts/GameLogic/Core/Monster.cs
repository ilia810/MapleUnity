using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public partial class Monster : IPhysicsObject
    {
        public MapleClient.GameLogic.Data.SkillUseVisual SkillHitEffect { get; internal set; }
        private readonly MonsterTemplate template;
        private static readonly Random random = new Random();
        private Vector2 position;
        private Vector2 velocity;
        private MovementPattern movementPattern;
        private float patrolRange;
        private float patrolOriginX;
        private NormalMovement movement;
        private double movementAccumulator;
        private double hitRemaining;
        private string contactAction = "stand";
        private long contactMilliseconds;
        public MobContactFrame ContactBounds
        {
            get
            {
                var animations = template.ContactAnimations;
                if (animations == null) return default;
                if (!animations.TryGetValue(contactAction, out var animation)) animations.TryGetValue("stand", out animation);
                return animation?.Sample(contactMilliseconds) ?? default;
            }
        }
        public Vector2 PreviousPosition { get; private set; }
        public bool FacingRight => movingRight;
        public bool IsGrounded => isGrounded;
        public bool IsHit => hitRemaining > 0;
        public int CurrentFootholdLayer => movement?.FootholdLayer ?? 0;
        public int CurrentFootholdId => movement?.FootholdId ?? 0;
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
            set {
                velocity = value;
                if (movement != null) { movement.HSpeed = NormalMovement.ToTickSpeed(value.X); movement.VSpeed = -NormalMovement.ToTickSpeed(value.Y); }
            }
        }

        public int Id { get; set; }
        public int MonsterId => template.MonsterId;
        public string Name => template.Name;
        public int HP { get; private set; }
        public int MaxHP => template.MaxHP;
        public Vector2 Position 
        { 
            get => position; 
            set {
                position = value; PreviousPosition = value;
                movement?.Teleport(value.X * 100, -value.Y * 100);
            }
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
            this.PreviousPosition = spawnPosition;
            this.HP = template.MaxHP;
            this.IsDead = false;
            this.movementPattern = MovementPattern.Stationary;
            this.velocity = Vector2.Zero;
        }

        public void TakeDamage(int damage) => TakeDamage(damage, !movingRight);

        public void TakeDamage(int damage, bool knockLeft)
        {
            if (IsDead)
                return;

            var actualDamage = Math.Max(0, damage);
            HP = Math.Max(0, HP - actualDamage);

            if (actualDamage > 0 && actualDamage >= template.KnockbackThreshold && HP > 0)
            {
                // Mob::apply_damage sets counter=170; HIT changes after counter>200.
                hitRemaining = 31 * NormalMovement.TickSeconds;
                movingRight = knockLeft;
                contactAction = "hit1"; contactMilliseconds = 0;
            }
            DamageTaken?.Invoke(this, actualDamage);

            if (HP <= 0)
            {
                HP = 0;
                IsDead = true;
                StatDebuff = null;
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

        public void ConfigureTerrain(NormalTerrain terrain, bool facingRight)
        {
            movingRight = facingRight;
            movement = new NormalMovement();
            movement.SetTerrain(terrain);
            if (terrain.TryFindSpawn(position.X * 100, -position.Y * 100, out var spawn))
                position = new Vector2(spawn.X, spawn.Y - Player.Height / 2);
            PreviousPosition = position;
            patrolOriginX = position.X;
            movement.Teleport(position.X * 100, -position.Y * 100);
            movementAccumulator = 0;
        }

        public void Update(float deltaTime) { } // The simulation tick owns movement and AI.

        public void UpdatePhysics(float fixedDeltaTime, MapData mapData)
        {
            if (IsDead || fixedDeltaTime <= 0) return;
            if (movement == null)
            {
                var footholds = new List<Foothold>();
                if (mapData != null)
                    foreach (var p in mapData.Platforms)
                        footholds.Add(new Foothold(p.Id, p.X1, p.Y1, p.X2, p.Y2) {
                            Layer = p.Layer, PreviousId = p.PreviousId, NextId = p.NextId, IsWall = p.X1 == p.X2
                        });
                ConfigureTerrain(new NormalTerrain(footholds), movingRight);
            }
            movementAccumulator += fixedDeltaTime;
            while (movementAccumulator + 0.000000001 >= NormalMovement.TickSeconds)
            {
                movementAccumulator -= NormalMovement.TickSeconds;
                TickDebuff();
                PreviousPosition = position;
                // Source Mob::update turns on the tick after the collision clears
                // TURNATEDGES. Keep this independent of rendering and host FPS.
                if (movement.TurnedAtEdge) { movingRight = !movingRight; hitRemaining = 0; }
                if (patrolRange > 0 && (movingRight ? position.X >= patrolOriginX + patrolRange : position.X <= patrolOriginX - patrolRange))
                    movingRight = !movingRight;
                bool walking = movementPattern == MovementPattern.Patrol && template.CanMove;
                // Mob::Mob stores the float result of (NX speed + 100) * .001f.
                double force = walking && hitRemaining <= 0 ? (template.Speed + 100) * .001f : 0;
                double signedForce = movingRight ? force : -force;
                if (IsHit && template.CanMove) signedForce = (movement.OnGround ? .2 : .1) * (movingRight ? -1 : 1);
                movement.StepGroundActor(signedForce);
                hitRemaining = Math.Max(0, hitRemaining - NormalMovement.TickSeconds);
                position = new Vector2((float)(movement.X / 100), (float)(-movement.Y / 100));
                velocity = new Vector2(NormalMovement.ToWorldSpeed(movement.HSpeed), -NormalMovement.ToWorldSpeed(movement.VSpeed));
                isGrounded = movement.OnGround;
                string action = IsHit ? "hit1" : template.CanFly ? "fly" : Math.Abs(velocity.X) > .01f ? "move" : "stand";
                if (action != contactAction) { contactAction = action; contactMilliseconds = 0; }
                else contactMilliseconds += 8;
            }
        }

        public void OnTerrainCollision(Vector2 collisionPoint, Vector2 collisionNormal) { }
    }

    public enum MovementPattern
    {
        Stationary,
        Patrol,
        Chase,
        Random
    }
}
