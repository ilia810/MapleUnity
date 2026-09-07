using System;
using System.Collections.Generic;
using MapleClient.GameData;
using MapleClient.GameLogic.Data;
using GameData;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    // Follow the camera before positioning the viewport's retained tiles.
    [DefaultExecutionOrder(1000)]
    public class ViewportBackgroundLayer : MonoBehaviour
    {
        public BackgroundData backgroundData;
        public Sprite tileSprite;
        public int sortingOrder;
        public bool isForeground;
        public DynamicBackgroundManager manager;
        private Camera viewCamera;
        private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        private NxBackgroundAnimation animation;
        private SourceAnimation timeline;
        private Vector2 initialSize;
        private long revision = -1, ticks;
        private float interpolation;
        public int FrameIndex { get; private set; }
        public int FrameCount => animation?.Sprites.Length ?? (tileSprite != null ? 1 : 0);
        public long AppliedTicks => ticks;

        public void Initialize()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (manager == null) manager = GetComponentInParent<DynamicBackgroundManager>();
            if (tiles.Count == 0) tiles.AddRange(GetComponentsInChildren<SpriteRenderer>(true));
            if (animation == null && !string.IsNullOrEmpty(backgroundData?.BgName))
            {
                animation = NxBackgroundAnimation.Load(NXDataManagerSingleton.Instance.DataManager,
                    backgroundData.BgName, backgroundData.SpriteNo, backgroundData.Ani != 0);
                if (animation != null)
                {
                    timeline = new SourceAnimation(animation.Frames, animation.Zigzag);
                    tileSprite = animation.Sprites[0];
                }
                else Debug.LogWarning($"Background sprite not found: {backgroundData.BgName}/{backgroundData.SpriteNo}");
            }
            if (initialSize == Vector2.zero && tileSprite != null)
                initialSize = tileSprite.rect.size / tileSprite.pixelsPerUnit;
            UpdateTiling();
        }

        private void Start()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            if (backgroundData == null || tileSprite == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            Render(manager?.Ticks ?? 0, manager?.Interpolation ?? 0, manager?.Revision ?? 0);
        }

        public void Render(long simulationTicks, float fraction, long mapRevision = 0)
        {
            if (revision != mapRevision) { timeline?.Reset(); revision = mapRevision; }
            ticks = simulationTicks;
            interpolation = Mathf.Clamp01(fraction);
            UpdateTiling();
        }

        private void UpdateTiling()
        {
            if (backgroundData == null || tileSprite == null || viewCamera == null) return;
            float alpha = 1f, scale = 1f;
            if (timeline != null)
            {
                timeline.AdvanceTo(ticks);
                FrameIndex = timeline.Sample(interpolation, out alpha, out scale);
                tileSprite = animation.Sprites[FrameIndex];
            }
            if (initialSize == Vector2.zero) initialSize = tileSprite.rect.size / tileSprite.pixelsPerUnit;
            var data = backgroundData;
            var cameraPosition = viewCamera.transform.position;
            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            int type = data.Type;
            bool tileX = type == 1 || type == 3 || type == 4 || type == 6 || type == 7;
            bool tileY = type == 2 || type == 3 || type == 5 || type == 6 || type == 7;
            bool moveX = (type == 4 || type == 6) && data.RX != 0;
            bool moveY = (type == 5 || type == 7) && data.RY != 0;

            // MovingObject interpolates between the last two completed 8 ms ticks.
            // Zero-speed moving types follow the stationary camera/parallax branch.
            double sampleTick = ticks > 0 ? ticks - 1 + (double)interpolation : 0;
            double anchorX = data.X / 100.0 + (moveX
                ? data.RX / 1600.0 * sampleTick : (1 + data.RX / 100.0) * cameraPosition.x);
            double anchorY = -data.Y / 100.0 + (moveY
                ? -data.RY / 1600.0 * sampleTick : (1 + data.RY / 100.0) * cameraPosition.y);
            // Source rounds screen coordinates before tiling; retain pixel alignment as the camera moves.
            float x = cameraPosition.x - halfWidth + (float)(Math.Round((anchorX - cameraPosition.x + halfWidth) * 100, MidpointRounding.AwayFromZero) / 100);
            float y = cameraPosition.y + halfHeight - (float)(Math.Round((cameraPosition.y + halfHeight - anchorY) * 100, MidpointRounding.AwayFromZero) / 100);
            // Background::settype captures the first frame's dimensions, even when later frames change size.
            float stepX = data.CX > 0 ? data.CX / 100f : Mathf.Max(.01f, initialSize.x);
            float stepY = data.CY > 0 ? data.CY / 100f : Mathf.Max(.01f, initialSize.y);

            // Use each frame's bitmap bounds, external NX origin, flip and animated scale.
            var bounds = tileSprite.bounds;
            float left = (data.F != 0 ? -bounds.max.x : bounds.min.x) * scale;
            float right = (data.F != 0 ? -bounds.min.x : bounds.max.x) * scale;
            float minX = Mathf.Min(left, right), maxX = Mathf.Max(left, right);
            float minY = Mathf.Min(bounds.min.y * scale, bounds.max.y * scale);
            float maxY = Mathf.Max(bounds.min.y * scale, bounds.max.y * scale);
            int firstX = tileX ? Mathf.FloorToInt((cameraPosition.x - halfWidth - x - maxX) / stepX) : 0;
            int lastX = tileX ? Mathf.CeilToInt((cameraPosition.x + halfWidth - x - minX) / stepX) : 0;
            int firstY = tileY ? Mathf.FloorToInt((cameraPosition.y - halfHeight - y - maxY) / stepY) : 0;
            int lastY = tileY ? Mathf.CeilToInt((cameraPosition.y + halfHeight - y - minY) / stepY) : 0;

            int index = 0;
            for (int ix = firstX; ix <= lastX; ix++)
            for (int iy = firstY; iy <= lastY; iy++)
            {
                SpriteRenderer tile;
                if (index < tiles.Count) tile = tiles[index];
                else
                {
                    var obj = new GameObject("BackgroundTile");
                    obj.transform.SetParent(transform, false);
                    tile = obj.AddComponent<SpriteRenderer>();
                    tiles.Add(tile);
                }
                tile.gameObject.SetActive(true);
                tile.sprite = tileSprite;
                tile.sortingLayerName = isForeground ? "Foreground" : "Background";
                tile.sortingOrder = sortingOrder;
                tile.color = new Color(1f, 1f, 1f, Mathf.Clamp01(data.A / 255f * alpha));
                tile.flipX = data.F != 0;
                tile.transform.localScale = new Vector3(scale, scale, 1);
                tile.transform.position = new Vector3(x + ix * stepX, y + iy * stepY, transform.position.z);
                index++;
            }
            for (int i = index; i < tiles.Count; i++) tiles[i].gameObject.SetActive(false);
        }

        public void LogCoverage()
        {
            Debug.Log($"Background {backgroundData?.BgName}: {tiles.Count} viewport tiles");
        }
    }
}
