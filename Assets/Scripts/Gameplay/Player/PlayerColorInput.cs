using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    [RequireComponent(typeof(PlayerInputSource))]
    [RequireComponent(typeof(PlayerColorSync))]
    public sealed class PlayerColorInput : NetworkBehaviour
    {
        private PlayerInputSource _input;
        private PlayerColorSync _color;

        private void Awake()
        {
            _input = GetComponent<PlayerInputSource>();
            _color = GetComponent<PlayerColorSync>();
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (_input.ConsumeColorPressed())
                _color.SetColorServerRpc(Random.ColorHSV());
        }
    }
}