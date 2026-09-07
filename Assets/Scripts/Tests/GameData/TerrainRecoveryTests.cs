using System.Collections.Generic;
using NUnit.Framework;
using MapleClient.GameLogic;
using MapleClient.GameData.Adapters;

namespace MapleClient.Tests.GameData
{
    public class TerrainRecoveryTests
    {
        [TestCase(0, 0)]
        [TestCase(-25, 75)]
        [TestCase(25, -75)]
        public void AdapterPreservesOriginalHeightAndSlope(float y1, float y2)
        {
            var result = FootholdDataAdapter.ConvertPlatformsToFootholds(new List<Platform> {
                new Platform { Id = 7, X1 = -100, Y1 = y1, X2 = 200, Y2 = y2 }
            });
            Assert.That(result[0].X1, Is.EqualTo(-100));
            Assert.That(result[0].Y1, Is.EqualTo(y1));
            Assert.That(result[0].X2, Is.EqualTo(200));
            Assert.That(result[0].Y2, Is.EqualTo(y2));
        }

        [Test]
        public void AdapterRoundTripRetainsSourceTopologyAndVerticalWalls()
        {
            var source = new Foothold(17,100,-100,100,0) {
                IsWall=true, PreviousId=8, NextId=29, Layer=4
            };
            var platforms = FootholdDataAdapter.ConvertFootholdsToPlatforms(new List<Foothold> { source });
            Assert.That(platforms[0].Type,Is.EqualTo(PlatformType.Normal),"A vertical foothold is not a climbable ladder.");
            Assert.That(platforms[0].HasSourceTopology,Is.True);
            var result = FootholdDataAdapter.ConvertPlatformsToFootholds(platforms)[0];
            Assert.That(result.Id,Is.EqualTo(17));
            Assert.That(result.PreviousId,Is.EqualTo(8));
            Assert.That(result.NextId,Is.EqualTo(29));
            Assert.That(result.Layer,Is.EqualTo(4));
            Assert.That(result.IsWall,Is.True);
        }

        [Test]
        public void AdapterRetainsAuthoredOrderForEqualHeightSupport()
        {
            var result = FootholdDataAdapter.ConvertPlatformsToFootholds(new List<Platform> {
                new Platform { Id = 17, X1 = -100, X2 = 100, Y1 = 0, Y2 = 0, Layer = 4 },
                new Platform { Id = 2, X1 = -100, X2 = 100, Y1 = 0, Y2 = 0, Layer = 1 }
            });
            var terrain = new MapleClient.GameLogic.Core.NormalTerrain(result);
            Assert.That(terrain.Below(0, -1), Is.EqualTo(17), "The adapter must not change source tie precedence by sorting IDs.");
        }

        [Test]
        public void VerticalFootholdIsAWall()
        {
            var result = FootholdDataAdapter.ConvertPlatformsToFootholds(new List<Platform> {
                new Platform { Id = 1, X1 = 20, X2 = 20, Y1 = 0, Y2 = 100 }
            });
            Assert.That(result[0].IsWall, Is.True);
        }
    }
}