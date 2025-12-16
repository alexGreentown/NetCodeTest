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
        [SerializeField] private float _turnSpeedDeg = 720f; // градусов/сек

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
            
            if (move.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(move.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    target,
                    _turnSpeedDeg * Time.deltaTime
                );
            }
        }
    }
}