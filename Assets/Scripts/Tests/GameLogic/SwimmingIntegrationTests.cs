using System;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class SwimmingIntegrationTests
    {
        private static Player InWater(int speed=100)
        {
            var terrain=new FootholdService();terrain.LoadFootholds(SwimmingTraceTests.Terrain("water_idle"));
            var player=new Player(terrain){Position=new Vector2(0,1.8f),IsGrounded=false,Speed=speed};
            player.UpdatePhysics(.008f,new MapData {IsUnderwater=true});return player;
        }
        [Test] public void WaterFacingKeepsKeyPressesReleasedBetweenTicks()
        {
            var player=InWater();var map=new MapData{IsUnderwater=true};Assert.That(player.State,Is.EqualTo(PlayerState.Swimming));
            player.MoveLeft(true);player.MoveLeft(false);Assert.That(player.FacingRight,Is.Not.EqualTo(false));
            player.UpdatePhysics(.008f,map);Assert.That(player.FacingRight,Is.False);Assert.That(player.Velocity.X,Is.Zero);
            player.MoveRight(true);player.MoveLeft(true);player.UpdatePhysics(.008f,map);
            Assert.That(player.FacingRight,Is.False,"Last pressed left faces left even with both keys held.");
            Assert.That(player.Velocity.X,Is.LessThan(0));
        }
        [Test] public void WaterDoesNotApplyLegacySpeedJumpMultipliersAndCannotDoubleJump()
        {
            var slow=InWater(100);var fast=InWater(140);var map=new MapData{IsUnderwater=true};
            fast.JumpPower=123;fast.EnableDoubleJump(true);
            for(int i=0;i<50;i++)
            {
                slow.MoveRight(true);fast.MoveRight(true);slow.ClimbUp(true);fast.ClimbUp(true);
                fast.Jump();slow.UpdatePhysics(.008f,map);fast.UpdatePhysics(.008f,map);fast.ReleaseJump();
            }
            Assert.That(fast.Position,Is.EqualTo(slow.Position));Assert.That(fast.GetJumpCount(),Is.Zero);
            Assert.That(fast.SwimAnimationTicks,Is.EqualTo(50));Assert.That(fast.GetActiveModifiers().Find(m=>m.Id=="swimming"),Is.Null);
        }
        [TestCase(4)][TestCase(16)][TestCase(34)][TestCase(67)]
        public void WaterMovementAndPoseTimeDoNotDependOnHostFrameSize(int milliseconds)
        {
            var expected=Replay(8);var actual=Replay(milliseconds);
            Assert.That(actual.Item1.X,Is.EqualTo(expected.Item1.X).Within(.00001));
            Assert.That(actual.Item1.Y,Is.EqualTo(expected.Item1.Y).Within(.00001));
            Assert.That(actual.Item2,Is.EqualTo(expected.Item2));
        }
        private static Tuple<Vector2,long> Replay(int milliseconds)
        {
            var loader=new FakeMapLoader();var map=new MapData{MapId=1,IsUnderwater=true};
            map.Platforms.Add(new Platform{Id=1,X1=-1000,X2=1000,Y1=0,Y2=0});map.Platforms.Add(new Platform{Id=3,X1=-1000,X2=1000,Y1=400,Y2=400});loader.AddMap(1,map);
            var input=new FakeInputProvider();var world=new GameWorld(input,loader);world.LoadMap(1);world.Player.Position=new Vector2(0,1.8f);world.Player.IsGrounded=false;
            for(int phase=0;phase<3;phase++)
            {
                input.IsRightPressed=phase==0;input.IsUpPressed=phase==0;input.IsLeftPressed=phase==2;input.IsDownPressed=phase==2;
                for(int time=0;time<240;){int delta=Math.Min(milliseconds,240-time);time+=delta;world.ProcessInput();world.UpdatePhysics(delta/1000f);}
            }
            return Tuple.Create(world.Player.Position,world.Player.SwimAnimationTicks);
        }
        [TestCase(12,1f,0)][TestCase(13,0f,0)][TestCase(13,.49f,0)][TestCase(13,.5f,1)][TestCase(13,1f,1)]
        [TestCase(27,1f,1)][TestCase(28,.49f,1)][TestCase(28,.5f,0)]
        public void SwimBodyFrameUsesCharLookRemainingDelayInterpolation(int ticks,float interpolation,int expected)
        {
            Assert.That(SourceStanceTiming.SampleLoop(ticks,interpolation,new[]{100,120}),Is.EqualTo(expected));
        }
    }
}
