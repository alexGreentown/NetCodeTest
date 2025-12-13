using System;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance;
        
        public NetworkVariable<bool> GameplayEnabled =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void StartGameServerRpc()
        {
            Debug.Log($"StartGameServerRpc()");
            GameplayEnabled.Value = true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }

        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
        }
    }
}