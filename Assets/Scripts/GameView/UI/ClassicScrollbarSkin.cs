using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Native VScr4 tiles and fixed-size thumb. ScrollRect can continue to supply its
    /// content ratio; after layout we retain the original thumb's pixels and actual hit bounds.</summary>
    public sealed class ClassicScrollbarSkin : MonoBehaviour
    {
        private Scrollbar bar;
        private Image track;
        private Sprite enabledTrack, disabledTrack;
        private float thumbHeight;
        private Button up, down;
        public ScrollRect ContentScroll { get; set; }
        public void Initialize(Scrollbar scrollbar, bool withArrows)
        {
            bar = scrollbar; track = GetComponent<Image>();
            enabledTrack = ClassicUI.Sprite("Basic.img/VScr4/enabled/base");
            disabledTrack = ClassicUI.Sprite("Basic.img/VScr4/disabled/base");
            thumbHeight = ((Image)bar.targetGraphic).sprite.rect.height;
            if (withArrows)
            {
                up = ClassicWindowSkin.ScrollArrow(name + "Up", transform, 0, 0, true, () => Step(1));
                down = ClassicWindowSkin.ScrollArrow(name + "Down", transform, 0, track.rectTransform.rect.height - 13, false, () => Step(-1));
            }
            Layout();
        }
        private void Step(int direction)
        {
            if (!bar.IsInteractable()) return;
            float step = ContentScroll != null ? 40 / Mathf.Max(1, ContentScroll.content.rect.height - ContentScroll.viewport.rect.height) : .1f;
            bar.value = Mathf.Clamp01(bar.value + direction * step);
        }
        private void OnEnable() => Canvas.willRenderCanvases += Layout;
        private void OnDisable() => Canvas.willRenderCanvases -= Layout;
        private void LateUpdate() => Layout();
        public void Layout()
        {
            if (bar == null || bar.handleRect == null) return;
            if (ContentScroll != null)
                bar.interactable = ContentScroll.content.rect.height > ContentScroll.viewport.rect.height + .1f;
            bool movable = bar.IsInteractable();
            track.sprite = movable ? enabledTrack : disabledTrack;
            float height = ((RectTransform)bar.handleRect.parent).rect.height;
            bar.size = Mathf.Min(1, thumbHeight / Mathf.Max(1, height));
            bar.handleRect.gameObject.SetActive(movable);
            if (up != null) up.interactable = movable && bar.value < .9999f;
            if (down != null) down.interactable = movable && bar.value > .0001f;
        }
    }
}
