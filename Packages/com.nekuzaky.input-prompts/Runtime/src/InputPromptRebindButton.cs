using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nekuzaky.InputPrompts
{
    [AddComponentMenu("Input Prompts/Input Prompt Rebind Button")]
    public class InputPromptRebindButton : MonoBehaviour
    {
        #region Private and Protected

        [Header("Action")]
        [SerializeField] private InputActionReference _action;

        [Tooltip("Part of a composite to rebind, e.g. \"up\". Leave empty for a plain binding.")]
        [SerializeField] private string _compositePart;

        [Space(15), Header("Display")]
        [Tooltip("Icon showing the control currently bound. Hidden while listening.")]
        [SerializeField] private InputPromptIcon _icon;

        [Tooltip("Optional label. Shows the listening text while waiting for an input.")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("Button that starts the rebind. Defaults to the one on this object, if any.")]
        [SerializeField] private Button _button;

        [SerializeField] private string _listeningText = "Press any key";

        [Space(15), Header("Rebinding")]
        [Tooltip("Control that cancels the rebind.")]
        [SerializeField] private string _cancelPath = "<Keyboard>/escape";

        [Tooltip("Controls the player cannot bind to.")]
        [SerializeField]
        private string[] _excludedPaths =
        {
            "<Mouse>/position",
            "<Mouse>/delta",
            "<Mouse>/scroll",
        };

        [Tooltip("What to do when the chosen control is already used by another binding.")]
        [SerializeField] private DuplicatePolicy _onDuplicate = DuplicatePolicy.Swap;

        [Tooltip("PlayerPrefs key the overrides are saved under. Leave empty to save nothing.")]
        [SerializeField] private string _saveKey = RebindStore.DefaultKey;

        [Space(15), Header("Events")]
        public UnityEvent m_started;
        public UnityEvent m_completed;
        public UnityEvent m_canceled;
        public UnityEvent m_rejected;

        private InputActionRebindingExtensions.RebindingOperation _operation;

        #endregion


        #region Public

        public InputAction Action => _action != null ? _action.action : null;

        public bool IsListening => _operation != null;

        #endregion


        #region Unity API

        private void Reset()
        {
            _button = GetComponent<Button>();
            _icon = GetComponentInChildren<InputPromptIcon>(includeInactive: true);
        }

        private void OnEnable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
                _button.onClick.AddListener(StartRebind);

            InputPromptService.PromptsChanged += RefreshLabel;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(StartRebind);

            InputPromptService.PromptsChanged -= RefreshLabel;
            Cancel();
        }

        #endregion


        #region Main API

        public void StartRebind()
        {
            var action = Action;
            if (action == null || IsListening)
                return;

            var bindingIndex = InputPromptService.ResolveBindingIndex(action, _compositePart);
            if (bindingIndex < 0)
                return;

            var previousPath = action.bindings[bindingIndex].effectivePath;
            var wasEnabled = action.enabled;
            action.Disable();

            SetListening(true);
            m_started?.Invoke();

            _operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough(_cancelPath)
                .OnCancel(operation => Finish(action, wasEnabled, canceled: true, bindingIndex, previousPath))
                .OnComplete(operation => Finish(action, wasEnabled, canceled: false, bindingIndex, previousPath));

            foreach (var path in _excludedPaths)
            {
                if (!string.IsNullOrEmpty(path))
                    _operation = _operation.WithControlsExcluding(path);
            }

            _operation.Start();
        }

        public void Cancel()
        {
            if (_operation == null)
                return;

            _operation.Cancel();
        }

        public void ResetBinding()
        {
            var action = Action;
            if (action == null)
                return;

            var bindingIndex = InputPromptService.ResolveBindingIndex(action, _compositePart);
            if (bindingIndex < 0)
                return;

            action.RemoveBindingOverride(bindingIndex);
            Persist();
            InputPromptService.Refresh();
        }

        #endregion


        #region Tools and Utilities

        private void Finish(InputAction action, bool wasEnabled, bool canceled, int bindingIndex,
            string previousPath)
        {
            _operation?.Dispose();
            _operation = null;

            if (!canceled)
                Resolve(action, bindingIndex, previousPath);

            if (wasEnabled)
                action.Enable();

            SetListening(false);
            InputPromptService.Refresh();

            if (canceled)
                m_canceled?.Invoke();
        }

        private void Resolve(InputAction action, int bindingIndex, string previousPath)
        {
            var conflict = RebindConflicts.Find(action, bindingIndex);

            if (!conflict.IsValid || _onDuplicate == DuplicatePolicy.Allow)
            {
                Persist();
                m_completed?.Invoke();
                return;
            }

            if (_onDuplicate == DuplicatePolicy.Reject)
            {
                action.ApplyBindingOverride(bindingIndex, previousPath);
                m_rejected?.Invoke();
                return;
            }

            RebindConflicts.Swap(conflict, previousPath);
            Persist();
            m_completed?.Invoke();
        }

        private void Persist()
        {
            var asset = Action?.actionMap?.asset;
            if (asset != null && !string.IsNullOrEmpty(_saveKey))
                RebindStore.Save(asset, _saveKey);
        }

        private void SetListening(bool isListening)
        {
            if (_icon != null)
                _icon.gameObject.SetActive(!isListening);

            if (_button != null)
                _button.interactable = !isListening;

            if (_label == null)
                return;

            _label.gameObject.SetActive(true);
            _label.text = isListening ? _listeningText : CurrentName();
        }

        private void RefreshLabel()
        {
            if (IsListening || _label == null)
                return;

            _label.text = CurrentName();
        }

        private string CurrentName() => InputPromptService.GetDisplayString(Action, _compositePart);

        #endregion
    }
}
