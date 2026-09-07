using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class InventorySlotIntegrationTests
    {
        private sealed class Maps : IMapLoader
        {
            public readonly MapData Map = new MapData { MapId=42,
                Platforms=new List<Platform>{new Platform{Id=1,X1=-3000,X2=3000,Y1=0,Y2=0}},
                Portals=new List<Portal>{new Portal{Id=0,Name="sp",Type=MapleClient.GameLogic.PortalType.Spawn}},
                NpcSpawns=new List<NpcSpawn>{new NpcSpawn{NpcId=OfflineEconomy.ShopNpcId}} };
            public MapData GetMap(int id) => id==42?Map:null;
        }
        private GameWorld world;
        private Player player;
        private Maps maps;
        private Inventory Bag => player.Inventory;
        [SetUp] public void Setup()
        {
            maps=new Maps();world=new GameWorld(null,maps,assetProvider:NXDataManagerSingleton.Instance.DataManager);
            world.LoadMap(42);player=world.Player;
        }
        private static InventoryStack Stack(int slot,int id,int count) => new InventoryStack{Slot=slot,ItemId=id,Quantity=count};
        private string[] Contents() => Bag.GetStacks().Select(s=>$"{s.Category}:{s.Slot}:{s.ItemId}:{s.Quantity}").ToArray();
        private bool Move(int category,int source,int destination,int id) => world.MoveInventoryStack(category,source,destination,id,Bag.Revision,out _);

        [Test] public void SixRowsAcceptSlotThirtyAndRestoreOlderSparseBagsWithoutRenumbering()
        {
            Assert.That(Inventory.SlotsPerCategory,Is.EqualTo(30));
            Assert.That(Bag.Restore(new[]{Stack(24,2000000,7),Stack(18,1302000,1)}),Is.True);
            Assert.That(Bag.GetStack(2,24).Quantity,Is.EqualTo(7));
            Assert.That(Move(2,24,30,2000000),Is.True);
            var saved=world.CaptureProgress();Bag.RemoveItem(2000000,7);
            Assert.That(world.TryRestoreProgress(saved,out var message),Is.True,message);
            Assert.That(Bag.GetStack(2,30).Quantity,Is.EqualTo(7));Assert.That(Bag.GetStack(1,18).ItemId,Is.EqualTo(1302000));
        }
        [TestCase(false)] [TestCase(true)]
        public void OrganizingOneCategoryPreservesCopiesQuantitiesAndOtherCategories(bool sort)
        {
            Assert.That(Bag.Restore(new[]{Stack(3,2000003,9),Stack(19,2000000,7),Stack(30,2000000,2),Stack(18,1302000,1)}),Is.True);
            long revision=Bag.Revision;int changed=0,added=0,removed=0;Bag.Changed+=()=>changed++;Bag.ItemAdded+=(_,n)=>added+=n;Bag.ItemRemoved+=(_,n)=>removed+=n;
            Assert.That(Bag.Organize(2,sort),Is.True);
            Assert.That(Bag.GetStacks().Where(s=>s.Category==2).Select(s=>s.Slot),Is.EqualTo(new[]{1,2,3}));
            Assert.That(Bag.GetStack(2,1).ItemId,Is.EqualTo(sort?2000000:2000003));
            Assert.That(Bag.GetItemCount(2000000),Is.EqualTo(9));Assert.That(Bag.GetItemCount(2000003),Is.EqualTo(9));
            Assert.That(Bag.GetStack(1,18).ItemId,Is.EqualTo(1302000));Assert.That(changed,Is.EqualTo(1));Assert.That(added+removed,Is.Zero);
            Assert.That(Bag.Revision,Is.EqualTo(revision+1));Assert.That(Bag.Organize(2,sort),Is.False);Assert.That(Bag.Revision,Is.EqualTo(revision+1));
        }
        [Test] public void MovingToAnEmptyCellRetainsGapsAndDoesNotEmitPickupOrRemovalEvents()
        {
            Bag.AddItem(2000000,12);int changed=0,added=0,removed=0;
            Bag.Changed+=()=>changed++;Bag.ItemAdded+=(id,n)=>added++;Bag.ItemRemoved+=(id,n)=>removed++;
            Assert.That(Move(2,1,24,2000000),Is.True);
            Assert.That(Bag.GetStack(2,1),Is.Null);Assert.That(Bag.GetStack(2,24).Quantity,Is.EqualTo(12));
            Assert.That(changed,Is.EqualTo(1));Assert.That(added+removed,Is.Zero);
            var copy=Bag.GetStack(2,24);copy.Quantity=999;copy.Slot=2;
            Assert.That(Bag.GetStack(2,24).Quantity,Is.EqualTo(12),"Read snapshots cannot mutate the bag.");
        }
        [Test] public void DifferentItemsSwapEvenWhenTheCategoryIsFull()
        {
            Assert.That(Bag.Restore(Enumerable.Range(1,Inventory.SlotsPerCategory).Select(s=>Stack(s,s==Inventory.SlotsPerCategory?2000003:2000000,100)).ToArray()),Is.True);
            Assert.That(Move(2,1,Inventory.SlotsPerCategory,2000000),Is.True);
            Assert.That(Bag.GetStack(2,1).ItemId,Is.EqualTo(2000003));Assert.That(Bag.GetStack(2,Inventory.SlotsPerCategory).ItemId,Is.EqualTo(2000000));
            Assert.That(Bag.GetItemCount(2000000),Is.EqualTo((Inventory.SlotsPerCategory-1)*100));Assert.That(Bag.UsedSlots(2),Is.EqualTo(Inventory.SlotsPerCategory));
        }
        [TestCase(70,60,30,100,true)] [TestCase(40,50,0,90,true)] [TestCase(7,100,7,100,false)]
        public void CombiningStacksRespectsNxCapacityAndRetainsOverflow(int source,int target,int remaining,int filled,bool succeeds)
        {
            Assert.That(Bag.Restore(new[]{Stack(8,2000000,source),Stack(24,2000000,target)}),Is.True);
            long revision=Bag.Revision;
            Assert.That(Move(2,8,24,2000000),Is.EqualTo(succeeds));
            Assert.That(Bag.GetStack(2,8)?.Quantity??0,Is.EqualTo(remaining));Assert.That(Bag.GetStack(2,24).Quantity,Is.EqualTo(filled));
            Assert.That(Bag.GetItemCount(2000000),Is.EqualTo(source+target));Assert.That(Bag.Revision,Is.EqualTo(revision+(succeeds?1:0)));
        }
        [TestCase(0,1,2,2000000)] [TestCase(6,1,2,2000000)] [TestCase(2,0,2,2000000)]
        [TestCase(2,31,2,2000000)] [TestCase(2,1,0,2000000)] [TestCase(2,1,31,2000000)]
        [TestCase(1,1,2,2000000)] [TestCase(2,3,2,2000000)] [TestCase(2,1,2,2000003)]
        public void InvalidAddressesAndStaleItemIdsCannotMutateAnyCategory(int category,int source,int destination,int id)
        {
            Bag.AddItem(2000000,5);Bag.AddItem(1302000,1);var before=Contents();long revision=Bag.Revision;
            Assert.That(Move(category,source,destination,id),Is.False);Assert.That(Contents(),Is.EqualTo(before));Assert.That(Bag.Revision,Is.EqualTo(revision));
        }
        [Test] public void DroppingOnTheSameCellIsANoop()
        {
            Bag.AddItem(2000000,7);long revision=Bag.Revision;
            Assert.That(Move(2,1,1,2000000),Is.True);Assert.That(Bag.Revision,Is.EqualTo(revision));
        }
        [Test] public void AChangedBagRejectsTheOldDragEvenWhenTheItemIdStillMatches()
        {
            Bag.AddItem(2000000,5);long revision=Bag.Revision;Bag.AddItem(2000000,1);
            Assert.That(world.MoveInventoryStack(2,1,2,2000000,revision,out _),Is.False);
            Assert.That(Bag.GetStack(2,1).Quantity,Is.EqualTo(6));Assert.That(Bag.GetStack(2,2),Is.Null);
        }
        [Test] public void ConsumablesSpendTheSelectedStackAndCannotFallBackToAnotherCopy()
        {
            Assert.That(Bag.Restore(new[]{Stack(1,2000000,30),Stack(9,2000000,2)}),Is.True);player.SetHPMP(1,player.MaxMP);
            Assert.That(world.UseInventorySlot(2,9,2000000,out _),Is.True);
            Assert.That(Bag.GetStack(2,1).Quantity,Is.EqualTo(30));Assert.That(Bag.GetStack(2,9).Quantity,Is.EqualTo(1));
            Assert.That(Move(2,9,10,2000000),Is.True);int hp=player.CurrentHP;
            Assert.That(world.UseInventorySlot(2,9,2000000,out _),Is.False);Assert.That(player.CurrentHP,Is.EqualTo(hp));
            Assert.That(Bag.GetItemCount(2000000),Is.EqualTo(31));
        }
        [Test] public void DuplicateEquipmentConsumesOnlyTheChosenCopy()
        {
            Assert.That(Bag.Restore(new[]{Stack(1,1302000,1),Stack(18,1302000,1)}),Is.True);
            Assert.That(world.UseInventorySlot(1,18,1302000,out _),Is.True);
            Assert.That(Bag.GetStack(1,1).ItemId,Is.EqualTo(1302000));Assert.That(Bag.GetStack(1,18),Is.Null);
            Assert.That(player.GetEquippedItems()[EquipSlot.Weapon],Is.EqualTo(1302000));
        }
        [Test] public void ExactSlotEquipmentExchangeRemainsAtomicWhenTwoDisplacedPiecesCannotFit()
        {
            player.Level=5;Bag.AddItem(1302000,1);Bag.AddItem(1092003,1);
            Assert.That(player.TryEquipItem(1302000,out _),Is.True);Assert.That(player.TryEquipItem(1092003,out _),Is.True);
            Bag.AddItem(1302000,Inventory.SlotsPerCategory-1);Bag.AddItem(1402009,1);var before=Contents();
            Assert.That(world.UseInventorySlot(1,Inventory.SlotsPerCategory,1402009,out _),Is.False);Assert.That(Contents(),Is.EqualTo(before));
            Assert.That(player.GetEquippedItems()[EquipSlot.Weapon],Is.EqualTo(1302000));Assert.That(player.GetEquippedItems()[EquipSlot.Shield],Is.EqualTo(1092003));
        }
        [Test] public void ReplacedEquipmentReturnsToTheChosenSlotBeforeEarlierBagGaps()
        {
            Bag.AddItem(1302000,1);Assert.That(player.TryEquipItem(1302000,out _),Is.True);
            Assert.That(Bag.Restore(new[]{Stack(1,1302000,1),Stack(18,1302000,1)}),Is.True);
            Assert.That(world.UseInventorySlot(1,18,1302000,out _),Is.True);
            Assert.That(Bag.GetStack(1,1).ItemId,Is.EqualTo(1302000));Assert.That(Bag.GetStack(1,18).ItemId,Is.EqualTo(1302000));
            Assert.That(Bag.GetStack(1,2),Is.Null);Assert.That(Bag.GetItemCount(1302000),Is.EqualTo(2));
        }
        [Test] public void ReorderingSelectsTheFirstCompatibleAmmunitionAndSurvivesSaveRestore()
        {
            Assert.That(world.RequestRangedPractice(145,out _),Is.True);Assert.That(player.TryEquipItem(1452002,out _),Is.True);
            Bag.AddItem(2060001,7);var better=Bag.GetStacks().Single(s=>s.ItemId==2060001);
            Assert.That(Move(2,better.Slot,1,2060001),Is.True);Assert.That(player.AmmunitionId,Is.EqualTo(2060001));Assert.That(player.AmmunitionCount,Is.EqualTo(7));
            var saved=world.CaptureProgress();var contents=Contents();
            Assert.That(Move(2,1,24,2060001),Is.True);Assert.That(player.AmmunitionId,Is.EqualTo(2060000));
            Assert.That(world.TryRestoreProgress(saved,out var message),Is.True,message);
            Assert.That(Contents(),Is.EqualTo(contents));Assert.That(player.AmmunitionId,Is.EqualTo(2060001));
        }
        [Test] public void DeathAndAnActiveAttackBlockBagMoves()
        {
            Bag.AddItem(1302000,1);Assert.That(player.TryEquipItem(1302000,out _),Is.True);Bag.AddItem(2000000,5);
            var combat=new Combat();combat.PerformBasicAttack(player,new List<Monster>(),1);Assert.That(player.IsBasicAttacking,Is.True);
            Assert.That(Move(2,1,2,2000000),Is.False);player.ResetMovementForMap();player.TakeDamage(int.MaxValue);
            Assert.That(Move(2,1,2,2000000),Is.False);Assert.That(Bag.GetStack(2,1).Quantity,Is.EqualTo(5));
        }
        [Test] public void ShopSellsOnlyTheChosenStackAndRejectsOverdrawOrStaleAddresses()
        {
            Assert.That(Bag.Restore(new[]{Stack(1,2000000,30),Stack(9,2000000,2)}),Is.True);var shop=maps.Map.NpcSpawns[0];
            Assert.That(world.SellToShop(shop,2000000,3,out _,9),Is.False);Assert.That(player.Mesos,Is.Zero);
            Assert.That(world.SellToShop(shop,2000000,1,out _,9),Is.True);Assert.That(player.Mesos,Is.EqualTo(25));
            Assert.That(Bag.GetStack(2,1).Quantity,Is.EqualTo(30));Assert.That(Bag.GetStack(2,9).Quantity,Is.EqualTo(1));
            Assert.That(Move(2,9,10,2000000),Is.True);Assert.That(world.SellToShop(shop,2000000,1,out _,9),Is.False);
            Assert.That(player.Mesos,Is.EqualTo(25));Assert.That(Bag.GetItemCount(2000000),Is.EqualTo(31));
        }
        [Test] public void SlotExchangeCannotTakeMissingQuantityFromAnotherStackOrCommitAnOverflowingGrant()
        {
            Assert.That(Bag.Restore(new[]{Stack(1,2000000,30),Stack(9,2000000,2)}),Is.True);var before=Contents();
            Assert.That(Bag.TryExchangeSlot(2,9,2000000,3,null),Is.False);
            Assert.That(Bag.TryExchangeSlot(2,9,2000000,1,new Dictionary<int,int>{{1302000,Inventory.SlotsPerCategory+1}}),Is.False);
            Assert.That(Contents(),Is.EqualTo(before));
        }
    }
}
