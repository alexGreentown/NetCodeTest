using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    /// <summary>
    /// Handles connection approval ONLY.
    /// Does not add players to the lobby.
    /// Player addition is done in LobbyManager.OnClientConnected.
    /// </summary>
    public static class LobbyConnectionApproval
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
                return;

            _installed = true;

            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCallback;
            Debug.Log("[Lobby][Approval] Installed");
        }

        private static void ApprovalCallback(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            ulong clientId = request.ClientNetworkId;

            Debug.Log($"[Lobby][Approval] Connection request clientId={clientId}");
            
            // Host connection is auto-approved by NGO and cannot be declined.
            if (clientId == NetworkManager.ServerClientId)
            {
                response.Approved = true;
                response.CreatePlayerObject = true;
                return;
            }

            var lobby = LobbyManager.Instance;

            if (lobby == null || !lobby.IsSpawned)
            {
                Deny(response, LobbyErrorCode.LobbyClosed);
                return;
            }

            string userId = DecodeUserId(request.Payload);

            if (!lobby.CanJoin(clientId, userId, out var error))
            {
                Deny(response, error);
                return;
            }

            response.Approved = true;
            response.CreatePlayerObject = true;

            Debug.Log($"[Lobby][Approval] Approved clientId={clientId}, userId={userId}");
        }

        private static string DecodeUserId(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return "unknown";

            try
            {
                return Encoding.UTF8.GetString(payload);
            }
            catch
            {
                return "invalid";
            }
        }

        private static void Deny(
            NetworkManager.ConnectionApprovalResponse response,
            LobbyErrorCode error)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = ((int)error).ToString();

            Debug.LogWarning($"[Lobby][Approval] Denied: {error}");
        }
    }
}
