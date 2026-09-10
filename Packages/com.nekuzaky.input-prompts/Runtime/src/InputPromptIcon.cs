using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InputPrompts
{
    /// <summary>
    /// Shows the icon of the control an action is bound to on the device the player is currently using.
    /// Put it on a UI Image; it swaps the sprite by itself when the player switches device or rebinds.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Input Prompts/Input Prompt Icon")]
    [RequireComponent(typeof(Image))]
    public class InputPromptIcon : MonoBehaviour
    {
        #region Private and Protected

        [Header("Action")]
        [SerializeField] private InputActionReference _action;

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

        #endregion


        #region Public

        /// <summary>Action currently displayed. Setting it refreshes the icon.</summary>
        public InputAction Action
        {
            get => _runtimeAction ?? _action?.action;
            set
            {
                _runtimeAction = value;
                Refresh();
            }
        }

        /// <summary>Composite part shown, e.g. "up". Setting it refreshes the icon.</summary>
        public string CompositePart
        {
            get => _compositePart;
            set
            {
                _compositePart = value;
                Refresh();
            }
        }

        /// <summary>Human readable name of the bound control, handy for building sentences next to the icon.</summary>
        public string DisplayString => InputPromptService.GetDisplayString(Action, _compositePart);

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
            InputPromptService.PromptsChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => InputPromptService.PromptsChanged -= Refresh;

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

        /// <summary>Re-resolve the binding and repaint. Called automatically on device change and rebind.</summary>
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

            var bindingIndex = InputPromptService.ResolveBindingIndex(action, _compositePart);
            if (bindingIndex < 0)
            {
                Show(isVisible: false);
                return;
            }

            var sprite = InputPromptService.GetSprite(action, bindingIndex);
            var usesBlank = sprite == null;
            if (usesBlank)
                sprite = InputPromptService.GetBlankSprite();

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

        private void UpdateFallbackLabel(InputAction action, int bindingIndex, bool usesBlank)
        {
            if (_fallbackLabel == null)
                return;

            _fallbackLabel.gameObject.SetActive(usesBlank);
            if (usesBlank)
                _fallbackLabel.text = InputPromptService.GetDisplayString(action, bindingIndex);
        }

        /// <summary>Keeps wide icons (Space, Enter) from being squeezed into a square slot.</summary>
        private void ApplyAspect(Sprite sprite)
        {
            var rect = sprite.rect;
            if (rect.height <= 0f)
                return;

            var rectTransform = (RectTransform)transform;

            // Only meaningful when the height is driven by the layout, not by a stretched anchor.
            if (!Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y))
                return;

            var size = rectTransform.rect.size;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.y * (rect.width / rect.height));
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
