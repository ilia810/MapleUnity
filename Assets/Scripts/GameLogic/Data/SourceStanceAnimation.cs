// Char/CharLook stance timing ported from the continued Journey client.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Data
{
    public interface IStanceDataProvider
    {
        IReadOnlyList<int> GetStanceDelays(CharacterState stance);
    }

    /// <summary>One CharLook body clock, advanced only by simulation. Named attacks own their existing clock.</summary>
    public sealed class SourceStanceAnimation
    {
        private IStanceDataProvider provider;
        private IReadOnlyList<int> delays;
        private int previousFrame;
        private float threshold;
        public CharacterState Stance { get; private set; } = CharacterState.Stand;
        public int Frame { get; private set; }
        public int ElapsedMilliseconds { get; private set; }

        public void SetData(IStanceDataProvider value)
        {
            if (ReferenceEquals(provider, value)) return;
            provider = value;
            delays = provider?.GetStanceDelays(Stance);
            Reset();
        }

        public void SetStance(CharacterState value)
        {
            // FALL and JUMP refer to the same source stance and retain its phase.
            if (value == CharacterState.Fall) value = CharacterState.Jump;
            if (Stance == value) return;
            Stance = value;
            delays = provider?.GetStanceDelays(value);
            Reset();
        }

        public void Reset()
        {
            Frame = previousFrame = ElapsedMilliseconds = 0;
            threshold = 0;
        }

        public void Advance(float speed)
        {
            // Char::update truncates 8 * speed, and permits zero at low speed.
            int step = Timestep(speed);
            previousFrame = Frame;
            if (step == 0) return;
            int delay = delays != null && Frame < delays.Count ? delays[Frame] : 100;
            if (delay <= 0) delay = 100;
            // Preserve CharLook's uint16 arithmetic and single advance per tick,
            // including unusually short authored frames (no while/catch-up loop).
            int remaining = unchecked((ushort)(delay - ElapsedMilliseconds));
            if (step >= remaining)
            {
                ElapsedMilliseconds = unchecked((ushort)(step - remaining));
                Frame = delays != null && Frame + 1 < delays.Count ? Frame + 1 : 0;
                threshold = (float)remaining / step;
            }
            else ElapsedMilliseconds = unchecked((ushort)(ElapsedMilliseconds + step));
        }

        public static int Timestep(float speed) => speed >= .125f && !float.IsInfinity(speed) ? (int)(8 * speed) : 0;

        public int Sample(float interpolation) => interpolation >= threshold ? Frame : previousFrame;
    }
}
