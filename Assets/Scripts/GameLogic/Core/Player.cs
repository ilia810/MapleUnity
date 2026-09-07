using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public partial class Player : IPhysicsObject
    {
        public event Action Landed;
        public event Action Jumped;
        public Vector2 PreviousPosition { get; private set; }
        public bool? FacingRight { get; private set; }
        public int CurrentFootholdId => normalMovement?.FootholdId ?? 0;
        public int CurrentFootholdLayer => normalMovement?.FootholdLayer ?? 0;
        private NormalMovement normalMovement;
        private NormalTerrain normalTerrain;
        private MapData normalTerrainMap;
        private MapData movementMap;
        private int normalTerrainRevision = -1;
        private double movementAccumulator;
        private bool updatingPhysics;
        private bool jumpRequested;
        private bool jumpDownRequested;
        private bool crouchRequested;
        private LadderInfo requestedLadder;
        private bool stopClimbingRequested;
        public bool CanDropThroughPlatform => normalMovement?.CanDrop == true && IsGrounded;
        public bool CanClimb => (normalMovement?.ClimbCooldownMilliseconds ?? 0) == 0;
        public int ClimbCooldownMilliseconds => normalMovement?.ClimbCooldownMilliseconds ?? 0;
        
        // View listener management
        private readonly List<IPlayerViewListener> viewListeners = new List<IPlayerViewListener>();
        public bool HasViewListeners => viewListeners.Count > 0;
        
        // Services
        private readonly IFootholdService footholdService;
        
        // Player dimensions (in units)
        public const float Height = 0.6f;
        private const float PLAYER_HEIGHT = Height; // 60 pixels / 100
        private const float PLAYER_WIDTH = 0.3f;  // 30 pixels / 100
        
        // Movement state
        private float actualWalkSpeed;
        private float actualJumpPower;
        private bool jumpKeyPressed = false; // Track jump key state for subsequent jumps
        
        // Special movement
        private bool hasDoubleJump = false;
        private bool hasFlashJump = false;
        private int jumpCount = 0; // 0 = no jumps used, 1 = double jump used
        private float flashJumpCooldown = 0f;
        private const float FLASH_JUMP_COOLDOWN = 1f; // 1 second cooldown
        private const float FLASH_JUMP_DISTANCE = 1.5f; // 150 pixels / 100
        
        // Movement modifiers
        private readonly List<IMovementModifier> movementModifiers = new List<IMovementModifier>();

        public int Id { get; set; }
        public string Name { get; set; } = "Player";
        
        private Vector2 position;
        private int positionSetCount = 0;
        public Vector2 Position 
        { 
            get => position;
            set
            {
                if (position != value)
                {
                    if (positionSetCount < 10) // Log first 10 position changes
                    {
                        System.Console.WriteLine($"[FOOTHOLD_COLLISION] Player.Position changed #{positionSetCount}: ({position.X:F2}, {position.Y:F2}) -> ({value.X:F2}, {value.Y:F2})");
                        positionSetCount++;
                    }
                    if (!updatingPhysics)
                    {
                        PreviousPosition = value;
                        normalMovement = null;
                        pendingContactKnockback = null;
                        jumpRequested = false;
                        requestedLadder = null;
                        stopClimbingRequested = false;
                        currentLadder = null;
                    }
                    position = value;
                    NotifyViewListeners(l => l.OnPositionChanged(value));
                }
            }
        }
        
        private Vector2 velocity;
        public Vector2 Velocity 
        { 
            get => velocity;
            set
            {
                if (velocity != value)
                {
                    if (!updatingPhysics) normalMovement = null;
                    velocity = value;
                    NotifyViewListeners(l => l.OnVelocityChanged(value));
                }
            }
        }
        
        private bool isGrounded;
        public bool IsGrounded 
        { 
            get => isGrounded;
            set
            {
                if (isGrounded != value)
                {
                    if (!updatingPhysics) normalMovement = null;
                    isGrounded = value;
                    NotifyViewListeners(l => l.OnGroundedStateChanged(value));
                }
            }
        }
        
        public bool IsJumping { get; set; }
        
        public LadderInfo GetCurrentLadder() => currentLadder;
        
        private PlayerState state;
        public PlayerState State 
        { 
            get => state;
            private set
            {
                if (!IsBasicAttacking && state != value)
                {
                    state = value;
                    SynchronizeBodyStance();
                    NotifyViewListeners(l => l.OnStateChanged(value));
                }
            }
        }

        // Combat stats
        private int invulnerableMilliseconds;
        private bool? pendingContactKnockback;
        private long experience;
        public bool IsDead => hp <= 0;
        public bool IsInvulnerable => IsDead || invulnerableMilliseconds > 0;
        public int InvulnerableMilliseconds => invulnerableMilliseconds;
        public long Experience => experience;
        public long ExperienceToNextLevel => ExperienceTable.RequiredForLevel(level);
        public event Action<int> DamageTaken;
        public event Action Died;
        public event Action<long> ExperienceGained;
        public event Action<int> LeveledUp;
        private int baseDamage = 20;
        public bool HasPracticeDamageOverride { get; private set; }
        private float combatMastery;
        public float CombatMastery { get => Passives.Mastery ?? combatMastery; set => combatMastery = value; }
        private float combatDamagePercent;
        public float CombatDamagePercent { get => Passives.DamagePercent ?? combatDamagePercent; set => combatDamagePercent = value; }
        private float criticalChance = .05f;
        public float CriticalChance { get => Math.Max(0, Math.Min(1, Passives.CriticalChance ?? criticalChance)); set => criticalChance = value; }
        public float CriticalDamageMultiplier => Passives.CriticalDamageMultiplier ?? 1.5f;
        public int ProjectileRangePixels => 400 + Passives.ProjectileRangeBonus;
        private int level = 1;
        private int hp = 100;
        private int baseMaxHp = 100;
        private int maxHp => WithPercentBuff(Math.Max(1, baseMaxHp + EquipmentBonus(StatType.MaxHP)), BuffType.MaxHPPercent);
        private int mp = 50;
        private int baseMaxMp = 50;
        private int maxMp => WithPercentBuff(Math.Max(0, baseMaxMp + EquipmentBonus(StatType.MaxMP)), BuffType.MaxMPPercent);
        
        // Character stats
        private int baseSTR = 15;
        public int STR { get => baseSTR + EquipmentBonus(StatType.STR); set => baseSTR = value; }
        private int baseDEX = 15;
        public int DEX { get => baseDEX + EquipmentBonus(StatType.DEX); set => baseDEX = value; }
        private int baseINT = 15;
        public int INT { get => baseINT + EquipmentBonus(StatType.INT); set => baseINT = value; }
        private int baseLUK = 15;
        public int LUK { get => baseLUK + EquipmentBonus(StatType.LUK); set => baseLUK = value; }
        private int baseWeaponAttack;
        public int WeaponAttack { get => WithBuff(baseWeaponAttack + EquipmentBonus(StatType.WeaponAttack) + AmmunitionAttack + Passives.WeaponAttack, BuffType.WeaponAttack, 999); set => baseWeaponAttack = value; }
        private int baseMagicAttack = 0;
        public int MagicAttack { get => WithBuff(baseMagicAttack + EquipmentBonus(StatType.MagicAttack) + Passives.MagicAttack, BuffType.MagicAttack, 2000); set => baseMagicAttack = value; }
        private int baseWeaponDefense = 10;
        public int WeaponDefense { get => WithBuff(baseWeaponDefense + EquipmentBonus(StatType.WeaponDefense), BuffType.WeaponDefense, 999); set => baseWeaponDefense = value; }
        private int baseMagicDefense = 10;
        public int MagicDefense { get => WithBuff(baseMagicDefense + EquipmentBonus(StatType.MagicDefense), BuffType.MagicDefense, 999); set => baseMagicDefense = value; }
        private int baseAccuracy;
        private int AccuracyBonus => Math.Min(999, WithBuff(baseAccuracy + EquipmentBonus(StatType.Accuracy) + Passives.Accuracy, BuffType.Accuracy, 999));
        public int Accuracy {
            get => AccuracyBonus +
                (int)(Math.Min(999, DEX) * .8f + Math.Min(999, LUK) * .5f);
            set => baseAccuracy = value;
        }
        private int baseAvoidability = 0;
        public int Avoidability { get => WithBuff(baseAvoidability + EquipmentBonus(StatType.Avoidability) + Passives.Avoidability, BuffType.Avoidability, 999); set => baseAvoidability = value; }
        private int baseSpeed = 100;
        public int Speed { get => WithBuff(baseSpeed + EquipmentBonus(StatType.Speed), BuffType.Speed, 140); set => baseSpeed = value; }
        private int baseJumpPower = 120;
        public int JumpPower { get => WithBuff(baseJumpPower + EquipmentBonus(StatType.Jump), BuffType.Jump, 123); set => baseJumpPower = value; }
        public int JobId { get; set; } = 0; // Beginner

        private bool isMovingLeft;
        private bool isMovingRight;
        private int pendingSwimFacing;
        public long SwimAnimationTicks { get; private set; }
        private bool isClimbingUp;
        private bool isClimbingDown;
        private LadderInfo currentLadder;
        
        // IPhysicsObject implementation
        private static int nextPhysicsId = 1;
        private int physicsId;
        
        public int PhysicsId => physicsId;
        public bool UseGravity => State != PlayerState.Climbing;
        public bool IsPhysicsActive => true; // Player is always active

        public Player() : this(null)
        {
        }
        
        public Player(IFootholdService footholdService)
        {
            this.footholdService = footholdService;
            physicsId = nextPhysicsId++;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            IsGrounded = false; // Start not grounded to let gravity work
            State = PlayerState.Standing;
            
            // Initialize movement speeds based on stats
            UpdateMovementSpeeds();
            
            if (footholdService != null)
            {
                System.Console.WriteLine("[FOOTHOLD_COLLISION] Player initialized with FootholdService");
            }
            else
            {
                System.Console.WriteLine("[FOOTHOLD_COLLISION] WARNING: Player initialized without FootholdService - using fallback platform detection");
            }
        }
        
        public Player(int id, string name, int level, int jobId) : this(null, id, name, level, jobId)
        {
        }
        
        public Player(IFootholdService footholdService, int id, string name, int level, int jobId)
        {
            this.footholdService = footholdService;
            physicsId = nextPhysicsId++;
            Id = id;
            Name = name;
            this.level = level;
            JobId = jobId;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            IsGrounded = false;
            State = PlayerState.Standing;
            
            // Initialize movement speeds based on stats
            UpdateMovementSpeeds();
        }

        public void MoveLeft(bool active)
        {
            if (active && !isMovingLeft && State == PlayerState.Swimming && !IsBasicAttacking) pendingSwimFacing = -1;
            isMovingLeft = active;
        }

        public void MoveRight(bool active)
        {
            if (active && !isMovingRight && State == PlayerState.Swimming && !IsBasicAttacking) pendingSwimFacing = 1;
            isMovingRight = active;
        }

        private void UpdateHorizontalVelocity(float deltaTime)
        {
            // Don't move horizontally when crouching or climbing
            if (State == PlayerState.Crouching || State == PlayerState.Climbing)
            {
                Velocity = new Vector2(0, Velocity.Y);
                return;
            }

            // Check if movement is prevented
            bool movementPrevented = false;
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsMovement)
                {
                    movementPrevented = true;
                    break;
                }
            }

            if (movementPrevented)
            {
                // Apply friction to stop movement
                float velocityX = Velocity.X;
                velocityX = MaplePhysics.ApplyFriction(velocityX, deltaTime, IsGrounded);
                Velocity = new Vector2(velocityX, Velocity.Y);
                return;
            }

            // Get modified walk speed
            float modifiedWalkSpeed = GetModifiedWalkSpeed();

            // Determine target velocity based on input
            float targetVelocityX = 0f;
            if (isMovingLeft && !isMovingRight)
            {
                targetVelocityX = -modifiedWalkSpeed;
            }
            else if (isMovingRight && !isMovingLeft)
            {
                targetVelocityX = modifiedWalkSpeed;
            }

            // Apply acceleration or friction
            float currentVelocityX = Velocity.X;
            if (targetVelocityX != 0)
            {
                // Accelerate towards target velocity
                currentVelocityX = MaplePhysics.ApplyMovementAcceleration(currentVelocityX, targetVelocityX, deltaTime, IsGrounded);
                
                // Update state to walking if we're moving
                if (IsGrounded && State != PlayerState.Walking && State != PlayerState.Swimming && System.Math.Abs(currentVelocityX) > 0.1f)
                {
                    State = PlayerState.Walking;
                    TriggerAnimationEvent(PlayerAnimationEvent.StartWalk);
                }
            }
            else
            {
                // Get friction multiplier from modifiers
                float frictionMultiplier = 1f;
                foreach (var modifier in movementModifiers)
                {
                    frictionMultiplier *= modifier.FrictionMultiplier;
                }
                
                // Apply modified friction when no input (only on ground)
                if (IsGrounded && frictionMultiplier > 0)
                {
                    float friction = MaplePhysics.WalkFriction * frictionMultiplier;
                    float deceleration = friction * deltaTime;
                    
                    if (currentVelocityX > 0)
                    {
                        currentVelocityX -= deceleration;
                        if (currentVelocityX < 0) currentVelocityX = 0;
                    }
                    else if (currentVelocityX < 0)
                    {
                        currentVelocityX += deceleration;
                        if (currentVelocityX > 0) currentVelocityX = 0;
                    }
                }
                
                // Update state to standing when stopped
                if (IsGrounded && (State == PlayerState.Walking || State == PlayerState.Swimming) && System.Math.Abs(currentVelocityX) < 0.01f)
                {
                    State = State == PlayerState.Swimming ? PlayerState.Swimming : PlayerState.Standing;
                    TriggerAnimationEvent(PlayerAnimationEvent.StopWalk);
                }
            }

            Velocity = new Vector2(currentVelocityX, Velocity.Y);
        }

        public void Jump()
        {
            if (jumpKeyPressed) return;
// Keep held keys, but discard blocked jump edges instead of buffering a jump.
            jumpKeyPressed = true;
            if (IsBasicAttacking && State != PlayerState.Crouching) return;
            jumpRequested = true;
            jumpDownRequested = crouchRequested || isClimbingDown;
        }

        private void ExecuteSpecialJump()
        {
            // Check if jumping is prevented
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsJumping)
                {
                    return;
                }
            }
            
            float modifiedJumpPower = GetModifiedJumpPower();
            
            if (IsGrounded && State != PlayerState.Crouching)
            {
                Velocity = new Vector2(Velocity.X, modifiedJumpPower);
                IsJumping = true;
                IsGrounded = false;
                State = PlayerState.Jumping;
                jumpCount = 0; // Reset jump count
                TriggerAnimationEvent(PlayerAnimationEvent.Jump);
                Jumped?.Invoke();
            }
            else if (!IsGrounded && hasDoubleJump && jumpCount == 0)
            {
                // Double jump
                jumpCount = 1;
                float doubleJumpPower = modifiedJumpPower * MaplePhysics.DoubleJumpModifier;
                Velocity = new Vector2(Velocity.X, doubleJumpPower);
                State = PlayerState.DoubleJumping;
                TriggerAnimationEvent(PlayerAnimationEvent.Jump);
                Jumped?.Invoke();
            }
        }
        
        public void ReleaseJump()
        {
            jumpKeyPressed = false;
        }

        public void ResetMovementForMap()
        {
            CombatContextVersion++;
            BasicAttack?.Cancel();
            SkillEffect = null;
            ArmorEchoMilliseconds = 0;
            normalMovement = null;
            pendingContactKnockback = null;
            normalTerrain = null;
            normalTerrainMap = null;
            movementMap = null;
            normalTerrainRevision = -1;
            jumpRequested = false;
            jumpKeyPressed = false;
            requestedLadder = null;
            stopClimbingRequested = false;
            currentLadder = null;
            isClimbingUp = false;
            isClimbingDown = false;
            isMovingLeft = false;
            isMovingRight = false;
            pendingSwimFacing = 0;
            SwimAnimationTicks = 0;
            StanceAnimation.Reset();
            pendingExpression = null;
            crouchRequested = false;
            IsJumping = false;
            State = IsGrounded ? PlayerState.Standing : PlayerState.Falling;
            movementAccumulator = 0;
            PreviousPosition = Position;
        }

        public void UpdatePhysics(float deltaTime, MapData mapData)
        {
            if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            movementAccumulator += deltaTime == PhysicsUpdateManager.FIXED_TIMESTEP ? NormalMovement.TickSeconds : Math.Min(deltaTime, 0.25f);
            while (movementAccumulator + 1e-8 >= NormalMovement.TickSeconds)
            {
                movementAccumulator = Math.Max(0, movementAccumulator - NormalMovement.TickSeconds);
                TickStatBuffs();
                invulnerableMilliseconds = Math.Max(0, invulnerableMilliseconds - 8);
                PreviousPosition = Position;
                if (IsDead) continue;
                updatingPhysics = true;
                try { StepMovement(mapData); }
                finally { updatingPhysics = false; }
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void StepMovement(MapData mapData)
        {
            // Let GameWorld relocate invalid external/network positions before
            // publishing another physics update into the views.
            if (!IsFinite(Position.X) || !IsFinite(Position.Y) || !IsFinite(Velocity.X) || !IsFinite(Velocity.Y)) return;
            ApplyExpressionInput();
            movementMap = mapData;
            const float step = PhysicsUpdateManager.FIXED_TIMESTEP;
            UpdateMovementModifiers(step);
            if (flashJumpCooldown > 0) flashJumpCooldown -= step;
            UpdateEnvironmentalEffects(mapData);

            bool special = mapData?.IsUnderwater != true && State != PlayerState.Climbing &&
                (State == PlayerState.DoubleJumping || State == PlayerState.FlashJumping);
            if (mapData?.IsUnderwater != true && !IsBasicAttacking && jumpRequested && State != PlayerState.Climbing && (!IsGrounded && hasDoubleJump || special))
            {
                ExecuteSpecialJump();
                special = true;
                jumpRequested = false;
            }
            if (special)
            {
                normalMovement = null;
                UpdateLegacyPhysics(step, mapData);
                SynchronizeBodyStance();
                if (!IsBasicAttacking) StanceAnimation.Advance(1);
                AdvanceFace(1);
                jumpRequested = false;
                requestedLadder = null;
                return;
            }

            int revision = (footholdService as FootholdService)?.Revision ?? 0;
            if (normalTerrain == null || (footholdService == null && normalTerrainMap != mapData) || normalTerrainRevision != revision)
            {
                IEnumerable<Foothold> footholds = footholdService != null
                    ? footholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue)
                    : GetCollisionFootholds(float.MinValue, float.MaxValue, mapData);
                normalTerrain = new NormalTerrain(footholds);
                normalTerrainMap = mapData;
                normalTerrainRevision = revision;
            }
            if (normalMovement == null)
            {
                normalMovement = new NormalMovement
                {
                    // Remove float conversion noise at external pixel/half-pixel positions.
                    X = Math.Round(Position.X * 100.0, 5),
                    Y = -(Position.Y - PLAYER_HEIGHT / 2) * 100.0,
                    HSpeed = NormalMovement.ToTickSpeed(Velocity.X),
                    VSpeed = -NormalMovement.ToTickSpeed(Velocity.Y),
                    OnGround = IsGrounded,
                    State = State == PlayerState.Walking ? NormalMovement.Stance.Walk :
                        State == PlayerState.Crouching ? NormalMovement.Stance.Prone :
                        State == PlayerState.Swimming && mapData?.IsUnderwater == true ? NormalMovement.Stance.Swim :
                        !IsGrounded ? NormalMovement.Stance.Fall : NormalMovement.Stance.Stand
                };
                // Undo only float conversion noise at an externally supplied ground contact.
                int floor = normalTerrain.Below(normalMovement.X, normalMovement.Y - 0.0001);
                var support = normalTerrain.Get(floor);
                if (support != null && Math.Abs(NormalTerrain.Ground(support, normalMovement.X) - normalMovement.Y) < 0.0001)
                    normalMovement.Y = NormalTerrain.Ground(support, normalMovement.X);
            }
            normalMovement.SetTerrain(normalTerrain);
            if (pendingContactKnockback.HasValue)
            {
                normalMovement.ApplyContactKnockback(pendingContactKnockback.Value);
                pendingContactKnockback = null;
            }
            bool preventMovement = movementModifiers.Any(m => m.PreventsMovement);
            float friction = 1;
            foreach (var modifier in movementModifiers) friction *= modifier.FrictionMultiplier;
            float walkForce = (float)(NormalMovement.ToTickSpeed(GetModifiedWalkSpeed()) / 5);
            float jumpForce = (float)NormalMovement.ToTickSpeed(GetModifiedJumpPower());
            if (stopClimbingRequested) normalMovement.CancelClimbing();
            bool down = crouchRequested || isClimbingDown;
            bool up = isClimbingUp || (requestedLadder != null && !down);
            IEnumerable<LadderInfo> ladders = requestedLadder != null ? new[] { requestedLadder } : mapData?.Ladders;
            bool wasSwimming = State == PlayerState.Swimming && !IsBasicAttacking;
            normalMovement.Step(!preventMovement && isMovingLeft, !preventMovement && isMovingRight,
                down, jumpRequested && jumpForce > 0, walkForce, jumpForce, friction,
                up, jumpKeyPressed && jumpForce > 0, preventMovement ? 0 : (float)Speed / 100,
                ladders, !preventMovement && !stopClimbingRequested, jumpDownRequested, IsBasicAttacking,
                mapData?.IsUnderwater == true, preventMovement ? 0 : .25f, pendingSwimFacing);
            SwimAnimationTicks = normalMovement.State == NormalMovement.Stance.Swim && !IsBasicAttacking
                ? (wasSwimming ? SwimAnimationTicks + 1 : 0) : 0;
            AdvanceBodyStance(normalMovement.AnimationState, normalMovement.AnimationSpeed);
            AdvanceFace(normalMovement.AnimationSpeed);
            pendingSwimFacing = 0;
            jumpRequested = false;
            requestedLadder = null;
            stopClimbingRequested = false;
            PublishNormalMovement(true);
        }

        private void PublishNormalMovement(bool physicsEvents)
        {
            var previousLadder = currentLadder;
            currentLadder = normalMovement.CurrentLadder;
            if (previousLadder == null && currentLadder != null)
                PreviousPosition = new Vector2((float)(normalMovement.X / 100), PreviousPosition.Y);
            if (!IsBasicAttacking && normalMovement.FacingDirection != 0) FacingRight = normalMovement.FacingDirection > 0;
            Velocity = new Vector2(NormalMovement.ToWorldSpeed(normalMovement.HSpeed),
                -NormalMovement.ToWorldSpeed(normalMovement.VSpeed));
            IsGrounded = normalMovement.OnGround;
            Position = new Vector2((float)(normalMovement.X / 100),
                (float)(-normalMovement.Y / 100) + PLAYER_HEIGHT / 2);
            if (physicsEvents && normalMovement.Jumped) { IsJumping = true; jumpCount = 0; }
            if (physicsEvents && normalMovement.Landed) { IsJumping = false; jumpCount = 0; }
            if (normalMovement.Dropped || normalMovement.IsClimbing || normalMovement.State == NormalMovement.Stance.Swim) IsJumping = false;
            var nextState = normalMovement.IsClimbing ? PlayerState.Climbing :
                normalMovement.State == NormalMovement.Stance.Walk ? PlayerState.Walking :
                normalMovement.State == NormalMovement.Stance.Prone ? PlayerState.Crouching :
                normalMovement.State == NormalMovement.Stance.Swim ? PlayerState.Swimming :
                normalMovement.State == NormalMovement.Stance.Fall ? (IsJumping ? PlayerState.Jumping : PlayerState.Falling) :
                physicsEvents && normalMovement.Jumped ? PlayerState.Jumping : PlayerState.Standing;
            var previousState = State;
            State = nextState;
            SynchronizeBodyStance();
            if (previousLadder == null && currentLadder != null) TriggerAnimationEvent(PlayerAnimationEvent.StartClimb);
            else if (previousLadder != null && currentLadder == null) TriggerAnimationEvent(PlayerAnimationEvent.StopClimb);
            if (physicsEvents && normalMovement.Jumped)
            {
                TriggerAnimationEvent(PlayerAnimationEvent.Jump);
                Jumped?.Invoke();
            }
            if (physicsEvents && normalMovement.Landed)
            {
                Landed?.Invoke();
                TriggerAnimationEvent(PlayerAnimationEvent.Land);
            }
            if (previousState != State)
            {
                if (State == PlayerState.Walking) TriggerAnimationEvent(PlayerAnimationEvent.StartWalk);
                else if (previousState == PlayerState.Walking && State == PlayerState.Standing)
                    TriggerAnimationEvent(PlayerAnimationEvent.StopWalk);
                else if (State == PlayerState.Crouching) TriggerAnimationEvent(PlayerAnimationEvent.Crouch);
                else if (previousState == PlayerState.Crouching) TriggerAnimationEvent(PlayerAnimationEvent.StandUp);
            }
        }

        private void UpdateLegacyPhysics(float deltaTime, MapData mapData)
        {
            if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            // Like HeavenClient Physics::move_normal, integrate intent once per tick.
            if (!IsBasicAttacking) UpdateHorizontalVelocity(deltaTime);
            if (!IsGrounded)
                Velocity = new Vector2(Velocity.X, MaplePhysics.ApplyGravity(Velocity.Y, deltaTime, mapData?.IsUnderwater ?? false));
            var newPosition = Position + Velocity * deltaTime;
            if (Velocity.Y <= 0)
            {
                var previousFeet = MaplePhysicsConverter.UnityToMaple(new Vector2(Position.X, Position.Y - PLAYER_HEIGHT / 2));
                var nextFeet = MaplePhysicsConverter.UnityToMaple(new Vector2(newPosition.X, newPosition.Y - PLAYER_HEIGHT / 2));
                var surfaces = GetCollisionFootholds(previousFeet.X, nextFeet.X, mapData).ToList();
                float supportY;
                if (IsGrounded && TryGetSupportedGround(previousFeet, nextFeet.X, surfaces, out supportY))
                {
                    newPosition = new Vector2(newPosition.X, MaplePhysicsConverter.MapleToUnityY(supportY) + PLAYER_HEIGHT / 2);
                    Velocity = new Vector2(Velocity.X, 0);
                }
                else
                {
                    IsGrounded = false;
                    Vector2 landingFeet;
                    if (TryFindLanding(previousFeet, nextFeet, surfaces, out landingFeet))
                    {
                        var landingPosition = MaplePhysicsConverter.MapleToUnity(landingFeet);
                        newPosition = new Vector2(landingPosition.X, landingPosition.Y + PLAYER_HEIGHT / 2);
                        Velocity = new Vector2(Velocity.X, 0);
                        IsGrounded = true;
                        IsJumping = false;
                        jumpCount = 0;
                        if (State != PlayerState.Crouching && State != PlayerState.Swimming)
                            State = Math.Abs(Velocity.X) > 0.01f && (isMovingLeft || isMovingRight)
                                ? PlayerState.Walking : PlayerState.Standing;
                        Landed?.Invoke();
                        TriggerAnimationEvent(PlayerAnimationEvent.Land);
                    }
                }
            }
            Position = newPosition;
            if (!IsGrounded && Velocity.Y <= 0 && State != PlayerState.Jumping &&
                State != PlayerState.DoubleJumping && State != PlayerState.FlashJumping)
                State = PlayerState.Falling;
        }

        // HeavenClient lands on the surface; ground-1 belongs only to its spawn query.
        private const float GroundOffset = 0f;
        private const float GroundContactTolerance = 1.01f; // Maple pixels.

        private IEnumerable<Foothold> GetCollisionFootholds(float startX, float endX, MapData mapData)
        {
            float minX = Math.Min(startX, endX);
            float maxX = Math.Max(startX, endX);
            if (footholdService != null)
            {
                foreach (var foothold in footholdService.GetFootholdsInArea(minX, float.MinValue, maxX, float.MaxValue))
                    if (!footholdService.IsWall(foothold)) yield return foothold;
            }
            else if (mapData?.Platforms != null)
            {
                foreach (var platform in mapData.Platforms)
                    if ((platform.Type == PlatformType.Normal || platform.Type == PlatformType.OneWay) &&
                        Math.Abs(platform.X2 - platform.X1) >= 0.1f &&
                        Math.Max(platform.X1, platform.X2) >= minX && Math.Min(platform.X1, platform.X2) <= maxX)
                        yield return new Foothold(platform.Id, platform.X1, platform.Y1, platform.X2, platform.Y2);
            }
        }

        private bool TryGetSupportedGround(Vector2 previousFeet, float nextX, List<Foothold> surfaces, out float groundY)
        {
            groundY = 0;
            Foothold support = null;
            float closestDistance = GroundContactTolerance;
            foreach (var surface in surfaces)
            {
                float distance = Math.Abs(previousFeet.Y - (surface.GetYAtX(previousFeet.X) - GroundOffset));
                if (!float.IsNaN(distance) && distance <= closestDistance)
                {
                    closestDistance = distance;
                    support = surface;
                }
            }
            bool movingRight = nextX >= previousFeet.X;
            // Follow the supporting surface or a shared endpoint, never an arbitrary floor below.
            for (int i = 0; support != null && i < surfaces.Count; i++)
            {
                float surfaceY = support.GetYAtX(nextX);
                if (!float.IsNaN(surfaceY))
                {
                    groundY = surfaceY - GroundOffset;
                    return true;
                }
                float edgeX = movingRight ? Math.Max(support.X1, support.X2) : Math.Min(support.X1, support.X2);
                float edgeY = support.GetYAtX(edgeX);
                Foothold adjacent = null;
                foreach (var candidate in surfaces)
                {
                    float entryX = movingRight ? Math.Min(candidate.X1, candidate.X2) : Math.Max(candidate.X1, candidate.X2);
                    float exitX = movingRight ? Math.Max(candidate.X1, candidate.X2) : Math.Min(candidate.X1, candidate.X2);
                    if (candidate != support && Math.Abs(entryX - edgeX) < 0.01f &&
                        (movingRight ? exitX > edgeX : exitX < edgeX) &&
                        Math.Abs(candidate.GetYAtX(entryX) - edgeY) < 0.01f)
                    {
                        adjacent = candidate;
                        break;
                    }
                }
                support = adjacent;
            }
            return false;
        }

        private bool TryFindLanding(Vector2 previousFeet, Vector2 nextFeet, List<Foothold> surfaces, out Vector2 landingFeet)
        {
            landingFeet = Vector2.Zero;
            float earliestHit = float.MaxValue;
            float deltaX = nextFeet.X - previousFeet.X;
            float deltaY = nextFeet.Y - previousFeet.Y;
            foreach (var surface in surfaces)
            {
                float slope = (surface.Y2 - surface.Y1) / (surface.X2 - surface.X1);
                float previousSurfaceY = surface.Y1 + (previousFeet.X - surface.X1) * slope - GroundOffset;
                float previousDistance = previousFeet.Y - previousSurfaceY;
                float relativeMovement = deltaY - deltaX * slope;
                // Sweep feet from previous to next position, approaching one-way terrain from above.
                if (previousDistance > GroundContactTolerance || relativeMovement <= 0) continue;
                float hitTime = Math.Max(0, -previousDistance / relativeMovement);
                if (hitTime > 1 || hitTime >= earliestHit) continue;
                float hitX = previousFeet.X + deltaX * hitTime;
                float hitY = surface.GetYAtX(hitX);
                if (float.IsNaN(hitY)) continue;
                earliestHit = hitTime;
                float finalY = surface.GetYAtX(nextFeet.X);
                landingFeet = float.IsNaN(finalY)
                    ? new Vector2(hitX, hitY - GroundOffset)
                    : new Vector2(nextFeet.X, finalY - GroundOffset);
            }
            return earliestHit != float.MaxValue;
        }

        private Platform GetPlatformBelow(Vector2 position, MapData mapData)
        {
            if (mapData?.Platforms == null) return null;
            var feet = MaplePhysicsConverter.UnityToMaple(new Vector2(position.X, position.Y - PLAYER_HEIGHT / 2));
            Platform closest = null;
            float closestDistance = float.MaxValue;
            foreach (var platform in mapData.Platforms)
            {
                if ((platform.Type != PlatformType.Normal && platform.Type != PlatformType.OneWay) ||
                    Math.Abs(platform.X2 - platform.X1) < 0.1f ||
                    feet.X < Math.Min(platform.X1, platform.X2) || feet.X > Math.Max(platform.X1, platform.X2)) continue;
                float t = (feet.X - platform.X1) / (platform.X2 - platform.X1);
                float distance = platform.Y1 + t * (platform.Y2 - platform.Y1) - feet.Y;
                if (distance >= -GroundContactTolerance && distance < closestDistance)
                {
                    closest = platform;
                    closestDistance = distance;
                }
            }
            return closestDistance <= GroundContactTolerance ? closest : null;
        }

        // Combat methods
        public int GetBaseDamage()
        {
            return Math.Max(1, baseDamage + EquipmentBonus(StatType.WeaponAttack));
        }

        public void SetBaseDamage(int damage)
        {
            baseDamage = damage;
            HasPracticeDamageOverride = true;
        }

        public int CurrentHP => hp;
        public int MaxHP
        {
            get => maxHp;
            set { baseMaxHp = Math.Max(1, value); hp = Math.Min(hp, maxHp); }
        }
        public int CurrentMP
        {
            get => mp;
            set => mp = System.Math.Max(0, System.Math.Min(value, maxMp));
        }
        public int MaxMP
        {
            get => maxMp;
            set { baseMaxMp = Math.Max(0, value); mp = Math.Min(mp, maxMp); }
        }
        public int Level
        {
            get => level;
            set { level = Math.Max(1, Math.Min(value, ExperienceTable.LevelCap)); experience = 0; }
        }
        
        // Inventory
        private Inventory inventory = new Inventory();
        public Inventory Inventory => inventory;
        private Dictionary<EquipSlot, int> equippedItems = new Dictionary<EquipSlot, int>();

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0) return;
            int actual = Math.Min(hp, damage);
            hp -= actual;
            if (IsDead)
            {
                ClearStatBuffs();
                pendingContactKnockback = null;
                ResetMovementForMap();
                Velocity = Vector2.Zero;
                Died?.Invoke();
            }
            DamageTaken?.Invoke(actual);
        }

        public bool ReceiveContactDamage(int damage, bool sourceToRight)
        {
            if (IgnoresContact || damage < 0) return false;
            invulnerableMilliseconds = 2000; // Char::show_damage
            if (damage > 0 && State != PlayerState.Climbing) pendingContactKnockback = sourceToRight;
            if (damage == 0) DamageTaken?.Invoke(0);
            else TakeDamage(GuardContactDamage(damage));
            OnStatsChanged();
            return true;
        }

        public void Revive()
        {
            hp = maxHp; mp = maxMp;
            pendingContactKnockback = null;
            invulnerableMilliseconds = 2000;
            ResetMovementForMap();
            Velocity = Vector2.Zero;
        }

        public void AddExperience(long amount)
        {
            if (amount <= 0 || IsDead || level >= ExperienceTable.LevelCap) return;
            experience += Math.Min(amount, long.MaxValue - experience);
            while (level < ExperienceTable.LevelCap && experience >= ExperienceTable.RequiredForLevel(level))
            {
                experience -= ExperienceTable.RequiredForLevel(level);
                level++;
                AwardLevelProgression();
                // Local play restores current HP/MP. Server-owned HP/MP growth remains separate.
                hp = maxHp; mp = maxMp;
                LeveledUp?.Invoke(level);
            }
            if (level == ExperienceTable.LevelCap) experience = 0;
            ExperienceGained?.Invoke(amount);
        }

        public void Heal(int amount)
        {
            if (!IsDead && amount > 0) hp += Math.Min(amount, maxHp - hp);
        }

        public void UseMana(int amount)
        {
            mp = System.Math.Max(0, mp - amount);
        }

        public void RestoreMana(int amount)
        {
            mp = (int)System.Math.Min(maxMp, (long)mp + amount);
        }
        
        // For network synchronization
        public void SetHPMP(int newHp, int newMp)
        {
            hp = System.Math.Max(0, System.Math.Min(newHp, maxHp));
            mp = System.Math.Max(0, System.Math.Min(newMp, maxMp));
        }

        public bool UseItem(int itemId) => TryUseItem(itemId, out _);

        // Crouching methods
        public void Crouch(bool active)
        {
            crouchRequested = active;
        }

        // Compatibility commands queue intent; all movement changes occur on an 8 ms tick.
        public void DropThroughPlatform()
        {
            Crouch(true);
            Jump();
        }

        public void StartClimbing(LadderInfo ladder)
        {
            requestedLadder = ladder;
        }

        public void StopClimbing()
        {
            stopClimbingRequested = true;
        }

        public void ClimbUp(bool active)
        {
            isClimbingUp = active;
        }

        public void ClimbDown(bool active)
        {
            isClimbingDown = active;
        }

        public Dictionary<EquipSlot, int> GetEquippedItems()
        {
            return new Dictionary<EquipSlot, int>(equippedItems);
        }
        
        public void EquipItem(int itemId, EquipSlot slot)
        {
            if (itemData?.GetItem(itemId)?.EquipmentSlot == slot) TryEquipItem(itemId, out _);
        }
        public void UnequipItem(EquipSlot slot) => TryUnequipItem(slot, out _);

        // Update movement speeds based on character stats
        private void UpdateMovementSpeeds()
        {
            actualWalkSpeed = MaplePhysics.GetWalkSpeed(Speed);
            actualJumpPower = MaplePhysics.GetJumpPower(JumpPower);
        }
        
        // Call this when Speed or JumpPower stats change
        public void OnStatsChanged()
        {
            UpdateMovementSpeeds();
        }
        
        // Special movement methods
        public void TryStartClimbing(MapData mapData, bool upPressed)
        {
            isClimbingUp = upPressed;
            isClimbingDown = !upPressed;
            requestedLadder = mapData?.Ladders?.FirstOrDefault(l => l.CanEnter(Position, upPressed));
        }

        public void EnableDoubleJump(bool enabled)
        {
            hasDoubleJump = enabled;
        }
        
        public void EnableFlashJump(bool enabled)
        {
            hasFlashJump = enabled;
        }
        
        public int GetJumpCount()
        {
            return jumpCount;
        }
        
        public void SetJumpCount(int count)
        {
            jumpCount = count;
        }
        
        public float GetWalkSpeed()
        {
            return GetModifiedWalkSpeed();
        }
        
        public bool CanFlashJump()
        {
            return !IsBasicAttacking && hasFlashJump && flashJumpCooldown <= 0 && !IsGrounded && (isMovingLeft || isMovingRight);
        }
        
        public void FlashJump()
        {
            if (!CanFlashJump()) return;
            
            // Teleport horizontally in movement direction
            float teleportDirection = isMovingRight ? 1f : -1f;
            Position = new Vector2(Position.X + (FLASH_JUMP_DISTANCE * teleportDirection), Position.Y);
            
            // Apply boost to velocity
            Velocity = new Vector2(actualWalkSpeed * 2f * teleportDirection, MaplePhysics.JumpSpeed * 0.3f);
            
            State = PlayerState.FlashJumping;
            flashJumpCooldown = FLASH_JUMP_COOLDOWN;
            TriggerAnimationEvent(PlayerAnimationEvent.Jump);
        }
        
        // Movement modifier methods
        public void AddMovementModifier(IMovementModifier modifier)
        {
            if (modifier != null && !movementModifiers.Contains(modifier))
            {
                movementModifiers.Add(modifier);
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public void RemoveMovementModifier(IMovementModifier modifier)
        {
            if (movementModifiers.Remove(modifier))
            {
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public void RemoveMovementModifierById(string id)
        {
            int removed = movementModifiers.RemoveAll(m => m.Id == id);
            if (removed > 0)
            {
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public bool HasActiveMovementModifier()
        {
            return movementModifiers.Count > 0;
        }
        
        public float GetModifiedWalkSpeed()
        {
            float speed = NormalMovement.ToWorldSpeed(NormalMovement.WalkForce(Speed) * 5.0);
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsMovement) return 0f;
                speed *= modifier.SpeedMultiplier;
            }
            return speed;
        }
        
        public float GetModifiedJumpPower()
        {
            float power = NormalMovement.ToWorldSpeed(NormalMovement.JumpForce(JumpPower));
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsJumping) return 0f;
                power *= modifier.JumpMultiplier;
            }
            return power;
        }
        
        public void UpdateMovementModifiers(float deltaTime)
        {
            int countBefore = movementModifiers.Count;
            movementModifiers.RemoveAll(modifier => !modifier.Update(deltaTime));
            if (movementModifiers.Count != countBefore)
            {
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public List<IMovementModifier> GetActiveModifiers()
        {
            return new List<IMovementModifier>(movementModifiers);
        }
        
        private void UpdateEnvironmentalEffects(MapData mapData)
        {
            if (mapData == null) return;
            
            // Remove existing environmental modifiers
            RemoveMovementModifierById("slippery_surface");
            RemoveMovementModifierById("conveyor_belt");
            RemoveMovementModifierById("swimming");
            
            // Water is a source physics/state transition, not a Speed/Jump multiplier.
            
            // Check current platform for special properties
            if (IsGrounded && footholdService == null)
            {
                // Only use platform-based environmental effects when no foothold service
                var platform = GetCurrentPlatform(mapData);
                if (platform != null)
                {
                    if (platform.IsSlippery)
                    {
                        AddMovementModifier(new SlipperyModifier());
                    }
                    if (platform.IsConveyor)
                    {
                        var conveyorMod = new ConveyorModifier(platform.ConveyorSpeed);
                        AddMovementModifier(conveyorMod);
                        // Apply conveyor velocity
                        Velocity = new Vector2(Velocity.X + platform.ConveyorSpeed, Velocity.Y);
                    }
                }
            }
            else if (IsGrounded && footholdService != null)
            {
                // Use foothold-based environmental effects
                Vector2 maplePos = MaplePhysicsConverter.UnityToMaple(new Vector2(Position.X, Position.Y - PLAYER_HEIGHT / 2));
                var foothold = footholdService.GetFootholdAt(maplePos.X, maplePos.Y);
                if (foothold != null)
                {
                    if (foothold.IsSlippery)
                    {
                        AddMovementModifier(new SlipperyModifier());
                    }
                    if (foothold.IsConveyor)
                    {
                        var conveyorMod = new ConveyorModifier(foothold.ConveyorSpeed / 100f); // Convert to Unity units
                        AddMovementModifier(conveyorMod);
                        // Apply conveyor velocity
                        Velocity = new Vector2(Velocity.X + foothold.ConveyorSpeed / 100f, Velocity.Y);
                    }
                }
            }
        }
        
        // IPhysicsObject implementation
        public void OnTerrainCollision(Vector2 collisionPoint, Vector2 collisionNormal)
        {
            // Handle terrain collision
            // For now, this is handled in UpdatePhysics, but we can expand this later
            // for more complex collision responses
        }
        
        // View listener methods
        public void AddViewListener(IPlayerViewListener listener)
        {
            if (listener != null && !viewListeners.Contains(listener))
            {
                viewListeners.Add(listener);
            }
        }
        
        public void RemoveViewListener(IPlayerViewListener listener)
        {
            viewListeners.Remove(listener);
        }
        
        private void NotifyViewListeners(Action<IPlayerViewListener> action)
        {
            foreach (var listener in viewListeners)
            {
                action(listener);
            }
        }
        
        public PhysicalAttackStats PhysicalAttackStats => PhysicalAttackStats.Calculate(
            equippedItems.TryGetValue(EquipSlot.Weapon, out int weaponId) ? weaponId : 0,
            JobId, STR, DEX, INT, LUK, WeaponAttack, AccuracyBonus, Level, CombatMastery, CombatDamagePercent,
            CriticalChance, State == PlayerState.Crouching, criticalDamageMultiplier: CriticalDamageMultiplier);
        public void ClearPracticeDamageOverride() => HasPracticeDamageOverride = false;

        public BasicAttackMotion BasicAttack { get; private set; }
        public bool IsBasicAttacking => BasicAttack != null && !BasicAttack.IsComplete;
        public bool PlayBasicAttackAnimation(int choice = 0)
        {
            if (IsDead || IsHidden || IsBasicAttacking || State == PlayerState.Climbing) return false;
            BasicAttack = CurrentWeapon == null ? BasicAttackMotion.Unarmed() : CurrentWeapon.CreateAttack(choice, State == PlayerState.Crouching, AttackSpeedModifier);
            if (BasicAttack == null) return false;
            BasicAttack.FacingRight = FacingRight ?? true;
            BasicAttack.Completed += FinishBasicAttack;
            if (CurrentWeapon != null) BasicAttack.TickAdvance = () => Math.Max(1, (int)(8 * (1.7f - EffectiveAttackSpeed / 10f)));
            TriggerAnimationEvent(PlayerAnimationEvent.Attack);
            return true;
        }

        private void FinishBasicAttack()
        {
            StanceAnimation.Reset();
            bool down = crouchRequested || isClimbingDown;
            if (normalMovement != null)
            {
                normalMovement.FinishAttack(isMovingLeft, isMovingRight, down);
                // Stage checks ladder entry after the attack animation ends on this tick.
                if (!movementModifiers.Any(m => m.PreventsMovement) && !stopClimbingRequested)
                    normalMovement.TryEnterLadder(isClimbingUp, down, movementMap?.Ladders);
                bool wasUpdating = updatingPhysics;
                updatingPhysics = true;
                try { PublishNormalMovement(false); }
                finally { updatingPhysics = wasUpdating; }
            }
            else
            {
                if (IsGrounded && (isMovingLeft || isMovingRight)) FacingRight = !isMovingLeft;
                State = !IsGrounded ? PlayerState.Falling : isMovingLeft || isMovingRight ? PlayerState.Walking :
                    down ? PlayerState.Crouching : PlayerState.Standing;
            }
            SynchronizeBodyStance();
        }

        private void TriggerAnimationEvent(PlayerAnimationEvent animEvent)
        {
            NotifyViewListeners(l => l.OnAnimationEvent(animEvent));
        }
        
        // Public helper methods for platform queries
        public bool IsOnOneWayPlatform(MapData mapData)
        {
            if (!IsGrounded || mapData == null) return false;
            
            var platform = GetPlatformBelow(Position, mapData);
            return platform != null && platform.Type == PlatformType.OneWay;
        }
        
        public Platform GetCurrentPlatform(MapData mapData)
        {
            if (!IsGrounded || mapData == null) return null;
            return GetPlatformBelow(Position, mapData);
        }
    }
}
