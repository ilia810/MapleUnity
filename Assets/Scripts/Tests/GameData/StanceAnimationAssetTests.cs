using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class StanceAnimationAssetTests
    {
        [TestCase(CharacterState.Stand)][TestCase(CharacterState.Stand2)]
        [TestCase(CharacterState.Walk)][TestCase(CharacterState.Walk2)]
        [TestCase(CharacterState.Jump)][TestCase(CharacterState.Prone)]
        [TestCase(CharacterState.Ladder)][TestCase(CharacterState.Rope)][TestCase(CharacterState.Fly)]
        public void OrdinaryPosesLoadEverySourceDelayAndRealBodyFrame(CharacterState stance)
        {
            var data = NXDataManagerSingleton.Instance.DataManager;
            var provider = data.CharacterData as IStanceDataProvider; Assert.That(provider, Is.Not.Null);
            var delays = provider.GetStanceDelays(stance);
            Assert.That(delays.Count, Is.EqualTo(data.CharacterData.GetAnimationFrameCount(stance)));
            Assert.That(provider.GetStanceDelays(stance), Is.SameAs(delays));
            for (int frame = 0; frame < delays.Count; frame++)
            {
                int source = data.GetNode("character", $"00002000.img/{CharacterStances.Name(stance)}/{frame}/delay")?.GetValue<int>() ?? 100;
                Assert.That(delays[frame], Is.EqualTo(source > 0 ? source : 100));
                Assert.That(((UnitySpriteData)data.CharacterData.GetBodySprite(0, stance, frame)).UnitySprite, Is.Not.Null);
            }
        }
    }
}
