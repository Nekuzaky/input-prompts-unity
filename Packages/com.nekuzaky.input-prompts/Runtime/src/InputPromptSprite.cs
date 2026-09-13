using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    [ExecuteAlways]
    [AddComponentMenu("Input Prompts/Input Prompt Sprite")]
    [RequireComponent(typeof(SpriteRenderer))]
    public class InputPromptSprite : MonoBehaviour
    {
        #region Private and Protected

        [Header("Action")]
        [SerializeField] private InputActionReference _action;

        [Tooltip("Part of a composite to show, e.g. \"up\". Leave empty for a plain binding.")]
        [SerializeField] private string _compositePart;

        [Tooltip("Player whose device drives this prompt. Leave empty to follow the last device used by anyone.")]
        [SerializeField] private InputPromptPlayer _player;

        [Space(15), Header("Display")]
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("Hide the renderer when the action has no binding for the current device.")]
        [SerializeField] private bool _hideWhenUnbound = true;

        [Tooltip("World height of the icon, in units. The width follows the sprite ratio. 0 leaves the scale alone.")]
        [SerializeField] private float _worldHeight;

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

        public float WorldHeight
        {
            get => _worldHeight;
            set
            {
                _worldHeight = Mathf.Max(0f, value);
                Refresh();
            }
        }

        public InputPromptContext Context => _player != null ? _player.Context : InputPromptService.Global;

        #endregion


        #region Unity API

        private void Reset() => _renderer = GetComponent<SpriteRenderer>();

        private void OnEnable()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();

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
            if (_renderer == null)
                return;

            var action = Action;
            var context = Context;
            var bindingIndex = action != null ? context.ResolveBindingIndex(action, _compositePart) : -1;

            var sprite = bindingIndex >= 0 ? context.GetSprite(action, bindingIndex) : null;
            if (sprite == null && bindingIndex >= 0)
                sprite = context.GetBlankSprite();

            if (sprite == null)
            {
                if (_hideWhenUnbound)
                    _renderer.enabled = false;
                return;
            }

            _renderer.enabled = true;
            _renderer.sprite = sprite;
            FitHeight(sprite);
        }

        #endregion


        #region Tools and Utilities

        private void FitHeight(Sprite sprite)
        {
            if (_worldHeight <= 0f)
                return;

            var height = sprite.bounds.size.y;
            if (height <= 0f)
                return;

            var scale = _worldHeight / height;
            transform.localScale = new Vector3(scale, scale, transform.localScale.z);
        }

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

        #endregion
    }
}
