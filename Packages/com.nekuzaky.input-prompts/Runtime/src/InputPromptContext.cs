using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    public class InputPromptContext
    {
        #region Public

        public event Action<InputDeviceStyle> StyleChanged;

        public event Action PromptsChanged;

        public InputDevice ActiveDevice => _activeDevice;

        public InputDeviceStyle CurrentStyle
        {
            get
            {
#if UNITY_EDITOR
                if (InputPromptService.UsesPreview)
                    return InputPromptService.EditorPreviewStyle.Value;
#endif
                var database = InputPromptService.Database;
                return database != null ? database.GetStyle(_activeDevice) : InputDeviceStyle.KeyboardMouse;
            }
        }

        public InputPromptSet CurrentSet
        {
            get
            {
                var database = InputPromptService.Database;
                if (database == null)
                    return null;
#if UNITY_EDITOR
                if (InputPromptService.UsesPreview)
                    return database.GetSet(InputPromptService.EditorPreviewStyle.Value);
#endif
                return database.GetSet(_activeDevice);
            }
        }

        public TMP_SpriteAsset CurrentSpriteAsset => CurrentSet != null ? CurrentSet.m_spriteAsset : null;

        #endregion


        #region Private and Protected

        private readonly Func<InputDevice, bool> _owns;
        private InputDevice _activeDevice;

        #endregion


        #region Main API

        public InputPromptContext(Func<InputDevice, bool> owns = null, InputDevice initialDevice = null)
        {
            _owns = owns;
            _activeDevice = initialDevice;
        }

        public bool Owns(InputDevice device) => device != null && (_owns == null || _owns(device));

        public void SetActiveDevice(InputDevice device)
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

        public void Refresh() => PromptsChanged?.Invoke();

        public int ResolveBindingIndex(InputAction action, string compositePart = null) =>
            ResolveBindingIndex(action, compositePart, allowAnyDevice: true);

        public int ResolveBindingIndex(InputAction action, string compositePart, bool allowAnyDevice)
        {
            if (action == null)
                return -1;

            if (InputPromptService.PreferExactDevice)
            {
                var exact = PromptResolution.FindBinding(action, compositePart, MatchesCurrentDevice);
                if (exact >= 0)
                    return exact;
            }

            var database = InputPromptService.Database;
            var layouts = database != null ? database.PreferredLayouts(CurrentStyle) : null;
            var index = PromptResolution.FindBinding(action, compositePart,
                path => PromptResolution.MatchesAnyLayout(path, layouts));
            if (index >= 0)
                return index;

            return allowAnyDevice ? PromptResolution.FindBinding(action, compositePart, _ => true) : -1;
        }

        public Sprite GetSprite(InputAction action, string compositePart = null)
        {
            var index = ResolveBindingIndex(action, compositePart);
            return index < 0 ? null : GetSprite(action, index);
        }

        public Sprite GetSprite(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return null;

            return GetSpriteForPath(action.bindings[bindingIndex].effectivePath);
        }

        public Sprite GetSpriteForPath(string path)
        {
            var set = CurrentSet;
            if (set == null)
                return null;

            var key = PromptResolution.KeyForPath(path);
            return key == null ? null : set.Find(key);
        }

        public Sprite GetBlankSprite()
        {
            var set = CurrentSet;
            for (var depth = 0; set != null && depth < PromptResolution.MaxFallbackDepth; depth++)
            {
                if (set.m_blankSprite != null)
                    return set.m_blankSprite;
                set = set.m_fallback != set ? set.m_fallback : null;
            }

            return null;
        }

        public string GetDisplayString(InputAction action, string compositePart = null)
        {
            var index = ResolveBindingIndex(action, compositePart);
            return index < 0 ? string.Empty : GetDisplayString(action, index);
        }

        public string GetDisplayString(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return string.Empty;

            var name = action.GetBindingDisplayString(
                bindingIndex, out _, out _,
                InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
                InputBinding.DisplayStringOptions.DontIncludeInteractions);

            var translator = InputPromptService.ControlNameTranslator;
            if (translator == null)
                return name;

            var key = PromptResolution.KeyForPath(action.bindings[bindingIndex].effectivePath);
            var translated = translator(key, name);
            return string.IsNullOrEmpty(translated) ? name : translated;
        }

        public string GetSpriteName(InputAction action, string compositePart = null)
        {
            var index = ResolveBindingIndex(action, compositePart);
            if (index < 0)
                return null;

            var set = CurrentSet;
            if (set == null || set.m_spriteAsset == null)
                return null;

            var key = PromptResolution.KeyForPath(action.bindings[index].effectivePath);
            return key != null && set.Contains(key) ? ControlPath.ToSpriteName(key) : null;
        }

        public bool MatchesCurrentStyle(string path) => PromptResolution.MatchesStyle(path, CurrentStyle);

        public bool MatchesCurrentDevice(string path)
        {
#if UNITY_EDITOR
            if (InputPromptService.UsesPreview)
                return PromptResolution.MatchesStyle(path, InputPromptService.EditorPreviewStyle.Value);
#endif
            return ControlPath.MatchesDevice(path, _activeDevice);
        }

        #endregion


        #region Tools and Utilities

        internal bool WantsDevice(InputDevice device) => device != _activeDevice && Owns(device);

        internal void NotifyDeviceUsed(InputDevice device)
        {
            if (WantsDevice(device))
                SetActiveDevice(device);
        }

        internal void NotifyDeviceLost(InputDevice device)
        {
            if (device == _activeDevice)
                SetActiveDevice(FindFallbackDevice());
        }

        private InputDevice FindFallbackDevice()
        {
            InputDevice keyboard = null;

            foreach (var device in InputSystem.devices)
            {
                if (!device.added || !Owns(device))
                    continue;

                if (device is Gamepad)
                    return device;

                if (keyboard == null && device is Keyboard)
                    keyboard = device;
            }

            return keyboard;
        }

        #endregion
    }
}
