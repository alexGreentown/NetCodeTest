using System;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    public enum LobbyPrivacy : byte
    {
        Public = 0,
        Private = 1
    }

    public enum LobbyErrorCode
    {
        None = 0,
        LobbyFull = 101,
        DuplicateUserId = 103,
        WrongUserId = 104,
        LobbyClosed = 102,
        NotAllPlayersReady = 106,
        UnauthorizedAction = 107
    }

    public struct PlayerLobbyData : INetworkSerializable, IEquatable<PlayerLobbyData>
    {
        public ulong ClientId;
        public FixedString64Bytes UserId;
        public bool IsReady;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref UserId);
            serializer.SerializeValue(ref IsReady);
        }    
        
        public bool Equals(PlayerLobbyData other)
            => ClientId == other.ClientId
               && UserId.Equals(other.UserId)
               && IsReady == other.IsReady;

        public override bool Equals(object obj)
            => obj is PlayerLobbyData other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(ClientId, UserId.GetHashCode(), IsReady);
    }

    /// <summary>
    /// server-authoritative lobby
    /// </summary>
    [RequireComponent(typeof(LobbyUIController))]
    public class LobbyManager : NetworkBehaviour
    {
        #region Fields
        public static LobbyManager Instance { get; private set; }

        [SerializeField] 
        private LobbyUIController _lobbyUI;

        // Lobby settings
        public NetworkVariable<FixedString64Bytes> LobbyName = new("Lobby");
        public NetworkVariable<int> MaxPlayers = new(4);
        public NetworkVariable<LobbyPrivacy> Privacy = new(LobbyPrivacy.Public);

        public NetworkList<PlayerLobbyData> Players;
        #endregion

        
        
        #region Unity LifeCycle

        private void Awake()
        {
            _lobbyUI = GetComponent<LobbyUIController>();
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Duplicated Instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            Players = new NetworkList<PlayerLobbyData>();
            
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LobbyConnectionApproval.Install();
        }

        private void OnEnable()
        {
            _lobbyUI.OnHostButtonPress += LobbyUI_OnHostButtonPress;
            _lobbyUI.OnJoinButtonPress += LobbyUI_OnJoinButtonPress;
            _lobbyUI.OnStartButtonPress += LobbyUI_OnStartButtonPress;
            _lobbyUI.OnReadyChanged += LobbyUI_OnReadyChange;
        }

        private void OnDisable()
        {
            _lobbyUI.OnHostButtonPress -= LobbyUI_OnHostButtonPress;
            _lobbyUI.OnJoinButtonPress -= LobbyUI_OnJoinButtonPress;
            _lobbyUI.OnStartButtonPress -= LobbyUI_OnStartButtonPress;
            _lobbyUI.OnReadyChanged -= LobbyUI_OnReadyChange;
        }
        #endregion
        
        
        
        #region Gameplay State

        public NetworkVariable<bool> GameplayEnabled = new(false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        #endregion
        
        
        #region Player Management (Server)

        public override void OnNetworkSpawn()
        {
            Debug.Log("[Lobby] OnNetworkSpawn()");
            
            Players.OnListChanged += OnPlayersListChanged;
            _lobbyUI.RebuildPlayers();
            
            if (IsServer)
            {
                Debug.Log("[Lobby][Server] LobbyManager spawned");
                
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                
            }
            
            if (IsServer && NetworkManager.Singleton.IsHost)
            {
                //AddHostPlayer(); 
            }
        }

        private void OnPlayersListChanged(NetworkListEvent<PlayerLobbyData> events)
        {
            // Debug.Log($"OnPlayersListChanged {events.Value.UserId} {Players.Count}");
            
            _lobbyUI.RebuildPlayers();
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"OnClientDisconnected {clientId}");
            
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].ClientId == clientId)
                {
                    Debug.Log($"[Lobby][Server] Player disconnected {clientId}");
                    Players.RemoveAt(i);
                    return;
                }
            }
            
        }
        
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[Lobby] OnClientConnected {clientId}");
            
            if (!IsServer) return;

            Debug.Log($"[Lobby][Server] OnClientConnected {clientId}");

            string userId;

            // Host: userId from input field; Host has no payload approval
            if (clientId == NetworkManager.ServerClientId)
            {
                userId = _lobbyUI.GetPlayerID();
            }
            else
            {
                // simple client, gets userId from approval payload
                if (!LobbyConnectionApproval.TryConsumeApprovedUserId(clientId, out userId))
                {
                    Debug.LogWarning($"[Lobby][Server] Missing approved userId for clientId={clientId}, disconnecting");
                    NetworkManager.Singleton.DisconnectClient(clientId);
                    return;
                }
            }

            if (!TryAddPlayer(clientId, userId, out var error))
            {
                Debug.LogWarning($"[Lobby][Server] Add on connect failed: {error} clientId={clientId}");
                if (clientId != NetworkManager.ServerClientId)
                    NetworkManager.Singleton.DisconnectClient(clientId);
            }
        }

        public bool CanJoin(ulong clientId, string userId, out LobbyErrorCode error)
        {
            error = LobbyErrorCode.None;

            if (Players.Count >= MaxPlayers.Value)
            {
                error = LobbyErrorCode.LobbyFull;
                return false;
            }

            if (string.IsNullOrWhiteSpace(userId) || userId == "unknown" || userId == "invalid")
            {
                error = LobbyErrorCode.WrongUserId; 
                return false;
            }

            foreach (var p in Players)
            {
                if (p.ClientId == clientId)
                {
                    error = LobbyErrorCode.DuplicateUserId;
                    return false;
                }

                if (p.UserId.ToString() == userId)
                {
                    error = LobbyErrorCode.DuplicateUserId;
                    return false;
                }
            }

            return true;
        }


        public bool TryAddPlayer(ulong clientId, string userId, out LobbyErrorCode error)
        {
            Debug.Log($"[Lobby][Server] TryAddPlayer {clientId} userId={userId}");

            if (!CanJoin(clientId, userId, out error))
                return false;
            
            Players.Add(new PlayerLobbyData
            {
                ClientId = clientId,
                UserId = userId,
                IsReady = false
            });

            return true;
        }

       [Rpc(SendTo.Server)]
        public void KickPlayerServerRpc(ulong targetClientId, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            if (sender != NetworkManager.ServerClientId)
                return;

            Debug.Log($"[Lobby][Server] Kicking {targetClientId}");

            NetworkManager.Singleton.DisconnectClient(
                targetClientId,
                ((int)LobbyErrorCode.LobbyClosed).ToString()
            );
        }

        #endregion

        
        
        #region Ready System

        [Rpc(SendTo.Server)]
        public void SetReadyServerRpc(bool ready, RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].ClientId == sender)
                {
                    var p = Players[i];
                    p.IsReady = ready;
                    Players[i] = p;

                    Debug.Log($"[Lobby][Server] Ready {sender} = {ready}");
                    return;
                }
            }
        }

        // [Rpc(SendTo.Server)]
        private bool AreAllReady()
        {
            if (Players.Count == 0)
                return false;

            foreach (var p in Players)
                if (!p.IsReady)
                    return false;

            return true;
        }

        #endregion

        #region Start Game (VALIDATED)

        [Rpc(SendTo.Server)]
        public void StartGameServerRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;

            // only host can start game
            if (sender != NetworkManager.ServerClientId)
            {
                SendErrorRPC(sender, LobbyErrorCode.UnauthorizedAction);
                return;
            }

            if (!AreAllReady())
            {
                SendErrorRPC(sender, LobbyErrorCode.NotAllPlayersReady);
                return;
            }

            ShowLoadingClientRpc();
            GameplayEnabled.Value = true;
            Debug.Log("[Lobby][Server] Game started");
        }
        
        [ClientRpc]
        private void ShowLoadingClientRpc()
        {
            _lobbyUI.ShowLoadingScreen();
        }

        #endregion

        #region Errors

        [ClientRpc]
        private void ErrorClientRpc(int errorCode, ClientRpcParams rpcParams = default)
        {
            var code = (LobbyErrorCode)errorCode;
            Debug.LogWarning($"[Lobby][Client] Error {code}");

            _lobbyUI.ShowError(code);
        }

        [Rpc(SendTo.Server)]
        private void SendErrorRPC(ulong targetClientId, LobbyErrorCode code)
        {
            ErrorClientRpc(
                (int)code,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { targetClientId }
                    }
                });
        }

        #endregion

        #region Start / Stop

        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        public void StartClient(string userId)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData =
                Encoding.UTF8.GetBytes(userId);

            NetworkManager.Singleton.StartClient();
        }

        #endregion

        #region Callbacks

        private void LobbyUI_OnHostButtonPress()
        {
            StartHost();
        }
        
        private void LobbyUI_OnJoinButtonPress()
        {
            string playerID = _lobbyUI.GetPlayerID();
            StartClient(playerID);
        }
        
        private void LobbyUI_OnStartButtonPress()
        {
            StartGameServerRpc();
        }
        
        private void LobbyUI_OnReadyChange(bool isReady)
        {
            SetReadyServerRpc(isReady);
        }

        #endregion

        public void HideLoadingScreen() => _lobbyUI.HideLoadingScreen();
    }
}
