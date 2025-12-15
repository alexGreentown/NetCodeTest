using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.NetworkObjects
{
    public class SharedObjectSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject sharedPrefab;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SpawnSharedObjectServerRpc(Vector3 position)
        {
            if (!IsServer) return;

            var obj = Instantiate(sharedPrefab, position, Quaternion.identity);
            obj.Spawn(destroyWithScene: true);

            Debug.Log("[Server] Shared object spawned");
        }
    }
}