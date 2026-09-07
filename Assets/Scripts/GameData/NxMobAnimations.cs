using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace MapleClient.GameData
{
    public sealed class MobAnimationFrame
    {
        public SpriteWithOrigin Image;
        public string Path;
        public int DelayMilliseconds;
        public float StartAlpha, EndAlpha, StartScale, EndScale;
    }

    public sealed class MobAnimation
    {
        public IReadOnlyList<MobAnimationFrame> Frames { get; }
        public bool Zigzag { get; }
        public double DurationSeconds { get; }
        private readonly int[] sequence;

        public MobAnimation(IReadOnlyList<MobAnimationFrame> frames, bool zigzag)
        {
            Frames = frames;
            Zigzag = zigzag;
            var order = Enumerable.Range(0, frames.Count).ToList();
            if (zigzag) for (int i = frames.Count - 2; i > 0; i--) order.Add(i);
            sequence = order.ToArray();
            DurationSeconds = sequence.Sum(i => frames[i].DelayMilliseconds) / 1000.0;
        }

        public int Sample(double seconds, bool loop, out float fraction)
        {
            double milliseconds = Math.Max(0, seconds) * 1000;
            if (loop && DurationSeconds > 0) milliseconds %= DurationSeconds * 1000;
            foreach (int index in sequence)
            {
                var frame = Frames[index];
                if (milliseconds < frame.DelayMilliseconds)
                {
                    fraction = (float)(milliseconds / frame.DelayMilliseconds);
                    return index;
                }
                milliseconds -= frame.DelayMilliseconds;
            }
            fraction = 1;
            return sequence[sequence.Length - 1];
        }
    }

    /// <summary>Mob::Mob info/link and Animation/Frame bitmap metadata, loaded per requested action.</summary>
    public sealed class NxMobAnimations
    {
        private readonly NXDataManager manager;
        private readonly Dictionary<(int, string), MobAnimation> cache = new Dictionary<(int, string), MobAnimation>();
        public NxMobAnimations(NXDataManager manager) { this.manager = manager; }

        public MobAnimation Get(int monsterId, string action)
        {
            var key = (monsterId, action);
            if (cache.TryGetValue(key, out var cached) && cached.Frames.All(f => f.Image.Sprite != null)) return cached;
            string id = monsterId.ToString("D7");
            var visited = new HashSet<string>();
            INxNode source = null;
            while (visited.Add(id))
            {
                source = manager.GetNode("mob", id + ".img");
                if (source == null) return null;
                string link = source["info"]?["link"]?.GetValue<string>();
                if (string.IsNullOrEmpty(link)) break;
                string target = int.TryParse(link, out int number) ? number.ToString("D7") : link;
                if (manager.GetNode("mob", target + ".img") == null) break;
                if (visited.Contains(target)) return null;
                id = target;
            }
            var animation = source?[action];
            if (animation == null) return null;
            var frames = new List<MobAnimationFrame>();
            foreach (var node in animation.Children.Where(n => int.TryParse(n.Name, out int i) && i >= 0)
                .OrderBy(n => int.Parse(n.Name)))
            {
                string path = $"mob/{id}.img/{action}/{node.Name}";
                var image = SpriteLoader.LoadSpriteWithOrigin(node, path, manager);
                if (image?.Sprite == null) continue;
                image.Sprite.name = path;
                int delay = node["delay"]?.GetValue<int>() ?? 100;
                int? a0 = node["a0"]?.GetValue<int>(), a1 = node["a1"]?.GetValue<int>();
                int? z0 = node["z0"]?.GetValue<int>(), z1 = node["z1"]?.GetValue<int>();
                frames.Add(new MobAnimationFrame {
                    Image = image, Path = path, DelayMilliseconds = delay > 0 ? delay : 100,
                    StartAlpha = (a0 ?? (a1.HasValue ? 255 - a1.Value : 255)) / 255f,
                    EndAlpha = (a1 ?? (a0.HasValue ? 255 - a0.Value : 255)) / 255f,
                    StartScale = (z0 ?? 100) / 100f, EndScale = (z1 ?? (z0.HasValue ? 0 : 100)) / 100f
                });
            }
            if (frames.Count == 0) return null;
            var result = new MobAnimation(frames, (animation["zigzag"]?.GetValue<int>() ?? 0) != 0);
            cache[key] = result;
            return result;
        }
    }
}
