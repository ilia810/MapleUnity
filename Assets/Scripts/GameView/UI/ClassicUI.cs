using System.Collections.Generic;
using MapleClient.GameData;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace MapleClient.GameView.UI
{
    /// <summary>Classic UI.nx artwork. Coordinates are bitmap top-left pixels. Window layout uses
    /// the actual classic textures, avoiding the source client's mixed-version offsets.</summary>
    public static class ClassicUI
    {
        public static Sprite Sprite(string path)
        {
            var sprite = MapleClient.GameData.SpriteLoader.LoadSprite(NXAssetLoader.Instance.GetNxFile("ui")?.GetNode(path), "ui/" + path);
            if (sprite != null) sprite.name = "ui/" + path;
            return sprite;
        }
        public static RectTransform Rect(string name, Transform parent, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.sizeDelta = new Vector2(w, h); return r;
        }
        public static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }
        public static Image Art(string name, Transform parent, string path, float x, float y)
        {
            var sprite = Sprite(path); var r = Rect(name, parent, 0, 0);
            Place(r, x, y, sprite != null ? sprite.rect.width : 0, sprite != null ? sprite.rect.height : 0);
            var image = r.gameObject.AddComponent<Image>(); image.sprite = sprite;
            image.raycastTarget = false; image.enabled = sprite != null; return image;
        }
        public static Text Text(string name, Transform parent, Font font, string value, int size = 12, bool light = false)
        {
            var t = Rect(name, parent, 0, 0).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.text = value; t.color = light ? Color.white : new Color(.13f,.18f,.23f);
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; return t;
        }
        public static Button ArtButton(string name, Transform parent, string path, float x, float y, UnityAction click)
        {
            var art = Art(name, parent, path + "/normal/0", x, y); art.raycastTarget = true;
            var b = art.gameObject.AddComponent<Button>(); b.targetGraphic = art;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState { highlightedSprite = Sprite(path + "/mouseOver/0"),
                pressedSprite = Sprite(path + "/pressed/0"), disabledSprite = Sprite(path + "/disabled/0") };
            if (click != null) b.onClick.AddListener(click); return b;
        }
        public static Button HudButton(string name, Transform parent, string path, float x, UnityAction click)
        {
            var r = Rect(name, parent, 74, 34); Place(r, x, 36, 74, 34);
            string resource = "UI/ClassicHudButtons/" + path.Substring(path.LastIndexOf('/') + 1);
            var art = r.gameObject.AddComponent<Image>();
            art.sprite = Resources.Load<Sprite>(resource + "-normal");
            var hover = Resources.Load<Sprite>(resource + "-mouseOver");
            var b = r.gameObject.AddComponent<ClassicHudButton>(); b.targetGraphic = art; b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState { highlightedSprite = hover, selectedSprite = hover,
                pressedSprite = Resources.Load<Sprite>(resource + "-pressed"),
                disabledSprite = Resources.Load<Sprite>(resource + "-disabled") };
            b.onClick.AddListener(click); return b;
        }
        // Reuse a region of the original texture; no resampling or edited NX bitmap is needed.
        public static Image Region(string name, Transform parent, string path, Rect crop, float x, float y, float w, float h)
        {
            var art = Art(name, parent, path, x, y); var source = art.sprite;
            if (source != null)
            {
                var region = UnityEngine.Sprite.Create(source.texture,
                    new Rect(source.rect.x + crop.x, source.rect.yMax - crop.yMax, crop.width, crop.height), Vector2.zero, source.pixelsPerUnit);
                region.name = source.name + "/region";
                art.sprite = region; art.gameObject.AddComponent<ClassicSpriteOwner>().Value = region;
            }
            art.rectTransform.sizeDelta = new Vector2(w, h); return art;
        }
        public static Image GaugeInterior(string name, Transform parent, float x, float y, float width, Color tint)
        {
            var art = Region(name, parent, "StatusBar.img/gauge/hpFlash/0", new Rect(54,2,1,14), x,y,width,14);
            art.color = tint; return art;
        }
        public static Button Button(string name, Transform parent, Font font, string value, UnityAction click)
        {
            var r = Rect(name, parent, 0, 0); var i = r.gameObject.AddComponent<Image>();
            i.color = Color.clear;
            var left = Art("Left", r, "Basic.img/Tab2/left0", 0, 0);
            var middle = Art("Middle", r, "Basic.img/Tab2/fill0", 4, 0);
            middle.rectTransform.anchorMax = new Vector2(1,1); middle.rectTransform.sizeDelta = new Vector2(-8,19);
            var right = Art("Right", r, "Basic.img/Tab2/right0", 0, 0);
            right.rectTransform.anchorMin = right.rectTransform.anchorMax = right.rectTransform.pivot = Vector2.one;
            right.rectTransform.anchoredPosition = Vector2.zero;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = middle; b.onClick.AddListener(click);
            var t = Text("Label", r, font, value); t.alignment = TextAnchor.MiddleCenter;
            t.color=Color.white;
            var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.45f);shadow.effectDistance=new Vector2(0,-1);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero; return b;
        }
        public static void NativeCaption(Image image,Sprite sprite,float width,float height)
        {
            image.sprite=sprite;image.color=Color.white;image.enabled=sprite!=null;
            if(sprite!=null)Place(image.rectTransform,Mathf.Floor((width-sprite.rect.width)/2),Mathf.Floor((height-sprite.rect.height)/2),sprite.rect.width,sprite.rect.height);
        }
        public static void TabState(Transform tab, bool selected)
        {
            foreach (string part in new[] { "Left", "Middle", "Right" })
            {
                var image = tab.Find(part)?.GetComponent<Image>();
                if (image != null) image.sprite = Sprite("Basic.img/Tab2/" + (part == "Middle" ? "fill" : part.ToLowerInvariant()) + (selected ? "1" : "0"));
            }
        }
        public static RectTransform Window(string name, Transform parent, float width, float height)
        {
            var r = Rect(name, parent, width, height); r.gameObject.AddComponent<ClassicWindow>();
            if (parent.GetComponent<ClassicWindowManager>() == null) parent.gameObject.AddComponent<ClassicWindowManager>();
            return r;
        }
        public static void DragTitle(RectTransform parent, RectTransform window, float width)
        {
            var r = Rect("DragTitle", parent, width - 24, 20); Place(r, 0, 0, width - 24, 20);
            r.gameObject.AddComponent<Image>().color = Color.clear;
            r.gameObject.AddComponent<ClassicWindowDrag>().Window = window;
        }
        // These three original frame pieces are authored to repeat vertically. Keep their width native.
        public static RectTransform Companion(string name, RectTransform parent, float x, float y, float height)
        {
            var r = Rect(name, parent, 266, height); Place(r, x, y, 266, height);
            var middle = Art("FrameMiddle", r, "Basic.img/Notice3/c", 0, 21);
            middle.rectTransform.sizeDelta = new Vector2(266, height - 76); middle.raycastTarget = true;
            Art("FrameTop", r, "Basic.img/Notice3/t", 0, 0).raycastTarget = true;
            Art("FrameBottom", r, "Basic.img/Notice3/s", 0, height - 55).raycastTarget = true;
            DragTitle(r, parent, 266); return r;
        }
        public static RectTransform Hud(Transform canvas)
        {
            var existing = canvas.Find("ClassicHUD") as RectTransform;
            if (existing != null) return existing;
            var r = Rect("ClassicHUD", canvas, 800, 71);
            r.anchorMin = r.anchorMax = new Vector2(.5f, 0); r.pivot = new Vector2(.5f, 0);
            r.anchoredPosition = Vector2.zero;
            r.gameObject.AddComponent<ClassicHudLayout>();
            Art("Background", r, "StatusBar.img/base/backgrnd", 0, 0);
            Art("StatusContainer", r, "StatusBar.img/base/backgrnd2", 0, 0);
            Art("Gauges", r, "StatusBar.img/gauge/bar", 217, 38);
            var border = Art("HudFrame", r, "StatusBar.img/base/quickSlot", 0, 0);
            if (border.sprite != null)
            {
                var source = border.sprite;
                var frame = UnityEngine.Sprite.Create(source.texture, source.rect, Vector2.zero, source.pixelsPerUnit, 0,
                    SpriteMeshType.FullRect, new Vector4(6,6,6,6));
                border.sprite = frame; border.gameObject.AddComponent<ClassicSpriteOwner>().Value = frame;
                border.type = Image.Type.Sliced; border.fillCenter = false; border.rectTransform.sizeDelta = new Vector2(800,71);
            }
            return r;
        }
    }
    public sealed class ClassicSpriteOwner : MonoBehaviour
    {
        public Sprite Value;
        private void OnDestroy() { if (Value != null) Destroy(Value); }
    }
    /// <summary>Original bitmap digits, laid out at native size and rebuilt only when the value changes.</summary>
    public sealed class ClassicNumberLabel : MonoBehaviour
    {
        private Text label;
        private string folder, previous;
        private TextAnchor previousAlignment;
        private readonly List<GameObject> glyphs = new List<GameObject>();
        public void Bind(Text text, string path) { label = text; folder = path; label.enabled = false; }
        private void LateUpdate()
        {
            if (label == null || previous == label.text && previousAlignment == label.alignment) return;
            previous = label.text; previousAlignment = label.alignment;
            foreach (var glyph in glyphs) { glyph.SetActive(false); Destroy(glyph); } glyphs.Clear();
            var sprites = new List<Sprite>(); float width = 0;
            foreach (char c in previous)
            {
                string key = c == '/' ? "slash" : c == '[' ? "Lbracket" : c == ']' ? "Rbracket" : c.ToString();
                var sprite = ClassicUI.Sprite(folder + "/" + key);
                if (sprite == null) continue;
                sprites.Add(sprite); width += sprite.rect.width + 1;
            }
            width = Mathf.Max(0, width - 1); float x = (label.rectTransform.rect.width - width) / 2;
            if (label.alignment == TextAnchor.LowerRight || label.alignment == TextAnchor.MiddleRight) x *= 2;
            if (label.alignment == TextAnchor.LowerLeft || label.alignment == TextAnchor.MiddleLeft || label.alignment == TextAnchor.UpperLeft) x = 0;
            foreach (var sprite in sprites)
            {
                var r = ClassicUI.Rect("Digit", transform, sprite.rect.width, sprite.rect.height);
                ClassicUI.Place(r, Mathf.Round(x), Mathf.Round((label.rectTransform.rect.height - sprite.rect.height) / 2), sprite.rect.width, sprite.rect.height);
                var image = r.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
                glyphs.Add(r.gameObject); x += sprite.rect.width + 1;
            }
        }
    }
    public sealed class ClassicHudLayout : MonoBehaviour
    {
        private void LateUpdate()
        {
            var r = (RectTransform)transform;
            float availableWidth = ((RectTransform)r.parent).rect.width;
            r.localScale = Vector3.one * Mathf.Min(1, availableWidth / 800f);
            r.anchoredPosition = new Vector2(Mathf.Round((availableWidth-800*r.localScale.x)/2)-
                (availableWidth-800*r.localScale.x)/2,0);
            PositionRight("EquipmentToggle", -184, 5);
            PositionRight("InventoryToggle", -154, 5);
            PositionRight("StatsToggle", -124, 5);
            PositionRight("SkillsToggle", -94, 5);
            PositionRight("TradeToggle", -64, 5);
            PositionRight("QuickslotToggle", -34, 5);
            PositionRight("SkillBar", -151, -77);
        }
        private void PositionRight(string name, float x, float y)
        {
            var r = transform.Find(name) as RectTransform;
            if (r == null) return;
            r.anchorMin = r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y);
        }
    }
    public sealed class ClassicWindow : MonoBehaviour
    {
        public bool DockLeft;
        public bool DismissOnOutsideClick;
        public System.Action CloseAction;
        private Vector2 defaultCenter = new Vector2(.5f,.5f), lastSize;
        private bool placed, dragged;
        public void Configure(Vector2 center, System.Action close)
        { defaultCenter=center;CloseAction=close;placed=false; }
        public void Focus() { transform.SetAsLastSibling(); }
        public void Close() { if(CloseAction!=null)CloseAction();else gameObject.SetActive(false); }
        public void BeginDrag() { dragged=true;DockLeft=false;Focus(); }
        public void SetPosition(Vector2 position)
        { var r=(RectTransform)transform;r.anchoredPosition=position;placed=true;dragged=true;lastSize=((RectTransform)r.parent).rect.size; }
        public void SavePosition()
        {
            if(Application.isBatchMode)return;
            var r=(RectTransform)transform;var size=((RectTransform)r.parent).rect.size;
            PlayerPrefs.SetFloat("ClassicUI."+name+".x",r.anchoredPosition.x/size.x);
            PlayerPrefs.SetFloat("ClassicUI."+name+".y",r.anchoredPosition.y/size.y);
            PlayerPrefs.Save();
        }
        void LateUpdate()
        {
            var r = (RectTransform)transform; var parent = (RectTransform)r.parent;
            if(!placed && parent.rect.width>0)
            {
                r.anchoredPosition=Vector2.Scale(defaultCenter-new Vector2(.5f,.5f),parent.rect.size);
                if(!Application.isBatchMode && PlayerPrefs.HasKey("ClassicUI."+name+".x"))
                { r.anchoredPosition=Vector2.Scale(new Vector2(PlayerPrefs.GetFloat("ClassicUI."+name+".x"),PlayerPrefs.GetFloat("ClassicUI."+name+".y")),parent.rect.size);dragged=true; }
                placed=true;lastSize=parent.rect.size;
            }
            else if(lastSize!=parent.rect.size && lastSize.x>0 && lastSize.y>0)
            {
                r.anchoredPosition=dragged?Vector2.Scale(r.anchoredPosition,new Vector2(parent.rect.width/lastSize.x,parent.rect.height/lastSize.y)):
                    Vector2.Scale(defaultCenter-new Vector2(.5f,.5f),parent.rect.size);
                lastSize=parent.rect.size;
            }
            // Keep the HUD and its raised quickslot tray reachable, including on 640px displays.
            float bottomInset=Mathf.Max(82,151*Mathf.Min(1,parent.rect.width/800f)+8);
            r.localScale = Vector3.one * Mathf.Max(.1f, Mathf.Min(1, (parent.rect.width - 16) / r.rect.width, (parent.rect.height - bottomInset - 8) / r.rect.height));
            var half = r.rect.size * r.localScale.x / 2; var available = parent.rect.size / 2;
            if (DockLeft && !dragged) r.anchoredPosition = new Vector2(-available.x + half.x + 12, available.y - half.y - 12);
            r.anchoredPosition = new Vector2(Mathf.Clamp(r.anchoredPosition.x, -available.x + half.x + 8, available.x - half.x - 8),
                Mathf.Clamp(r.anchoredPosition.y, -available.y + half.y + bottomInset, available.y - half.y - 8));
        }
    }
    public sealed class ClassicWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Window;
        public void OnBeginDrag(PointerEventData e) { Window.GetComponent<ClassicWindow>()?.BeginDrag(); }
        public void OnDrag(PointerEventData e) { Window.anchoredPosition += e.delta / Window.GetComponentInParent<Canvas>().scaleFactor; }
        public void OnEndDrag(PointerEventData e) { Window.GetComponent<ClassicWindow>()?.SavePosition(); }
    }
}
