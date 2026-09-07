using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Core;
using UnityEngine;

namespace MapleClient.GameView.UI
{
    public sealed class ClassicKeyPosition
    {
        public readonly KeyboardKey Key;
        public readonly Rect Rect;
        public ClassicKeyPosition(KeyboardKey key, float x, float y, float width = 32)
        { Key = key; Rect = new Rect(x, y, width, 32); }
    }
    /// <summary>Physical keys measured against the original 629 x 373 NX keyboard.</summary>
    public static class ClassicKeyboardLayout
    {
        public static readonly IReadOnlyList<ClassicKeyPosition> Keys = Build();
        private static readonly Dictionary<KeyboardKey, KeyCode> native = BuildNative();
        public static KeyCode Native(KeyboardKey key) => native.TryGetValue(key, out var result) ? result : KeyCode.None;
        private static Dictionary<KeyboardKey, KeyCode> BuildNative()
        {
            var result = new Dictionary<KeyboardKey, KeyCode>();
            foreach (var key in KeyboardMap.Keys) result[key] = (KeyCode)Enum.Parse(typeof(KeyCode), key.ToString());
            return result;
        }
        public static int Glyph(KeyboardKey key)
        {
            if (key == KeyboardKey.RightControl) return 29;
            if (key == KeyboardKey.RightShift) return 42;
            if (key == KeyboardKey.RightAlt) return 56;
            return (int)key < 84 ? (int)key : 0;
        }
        public static string Label(KeyboardKey key)
        {
            if (key >= KeyboardKey.Alpha1 && key <= KeyboardKey.Alpha8) return ((int)key - 1).ToString();
            switch (key)
            {
                case KeyboardKey.Alpha9: return "9"; case KeyboardKey.Alpha0: return "0";
                case KeyboardKey.LeftShift: return "Shift"; case KeyboardKey.RightShift: return "R Shift";
                case KeyboardKey.LeftControl: return "Ctrl"; case KeyboardKey.RightControl: return "R Ctrl";
                case KeyboardKey.LeftAlt: return "Alt"; case KeyboardKey.RightAlt: return "R Alt";
                case KeyboardKey.LeftWindows: return "L Win"; case KeyboardKey.RightWindows: return "R Win";
                case KeyboardKey.PageUp: return "PgUp"; case KeyboardKey.PageDown: return "PgDn";
                case KeyboardKey.Insert: return "Ins"; case KeyboardKey.Delete: return "Del";
                case KeyboardKey.Escape: return "Esc"; case KeyboardKey.Return: return "Enter";
                case KeyboardKey.Backspace: return "Bksp"; case KeyboardKey.CapsLock: return "Caps";
                case KeyboardKey.ScrollLock: return "ScrLk"; case KeyboardKey.Print: return "PrtSc"; case KeyboardKey.Pause: return "Break";
                case KeyboardKey.UpArrow: return "↑"; case KeyboardKey.DownArrow: return "↓";
                case KeyboardKey.LeftArrow: return "←"; case KeyboardKey.RightArrow: return "→";
                case KeyboardKey.Minus: return "-"; case KeyboardKey.Equals: return "=";
                case KeyboardKey.LeftBracket: return "["; case KeyboardKey.RightBracket: return "]";
                case KeyboardKey.Semicolon: return ";"; case KeyboardKey.Quote: return "'";
                case KeyboardKey.BackQuote: return "`"; case KeyboardKey.Backslash: return "\\";
                case KeyboardKey.Comma: return ","; case KeyboardKey.Period: return "."; case KeyboardKey.Slash: return "/";
                default: return key.ToString();
            }
        }
        public static string ObjectName(KeyboardKey key)
        {
            for (int i = 0; i < 8; i++) if (KeyboardMap.TrayKeys[i] == key) return "ShortcutBinding_" + i;
            if (key >= KeyboardKey.Alpha1 && key <= KeyboardKey.Alpha8) return "KeyboardAlias_" + ((int)key - 2);
            return "KeyboardKey_" + key;
        }
        private static IReadOnlyList<ClassicKeyPosition> Build()
        {
            var keys = new List<ClassicKeyPosition> { new ClassicKeyPosition(KeyboardKey.Escape, 13, 28) };
            float[] functions = { 81, 115, 149, 183, 225, 259, 293, 327, 369, 403, 437, 471 };
            for (int i = 0; i < 12; i++) keys.Add(new ClassicKeyPosition(KeyboardKey.F1 + i, functions[i], 28));
            keys.Add(new ClassicKeyPosition(KeyboardKey.Print, 513, 28)); keys.Add(new ClassicKeyPosition(KeyboardKey.ScrollLock, 547, 28)); keys.Add(new ClassicKeyPosition(KeyboardKey.Pause, 581, 28));
            keys.Add(new ClassicKeyPosition(KeyboardKey.BackQuote, 13, 66));
            for (int i = 0; i < 12; i++) keys.Add(new ClassicKeyPosition(KeyboardKey.Alpha1 + i, 47 + i * 34, 66));
            keys.Add(new ClassicKeyPosition(KeyboardKey.Backspace, 455, 66, 48));
            keys.Add(new ClassicKeyPosition(KeyboardKey.Tab, 13, 99, 48));
            for (int i = 0; i < 12; i++) keys.Add(new ClassicKeyPosition(KeyboardKey.Q + i, 63 + i * 34, 99));
            keys.Add(new ClassicKeyPosition(KeyboardKey.Backslash, 471, 99));
            keys.Add(new ClassicKeyPosition(KeyboardKey.CapsLock, 13, 132, 66));
            for (int i = 0; i < 11; i++) keys.Add(new ClassicKeyPosition(KeyboardKey.A + i, 81 + i * 34, 132));
            keys.Add(new ClassicKeyPosition(KeyboardKey.Return, 455, 132, 48));
            keys.Add(new ClassicKeyPosition(KeyboardKey.LeftShift, 13, 166, 82));
            for (int i = 0; i < 10; i++) keys.Add(new ClassicKeyPosition(KeyboardKey.Z + i, 97 + i * 34, 166));
            keys.Add(new ClassicKeyPosition(KeyboardKey.RightShift, 437, 166, 66));
            keys.Add(new ClassicKeyPosition(KeyboardKey.LeftControl, 13, 199, 47)); keys.Add(new ClassicKeyPosition(KeyboardKey.LeftWindows, 62, 199, 47));
            keys.Add(new ClassicKeyPosition(KeyboardKey.LeftAlt, 111, 199, 53)); keys.Add(new ClassicKeyPosition(KeyboardKey.Space, 166, 199, 166));
            keys.Add(new ClassicKeyPosition(KeyboardKey.RightAlt, 335, 199, 53)); keys.Add(new ClassicKeyPosition(KeyboardKey.RightWindows, 390, 199, 54));
            keys.Add(new ClassicKeyPosition(KeyboardKey.RightControl, 446, 199, 57));
            keys.Add(new ClassicKeyPosition(KeyboardKey.Insert, 513, 66)); keys.Add(new ClassicKeyPosition(KeyboardKey.Home, 547, 66)); keys.Add(new ClassicKeyPosition(KeyboardKey.PageUp, 581, 66));
            keys.Add(new ClassicKeyPosition(KeyboardKey.Delete, 513, 99)); keys.Add(new ClassicKeyPosition(KeyboardKey.End, 547, 99)); keys.Add(new ClassicKeyPosition(KeyboardKey.PageDown, 581, 99));
            keys.Add(new ClassicKeyPosition(KeyboardKey.UpArrow, 547, 166)); keys.Add(new ClassicKeyPosition(KeyboardKey.LeftArrow, 513, 199));
            keys.Add(new ClassicKeyPosition(KeyboardKey.DownArrow, 547, 199)); keys.Add(new ClassicKeyPosition(KeyboardKey.RightArrow, 581, 199));
            return keys.AsReadOnly();
        }
    }
}
