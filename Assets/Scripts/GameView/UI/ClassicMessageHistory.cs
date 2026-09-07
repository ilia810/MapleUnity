using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Local message presentation in the original chat strip. No network chat or input field.</summary>
    public sealed class ClassicMessageHistory : MonoBehaviour
    {
        private sealed class Entry { public string Text; public Color Color; }
        private readonly List<Entry> entries = new List<Entry>();
        private RectTransform hud, panel, viewport, content;
        private Text message, history;
        private Image notice;
        private GameObject channel;
        private Button toggle, up, down;
        private Scrollbar scrollbar;
        private ScrollRect scroll;
        private ClassicTooltipView tooltip;
        private bool initialized, expanded = true, resized;
        private float preferredHeight = 86, lastHeight = -1;
        private string latest = "Welcome to Maple World.";
        public int Count => entries.Count;
        public bool Expanded => expanded;
        public float Height => panel != null ? panel.rect.height : 0;
        public float ScrollPosition => scroll != null ? scroll.verticalNormalizedPosition : 0;
        public string Latest => latest;

        public void Initialize(Font font)
        {
            if (initialized) return;
            initialized = true; hud = (RectTransform)transform; tooltip = ClassicTooltipView.For(hud.parent);
            notice = ClassicWindowSkin.Fill("HudMessageNotice", hud, 3, 4, 563, 25, new Color32(126, 126, 126, 255)); notice.enabled = false;
            channel = ClassicUI.Art("MessageChannel", hud, "StatusBar.img/base/chatTarget", 3, 6).gameObject;
            var label = ClassicWindowSkin.Label("MessageChannelLabel", channel.transform, font, "All", 0, 0, 81, 20, 12, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            Hover(channel, "All local messages", () => "EXP, pickups and system messages appear in the history above this bar.");
            message = ClassicUI.Text("HudMessage", hud, font, latest, 12);
            ClassicUI.Place(message.rectTransform, 90, 5, 438, 23); message.alignment = TextAnchor.MiddleLeft;
            message.horizontalOverflow = HorizontalWrapMode.Overflow; message.supportRichText = false;
            var hit = ClassicWindowSkin.Fill("HudMessageHover", hud, 87, 4, 445, 25, Color.clear, true);
            Hover(hit.gameObject, "System message", () => latest);
            toggle = ClassicUI.ArtButton("ToggleMessageHistory", hud, "UIWindow.img/SoftKeyboard/Bt/0/BtMin", 535, 10, () => SetExpanded(!expanded));
            Hover(toggle.gameObject, "Message history", () => expanded ? "Hide the history. Messages continue to be retained." : "Show the history. Use the arrows or mouse wheel to read earlier messages.");
            up = ClassicWindowSkin.ScrollArrow("MessagePrevious", hud, 551, 4, true, () => Step(1));
            down = ClassicWindowSkin.ScrollArrow("MessageNext", hud, 551, 17, false, () => Step(-1));

            // Retain the native button well; the newer envelope is a small code-drawn glyph.
            var envelope = ClassicUI.Region("MessageEnvelope", hud, "StatusBar.img/base/box", new Rect(0, 0, 20, 19), 574, 7, 20, 19);
            envelope.color = new Color(.75f, .75f, .75f); envelope.raycastTarget = true;
            var memo = ClassicUI.Rect("MemoIcon", envelope.transform, 14, 11); ClassicUI.Place(memo, 3, 4, 14, 11);
            memo.gameObject.AddComponent<ClassicEnvelopeGraphic>().raycastTarget = false;
            var unavailable = envelope.gameObject.AddComponent<Button>(); unavailable.targetGraphic = envelope; unavailable.interactable = false;
            Hover(envelope.gameObject, "Messages", () => "Personal messages are unavailable in local play.");

            panel = ClassicUI.Rect("LocalMessageHistory", hud, 566, preferredHeight);
            var background = panel.gameObject.AddComponent<Image>(); background.color = new Color(0, 0, 0, .38f);
            var grip = ClassicUI.Art("MessageHistoryResize", panel, "StatusBar.img/base/chat", 0, 0); grip.raycastTarget = true;
            grip.gameObject.AddComponent<ClassicHistoryResize>().History = this;
            Hover(grip.gameObject, "Resize history", () => "Drag this edge up or down to change the history height.");
            viewport = ClassicUI.Rect("MessageViewport", panel, 543, 60); ClassicUI.Place(viewport, 4, 6, 543, 60);
            var mask = viewport.gameObject.AddComponent<Image>(); mask.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            history = ClassicWindowSkin.Label("HistoryMessages", viewport, font, "", 0, 0, 543, 60, 12, Color.white);
            history.lineSpacing = 1.05f; history.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = history.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .65f); shadow.effectDistance = new Vector2(1, -1);
            content = history.rectTransform;
            scroll = panel.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
            scrollbar = ClassicWindowSkin.Scrollbar("MessageHistoryScroll", panel, 551, 5, preferredHeight - 5, null, true);
            scrollbar.GetComponent<ClassicScrollbarSkin>().ContentScroll = scroll; scroll.verticalScrollbar = scrollbar;
            scroll.onValueChanged.AddListener(_ => Controls());
            // Wheel input anywhere in the message strip scrolls the same history.
            foreach (var target in new[] { hit.gameObject, channel, up.gameObject, down.gameObject })
                target.AddComponent<ClassicScrollWheel>().Scroll = direction => Step(-direction);
            Layout(); Controls();
        }

        private void Hover(GameObject target, string title, System.Func<string> body)
        {
            var image = target.GetComponent<Image>(); if (image != null) image.raycastTarget = true;
            var hover = target.AddComponent<ClassicHover>(); hover.Enter = e => tooltip.ShowText(target, e.position, title, body()); hover.Exit = () => tooltip.Hide(target);
        }
        public void Post(string value, Color color, bool attention = false)
        {
            if (!initialized || string.IsNullOrWhiteSpace(value)) return;
            bool atEnd = entries.Count == 0 || content.rect.height <= viewport.rect.height || ScrollPosition <= .001f;
            float offset = content.anchoredPosition.y, removedHeight = 0;
            value = value.Replace("\r\n", "\n").Replace('\r', '\n');
            // Keep explicit line breaks and escape all user/data text before applying our colors.
            if (entries.Count == 50) { removedHeight = Measure(entries[0].Text); entries.RemoveAt(0); }
            entries.Add(new Entry { Text = value, Color = color }); latest = value;
            history.text = string.Join("\n", entries.Select(e => "<color=#" + ColorUtility.ToHtmlStringRGB(e.Color) + ">" + ClassicTooltipView.Escape(e.Text) + "</color>"));
            content.sizeDelta = new Vector2(543, Mathf.Ceil(history.preferredHeight));
            channel.SetActive(!attention); notice.enabled = attention;
            ClassicUI.Place(message.rectTransform, attention ? 12 : 90, 5, attention ? 516 : 438, 23);
            message.color = attention ? new Color32(244, 182, 193, 255) : ClassicWindowSkin.Ink;
            FitMessage(value.Replace('\n', ' '));
            Layout();
            if (atEnd) scroll.verticalNormalizedPosition = 0;
            else content.anchoredPosition = new Vector2(0, Mathf.Clamp(offset - removedHeight, 0, Mathf.Max(0, content.rect.height - viewport.rect.height)));
            Controls();
        }
        private float Measure(string text)
        {
            var settings = history.GetGenerationSettings(new Vector2(543, 0));
            return history.cachedTextGeneratorForLayout.GetPreferredHeight(ClassicTooltipView.Escape(text), settings) / history.pixelsPerUnit;
        }
        private void FitMessage(string value)
        {
            ClassicMessageText.Fit(message, value, false);
        }
        public void SetExpanded(bool value) { expanded = value; tooltip?.Hide(); Layout(); Controls(); }
        public void SetHeight(float value) { preferredHeight = Mathf.Clamp(value, 60, 320); resized = true; Layout(); }
        private void Step(int direction)
        {
            SetExpanded(true);
            float overflow = content.rect.height - viewport.rect.height;
            if (overflow > .1f) scroll.verticalNormalizedPosition = Mathf.Clamp01(ScrollPosition + direction * 28 / overflow);
            Controls();
        }
        private void LateUpdate() { Layout(); Controls(); }
        public void Layout()
        {
            if (!initialized) return;
            float scale = Mathf.Min(1, ((RectTransform)hud.parent).rect.width / 800f);
            float available = Mathf.Max(32, ((RectTransform)hud.parent).rect.height / Mathf.Max(.1f, scale) - 87);
            float height = Mathf.Min(available, resized ? preferredHeight : Mathf.Clamp(content.rect.height + 12, 32, preferredHeight));
            panel.gameObject.SetActive(expanded && entries.Count > 0);
            if (Mathf.Abs(lastHeight - height) < .01f) return;
            bool atEnd = ScrollPosition <= .001f;
            lastHeight = height;
            ClassicUI.Place(panel, 2, -height - 2, 566, height);
            ClassicUI.Place(viewport, 4, 6, 543, height - 10);
            ClassicUI.Place(scrollbar.GetComponent<RectTransform>(), 551, 5, 15, height - 5);
            var slide = (RectTransform)scrollbar.handleRect.parent; ClassicUI.Place(slide, 0, 13, 15, Mathf.Max(1, height - 31));
            ClassicUI.Place(scrollbar.transform.Find("MessageHistoryScrollDown").GetComponent<RectTransform>(), 0, height - 18, 15, 13);
            scrollbar.GetComponent<ClassicScrollbarSkin>().Layout();
            if (atEnd) scroll.verticalNormalizedPosition = 0;
        }
        private void Controls()
        {
            if (!initialized) return;
            bool movable = content.rect.height > viewport.rect.height + .1f;
            up.interactable = entries.Count > 0 && movable && ScrollPosition < .9999f;
            down.interactable = entries.Count > 0 && movable && ScrollPosition > .0001f;
            string path = "UIWindow.img/SoftKeyboard/Bt/0/" + (expanded ? "BtMin" : "BtMax");
            var icon = (Image)toggle.targetGraphic; var sprite = ClassicUI.Sprite(path + "/normal/0");
            if (icon.sprite == sprite) return;
            icon.sprite = sprite;
            toggle.spriteState = new SpriteState { highlightedSprite = ClassicUI.Sprite(path + "/mouseOver/0"), pressedSprite = ClassicUI.Sprite(path + "/pressed/0"), disabledSprite = ClassicUI.Sprite(path + "/disabled/0") };
        }
        private void OnDisable() => tooltip?.Hide();
    }

    public static class ClassicMessageText
    {
        // Keep full text in the model/history; only the narrow status strip and transient toast abbreviate.
        public static void Fit(Text label, string value, bool wrap)
        {
            label.text = value;
            if (Fits(label, wrap)) return;
            int low = 0, high = value.Length;
            while (low < high)
            {
                int middle = (low + high + 1) / 2; label.text = value.Substring(0, middle) + "…";
                if (Fits(label, wrap)) low = middle; else high = middle - 1;
            }
            if (low > 0 && char.IsHighSurrogate(value[low - 1])) low--;
            label.text = value.Substring(0, low).TrimEnd() + "…";
        }
        private static bool Fits(Text label, bool wrap) => wrap ? label.preferredHeight <= label.rectTransform.rect.height : label.preferredWidth <= label.rectTransform.rect.width;
    }

    /// <summary>Reference-style disabled envelope. Geometry stays on a 14 x 11 pixel grid.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ClassicEnvelopeGraphic : MaskableGraphic
    {
        public ClassicEnvelopeGraphic() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = GetPixelAdjustedRect();
            Color32 border = new Color32(116, 126, 139, 255), fill = new Color32(215, 222, 228, 255), light = new Color32(246, 249, 251, 255);
            Box(mesh, rect, 0, 0, 14, 11, border); Box(mesh, rect, 1, 1, 12, 9, fill);
            Box(mesh, rect, 1, 9, 12, 1, light); Box(mesh, rect, 1, 1, 1, 8, light);
            for (int x = 0; x < 6; x++)
            {
                Box(mesh, rect, 1 + x, 8 - x, 1, 1, border); Box(mesh, rect, 12 - x, 8 - x, 1, 1, border);
                if (x < 4) { Box(mesh, rect, 1 + x, 1 + x, 1, 1, border); Box(mesh, rect, 12 - x, 1 + x, 1, 1, border); }
            }
        }
        private static void Box(VertexHelper mesh, Rect rect, float x, float y, float w, float h, Color32 color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin + x, rect.yMin + y), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin + x, rect.yMin + y + h), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin + x + w, rect.yMin + y + h), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin + x + w, rect.yMin + y), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start + 2, start + 3, start);
        }
    }

    public sealed class ClassicHistoryResize : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ClassicMessageHistory History;
        private float startY, startHeight;
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            startY = e.position.y; startHeight = History.Height; e.eligibleForClick = false;
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            History.SetHeight(startHeight + (e.position.y - startY) / Mathf.Max(.1f, History.transform.lossyScale.y));
        }
        public void OnEndDrag(PointerEventData e) { }
    }
}
