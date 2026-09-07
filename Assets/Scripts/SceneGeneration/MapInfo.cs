using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    public class MapInfo : MonoBehaviour
    {
        public int mapId;
        public string bgm;
        public int returnMap;
        public int forcedReturn;
        public int fieldLimit;
        public Bounds vrBounds;

        private void Start()
        {
            // FootholdManager's runtime lookup was never serialized by the old
            // generator. Rebuild it for saved scenes as well as generated maps.
            var sourceMap = new MapDataExtractor().ExtractMapData(mapId);
            if (sourceMap == null) return;
            var footholds = GetComponent<FootholdManager>();
            if (footholds == null) footholds = gameObject.AddComponent<FootholdManager>();
            footholds.Initialize(sourceMap.Footholds);
            foreach (var npc in GetComponentsInChildren<NPCBehavior>())
            {
                var position = npc.transform.position;
                if (footholds.TryGetGroundBelow(position.x * 100f, -position.y * 100f, out float groundY))
                    npc.transform.position = new Vector3(position.x, -groundY / 100f, position.z);
                npc.ApplyStageOrder(footholds);
            }

            // Old editor-generated scenes may have saved null sprite references
            // after shutting down the NX cache. Rebuild only damaged categories.
            if (sourceMap.Tiles.Count > 0 && NeedsArt<MapTile>("Tiles", sourceMap.Tiles.Count))
            {
                RemoveContainer("Tiles");
                new TileGenerator().GenerateTiles(sourceMap.Tiles, transform);
            }
            if (sourceMap.Objects.Count > 0 && NeedsArt<MapObject>("Objects", sourceMap.Objects.Count))
            {
                RemoveContainer("Objects");
                new ObjectGenerator().GenerateObjects(sourceMap.Objects, transform);
            }

            var savedBackgrounds = transform.Find("Backgrounds");
            var backgrounds = savedBackgrounds?.GetComponentsInChildren<ViewportBackgroundLayer>(true);
            bool restoreBackgrounds = backgrounds == null ||
                backgrounds.Length != sourceMap.Backgrounds.Count(b => !string.IsNullOrEmpty(b.BgName)) ||
                backgrounds.Any(layer => layer.backgroundData == null || string.IsNullOrEmpty(layer.backgroundData.BgName) ||
                    layer.tileSprite == null || layer.tileSprite.texture == null);
            vrBounds = sourceMap.VRBounds;
            if (restoreBackgrounds)
            {
                RemoveContainer("Backgrounds");
                new BackgroundGenerator().GenerateBackgrounds(sourceMap.Backgrounds, transform, vrBounds);
            }
            MapRenderOrder.Apply(transform);
            var cameraBounds = FindObjectOfType<CameraBounds>();
            if (cameraBounds != null) cameraBounds.SetBounds(vrBounds);
        }

        private bool NeedsArt<T>(string containerName, int expectedCount) where T : Component
        {
            var container = transform.Find(containerName);
            if (container == null) return true;
            var items = container.GetComponentsInChildren<T>(true);
            return items.Length != expectedCount || items.Any(item =>
            {
                var sprite = item.GetComponentInChildren<SpriteRenderer>(true);
                return sprite == null || sprite.sprite == null || sprite.sprite.texture == null;
            });
        }

        private void RemoveContainer(string name)
        {
            var container = transform.Find(name);
            if (container == null) return;
            container.gameObject.SetActive(false);
            Destroy(container.gameObject);
        }
    }
}
