// Clothing attachment shifts follow HeavenClient Character/Look/Clothing.cpp.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System.Collections.Generic;
using MapleClient.GameLogic.Data;
using UnityEngine;

namespace MapleClient.GameData
{
    public sealed class EquipmentFramePart
    {
        public string Name, Layer;
        public Sprite Sprite;
    }
    public static class NxEquipmentFrames
    {
        public static List<EquipmentFramePart> Load(int id, string stance, int frame, Dictionary<string, Vector2> attachments,
            string expression = "default")
        {
            var result = new List<EquipmentFramePart>();
            var loader = NXAssetLoader.Instance;
            var file = loader.GetNxFile("character");
            string category = ItemPaths.EquipmentCategory(id);
            bool faceAccessory = id / 10000 == 101;
            if (faceAccessory)
            {
                if (stance == "ladder" || stance == "rope") return result;
                stance = expression;
                frame = 0;
            }
            string path = faceAccessory ? $"{category}/{id:D8}.img/{stance}" : $"{category}/{id:D8}.img/{stance}/{frame}";
            var source = file?.GetNode(path);
            if (source == null && faceAccessory)
            {
                path = $"{category}/{id:D8}.img/default";
                source = file?.GetNode(path);
            }
            // RealNxNode.Value can expose a container's first bitmap. Inspect its
            // authored child structure instead of using Value to classify this node.
            if (faceAccessory && source?["0"]?["default"] != null)
            {
                path += "/0";
                source = source["0"];
            }
            // Some items deliberately have no layer on ladder/rope poses.
            if (source == null) return result;
            Vector2 Point(string key) => attachments.TryGetValue(key, out var point) ? point : Vector2.zero;
            foreach (var part in source.Children)
            {
                if (part.Name == "delay" || part.Name == "face") continue;
                var node = loader.ResolveEquipmentNode(part, file);
                if (node == null || !(node.Value is byte[])) continue;
                string parent = null; Vector2 anchor = Vector2.zero;
                if (node["map"] != null)
                    foreach (var point in node["map"].Children)
                        if (point.Value is Vector2 value) { parent = point.Name; anchor = value; }
                Vector2 target = Point("body.map.navel");
                if (category == "Cap" || id / 10000 >= 101 && id / 10000 <= 103)
                    target = Point("body.map.neck") - Point("head.map.neck") + Point("head.map.brow");
                if (category == "Weapon" || category == "Shield")
                {
                    if (parent == "handMove") target = Point("source.handPosition");
                    else if (parent == "hand") target = Point("body.map.navel") + Point("arm.map.hand") - Point("arm.map.navel");
                }
                string spritePath = $"{path}/{part.Name}";
                var sprite = SpriteLoader.LoadSpriteWithShift(node, target - anchor, spritePath);
                if (sprite == null) continue;
                sprite.name = spritePath;
                result.Add(new EquipmentFramePart { Name = part.Name, Layer = node["z"]?.GetValue<string>() ?? part.Name, Sprite = sprite });
            }
            return result;
        }
    }
}
