using System;
using UnityEngine.InputSystem;

namespace InputPrompts
{
    /// <summary>
    /// Turns Input System control paths into the flat, lower-case keys used by <see cref="InputPromptSet"/>.
    /// "&lt;Keyboard&gt;/space" and "/Keyboard/space" both become "space";
    /// "&lt;Gamepad&gt;/leftStick/up" becomes "leftstick/up".
    /// </summary>
    public static class ControlPath
    {
        #region Main API

        /// <summary>Strips the device part of a binding path and lower-cases the rest.</summary>
        public static string ToKey(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var start = SkipDevice(path);
            if (start < 0)
                return null;

            var key = path.Substring(start);

            // Usages such as "<Gamepad>/{Submit}" cannot be mapped to an icon directly.
            if (key.IndexOf('{') >= 0)
                return null;

            return key.ToLowerInvariant();
        }

        /// <summary>Key for an actual control instance, e.g. the "space" of a live Keyboard.</summary>
        public static string ToKey(InputControl control) => control == null ? null : ToKey(control.path);

        /// <summary>Layout name a binding path targets, e.g. "Gamepad" for "&lt;Gamepad&gt;/buttonSouth".</summary>
        public static string LayoutOf(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '<')
                return null;

            var close = path.IndexOf('>');
            return close <= 1 ? null : path.Substring(1, close - 1);
        }

        /// <summary>True when <paramref name="device"/> can actuate a binding written against <paramref name="path"/>.</summary>
        public static bool MatchesDevice(string path, InputDevice device)
        {
            if (device == null)
                return false;

            var layout = LayoutOf(path);
            if (string.IsNullOrEmpty(layout))
                return false;

            return string.Equals(layout, device.layout, StringComparison.OrdinalIgnoreCase) ||
                   InputSystem.IsFirstLayoutBasedOnSecond(device.layout, layout);
        }

        #endregion


        #region Tools and Utilities

        /// <summary>Index of the first character after the device part, or -1 when there is nothing left.</summary>
        private static int SkipDevice(string path)
        {
            var start = 0;

            if (path[0] == '<')
            {
                var close = path.IndexOf('>');
                if (close < 0)
                    return -1;
                start = close + 1;
            }
            else if (path[0] == '/')
            {
                // Runtime control paths look like "/Keyboard/space".
                var next = path.IndexOf('/', 1);
                if (next < 0)
                    return -1;
                start = next;
            }

            while (start < path.Length && path[start] == '/')
                start++;

            return start >= path.Length ? -1 : start;
        }

        #endregion
    }
}
