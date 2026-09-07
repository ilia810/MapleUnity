using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>Every physical key edits the same persistent map used by player and UI input.</summary>
    public sealed class ClassicKeyboardView : MonoBehaviour
    {
        private const string Art = "UIWindow.img/KeyConfig/";
        private const int PageSize = 54;
        private readonly List<ClassicKeyboardKey> keys = new List<ClassicKeyboardKey>();
        private readonly List<QuickslotBinding> palette = new List<QuickslotBinding>();
        private readonly List<GameObject> entries = new List<GameObject>();
        private SkillBar owner;
        private GameManager manager;
        private Font font;
        private ClassicTooltipView tooltip;
        private Text status, pageLabel;
        private Button clear, previous, next;
        private int page, bindingRevision = -1, skillRevision = -1;
        private KeyboardKey selected = KeyboardKey.LeftShift;
        private Sprite keyFrame, keyBlank;
        private long inventoryRevision = -1;

        public void Initialize(SkillBar bar, GameManager game, Font textFont)
        {
            owner = bar; manager = game; font = textFont; tooltip = ClassicTooltipView.For(bar.transform);
            var root = (RectTransform)transform;
            GetComponent<ClassicWindow>().Configure(new Vector2(.5f, .62f), Close);
            ClassicUI.Art("KeyboardBackground", root, Art + "backgrnd", 0, 0).raycastTarget = true;
            ClassicUI.DragTitle(root, root, 629);
            var help = ClassicUI.ArtButton("KeyboardHelp", root, Art + "BtHelp", 590, 6, () => Help(ScreenPoint(transform)));
            Hover(help.gameObject, () => "Keyboard setting", () => HelpText);
            ClassicUI.ArtButton("CloseShortcuts", root, Art + "BtClose", 609, 6, Close);

            keyFrame = Crop(new Rect(319, 66, 32, 32), new Vector4(3, 3, 3, 3));
            keyBlank = Crop(new Rect(342, 68, 1, 12), Vector4.zero);
            foreach (var position in ClassicKeyboardLayout.Keys) AddKey(position);

            ClassicUI.ArtButton("DefaultShortcuts", root, Art + "BtDefault", 8, 239, Defaults);
            ClassicUI.ArtButton("ClearAllShortcuts", root, Art + "BtDelete", 74, 239, () =>
            { owner.ClearKeyboard(); Refresh(); });
            clear = ClassicWindowSkin.Action("ClearQuickslot", root, font, "CLEAR KEY", 157, 239, 70, () =>
            { owner.AssignKey(selected, new QuickslotBinding()); Refresh(); }, new Color32(177, 194, 70, 255));
            status = ClassicWindowSkin.Label("SelectedKeyboardKey", root, font, "", 235, 241, 262, 14, 10);
            previous = ClassicWindowSkin.ScrollArrow("KeyboardPreviousPage", root, 500, 239, true, () => ChangePage(-1));
            next = ClassicWindowSkin.ScrollArrow("KeyboardNextPage", root, 549, 239, false, () => ChangePage(1));
            pageLabel = ClassicWindowSkin.Label("KeyboardPage", root, font, "", 514, 241, 33, 14, 10);
            pageLabel.alignment = TextAnchor.UpperCenter;
            ClassicUI.ArtButton("ApplyShortcuts", root, Art + "BtOK", 569, 239, Close);
            var wheel = ClassicWindowSkin.Fill("KeyboardPalette", root, 7, 265, 611, 103, Color.clear, true);
            wheel.gameObject.AddComponent<ClassicScrollWheel>().Scroll = ChangePage;
            gameObject.SetActive(false);
        }

        private const string HelpText = "Drag any shortcut to another key. Occupied keys swap assignments.\n" +
            "Select any key, then click an action below; or drag in a bag consumable or learned skill.\n" +
            "1–8 follow the tray until individually changed. Right-click a key to clear it.\n" +
            "Changes apply immediately and are included in your local save. Defaults restores every control.\n" +
            "Use the arrows or mouse wheel for more palette pages.";
        private Sprite Crop(Rect crop, Vector4 border)
        {
            var source = ClassicUI.Sprite(Art + "backgrnd");
            if (source == null) return null;
            var sprite = Sprite.Create(source.texture, new Rect(source.rect.x + crop.x, source.rect.yMax - crop.yMax, crop.width, crop.height),
                Vector2.zero, source.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = source.name + "/keycap";
            gameObject.AddComponent<ClassicSpriteOwner>().Value = sprite; return sprite;
        }
        private static Vector2 ScreenPoint(Transform target)
        {
            var canvas = target.GetComponentInParent<Canvas>();
            return RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, target.position);
        }
        private void Help(Vector2 screen) => tooltip.ShowText(this, screen, "Keyboard setting", HelpText);
        private void Hover(GameObject target, Func<string> title, Func<string> body, Func<Sprite> sprite = null)
        {
            var hover = target.AddComponent<ClassicHover>();
            hover.Enter = e => tooltip.ShowText(target, e.position, title(), body(), sprite?.Invoke());
            hover.Exit = () => tooltip.Hide(target);
        }
        public void Show(int index = -1)
        {
            if (index >= 0) selected = KeyboardMap.TrayKeys[Mathf.Clamp(index, 0, 7)];
            gameObject.SetActive(index >= 0 || !gameObject.activeSelf);
            if (!gameObject.activeSelf) return;
            GetComponent<ClassicWindow>().Focus(); Refresh();
        }
        private void Close() => gameObject.SetActive(false);
        private void OnDisable()
        {
            tooltip?.Hide();
            // Closing this independent window ends drags that originated in its keys or palette.
            foreach (var drag in GetComponentsInChildren<ClassicQuickslotDrag>(true)) owner?.CancelDrag(drag);
        }
        private void Update() => Refresh();
        internal void Select(KeyboardKey key) { selected = key; tooltip.Hide(); Refresh(); }
        internal void Clear(KeyboardKey key) { Select(key); owner.AssignKey(key, new QuickslotBinding()); Refresh(); }
        internal void Drop(KeyboardKey key, GameObject source) { Select(key); owner.DropKey(key, source); Refresh(); }
        private void Defaults() { owner.ResetKeyboard(); Select(KeyboardKey.LeftControl); }
        private void AddKey(ClassicKeyPosition position)
        {
            var bounds = position.Rect; var physical = position.Key;
            var image = ClassicWindowSkin.Fill(ClassicKeyboardLayout.ObjectName(physical), transform, bounds.x, bounds.y, bounds.width, bounds.height, Color.white, true);
            image.sprite = keyFrame; image.type = Image.Type.Sliced;
            // Recompose the native blank key so baked Menu / Move Menu / Screenshot labels also move.
            var blank = ClassicWindowSkin.Fill("BlankKeyLabel", image.transform, 2, 2, bounds.width - 4, 12, Color.white);
            blank.sprite = keyBlank;
            var key = image.gameObject.AddComponent<ClassicKeyboardKey>(); key.Initialize(this, owner, physical, font, bounds.width, bounds.height);
            keys.Add(key);
            Hover(image.gameObject, () => ClassicKeyboardLayout.Label(physical) + " — " + owner.Name(owner.BindingAtKey(physical)),
                () => "Drag to another key to move or swap. Select an action below, or drag an item or learned skill here.\nRight-click to clear." +
                    (owner.Bindings.IsNumberAlias(physical) ? " This number key follows its tray key until changed." : ""),
                () => SkillSlot.SpriteFor(owner.BindingAtKey(physical)));
        }
        private void Refresh()
        {
            if (owner == null || manager.World == null) return;
            int currentSkills = 17;
            foreach (var skill in manager.SkillManager.LearnedSkills)
                unchecked { currentSkills = currentSkills * 31 + skill.SkillId * 31 + skill.CurrentLevel; }
            if (inventoryRevision != manager.World.Player.Inventory.Revision || bindingRevision != owner.BindingRevision || skillRevision != currentSkills)
            {
                inventoryRevision = manager.World.Player.Inventory.Revision; bindingRevision = owner.BindingRevision; skillRevision = currentSkills;
                RebuildPalette();
            }
            foreach (var key in keys) key.Refresh(key.Key == selected);
            clear.interactable = true;
            string value = ClassicKeyboardLayout.Label(selected) + ": " + owner.Name(owner.BindingAtKey(selected));
            status.text = value;
            while (status.preferredWidth > status.rectTransform.rect.width && value.Length > 3) { value = value.Substring(0, value.Length - 1); status.text = value + "…"; }
        }
        private void RebuildPalette()
        {
            var updated = ((QuickslotAction[])Enum.GetValues(typeof(QuickslotAction))).Select(action => new QuickslotBinding(QuickslotKind.Action, (int)action)).ToList();
            updated.AddRange(manager.SkillManager.LearnedSkills.Where(s => s.CurrentLevel > 0).OrderBy(s => s.SkillId)
                .Select(s => new QuickslotBinding(QuickslotKind.Skill, s.SkillId)).Where(owner.CanBind));
            var items = manager.World.Player.Inventory.GetStacks().Select(s => s.ItemId)
                .Concat(owner.Bindings.Capture().Where(e => e.Binding.Kind == QuickslotKind.Item).Select(e => e.Binding.Id)).Distinct().OrderBy(id => id);
            updated.AddRange(items.Select(id => new QuickslotBinding(QuickslotKind.Item, id)).Where(owner.CanBind));
            // Do not destroy a drag source merely because its destination changes another binding.
            if (palette.Count == updated.Count && palette.Zip(updated, (a, b) => a.Kind == b.Kind && a.Id == b.Id).All(equal => equal)) return;
            palette.Clear(); palette.AddRange(updated); page = Mathf.Clamp(page, 0, (palette.Count - 1) / PageSize); DrawPalette();
        }
        private void ChangePage(int delta)
        {
            int target = Mathf.Clamp(page + delta, 0, (palette.Count - 1) / PageSize);
            if (target == page) return; page = target; DrawPalette();
        }
        private void DrawPalette()
        {
            tooltip.Hide();
            foreach (var entry in entries) { entry.SetActive(false); Destroy(entry); } entries.Clear();
            for (int cell = 0; cell < PageSize && page * PageSize + cell < palette.Count; cell++)
            {
                var binding = palette[page * PageSize + cell];
                string name = binding.Kind == QuickslotKind.Action ? "Bind" + (QuickslotAction)binding.Id : "KeyboardPalette_" + binding.Kind + "_" + binding.Id;
                var image = ClassicWindowSkin.Fill(name, transform, 8 + cell % 18 * 34, 267 + cell / 18 * 34, 32, 32, Color.clear, true);
                var icon = ClassicWindowSkin.Fill("Icon", image.transform, 0, 0, 32, 32, Color.white);
                icon.sprite = SkillSlot.SpriteFor(binding); icon.preserveAspect = true; icon.enabled = icon.sprite != null;
                var caption = ClassicWindowSkin.Label("ActionCaption", image.transform, font, SkillSlot.CaptionFor(binding), 0, 10, 32, 20, 8, ClassicWindowSkin.Ink, true);
                caption.alignment = TextAnchor.MiddleCenter;
                var button = image.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Assign(binding));
                var drag = image.gameObject.AddComponent<ClassicQuickslotDrag>(); drag.Owner = owner; drag.PaletteBinding = binding.Copy();
                image.gameObject.AddComponent<ClassicScrollWheel>().Scroll = ChangePage;
                Hover(image.gameObject, () => owner.Name(binding), () => "Drag onto any key or the tray, or click to assign to the selected key. Changes apply immediately.", () => icon.sprite);
                entries.Add(image.gameObject);
            }
            previous.interactable = page > 0; next.interactable = (page + 1) * PageSize < palette.Count;
            pageLabel.text = (page + 1) + "/" + ((palette.Count + PageSize - 1) / PageSize);
        }
        private void Assign(QuickslotBinding binding) { owner.AssignKey(selected, binding); Refresh(); }
    }

    public sealed class ClassicKeyboardKey : MonoBehaviour, IDropHandler
    {
        private ClassicKeyboardView view;
        private SkillBar owner;
        private Image icon;
        private Text caption;
        private GameObject selection;
        private QuickslotKind kind = (QuickslotKind)(-1);
        private int id;
        public KeyboardKey Key { get; private set; }
        public void Initialize(ClassicKeyboardView keyboard, SkillBar bar, KeyboardKey physical, Font font, float width, float height)
        {
            view = keyboard; owner = bar; Key = physical;
            var button = gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => view.Select(Key));
            icon = ClassicWindowSkin.Fill("BindingIcon", transform, Mathf.Floor((width - 32) / 2), 0, 32, 32, Color.white); icon.preserveAspect = true;
            caption = ClassicWindowSkin.Label("ActionCaption", transform, font, "", 0, 13, width, 17, 8, ClassicWindowSkin.Ink, true); caption.alignment = TextAnchor.MiddleCenter;
            var label = ClassicUI.Art("KeyLabel", transform, "UIWindow.img/KeyConfig/key/" + ClassicKeyboardLayout.Glyph(Key), 2, 2);
            if (label.sprite == null)
            {
                label.enabled = false;
                var fallback = ClassicWindowSkin.Label("KeyLabelFallback", transform, font, ClassicKeyboardLayout.Label(Key), 2, 0, width - 3, 13, 9, Color.white, true);
                fallback.gameObject.AddComponent<Outline>().effectColor = new Color32(135, 154, 172, 255);
            }
            var rect = ClassicUI.Rect("Selection", transform, width, height); ClassicUI.Place(rect, 0, 0, width, height); selection = rect.gameObject;
            Color gold = new Color32(231, 166, 33, 255);
            ClassicWindowSkin.Fill("Top", rect, 0, 0, width, 1, gold); ClassicWindowSkin.Fill("Bottom", rect, 0, height - 1, width, 1, gold);
            ClassicWindowSkin.Fill("Left", rect, 0, 0, 1, height, gold); ClassicWindowSkin.Fill("Right", rect, width - 1, 0, 1, height, gold);
            gameObject.AddComponent<ClassicItemClick>().Right = e => view.Clear(Key);
            var drag = gameObject.AddComponent<ClassicQuickslotDrag>(); drag.Owner = owner; drag.Key = Key;
        }
        internal void Refresh(bool selected)
        {
            var binding = owner.BindingAtKey(Key);
            if (binding.Kind != kind || binding.Id != id) { kind = binding.Kind; id = binding.Id; icon.sprite = SkillSlot.SpriteFor(binding); icon.enabled = icon.sprite != null; caption.text = SkillSlot.CaptionFor(binding); }
            selection.SetActive(selected);
        }
        public void OnDrop(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) view.Drop(Key, e.pointerDrag); }
    }
}
