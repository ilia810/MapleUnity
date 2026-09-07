using System.Linq;
using System.Reflection;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.Integration
{
    public class BackgroundRecoveryTests
    {
        [Test]
        public void StationaryBackgroundUsesSourceCameraParallax()
        {
            // HeavenClient screen=(495-200+500,45+12+500) at camera=(1000,400).
            AssertAnchor(new BackgroundData {X=495,Y=45,RX=-20,RY=3,Type=0,A=255},
                new Vector3(10,-4,-10),0,new Vector2(12.95f,-4.57f));
        }
        [TestCase(0,1.4f)]
        [TestCase(16,2.25f)]
        [TestCase(-16,-0.25f)]
        public void HorizontalMovingTypeUsesSourceSpeedOrStationaryBranch(int rate,float expectedX)
        {
            AssertAnchor(new BackgroundData {X=100,RX=rate,Type=4,A=255},
                new Vector3(0.4f,-0.8f,-10),1f,new Vector2(expectedX,-0.8f));
        }
        [TestCase(0,-1.8f)]
        [TestCase(16,-2.25f)]
        [TestCase(-16,0.25f)]
        public void VerticalMovingTypeUsesSourceSpeedOrStationaryBranch(int rate,float expectedY)
        {
            AssertAnchor(new BackgroundData {Y=100,RY=rate,Type=5,A=255},
                new Vector3(0.4f,-0.8f,-10),1f,new Vector2(0.4f,expectedY));
        }
        private static void AssertAnchor(BackgroundData data,Vector3 cameraPosition,float elapsed,Vector2 expected)
        {
            var cameraObject = new GameObject("BackgroundTestCamera");
            var layerObject = new GameObject("BackgroundTestLayer");
            var texture = new Texture2D(100,100);
            var sprite = Sprite.Create(texture,new Rect(0,0,100,100),new Vector2(0.5f,0.5f),100);
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 5; camera.aspect = 1;
                camera.transform.position = cameraPosition;
                var layer = layerObject.AddComponent<ViewportBackgroundLayer>();
                layer.backgroundData = data; layer.tileSprite = sprite;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(ViewportBackgroundLayer).GetField("viewCamera",flags).SetValue(layer,camera);
                layer.Render((long)(elapsed * 125), 1);
                var tiles = layerObject.GetComponentsInChildren<SpriteRenderer>();
                Assert.That(tiles.Any(t => Vector2.Distance(t.transform.position,expected)<0.0001f),Is.True,
                    "The source anchor must appear in the visible tile lattice.");
                typeof(ViewportBackgroundLayer).GetMethod("UpdateTiling",flags).Invoke(layer,null);
                CollectionAssert.AreEquivalent(tiles,layerObject.GetComponentsInChildren<SpriteRenderer>(),
                    "A stationary viewport should reuse its tiles.");
            }
            finally
            {
                Object.DestroyImmediate(layerObject); Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
            }
        }
    }
}
