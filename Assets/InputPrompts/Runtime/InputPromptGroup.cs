using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuz.InputPrompts
{
    /// <summary>
    /// Spawns one <see cref="InputPromptIcon"/> per part of an action, e.g. the four keys of a WASD
    /// composite on keyboard and the single left stick icon on a gamepad.
    /// </summary>
    [AddComponentMenu("Input Prompts/Input Prompt Group")]
    public class InputPromptGroup : MonoBehaviour
    {
        [SerializeField] private InputActionReference action;

        [Tooltip("Prefab holding an InputPromptIcon. One instance is created per displayed part.")]
        [SerializeField] private InputPromptIcon iconPrefab;

        [Tooltip("Parent for the spawned icons. Defaults to this transform.")]
        [SerializeField] private Transform container;

        private readonly List<InputPromptIcon> _spawned = new();

        public InputAction Action => action != null ? action.action : null;

        private void OnEnable()
        {
            InputPromptService.Initialize();
            InputPromptService.PromptsChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            InputPromptService.PromptsChanged -= Rebuild;
        }

        /// <summary>Recreate the icons for the current device. Called automatically on device change.</summary>
        public void Rebuild()
        {
            var currentAction = Action;
            if (iconPrefab == null || currentAction == null)
            {
                SetCount(0);
                return;
            }

            var parts = CollectParts(currentAction);
            SetCount(parts.Count);

            for (var i = 0; i < parts.Count; i++)
            {
                _spawned[i].CompositePart = parts[i];
                _spawned[i].Action = currentAction;
            }
        }

        /// <summary>
        /// Composite part names the active device uses, or a single empty entry for a plain binding.
        /// </summary>
        private static List<string> CollectParts(InputAction currentAction)
        {
            var result = new List<string>();

            var index = InputPromptService.ResolveBindingIndex(currentAction);
            if (index >= 0)
            {
                // Plain binding for this device: one icon, no part name.
                result.Add(null);
                return result;
            }

            var bindings = currentAction.bindings;
            var compositeStart = -1;
            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].isComposite)
                {
                    compositeStart = i;
                    result.Clear();
                    continue;
                }

                if (!bindings[i].isPartOfComposite || compositeStart < 0)
                    continue;

                if (!ControlPath.MatchesDevice(bindings[i].effectivePath, InputPromptService.ActiveDevice))
                    continue;

                var name = bindings[i].name;
                if (!string.IsNullOrEmpty(name) && !result.Contains(name))
                    result.Add(name);
            }

            return result;
        }

        private void SetCount(int count)
        {
            var parent = container != null ? container : transform;

            while (_spawned.Count < count)
            {
                var icon = Instantiate(iconPrefab, parent);
                icon.gameObject.name = $"{iconPrefab.name} ({_spawned.Count})";
                _spawned.Add(icon);
            }

            for (var i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    _spawned[i].gameObject.SetActive(i < count);
            }
        }
    }
}
