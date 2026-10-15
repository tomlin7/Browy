using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Browy.Services
{
    public static class ShortcutService
    {
        private static readonly Dictionary<string, string> DefaultShortcuts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["new_tab"] = "Ctrl+T",
            ["close_tab"] = "Ctrl+W",
            ["reopen_tab"] = "Ctrl+Shift+T",
            ["command_palette"] = "Ctrl+K",
            ["find"] = "Ctrl+F",
            ["downloads"] = "Ctrl+Shift+J",
            ["history"] = "Ctrl+H",
            ["split_view"] = @"Ctrl+\",
            ["mute"] = "Ctrl+M",
            ["fullscreen"] = "F11",
            ["sidebar"] = "Ctrl+S",
            ["devtools"] = "F12",
            ["reader_mode"] = "F9",
            ["bookmarks_bar"] = "Ctrl+Shift+B",
            ["reload"] = "Ctrl+R",
            ["print"] = "Ctrl+P",
            ["pip"] = "Alt+P"
        };

        public static IReadOnlyDictionary<string, string> Defaults => DefaultShortcuts;

        public static string GetDefaultShortcut(string actionId)
        {
            if (DefaultShortcuts.TryGetValue(actionId, out var shortcut))
            {
                return shortcut;
            }
            return string.Empty;
        }

        public static string GetEffectiveShortcut(string actionId, IDictionary<string, string>? customBindings)
        {
            if (customBindings != null && customBindings.TryGetValue(actionId, out var custom) && !string.IsNullOrWhiteSpace(custom))
            {
                return NormalizeKeyChord(custom);
            }
            return GetDefaultShortcut(actionId);
        }

        public static string NormalizeKeyChord(string chord)
        {
            if (string.IsNullOrWhiteSpace(chord)) return string.Empty;
            chord = chord.Trim();
            var parts = chord.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool ctrl = false, shift = false, alt = false;
            string keyPart = string.Empty;

            foreach (var part in parts)
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    ctrl = true;
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    shift = true;
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    alt = true;
                else
                    keyPart = NormalizeKeyPart(part);
            }

            var result = new List<string>(4);
            if (ctrl) result.Add("Ctrl");
            if (alt) result.Add("Alt");
            if (shift) result.Add("Shift");
            if (!string.IsNullOrEmpty(keyPart)) result.Add(keyPart);

            return string.Join("+", result);
        }

        private static string NormalizeKeyPart(string part)
        {
            if (part == @"\" || part.Equals("OemBackslash", StringComparison.OrdinalIgnoreCase) || part.Equals("Oem5", StringComparison.OrdinalIgnoreCase))
                return @"\";
            if (part == "," || part.Equals("OemComma", StringComparison.OrdinalIgnoreCase))
                return ",";
            if (part == "." || part.Equals("OemPeriod", StringComparison.OrdinalIgnoreCase))
                return ".";
            if (part == "/" || part.Equals("OemQuestion", StringComparison.OrdinalIgnoreCase))
                return "/";
            if (part == "+" || part.Equals("OemPlus", StringComparison.OrdinalIgnoreCase) || part.Equals("Add", StringComparison.OrdinalIgnoreCase))
                return "+";
            if (part == "-" || part.Equals("OemMinus", StringComparison.OrdinalIgnoreCase) || part.Equals("Subtract", StringComparison.OrdinalIgnoreCase))
                return "-";

            return part.ToUpperInvariant();
        }

        public static string? ToKeyChord(ModifierKeys modifiers, Key key)
        {
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return null;
            }

            string keyName = KeyToString(key);
            if (string.IsNullOrEmpty(keyName)) return null;

            var result = new List<string>(4);
            if (modifiers.HasFlag(ModifierKeys.Control)) result.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Alt)) result.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Shift)) result.Add("Shift");
            result.Add(keyName);

            return string.Join("+", result);
        }

        public static string? KeyEventArgsToChord(KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return null;
            }

            var modifiers = Keyboard.Modifiers;
            string keyName = KeyToString(key);
            if (string.IsNullOrEmpty(keyName)) return null;

            var result = new List<string>(4);
            if (modifiers.HasFlag(ModifierKeys.Control)) result.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Alt) || e.Key == Key.System) result.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Shift)) result.Add("Shift");
            result.Add(keyName);

            return string.Join("+", result);
        }

        private static string KeyToString(Key key)
        {
            return key switch
            {
                Key.OemBackslash or Key.Oem5 => @"\",
                Key.OemComma => ",",
                Key.OemPeriod => ".",
                Key.OemQuestion => "/",
                Key.OemMinus => "-",
                Key.OemPlus => "+",
                Key.Subtract => "-",
                Key.Add => "+",
                Key.D0 => "0",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                _ => key.ToString().ToUpperInvariant()
            };
        }

        public static bool TryMatchAction(KeyEventArgs e, IDictionary<string, string>? customBindings, out string actionId)
        {
            actionId = string.Empty;
            string? chord = KeyEventArgsToChord(e);
            if (string.IsNullOrEmpty(chord)) return false;

            return TryMatchChord(chord, customBindings, out actionId);
        }

        public static bool TryMatchChord(string rawChord, IDictionary<string, string>? customBindings, out string actionId)
        {
            actionId = string.Empty;
            if (string.IsNullOrWhiteSpace(rawChord)) return false;

            string normalized = NormalizeKeyChord(rawChord);

            // 1. Check custom bindings first
            if (customBindings != null && customBindings.Count > 0)
            {
                foreach (var kvp in customBindings)
                {
                    if (NormalizeKeyChord(kvp.Value).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        actionId = kvp.Key;
                        return true;
                    }
                }
            }

            // 2. Check default shortcuts, but only if that action has not been overridden to something else
            foreach (var kvp in DefaultShortcuts)
            {
                if (NormalizeKeyChord(kvp.Value).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    if (customBindings != null && customBindings.TryGetValue(kvp.Key, out var customVal) && !string.IsNullOrWhiteSpace(customVal))
                    {
                        // Action was remapped away from default
                        continue;
                    }
                    actionId = kvp.Key;
                    return true;
                }
            }

            return false;
        }
    }
}
