using System;
using System.Collections.Generic;
using NetCodeTest.SceneManagement;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace NetCodeTest.Lobby
{
    [RequireComponent(typeof(LobbyManager))]
    public class LobbyUIController : MonoBehaviour
    {
        #region Events
        public event Action OnHostButtonPress;
        public event Action OnJoinButtonPress;
        public event Action OnStartButtonPress;
        public event Action<bool> OnReadyChanged;
        #endregion
        
        [SerializeField] private Toggle _readyToggle;
        
        [Header("Buttons")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _startGameButton;
        
        private LobbyManager _lobbyManager;
        
        [Header("Players UI")]
        [SerializeField] private Transform _playersRoot;
        [SerializeField] private LobbyPlayerRow _playerRowPrefab;
        [SerializeField] private TMP_InputField _playerIDInput;
        
        [SerializeField] private NetworkSceneLoader _sceneLoader;

        [SerializeField] private GameObject _loadingScreen;

        private readonly Dictionary<ulong, LobbyPlayerRow> _rows = new();
        #region Unity LifeCycle

        private void Awake()
        {
            _lobbyManager = GetComponent<LobbyManager>();
        }

        private void OnEnable()
        {
            _hostButton.onClick.AddListener(() => OnHostButtonPress?.Invoke() ); 
            _joinButton.onClick.AddListener(() => OnJoinButtonPress?.Invoke() );
            _startGameButton.onClick.AddListener(() => OnStartButtonPress?.Invoke() );
            _readyToggle.onValueChanged.AddListener((isReady) => OnReadyChanged?.Invoke(isReady));
        }

        private void OnDisable()
        {
            _hostButton.onClick.RemoveAllListeners();
            _joinButton.onClick.RemoveAllListeners();
            _startGameButton.onClick.RemoveAllListeners();
            _readyToggle.onValueChanged.RemoveAllListeners();
        }

        private void Start()
        {
            LobbyManager.Instance.Players.OnListChanged += OnPlayersChanged;
            RebuildPlayers();
        }
#endregion

        public void HideLoadingScreen()
        {
            _loadingScreen.SetActive(false);
        }
        
        private void OnPlayersChanged(NetworkListEvent<PlayerLobbyData> _)
        {
            RebuildPlayers();
        }

        private void RebuildPlayers()
        {
            foreach (var row in _rows.Values)
                Destroy(row.gameObject);

            _rows.Clear();

            foreach (var player in LobbyManager.Instance.Players)
            {
                var row = Instantiate<LobbyPlayerRow>(_playerRowPrefab, _playersRoot);
                row.Bind(player, NetworkManager.Singleton.IsHost);
                _rows[player.ClientId] = row;
            }
        }

        public string GetPlayerID()
        {
            return _playerIDInput.text;
        }
        

    }
}