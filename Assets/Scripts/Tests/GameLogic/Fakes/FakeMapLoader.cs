using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Tests.Fakes
{
    public class FakeMapLoader : IMapLoader
    {
        private readonly Dictionary<int, MapData> maps = new Dictionary<int, MapData>();

        public void AddMap(int mapId, MapData mapData)
        {
            maps[mapId] = mapData;
        }

        public MapData GetMap(int mapId)
        {
            return maps.TryGetValue(mapId, out var mapData) ? mapData : null;
        }
    }
}