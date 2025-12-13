using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetCodeTest.SceneManagement
{
    public class NetworkSceneLoader : NetworkBehaviour
    {
#region Fields
        [SerializeField] private string _gameSceneName = "Game";
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



#region NGO Callbacks
        public void LoadGameScene()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
            }
        }

        private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            Debug.Log($"[NetworkSceneLoader] Scene '{sceneName}' loaded. " +
                      $"Completed: {clientsCompleted.Count}, Timed out: {clientsTimedOut.Count}");

            if (clientsTimedOut.Count > 0)
            {
                Debug.LogWarning("[NetworkSceneLoader] WARNING: Some clients failed to load the scene!");
            }
        }
#endregion

    }
}