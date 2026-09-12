using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nekuzaky.InputPrompts
{
    [ExecuteAlways]
    [AddComponentMenu("Input Prompts/Input Prompt Icon")]
    [RequireComponent(typeof(Image))]
    public class InputPromptIcon : MonoBehaviour
    {
        #region Private and Protected

        [Header("Action")]
        [SerializeField] private InputActionReference _action;

        [Tooltip("Player whose device drives this prompt. Leave empty to follow the last device used by anyone.")]
        [SerializeField] private InputPromptPlayer _player;

        [Tooltip("Part of a composite to show, e.g. \"up\" for the W of a WASD composite. Leave empty for plain bindings.")]
        [SerializeField] private string _compositePart;

        [Space(15), Header("Display")]
        [SerializeField] private Image _targetImage;

        [Tooltip("Optional. Shows the control name (\"F13\") on the blank key cap when no icon exists for it.")]
        [SerializeField] private TMP_Text _fallbackLabel;

        [Tooltip("Hide the image when the action has no binding for the current device.")]
        [SerializeField] private bool _hideWhenUnbound = true;

        [Tooltip("Stretch the RectTransform width to the sprite ratio, so wide caps like Space stay undistorted.")]
        [SerializeField] private bool _resizeToSpriteAspect = true;

        private InputAction _runtimeAction;
        private InputPromptContext _subscribed;

        #endregion


        #region Public

        public InputAction Action
        {
            get => _runtimeAction ?? _action?.action;
            set
            {
                _runtimeAction = value;
                Refresh();
            }
        }

        public string CompositePart
        {
            get => _compositePart;
            set
            {
                _compositePart = value;
                Refresh();
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
                Refresh();
            }
        }

        public InputPromptContext Context => _player != null ? _player.Context : InputPromptService.Global;

        public string DisplayString => Context.GetDisplayString(Action, _compositePart);

        #endregion


        #region Unity API

        private void Reset()
        {
            _targetImage = GetComponent<Image>();
            _fallbackLabel = GetComponentInChildren<TMP_Text>(includeInactive: true);
        }

        private void OnEnable()
        {
            if (_targetImage == null)
                _targetImage = GetComponent<Image>();

            InputPromptService.Initialize();
            Subscribe();
            Refresh();
        }

        private void OnDisable() => Unsubscribe();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    Refresh();
            };
        }
#endif

        #endregion


        #region Main API

        public void Refresh()
        {
            if (_targetImage == null)
                return;

            var action = Action;
            if (action == null)
            {
                Show(isVisible: false);
                return;
            }

            var context = Context;
            var bindingIndex = context.ResolveBindingIndex(action, _compositePart);
            if (bindingIndex < 0)
            {
                Show(isVisible: false);
                return;
            }

            var sprite = context.GetSprite(action, bindingIndex);
            var usesBlank = sprite == null;
            if (usesBlank)
                sprite = context.GetBlankSprite();

            if (sprite == null)
            {
                Show(isVisible: false);
                return;
            }

            Show(isVisible: true);
            _targetImage.sprite = sprite;
            _targetImage.preserveAspect = true;

            UpdateFallbackLabel(action, bindingIndex, usesBlank);

            if (_resizeToSpriteAspect)
                ApplyAspect(sprite);
        }

        #endregion


        #region Tools and Utilities

        private void Subscribe()
        {
            Unsubscribe();
            _subscribed = Context;
            _subscribed.PromptsChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (_subscribed != null)
                _subscribed.PromptsChanged -= Refresh;

            _subscribed = null;
        }

        private void UpdateFallbackLabel(InputAction action, int bindingIndex, bool usesBlank)
        {
            if (_fallbackLabel == null)
                return;

            _fallbackLabel.gameObject.SetActive(usesBlank);
            if (usesBlank)
                _fallbackLabel.text = Context.GetDisplayString(action, bindingIndex);
        }

        private void ApplyAspect(Sprite sprite)
        {
            var rect = sprite.rect;
            if (rect.height <= 0f)
                return;

            var rectTransform = (RectTransform)transform;

            if (!Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y))
                return;

            var height = rectTransform.rect.height;
            if (height <= 0f)
                return;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, height * (rect.width / rect.height));
        }

        private void Show(bool isVisible)
        {
            if (!_hideWhenUnbound)
            {
                _targetImage.enabled = true;
                return;
            }

            _targetImage.enabled = isVisible;
            if (_fallbackLabel != null && !isVisible)
                _fallbackLabel.gameObject.SetActive(false);
        }

        #endregion
    }
}
