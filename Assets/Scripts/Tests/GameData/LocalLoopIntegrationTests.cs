using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class LocalLoopIntegrationTests
    {
        private sealed class Maps : IMapLoader
        {
            public MapData Map = new MapData { MapId = 42, Platforms = new List<Platform> {
                new Platform { Id = 1, X1 = -3000, X2 = 3000, Y1 = 0, Y2 = 0 } },
                Portals = new List<Portal> { new Portal { Id = 0, Name = "sp", Type = MapleClient.GameLogic.PortalType.Spawn, X = 0, Y = 0 } },
                NpcSpawns = new List<NpcSpawn> { new NpcSpawn { NpcId = OfflineEconomy.ShopNpcId } } };
            public MapData GetMap(int id) => id == 42 ? Map : null;
        }
        private GameWorld world;
        private Player player;
        private Maps maps;
        private NXDataManager data;
        private NpcSpawn Shop => maps.Map.NpcSpawns[0];
        [SetUp] public void Setup()
        {
            data = NXDataManagerSingleton.Instance.DataManager; maps = new Maps();
            world = new GameWorld(null, maps, assetProvider: data); world.LoadMap(42); player = world.Player;
        }
        [Test] public void NxStacksSplitFillAndReuseSlotsWithinEachCategory()
        {
            Assert.That(player.Inventory.TryAddItem(2000000, 201), Is.True);
            Assert.That(player.Inventory.GetStacks().Select(s => s.Quantity), Is.EqualTo(new[] {100,100,1}));
            Assert.That(player.Inventory.RemoveItem(2000000, 100), Is.True);
            Assert.That(player.Inventory.TryAddItem(2000003, 2), Is.True);
            Assert.That(player.Inventory.GetStacks().First().ItemId, Is.EqualTo(2000003));
            Assert.That(player.Inventory.TryAddItem(1302000, 2), Is.True);
            Assert.That(player.Inventory.GetStacks().Where(s => s.Category == 1).Select(s => s.Quantity), Is.EqualTo(new[]{1,1}));
        }
        [Test] public void CapacityFailuresAndMultiItemGrantsAreAtomic()
        {
            Assert.That(player.Inventory.TryAddItem(2000000, Inventory.SlotsPerCategory*100), Is.True);
            Assert.That(player.Inventory.TryAddItem(2000000, int.MaxValue), Is.False);
            Assert.That(world.RequestPracticeSupplies(out _), Is.False);
            Assert.That(world.HasClaimedPracticeSupplies, Is.False);
            Assert.That(player.Inventory.GetItemCount(1302000), Is.Zero);
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(Inventory.SlotsPerCategory*100));
        }
        [Test] public void FailedMagicAndRangedKitsDoNotChangeTheJobOrClaimTheKit()
        {
            player.Inventory.AddItem(1302000, Inventory.SlotsPerCategory);
            Assert.That(world.RequestPracticeMagicSkills(out _), Is.False);
            Assert.That(world.RequestRangedPractice(145, out _), Is.False);
            Assert.That(player.JobId, Is.Zero); Assert.That(player.Level, Is.EqualTo(1));
            player.Inventory.RemoveItem(1302000, 1);
            Assert.That(world.RequestRangedPractice(145, out _), Is.True);
            Assert.That(player.Inventory.GetItemCount(2060000), Is.EqualTo(100));
        }
        [Test] public void FullEquipmentBagCanSwapButCannotUnequipOrDisplaceTwoPieces()
        {
            player.Inventory.AddItem(1302000,1); Assert.That(player.TryEquipItem(1302000,out _),Is.True);
            player.Inventory.AddItem(1302000,Inventory.SlotsPerCategory);
            Assert.That(player.TryUnequipItem(EquipSlot.Weapon,out _),Is.False);
            Assert.That(player.TryEquipItem(1302000,out _),Is.True);
            Assert.That(player.Inventory.GetItemCount(1302000),Is.EqualTo(Inventory.SlotsPerCategory));
            Assert.That(player.GetEquippedItems()[EquipSlot.Weapon],Is.EqualTo(1302000));
        }
        [Test] public void AmmunitionUsesTheFirstStackRatherThanSummingLaterStacks()
        {
            Assert.That(world.RequestRangedPractice(145,out _),Is.True); player.TryEquipItem(1452002,out _);
            player.Inventory.AddItem(2060000,1901); player.Inventory.RemoveItem(2060000,1999);
            Assert.That(player.Inventory.GetItemCount(2060000),Is.EqualTo(2)); Assert.That(player.AmmunitionCount,Is.EqualTo(1));
        }
        [Test] public void TwoHandedSwapThatWouldOverflowTheBagKeepsWeaponShieldAndItems()
        {
            player.Level=5; player.Inventory.AddItem(1302000,1); player.Inventory.AddItem(1092003,1);
            Assert.That(player.TryEquipItem(1302000,out _),Is.True); Assert.That(player.TryEquipItem(1092003,out _),Is.True);
            player.Inventory.AddItem(1402009,1); player.Inventory.AddItem(1302000,Inventory.SlotsPerCategory-1);
            Assert.That(player.TryEquipItem(1402009,out _),Is.False);
            Assert.That(player.GetEquippedItems()[EquipSlot.Weapon],Is.EqualTo(1302000));
            Assert.That(player.GetEquippedItems()[EquipSlot.Shield],Is.EqualTo(1092003));
            Assert.That(player.Inventory.GetItemCount(1402009),Is.EqualTo(1)); Assert.That(player.Inventory.UsedSlots(1),Is.EqualTo(Inventory.SlotsPerCategory));
        }
        [Test] public void MonsterLootUsesTheLocalTableAndPickupCreditsWalletAndInventory()
        {
            world.SpawnMonsterForTesting(100100,new Vector2(2,0)); var mob=world.Monsters.Single(); mob.TakeDamage(int.MaxValue);
            Assert.That(world.DroppedItems.Any(i=>i.IsMeso),Is.True);
            Assert.That(world.DroppedItems.Any(i=>i.ItemId==4000019),Is.True);
            player.Position=new Vector2(2,.3f); world.UpdatePhysics(.008f);
            Assert.That(player.Mesos,Is.EqualTo(5)); Assert.That(player.Inventory.GetItemCount(4000019),Is.EqualTo(1));
            Assert.That(world.DroppedItems,Is.Empty);
        }
        [Test] public void FullBagLeavesLootButStillCollectsMesosAndRemovesExactDropIdentity()
        {
            player.Inventory.AddItem(2000000,Inventory.SlotsPerCategory*100);
            world.AddDroppedItem(2000000,1,player.Position); world.AddDroppedItem(0,25,player.Position);
            world.AddDroppedItem(2000000,1,new Vector2(5,0)); var removed=new List<DroppedItem>(); world.DropRemoved+=removed.Add;
            world.UpdatePhysics(.008f); Assert.That(world.DroppedItems.Count,Is.EqualTo(2)); Assert.That(player.Mesos,Is.EqualTo(25));
            Assert.That(removed.Single().IsMeso,Is.True);
            player.Inventory.RemoveItem(2000000,1); var near=world.DroppedItems[0]; world.UpdatePhysics(.008f);
            Assert.That(removed.Last(),Is.SameAs(near)); Assert.That(world.DroppedItems.Single().Position.X,Is.EqualTo(5));
            world.DroppedItems.Single().LifeTime=0; world.UpdatePhysics(.008f); Assert.That(world.DroppedItems,Is.Empty);
        }
        [Test] public void ShopBuySellRejectsInsufficientFundsOverflowAndInvalidQuantities()
        {
            Assert.That(world.BuyFromShop(Shop,2000000,1,out _),Is.False);
            player.TryGainMesos(500); Assert.That(world.BuyFromShop(Shop,2000000,2,out _),Is.True);
            Assert.That(player.Mesos,Is.EqualTo(400)); Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(2));
            Assert.That(world.SellToShop(Shop,2000000,1,out _),Is.True); Assert.That(player.Mesos,Is.EqualTo(425));
            Assert.That(world.SellToShop(Shop,2000000,2,out _),Is.False);
            Assert.That(world.BuyFromShop(Shop,2000000,int.MaxValue,out _),Is.False);
            player.TryGainMesos(int.MaxValue-player.Mesos); Assert.That(world.SellToShop(Shop,2000000,1,out _),Is.False);
            Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(1));
        }
        [Test] public void ShopRevalidatesNpcIdentityDistanceLifeAndCapacity()
        {
            player.TryGainMesos(1000);
            Assert.That(world.BuyFromShop(new NpcSpawn{NpcId=OfflineEconomy.ShopNpcId},2000000,1,out _),Is.False);
            player.Position=new Vector2(10,.3f); Assert.That(world.BuyFromShop(Shop,2000000,1,out _),Is.False);
            player.Position=new Vector2(0,.3f); player.Inventory.AddItem(2000000,Inventory.SlotsPerCategory*100);
            Assert.That(world.BuyFromShop(Shop,2000000,1,out _),Is.False); Assert.That(player.Mesos,Is.EqualTo(1000));
            player.TakeDamage(int.MaxValue); Assert.That(world.SellToShop(Shop,2000000,1,out _),Is.False);
        }
        [TestCase(2060000)] [TestCase(2061000)] [TestCase(2070000)] [TestCase(2330000)]
        public void ShopAmmunitionPacksCannotBeResoldForAnArbitrage(int id)
        {
            player.TryGainMesos(100); Assert.That(world.BuyFromShop(Shop,id,1,out _),Is.True);
            Assert.That(player.Inventory.GetItemCount(id),Is.EqualTo(100)); Assert.That(world.SellToShop(Shop,id,1,out _),Is.False);
        }
        [Test] public void SaveRoundTripRestoresBaseStatsGearWalletSkillsAndClaimsWithoutDuplication()
        {
            Assert.That(world.RequestPracticeSupplies(out _),Is.True); Assert.That(world.RequestRangedPractice(145,out _),Is.True);
            player.TryEquipItem(1040002,out _); player.TryEquipItem(1452002,out _); player.AddExperience(7); player.TryGainMesos(83);
            player.TakeDamage(17); player.CurrentMP=12; var save=world.CaptureProgress(); int defense=player.WeaponDefense;
            player.Inventory.RemoveItem(2000000,3); player.TrySpendMesos(50); player.STR=100;
            player.ApplyStatBuffs(-1,"test",new Dictionary<BuffType,int>{{BuffType.Speed,20}},1000);
            for(int i=0;i<2;i++) Assert.That(world.TryRestoreProgress(save,out var message),Is.True,message);
            Assert.That(player.Experience,Is.EqualTo(7)); Assert.That(player.Mesos,Is.EqualTo(83)); Assert.That(player.CurrentHP,Is.EqualTo(save.Player.HP));
            Assert.That(player.CurrentMP,Is.EqualTo(12)); Assert.That(player.WeaponDefense,Is.EqualTo(defense)); Assert.That(player.STR,Is.EqualTo(15));
            Assert.That(player.ActiveBuffs,Is.Empty); Assert.That(world.RequestPracticeSupplies(out _),Is.False);
            Assert.That(world.RequestRangedPractice(145,out _),Is.True); Assert.That(player.Inventory.GetItemCount(2060000),Is.EqualTo(100));
        }
        [TestCase("version")] [TestCase("slot")] [TestCase("count")] [TestCase("skill")] [TestCase("map")] [TestCase("exp")] [TestCase("null")]
        public void InvalidSaveLeavesCurrentProgressAndMapUntouched(string fault)
        {
            player.Inventory.AddItem(2000000,2); player.TryGainMesos(90); var save=world.CaptureProgress();
            if(fault=="version") save.Version=99; if(fault=="slot") save.Player.Bag[0].Slot=Inventory.SlotsPerCategory+1;
            if(fault=="count") save.Player.Bag[0].Quantity=101; if(fault=="skill") save.Skills=new SavedSkill[]{null};
            if(fault=="map") save.MapId=999; if(fault=="exp") save.Player.Experience=long.MaxValue;
            if(fault=="null") save.Player.Equipment=null;
            Assert.That(world.TryRestoreProgress(save,out _),Is.False); Assert.That(player.Mesos,Is.EqualTo(90));
            Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(2)); Assert.That(world.CurrentMapId,Is.EqualTo(42));
        }
        [Test] public void SaveFilesRoundTripKeepBackupAndRejectMalformedJsonWithoutTouchingPlayer()
        {
            string directory=Path.Combine(Path.GetTempPath(),"MapleUnity-save-test-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"character.json"); var store=new LocalSaveStore(path);
            try {
                Assert.That(store.TryRead(out _,out _),Is.False);
                player.TryGainMesos(50); Assert.That(store.TryWrite(world.CaptureProgress(),out _),Is.True);
                player.TryGainMesos(20); Assert.That(store.TryWrite(world.CaptureProgress(),out _),Is.True);
                Assert.That(store.TryRead(out var save,out _),Is.True); Assert.That(save.Player.Mesos,Is.EqualTo(70));
                var backup=new LocalSaveStore(path+".bak"); Assert.That(backup.TryRead(out save,out _),Is.True); Assert.That(save.Player.Mesos,Is.EqualTo(50));
                File.WriteAllText(path,"{broken json"); Assert.That(store.TryRead(out _,out _),Is.False); Assert.That(player.Mesos,Is.EqualTo(70));
                var blocked=new LocalSaveStore(Path.Combine(path,"child.json")); Assert.That(blocked.TryWrite(world.CaptureProgress(),out _),Is.False);
                Assert.That(File.ReadAllText(path),Is.EqualTo("{broken json"));
            } finally { foreach(string file in new[]{path,path+".bak",path+".tmp"}) if(File.Exists(file))File.Delete(file); if(Directory.Exists(directory))Directory.Delete(directory); }
        }
        [Test] public void SaveDestinationPreviewDoesNotReplaceTheLiveCollisionService()
        {
            var service=new FootholdService(); service.LoadFootholds(new List<Foothold> {new Foothold(1,-100,0,100,0)});
            int revision=service.Revision; float groundBefore=service.GetGroundBelow(0,-1); var loader=new NxMapLoader("",service);
            Assert.That(loader.PreviewMap(100000102).NpcSpawns.Any(n=>n.NpcId==OfflineEconomy.ShopNpcId),Is.True);
            Assert.That(service.Revision,Is.EqualTo(revision)); Assert.That(service.GetGroundBelow(0,-1),Is.EqualTo(groundBefore));
        }
        [Test] public void OfflineCatalogAndDropIconsExistInTheAvailableNxFiles()
        {
            foreach(var offer in OfflineEconomy.Stock) { Assert.That(data.ItemData.GetItem(offer.ItemId),Is.Not.Null); Assert.That(NXAssetLoader.Instance.LoadItemIcon(offer.ItemId),Is.Not.Null); }
            foreach(int id in new[]{4000000,4000001,4000016,4000019}) Assert.That(NXAssetLoader.Instance.LoadItemIcon(id),Is.Not.Null);
            foreach(int id in new[]{2000000,4000000,4000001,4000016,4000019})
            {
                var sprite=NXAssetLoader.Instance.LoadDroppedItemIcon(id); Assert.That(sprite,Is.Not.Null);
                var origin=data.GetNode(ItemPaths.File(id),ItemPaths.Node(id)+"/info/iconRaw/origin").GetValue<UnityEngine.Vector2>();
                Assert.That(sprite.pivot.x,Is.EqualTo(origin.x).Within(.001)); Assert.That(sprite.rect.height-sprite.pivot.y,Is.EqualTo(origin.y).Within(.001));
            }
            for(int f=0;f<4;f++) Assert.That(NXAssetLoader.Instance.LoadMesoIcon(5,f),Is.Not.Null);
            var shopMap=new NxMapLoader("").GetMap(100000102); Assert.That(shopMap.NpcSpawns.Any(n=>n.NpcId==OfflineEconomy.ShopNpcId),Is.True);
        }
        [TestCase(5,0)] [TestCase(50,1)] [TestCase(100,2)] [TestCase(1000,3)]
        public void MesoIconsUseSourceAmountBandsOriginsAndFrameDelays(int amount,int kind)
        {
            var node=data.GetNode("item",$"Special/0900.img/0900000{kind}/iconRaw/0");
            var sprite=NXAssetLoader.Instance.LoadMesoIcon(amount,0); var origin=node["origin"].GetValue<UnityEngine.Vector2>();
            Assert.That(sprite.pivot.x,Is.EqualTo(origin.x).Within(.001)); Assert.That(sprite.rect.height-sprite.pivot.y,Is.EqualTo(origin.y).Within(.001));
            Assert.That(NXAssetLoader.Instance.MesoFrameAt(amount,0),Is.Zero);
            Assert.That(NXAssetLoader.Instance.MesoFrameAt(amount,(node["delay"]?.GetValue<int>() ?? 100)/1000f+.001f),Is.EqualTo(1));
        }
    }
}
