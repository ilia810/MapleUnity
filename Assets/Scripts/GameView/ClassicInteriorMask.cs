using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>Keep tiled scenery inside the authored 800 by 600 interior aperture.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ClassicInteriorMask : MonoBehaviour
    {
        private Bounds bounds;
        private bool active;
        private Sprite sprite;
        private Texture2D texture;
        private readonly SpriteRenderer[] margins = new SpriteRenderer[4];
        public void SetBounds(Bounds value)
        {
            bounds = value;
            active = value.size.x > 0 && value.size.y > 0 && value.size.x <= 8.001f && value.size.y <= 6.001f;
            if (active && sprite == null)
            {
                texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "InteriorBlack", filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.black); texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
                for (int i = 0; i < margins.Length; i++)
                {
                    var go = new GameObject("InteriorMargin" + i); go.transform.SetParent(transform, false);
                    margins[i] = go.AddComponent<SpriteRenderer>(); margins[i].sprite = sprite;
                    margins[i].sortingLayerName = "Foreground"; margins[i].sortingOrder = 32000;
                }
            }
            Present();
        }
        // CaptureScreen can render a different aspect without waiting for LateUpdate.
        private void OnPreCull() => Present();
        public void Present()
        {
            var camera = GetComponent<Camera>();
            float h = camera.orthographicSize, w = h * camera.aspect;
            var p = camera.transform.position;
            for (int i = 0; i < margins.Length; i++) if (margins[i] != null) margins[i].enabled = active;
            if (!active) return;
            Place(0, p.x - w, p.y - h, Mathf.Min(bounds.min.x, p.x + w), p.y + h);
            Place(1, Mathf.Max(bounds.max.x, p.x - w), p.y - h, p.x + w, p.y + h);
            Place(2, Mathf.Max(bounds.min.x, p.x - w), Mathf.Max(bounds.max.y, p.y - h), Mathf.Min(bounds.max.x, p.x + w), p.y + h);
            Place(3, Mathf.Max(bounds.min.x, p.x - w), p.y - h, Mathf.Min(bounds.max.x, p.x + w), Mathf.Min(bounds.min.y, p.y + h));
        }
        private void Place(int index, float left, float bottom, float right, float top)
        {
            var renderer = margins[index]; renderer.enabled = right > left && top > bottom;
            renderer.transform.position = new Vector3((left + right) / 2, (bottom + top) / 2, 0);
            renderer.transform.localScale = new Vector3(Mathf.Max(0, right - left), Mathf.Max(0, top - bottom), 1);
        }
        private void OnDestroy() { if (sprite != null) Destroy(sprite); if (texture != null) Destroy(texture); }
    }
}
