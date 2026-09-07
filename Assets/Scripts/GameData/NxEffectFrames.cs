using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameData
{
    internal static class NxEffectFrames
    {
        private static int Int(INxNode node, string key, int fallback = 0) => node?[key]?.GetValue<int>() ?? fallback;
        public static SkillEffectDefinition Read(NXDataManager manager, string file, string path)
        {
            var node = manager.GetNode(file, path);
            var frames = new List<AfterimageFrame>();
            // Animation.cpp supports both a single bitmap (arrows/stars/bullets) and numbered frames.
            var numbered = node?.Children.Where(n => int.TryParse(n.Name, out _)).OrderBy(n => int.Parse(n.Name)).ToArray() ?? new INxNode[0];
            // RealNxNode.Value can expose a container's first bitmap for legacy callers.
            // Numeric children therefore take precedence over that convenience value.
            var sources = numbered.Length > 0 ? numbered : node?.Value is byte[] ? new[] { node } : new INxNode[0];
            foreach (var frame in sources)
            {
                int? a0 = frame["a0"]?.GetValue<int>(), a1 = frame["a1"]?.GetValue<int>();
                int? z0 = frame["z0"]?.GetValue<int>(), z1 = frame["z1"]?.GetValue<int>();
                frames.Add(new AfterimageFrame { Path = ReferenceEquals(frame, node) ? path : path + "/" + frame.Name,
                    DelayMilliseconds = Int(frame, "delay", 100) > 0 ? Int(frame, "delay", 100) : 100,
                    StartAlpha = (a0 ?? (a1.HasValue ? 255 - a1.Value : 255)) / 255f,
                    EndAlpha = (a1 ?? (a0.HasValue ? 255 - a0.Value : 255)) / 255f,
                    StartScale = (z0 ?? 100) / 100f, EndScale = (z1 ?? (z0.HasValue ? 0 : 100)) / 100f });
            }
            return new SkillEffectDefinition { Frames = frames.ToArray(), Position = Int(node, "pos"), Z = Int(node, "z"), AssetFile = file };
        }
    }
}
