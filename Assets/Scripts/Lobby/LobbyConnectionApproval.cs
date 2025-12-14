using Unity.Netcode;
using UnityEngine;
using System.Text;

namespace NetCodeTest.Lobby
{
    public static class LobbyConnectionApproval
    {
        public static void Install()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback =
                (request, response) =>
                {
                    var lobby = LobbyManager.Instance;
                    if (lobby == null || !lobby.IsSpawned)
                    {
                        Deny(response, LobbyErrorCode.LobbyClosed);
                        return;
                    }

                    var userId = Encoding.UTF8.GetString(request.Payload);

                    if (!lobby.TryAddPlayer(request.ClientNetworkId, userId, out var error))
                    {
                        Deny(response, error);
                        return;
                    }

                    response.Approved = true;
                    response.CreatePlayerObject = true;

                    Debug.Log($"[Lobby][Server] Approved {request.ClientNetworkId}");
                };
        }

        private static void Deny(
            NetworkManager.ConnectionApprovalResponse response,
            LobbyErrorCode code)
        {
            response.Approved = false;
            response.Reason = ((int)code).ToString();
            response.CreatePlayerObject = false;
        }
        
    }
}