using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.GameLogic
{
    public class BackgroundAnimationTests
    {
        private static SourceAnimationFrame Frame(int delay,float a0,float a1,float z0,float z1) =>
            new SourceAnimationFrame {DelayMilliseconds=delay,StartOpacity=a0,EndOpacity=a1,StartScale=z0,EndScale=z1};
        private static SourceAnimation Create(int id)
        {
            var frames = id<2 ? new[]{Frame(10,255,100,100,200),Frame(17,100,255,200,100),Frame(24,255,255,100,100)} :
                id<4 ? new[]{Frame(1,0,255,100,0),Frame(3,255,0,0,100),Frame(5,50,200,50,150)} :
                id<6 ? new[]{Frame(100,255,0,100,0)} : new[]{Frame(4000,255,170,100,100),Frame(4000,170,255,100,100)};
            return new SourceAnimation(frames,id%2!=0);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void MatchesCompiledHeavenAnimationAtTickAndInterpolationBoundaries(int id)
        {
            var timeline=Create(id);int count=0;
            foreach(string line in File.ReadLines(Path.Combine(Application.dataPath,"Scripts/Tests/GameLogic/Fixtures/HeavenBackgroundAnimation.csv")).Skip(1))
            {
                var p=line.Split(',');if(int.Parse(p[0])!=id)continue;
                long ticks=long.Parse(p[1]);float t=float.Parse(p[2],CultureInfo.InvariantCulture);
                timeline.AdvanceTo(ticks);int frame=timeline.Sample(t,out float alpha,out float size);
                string at=$"case {id}, tick {ticks}, interpolation {t}";
                Assert.That(frame,Is.EqualTo(int.Parse(p[3])),at);
                Assert.That(alpha,Is.EqualTo(float.Parse(p[4],CultureInfo.InvariantCulture)).Within(.000025f),at);
                Assert.That(size,Is.EqualTo(float.Parse(p[5],CultureInfo.InvariantCulture)).Within(.000025f),at);count++;
            }
            Assert.That(count,Is.EqualTo(5505));
        }
        [TestCase(30)][TestCase(60)][TestCase(144)]
        public void HostRateAndPauseShareTheWorldClockAndReloadResetsBeforeObservers(int fps)
        {
            var loader=new FakeMapLoader();var map=new MapData {MapId=1};
            map.Platforms.Add(new Platform {Id=1,X1=-1000,X2=1000,Y1=0,Y2=0});loader.AddMap(1,map);
            var world=new GameWorld(new FakeInputProvider(),loader);world.LoadMap(1);
            for(int i=0;i<fps;i++)world.UpdatePhysics(1f/fps);
            Assert.That(world.MapSimulationTicks,Is.EqualTo(125));
            long revision=world.MapRevision;
            world.UpdatePhysics(0);world.UpdatePhysics(float.NaN);world.UpdatePhysics(-1);
            Assert.That(world.MapSimulationTicks,Is.EqualTo(125));
            world.Player.Position=new MapleClient.GameLogic.Vector2(2,.3f);
            Assert.That(world.MapRevision,Is.EqualTo(revision),"Local movement/teleport preserves background phase.");
            world.MapLoaded+=_=>{Assert.That(world.MapSimulationTicks,Is.Zero);Assert.That(world.MapRevision,Is.EqualTo(revision+1));};
            world.LoadMap(1);world.UpdatePhysics(.024f);Assert.That(world.MapSimulationTicks,Is.EqualTo(3));
        }
        [Test] public void SeekingBackToMapStartResetsFrameFadeScaleAndZigzagDirection()
        {
            var timeline=Create(1);timeline.AdvanceTo(111);timeline.AdvanceTo(0);
            Assert.That(timeline.Sample(.5f,out float alpha,out float scale),Is.Zero);
            Assert.That(alpha,Is.EqualTo(1));Assert.That(scale,Is.EqualTo(1));
            var fresh=Create(1);timeline.AdvanceTo(21);fresh.AdvanceTo(21);
            Assert.That(timeline.Sample(.75f,out alpha,out scale),Is.EqualTo(fresh.Sample(.75f,out float a,out float z)));
            Assert.That(alpha,Is.EqualTo(a));Assert.That(scale,Is.EqualTo(z));
        }
    }
}
