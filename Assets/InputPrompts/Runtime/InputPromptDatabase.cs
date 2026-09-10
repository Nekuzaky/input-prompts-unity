using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuz.InputPrompts
{
    /// <summary>
    /// Maps devices to a <see cref="InputPromptSet"/>. Assign one to <see cref="InputPromptService"/>
    /// (it is picked up automatically when placed in a Resources folder as "InputPromptDatabase").
    /// </summary>
    [CreateAssetMenu(menuName = "Input Prompts/Prompt Database", fileName = "InputPromptDatabase")]
    public class InputPromptDatabase : ScriptableObject
    {
        [Tooltip("One set per device family. The first set matching a device wins.")]
        public List<InputPromptSet> sets = new();

        [Tooltip("Used for gamepads that match none of the sets above.")]
        public InputPromptSet gamepadFallback;

        [Tooltip("Style shown before the player has touched anything.")]
        public InputDeviceStyle defaultStyle = InputDeviceStyle.KeyboardMouse;

        public InputPromptSet GetSet(InputDeviceStyle style)
        {
            foreach (var set in sets)
            {
                if (set != null && set.style == style)
                    return set;
            }
            return null;
        }

        /// <summary>Set whose <see cref="InputPromptSet.layouts"/> covers <paramref name="device"/>.</summary>
        public InputPromptSet GetSet(InputDevice device)
        {
            if (device == null)
                return GetSet(defaultStyle);

            foreach (var set in sets)
            {
                if (set == null || set.layouts == null)
                    continue;
                foreach (var layout in set.layouts)
                {
                    if (string.IsNullOrEmpty(layout))
                        continue;
                    if (string.Equals(layout, device.layout, StringComparison.OrdinalIgnoreCase) ||
                        InputSystem.IsFirstLayoutBasedOnSecond(device.layout, layout))
                        return set;
                }
            }

            if (device is Gamepad && gamepadFallback != null)
                return gamepadFallback;

            return GetSet(defaultStyle);
        }

        public InputDeviceStyle GetStyle(InputDevice device)
        {
            var set = GetSet(device);
            return set != null ? set.style : defaultStyle;
        }

        /// <summary>Layouts to look for in an action's bindings when no device is active yet.</summary>
        public IEnumerable<string> PreferredLayouts(InputDeviceStyle style)
        {
            var set = GetSet(style);
            if (set?.layouts == null)
                yield break;
            foreach (var layout in set.layouts)
                yield return layout;
        }
    }
}
