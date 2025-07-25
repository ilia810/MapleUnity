using System.Collections.Generic;

namespace MapleClient.GameLogic
{
    public class MonsterTemplate
    {
        public int MonsterId { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int MaxHP { get; set; }
        public int MaxMP { get; set; }
        public int Exp { get; set; }
        public int PhysicalDamage { get; set; }
        public int MagicDamage { get; set; }
        public int PhysicalDefense { get; set; }
        public int MagicDefense { get; set; }
        public int Accuracy { get; set; }
        public int Avoidability { get; set; }
        public float Speed { get; set; }
        public int MesoMin { get; set; }
        public int MesoMax { get; set; }
        public List<DropInfo> DropTable { get; set; }
    }
}