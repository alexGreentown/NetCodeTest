using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    public class LobbyManager : MonoBehaviour
    {
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