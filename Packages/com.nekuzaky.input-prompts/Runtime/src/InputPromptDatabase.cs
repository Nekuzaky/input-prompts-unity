using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    [CreateAssetMenu(menuName = "Input Prompts/Prompt Database", fileName = "SO_InputPromptDatabase")]
    public class InputPromptDatabase : ScriptableObject
    {
        #region Public

        [Header("Sets")]
        [Tooltip("One set per device family. A family naming a specific layout wins over one that only names Gamepad.")]
        public List<InputPromptSet> m_sets = new();

        [Tooltip("Used for gamepads that match none of the sets above.")]
        public InputPromptSet m_gamepadFallback;

        [Space(15), Header("Startup")]
        [Tooltip("Style shown before the player has touched anything.")]
        public InputDeviceStyle m_defaultStyle = InputDeviceStyle.KeyboardMouse;

        [Tooltip("Moving the mouse switches the prompts back to mouse icons.")]
        public bool m_pointerMotionSwitchesStyle;

        [Tooltip("Pick key icons from the label printed on the player keyboard (AZERTY, QWERTZ, ...).")]
        public bool m_useKeyboardLayoutLabels = true;

        [Tooltip("Resolve bindings against the exact device rather than its family. Off keeps keyboard and mouse as one.")]
        public bool m_preferExactDevice;

        #endregion


        #region Private and Protected

        private const string RootGamepadLayout = "Gamepad";

        #endregion


        #region Main API

        public InputPromptSet GetSet(InputDeviceStyle style)
        {
            foreach (var set in m_sets)
            {
                if (set != null && set.m_style == style)
                    return set;
            }

            return null;
        }

        public InputPromptSet GetSet(InputDevice device)
        {
            if (device == null)
                return GetSet(m_defaultStyle);

            foreach (var set in m_sets)
            {
                if (!IsCatchAll(set) && Covers(set, device))
                    return set;
            }

            if (device is Gamepad && m_gamepadFallback != null)
                return m_gamepadFallback;

            foreach (var set in m_sets)
            {
                if (IsCatchAll(set) && Covers(set, device))
                    return set;
            }

            return GetSet(m_defaultStyle);
        }

        public InputDeviceStyle GetStyle(InputDevice device)
        {
            var set = GetSet(device);
            return set != null ? set.m_style : m_defaultStyle;
        }

        public List<InputPromptSet> FindSetsFor(string bindingPath)
        {
            var result = new List<InputPromptSet>();

            foreach (var set in m_sets)
            {
                if (set == null || set.m_layouts == null)
                    continue;

                foreach (var layout in set.m_layouts)
                {
                    if (!ControlPath.TargetsLayout(bindingPath, layout))
                        continue;

                    result.Add(set);
                    break;
                }
            }

            return result;
        }

        public bool IsShadowedByFallback(InputPromptSet set) =>
            IsCatchAll(set) && m_gamepadFallback != null && m_gamepadFallback != set;

        public IReadOnlyList<string> PreferredLayouts(InputDeviceStyle style)
        {
            var set = GetSet(style);
            return set?.m_layouts ?? Array.Empty<string>();
        }

        #endregion


        #region Tools and Utilities

        private static bool IsCatchAll(InputPromptSet set)
        {
            if (set == null || set.m_layouts == null || set.m_layouts.Length == 0)
                return false;

            foreach (var layout in set.m_layouts)
            {
                if (!string.Equals(layout, RootGamepadLayout, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static bool Covers(InputPromptSet set, InputDevice device)
        {
            if (set == null || set.m_layouts == null)
                return false;

            foreach (var layout in set.m_layouts)
            {
                if (string.IsNullOrEmpty(layout))
                    continue;

                if (string.Equals(layout, device.layout, StringComparison.OrdinalIgnoreCase) ||
                    InputSystem.IsFirstLayoutBasedOnSecond(device.layout, layout))
                    return true;
            }

            return false;
        }

        #endregion
    }
}
