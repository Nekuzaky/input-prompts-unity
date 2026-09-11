using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Nekuzaky.InputPrompts
{
    /// <summary>
    /// Tracks which device the player is actually using and resolves actions to icons.
    /// Everything else in this package is a thin view on top of it.
    /// </summary>
    public static class InputPromptService
    {
        #region Public

        /// <summary>Raised when the active device changes to one using a different icon set.</summary>
        public static event Action<InputDeviceStyle> StyleChanged;

        /// <summary>Raised whenever displayed prompts may be stale (device change, rebind, database swap).</summary>
        public static event Action PromptsChanged;

        /// <summary>Set to true to switch back to mouse icons as soon as the player moves the mouse.</summary>
        public static bool PointerMotionSwitchesStyle { get; set; }

        /// <summary>Use the label printed on the physical keyboard (AZERTY, QWERTZ, ...) to pick key icons.</summary>
        public static bool UseKeyboardLayoutLabels { get; set; } = true;

        public static InputDevice ActiveDevice => _activeDevice;

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

        public static InputDeviceStyle CurrentStyle
        {
            get
            {
#if UNITY_EDITOR
                if (UsesPreview)
                    return EditorPreviewStyle.Value;
#endif
                return Database != null ? Database.GetStyle(_activeDevice) : InputDeviceStyle.KeyboardMouse;
            }
        }

        public static InputPromptSet CurrentSet
        {
            get
            {
                if (Database == null)
                    return null;
#if UNITY_EDITOR
                if (UsesPreview)
                    return Database.GetSet(EditorPreviewStyle.Value);
#endif
                return Database.GetSet(_activeDevice);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Style to show while editing, so a menu can be checked against PlayStation icons without a
        /// DualSense plugged in. Ignored in play mode and in builds.
        /// </summary>
        public static InputDeviceStyle? EditorPreviewStyle { get; set; }
#endif

        #endregion


        #region Private and Protected

        private const string ResourcesPath = "SO_InputPromptDatabase";
        private const float ActuationThreshold = 0.15f;
        private const int MaxFallbackDepth = 8;

        private static InputPromptDatabase _database;
        private static InputDevice _activeDevice;
        private static bool _isInitialized;
        private static bool _hasWarnedAboutDatabase;

#if UNITY_EDITOR
        private static bool UsesPreview => !Application.isPlaying && EditorPreviewStyle.HasValue;
#endif

        #endregion


        #region Main API

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            InputSystem.onEvent += OnEvent;
            InputSystem.onDeviceChange += OnDeviceChange;
            InputSystem.onActionChange += OnActionChange;

            _activeDevice ??= Gamepad.current as InputDevice ?? Keyboard.current;

            var database = Database;
            if (database == null)
            {
                WarnAboutMissingDatabase();
                return;
            }

            PointerMotionSwitchesStyle = database.m_pointerMotionSwitchesStyle;
            UseKeyboardLayoutLabels = database.m_useKeyboardLayoutLabels;
        }

        public static void Shutdown()
        {
            if (!_isInitialized)
                return;

            _isInitialized = false;
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

        /// <summary>
        /// Index of the binding <paramref name="action"/> uses on the active device, or -1.
        /// Pass <paramref name="compositePart"/> ("up", "left", ...) to target one part of a composite.
        /// </summary>
        public static int ResolveBindingIndex(InputAction action, string compositePart = null) =>
            ResolveBindingIndex(action, compositePart, allowAnyDevice: true);

        /// <summary>
        /// Same as <see cref="ResolveBindingIndex(InputAction,string)"/>, but with
        /// <paramref name="allowAnyDevice"/> set to false it returns -1 instead of falling back to a
        /// binding meant for another device.
        /// </summary>
        public static int ResolveBindingIndex(InputAction action, string compositePart, bool allowAnyDevice)
        {
            if (action == null)
                return -1;

            var index = FindBinding(action, compositePart, MatchesCurrentDevice);
            if (index >= 0)
                return index;

            // No device yet (or it has no binding): fall back to what the current style would use.
            if (Database != null)
            {
                var layouts = Database.PreferredLayouts(CurrentStyle);
                for (var i = 0; i < layouts.Count; i++)
                {
                    var preferred = layouts[i];
                    if (string.IsNullOrEmpty(preferred))
                        continue;

                    index = FindBinding(action, compositePart, path => TargetsLayout(path, preferred));
                    if (index >= 0)
                        return index;
                }
            }

            return allowAnyDevice ? FindBinding(action, compositePart, _ => true) : -1;
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
            for (var depth = 0; set != null && depth < MaxFallbackDepth; depth++)
            {
                if (set.m_blankSprite != null)
                    return set.m_blankSprite;
                set = set.m_fallback != set ? set.m_fallback : null;
            }

            return null;
        }

        /// <summary>Human readable name of the bound control, e.g. "Space" or "A".</summary>
        public static string GetDisplayString(InputAction action, string compositePart = null)
        {
            var index = ResolveBindingIndex(action, compositePart);
            return index < 0 ? string.Empty : GetDisplayString(action, index);
        }

        public static string GetDisplayString(InputAction action, int bindingIndex) =>
            action.GetBindingDisplayString(
                bindingIndex, out _, out _,
                InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
                InputBinding.DisplayStringOptions.DontIncludeInteractions);

        /// <summary>True when the binding path belongs to the device the prompts are currently showing.</summary>
        public static bool MatchesCurrentDevice(string path)
        {
#if UNITY_EDITOR
            if (UsesPreview)
                return MatchesStyle(path, EditorPreviewStyle.Value);
#endif
            return ControlPath.MatchesDevice(path, _activeDevice);
        }

        #endregion


        #region Tools and Utilities

        /// <summary>
        /// Without a database nothing can be resolved and every prompt stays blank, which is hard to
        /// diagnose from the outside. Say it once, loudly enough to be actionable.
        /// </summary>
        private static void WarnAboutMissingDatabase()
        {
            if (_hasWarnedAboutDatabase)
                return;

            _hasWarnedAboutDatabase = true;
            Debug.LogWarning(
                $"[Input Prompts] No prompt database found at Resources/{ResourcesPath}. Prompts will stay "
                + "empty until you generate one: Tools > Input Prompts > Dashboard, then Generate.");
        }

        private static bool MatchesStyle(string path, InputDeviceStyle style)
        {
            if (Database == null)
                return false;

            var layouts = Database.PreferredLayouts(style);
            for (var i = 0; i < layouts.Count; i++)
            {
                if (!string.IsNullOrEmpty(layouts[i]) && TargetsLayout(path, layouts[i]))
                    return true;
            }

            return false;
        }

        /// <summary>True when a device of <paramref name="layout"/> can actuate <paramref name="path"/>.</summary>
        private static bool TargetsLayout(string path, string layout)
        {
            var pathLayout = ControlPath.LayoutOf(path);
            if (string.IsNullOrEmpty(pathLayout))
                return false;

            return string.Equals(pathLayout, layout, StringComparison.OrdinalIgnoreCase) ||
                   InputSystem.IsFirstLayoutBasedOnSecond(layout, pathLayout);
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

        /// <summary>True for the controls a mouse keeps reporting even when the player is on a gamepad.</summary>
        private static bool IsPointerNoise(InputControl control)
        {
            if (control == null || control.device is not Pointer)
                return false;

            for (var current = control; current != null; current = current.parent)
            {
                switch (current.name)
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

        private static void OnActionChange(object subject, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged)
                PromptsChanged?.Invoke();
        }

        #endregion
    }
}
