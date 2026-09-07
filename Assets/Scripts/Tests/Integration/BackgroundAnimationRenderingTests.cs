using System.Linq;
using System.Reflection;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.Integration
{
    public class BackgroundAnimationRenderingTests
    {
        [Test]
        public void AnimatedTilesKeepTheirFirstFrameSpacingOriginsFlipOpacityAndForegroundBand()
        {
            var cameraObject=new GameObject("BackgroundCamera");var root=new GameObject("BackgroundLayer");
            try
            {
                var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3.84f;camera.aspect=16f/9;
                var layer=root.AddComponent<ViewportBackgroundLayer>();
                layer.backgroundData=new BackgroundData {BgName="aquaRoad",SpriteNo=16,Ani=1,Type=4,RX=16,A=160,F=1};
                layer.isForeground=true;layer.sortingOrder=10070;
                typeof(ViewportBackgroundLayer).GetField("viewCamera",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(layer,camera);
                layer.Initialize();float spacing=layer.tileSprite.rect.width/100;
                layer.Render(26,1);Assert.That(layer.FrameIndex,Is.EqualTo(2));
                var tiles=root.GetComponentsInChildren<SpriteRenderer>().OrderBy(t=>t.transform.position.x).ToArray();
                Assert.That(tiles.Length,Is.GreaterThan(2));
                foreach(var tile in tiles)
                {
                    Assert.That(tile.sprite,Is.SameAs(layer.tileSprite));Assert.That(tile.flipX,Is.True);
                    Assert.That(tile.color.a,Is.EqualTo(160f/255));Assert.That(tile.sortingLayerName,Is.EqualTo("Foreground"));
                    Assert.That(tile.sortingOrder,Is.EqualTo(10070));
                }
                for(int i=1;i<tiles.Length;i++)Assert.That(tiles[i].transform.position.x-tiles[i-1].transform.position.x,Is.EqualTo(spacing).Within(.00001f));
                Assert.That(tiles[0].bounds.min.x,Is.LessThan(-camera.orthographicSize*camera.aspect));
                Assert.That(tiles[tiles.Length-1].bounds.max.x,Is.GreaterThan(camera.orthographicSize*camera.aspect));
                var positions=tiles.Select(t=>t.transform.position).ToArray();
                layer.Render(26,1);CollectionAssert.AreEqual(positions,tiles.Select(t=>t.transform.position));
                // Runtime-only tile lists are lost when a generated scene is saved/reopened.
                ((System.Collections.IList)typeof(ViewportBackgroundLayer).GetField("tiles",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(layer)).Clear();
                layer.Initialize();Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length,Is.EqualTo(tiles.Length));
                layer.Render(0,0,2);Assert.That(layer.FrameIndex,Is.Zero);
            }
            finally {Object.DestroyImmediate(root);Object.DestroyImmediate(cameraObject);}
        }
    }
}
