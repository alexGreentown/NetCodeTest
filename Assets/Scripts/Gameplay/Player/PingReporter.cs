using NetCodeTest.Core.Contracts;
using NetCodeTest.Core.DI;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    public class PingReporter : NetworkBehaviour
    {
        private ILobbyService _lobby;
        [SerializeField] private float _sendInterval = 1.0f;
        private float _timer;
        
        private void Awake()
        {
            _lobby = ServiceContainer.Resolve<ILobbyService>();
        }
        
        private void Update()
        {
            if (!IsClient || !IsOwner) return;

            _timer += Time.deltaTime;
            if (_timer < _sendInterval) return;
            _timer = 0f;

            if (NetworkManager.Singleton?.NetworkConfig?.NetworkTransport == null)
                return;

            ulong rtt =
                NetworkManager.Singleton.NetworkConfig.NetworkTransport
                    .GetCurrentRtt(NetworkManager.Singleton.LocalClientId);

            ushort pingMs = (ushort)Mathf.Clamp(rtt, 0, ushort.MaxValue);

            SendPingServerRpc(pingMs);
        }

        [ServerRpc]
        private void SendPingServerRpc(ushort pingMs, ServerRpcParams rpcParams = default)
        {
            _lobby?.UpdatePing(rpcParams.Receive.SenderClientId, pingMs);
        }
    }
}