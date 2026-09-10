using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Nekuz.InputPrompts
{
    /// <summary>
    /// Tracks which device the player is actually using and resolves actions to icons.
    /// Everything else in this package is a thin view on top of it.
    /// </summary>
    public static class InputPromptService
    {
        private const string ResourcesPath = "InputPromptDatabase";
        private const float ActuationThreshold = 0.15f;

        private static InputPromptDatabase _database;
        private static InputDevice _activeDevice;
        private static bool _initialized;

        /// <summary>Raised when the active device changes to one using a different icon set.</summary>
        public static event Action<InputDeviceStyle> StyleChanged;

        /// <summary>Raised whenever displayed prompts may be stale (device change, rebind, database swap).</summary>
        public static event Action PromptsChanged;

        /// <summary>Set to false to keep prompts on the gamepad while the player nudges the mouse.</summary>
        public static bool PointerMotionSwitchesStyle { get; set; }

        /// <summary>Use the label printed on the physical keyboard (AZERTY, QWERTZ, ...) to pick key icons.</summary>
        public static bool UseKeyboardLayoutLabels { get; set; } = true;

        public static InputPromptDatabase Database
        {
            get
            {
                if (_database == null)
                    _database = Resources.Load<InputPromptDatabase>(ResourcesPath);
                return _database;
            }
            set
            {
                _database = value;
                PromptsChanged?.Invoke();
            }
        }

        public static InputDevice ActiveDevice => _activeDevice;

        public static InputDeviceStyle CurrentStyle =>
            Database != null ? Database.GetStyle(_activeDevice) : InputDeviceStyle.KeyboardMouse;

        public static InputPromptSet CurrentSet =>
            Database != null ? Database.GetSet(_activeDevice) : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;

            InputSystem.onEvent += OnEvent;
            InputSystem.onDeviceChange += OnDeviceChange;
            InputSystem.onActionChange += OnActionChange;

            _activeDevice ??= Gamepad.current as InputDevice ?? Keyboard.current;
        }

        public static void Shutdown()
        {
            if (!_initialized)
                return;
            _initialized = false;
            InputSystem.onEvent -= OnEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onActionChange -= OnActionChange;
        }

        /// <summary>Force the prompts onto a device, e.g. from a PlayerInput in a local co-op game.</summary>
        public static void SetActiveDevice(InputDevice device)
        {
            if (device == _activeDevice)
                return;

            var previousStyle = CurrentStyle;
            _activeDevice = device;
            var style = CurrentStyle;

            if (style != previousStyle)
                StyleChanged?.Invoke(style);
            PromptsChanged?.Invoke();
        }

        /// <summary>Tell every live prompt to refresh, e.g. after applying binding overrides.</summary>
        public static void Refresh() => PromptsChanged?.Invoke();

        // ---------------------------------------------------------------- resolving

        /// <summary>
        /// Index of the binding <paramref name="action"/> uses on the active device, or -1.
        /// Pass <paramref name="compositePart"/> ("up", "left", ...) to target one part of a composite.
        /// </summary>
        public static int ResolveBindingIndex(InputAction action, string compositePart = null)
        {
            if (action == null)
                return -1;

            var index = FindBinding(action, compositePart, path => ControlPath.MatchesDevice(path, _activeDevice));
            if (index >= 0)
                return index;

            // No device yet (or it has no binding): fall back to what the current style would use.
            if (Database != null)
            {
                foreach (var layout in Database.PreferredLayouts(CurrentStyle))
                {
                    if (string.IsNullOrEmpty(layout))
                        continue;

                    var preferred = layout;
                    index = FindBinding(action, compositePart, path =>
                    {
                        var pathLayout = ControlPath.LayoutOf(path);
                        return !string.IsNullOrEmpty(pathLayout) &&
                               (string.Equals(pathLayout, preferred, StringComparison.OrdinalIgnoreCase) ||
                                InputSystem.IsFirstLayoutBasedOnSecond(preferred, pathLayout));
                    });
                    if (index >= 0)
                        return index;
                }
            }

            return FindBinding(action, compositePart, _ => true);
        }

        /// <summary>Icon for an action on the active device, or null when nothing matches.</summary>
        public static Sprite GetSprite(InputAction action, string compositePart = null)
        {
            var index = ResolveBindingIndex(action, compositePart);
            return index < 0 ? null : GetSprite(action, index);
        }

        public static Sprite GetSprite(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return null;
            return GetSpriteForPath(action.bindings[bindingIndex].effectivePath);
        }

        /// <summary>Icon for a raw binding path such as "&lt;Gamepad&gt;/buttonSouth".</summary>
        public static Sprite GetSpriteForPath(string path)
        {
            var set = CurrentSet;
            if (set == null)
                return null;

            var key = KeyForPath(path);
            return key == null ? null : set.Find(key);
        }

        /// <summary>Blank key cap of the current set, drawn behind the display string when no icon exists.</summary>
        public static Sprite GetBlankSprite()
        {
            var set = CurrentSet;
            while (set != null)
            {
                if (set.blankSprite != null)
                    return set.blankSprite;
                set = set.fallback;
            }
            return null;
        }

        /// <summary>Human readable name of the bound control, e.g. "Space" or "A".</summary>
        public static string GetDisplayString(InputAction action, string compositePart = null)
        {
            var index = ResolveBindingIndex(action, compositePart);
            if (index < 0)
                return string.Empty;

            return action.GetBindingDisplayString(
                index, out _, out _,
                InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        private static string KeyForPath(string path)
        {
            var key = ControlPath.ToKey(path);

            // On non-QWERTY hardware "<Keyboard>/w" sits where the player reads "Z": show the label they see.
            if (UseKeyboardLayoutLabels &&
                key != null &&
                Keyboard.current != null &&
                string.Equals(ControlPath.LayoutOf(path), "Keyboard", StringComparison.OrdinalIgnoreCase))
            {
                var label = (InputControlPath.TryFindControl(Keyboard.current, path) as KeyControl)?.displayName;
                if (!string.IsNullOrEmpty(label) && label.Length == 1 && char.IsLetterOrDigit(label[0]))
                    return char.ToLowerInvariant(label[0]).ToString();
            }

            return key;
        }

        private static int FindBinding(InputAction action, string compositePart, Func<string, bool> pathFilter)
        {
            var bindings = action.bindings;
            var wantsPart = !string.IsNullOrEmpty(compositePart);

            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite)
                    continue;

                if (wantsPart)
                {
                    if (!binding.isPartOfComposite ||
                        !string.Equals(binding.name, compositePart, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                else if (binding.isPartOfComposite)
                {
                    continue;
                }

                var path = binding.effectivePath;
                if (!string.IsNullOrEmpty(path) && pathFilter(path))
                    return i;
            }

            return -1;
        }

        // ---------------------------------------------------------------- device tracking

        private static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null || device == _activeDevice)
                return;
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            foreach (var control in eventPtr.EnumerateChangedControls(device, ActuationThreshold))
            {
                if (!PointerMotionSwitchesStyle && IsPointerNoise(control))
                    continue;
                SetActiveDevice(device);
                return;
            }
        }

        private static bool IsPointerNoise(InputControl control)
        {
            if (control == null || control.device is not Pointer)
                return false;

            for (var c = control; c != null; c = c.parent)
            {
                switch (c.name)
                {
                    case "position":
                    case "delta":
                    case "radius":
                    case "pressure":
                    case "twist":
                        return true;
                }
            }
            return false;
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    if (device == _activeDevice)
                        SetActiveDevice(Gamepad.current as InputDevice ?? Keyboard.current);
                    break;
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                    PromptsChanged?.Invoke();
                    break;
            }
        }

        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged)
                PromptsChanged?.Invoke();
        }
    }
}
