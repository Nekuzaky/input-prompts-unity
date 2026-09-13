using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts.Samples
{
    public class SampleBootstrap : MonoBehaviour
    {
        #region Private and Protected

        [Tooltip("Action asset of the sample. Saved bindings are loaded into it on start.")]
        [SerializeField] private InputActionAsset _actions;

        #endregion


        #region Unity API

        private void OnEnable()
        {
            if (_actions == null)
                return;

            RebindStore.Load(_actions);
            _actions.Enable();
        }

        private void OnDisable()
        {
            if (_actions != null)
                _actions.Disable();
        }

        #endregion


        #region Main API

        public void ResetBindings() => RebindStore.Clear(_actions);

        #endregion
    }
}
