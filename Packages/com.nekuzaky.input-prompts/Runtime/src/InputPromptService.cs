using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Nekuzaky.InputPrompts
{
    public static class InputPromptService
    {
        #region Public

        public static event Action<InputDeviceStyle> StyleChanged
        {
            add => Global.StyleChanged += value;
            remove => Global.StyleChanged -= value;
        }

        public static event Action PromptsChanged
        {
            add => Global.PromptsChanged += value;
            remove => Global.PromptsChanged -= value;
        }

        public static bool PointerMotionSwitchesStyle { get; set; }

        public static bool UseKeyboardLayoutLabels { get; set; } = true;

        public static bool PreferExactDevice { get; set; }

        public static Func<string, string, string> ControlNameTranslator { get; set; }

        public static InputPromptContext Global => _global ??= CreateGlobal();

        public static IReadOnlyList<InputPromptContext> Contexts => _contexts;

        public static InputDevice ActiveDevice => Global.ActiveDevice;

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
                RefreshAll();
            }
        }

        public static InputDeviceStyle CurrentStyle => Global.CurrentStyle;

        public static InputPromptSet CurrentSet => Global.CurrentSet;

        public static TMP_SpriteAsset CurrentSpriteAsset => Global.CurrentSpriteAsset;

#if UNITY_EDITOR
        public static InputDeviceStyle? EditorPreviewStyle { get; set; }
#endif

        #endregion


        #region Private and Protected

        private const string ResourcesPath = "SO_InputPromptDatabase";
        private const float ActuationThreshold = 0.15f;

        private static readonly List<InputPromptContext> _contexts = new();

        private static InputPromptDatabase _database;
        private static InputPromptContext _global;
        private static bool _isInitialized;
        private static bool _hasWarnedAboutDatabase;

#if UNITY_EDITOR
        internal static bool UsesPreview => !Application.isPlaying && EditorPreviewStyle.HasValue;
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

            if (Global.ActiveDevice == null)
                Global.SetActiveDevice(Gamepad.current as InputDevice ?? Keyboard.current);

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

        public static void Register(InputPromptContext context)
        {
            if (context != null && !_contexts.Contains(context))
                _contexts.Add(context);
        }

        public static void Unregister(InputPromptContext context)
        {
            if (context != null && context != _global)
                _contexts.Remove(context);
        }

        public static void SetActiveDevice(InputDevice device) => Global.SetActiveDevice(device);

        public static void Refresh() => RefreshAll();

        public static int ResolveBindingIndex(InputAction action, string compositePart = null) =>
            Global.ResolveBindingIndex(action, compositePart);

        public static int ResolveBindingIndex(InputAction action, string compositePart, bool allowAnyDevice) =>
            Global.ResolveBindingIndex(action, compositePart, allowAnyDevice);

        public static Sprite GetSprite(InputAction action, string compositePart = null) =>
            Global.GetSprite(action, compositePart);

        public static Sprite GetSprite(InputAction action, int bindingIndex) => Global.GetSprite(action, bindingIndex);

        public static Sprite GetSpriteForPath(string path) => Global.GetSpriteForPath(path);

        public static Sprite GetBlankSprite() => Global.GetBlankSprite();

        public static string GetDisplayString(InputAction action, string compositePart = null) =>
            Global.GetDisplayString(action, compositePart);

        public static string GetDisplayString(InputAction action, int bindingIndex) =>
            Global.GetDisplayString(action, bindingIndex);

        public static string GetSpriteName(InputAction action, string compositePart = null) =>
            Global.GetSpriteName(action, compositePart);

        public static bool MatchesCurrentStyle(string path) => Global.MatchesCurrentStyle(path);

        public static bool MatchesCurrentDevice(string path) => Global.MatchesCurrentDevice(path);

        #endregion


        #region Tools and Utilities

        private static InputPromptContext CreateGlobal()
        {
            var context = new InputPromptContext();
            _contexts.Insert(0, context);
            return context;
        }

        private static void RefreshAll()
        {
            foreach (var context in Snapshot())
                context.Refresh();
        }

        private static InputPromptContext[] Snapshot()
        {
            _ = Global;
            return _contexts.ToArray();
        }

        private static void WarnAboutMissingDatabase()
        {
            if (_hasWarnedAboutDatabase)
                return;

            _hasWarnedAboutDatabase = true;
            Debug.LogWarning(
                $"[Input Prompts] No prompt database found at Resources/{ResourcesPath}. Prompts will stay "
                + "empty until you generate one: Tools > Input Prompts > Dashboard, then Generate.");
        }

        private static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null || !AnyContextWants(device))
                return;

            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            foreach (var control in eventPtr.EnumerateChangedControls(device, ActuationThreshold))
            {
                if (!PointerMotionSwitchesStyle && IsPointerNoise(control))
                    continue;

                foreach (var context in Snapshot())
                    context.NotifyDeviceUsed(device);
                return;
            }
        }

        private static bool AnyContextWants(InputDevice device)
        {
            _ = Global;
            for (var i = 0; i < _contexts.Count; i++)
            {
                if (_contexts[i].WantsDevice(device))
                    return true;
            }

            return false;
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
                    foreach (var context in Snapshot())
                        context.NotifyDeviceLost(device);
                    break;

                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                    RefreshAll();
                    break;
            }
        }

        private static void OnActionChange(object subject, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged)
                RefreshAll();
        }

        #endregion
    }
}
