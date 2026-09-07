using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Data
{
    public static class CharacterExpressions
    {
        // Explicit mapping keeps existing Unity enum values while preserving source action IDs.
        public static IReadOnlyList<CharacterExpression> SourceOrder { get; } = Array.AsReadOnly(new[] {
            CharacterExpression.Default, CharacterExpression.Blink, CharacterExpression.Hit, CharacterExpression.Smile,
            CharacterExpression.Troubled, CharacterExpression.Cry, CharacterExpression.Angry, CharacterExpression.Bewildered,
            CharacterExpression.Stunned, CharacterExpression.Blaze, CharacterExpression.Bowing, CharacterExpression.Cheers,
            CharacterExpression.Chu, CharacterExpression.Dam, CharacterExpression.Despair, CharacterExpression.Glitter,
            CharacterExpression.Hot, CharacterExpression.Hum, CharacterExpression.Love, CharacterExpression.Oops,
            CharacterExpression.Pain, CharacterExpression.Shine, CharacterExpression.Vomit, CharacterExpression.Wink
        });
        public static string Name(CharacterExpression expression) => expression.ToString().ToLowerInvariant();
        public static CharacterExpression? ForFunctionKey(int number) => number >= 1 && number <= 7
            ? (CharacterExpression?)SourceOrder[number + 1] : null;
    }
}
