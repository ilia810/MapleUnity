// Normal, drop-through and climbing physics ported from HeavenClient (continued Journey client).
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton.
// Source license: AGPL-3.0-or-later; see C++ source attribution in PROJECT_STATUS.md.
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameLogic.Core
{
    /// <summary>
    /// HeavenClient NORMAL and FIXATED movement. Positions are feet in Maple pixels (Y down);
    /// velocities are pixels per 8 ms tick. Keep doubles until crossing the view boundary.
    /// Source: Physics::move_normal, FootholdTree, PlayerStates, Ladder and Stage.
    /// </summary>
    public sealed class NormalMovement
    {
        public const double TickSeconds = 0.008;
        public const double PixelsPerUnit = 100;
        public enum Stance { Stand, Walk, Fall, Prone, Ladder, Rope, Swim }
        public double X, Y, HSpeed, VSpeed;
        public bool OnGround;
        public int FootholdId { get; private set; }
        public int FootholdLayer { get; private set; }
        public double Slope { get; private set; }
        private Stance state;
        private bool swimmingPhysics, leftWasDown, rightWasDown;
        public Stance State
        {
            get => state;
            set
            {
                state = value;
                // Prone has no initializer in HeavenClient: landing into it from
                // SWIM retains water physics until a stand/walk/fall transition.
                if (value == Stance.Swim) swimmingPhysics = true;
                else if (value != Stance.Prone) swimmingPhysics = false;
            }
        }
        // Snapshot at Char::update, before PlayerState::update_state and Stage ladder entry.
        public Stance AnimationState { get; private set; }
        public float AnimationSpeed { get; private set; }
        public bool Jumped { get; private set; }
        public bool Landed { get; private set; }
        public int FacingDirection { get; private set; }
        public bool Dropped { get; private set; }
        public bool CanDrop { get; private set; }
        public double GroundBelow { get; private set; }
        public LadderInfo CurrentLadder { get; private set; }
        public bool IsClimbing => CurrentLadder != null;
        public int ClimbCooldownMilliseconds { get; private set; }
        private bool checkBelow;
        private NormalTerrain terrain;
        private double verticalForce;
        private bool turnAtEdges;
        public bool TurnedAtEdge { get; private set; }

        // Mob::update supplies force directly, without the player's stance/input rules.
        public void StepGroundActor(double horizontalForce)
        {
            TurnedAtEdge = false;
            turnAtEdges = true;
            UpdateFoothold();
            MoveNormal(horizontalForce, 1);
            X += HSpeed;
            Y += VSpeed;
            turnAtEdges = false;
        }

        public static float WalkForce(int speed) => 0.05f + 0.11f * speed / 100;
        public static float JumpForce(int jump) => 1.0f + 3.5f * jump / 100;
        public static double ToTickSpeed(float unitsPerSecond) => unitsPerSecond * (PixelsPerUnit * TickSeconds);
        public static float ToWorldSpeed(double pixelsPerTick) => (float)(pixelsPerTick / (PixelsPerUnit * TickSeconds));

        public void SetTerrain(NormalTerrain value)
        {
            if (ReferenceEquals(terrain, value)) return;
            terrain = value;
            FootholdId = 0;
            FootholdLayer = 0;
            CanDrop = false;
            checkBelow = false;
        }

        public void Teleport(double x, double y)
        {
            X = x; Y = y; FootholdId = 0; FootholdLayer = 0;
            verticalForce = 0;
            CanDrop = false; checkBelow = false;
            if (IsClimbing) State = Stance.Fall;
            CurrentLadder = null; ClimbCooldownMilliseconds = 0;
        }

        // Player::damage: the vertical impulse is consumed by the next ground physics step.
        public void ApplyContactKnockback(bool sourceToRight)
        {
            if (IsClimbing) return;
            HSpeed = sourceToRight ? -1.5 : 1.5;
            verticalForce -= 3.5;
        }

        public void CancelClimbing()
        {
            if (!IsClimbing) return;
            CurrentLadder = null;
            State = Stance.Fall;
            ClimbCooldownMilliseconds = 1000;
        }

        public void Step(bool left, bool right, bool down, bool jump, float walkForce, float jumpForce,
            float frictionMultiplier = 1, bool up = false, bool jumpHeld = false,
            float climbForce = 1, IEnumerable<LadderInfo> ladders = null, bool allowClimb = true, bool? jumpDown = null, bool attacking = false,
            bool underwater = false, float swimForce = .25f, int? swimFacingChange = null)
        {
            Jumped = false;
            Landed = false;
            Dropped = false;
            bool wasGrounded = OnGround;
            if (!underwater && State == Stance.Swim) State = Stance.Fall;
            // FlyState changes facing on key-down, including a press released before
            // the tick. Both held directions still apply leftward force below.
            if (!attacking && State == Stance.Swim)
            {
                if (swimFacingChange.HasValue)
                {
                    if (swimFacingChange.Value != 0) FacingDirection = swimFacingChange.Value;
                }
                else
                {
                    if (left && !leftWasDown) FacingDirection = -1;
                    if (right && !rightWasDown) FacingDirection = 1;
                }
            }
            leftWasDown = left; rightWasDown = right;
            // send_action precedes the update's state lookup. Walking and prone
            // support down-jump; a standing jump remains a regular jump.
            if (!attacking && jump && (State == Stance.Stand || State == Stance.Walk))
                verticalForce = -jumpForce;
            // ProneState::send_action permits dropping during a prone stab. Its state
            // setter remains locked, while the feet move below the platform.
            if (jump && (jumpDown ?? down) && CanDrop && ((!attacking && State == Stance.Walk) || State == Stance.Prone))
            {
                Y = GroundBelow;
                if (!attacking) State = Stance.Fall;
                Dropped = true;
            }
            Stance before = State;
            bool leftOnly = left && !right, rightOnly = right && !left;
            bool walkingInput = left || right;
            double horizontalForce = 0;

            if (!CanDrop && (before == Stance.Stand || before == Stance.Walk || before == Stance.Prone))
                checkBelow = true;
            // PlayerStates suppress input force; Player setters suppress stance/facing.
            // Physics still consumes momentum, gravity and contact impulses.
            if (!attacking) switch (before)
            {
                case Stance.Stand:
                    if (leftOnly || rightOnly)
                    {
                        State = Stance.Walk;
                        FacingDirection = rightOnly ? 1 : -1;
                    }
                    if (down && !up && !walkingInput) State = Stance.Prone;
                    break;
                case Stance.Walk:
                    if (leftOnly || rightOnly)
                    {
                        horizontalForce = rightOnly ? walkForce : -walkForce;
                        FacingDirection = rightOnly ? 1 : -1;
                    }
                    else if (!walkingInput && down) State = Stance.Prone;
                    break;
                case Stance.Fall:
                    // Source permits braking in air, not accelerating from rest.
                    if (leftOnly && HSpeed > 0) HSpeed -= 0.025;
                    else if (rightOnly && HSpeed < 0) HSpeed += 0.025;
                    if (leftOnly || rightOnly) FacingDirection = rightOnly ? 1 : -1;
                    break;
                case Stance.Prone:
                    if (up || !down) State = Stance.Stand;
                    if (left) { State = Stance.Walk; FacingDirection = -1; }
                    if (right) { State = Stance.Walk; FacingDirection = 1; }
                    break;
                case Stance.Swim:
                    if (left) horizontalForce = -swimForce;
                    else if (right) horizontalForce = swimForce;
                    if (up) verticalForce = -swimForce;
                    else if (down) verticalForce = swimForce;
                    break;
                case Stance.Ladder:
                case Stance.Rope:
                    VSpeed = up && !down ? -climbForce : down && !up ? climbForce : 0;
                    // ClimbState checks the held key, including a jump held before grabbing.
                    if (jumpHeld && walkingInput)
                    {
                        HSpeed = left ? -walkForce * 8.0 : walkForce * 8.0;
                        VSpeed = -jumpForce / 1.5;
                        FacingDirection = right ? 1 : -1;
                        CancelClimbing();
                        Jumped = true;
                    }
                    break;
            }

            UpdateFoothold();
            if (!IsClimbing)
            {
                if (swimmingPhysics) MoveSwimming(horizontalForce);
                else MoveNormal(horizontalForce, frictionMultiplier);
            }
            X += HSpeed;
            Y += VSpeed;

            AnimationState = State;
            AnimationSpeed = State == Stance.Walk ? (float)Math.Abs(HSpeed) :
                State == Stance.Ladder || State == Stance.Rope ? (float)Math.Abs(VSpeed) : 1f;

            // Source updates stance using the pre-move foothold contact.
            if (!attacking)
            {
                if (before == Stance.Stand || before == Stance.Walk)
                {
                    if (!OnGround) State = Stance.Fall;
                    else if (before == Stance.Walk && (!walkingInput || HSpeed == 0)) State = Stance.Stand;
                }
                else if (before == Stance.Fall)
                {
                    if (OnGround) State = down && !walkingInput ? Stance.Prone : Stance.Stand;
                    else if (underwater) State = Stance.Swim;
                }
                else if (before == Stance.Swim && OnGround && underwater)
                {
                    State = walkingInput ? Stance.Walk : down ? Stance.Prone : Stance.Stand;
                    if (walkingInput) FacingDirection = left ? -1 : 1;
                }
                else if ((before == Stance.Ladder || before == Stance.Rope) &&
                         CurrentLadder != null && CurrentLadder.FellOff(Y, down))
                    CancelClimbing();
            }

            // Player::update decrements the timer, then Stage::update checks entry.
            ClimbCooldownMilliseconds = Math.Max(0, ClimbCooldownMilliseconds - 8);
            if (!attacking && allowClimb) TryEnterLadder(up, down, ladders);
            Landed = !wasGrounded && OnGround;
        }

        // PlayerNullState selects from current contact/held keys on the final attack tick.
        // Left has priority here even when both horizontal keys are held.
        public void FinishAttack(bool left, bool right, bool down)
        {
            if (OnGround)
            {
                State = left || right ? Stance.Walk : down ? Stance.Prone : Stance.Stand;
                if (left || right) FacingDirection = left ? -1 : 1;
            }
            else State = IsClimbing ? (CurrentLadder.IsLadder ? Stance.Ladder : Stance.Rope) : Stance.Fall;
            checkBelow = false;
        }

        public void TryEnterLadder(bool up, bool down, IEnumerable<LadderInfo> ladders)
        {
            if (IsClimbing || ClimbCooldownMilliseconds != 0 || ladders == null || (!up && !down)) return;
            foreach (var ladder in ladders)
            {
                if (!ladder.InRange(X, Y, up && !down)) continue;
                CurrentLadder = ladder;
                FootholdLayer = 7;
                X = ladder.SourceX;
                HSpeed = 0; VSpeed = 0;
                State = ladder.IsLadder ? Stance.Ladder : Stance.Rope;
                break;
            }
        }

        private void MoveNormal(double horizontalForce, float frictionMultiplier)
        {
            double hacc = 0, vacc = 0;
            if (OnGround)
            {
                hacc = horizontalForce;
                vacc = verticalForce;
                Jumped |= verticalForce < 0;
                if (hacc == 0 && HSpeed < 0.1 && HSpeed > -0.1)
                    HSpeed = 0;
                else
                {
                    double inertia = HSpeed / 3.0;
                    double slope = Math.Max(-0.5, Math.Min(0.5, Slope));
                    hacc -= (0.5 + 0.1 * (1 + slope * -inertia)) * inertia * frictionMultiplier;
                }
            }
            else vacc = 0.14;
            verticalForce = 0;
            HSpeed += hacc;
            VSpeed += vacc;
            LimitMovement();
        }

        private void MoveSwimming(double horizontalForce)
        {
            // Physics::move_swimming: constant input force, drag on both axes,
            // and downward buoyancy drift. Speed/Jump affect only ground movement.
            double hacc = horizontalForce - .08 * HSpeed;
            double vacc = verticalForce - .08 * VSpeed + .03;
            verticalForce = 0;
            HSpeed += hacc;
            VSpeed += vacc;
            if (hacc == 0 && HSpeed < .1 && HSpeed > -.1) HSpeed = 0;
            if (vacc == 0 && VSpeed < .1 && VSpeed > -.1) VSpeed = 0;
            LimitMovement();
        }

        private void UpdateFoothold()
        {
            if (IsClimbing && FootholdId > 0) return;
            if (terrain == null || terrain.Count == 0)
            {
                OnGround = false;
                return;
            }
            var current = terrain.Get(FootholdId);
            bool checkSlope = false;
            if (OnGround)
            {
                if (current != null)
                {
                    if (Math.Floor(X) > NormalTerrain.Right(current)) FootholdId = current.NextId;
                    else if (Math.Ceiling(X) < NormalTerrain.Left(current)) FootholdId = current.PreviousId;
                }
                if (FootholdId == 0) FootholdId = terrain.Below(X, Y);
                else checkSlope = true;
            }
            else FootholdId = terrain.Below(X, Y);

            var next = terrain.Get(FootholdId);
            if (next == null) { OnGround = false; return; }
            Slope = NormalTerrain.Slope(next);
            double ground = NormalTerrain.Ground(next, X);
            if (VSpeed == 0 && checkSlope)
            {
                double delta = Math.Abs(Slope);
                if (Slope < 0) delta *= ground - Y;
                else if (Slope > 0) delta *= Y - ground;
                if (NormalTerrain.Slope(current) != 0 || Slope != 0)
                {
                    if ((HSpeed > 0 && delta <= HSpeed) || (HSpeed < 0 && delta >= HSpeed))
                        Y = ground;
                }
            }
            OnGround = Y == ground;
            if (FootholdLayer == 0 || OnGround) FootholdLayer = next.Layer;
            if (CanDrop || checkBelow)
            {
                var below = terrain.Get(terrain.Below(X, ground + 1));
                CanDrop = below != null && NormalTerrain.Ground(below, X) - ground < 600;
                if (below != null) GroundBelow = ground + 1;
                checkBelow = false;
            }
        }

        private void LimitMovement()
        {
            if (terrain == null || terrain.Count == 0) return;
            if (HSpeed != 0)
            {
                double wall = terrain.Wall(FootholdId, HSpeed < 0, Y + VSpeed);
                bool hit = HSpeed < 0 ? X >= wall && X + HSpeed <= wall : X <= wall && X + HSpeed >= wall;
                if (!hit && turnAtEdges)
                {
                    wall = terrain.Edge(FootholdId, HSpeed < 0);
                    hit = HSpeed < 0 ? X >= wall && X + HSpeed <= wall : X <= wall && X + HSpeed >= wall;
                }
                if (hit) { X = wall; HSpeed = 0; TurnedAtEdge = turnAtEdges; }
            }
            if (VSpeed == 0) return;
            var foothold = terrain.Get(FootholdId);
            if (foothold != null && !NormalTerrain.IsWall(foothold) &&
                Y <= NormalTerrain.Ground(foothold, X) && Y + VSpeed >= NormalTerrain.Ground(foothold, X + HSpeed))
            {
                Y = NormalTerrain.Ground(foothold, X + HSpeed);
                VSpeed = 0;
                // Vertical contact can change which connected wall blocks the body.
                LimitMovement();
            }
            else if (Y + VSpeed < terrain.Top) { Y = terrain.Top; VSpeed = 0; }
            else if (Y + VSpeed > terrain.Bottom) { Y = terrain.Bottom; VSpeed = 0; }
        }
    }

    /// <summary>Source foothold topology and NORMAL collision queries; immutable per map load.</summary>
    public sealed class NormalTerrain
    {
        private readonly Dictionary<int, Foothold> byId;
        private readonly List<Foothold> floors;
        public int Count => byId.Count;
        public bool HasGround => floors.Count != 0;
        public double LeftWall { get; }
        public double RightWall { get; }
        public double Top { get; }
        public double Bottom { get; }

        public NormalTerrain(IEnumerable<Foothold> footholds)
        {
            byId = new Dictionary<int, Foothold>();
            floors = new List<Foothold>();
            foreach (var fh in footholds)
            {
                if (byId.ContainsKey(fh.Id)) continue; // C++ emplace preserves the first ID.
                byId.Add(fh.Id, fh);
                if (!IsWall(fh)) floors.Add(fh);
            }
            if (byId.Count == 0)
            {
                LeftWall = double.NegativeInfinity; RightWall = double.PositiveInfinity;
                Top = double.NegativeInfinity; Bottom = double.PositiveInfinity;
                return;
            }
            double left = byId.Values.Min(Left), right = byId.Values.Max(Right);
            double top = byId.Values.Min(f => Math.Min(f.Y1, f.Y2));
            double bottom = byId.Values.Max(f => Math.Max(f.Y1, f.Y2));
            // A map with one flat floor has equal raw top/bottom. Its margins
            // still define valid bounds; do not copy the C++ debug fallback to Y=0.
            if (left >= right) { left = -1000; right = 1000; }
            double margin = Math.Min(25, (right - left) / 2);
            LeftWall = left + margin; RightWall = right - margin;
            Top = top - 300; Bottom = bottom + 100;
        }

        public Foothold Get(int id) => byId.TryGetValue(id, out var fh) ? fh : null;
        public static bool IsWall(Foothold f) => f.IsWall || f.X1 == f.X2;
        public static double Left(Foothold f) => Math.Min(f.X1, f.X2);
        public static double Right(Foothold f) => Math.Max(f.X1, f.X2);
        public static double Slope(Foothold f) => f == null || IsWall(f) ? 0 : (double)(f.Y2 - f.Y1) / (f.X2 - f.X1);
        public static double Ground(Foothold f, double x) => f.Y1 + Slope(f) * (x - f.X1);

        public int Below(double x, double y)
        {
            int id = 0;
            double closest = Bottom;
            int column = (int)x; // Source spatial index truncates towards zero.
            foreach (var f in floors)
            {
                if (column < Left(f) || column > Right(f)) continue;
                double ground = Ground(f, x);
                // The source equal-key bucket visits later insertions first and
                // overwrites equal heights. Keep the first authored surface on a tie.
                if (ground >= y && (id == 0 ? ground <= closest : ground < closest))
                    { closest = ground; id = f.Id; }
            }
            return id;
        }

        public bool TryFindSpawn(double x, double y, out Vector2 position)
        {
            position = Vector2.Zero;
            if (!HasGround || double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) return false;
            x = Math.Max(LeftWall, Math.Min(RightWall, x));
            var floor = Get(Below(x, y));
            if (floor == null)
            {
                // A missing portal column must not create an imaginary ground plane.
                // Fall back to the closest real floor inside the movement boundaries.
                double distance = double.PositiveInfinity, bestX = 0;
                foreach (var candidate in floors)
                {
                    double left = Math.Max(Left(candidate), LeftWall);
                    double right = Math.Min(Right(candidate), RightWall);
                    if (left > right) continue;
                    double candidateX = Math.Max(left, Math.Min(right, x));
                    double dx = candidateX - x, dy = Ground(candidate, candidateX) - y;
                    double squared = dx * dx + dy * dy;
                    if (squared >= distance) continue;
                    distance = squared; floor = candidate; bestX = candidateX;
                }
                x = bestX;
            }
            if (floor == null) return false;
            // Physics::get_y_below puts source feet one pixel above the floor.
            position = new Vector2((float)(x / 100), (float)(-(Ground(floor, x) - 1) / 100) + Player.Height / 2);
            return true;
        }

        public double Wall(int id, bool left, double feetY)
        {
            var current = Get(id);
            if (current != null)
            {
                var adjacent = Get(left ? current.PreviousId : current.NextId);
                if (Blocks(adjacent, feetY)) return left ? Left(current) : Right(current);
                var next = adjacent == null ? null : Get(left ? adjacent.PreviousId : adjacent.NextId);
                if (Blocks(next, feetY)) return left ? Left(adjacent) : Right(adjacent);
            }
            return left ? LeftWall : RightWall;
        }

        private static bool Blocks(Foothold f, double feetY)
        {
            if (f == null || !IsWall(f)) return false;
            int feet = (int)feetY;
            return Math.Max(f.Y1, f.Y2) >= feet - 50 && Math.Min(f.Y1, f.Y2) <= feet - 1;
        }

        // FootholdTree::get_edge checks the current and next linked segment.
        public double Edge(int id, bool left)
        {
            var current = Get(id);
            if (current == null) return left ? LeftWall : RightWall;
            int adjacentId = left ? current.PreviousId : current.NextId;
            if (adjacentId == 0) return left ? Left(current) : Right(current);
            var adjacent = Get(adjacentId);
            if (adjacent != null && (left ? adjacent.PreviousId : adjacent.NextId) == 0)
                return left ? Left(adjacent) : Right(adjacent);
            return left ? LeftWall : RightWall;
        }
    }
}
