using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class ClassicNavigationSceneSmokeTests
    {
        private readonly List<string> errors = new List<string>();
        [SetUp] public void Watch() { errors.Clear(); Application.logMessageReceived += Log; }
        private void Log(string text,string stack,LogType type) { if(type!=LogType.Log) errors.Add(type+": "+text); }
        [TearDown] public void Check() { Application.logMessageReceived -= Log; Assert.That(errors,Is.Empty); }
        [UnityTest]
        public IEnumerator MinimapTracksFeetAndAtlasCoexistsWithOtherWindows()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single); yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var mini = Object.FindFirstObjectByType<ClassicMinimapView>(); var world = manager.World;
            for(int i=0;i<100;i++)world.UpdatePhysics(.008f);
            yield return null; yield return null;
            Assert.That(mini.ShownMapId,Is.EqualTo(100000000));
            Assert.That(GameObject.Find("MinimapRegion").GetComponent<Text>().text,Is.EqualTo("Victoria Road"));
            Assert.That(GameObject.Find("MinimapLocation").GetComponent<Text>().text,Is.EqualTo("Henesys"));
            Assert.That(GameObject.Find("MinimapCanvas").GetComponent<Image>().sprite.rect.size,Is.EqualTo(new Vector2(465,86)));
            Assert.That(mini.NpcMarkerCount,Is.EqualTo(world.CurrentMap.NpcSpawns.Count));
            var position = world.Player.Position; var oldPoint = mini.PlayerPoint;
            world.Player.Position = new MapleClient.GameLogic.Vector2(position.X+.32f,position.Y+.16f);
            yield return null; yield return null;
            Assert.That(mini.PlayerPoint.x-oldPoint.x,Is.EqualTo(2).Within(.001));
            Assert.That(mini.PlayerPoint.y-oldPoint.y,Is.EqualTo(-1).Within(.001));
            world.Player.Position = position;
            Assert.That(ClassicMinimapView.CropOffset(0,465,288),Is.EqualTo(0));
            Assert.That(ClassicMinimapView.CropOffset(465,465,288),Is.EqualTo(-177));
            Assert.That(ClassicMinimapView.CropOffset(0,86,100),Is.EqualTo(7));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-minimap-henesys",1366,768,AssertMinimapBounds);
            yield return InventorySceneSmokeTests.Click("MinimapMinus");
            Assert.That(mini.Mode,Is.EqualTo(1)); Assert.That(GameObject.Find("MinimapRegion"),Is.Null);
            yield return InventorySceneSmokeTests.Click("MinimapMinus");
            Assert.That(mini.Mode,Is.EqualTo(0)); Assert.That(GameObject.Find("MinimapCanvas"),Is.Null);
            Assert.That(GameObject.Find("MiniMapPanel").GetComponent<RectTransform>().rect.height,Is.EqualTo(20));
            yield return InventorySceneSmokeTests.Click("MinimapPlus"); yield return InventorySceneSmokeTests.Click("MinimapPlus");
            yield return InventorySceneSmokeTests.Click("InventoryToggle");
            yield return InventorySceneSmokeTests.Click("WorldMapToggle");
            var atlas = Object.FindFirstObjectByType<ClassicWorldMapView>();
            Assert.That(atlas.Visible,Is.True); Assert.That(atlas.Region,Is.EqualTo("WorldMap010.img"));
            Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible,Is.True);
            Assert.That(GameObject.Find("WorldMapCurrentPosition"),Is.Not.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-world-map",1366,768);
            yield return InventorySceneSmokeTests.Click("WorldMapParent");
            Assert.That(atlas.Region,Is.EqualTo("WorldMap.img"));
            yield return InventorySceneSmokeTests.Click("WorldMapCurrent");
            Assert.That(atlas.Region,Is.EqualTo("WorldMap010.img"));
            int mapId = world.CurrentMapId;
            atlas.Browse("WorldMap011"); yield return null;
            Assert.That(atlas.Region,Is.EqualTo("WorldMap011.img")); Assert.That(world.CurrentMapId,Is.EqualTo(mapId));
            atlas.ShowCurrent(); yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-world-map-small",640,480,()=>AssertInCanvas(GameObject.Find("WorldMapPanel").GetComponent<RectTransform>()));
            Object.FindFirstObjectByType<ClassicWindowManager>().CloseFrontmost(); yield return null;
            Assert.That(atlas.Visible,Is.False); Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible,Is.True);
            Object.FindFirstObjectByType<InventoryView>().Show(false);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-minimap-small",640,480,AssertMinimapBounds);
        }
        [UnityTest]
        public IEnumerator NameplatesUseRealMetadataStayUprightAndCleanUpAcrossTravelAndDeath()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single); yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var mini = Object.FindFirstObjectByType<ClassicMinimapView>();
            var oldNpc = WorldLabelAnchor.Active.First(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc);
            world.LoadMap(100000102); yield return null; yield return null; yield return null;
            for(int i=0;i<100;i++)world.UpdatePhysics(.008f);
            yield return null; yield return null;
            Assert.That(oldNpc==null,Is.True);
            Assert.That(mini.ShownMapId,Is.EqualTo(100000102)); Assert.That(mini.NpcMarkerCount,Is.EqualTo(0));
            Assert.That(GameObject.Find("MinimapCanvas"),Is.Null,"A map without a preview must not reuse the last map's image or markers.");
            Assert.That(GameObject.Find("CollapsedMinimapName").GetComponent<Text>().text,Does.Contain("Henesys Department Store"));
            var npc = WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc);
            Assert.That(npc.Label,Is.EqualTo("Luna")); Assert.That(npc.Service,Is.EqualTo("Grocer"));
            var labels = GameObject.Find("WorldNameplates").transform;
            Assert.That(labels.GetComponentsInChildren<Text>(true).Any(t=>t.text=="Luna"),Is.True);
            Assert.That(labels.GetComponentsInChildren<Text>(true).Any(t=>t.text=="Grocer"),Is.True);
            Assert.That(labels.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),Is.True);
            npc.transform.localScale = new Vector3(-1,1,1);
            world.Player.Name = "Local Explorer"; yield return null; yield return null;
            Assert.That(labels.GetComponentsInChildren<Text>(true).Any(t=>t.text=="Local Explorer"),Is.True);
            Assert.That(labels.GetComponentsInChildren<Text>(true).All(t=>t.rectTransform.lossyScale.x>0),Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-nameplates-shop",1366,768,()=>{
                var name = labels.GetComponentsInChildren<Text>(true).Single(t=>t.text=="Luna");
                var screen = Camera.main.WorldToScreenPoint(npc.Feet);
                var plate = (RectTransform)name.transform.parent.parent;
                var actual = RectTransformUtility.WorldToScreenPoint(name.GetComponentInParent<Canvas>().worldCamera,plate.position);
                Assert.That(actual.x,Is.EqualTo(screen.x).Within(1)); Assert.That(actual.y,Is.EqualTo(screen.y-2).Within(1));
            });
            world.LoadMap(100010000); yield return null; yield return null;
            for(int i=0;i<100;i++)world.UpdatePhysics(.008f);
            yield return null; yield return null;
            Assert.That(npc==null,Is.True);
            var monster = WorldLabelAnchor.Active.Where(a=>a.Kind==WorldLabelAnchor.ActorKind.Monster)
                .OrderBy(a=>(a.Feet-GameObject.Find("Player").transform.position).sqrMagnitude).First();
            var model = monster.GetComponent<MonsterView>().Model;
            Assert.That(monster.Label,Is.EqualTo($"Lv. {model.Template.Level} {model.Name}"));
            string plateName = "Nameplate_Monster_"+monster.GetInstanceID();
            Assert.That(labels.Find(plateName),Is.Null,"Idle monsters must not crowd the scene with nameplates.");
            model.TakeDamage(1); yield return null; yield return null;
            Assert.That(monster.ShowName,Is.True); Assert.That(labels.Find(plateName),Is.Not.Null);
            yield return new WaitForSeconds(2.1f); yield return null;
            Assert.That(monster.ShowName,Is.False); Assert.That(labels.Find(plateName),Is.Null,"Nameplates expire with the source's two-second health feedback.");
            model.TakeDamage(1); yield return null; yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-nameplates-field",1366,768,()=>{
                // CameraBounds runs after the ordinary camera follow; labels must use its final position.
                foreach(var actor in WorldLabelAnchor.Active.Where(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc && a.ShowName))
                {
                    var name = labels.GetComponentsInChildren<Text>(true).Single(t=>t.text==actor.Label);
                    var plate = (RectTransform)name.transform.parent.parent;
                    var screen = Camera.main.WorldToScreenPoint(actor.Feet);
                    var actual = RectTransformUtility.WorldToScreenPoint(name.GetComponentInParent<Canvas>().worldCamera,plate.position);
                    Assert.That(actual.x,Is.EqualTo(screen.x).Within(1),actor.Label+" drifted when the camera reached its map bounds.");
                    Assert.That(actual.y,Is.EqualTo(screen.y-2).Within(1));
                }
            });
            model.TakeDamage(model.MaxHP); yield return null; yield return null;
            Assert.That(monster.Visible,Is.False); Assert.That(labels.Find(plateName),Is.Null);
            world.LoadMap(100000000); yield return null; yield return null;
            Assert.That(mini.NpcMarkerCount,Is.EqualTo(world.CurrentMap.NpcSpawns.Count));
        }
        private static void AssertMinimapBounds() => AssertInCanvas(GameObject.Find("MiniMapPanel").GetComponent<RectTransform>());
        private static void AssertInCanvas(RectTransform r)
        {
            var canvas = (RectTransform)r.GetComponentInParent<Canvas>().transform;
            var corners = new Vector3[4]; r.GetWorldCorners(corners);
            foreach(var corner in corners) { var p = canvas.InverseTransformPoint(corner); Assert.That(p.x,Is.InRange(canvas.rect.xMin-.1f,canvas.rect.xMax+.1f)); Assert.That(p.y,Is.InRange(canvas.rect.yMin-.1f,canvas.rect.yMax+.1f)); }
        }
    }
}
