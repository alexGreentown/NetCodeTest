using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    [RequireComponent(typeof(PlayerInputSource))]
    public sealed class OwnerAuthMovement : NetworkBehaviour
    {
        [SerializeField] private float _speed = 5f;
        private PlayerInputSource _input;
        private bool _gameplayEnabled;

        private void Awake()
        {
            _input = GetComponent<PlayerInputSource>();
        }

        public void SetGameplayEnabled(bool enabledGameplay)
        {
            _gameplayEnabled = enabledGameplay;
        }

        private void Update()
        {
            if (!IsOwner || !_gameplayEnabled) return;

            Vector2 m = _input.Move;
            Vector3 move = new Vector3(m.x, 0, m.y);
            transform.Translate(move * _speed * Time.deltaTime, Space.World);
            Debug.Log($"Owner {m}");
        }
    }
}