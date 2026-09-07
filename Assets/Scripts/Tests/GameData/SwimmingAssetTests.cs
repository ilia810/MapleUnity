using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class SwimmingAssetTests
    {
        [TestCase(230000000,true)][TestCase(230010000,true)][TestCase(100000000,false)][TestCase(103000000,false)]
        public void MapsImportTheirSourceUnderwaterFlag(int id,bool expected)
        {
            var source=NXDataManagerSingleton.Instance.GetMapNode(id);
            Assert.That((source["info"]?["swim"]?.GetValue<int>()??0)!=0,Is.EqualTo(expected));
            Assert.That(new NxMapLoader().GetMap(id).IsUnderwater,Is.EqualTo(expected));
        }
        [Test] public void SwimPoseHasTwoRealBodyFramesWithSupportedSourceDelays()
        {
            var data=NXDataManagerSingleton.Instance.DataManager;
            Assert.That(data.CharacterData.GetAnimationFrameCount(CharacterState.Fly),Is.EqualTo(2));
            for(int frame=0;frame<2;frame++)
            {
                var body=data.GetNode("character","00002000.img/fly/"+frame);
                Assert.That(body["delay"].GetValue<int>(),Is.GreaterThanOrEqualTo(8));
                Assert.That(((UnitySpriteData)data.CharacterData.GetBodySprite(0,CharacterState.Fly,frame)).UnitySprite,Is.Not.Null);
                Assert.That(((UnitySpriteData)data.CharacterData.GetHeadSprite(0,CharacterState.Fly,frame)).UnitySprite,Is.Not.Null);
            }
        }
    }
}
