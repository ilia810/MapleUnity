namespace MapleClient.GameData
{
    /// <summary>Map.img groups maps by region, not by the first digit of their IDs.</summary>
    public static class NxMapNames
    {
        public static INxNode Find(INxNode maps, int mapId)
        {
            if (maps == null) return null;
            string id = mapId.ToString();
            foreach (var region in maps.Children)
            {
                var entry = region[id];
                if (entry?["mapName"] != null) return entry;
            }
            return null;
        }

        public static string Name(INxNode maps, int mapId) =>
            Find(maps, mapId)?["mapName"]?.GetValue<string>() ?? $"Map {mapId}";
    }
}
