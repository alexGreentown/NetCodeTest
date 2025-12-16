using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    [RequireComponent(typeof(PlayerInputSource))]
    public sealed class PlayerSharedObjectInteractor : NetworkBehaviour
    {
        [Header("Grab")]
        [SerializeField] private float _grabDistance = 3.0f;
        [SerializeField] private LayerMask _grabbableMask = ~0;

        [Header("Throw")]
        [SerializeField] private float _throwImpulse = 10f;

        private PlayerInputSource _input;
        private NetworkObjectReference _heldRef;
        
        private readonly NetworkVariable<ulong> _heldObjectId =
            new(0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private bool IsHolding => _heldObjectId.Value != 0;
        private readonly Collider[] _nearby = new Collider[64];
        
        private void Awake()
        {
            _input = GetComponent<PlayerInputSource>();
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (_input.ConsumeGrabPressed())
            {
                if (IsHolding)
                {
                    RequestDropServerRpc();      // E = drop
                }
                else if (TryFindTarget(out ulong targetId))
                {
                    RequestGrabServerRpc(targetId); // E = grab
                }
            }

            if (_input.ConsumeThrowPressed())  RequestThrowServerRpc();
            if (_input.ConsumeDeletePressed()) RequestDeleteServerRpc();
        }

        public override void OnNetworkSpawn() => enabled = IsOwner;
        
        public override void OnNetworkDespawn()
        {
            // if player exited and the object is hold, then release
            if (!IsServer) return;
            if (_heldRef.TryGet(out var networkObject))
            {
                var shared = networkObject.GetComponent<SharedPhysicsObject>();
                if (shared != null) shared.DropServer(OwnerClientId);
            }
            
            _heldRef = default;
            _heldObjectId.Value = 0;
        }
        
        private bool TryFindTarget(out ulong objectId)
        {
            objectId = 0;

            Vector3 center = transform.position; // просто расстояние до игрока
            int count = Physics.OverlapSphereNonAlloc(
                center,
                _grabDistance,
                _nearby,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            if (count <= 0)
                return false;

            float bestSqr = float.MaxValue;
            SharedPhysicsObject best = null;

            for (int i = 0; i < count; i++)
            {
                var col = _nearby[i];
                if (col == null) continue;

                var shared = col.GetComponentInParent<SharedPhysicsObject>();
                if (shared == null) continue;

                if (shared.IsHeld) continue;

                if (shared.NetworkObject == null || !shared.NetworkObject.IsSpawned) continue;

                float sqr = (shared.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = shared;
                }
            }

            if (best == null)
                return false;

            objectId = best.NetworkObject.NetworkObjectId;
            return true;
        }


        [Rpc(SendTo.Server)]
        private void RequestGrabServerRpc(ulong objectId)
        {
            if (!IsServer) return;
            if (_heldRef.TryGet(out _)) return; // already holding object

            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var networkObject))
                return;

            var shared = networkObject.GetComponent<SharedPhysicsObject>();
            if (shared == null) return;

            // simple validation by distance
            if ((networkObject.transform.position - transform.position).sqrMagnitude > _grabDistance * _grabDistance)
                return;

            if (shared.TryGrabServer(OwnerClientId))
            {
                _heldRef = new NetworkObjectReference(networkObject);
                _heldObjectId.Value = networkObject.NetworkObjectId;
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestDropServerRpc()
        {
            if (!IsServer) return;
            if (!_heldRef.TryGet(out var nob)) return;

            var shared = nob.GetComponent<SharedPhysicsObject>();
            if (shared != null && shared.DropServer(OwnerClientId))
            {
                _heldRef = default;
                _heldObjectId.Value = 0;
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestThrowServerRpc()
        {
            if (!IsServer) return;
            if (!_heldRef.TryGet(out var nob)) return;

            var shared = nob.GetComponent<SharedPhysicsObject>();
            if (shared != null && shared.ThrowServer(OwnerClientId, transform.forward, _throwImpulse))
            {
                _heldRef = default;
                _heldObjectId.Value = 0;
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestDeleteServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong sender = rpcParams.Receive.SenderClientId;

            if (!_heldRef.TryGet(out var nob)) return;

            var shared = nob.GetComponent<SharedPhysicsObject>();
            if (shared != null && shared.DeleteServer(sender))
            {
                _heldRef = default;
                _heldObjectId.Value = 0;
            }
        }
        
        
    }
}
