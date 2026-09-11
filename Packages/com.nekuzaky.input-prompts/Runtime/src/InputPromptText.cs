using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    /// <summary>
    /// Writes a sentence with control names substituted in, e.g. "Press {Player/Jump} to jump"
    /// becomes "Press Space to jump" or "Press A to jump" depending on the device in use.
    /// Use it next to an <see cref="InputPromptIcon"/> when an icon alone is not enough.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Input Prompts/Input Prompt Text")]
    [RequireComponent(typeof(TMP_Text))]
    public class InputPromptText : MonoBehaviour
    {
        #region Private and Protected

        [Header("Source")]
        [SerializeField] private TMP_Text _target;

        [Tooltip("Action asset the {Map/Action} tokens are resolved against.")]
        [SerializeField] private InputActionAsset _actions;

        [Space(15), Header("Content")]
        [Tooltip("Sentence to display. Tokens look like {Player/Jump}, or {Player/Move#up} for a composite part.")]
        [TextArea]
        [SerializeField] private string _format = "Press {Player/Jump} to jump";

        private readonly StringBuilder _builder = new();

        #endregion


        #region Public

        /// <summary>Sentence template. Setting it repaints the text.</summary>
        public string Format
        {
            get => _format;
            set
            {
                _format = value;
                Refresh();
            }
        }

        #endregion


        #region Unity API

        private void Reset() => _target = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            if (_target == null)
                _target = GetComponent<TMP_Text>();

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

        /// <summary>Re-resolve every token and repaint.</summary>
        public void Refresh()
        {
            if (_target == null || string.IsNullOrEmpty(_format))
                return;

            _builder.Clear();

            for (var i = 0; i < _format.Length; i++)
            {
                if (_format[i] != '{')
                {
                    _builder.Append(_format[i]);
                    continue;
                }

                var end = _format.IndexOf('}', i + 1);
                if (end < 0)
                {
                    _builder.Append(_format, i, _format.Length - i);
                    break;
                }

                _builder.Append(Resolve(_format.Substring(i + 1, end - i - 1)));
                i = end;
            }

            _target.text = _builder.ToString();
        }

        #endregion


        #region Tools and Utilities

        /// <summary>Turns "Player/Move#up" into the control name bound to that action on this device.</summary>
        private string Resolve(string token)
        {
            if (_actions == null || string.IsNullOrEmpty(token))
                return token;

            var part = (string)null;
            var separator = token.IndexOf('#');
            if (separator >= 0)
            {
                part = token[(separator + 1)..];
                token = token[..separator];
            }

            var action = _actions.FindAction(token, throwIfNotFound: false);
            if (action == null)
                return token;

            var display = InputPromptService.GetDisplayString(action, part);
            return string.IsNullOrEmpty(display) ? token : display;
        }

        #endregion
    }
}
