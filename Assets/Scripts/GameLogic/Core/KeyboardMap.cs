using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameLogic.Core
{
    // Stable save IDs follow KeyConfig.h where available; additional physical keys fill unused IDs.
    public enum KeyboardKey
    {
        None = 0, Alpha1 = 2, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9, Alpha0, Minus, Equals,
        Backspace = 14, Tab = 15, Q = 16, W, E, R, T, Y, U, I, O, P, LeftBracket, RightBracket, Return,
        LeftControl = 29, A, S, D, F, G, H, J, K, L, Semicolon, Quote, BackQuote, LeftShift, Backslash,
        Z, X, C, V, B, N, M, Comma, Period, Slash,
        LeftAlt = 56, Space, CapsLock, F1 = 59, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        Home = 71, PageUp = 73, End = 79, PageDown = 81, Insert, Delete, Escape, RightControl, RightShift, RightAlt, ScrollLock,
        UpArrow = 89, DownArrow, LeftArrow, RightArrow, Print, Pause, LeftWindows, RightWindows
    }

    [Serializable] public sealed class KeyboardEntry
    {
        public KeyboardKey Key;
        public QuickslotBinding Binding;
        public KeyboardEntry() { }
        public KeyboardEntry(KeyboardKey key, QuickslotBinding binding) { Key = key; Binding = binding.Copy(); }
    }

    /// <summary>One physical key owns one assignment. Legacy number aliases follow the tray until
    /// edited; moving an alias materializes its current action, so swaps cannot create cycles.</summary>
    public sealed class KeyboardMap
    {
        public static readonly IReadOnlyList<KeyboardKey> Keys = Array.AsReadOnly(((KeyboardKey[])Enum.GetValues(typeof(KeyboardKey))).Where(k => k != KeyboardKey.None).ToArray());
        public static readonly IReadOnlyList<KeyboardKey> TrayKeys = Array.AsReadOnly(new[] {
            KeyboardKey.LeftShift, KeyboardKey.Insert, KeyboardKey.Home, KeyboardKey.PageUp,
            KeyboardKey.LeftControl, KeyboardKey.Delete, KeyboardKey.End, KeyboardKey.PageDown });
        private readonly Dictionary<KeyboardKey, QuickslotBinding> bindings = new Dictionary<KeyboardKey, QuickslotBinding>();
        public int Revision { get; private set; }
        public KeyboardMap() { ResetDefaults(); Revision = 0; }
        public static bool IsKey(KeyboardKey key) => key != KeyboardKey.None && Enum.IsDefined(typeof(KeyboardKey), key);
        public QuickslotBinding Raw(KeyboardKey key) => bindings.TryGetValue(key, out var binding) ? binding.Copy() : new QuickslotBinding();
        private QuickslotBinding Resolve(KeyboardKey key)
        {
            if (!bindings.TryGetValue(key, out var binding)) return null;
            return binding.Kind == QuickslotKind.KeyReference ?
                (bindings.TryGetValue((KeyboardKey)binding.Id, out var target) ? target : null) : binding;
        }
        public QuickslotBinding Get(KeyboardKey key) => Resolve(key)?.Copy() ?? new QuickslotBinding();
        public bool IsNumberAlias(KeyboardKey key) => Raw(key).Kind == QuickslotKind.KeyReference;
        public static bool IsReference(KeyboardKey key, QuickslotBinding binding) =>
            key >= KeyboardKey.Alpha1 && key <= KeyboardKey.Alpha8 && binding != null &&
            binding.Kind == QuickslotKind.KeyReference && binding.Id == (int)TrayKeys[(int)key - (int)KeyboardKey.Alpha1];
        public static bool HasValidShape(KeyboardKey key, QuickslotBinding binding) => IsKey(key) && binding != null &&
            (binding.Kind == QuickslotKind.Empty ? binding.Id == 0 :
             binding.Kind == QuickslotKind.Action ? QuickslotBinding.IsAction(binding.Id) :
             binding.Kind == QuickslotKind.KeyReference ? IsReference(key, binding) :
             (binding.Kind == QuickslotKind.Item || binding.Kind == QuickslotKind.Skill) && binding.Id > 0);
        public bool Set(KeyboardKey key, QuickslotBinding binding)
        {
            if (!HasValidShape(key, binding)) return false;
            if (binding.Kind == QuickslotKind.Empty) bindings.Remove(key); else bindings[key] = binding.Copy();
            Revision++; return true;
        }
        public bool Swap(KeyboardKey source, KeyboardKey destination, int expectedRevision)
        {
            if (Revision != expectedRevision || !IsKey(source) || !IsKey(destination)) return false;
            if (source == destination) return true;
            var moved = Get(source); var replaced = Get(destination);
            if (moved.Kind == QuickslotKind.Empty) return false;
            // Resolve both sides before writing; moving a number alias onto its tray key is safe.
            Set(source, replaced); Set(destination, moved); return true;
        }
        public void Clear() { bindings.Clear(); Revision++; }
        public void ResetDefaults(bool consumables = false)
        {
            bindings.Clear();
            Default(QuickslotAction.Equipment, KeyboardKey.E); Default(QuickslotAction.Inventory, KeyboardKey.I);
            Default(QuickslotAction.Stats, KeyboardKey.C, KeyboardKey.P); Default(QuickslotAction.Skills, KeyboardKey.K);
            Default(QuickslotAction.WorldMap, KeyboardKey.M); Default(QuickslotAction.Minimap, KeyboardKey.Tab);
            Default(QuickslotAction.Quests, KeyboardKey.Q); Default(QuickslotAction.Menu, KeyboardKey.Escape);
            Default(QuickslotAction.Attack, KeyboardKey.LeftControl, KeyboardKey.Z);
            Default(QuickslotAction.Jump, KeyboardKey.LeftAlt, KeyboardKey.Space);
            Default(QuickslotAction.Left, KeyboardKey.A, KeyboardKey.LeftArrow); Default(QuickslotAction.Right, KeyboardKey.D, KeyboardKey.RightArrow);
            Default(QuickslotAction.Up, KeyboardKey.W, KeyboardKey.UpArrow); Default(QuickslotAction.Down, KeyboardKey.S, KeyboardKey.DownArrow);
            Default(QuickslotAction.Talk, KeyboardKey.V); Default(QuickslotAction.Shop, KeyboardKey.N);
            Default(QuickslotAction.Screenshot, KeyboardKey.ScrollLock);
            for (int i = 0; i < 7; i++) Default((QuickslotAction)(100 + i), (KeyboardKey)((int)KeyboardKey.F1 + i));
            for (int i = 0; i < 8; i++) bindings[(KeyboardKey)((int)KeyboardKey.Alpha1 + i)] = new QuickslotBinding(QuickslotKind.KeyReference, (int)TrayKeys[i]);
            if (consumables)
            {
                bindings[KeyboardKey.PageUp] = new QuickslotBinding(QuickslotKind.Item, 2000000);
                bindings[KeyboardKey.PageDown] = new QuickslotBinding(QuickslotKind.Item, 2000003);
            }
            Revision++;
        }
        private void Default(QuickslotAction action, params KeyboardKey[] keys)
        { foreach (var key in keys) bindings[key] = new QuickslotBinding(QuickslotKind.Action, (int)action); }
        public KeyboardEntry[] Capture() => bindings.OrderBy(p => p.Key).Select(p => new KeyboardEntry(p.Key, p.Value)).ToArray();
        public static bool ValidEntries(KeyboardEntry[] entries) => entries != null && entries.Length <= Keys.Count &&
            entries.All(e => e != null && HasValidShape(e.Key, e.Binding)) && entries.Select(e => e.Key).Distinct().Count() == entries.Length;
        internal void Restore(KeyboardEntry[] entries)
        {
            bindings.Clear(); foreach (var entry in entries) if (entry.Binding.Kind != QuickslotKind.Empty) bindings[entry.Key] = entry.Binding.Copy();
            Revision++;
        }
        public IEnumerable<QuickslotBinding> Pressed(Func<KeyboardKey, bool> pressed) => Keys.Where(pressed).Select(Get)
            .Where(b => b.Kind != QuickslotKind.Empty).GroupBy(b => (b.Kind, b.Id)).Select(g => g.First());
        public bool IsPressed(QuickslotAction action, Func<KeyboardKey, bool> pressed)
        {
            foreach (var key in Keys)
            {
                var binding = Resolve(key);
                if (binding != null && binding.Kind == QuickslotKind.Action && binding.Id == (int)action && pressed(key)) return true;
            }
            return false;
        }
    }

    public partial class GameWorld
    {
        public KeyboardMap Keyboard { get; } = new KeyboardMap();
        private bool CanRestoreKeyboard(LocalProgress save) => save.Version < 5 || KeyboardMap.ValidEntries(save.Keyboard) &&
            save.Keyboard.All(e => KeyboardMap.IsReference(e.Key, e.Binding) || CanRestoreBinding(e.Binding, save));
        private void RestoreKeyboard(LocalProgress save)
        {
            if (save.Version >= 5) { Keyboard.Restore(save.Keyboard); return; }
            Keyboard.ResetDefaults();
            for (int i = 0; i < 8; i++)
            {
                var binding = save.Version >= 4 ? (i < save.Quickslots.Length ? save.Quickslots[i] : new QuickslotBinding()) :
                    (i < save.Hotbar.Length && save.Hotbar[i] != 0 ? new QuickslotBinding(QuickslotKind.Skill, save.Hotbar[i]) : new QuickslotBinding());
                Keyboard.Set(KeyboardMap.TrayKeys[i], CanRestoreBinding(binding, save) ? binding : new QuickslotBinding());
            }
            if (save.Version < 4 && Keyboard.Get(KeyboardKey.LeftControl).Kind == QuickslotKind.Empty)
                Keyboard.Set(KeyboardKey.LeftControl, new QuickslotBinding(QuickslotKind.Action, (int)QuickslotAction.Attack));
        }
    }
}
