using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    public sealed class RemoteSmoothing : NetworkBehaviour
    {
        [SerializeField] private float _lerpSpeed = 12f;

        private Vector3 _target;

        public override void OnNetworkSpawn()
        {
            _target = transform.position;
        }

        public void SetTarget(Vector3 targetPos)
        {
            if (IsOwner) return;
            _target = targetPos;
        }

        private void LateUpdate()
        {
            if (IsOwner) return;

            transform.position = Vector3.Lerp(
                transform.position,
                _target,
                Time.deltaTime * _lerpSpeed
            );
        }
    }
}