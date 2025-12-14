using System.Collections.Generic;
using NetCodeTest.Lobby;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetCodeTest.SceneManagement
{
    public class NetworkSceneLoader : NetworkBehaviour
    {
#region Fields
        [SerializeField] private string _gameSceneName = "Game";
        
        private HashSet<ulong> _loadedClients = new();
#endregion



#region Unity Methods
        private void OnEnable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            }
        }

        void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }
        }
#endregion



#region Methods
        public void LoadGameScene()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.GameplayEnabled.OnValueChanged += OnGameplayChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.GameplayEnabled.OnValueChanged -= OnGameplayChanged;
            }
        }

        private void OnGameplayChanged(bool _, bool enabled)
        {
            if (!IsServer) return;

            if (enabled)
            {
                Debug.Log("[SceneLoader][Server] GameplayEnabled -> loading scene");
                LoadGameScene();
            }
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer) return;

            Debug.Log($"[NetworkSceneLoader] Scene '{sceneName}' loaded. " +
                      $"Completed: {clientsCompleted.Count}, Timed out: {clientsTimedOut.Count}");

            if (clientsTimedOut.Count > 0)
            {
                Debug.LogWarning("[NetworkSceneLoader] WARNING: Some clients failed to load the scene!");
            }
            
            foreach (var clientId in clientsCompleted)
            {
                _loadedClients.Add(clientId);
            }

            TryFinishLoading();
        }

        private void TryFinishLoading()
        {
            if (_loadedClients.Count == NetworkManager.Singleton.ConnectedClientsIds.Count)
            {
                Debug.Log("[SceneLoader][Server] All clients loaded");
                HideLoadingClientRpc();
            }
        }

        [ClientRpc]
        private void HideLoadingClientRpc()
        {
            LobbyManager.Instance.HideLoadingScreen();
        }

#endregion

    }
}