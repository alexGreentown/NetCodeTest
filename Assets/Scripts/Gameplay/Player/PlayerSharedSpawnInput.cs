using NetCodeTest.Gameplay.NetworkObjects;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    [RequireComponent(typeof(PlayerInputSource))]
    public sealed class PlayerSharedSpawnInput : NetworkBehaviour
    {
        [SerializeField] private SharedObjectSpawner _sharedSpawner; 
        private PlayerInputSource _input;

        private void Awake()
        {
            _input = GetComponent<PlayerInputSource>();
            if (_sharedSpawner == null)
                _sharedSpawner = FindFirstObjectByType<SharedObjectSpawner>();
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (_input.ConsumeSpawnPressed())
            {
                Vector3 spawnPos = transform.position + transform.forward * 2f;
                RequestSpawnSharedObjectServerRpc(spawnPos);
            }
        }
        
        [Rpc(SendTo.Server)]
        private void RequestSpawnSharedObjectServerRpc(Vector3 position)
        {
            if (!IsServer) return;

            // найдём спавнер и поручим ему заспавнить
            var spawner = Object.FindFirstObjectByType<SharedObjectSpawner>();
            if (spawner != null)
                spawner.SpawnSharedObject(position);
        }
    }
}