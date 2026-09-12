using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    [ExecuteAlways]
    [AddComponentMenu("Input Prompts/Input Prompt Group")]
    public class InputPromptGroup : MonoBehaviour
    {
        #region Private and Protected

        [Header("Action")]
        [SerializeField] private InputActionReference _action;

        [Tooltip("Player whose device drives these prompts. Leave empty to follow the last device used by anyone.")]
        [SerializeField] private InputPromptPlayer _player;

        [Space(15), Header("Display")]
        [Tooltip("Prefab holding an InputPromptIcon. One instance is created per displayed part.")]
        [SerializeField] private InputPromptIcon _iconPrefab;

        [Tooltip("Parent for the spawned icons. Defaults to this transform.")]
        [SerializeField] private Transform _container;

        private readonly List<InputPromptIcon> _spawned = new();

        private InputAction _runtimeAction;
        private InputPromptContext _subscribed;

        #endregion


        #region Public

        public InputAction Action
        {
            get => _runtimeAction ?? (_action != null ? _action.action : null);
            set
            {
                _runtimeAction = value;
                Rebuild();
            }
        }

        public InputPromptPlayer Player
        {
            get => _player;
            set
            {
                _player = value;
                if (!isActiveAndEnabled)
                    return;

                Subscribe();
                Rebuild();
            }
        }

        public InputPromptContext Context => _player != null ? _player.Context : InputPromptService.Global;

        #endregion


        #region Unity API

        private void OnEnable()
        {
            InputPromptService.Initialize();
            Subscribe();
            Rebuild();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (!Application.isPlaying)
                ClearSpawned();
        }

        #endregion


        #region Main API

        public void Rebuild()
        {
            var action = Action;
            if (_iconPrefab == null || action == null)
            {
                SetCount(0);
                return;
            }

            var parts = CollectParts(Context, action);
            SetCount(parts.Count);

            for (var i = 0; i < parts.Count; i++)
            {
                _spawned[i].Player = _player;
                _spawned[i].CompositePart = parts[i];
                _spawned[i].Action = action;
            }
        }

        #endregion


        #region Tools and Utilities

        private static List<string> CollectParts(InputPromptContext context, InputAction action)
        {
            var result = new List<string>();

            if (context.ResolveBindingIndex(action, null, allowAnyDevice: false) >= 0)
            {
                result.Add(null);
                return result;
            }

            var bindings = action.bindings;
            var style = context.CurrentStyle;
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

                if (!PromptResolution.MatchesStyle(bindings[i].effectivePath, style))
                    continue;

                var name = bindings[i].name;
                if (!string.IsNullOrEmpty(name) && !result.Contains(name))
                    result.Add(name);
            }

            if (result.Count == 0 && context.ResolveBindingIndex(action) >= 0)
                result.Add(null);

            return result;
        }

        private void Subscribe()
        {
            Unsubscribe();
            _subscribed = Context;
            _subscribed.PromptsChanged += Rebuild;
        }

        private void Unsubscribe()
        {
            if (_subscribed != null)
                _subscribed.PromptsChanged -= Rebuild;

            _subscribed = null;
        }

        private void SetCount(int count)
        {
            var parent = _container != null ? _container : transform;

            while (_spawned.Count < count)
            {
                var icon = Instantiate(_iconPrefab, parent);
                icon.gameObject.name = $"{_iconPrefab.name} ({_spawned.Count})";

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
