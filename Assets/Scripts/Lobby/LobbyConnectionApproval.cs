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
    /// Stores userId from payload so LobbyManager can use it in OnClientConnected.
    /// </summary>
    public static class LobbyConnectionApproval
    {
        private static bool _installed;

        // server-only cache: clientId -> userId (from approval payload)
        private static readonly Dictionary<ulong, string> _approvedUserIds = new();

        public static void Install()
        {
            if (_installed)
                return;

            _installed = true;

            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCallback;
            Debug.Log("[Lobby][Approval] Installed");
        }

        /// <summary>
        /// Called by LobbyManager (server) when NGO fires OnClientConnectedCallback.
        /// We "consume" it so it can't be used twice.
        /// </summary>
        public static bool TryConsumeApprovedUserId(ulong clientId, out string userId)
        {
            if (_approvedUserIds.TryGetValue(clientId, out userId))
            {
                _approvedUserIds.Remove(clientId);
                return true;
            }

            userId = null;
            return false;
        }

        public static void Clear(ulong clientId) => _approvedUserIds.Remove(clientId);

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

            // store real userId for OnClientConnected
            _approvedUserIds[clientId] = userId;

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
                var s = Encoding.UTF8.GetString(payload);
                return string.IsNullOrWhiteSpace(s) ? "unknown" : s.Trim();
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
