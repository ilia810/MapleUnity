// Weapon rules and attack stance pools ported from HeavenClient WeaponData/CharLook.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Data
{
    public sealed class WeaponProfile
    {
        public int AttackType { get; set; }
        public int AttackSpeed { get; set; }
        public bool IsTwoHanded { get; set; }
        public CharacterState Stand { get; set; } = CharacterState.Stand;
        public CharacterState Walk { get; set; } = CharacterState.Walk;
        public Dictionary<CharacterState, int[]> FrameDelays { get; } = new Dictionary<CharacterState, int[]>();
        public Dictionary<CharacterState, WeaponAfterimage> Afterimages { get; } = new Dictionary<CharacterState, WeaponAfterimage>();
        public CharacterState[] ActionStances { get; set; }
        public int[] ActionFrames { get; set; }
        public int[] ActionDelays { get; set; }
        public Vector2[] ActionMoves { get; set; }
        public int ActionHitDelay { get; set; }

        public static bool UsesTwoHands(int id)
        {
            int type = id / 10000;
            return type == 138 || (type >= 140 && type <= 144) || type == 146;
        }

        public static bool SupportsRanged(int id) => AmmunitionRules.Prefix(id / 10000) != 0;
        // Knuckle actions and the remaining skill actions are separate ports.
        public static bool SupportsMelee(int id)
        {
            int type = id / 10000;
            return (type >= 130 && type <= 133) || type == 137 || type == 138 || (type >= 140 && type <= 144);
        }

        public static IReadOnlyList<CharacterState> AttackStances(int attackType)
        {
            switch (attackType) {
                case 1: return OneHanded;
                case 2: return Polearms;
                case 5: return TwoHanded;
                case 6: return Wands;
                case 3: return Bows;
                case 4: return Crossbows;
                case 7: return Wands; // Claws share swingO1/swingO2 in CharLook.
                case 9: return Guns;
                default: return Array.Empty<CharacterState>();
            }
        }
        private static readonly IReadOnlyList<CharacterState> OneHanded = Array.AsReadOnly(new[] {
            CharacterState.Attack1, CharacterState.StabO2, CharacterState.Attack2, CharacterState.SwingO2, CharacterState.SwingO3 });
        private static readonly IReadOnlyList<CharacterState> TwoHanded = Array.AsReadOnly(new[] {
            CharacterState.Attack1, CharacterState.StabO2, CharacterState.SwingT1, CharacterState.SwingT2, CharacterState.SwingT3 });
        private static readonly IReadOnlyList<CharacterState> Polearms = Array.AsReadOnly(new[] { CharacterState.StabT1, CharacterState.SwingP1 });
        private static readonly IReadOnlyList<CharacterState> Wands = Array.AsReadOnly(new[] { CharacterState.Attack2, CharacterState.SwingO2 });
        private static readonly IReadOnlyList<CharacterState> Bows = Array.AsReadOnly(new[] { CharacterState.Shoot1 });
        private static readonly IReadOnlyList<CharacterState> Crossbows = Array.AsReadOnly(new[] { CharacterState.Shoot2 });
        private static readonly IReadOnlyList<CharacterState> Guns = Array.AsReadOnly(new[] { CharacterState.Shot });

        public bool UsesTwoHandedDrawOrder(CharacterState stance) =>
            stance == CharacterState.Stand2 || stance == CharacterState.Walk2 ||
            (stance != CharacterState.Stand && stance != CharacterState.Walk && IsTwoHanded);

        public BasicAttackMotion CreateAttack(int choice, bool prone, int speedModifier = 0)
        {
            if (AttackType == 9 && !prone)
            {
                if (ActionDelays == null || ActionDelays.Length == 0) return null;
                var timing = new SourceAttackTiming(AttackSpeed + speedModifier, ActionHitDelay);
                Afterimages.TryGetValue(ActionStances[0], out var trail);
                return new BasicAttackMotion(ActionStances[0], ActionDelays, timing.AdvancePerTick, trail,
                    timing.HitDelayMilliseconds, ActionStances, ActionFrames, ActionMoves);
            }
            var stances = AttackStances(AttackType);
            if (stances.Count == 0) return null;
            var stance = prone ? CharacterState.ProneStab : stances[(int)((uint)choice % stances.Count)];
            if (!FrameDelays.TryGetValue(stance, out var delays) || delays.Length == 0) return null;
            Afterimages.TryGetValue(stance, out var afterimage);
            int hitDelay = 0;
            if (afterimage != null)
                for (int frame = 0; frame < Math.Min(afterimage.FirstFrame, delays.Length); frame++) hitDelay += delays[frame];
            // Char::get_attackdelay uses the floating speed, independently of the
            // truncated stance clock. Damage can precede the first trail by a tick.
            var sourceTiming = new SourceAttackTiming(AttackSpeed + speedModifier, hitDelay);
            return new BasicAttackMotion(stance, delays, sourceTiming.AdvancePerTick, afterimage, sourceTiming.HitDelayMilliseconds);
        }
    }
}
