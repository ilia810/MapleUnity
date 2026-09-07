using System;
using GameData;
using System.Collections.Generic;
using MapleClient.GameData;
using MapleClient.GameView;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    // HeavenClient draws each map layer's objects, then its tiles.
    public static class MapRenderOrder
    {
        public const string SortingLayerName = StageRenderOrder.SortingLayerName;
        public const int LayerStride = StageRenderOrder.LayerStride;
        public static int ObjectOrder(int layer, int sourceZ)
        {
            // Obj.cpp ignores object zM; MapTilesObjs uses signed int8_t keys.
            return layer * LayerStride + 128 + unchecked((sbyte)sourceZ);
        }
        public static int TileOrder(int layer, int bitmapZ, int bitmapZM = 0)
        {
            byte depth = unchecked((byte)bitmapZ);
            if (depth == 0) depth = unchecked((byte)bitmapZM);
            return layer * LayerStride + 256 + 128 + unchecked((sbyte)depth);
        }
        public static int ReadTileOrder(int layer, string tileSet, string variant,
            int number, Func<string, INxNode> getMapNode)
        {
            var bitmap = getMapNode($"Tile/{tileSet}.img/{variant}/{number}")
                ?? getMapNode($"Tile/{tileSet}/{variant}/{number}");
            string link = bitmap?["_outlink"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(link) && link.StartsWith("Map/", StringComparison.Ordinal))
                bitmap = getMapNode(link.Substring(4));
            return TileOrder(layer, bitmap?["z"]?.GetValue<int>() ?? 0,
                bitmap?["zM"]?.GetValue<int>() ?? 0);
        }
        public static void Apply(Transform mapRoot)
        {
            Apply(mapRoot, path => NXDataManagerSingleton.Instance.DataManager.GetNode("map", path));
        }
        public static void Apply(Transform mapRoot, Func<string, INxNode> getMapNode)
        {
            if (mapRoot == null) return;
            foreach (var mapObject in mapRoot.GetComponentsInChildren<MapObject>(true))
            {
                int order = ObjectOrder(mapObject.layer, mapObject.zOrder);
                foreach (var renderer in mapObject.GetComponentsInChildren<SpriteRenderer>(true))
                    SetOrder(renderer, order);
            }
            var tileOrders = new Dictionary<(int layer, string set, string variant, int number), int>();
            foreach (var tile in mapRoot.GetComponentsInChildren<MapTile>(true))
            {
                var key = (tile.layer, tile.tileSet, tile.variant, tile.tileNumber);
                if (!tileOrders.TryGetValue(key, out int order))
                {
                    order = ReadTileOrder(tile.layer, tile.tileSet, tile.variant, tile.tileNumber, getMapNode);
                    tileOrders.Add(key, order);
                }
                tile.sortingOrder = order;
                foreach (var renderer in tile.GetComponentsInChildren<SpriteRenderer>(true))
                    SetOrder(renderer, order);
            }
        }
        public static void SetOrder(SpriteRenderer renderer, int order)
        {
            // Separate Unity layers would place every object above every tile.
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = order;
        }
    }
}
