using System;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    public static class ControlPath
    {
        #region Main API

        public static string ToKey(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var start = SkipDevice(path);
            if (start < 0)
                return null;

            var key = path.Substring(start);

            if (key.IndexOf('{') >= 0)
                return null;

            return key.ToLowerInvariant();
        }

        public static string ToKey(InputControl control) => control == null ? null : ToKey(control.path);

        public static string ToSpriteName(string key) =>
            string.IsNullOrEmpty(key) ? null : key.Replace("/", "_");

        public static string LayoutOf(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '<')
                return null;

            var close = path.IndexOf('>');
            return close <= 1 ? null : path.Substring(1, close - 1);
        }

        public static bool TargetsLayout(string path, string layout)
        {
            var pathLayout = LayoutOf(path);
            if (string.IsNullOrEmpty(pathLayout) || string.IsNullOrEmpty(layout))
                return false;

            return string.Equals(pathLayout, layout, StringComparison.OrdinalIgnoreCase) ||
                   InputSystem.IsFirstLayoutBasedOnSecond(layout, pathLayout);
        }

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
