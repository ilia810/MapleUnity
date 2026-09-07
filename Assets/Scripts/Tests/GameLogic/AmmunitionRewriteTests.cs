using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class AmmunitionRewriteTests
    {
        private static int[][] Rows(string name) => File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(),
            "Assets/Scripts/Tests/GameLogic/Fixtures/" + name + ".csv")).Skip(1).Select(s => s.Split(',').Select(int.Parse).ToArray()).ToArray();
        [TestCase(0)] [TestCase(130)] [TestCase(137)] [TestCase(145)] [TestCase(146)] [TestCase(147)] [TestCase(148)] [TestCase(149)]
        public void FirstCompatibleInventorySlotMatchesCpp(int weapon)
        {
            foreach (var row in Rows("HeavenAmmunition").Where(r => r[0] == weapon))
            {
                var slots = new SortedDictionary<int, KeyValuePair<int, int>>();
                void Put(int slot, int id, int count) => slots[slot] = new KeyValuePair<int, int>(id, count);
                Put(1,2000000,20);Put(3,2060001,7);Put(4,2060000,9);Put(5,2061000,8);Put(8,2070001,2);Put(10,2070000,9);Put(11,2330000,1);
                switch (row[1]) {
                    case 1: Put(3,2060001,0);Put(8,2070001,0);Put(11,2330000,0);break;
                    case 2: slots.Remove(3);Put(2,2060000,2);Put(7,2070000,3);break;
                    case 3: slots.Clear();break;
                    case 4: Put(2,2060001,0);Put(6,2070001,0);break;
                    case 5: foreach(int key in new[]{3,4,5,8,10,11})slots.Remove(key);break;
                }
                int selected = AmmunitionRules.Select(weapon, slots.Values);
                Assert.That(selected, Is.EqualTo(row[2]), "Scenario " + row[1]);
                int bonus = selected == 0 ? 0 : selected / 1000 == 2070 ? 15 + 2 * (selected % 1000) : selected / 1000 == 2330 ? 10 : selected % 1000;
                Assert.That(bonus, Is.EqualTo(row[3]));
            }
        }
        public static int[] Speeds => Enumerable.Range(0, 16).ToArray();
        [TestCaseSource(nameof(Speeds))]
        public void GunLaunchDelayMatchesCppFloatSpeedCalculation(int speed)
        {
            var row = Rows("HeavenGunDelay").Single(r => r[0] == speed);
            var gun = new WeaponProfile { AttackType = 9, AttackSpeed = speed, ActionDelays = new[] { 240, 540, 100 },
                ActionStances = new[] { CharacterState.Shoot2, CharacterState.Attack1, CharacterState.Shoot2 }, ActionFrames = new[] { 0, 0, 0 }, ActionHitDelay = 240 };
            var motion = gun.CreateAttack(0, false);
            Assert.That(motion.HitDelayMilliseconds, Is.EqualTo(row[1]));
            int ticks = (240 + row[2] - 1) / row[2];
            for (int i = 1; i < ticks; i++) motion.Advance(.008f);
            Assert.That(motion.Stance, Is.EqualTo(CharacterState.Shoot2)); motion.Advance(.008f);
            Assert.That(motion.Stance, Is.EqualTo(CharacterState.Attack1));
        }
        [Test]
        public void LocalInventoryKeepsSlotOrderAndReusesVacatedSlotsWithoutItemIdSorting()
        {
            var bag = new Inventory();bag.AddItem(2000000,1);bag.AddItem(2060001,2);bag.AddItem(2060000,5);
            Assert.That(AmmunitionRules.Select(145, bag.GetItemsInSlotOrder()), Is.EqualTo(2060001));
            bag.RemoveItem(2060001,2);
            Assert.That(AmmunitionRules.Select(145, bag.GetItemsInSlotOrder()), Is.EqualTo(2060000));
            bag.AddItem(2060001,1);
            Assert.That(AmmunitionRules.Select(145, bag.GetItemsInSlotOrder()), Is.EqualTo(2060001));
            bag.AddItem(2060000,1); Assert.That(bag.GetItemCount(2060000), Is.EqualTo(6));
            Assert.That(bag.RemoveItem(2060001,2), Is.False); Assert.That(bag.GetItemCount(2060001), Is.EqualTo(1));
        }
    }
}
