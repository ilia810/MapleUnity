// Attack accuracy adapted from HeavenClient (continued Journey client).
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;

namespace MapleClient.GameLogic.Data
{
    /// <summary>Existing source attack accuracy rule, also used by local contact evasion.</summary>
    public static class AccuracyRules
    {
        public static float HitChance(int accuracy, int attackerLevel, int avoidability, int defenderLevel)
        {
            int delta = Math.Max(0, defenderLevel - attackerLevel);
            return Math.Max(.01f, accuracy / ((1.84f + .07f * delta) * avoidability + 1f));
        }
    }
}
