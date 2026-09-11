using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Nekuzaky.InputPrompts
{
    public static class InputPromptService
    {
        #region Public

        public static event Action<InputDeviceStyle> StyleChanged;

        public static event Action PromptsChanged;

        public static bool PointerMotionSwitchesStyle { get; set; }

        public static bool UseKeyboardLayoutLabels { get; set; } = true;

        public static bool PreferExactDevice { get; set; }

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
            PreferExactDevice = database.m_preferExactDevice;
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

        public static void Refresh() => PromptsChanged?.Invoke();

        public static int ResolveBindingIndex(InputAction action, string compositePart = null) =>
            ResolveBindingIndex(action, compositePart, allowAnyDevice: true);

        public static int ResolveBindingIndex(InputAction action, string compositePart, bool allowAnyDevice)
        {
            if (action == null)
                return -1;

            if (PreferExactDevice)
            {
                var exact = FindBinding(action, compositePart, MatchesCurrentDevice);
                if (exact >= 0)
                    return exact;
            }

            var layouts = Database != null ? Database.PreferredLayouts(CurrentStyle) : null;
            var index = FindBinding(action, compositePart, path => MatchesAnyLayout(path, layouts));
            if (index >= 0)
                return index;

            return allowAnyDevice ? FindBinding(action, compositePart, _ => true) : -1;
        }

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

        public static Sprite GetSpriteForPath(string path)
        {
            var set = CurrentSet;
            if (set == null)
                return null;

            var key = KeyForPath(path);
            return key == null ? null : set.Find(key);
        }

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

        public static bool MatchesCurrentStyle(string path) => MatchesStyle(path, CurrentStyle);

        internal static bool MatchesStyle(string path, InputDeviceStyle style) =>
            Database != null && MatchesAnyLayout(path, Database.PreferredLayouts(style));

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

        private static void WarnAboutMissingDatabase()
        {
            if (_hasWarnedAboutDatabase)
                return;

            _hasWarnedAboutDatabase = true;
            Debug.LogWarning(
                $"[Input Prompts] No prompt database found at Resources/{ResourcesPath}. Prompts will stay "
                + "empty until you generate one: Tools > Input Prompts > Dashboard, then Generate.");
        }

        private static bool MatchesAnyLayout(string path, IReadOnlyList<string> layouts)
        {
            if (layouts == null)
                return false;

            for (var i = 0; i < layouts.Count; i++)
            {
                if (!string.IsNullOrEmpty(layouts[i]) && TargetsLayout(path, layouts[i]))
                    return true;
            }

            return false;
        }

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
