using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>The eight-cell tray is a view of physical keys in the world's complete keyboard map.</summary>
    public sealed partial class SkillBar : MonoBehaviour
    {
        private static readonly string[] Labels = { "Shift", "Ins", "Home", "PgUp", "Ctrl", "Del", "End", "PgDn" };
        private readonly List<SkillSlot> slots = new List<SkillSlot>();
        private readonly HashSet<QuickslotAction> queued = new HashSet<QuickslotAction>();
        private GameManager manager;
        private GameWorld World => manager?.World;
        public KeyboardMap Bindings => World?.Keyboard;
        private Font font;
        private RectTransform tray, dragIcon;
        private ClassicKeyboardView keyboard;
        private ClassicTooltipView tooltip;
        private ClassicQuickslotDrag dragSource;
        private QuickslotBinding dragged;
        private int dragRevision, renderedRevision = -1;
        private EventSystem inputEvents; private bool previousNavigation;
        internal int BindingRevision => Bindings?.Revision ?? 0;
        public static string KeyLabel(int index) => Labels[index];
        public static KeyCode Key(int index) => ClassicKeyboardLayout.Native(KeyboardMap.TrayKeys[index]);
        public QuickslotBinding BindingAt(int index) => index >= 0 && index < 8 ? BindingAtKey(KeyboardMap.TrayKeys[index]) : new QuickslotBinding();
        public QuickslotBinding BindingAtKey(KeyboardKey key) => Bindings?.Get(key) ?? new QuickslotBinding();

        private void Start()
        {
            manager = FindFirstObjectByType<GameManager>();
            inputEvents = EventSystem.current;
            if (inputEvents != null) { previousNavigation = inputEvents.sendNavigationEvents; inputEvents.sendNavigationEvents = false; }
            font = Font.CreateDynamicFontFromOSFont("Arial", 12); tooltip = ClassicTooltipView.For(transform);
            tray = ClassicUI.Rect("SkillBar", ClassicUI.Hud(transform), 151, 80);
            ClassicUI.Place(tray, 642, -80, 151, 80);
            ClassicUI.Art("QuickSlotBackground", tray, "StatusBar.img/base/quickSlot", 0, 0);
            for (int i = 0; i < 8; i++)
            {
                var rect = ClassicUI.Rect("SkillSlot_" + i, tray, 32, 32);
                ClassicUI.Place(rect, 7 + i % 4 * 35, 7 + i / 4 * 34, 32, 32);
                var slot = rect.gameObject.AddComponent<SkillSlot>(); slot.Initialize(this, i, font); slots.Add(slot);
            }
            // A previously restored/cleared keyboard must not receive fresh-scene defaults.
            if (Bindings?.Revision == 0)
            {
                AssignItemToSlot(2000000, 3); AssignItemToSlot(2000003, 7);
            }
            RefreshBindings();
            keyboard = ClassicUI.Window("QuickslotSettings", transform, 629, 373).gameObject.AddComponent<ClassicKeyboardView>();
            keyboard.Initialize(this, manager, font);
            if (World != null) World.MapLoaded += MapChanged;
        }
        public void ToggleVisible() { if (tray != null) { tray.gameObject.SetActive(!tray.gameObject.activeSelf); tooltip.Hide(); CancelDrag(); } }
        private void MapChanged(MapleClient.GameLogic.MapData map) { CancelDrag(); tooltip.Hide(); queued.Clear(); RefreshBindings(); }
        private void Update()
        {
            if (World == null) return;
            if (ClassicWindowManager.IsTyping) queued.Clear(); else HandleKeys(Input.GetKeyDown);
            RefreshBindings(); foreach (var slot in slots) slot.Refresh(World);
        }
        public void RefreshBindings()
        {
            if (World == null || renderedRevision == BindingRevision) return;
            renderedRevision = BindingRevision;
            foreach (var slot in slots) slot.SetBinding(BindingAt(slot.Index), World);
        }
        public void HandleKeys(Func<KeyCode, bool> pressed)
        {
            if (Bindings == null || ClassicWindowManager.IsTyping) return;
            // A physical key has one owner; duplicate aliases down in a frame dispatch once.
            foreach (var binding in Bindings.Pressed(k => pressed(ClassicKeyboardLayout.Native(k))).ToArray())
                if (binding.Kind != QuickslotKind.Action || !SampledByPlayer((QuickslotAction)binding.Id)) ActivateBinding(binding);
        }
        public bool ActionPressed(QuickslotAction action, Func<KeyCode, bool> pressed = null)
        {
            if (ClassicWindowManager.IsTyping) { queued.Clear(); return false; }
            bool clicked = queued.Remove(action);
            if (pressed == null) pressed = action == QuickslotAction.Attack ? (Func<KeyCode, bool>)Input.GetKeyDown : Input.GetKey;
            return clicked || Bindings?.IsPressed(action, k => pressed(ClassicKeyboardLayout.Native(k))) == true;
        }
        public MapleClient.GameLogic.Interfaces.CharacterExpression? ExpressionPressed(Func<KeyCode, bool> pressed = null)
        {
            if (ClassicWindowManager.IsTyping) { queued.Clear(); return null; }
            pressed = pressed ?? Input.GetKeyDown;
            for (int face = 0; face < 7; face++)
                if (ActionPressed((QuickslotAction)(100 + face), pressed))
                    return MapleClient.GameLogic.Data.CharacterExpressions.ForFunctionKey(face + 1);
            return null;
        }
        private static bool SampledByPlayer(QuickslotAction action) => action == QuickslotAction.Attack || action == QuickslotAction.Jump ||
            action >= QuickslotAction.Hit && action <= QuickslotAction.Stunned || action >= QuickslotAction.Left && action <= QuickslotAction.Down;
        public bool Activate(int index) => index >= 0 && index < 8 && ActivateBinding(BindingAt(index));
        private bool ActivateBinding(QuickslotBinding binding)
        {
            if (World == null || ClassicWindowManager.IsTyping) return false;
            switch (binding.Kind)
            {
                case QuickslotKind.Skill:
                    var result = manager.SkillManager.UseSkill(binding.Id);
                    if (!result.Success) Notify(result.ErrorMessage); return result.Success;
                case QuickslotKind.Item:
                    if (World.Player.Inventory.GetItemCount(binding.Id) == 0) { Notify("You have no " + Name(binding) + " left."); return false; }
                    bool used = World.UseInventoryItem(binding.Id, out var message); Notify(message); return used;
                case QuickslotKind.Action:
                    var action = (QuickslotAction)binding.Id;
                    if (SampledByPlayer(action)) { if (World.Player.IsDead) return false; queued.Add(action); return true; }
                    return ExecuteCommand(action);
                default: return false;
            }
        }
        private void Notify(string message) => GetComponent<PlayerCombatFeedback>()?.ShowActionMessage(message);
        public bool CanBind(QuickslotBinding binding)
        {
            if (binding == null || World == null) return false;
            switch (binding.Kind)
            {
                case QuickslotKind.Empty: return binding.Id == 0;
                case QuickslotKind.Item: return QuickslotBinding.SupportsItem(World.Player.GetItemInfo(binding.Id));
                case QuickslotKind.Action: return QuickslotBinding.IsAction(binding.Id);
                case QuickslotKind.Skill:
                    return manager.SkillManager.GetSkillLevel(binding.Id) > 0 &&
                        global::GameData.NXDataManagerSingleton.Instance.DataManager.SkillData.GetSkill(binding.Id)?.IsPassive == false;
                default: return false;
            }
        }
        public bool AssignKey(KeyboardKey key, QuickslotBinding binding)
        {
            if (!CanBind(binding) || !Bindings.Set(key, binding)) return false;
            queued.Clear(); tooltip.Hide(); RefreshBindings(); return true;
        }
        private bool SetBinding(int index, QuickslotBinding binding) => index >= 0 && index < 8 && AssignKey(KeyboardMap.TrayKeys[index], binding);
        public void AssignSkillToSlot(int id, int index) => SetBinding(index, new QuickslotBinding(id == 0 ? QuickslotKind.Empty : QuickslotKind.Skill, id));
        public bool AssignItemToSlot(int id, int index) => SetBinding(index, new QuickslotBinding(QuickslotKind.Item, id));
        public bool AssignActionToSlot(QuickslotAction action, int index) => SetBinding(index, new QuickslotBinding(QuickslotKind.Action, (int)action));
        public void ClearSlot(int index) => SetBinding(index, new QuickslotBinding());
        public void ResetKeyboard() { Bindings.ResetDefaults(true); queued.Clear(); CancelDrag(); tooltip.Hide(); RefreshBindings(); }
        public void ClearKeyboard() { Bindings.Clear(); queued.Clear(); CancelDrag(); tooltip.Hide(); RefreshBindings(); }
        public int[] CaptureSlots() => CaptureBindings().Select(b => b.Kind == QuickslotKind.Skill ? b.Id : 0).ToArray();
        public QuickslotBinding[] CaptureBindings() => KeyboardMap.TrayKeys.Select(BindingAtKey).ToArray();
        public void RestoreSlots(int[] skills)
        {
            for (int i = 0; i < 8; i++)
            {
                ClearSlot(i); AssignSkillToSlot(skills != null && i < skills.Length ? skills[i] : 0, i);
            }
            if (BindingAt(4).Kind == QuickslotKind.Empty) AssignActionToSlot(QuickslotAction.Attack, 4);
            CancelDrag(); queued.Clear();
        }
        public void RestoreBindings(QuickslotBinding[] bindings)
        {
            for (int i = 0; i < 8; i++) Bindings.Set(KeyboardMap.TrayKeys[i], bindings != null && i < bindings.Length ? bindings[i] : new QuickslotBinding());
            RefreshBindings(); CancelDrag(); tooltip.Hide(); queued.Clear();
        }
        public void LoadSkillsForCurrentJob()
        {
            int index = 0;
            foreach (var info in manager.SkillManager.GetAvailableSkills().Values.Where(s => !s.IsPassive && manager.SkillManager.GetSkillLevel(s.SkillId) > 0).Take(8))
                AssignSkillToSlot(info.SkillId, index++);
        }
        internal string Name(QuickslotBinding binding)
        {
            if (binding.Kind == QuickslotKind.Item) return World.Player.GetItemInfo(binding.Id)?.Name ?? "Item";
            if (binding.Kind == QuickslotKind.Skill) return global::GameData.NXDataManagerSingleton.Instance.DataManager.SkillData.GetSkill(binding.Id)?.Name ?? "Skill";
            return binding.Kind == QuickslotKind.Action ? CommandName((QuickslotAction)binding.Id) : "Unassigned";
        }
        public string ShortcutHint(QuickslotAction action) => string.Join(" / ", KeyboardMap.Keys.Where(k =>
            BindingAtKey(k).Kind == QuickslotKind.Action && BindingAtKey(k).Id == (int)action).Select(ClassicKeyboardLayout.Label));
        internal void Hover(SkillSlot slot, Vector2 screen)
        {
            bool alias = Bindings.IsNumberAlias((KeyboardKey)((int)KeyboardKey.Alpha1 + slot.Index));
            string body = Labels[slot.Index] + (alias ? " or " + (slot.Index + 1) : "") + " · Right-click to change.\nDrag a shortcut, item or learned skill here.";
            if (slot.Binding.Kind == QuickslotKind.Item) body = World.Player.Inventory.GetItemCount(slot.Binding.Id) + " remaining.\n" + body;
            if (slot.Binding.Kind == QuickslotKind.Skill) body = "Level " + manager.SkillManager.GetSkillLevel(slot.SkillId) + "\n" + body;
            tooltip.ShowText(slot, screen, Name(slot.Binding), body, slot.Icon.sprite);
        }
        internal void HideHover(SkillSlot slot) => tooltip.Hide(slot);
        internal bool BeginDrag(ClassicQuickslotDrag source, Vector2 screen)
        {
            var binding = source.GetBinding();
            if (!CanBind(binding) || binding.Kind == QuickslotKind.Empty) return false;
            CancelDrag(); tooltip.Hide(); dragged = binding; dragSource = source; dragRevision = BindingRevision;
            dragIcon = ClassicUI.Rect("DraggedQuickslot", transform, 32, 32);
            var icon = dragIcon.gameObject.AddComponent<Image>(); icon.sprite = SkillSlot.SpriteFor(binding); icon.preserveAspect = true; icon.raycastTarget = false;
            if (icon.sprite == null) icon.color = new Color32(176, 128, 148, 255);
            var caption = ClassicWindowSkin.Label("ActionCaption", dragIcon, font, SkillSlot.CaptionFor(binding), 0, 12, 32, 18, 8, Color.white, true); caption.alignment = TextAnchor.MiddleCenter;
            Drag(screen); return true;
        }
        internal void Drag(Vector2 screen) { if (dragIcon != null) dragIcon.anchoredPosition = ClassicSpriteFrames.CanvasPoint((RectTransform)transform, GetComponent<Canvas>(), screen); }
        internal void CancelDrag(ClassicQuickslotDrag source = null)
        {
            if (source != null && source != dragSource) return;
            dragSource = null; dragged = null;
            if (dragIcon != null) { dragIcon.gameObject.SetActive(false); Destroy(dragIcon.gameObject); dragIcon = null; }
        }
        internal void Drop(int index, GameObject source) { if (index >= 0 && index < 8) DropKey(KeyboardMap.TrayKeys[index], source); }
        internal void DropKey(KeyboardKey key, GameObject source)
        {
            if (source == null || !KeyboardMap.IsKey(key)) return;
            var bag = source.GetComponent<InventorySlotInteraction>();
            if (bag != null)
            {
                if (bag.TryGetDraggedItem(out int id) && !AssignKey(key, new QuickslotBinding(QuickslotKind.Item, id))) Notify("Only supported potions and consumables can use shortcuts.");
                return;
            }
            var origin = source.GetComponent<ClassicQuickslotDrag>();
            if (origin == null || origin != dragSource || dragRevision != BindingRevision || dragged == null) return;
            var previous = origin.SourceKey;
            if (previous != KeyboardKey.None)
            {
                if (Bindings.Swap(previous, key, dragRevision)) { queued.Clear(); tooltip.Hide(); RefreshBindings(); }
            }
            else AssignKey(key, dragged);
            CancelDrag();
        }
        public void ShowShortcuts(int index = -1) => keyboard?.Show(index);
        private void OnDestroy() { if (inputEvents != null) inputEvents.sendNavigationEvents = previousNavigation; if (World != null) World.MapLoaded -= MapChanged; CancelDrag(); if (font != null) Destroy(font); }
    }

    public sealed class SkillSlot : MonoBehaviour, IDropHandler
    {
        private SkillBar owner;
        private Image cooldown;
        private Text level, caption;
        public int Index { get; private set; }
        public QuickslotBinding Binding { get; private set; } = new QuickslotBinding();
        public int SkillId => Binding.Kind == QuickslotKind.Skill ? Binding.Id : 0;
        public KeyCode Hotkey => SkillBar.Key(Index);
        public Image Icon { get; private set; }
        public void Initialize(SkillBar bar, int index, Font font)
        {
            owner = bar; Index = index;
            var background = gameObject.AddComponent<Image>(); background.color = Color.clear;
            var button = gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.onClick.AddListener(() => owner.Activate(Index));
            Icon = ClassicWindowSkin.Fill("Icon", transform, 0, 0, 32, 32, Color.clear); Icon.preserveAspect = true;
            caption = ClassicWindowSkin.Label("ActionCaption", transform, font, "", 0, 13, 32, 17, 8, ClassicWindowSkin.Ink, true); caption.alignment = TextAnchor.MiddleCenter;
            cooldown = ClassicUI.Region("Cooldown", transform, "StatusBar.img/gauge/hpFlash/0", new Rect(54,2,1,1), 2,2,28,28);
            cooldown.color = new Color(0,0,0,.7f); cooldown.type = Image.Type.Filled; cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top; cooldown.fillClockwise = false; cooldown.enabled = false;
            ClassicUI.Art("Hotkey", transform, "StatusBar.img/key/" + index, 1, 0);
            level = ClassicWindowSkin.Label("Level", transform, font, "", 0, 21, 32, 11, 9, Color.white);
            level.gameObject.AddComponent<ClassicNumberLabel>().Bind(level, "Basic.img/ItemNo");
            var hover = gameObject.AddComponent<ClassicHover>(); hover.Enter = e => owner.Hover(this, e.position); hover.Exit = () => owner.HideHover(this);
            gameObject.AddComponent<ClassicItemClick>().Right = e => owner.ShowShortcuts(Index);
            var drag = gameObject.AddComponent<ClassicQuickslotDrag>(); drag.Owner = owner; drag.Slot = index;
        }
        internal static Sprite SpriteFor(QuickslotBinding binding)
        {
            if (binding.Kind == QuickslotKind.Item) return NXAssetLoader.Instance.LoadItemIcon(binding.Id);
            if (binding.Kind == QuickslotKind.Action) return binding.Id < 200 ? ClassicUI.Sprite("UIWindow.img/KeyConfig/icon/" + binding.Id) : null;
            if (binding.Kind != QuickslotKind.Skill) return null;
            var info = global::GameData.NXDataManagerSingleton.Instance.DataManager.SkillData.GetSkill(binding.Id);
            return MapleClient.GameData.SkillSprites.Icon(info);
        }
        internal static string CaptionFor(QuickslotBinding binding) => binding.Kind == QuickslotKind.Action && binding.Id >= 200 ?
            ((QuickslotAction)binding.Id == QuickslotAction.Screenshot ? "SNAP" : ((QuickslotAction)binding.Id).ToString().ToUpperInvariant()) : "";
        internal void SetBinding(QuickslotBinding binding, GameWorld world) { Binding = binding; Icon.sprite = SpriteFor(binding); caption.text = CaptionFor(binding); Refresh(world); }
        internal void Refresh(GameWorld world)
        {
            Icon.color = Icon.sprite == null ? Color.clear : Color.white;
            cooldown.enabled = false; string value = "";
            if (Binding.Kind == QuickslotKind.Item)
            {
                int count = world.Player.Inventory.GetItemCount(Binding.Id); value = count.ToString();
                if (count == 0) Icon.color = new Color(.55f,.55f,.55f,.8f);
                level.alignment = TextAnchor.LowerLeft;
            }
            else if (Binding.Kind == QuickslotKind.Skill)
            {
                var skill = world.SkillManager.LearnedSkills.FirstOrDefault(s => s.SkillId == Binding.Id);
                value = skill != null && skill.CurrentLevel > 0 ? skill.CurrentLevel.ToString() : "";
                level.alignment = TextAnchor.LowerRight;
                float total = (skill?.GetCurrentLevelData()?.Cooldown ?? 0) / 1000f;
                cooldown.enabled = skill?.IsOnCooldown == true && total > 0;
                cooldown.fillAmount = total > 0 ? Mathf.Clamp01(skill.CooldownRemaining / total) : 0;
            }
            if (level.text != value) level.text = value;
        }
        public void OnDrop(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) owner.Drop(Index, e.pointerDrag); }
        private void OnDisable() { if (owner != null) owner.HideHover(this); }
    }

    public sealed class ClassicQuickslotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public SkillBar Owner;
        public int Slot = -1, SkillId;
        public KeyboardKey Key = KeyboardKey.None;
        public KeyboardKey SourceKey => Key != KeyboardKey.None ? Key : Slot >= 0 ? KeyboardMap.TrayKeys[Slot] : KeyboardKey.None;
        // Palette entries copy an action, consumable or learned skill. Tray/key entries swap slots.
        public QuickslotBinding PaletteBinding;
        public QuickslotBinding GetBinding() => SourceKey != KeyboardKey.None ? Owner.BindingAtKey(SourceKey) :
            PaletteBinding?.Copy() ?? new QuickslotBinding(QuickslotKind.Skill, SkillId);
        public void OnBeginDrag(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left && Owner != null && Owner.BeginDrag(this, e.position)) e.eligibleForClick = false; }
        public void OnDrag(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) Owner?.Drag(e.position); }
        public void OnEndDrag(PointerEventData e) => Owner?.CancelDrag(this);
        private void OnDisable() => Owner?.CancelDrag(this);
    }
}
