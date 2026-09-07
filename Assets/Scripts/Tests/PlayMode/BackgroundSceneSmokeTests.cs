using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class BackgroundSceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        [SetUp] public void WatchLogs(){diagnostics.Clear();Application.logMessageReceived+=OnLog;}
        private void OnLog(string message,string stack,LogType type){if(type!=LogType.Log)diagnostics.Add(type+": "+message);}
        [TearDown] public void CheckLogs(){Application.logMessageReceived-=OnLog;Assert.That(diagnostics,Is.Empty);}
        private static void Step(GameWorld world,int ticks){for(int i=0;i<ticks;i++)world.UpdatePhysics(.008f);}
        private static ViewportBackgroundLayer[] Layers()=>Object.FindObjectsByType<ViewportBackgroundLayer>(FindObjectsSortMode.None);
        [UnityTest]
        public IEnumerator ExistingSceneCloudsAndGeneratedAnimatedMapsFollowSimulationAndResetOnTravel()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var game=Object.FindFirstObjectByType<GameManager>();game.enabled=false;var world=game.World;
            Camera.main.GetComponent<SimpleCameraFollow>().enabled=false;
            var cloud=Layers().Single(l=>l.backgroundData.No==1);
            Step(world,125);yield return null;yield return null;
            Assert.That(cloud.AppliedTicks,Is.EqualTo(world.MapSimulationTicks));
            var cloudTiles=cloud.GetComponentsInChildren<SpriteRenderer>();var positions=cloudTiles.Select(t=>t.transform.position).ToArray();
            yield return new WaitForSeconds(.15f);
            CollectionAssert.AreEqual(positions,cloudTiles.Select(t=>t.transform.position));
            Step(world,125);yield return null;yield return null;
            Assert.That(cloudTiles[0].transform.position.x,Is.Not.EqualTo(positions[0].x));

            world.LoadMap(103000000);yield return null;yield return null;
            var lights=Layers().Where(l=>l.backgroundData.Ani!=0).ToArray();
            Assert.That(lights.Length,Is.EqualTo(6));Assert.That(lights.All(l=>l.FrameCount==2 && l.AppliedTicks==0),Is.True);
            foreach(var light in lights)Assert.That(light.GetComponentInChildren<SpriteRenderer>().color.a,Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-kerning-lights-bright");
            Step(world,499);yield return null;yield return null;
            float faded=lights[0].GetComponentInChildren<SpriteRenderer>().color.a;
            Assert.That(faded,Is.InRange(.66f,.68f));Assert.That(lights.All(l=>l.FrameIndex==0),Is.True);
            yield return new WaitForSeconds(.15f);
            Assert.That(lights[0].GetComponentInChildren<SpriteRenderer>().color.a,Is.EqualTo(faded));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-kerning-lights-dim");
            Step(world,1);yield return null;yield return null;
            Assert.That(lights.All(l=>l.FrameIndex==1),Is.True);
            world.LoadMap(103000000);yield return null;yield return null;
            Assert.That(Layers().Where(l=>l.backgroundData.Ani!=0).All(l=>l.FrameIndex==0 && l.AppliedTicks==0),Is.True,
                "Reloading the same map resets retained layers through the map revision.");

            world.LoadMap(230000000);yield return null;yield return null;
            Assert.That(lights.All(l=>l==null),Is.True,"Travel releases old layers.");
            var fish=Layers().Where(l=>l.backgroundData.Ani!=0 && l.backgroundData.SpriteNo==16).ToArray();
            Assert.That(fish.Length,Is.GreaterThan(1));Assert.That(fish.All(l=>l.FrameCount==4 && l.FrameIndex==0),Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-aqua-fish-first");
            Step(world,26);yield return null;yield return null;
            Assert.That(fish.All(l=>l.FrameIndex==2),Is.True);
            foreach(var layer in fish)foreach(var tile in layer.GetComponentsInChildren<SpriteRenderer>())
            {
                Assert.That(tile.sprite.name,Does.EndWith("/ani/16/2"));
                Assert.That(tile.flipX,Is.EqualTo(layer.backgroundData.F!=0));
                Assert.That(tile.sortingLayerName,Is.EqualTo(layer.isForeground?"Foreground":"Background"));
            }
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-aqua-fish-swimming");
            world.LoadMap(100000000);yield return null;yield return null;
            Assert.That(fish.All(l=>l==null),Is.True);Assert.That(Layers().Length,Is.EqualTo(8));
            Assert.That(Layers().All(l=>l.AppliedTicks==0),Is.True);
        }
    }
}
