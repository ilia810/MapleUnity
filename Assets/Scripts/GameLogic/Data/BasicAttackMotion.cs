// Fixed-tick stance playback follows HeavenClient Char::update / CharLook::update.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Data
{
    /// <summary>One swing, shared by combat readiness and the character view.</summary>
    public sealed class BasicAttackMotion
    {
        private readonly int[] delays;
        private readonly int advancePerTick;
        private int elapsed;
        private double accumulator;
        private readonly CharacterState stance;
        private readonly CharacterState[] actionStances;
        private readonly int[] actionFrames;
        private readonly Vector2[] actionMoves;
        private readonly int[] hitDelays;
        private int frameIndex;
        public CharacterState Stance => actionStances == null ? stance : actionStances[frameIndex];
        public int Frame => actionFrames == null ? frameIndex : actionFrames[frameIndex];
        public Vector2 VisualOffset => actionMoves == null ? Vector2.Zero : actionMoves[frameIndex];
        public bool IsComplete { get; private set; }
        public bool IsCancelled { get; private set; }
        internal event Action Completed;
        internal Func<int> TickAdvance { get; set; }
        internal int AnimationStepMilliseconds => TickAdvance?.Invoke() ?? advancePerTick;
        public bool FacingRight { get; internal set; } = true;
        public WeaponAfterimage Afterimage { get; }
        public int HitDelayMilliseconds { get; }
        // BodyAction::getattackdelay returns zero for an absent marker. Ordinary
        // stances reuse their afterimage delay for every damage line.
        public int HitDelayForLine(int line) => hitDelays == null ? HitDelayMilliseconds :
            line >= 0 && line < hitDelays.Length ? hitDelays[line] : 0;
        public int ElapsedMilliseconds { get; private set; }
        public int AfterimageMilliseconds { get; private set; }

        public BasicAttackMotion(CharacterState stance, int[] frameDelays, int advancePerTick,
            WeaponAfterimage afterimage = null, int hitDelayMilliseconds = 0,
            CharacterState[] actionStances = null, int[] actionFrames = null, Vector2[] actionMoves = null, int[] hitDelays = null)
        {
            if (frameDelays == null || frameDelays.Length == 0) throw new ArgumentException("An attack needs frame delays.", nameof(frameDelays));
            if ((actionStances != null && actionStances.Length != frameDelays.Length) ||
                (actionFrames != null && actionFrames.Length != frameDelays.Length) ||
                (actionMoves != null && actionMoves.Length != frameDelays.Length)) throw new ArgumentException("Action frame counts must match delays.");
            this.stance = stance;
            this.actionStances = actionStances == null ? null : (CharacterState[])actionStances.Clone();
            this.actionFrames = actionFrames == null ? null : (int[])actionFrames.Clone();
            this.actionMoves = actionMoves == null ? null : (Vector2[])actionMoves.Clone();
            this.hitDelays = hitDelays == null ? null : (int[])hitDelays.Clone();
            Afterimage = afterimage;
            HitDelayMilliseconds = Math.Max(0, hitDelayMilliseconds);
            delays = (int[])frameDelays.Clone();
            for (int i = 0; i < delays.Length; i++) delays[i] = Math.Max(1, delays[i]);
            this.advancePerTick = Math.Max(1, advancePerTick);
        }
        public void Advance(float seconds)
        {
            if (IsComplete || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            accumulator += seconds;
            while (!IsComplete && accumulator + 1e-8 >= .008)
            {
                accumulator = Math.Max(0, accumulator - .008);
                ElapsedMilliseconds += 8;
                int advance = AnimationStepMilliseconds;
                // Char updates the afterimage before advancing the body stance.
                if (Afterimage != null && Frame >= Afterimage.FirstFrame) AfterimageMilliseconds += advance;
                elapsed += advance;
                while (!IsComplete && elapsed >= delays[frameIndex])
                {
                    elapsed -= delays[frameIndex];
                    if (frameIndex + 1 == delays.Length) IsComplete = true;
                    else frameIndex++;
                }
            }
            if (IsComplete) Completed?.Invoke();
        }
        public void Cancel() { IsComplete = true; IsCancelled = true; }
        // Preserve the existing local unarmed practice attack until unarmed combat is ported.
        public static BasicAttackMotion Unarmed() => new BasicAttackMotion(CharacterState.Attack1, new[] { 300, 300 }, 8);
    }
}
