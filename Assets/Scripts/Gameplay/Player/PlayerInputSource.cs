using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetCodeTest.Gameplay.Player
{
    /// <summary>
    /// Reads New Input System events and stores current input state.
    /// </summary>
    public sealed class PlayerInputSource : NetworkBehaviour
    {
        public Vector2 Move { get; private set; }

        private bool _colorPressed;
        
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }
        
        // PlayerInput (Invoke Unity Events) Move
        public void OnMove(InputAction.CallbackContext ctx)
        {
            Move = ctx.ReadValue<Vector2>();
        }

        // PlayerInput (Invoke Unity Events) Color (bind key C)
        public void OnColor(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _colorPressed = true;
        }

        public bool ConsumeColorPressed()
        {
            if (!_colorPressed) return false;
            _colorPressed = false;
            return true;
        }
        
        public override void OnNetworkSpawn()
        {
            _playerInput.enabled = IsOwner;
        }
        
    }
}
