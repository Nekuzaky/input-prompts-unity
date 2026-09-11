using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    /// <summary>
    /// Spawns one <see cref="InputPromptIcon"/> per part of an action, e.g. the four keys of a WASD
    /// composite on keyboard and the single left stick icon on a gamepad.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Input Prompts/Input Prompt Group")]
    public class InputPromptGroup : MonoBehaviour
    {
        #region Private and Protected

        [Header("Action")]
        [SerializeField] private InputActionReference _action;

        [Space(15), Header("Display")]
        [Tooltip("Prefab holding an InputPromptIcon. One instance is created per displayed part.")]
        [SerializeField] private InputPromptIcon _iconPrefab;

        [Tooltip("Parent for the spawned icons. Defaults to this transform.")]
        [SerializeField] private Transform _container;

        private readonly List<InputPromptIcon> _spawned = new();

        private InputAction _runtimeAction;

        #endregion


        #region Public

        /// <summary>Action currently displayed. Setting it rebuilds the icons.</summary>
        public InputAction Action
        {
            get => _runtimeAction ?? (_action != null ? _action.action : null);
            set
            {
                _runtimeAction = value;
                Rebuild();
            }
        }

        #endregion


        #region Unity API

        private void OnEnable()
        {
            InputPromptService.Initialize();
            InputPromptService.PromptsChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            InputPromptService.PromptsChanged -= Rebuild;

            // Icons spawned while editing are previews, not scene content: drop them.
            if (!Application.isPlaying)
                ClearSpawned();
        }

        #endregion


        #region Main API

        /// <summary>Recreate the icons for the current device. Called automatically on device change.</summary>
        public void Rebuild()
        {
            var action = Action;
            if (_iconPrefab == null || action == null)
            {
                SetCount(0);
                return;
            }

            var parts = CollectParts(action);
            SetCount(parts.Count);

            for (var i = 0; i < parts.Count; i++)
            {
                _spawned[i].CompositePart = parts[i];
                _spawned[i].Action = action;
            }
        }

        #endregion


        #region Tools and Utilities

        /// <summary>
        /// Composite part names the active device uses, or a single empty entry for a plain binding.
        /// </summary>
        private static List<string> CollectParts(InputAction action)
        {
            var result = new List<string>();

            if (InputPromptService.ResolveBindingIndex(action, null, allowAnyDevice: false) >= 0)
            {
                // Plain binding for this device: one icon, no part name.
                result.Add(null);
                return result;
            }

            var bindings = action.bindings;
            var hasComposite = false;

            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].isComposite)
                {
                    hasComposite = true;
                    result.Clear();
                    continue;
                }

                if (!bindings[i].isPartOfComposite || !hasComposite)
                    continue;

                if (!InputPromptService.MatchesCurrentStyle(bindings[i].effectivePath))
                    continue;

                var name = bindings[i].name;
                if (!string.IsNullOrEmpty(name) && !result.Contains(name))
                    result.Add(name);
            }

            // Nothing bound on this device: show whatever the action does have, rather than nothing.
            if (result.Count == 0 && InputPromptService.ResolveBindingIndex(action) >= 0)
                result.Add(null);

            return result;
        }

        private void SetCount(int count)
        {
            var parent = _container != null ? _container : transform;

            while (_spawned.Count < count)
            {
                var icon = Instantiate(_iconPrefab, parent);
                icon.gameObject.name = $"{_iconPrefab.name} ({_spawned.Count})";

                // Edit mode previews must never end up saved in the scene.
                if (!Application.isPlaying)
                    icon.gameObject.hideFlags = HideFlags.DontSave;

                _spawned.Add(icon);
            }

            for (var i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    _spawned[i].gameObject.SetActive(i < count);
            }
        }

        private void ClearSpawned()
        {
            foreach (var icon in _spawned)
            {
                if (icon != null)
                    DestroyImmediate(icon.gameObject);
            }

            _spawned.Clear();
        }

        #endregion
    }
}
