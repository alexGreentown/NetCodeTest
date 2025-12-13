using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetCodeTest.Gameplay.Player
{
    /// <summary>Ensures only the Owner can read input.</summary>
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerOwnershipGate : NetworkBehaviour
    {
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        public override void OnNetworkSpawn()
        {
            _playerInput.enabled = IsOwner; 
        }
    }
}