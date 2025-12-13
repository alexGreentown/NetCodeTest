using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    public sealed class PlayerColorSync : NetworkBehaviour
    {
        [SerializeField] private Renderer _renderer;

        private readonly NetworkVariable<Color> _color =
            new NetworkVariable<Color>(Color.white, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            _color.OnValueChanged += OnColorChanged;

            // Apply immediately (important for late-join)
            Apply(_color.Value);

            // Give initial random color (server only)
            if (IsServer && _color.Value == Color.white)
                _color.Value = Random.ColorHSV();
        }

        public override void OnNetworkDespawn()
        {
            _color.OnValueChanged -= OnColorChanged;
        }

        private void OnColorChanged(Color oldValue, Color newValue) => Apply(newValue);

        private void Apply(Color c)
        {
            if (_renderer != null)
                _renderer.material.color = c;
        }

        [ServerRpc]
        public void SetColorServerRpc(Color newColor)
        {
            _color.Value = newColor;
        }
    }
}