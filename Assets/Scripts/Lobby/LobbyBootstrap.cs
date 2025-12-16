using NetCodeTest.Core.Contracts;
using NetCodeTest.Core.DI;
using NetCodeTest.SceneManagement;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    public sealed class LobbyBootstrap : MonoBehaviour
    {
        [SerializeField] private LobbyManager _lobbyManager;

        private void Awake()
        {
            ServiceContainer.Clear();

            var lobbyService = new LobbyService(_lobbyManager);

            ServiceContainer.Register<ILobbyService>(lobbyService);

            Debug.Log("[DI] Lobby scene services registered");
        }
    }
}