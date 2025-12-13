using NetCodeTest.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NetCodeTest.Lobby
{
    public class LobbyUIController : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private LobbyManager _lobbyManager;
        [SerializeField] private NetworkSceneLoader _sceneLoader;

        private void Start()
        {
            _hostButton.onClick.AddListener(() => _lobbyManager.StartHost());
            _joinButton.onClick.AddListener(() => _lobbyManager.StartClient());
            
            _startGameButton.onClick.AddListener(() =>
            {
                
                _sceneLoader.LoadGameScene();
                
                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.StartGameServerRpc();
                }
            });
        }
    }
}