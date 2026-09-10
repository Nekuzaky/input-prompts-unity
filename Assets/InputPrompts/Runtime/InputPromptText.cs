using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuz.InputPrompts
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
        [SerializeField] private TMP_Text target;

        [Tooltip("Action asset the {Map/Action} tokens are resolved against.")]
        [SerializeField] private InputActionAsset actions;

        [Tooltip("Sentence to display. Tokens look like {Player/Jump} or {Jump}.")]
        [TextArea]
        [SerializeField] private string format = "Press {Player/Jump} to jump";

        private readonly StringBuilder _builder = new();

        /// <summary>Sentence template. Setting it repaints the text.</summary>
        public string Format
        {
            get => format;
            set
            {
                format = value;
                Refresh();
            }
        }

        private void Reset() => target = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            if (target == null)
                target = GetComponent<TMP_Text>();

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

        /// <summary>Re-resolve every token and repaint.</summary>
        public void Refresh()
        {
            if (target == null || string.IsNullOrEmpty(format))
                return;

            _builder.Clear();

            for (var i = 0; i < format.Length; i++)
            {
                if (format[i] != '{')
                {
                    _builder.Append(format[i]);
                    continue;
                }

                var end = format.IndexOf('}', i + 1);
                if (end < 0)
                {
                    _builder.Append(format, i, format.Length - i);
                    break;
                }

                var token = format.Substring(i + 1, end - i - 1);
                _builder.Append(Resolve(token));
                i = end;
            }

            target.text = _builder.ToString();
        }

        private string Resolve(string token)
        {
            if (actions == null || string.IsNullOrEmpty(token))
                return token;

            var part = (string)null;
            var separator = token.IndexOf('#');
            if (separator >= 0)
            {
                part = token[(separator + 1)..];
                token = token[..separator];
            }

            var found = actions.FindAction(token, false);
            if (found == null)
                return token;

            var display = InputPromptService.GetDisplayString(found, part);
            return string.IsNullOrEmpty(display) ? token : display;
        }
    }
}
