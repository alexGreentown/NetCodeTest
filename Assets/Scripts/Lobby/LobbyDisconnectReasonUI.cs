using System;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    public class LobbyDisconnectReasonUI : MonoBehaviour
    {
        [SerializeField] private LobbyUIController _ui;

        private void Start()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            // if its not this player
            if (clientId != NetworkManager.Singleton.LocalClientId) return;

            string reason = NetworkManager.Singleton.DisconnectReason;
            Debug.Log($"[Lobby][Client] Disconnected. Reason='{reason}'");

            // parse reason
            if (!string.IsNullOrWhiteSpace(reason) &&
                Enum.TryParse(reason, out LobbyErrorCode code))
            {
                _ui.ShowError(code);
            }
            else
            {
                _ui.ShowError(0);
            }
            
            _ui.RebuildPlayers();
        }
        
    }
}