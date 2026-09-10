using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nekuz.InputPrompts
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
        [Header("Action")]
        [SerializeField] private InputActionReference action;

        [Tooltip("Part of a composite to show, e.g. \"up\" for the W of a WASD composite. Leave empty for plain bindings.")]
        [SerializeField] private string compositePart;

        [Header("Display")]
        [SerializeField] private Image targetImage;

        [Tooltip("Optional. Shows the control name (\"F13\") on the blank key cap when no icon exists for it.")]
        [SerializeField] private TMP_Text fallbackLabel;

        [Tooltip("Hide the whole object when the action has no binding for the current device.")]
        [SerializeField] private bool hideWhenUnbound = true;

        [Tooltip("Stretch the RectTransform width to the sprite ratio, so wide caps like Space stay undistorted.")]
        [SerializeField] private bool resizeToSpriteAspect = true;

        private InputAction _runtimeAction;

        /// <summary>Action currently displayed. Setting it refreshes the icon.</summary>
        public InputAction Action
        {
            get => _runtimeAction ?? action?.action;
            set
            {
                _runtimeAction = value;
                Refresh();
            }
        }

        /// <summary>Composite part shown, e.g. "up". Setting it refreshes the icon.</summary>
        public string CompositePart
        {
            get => compositePart;
            set
            {
                compositePart = value;
                Refresh();
            }
        }

        /// <summary>Human readable name of the bound control, handy for building sentences next to the icon.</summary>
        public string DisplayString => InputPromptService.GetDisplayString(Action, compositePart);

        private void Reset()
        {
            targetImage = GetComponent<Image>();
            fallbackLabel = GetComponentInChildren<TMP_Text>(true);
        }

        private void OnEnable()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            InputPromptService.Initialize();
            InputPromptService.PromptsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            InputPromptService.PromptsChanged -= Refresh;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isActiveAndEnabled)
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        Refresh();
                };
        }
#endif

        /// <summary>Re-resolve the binding and repaint. Called automatically on device change and rebind.</summary>
        public void Refresh()
        {
            if (targetImage == null)
                return;

            var currentAction = Action;
            if (currentAction == null)
            {
                Show(false);
                return;
            }

            var bindingIndex = InputPromptService.ResolveBindingIndex(currentAction, compositePart);
            if (bindingIndex < 0)
            {
                Show(false);
                return;
            }

            var sprite = InputPromptService.GetSprite(currentAction, bindingIndex);
            var usesBlank = sprite == null;
            if (usesBlank)
                sprite = InputPromptService.GetBlankSprite();

            if (sprite == null)
            {
                Show(false);
                return;
            }

            Show(true);
            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;

            if (fallbackLabel != null)
            {
                fallbackLabel.gameObject.SetActive(usesBlank);
                if (usesBlank)
                    fallbackLabel.text = currentAction.GetBindingDisplayString(
                        bindingIndex, out _, out _,
                        InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
                        InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }

            if (resizeToSpriteAspect)
                ApplyAspect(sprite);
        }

        private void ApplyAspect(Sprite sprite)
        {
            var rect = sprite.rect;
            if (rect.height <= 0f)
                return;

            var rectTransform = (RectTransform)transform;

            // Only meaningful when the height is driven by the layout, not by a stretched anchor.
            if (Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y))
            {
                var size = rectTransform.rect.size;
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.y * (rect.width / rect.height));
            }
        }

        private void Show(bool visible)
        {
            if (hideWhenUnbound)
            {
                if (targetImage.enabled != visible)
                    targetImage.enabled = visible;
                if (fallbackLabel != null && !visible)
                    fallbackLabel.gameObject.SetActive(false);
            }
            else if (!targetImage.enabled)
            {
                targetImage.enabled = true;
            }
        }
    }
}
