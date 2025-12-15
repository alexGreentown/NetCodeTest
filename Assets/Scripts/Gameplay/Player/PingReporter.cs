using NetCodeTest.Lobby;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    public class PingReporter : NetworkBehaviour
    {
        [SerializeField] private float _sendInterval = 1.0f;
        private float _timer;

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
            LobbyManager.Instance?.UpdatePing(rpcParams.Receive.SenderClientId, pingMs);
        }
    }
}