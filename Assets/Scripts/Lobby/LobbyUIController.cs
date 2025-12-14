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



        #region Fields
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
        [SerializeField] private TMP_Text _errorText;
        private readonly Dictionary<ulong, LobbyPlayerRow> _rows = new();
        
        private bool _suppressReadyToggleCallback;

        #endregion
        
        
        
        #region Unity LifeCycle

        private void OnEnable()
        {
            _hostButton.onClick.AddListener(() => OnHostButtonPress?.Invoke() ); 
            _joinButton.onClick.AddListener(() => OnJoinButtonPress?.Invoke() );
            _startGameButton.onClick.AddListener(() => OnStartButtonPress?.Invoke() );
            
            _readyToggle.onValueChanged.AddListener(isReady =>
            {
                if (_suppressReadyToggleCallback)
                    return;

                OnReadyChanged?.Invoke(isReady);
            });
            
            _errorText.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _hostButton.onClick.RemoveAllListeners();
            _joinButton.onClick.RemoveAllListeners();
            _startGameButton.onClick.RemoveAllListeners();
            _readyToggle.onValueChanged.RemoveAllListeners();
        }

        private void Awake()
        {
            _lobbyManager = GetComponent<LobbyManager>();
        }
        
        private void Start()
        {
            _loadingScreen.SetActive(false);
        }
#endregion

        public void ShowLoadingScreen()
        {
            _loadingScreen.SetActive(true);
        }

        public void HideLoadingScreen()
        {
            _loadingScreen.SetActive(false);
        }

        public void RebuildPlayers()
        {
            Debug.Log($"RebuildPlayers {_lobbyManager.Players.Count}");
            
            foreach (var row in _rows.Values)
                Destroy(row.gameObject);

            _rows.Clear();

            
            // check for strange NGO error, same player repeats twice
            List<PlayerLobbyData> ps = new();
            
            
            foreach (var player in _lobbyManager.Players)
            {
                bool isDuplicated = default;
                foreach (var pl in ps)
                {
                    if(pl.ClientId == player.ClientId)
                        isDuplicated = true;
                }
                if (isDuplicated)
                    continue;
                ps.Add(player);
                
                var row = Instantiate<LobbyPlayerRow>(_playerRowPrefab, _playersRoot);
                row.Bind(player, NetworkManager.Singleton.IsHost);
                _rows[player.ClientId] = row;
                
                if (player.ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    _suppressReadyToggleCallback = true;
                    _readyToggle.isOn = player.IsReady;
                    _suppressReadyToggleCallback = false;
                }
            }            
        }

        public string GetPlayerID()
        {
            return _playerIDInput.text;
        }
        
        public void ShowError(LobbyErrorCode code)
        {
            _errorText.gameObject.SetActive(true);
            _errorText.text = code switch
            {
                LobbyErrorCode.LobbyFull => "Lobby is full",
                LobbyErrorCode.DuplicateUserId => "User ID already in lobby",
                LobbyErrorCode.NotAllPlayersReady => "Not all players are ready",
                LobbyErrorCode.UnauthorizedAction => "Only host can start the game",
                LobbyErrorCode.LobbyClosed => "Lobby was closed",
                _ => "Unknown error"
            };
        }

    }
}