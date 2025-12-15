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


#region Spawn Object     
        private bool _spawnObjectPressed;
        private bool _grabObjectPressed;
        private bool _dropObjectPressed;
        private bool _throwObjectPressed;
        private bool _deleteObjectPressed;

        // PlayerInput (Invoke Unity Events) Spawn Object (bind key F)
        public void OnSpawnObject(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _spawnObjectPressed = true;
        }

        public bool ConsumeSpawnPressed()
        {
            if (!_spawnObjectPressed) return false;
            _spawnObjectPressed = false;
            return true;
        }         
        
        // PlayerInput (Invoke Unity Events) Grab Object (bind key E)
        public void OnGrabObject(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _grabObjectPressed = true;
        }
        
        public bool ConsumeGrabPressed()
        {
            if (!_grabObjectPressed) return false;
            _grabObjectPressed = false;
            return true;
        }      
        
        // PlayerInput (Invoke Unity Events) Throw Object (bind key T)
        public void OnThrowObject(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _throwObjectPressed = true;
        }
        
        public bool ConsumeThrowPressed()
        {
            if (!_throwObjectPressed) return false;
            _throwObjectPressed = false;
            return true;
        }
        
        // PlayerInput (Invoke Unity Events) Delete Object (bind key V)
        public void OnDeleteObject(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _deleteObjectPressed = true;
        }
        
        public bool ConsumeDeletePressed()
        {
            if (!_deleteObjectPressed) return false;
            _deleteObjectPressed = false;
            return true;
        }
#endregion   

        
    }
}
