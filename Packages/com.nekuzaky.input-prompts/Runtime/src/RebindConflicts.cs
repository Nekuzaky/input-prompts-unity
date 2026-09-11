using System;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    public enum DuplicatePolicy
    {
        Allow = 0,
        Reject = 1,
        Swap = 2,
    }

    public readonly struct RebindConflict
    {
        public readonly InputAction m_action;
        public readonly int m_bindingIndex;

        public RebindConflict(InputAction action, int bindingIndex)
        {
            m_action = action;
            m_bindingIndex = bindingIndex;
        }

        public bool IsValid => m_action != null && m_bindingIndex >= 0;
    }

    public static class RebindConflicts
    {
        #region Main API

        public static RebindConflict Find(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return default;

            var path = action.bindings[bindingIndex].effectivePath;
            if (string.IsNullOrEmpty(path))
                return default;

            var map = action.actionMap;
            if (map == null)
                return FindIn(action, action, bindingIndex, path);

            var asset = map.asset;
            if (asset == null)
                return FindInMap(map, action, bindingIndex, path);

            foreach (var other in asset.actionMaps)
            {
                var conflict = FindInMap(other, action, bindingIndex, path);
                if (conflict.IsValid)
                    return conflict;
            }

            return default;
        }

        public static void Swap(RebindConflict conflict, string previousPath)
        {
            if (!conflict.IsValid)
                return;

            if (string.IsNullOrEmpty(previousPath))
                conflict.m_action.ApplyBindingOverride(conflict.m_bindingIndex, string.Empty);
            else
                conflict.m_action.ApplyBindingOverride(conflict.m_bindingIndex, previousPath);
        }

        #endregion


        #region Tools and Utilities

        private static RebindConflict FindInMap(InputActionMap map, InputAction source, int bindingIndex,
            string path)
        {
            foreach (var action in map.actions)
            {
                var conflict = FindIn(action, source, bindingIndex, path);
                if (conflict.IsValid)
                    return conflict;
            }

            return default;
        }

        private static RebindConflict FindIn(InputAction candidate, InputAction source, int bindingIndex,
            string path)
        {
            var bindings = candidate.bindings;

            for (var i = 0; i < bindings.Count; i++)
            {
                if (candidate == source && i == bindingIndex)
                    continue;

                if (bindings[i].isComposite)
                    continue;

                if (string.Equals(bindings[i].effectivePath, path, StringComparison.OrdinalIgnoreCase))
                    return new RebindConflict(candidate, i);
            }

            return default;
        }

        #endregion
    }
}
