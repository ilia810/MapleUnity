// CharLook/Face timing ported from the continued Journey client.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Data
{
    public interface IFaceDataProvider
    {
        FaceAnimationData GetFaceAnimation(int faceId);
    }

    public sealed class FaceAnimationData
    {
        private readonly Dictionary<CharacterExpression, int[]> delays = new Dictionary<CharacterExpression, int[]>();
        public int FaceId { get; }
        public FaceAnimationData(int faceId, IDictionary<CharacterExpression, int[]> frames)
        {
            FaceId = faceId;
            foreach (var pair in frames) delays[pair.Key] = (int[])pair.Value.Clone();
        }
        public int FrameCount(CharacterExpression expression) => delays.TryGetValue(expression, out var frames) ? frames.Length : 0;
        public int Delay(CharacterExpression expression, int frame) => delays.TryGetValue(expression, out var frames) && frame >= 0 && frame < frames.Length ? frames[frame] : 100;
        public int NextFrame(CharacterExpression expression, int frame) => frame + 1 < FrameCount(expression) ? frame + 1 : 0;
    }

    public sealed class SourceFaceAnimation
    {
        private FaceAnimationData data;
        private CharacterExpression previousExpression;
        private int previousFrame;
        private float expressionThreshold, frameThreshold;
        public CharacterExpression Expression { get; private set; } = CharacterExpression.Default;
        public int Frame { get; private set; }
        public int ElapsedMilliseconds { get; private set; }
        public int CooldownMilliseconds { get; private set; }
        public int FaceId => data?.FaceId ?? 20000;

        // CharLook::set_face replaces assets while keeping expression and phase.
        public void SetData(FaceAnimationData value) => data = value;
        public bool HasExpression(CharacterExpression expression) => data?.FrameCount(expression) > 0;
        public bool CanSetExpression(CharacterExpression expression) => expression != Expression && CooldownMilliseconds == 0;
        public bool TrySetExpression(CharacterExpression expression)
        {
            if (!CanSetExpression(expression)) return false;
            Expression = previousExpression = expression;
            Frame = previousFrame = ElapsedMilliseconds = 0;
            CooldownMilliseconds = 5000;
            return true;
        }

        public void Reset()
        {
            Expression = previousExpression = CharacterExpression.Default;
            Frame = previousFrame = ElapsedMilliseconds = CooldownMilliseconds = 0;
            expressionThreshold = frameThreshold = 0;
        }

        public void Advance(int timestep)
        {
            if (timestep <= 0)
            {
                previousExpression = Expression; previousFrame = Frame;
                return;
            }
            // TimedBool uses eight real simulation milliseconds, even when the
            // face frames are accelerated by movement/attack speed. A zero body
            // step returns before this cooldown update in the original CharLook.
            CooldownMilliseconds = Math.Max(0, CooldownMilliseconds - 8);
            if (data == null) return;
            int remaining = unchecked((ushort)(data.Delay(Expression, Frame) - ElapsedMilliseconds));
            if (timestep >= remaining)
            {
                ElapsedMilliseconds = unchecked((ushort)(timestep - remaining));
                previousFrame = Frame;
                Frame = data.NextFrame(Expression, Frame);
                frameThreshold = (float)remaining / timestep;
                if (Frame == 0)
                {
                    previousExpression = Expression;
                    Expression = Expression == CharacterExpression.Default ? CharacterExpression.Blink : CharacterExpression.Default;
                    expressionThreshold = frameThreshold;
                }
            }
            else
            {
                previousExpression = Expression; previousFrame = Frame;
                ElapsedMilliseconds = unchecked((ushort)(ElapsedMilliseconds + timestep));
            }
        }

        public CharacterExpression SampleExpression(float interpolation) => interpolation >= expressionThreshold ? Expression : previousExpression;
        public int SampleFrame(float interpolation) => interpolation >= frameThreshold ? Frame : previousFrame;
    }
}
