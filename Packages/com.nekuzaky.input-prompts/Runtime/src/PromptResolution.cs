using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Nekuzaky.InputPrompts
{
    internal static class PromptResolution
    {
        #region Public

        public const int MaxFallbackDepth = 8;

        #endregion


        #region Main API

        public static bool MatchesStyle(string path, InputDeviceStyle style)
        {
            var database = InputPromptService.Database;
            return database != null && MatchesAnyLayout(path, database.PreferredLayouts(style));
        }

        public static bool MatchesAnyLayout(string path, IReadOnlyList<string> layouts)
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

        public static string KeyForPath(string path)
        {
            var key = ControlPath.ToKey(path);

            if (InputPromptService.UseKeyboardLayoutLabels &&
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

        public static int FindBinding(InputAction action, string compositePart, Func<string, bool> pathFilter)
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

        #endregion


        #region Tools and Utilities

        private static bool TargetsLayout(string path, string layout) => ControlPath.TargetsLayout(path, layout);

        #endregion
    }
}
