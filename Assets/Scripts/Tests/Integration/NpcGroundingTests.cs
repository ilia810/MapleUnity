using System.Collections.Generic;
using System.Reflection;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.Integration
{
    public class NpcGroundingTests
    {
        private GameObject root;
        private FootholdManager ground;
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("NpcGroundTest");
            ground = root.AddComponent<FootholdManager>();
        }
        [TearDown]
        public void TearDown() { Object.DestroyImmediate(root); }
        [TestCase(149,267)]
        [TestCase(424,259)]
        public void StaticNpcUsesSettledSurface_WhileSpawnQueryKeepsOnePixelOffset(int x,int y)
        {
            ground.Initialize(new List<Foothold> { new Foothold { Id=157,X1=0,X2=450,Y1=274,Y2=274 } });
            Assert.That(ground.TryGetGroundBelow(x,y,out float surface),Is.True);
            Assert.That(surface,Is.EqualTo(274));
            Assert.That(ground.GetYBelow(x,y),Is.EqualTo(273));
        }
        [Test]
        public void GroundQueryIgnoresWallsAndInterpolatesReversedSlope()
        {
            ground.Initialize(new List<Foothold> {
                new Foothold { X1=50,X2=50,Y1=0,Y2=100 },
                new Foothold { X1=100,X2=0,Y1=300,Y2=200 } });
            Assert.That(ground.TryGetGroundBelow(50,0,out float surface),Is.True);
            Assert.That(surface,Is.EqualTo(250));
        }
        [Test]
        public void MissingSurfaceDoesNotMoveNpcUpward()
        {
            ground.Initialize(new List<Foothold> { new Foothold { X1=0,X2=100,Y1=274,Y2=274 } });
            Assert.That(ground.TryGetGroundBelow(50,500,out float y),Is.False);
            Assert.That(y,Is.EqualTo(500));
            Assert.That(ground.GetYBelow(200,123),Is.EqualTo(123));
        }
    }
}
