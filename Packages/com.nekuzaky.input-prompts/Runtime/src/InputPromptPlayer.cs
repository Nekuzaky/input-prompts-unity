using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    [AddComponentMenu("Input Prompts/Input Prompt Player")]
    [RequireComponent(typeof(PlayerInput))]
    public class InputPromptPlayer : MonoBehaviour
    {
        #region Private and Protected

        private PlayerInput _playerInput;
        private InputPromptContext _context;

        private PlayerInput PlayerInput => _playerInput != null ? _playerInput : _playerInput = GetComponent<PlayerInput>();

        #endregion


        #region Public

        public InputPromptContext Context => _context ??= new InputPromptContext(Owns);

        #endregion


        #region Unity API

        private void OnEnable()
        {
            InputPromptService.Initialize();
            InputPromptService.Register(Context);

            PlayerInput.onControlsChanged += OnControlsChanged;
            SyncDevice();
        }

        private void OnDisable()
        {
            if (_playerInput != null)
                _playerInput.onControlsChanged -= OnControlsChanged;

            if (_context != null)
                InputPromptService.Unregister(_context);
        }

        #endregion


        #region Tools and Utilities

        private bool Owns(InputDevice device)
        {
            var devices = PlayerInput.devices;
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i] == device)
                    return true;
            }

            return false;
        }

        private void OnControlsChanged(PlayerInput input) => SyncDevice();

        private void SyncDevice()
        {
            var devices = PlayerInput.devices;
            if (devices.Count > 0 && !Owns(Context.ActiveDevice))
                Context.SetActiveDevice(devices[0]);

            Context.Refresh();
        }

        #endregion
    }
}
