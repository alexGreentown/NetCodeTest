using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    public enum MovementMode
    {
        OwnerAuthoritative,
        ServerAuthoritative
    }

    public sealed class PlayerMovementMode : NetworkBehaviour
    {
        [SerializeField] private OwnerAuthMovement _ownerAuth;
        [SerializeField] private ServerAuthMovement _serverAuth;
        [SerializeField] private NetworkTransform _networkTransform;

        private readonly NetworkVariable<MovementMode> _netMode =
            new NetworkVariable<MovementMode>(
                MovementMode.OwnerAuthoritative,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        // public PlayerMovementMode CurrentMode;
        
        [SerializeField] private MovementMode _runtimeModeView; // read-only view
        public MovementMode CurrentMode => _runtimeModeView;
        
        private void Awake()
        {
            if (_ownerAuth == null) _ownerAuth = GetComponent<OwnerAuthMovement>();
            if (_serverAuth == null) _serverAuth = GetComponent<ServerAuthMovement>();
            if (_networkTransform == null) _networkTransform = GetComponent<NetworkTransform>();
        }

        public override void OnNetworkSpawn()
        {
            _netMode.OnValueChanged += (_, __) => ApplyMode();

            if (IsServer)
            {
                var detected = DetectFromNetworkTransform();
                _netMode.Value = detected;
                Debug.Log($"[MovementMode] Server set mode = {detected}");
            }

            ApplyMode();
        }

        private MovementMode DetectFromNetworkTransform()
        {
            if (_networkTransform == null)
                return MovementMode.OwnerAuthoritative;

            return _networkTransform.AuthorityMode switch
            {
                NetworkTransform.AuthorityModes.Owner  => MovementMode.OwnerAuthoritative,
                NetworkTransform.AuthorityModes.Server => MovementMode.ServerAuthoritative,
                _ => MovementMode.OwnerAuthoritative
            };
        }

        public void ApplyMode()
        {
            _runtimeModeView = _netMode.Value;

            if (CurrentMode == MovementMode.ServerAuthoritative)
            {
                if (_networkTransform != null && IsOwner && !IsServer)
                    _networkTransform.enabled = false;

                _serverAuth.enabled = true;
                _ownerAuth.enabled = false;
            }
            else
            {
                if (_networkTransform != null)
                    _networkTransform.enabled = true;

                _serverAuth.enabled = false;
                _ownerAuth.enabled = true;
            }
        }

        public void SetGameplayEnabled(bool enabled)
        {
            Debug.Log($"SetGameplayEnabled2 enabled={enabled}");
            if (_ownerAuth != null) _ownerAuth.SetGameplayEnabled(enabled);
            if (_serverAuth != null) _serverAuth.SetGameplayEnabled(enabled);
        }
    }
}
