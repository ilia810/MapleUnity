using System.Collections.Generic;

namespace MapleClient.GameLogic
{
    public class MapData
    {
        public int MapId { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<Platform> Platforms { get; set; }
        public List<Portal> Portals { get; set; }
        public List<MonsterSpawn> MonsterSpawns { get; set; }
        public List<NpcSpawn> NpcSpawns { get; set; }
        public List<Core.LadderInfo> Ladders { get; set; }
        public string BgmId { get; set; }
        
        // Environmental properties
        public bool IsUnderwater { get; set; }

        public MapData()
        {
            Platforms = new List<Platform>();
            Portals = new List<Portal>();
            MonsterSpawns = new List<MonsterSpawn>();
            NpcSpawns = new List<NpcSpawn>();
            Ladders = new List<Core.LadderInfo>();
        }
    }
}