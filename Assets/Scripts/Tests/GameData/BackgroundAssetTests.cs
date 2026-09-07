using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Data;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class BackgroundAssetTests
    {
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void KerningLightsLoadBothBitmapFramesAndTheirFourSecondFade(int number)
        {
            var animation=NxBackgroundAnimation.Load(NXDataManagerSingleton.Instance.DataManager,"sunsetCity",number,true);
            Assert.That(animation.Sprites.Length,Is.EqualTo(2));Assert.That(animation.Zigzag,Is.False);
            for(int i=0;i<2;i++)
            {
                Assert.That(animation.Sprites[i],Is.Not.Null);
                Assert.That(animation.Sprites[i].name,Does.EndWith("/ani/"+number+"/"+i));
                Assert.That(animation.Frames[i].DelayMilliseconds,Is.EqualTo(4000));
            }
            Assert.That(animation.Frames[0].StartOpacity,Is.EqualTo(255));Assert.That(animation.Frames[0].EndOpacity,Is.EqualTo(170));
            var timeline=new SourceAnimation(animation.Frames,animation.Zigzag);timeline.AdvanceTo(250);
            Assert.That(timeline.Sample(1,out float alpha,out float scale),Is.Zero);
            Assert.That(alpha,Is.EqualTo(212.5f/255).Within(.00003f));Assert.That(scale,Is.EqualTo(1));
            timeline.AdvanceTo(500);Assert.That(timeline.Sample(0,out alpha,out scale),Is.EqualTo(1));
            Assert.That(alpha,Is.EqualTo(170f/255));
        }
        [Test] public void AquaFishUseFourFramesAndDefaultHundredMillisecondDelay()
        {
            var animation=NxBackgroundAnimation.Load(NXDataManagerSingleton.Instance.DataManager,"aquaRoad",16,true);
            Assert.That(animation.Sprites.Length,Is.EqualTo(4));
            Assert.That(animation.Sprites[2].pivot.x,Is.EqualTo(162).Within(.001));
            foreach(var frame in animation.Frames)Assert.That(frame.DelayMilliseconds,Is.EqualTo(100));
        }
        [Test] public void StaticCloudLoadsAsOneBitmapAndMissingAssetReturnsNull()
        {
            var manager=NXDataManagerSingleton.Instance.DataManager;
            Assert.That(NxBackgroundAnimation.Load(manager,"grassySoil",1,false).Sprites.Length,Is.EqualTo(1));
            Assert.That(NxBackgroundAnimation.Load(manager,"grassySoil",9999,true),Is.Null);
        }
    }
}
