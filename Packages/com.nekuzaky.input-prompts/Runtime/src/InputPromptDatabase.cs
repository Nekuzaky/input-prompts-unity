using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    /// <summary>
    /// Maps devices to a <see cref="InputPromptSet"/>. Assign one to <see cref="InputPromptService"/>
    /// (it is picked up automatically when placed in a Resources folder as "SO_InputPromptDatabase").
    /// </summary>
    [CreateAssetMenu(menuName = "Input Prompts/Prompt Database", fileName = "SO_InputPromptDatabase")]
    public class InputPromptDatabase : ScriptableObject
    {
        #region Public

        [Header("Sets")]
        [Tooltip("One set per device family. The first set matching a device wins.")]
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

        /// <summary>Set whose layouts cover <paramref name="device"/>.</summary>
        public InputPromptSet GetSet(InputDevice device)
        {
            if (device == null)
                return GetSet(m_defaultStyle);

            foreach (var set in m_sets)
            {
                if (Covers(set, device))
                    return set;
            }

            if (device is Gamepad && m_gamepadFallback != null)
                return m_gamepadFallback;

            return GetSet(m_defaultStyle);
        }

        public InputDeviceStyle GetStyle(InputDevice device)
        {
            var set = GetSet(device);
            return set != null ? set.m_style : m_defaultStyle;
        }

        /// <summary>
        /// Layouts to look for in an action's bindings when no device is active yet. Returns the stored
        /// array rather than an iterator, so resolving a prompt allocates nothing.
        /// </summary>
        public IReadOnlyList<string> PreferredLayouts(InputDeviceStyle style)
        {
            var set = GetSet(style);
            return set?.m_layouts ?? Array.Empty<string>();
        }

        #endregion


        #region Tools and Utilities

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
