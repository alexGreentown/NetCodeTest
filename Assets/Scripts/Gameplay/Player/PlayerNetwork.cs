using System;
using NetCodeTest.Lobby;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetCodeTest.Gameplay.Player
{
    /// <summary>
    /// Player facade: ownership, lifecycle, scene state.
    /// Does NOT handle input or movement directly.
    /// </summary>
    public sealed class PlayerNetwork : NetworkBehaviour
    {
        #region Fields
        [Header("Gameplay")]
        [SerializeField] private string _gameSceneName = "Game";

        private PlayerMovementMode _movementMode;

        #endregion


        private void Awake()
        {
            _movementMode = GetComponent<PlayerMovementMode>();
        }

        public override void OnNetworkSpawn()
        {
            // apply movement mode on spawn
            if (_movementMode != null)
            {
                _movementMode.ApplyMode();
            }
            
            if (IsOwner)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
             
            if (LobbyManager.Instance != null)
            {
                var gameplay = LobbyManager.Instance.GameplayEnabled;

                gameplay.OnValueChanged += OnGameplayChanged;

                OnGameplayChanged(false, gameplay.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }        
            
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.GameplayEnabled.OnValueChanged -= OnGameplayChanged;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool gameplay = scene.name == _gameSceneName;

            Debug.Log($"OnSceneLoaded scene.name={scene.name} {gameplay}");
            
            // enable/disable movement scripts safely
            _movementMode?.SetGameplayEnabled(gameplay);
        }
        
        
        private void OnGameplayChanged(bool _, bool enabled)
        {
            _movementMode?.SetGameplayEnabled(enabled);
        }
        
        
    }
}