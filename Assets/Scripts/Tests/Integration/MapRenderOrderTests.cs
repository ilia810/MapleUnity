using MapleClient.GameData;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.Integration
{
    public class MapRenderOrderTests
    {
        [Test]
        public void SourceStageSequenceFitsAllEightMapLayers()
        {
            for (int layer = 0; layer < 8; layer++)
            {
                int lastTile = MapRenderOrder.TileOrder(layer, 127);
                Assert.That(StageRenderOrder.NpcOrder(layer), Is.GreaterThan(lastTile));
                Assert.That(StageRenderOrder.PlayerOrder(layer), Is.GreaterThan(StageRenderOrder.NpcOrder(layer)));
                Assert.That(StageRenderOrder.PlayerOrder(layer), Is.LessThan(MapRenderOrder.ObjectOrder(layer + 1, -128)));
            }
        }

        [Test]
        public void SavedNpcSortingUsesItsOwnMapsFootholdDespiteHeightAndAnotherActiveMap()
        {
            var root = new GameObject("SavedMap");
            var other = new GameObject("OtherMap");
            try
            {
                var terrain = root.AddComponent<FootholdManager>();
                terrain.Initialize(new System.Collections.Generic.List<Foothold> {
                    new Foothold { Id = 123, Layer = 2 }
                });
                other.AddComponent<FootholdManager>().Initialize(new System.Collections.Generic.List<Foothold> {
                    new Foothold { Id = 123, Layer = 6 }
                });
                var npcObject = new GameObject("SavedNPC"); npcObject.transform.SetParent(root.transform);
                npcObject.transform.position = new Vector3(0, 500, 0);
                var npc = npcObject.AddComponent<NPCBehavior>(); npc.footholdId = 123;
                var sprite = npcObject.AddComponent<SpriteRenderer>();
                sprite.sortingLayerName = "NPCs"; sprite.sortingOrder = -500;
                npc.ApplyStageOrder(npc.GetComponentInParent<FootholdManager>());
                Assert.That(sprite.sortingLayerName, Is.EqualTo("Objects"));
                Assert.That(sprite.sortingOrder, Is.EqualTo(2600));
                Assert.That(npc.transform.position.y, Is.EqualTo(500));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(other); }
        }

        [Test]
        public void SavedMapInterleavesLayersAndKeepsBushesBehindTheirGround()
        {
            var root = new GameObject("MapOrderTest");
            try
            {
                var bush = new GameObject("BackgroundBush"); bush.transform.SetParent(root.transform);
                var obj = bush.AddComponent<MapObject>(); obj.layer=0; obj.zOrder=3; obj.zModifier=44;
                var bushRenderer = bush.AddComponent<SpriteRenderer>();
                bushRenderer.sortingLayerName="Objects"; bushRenderer.sortingOrder=47;
                var ground = new GameObject("Ground"); ground.transform.SetParent(root.transform);
                var tile = ground.AddComponent<MapTile>();
                tile.layer=1; tile.tileSet="woodMarble"; tile.variant="enH0"; tile.tileNumber=2; tile.z=0; tile.zM=99;
                var tileRenderer = ground.AddComponent<SpriteRenderer>();
                tileRenderer.sortingLayerName="Tiles"; tileRenderer.sortingOrder=1256;
                var front = new GameObject("HigherMapLayer"); front.transform.SetParent(root.transform);
                var higher = front.AddComponent<MapObject>(); higher.layer=2; higher.zOrder=-2;
                var frontRenderer = front.AddComponent<SpriteRenderer>();
                var bitmap = new NxNode("2"); bitmap.AddChild(new NxNode("z",-3));
                MapRenderOrder.Apply(root.transform,path => path=="Tile/woodMarble.img/enH0/2"?bitmap:null);
                Assert.That(bushRenderer.sortingLayerID,Is.EqualTo(tileRenderer.sortingLayerID));
                Assert.That(frontRenderer.sortingLayerID,Is.EqualTo(tileRenderer.sortingLayerID));
                Assert.That(bushRenderer.sortingOrder,Is.EqualTo(131),"Object zM must not become depth.");
                Assert.That(tileRenderer.sortingOrder,Is.EqualTo(1381),"Use bitmap depth -3, not placement z/zM.");
                Assert.That(bushRenderer.sortingOrder,Is.LessThan(tileRenderer.sortingOrder));
                Assert.That(tileRenderer.sortingOrder,Is.LessThan(frontRenderer.sortingOrder));
                Assert.That(tile.sortingOrder,Is.EqualTo(tileRenderer.sortingOrder));
                Assert.That(tile.zM,Is.EqualTo(99),"Retain authored placement metadata.");
                Assert.That(ground.transform.localPosition,Is.EqualTo(Vector3.zero),"Sorting must not move art.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void LinkedTileUsesTargetBitmapDepth()
        {
            var link = new NxNode("2"); link.AddChild(new NxNode("z",90));
            link.AddChild(new NxNode("_outlink","Map/Tile/shared.img/ground/0"));
            var target = new NxNode("0"); target.AddChild(new NxNode("z",-5));
            int order = MapRenderOrder.ReadTileOrder(0,"woodMarble","enH0",2,
                path => path=="Tile/shared.img/ground/0"?target:link);
            Assert.That(order,Is.EqualTo(379));
        }
        [TestCase(-3,99,381)]
        [TestCase(0,-6,378)]
        [TestCase(255,0,383)]
        public void BitmapDepthRetainsSourceSignedOrdering(int z,int zm,int expected)
        {
            Assert.That(MapRenderOrder.TileOrder(0,z,zm),Is.EqualTo(expected));
        }
    }
}
