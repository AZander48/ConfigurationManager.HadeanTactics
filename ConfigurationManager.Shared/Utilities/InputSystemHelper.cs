// Input System support for BepInEx 5 Mono — added 2026 by AZander48.
// Based on BepInEx.ConfigurationManager (LGPL v3) by ManlyMarco / MarC0.
#if Mono
using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ConfigurationManager.Utilities
{
    /// <summary>
    /// Input System backend for games that do not feed legacy UnityEngine.Input.
    /// </summary>
    internal static class InputSystemHelper
    {
        private static Action _hotkeyAction;
        private static bool _registered;

        public static bool Available => Keyboard.current != null;

        public static void RegisterHotkeyChecker(Action onHotkey)
        {
            _hotkeyAction = onHotkey;
            if (_registered)
                return;

            InputSystem.onAfterUpdate += OnAfterInputUpdate;
            _registered = true;
        }

        public static void UnregisterHotkeyChecker()
        {
            if (!_registered)
                return;

            InputSystem.onAfterUpdate -= OnAfterInputUpdate;
            _registered = false;
            _hotkeyAction = null;
        }

        private static void OnAfterInputUpdate()
        {
            try
            {
                if (_hotkeyAction != null)
                    _hotkeyAction();
            }
            catch (Exception ex)
            {
                ConfigurationManager.Logger.LogError("Input System hotkey handler failed: " + ex);
            }
        }

        /// <summary>
        /// Main key pressed this frame and all listed modifiers held.
        /// </summary>
        public static bool IsShortcutDown(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
                return false;

            if (!WasPressedThisFrame(shortcut.MainKey))
                return false;

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!IsPressed(modifier))
                    return false;
            }

            return true;
        }

        public static bool GetKeyUp(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None)
                return false;

            KeyControl control = FindKeyControl(keyCode);
            return control != null && control.wasReleasedThisFrame;
        }

        public static bool GetKey(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None)
                return false;

            KeyControl control = FindKeyControl(keyCode);
            return control != null && control.isPressed;
        }

        public static Vector2 GetMousePosition()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return Vector2.zero;

            Vector2 position = mouse.position.ReadValue();
            position.y = Screen.height - position.y;
            return position;
        }

        private static bool WasPressedThisFrame(KeyCode keyCode)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            KeyControl direct = GetDirectKey(keyboard, keyCode);
            if (direct != null)
                return direct.wasPressedThisFrame;

            for (int i = 0; i < keyboard.allKeys.Count; i++)
            {
                KeyControl key = keyboard.allKeys[i];
                if (key != null && key.wasPressedThisFrame && KeyMatches(key, keyCode))
                    return true;
            }

            return false;
        }

        private static bool IsPressed(KeyCode keyCode)
        {
            KeyControl control = FindKeyControl(keyCode);
            return control != null && control.isPressed;
        }

        private static KeyControl FindKeyControl(KeyCode keyCode)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return null;

            KeyControl direct = GetDirectKey(keyboard, keyCode);
            if (direct != null)
                return direct;

            for (int i = 0; i < keyboard.allKeys.Count; i++)
            {
                KeyControl key = keyboard.allKeys[i];
                if (key != null && KeyMatches(key, keyCode))
                    return key;
            }

            return null;
        }

        private static KeyControl GetDirectKey(Keyboard keyboard, KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.F1: return keyboard.f1Key;
                case KeyCode.F2: return keyboard.f2Key;
                case KeyCode.F3: return keyboard.f3Key;
                case KeyCode.F4: return keyboard.f4Key;
                case KeyCode.F5: return keyboard.f5Key;
                case KeyCode.F6: return keyboard.f6Key;
                case KeyCode.F7: return keyboard.f7Key;
                case KeyCode.F8: return keyboard.f8Key;
                case KeyCode.F9: return keyboard.f9Key;
                case KeyCode.F10: return keyboard.f10Key;
                case KeyCode.F11: return keyboard.f11Key;
                case KeyCode.F12: return keyboard.f12Key;
                case KeyCode.LeftShift: return keyboard.leftShiftKey;
                case KeyCode.RightShift: return keyboard.rightShiftKey;
                case KeyCode.LeftControl: return keyboard.leftCtrlKey;
                case KeyCode.RightControl: return keyboard.rightCtrlKey;
                case KeyCode.LeftAlt: return keyboard.leftAltKey;
                case KeyCode.RightAlt: return keyboard.rightAltKey;
                default: return null;
            }
        }

        private static bool KeyMatches(KeyControl control, KeyCode keyCode)
        {
            string expected = keyCode.ToString().ToLowerInvariant();
            string path = control.path;
            if (path != null && path.EndsWith("/" + expected, StringComparison.OrdinalIgnoreCase))
                return true;

            string name = control.name;
            return name != null && string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
