using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Nekuzaky.InputPrompts
{
    [UxmlElement]
    public partial class InputPromptElement : Image
    {
        #region Public

        public const string UssClassName = "input-prompt";

        [UxmlAttribute]
        public InputActionReference ActionReference
        {
            get => _reference;
            set
            {
                _reference = value;
                Refresh();
            }
        }

        [UxmlAttribute]
        public string CompositePart
        {
            get => _compositePart;
            set
            {
                _compositePart = value;
                Refresh();
            }
        }

        [UxmlAttribute]
        public bool HideWhenUnbound
        {
            get => _hideWhenUnbound;
            set
            {
                _hideWhenUnbound = value;
                Refresh();
            }
        }

        public InputAction Action
        {
            get => _runtimeAction ?? (_reference != null ? _reference.action : null);
            set
            {
                _runtimeAction = value;
                Refresh();
            }
        }

        public InputPromptContext Context
        {
            get => _context ?? InputPromptService.Global;
            set
            {
                _context = value;
                if (panel == null)
                    return;

                Subscribe();
                Refresh();
            }
        }

        #endregion


        #region Private and Protected

        private InputActionReference _reference;
        private InputAction _runtimeAction;
        private string _compositePart;
        private bool _hideWhenUnbound = true;
        private InputPromptContext _context;
        private InputPromptContext _subscribed;

        #endregion


        #region Main API

        public InputPromptElement()
        {
            AddToClassList(UssClassName);
            scaleMode = ScaleMode.ScaleToFit;

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void Refresh()
        {
            var action = Action;
            var context = Context;
            var bindingIndex = action != null ? context.ResolveBindingIndex(action, _compositePart) : -1;

            var icon = bindingIndex >= 0 ? context.GetSprite(action, bindingIndex) : null;
            if (icon == null && bindingIndex >= 0)
                icon = context.GetBlankSprite();

            sprite = icon;
            visible = icon != null || !_hideWhenUnbound;
        }

        #endregion


        #region Tools and Utilities

        private void OnAttach(AttachToPanelEvent evt)
        {
            InputPromptService.Initialize();
            Subscribe();
            Refresh();
        }

        private void OnDetach(DetachFromPanelEvent evt) => Unsubscribe();

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
