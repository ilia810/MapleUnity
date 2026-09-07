using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameData
{
    public static class NxWorldMapIndex
    {
        public static INxNode FindRegion(INxNode atlases, int mapId)
        {
            if (atlases == null) return null;
            return atlases.Children.Where(n => n["MapList"]?.Children.Any(spot =>
                    spot["mapNo"]?.Children.Any(m => m.GetValue<int>() == mapId) == true) == true)
                .OrderByDescending(n => Depth(atlases,n)).FirstOrDefault() ?? atlases["WorldMap.img"];
        }
        private static int Depth(INxNode atlases, INxNode node)
        {
            var visited = new HashSet<string>(); int depth = 0;
            while (node != null && visited.Add(node.Name))
            {
                string parent = node["info"]?["parentMap"]?.GetValue<string>();
                if (string.IsNullOrEmpty(parent)) break;
                node = atlases[parent.EndsWith(".img") ? parent : parent+".img"]; depth++;
            }
            return depth;
        }
    }
}
