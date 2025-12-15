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

        [Header("Refs")]
        [SerializeField] private Camera _camera; // можно оставить null -> возьмём Camera.main

        private PlayerInputSource _input;
        private NetworkObjectReference _heldRef;

        private void Awake()
        {
            _input = GetComponent<PlayerInputSource>();
            if (_camera == null) _camera = Camera.main;
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (_input.ConsumeGrabPressed())
            {
                if (TryFindTarget(out var objId))
                    RequestGrabServerRpc(objId);
            }

            if (_input.ConsumeDropPressed())   RequestDropServerRpc();
            if (_input.ConsumeThrowPressed())  RequestThrowServerRpc();
            if (_input.ConsumeDeletePressed()) RequestDeleteServerRpc();
        }

        private bool TryFindTarget(out ulong objectId)
        {
            objectId = 0;

            var cam = _camera != null ? _camera : Camera.main;
            Vector3 origin = cam != null ? cam.transform.position : (transform.position + Vector3.up);
            Vector3 dir    = cam != null ? cam.transform.forward  : transform.forward;

            if (!Physics.Raycast(origin, dir, out var hit, _grabDistance, _grabbableMask))
                return false;

            var nob = hit.collider.GetComponentInParent<NetworkObject>();
            if (nob == null) return false;

            objectId = nob.NetworkObjectId;
            return true;
        }

        [Rpc(SendTo.Server)]
        private void RequestGrabServerRpc(ulong objectId)
        {
            if (!IsServer) return;
            if (_heldRef.TryGet(out _)) return; // already holding object

            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var nob))
                return;

            var shared = nob.GetComponent<SharedPhysicsObject>();
            if (shared == null) return;

            // simple validation by distance
            if ((nob.transform.position - transform.position).sqrMagnitude > _grabDistance * _grabDistance)
                return;

            if (shared.TryGrabServer(OwnerClientId))
                _heldRef = new NetworkObjectReference(nob);
        }

        [Rpc(SendTo.Server)]
        private void RequestDropServerRpc()
        {
            if (!IsServer) return;
            if (!_heldRef.TryGet(out var nob)) return;

            var shared = nob.GetComponent<SharedPhysicsObject>();
            if (shared != null && shared.DropServer(OwnerClientId))
                _heldRef = default;
        }

        [Rpc(SendTo.Server)]
        private void RequestThrowServerRpc()
        {
            if (!IsServer) return;
            if (!_heldRef.TryGet(out var nob)) return;

            var shared = nob.GetComponent<SharedPhysicsObject>();
            if (shared != null && shared.ThrowServer(OwnerClientId, transform.forward, _throwImpulse))
                _heldRef = default;
        }

        [Rpc(SendTo.Server)]
        private void RequestDeleteServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong sender = rpcParams.Receive.SenderClientId;

            if (!_heldRef.TryGet(out var nob)) return;

            var shared = nob.GetComponent<SharedPhysicsObject>();
            if (shared != null && shared.DeleteServer(sender))
                _heldRef = default;
        }
        
        public override void OnNetworkDespawn()
        {
            // if player exited and the object is hold, then release
            if (!IsServer) return;
            if (_heldRef.TryGet(out var nob))
            {
                var shared = nob.GetComponent<SharedPhysicsObject>();
                if (shared != null) shared.DropServer(OwnerClientId);
            }
        }
    }
}
