using NetCodeTest.Lobby;
using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    public struct InputTick : INetworkSerializable
    {
        public int ClientTick;
        public Vector2 Move;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientTick);
            serializer.SerializeValue(ref Move);
        }
    }


    public struct StateTick : INetworkSerializable
    {
        public int ServerTick;
        public Vector3 Position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ServerTick);
            serializer.SerializeValue(ref Position);
        }
    }


    /// <summary>
    /// Server-authoritative movement with client prediction + reconciliation.
    /// </summary>
    [RequireComponent(typeof(PlayerInputSource))]
    public sealed class ServerAuthMovement : NetworkBehaviour
    {
#region Fields
        private const int BufferSize = 1024;

        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _reconcileThreshold = 0.05f;

        private PlayerInputSource _input;

        private InputTick[] _inputBuffer = new InputTick[BufferSize];
        private Vector3[] _predictedPosBuffer = new Vector3[BufferSize];
        private int _clientTick;
        
        private float _lastError;
        private float _serverTickOffsetAvg;
        [SerializeField] private float _offsetSmoothing = 0.1f; // 0.05–0.2

        private bool _gameplayEnabled;
        
        private Vector2 _lastServerMove;
        private float _lastInputTime;
        private const float InputTimeout = 0.2f;
        
        private RemoteSmoothing _remoteSmoothing;
        private int _serverSimTick;

#endregion
        
        
        
#region Debug Accessors
        public int ClientTick => _clientTick;
        public float LastReconciliationError => _lastError;
        public float ServerTickOffsetAvg => _serverTickOffsetAvg;
#endregion



#region Unity LifeCycle
        private void Awake()
        {
            _input = GetComponent<PlayerInputSource>();
            _remoteSmoothing = GetComponent<RemoteSmoothing>();
        }

        private void FixedUpdate()
        {
            // CLIENT — prediction only
            if (IsOwner && _gameplayEnabled)
            {
                _clientTick++;
                int idx = _clientTick % BufferSize;

                Vector2 moveInput = _input.Move;
                if (moveInput.sqrMagnitude < 0.0001f)
                    moveInput = Vector2.zero;

                var input = new InputTick
                {
                    ClientTick = _clientTick,
                    Move = moveInput
                };

                _inputBuffer[idx] = input;

                // client-side prediction (LOCAL input only)
                SimulatePrediction(moveInput);
                _predictedPosBuffer[idx] = transform.position;

                // send intent to server
                SendInputServerRpc(input);
            }

            // SERVER — authoritative simulation
            if (IsServer && _gameplayEnabled)
            {
                // input timeout protection (packet loss safe)
                if (Time.time - _lastInputTime > InputTimeout)
                    _lastServerMove = Vector2.zero;
                
                Debug.Log($"[SERVER] tick move={_lastServerMove} owner={OwnerClientId}");

                // authoritative movement
                _serverSimTick++;

                SimulateServer(_lastServerMove);

                var state = new StateTick
                {
                    ServerTick = _serverSimTick,
                    Position = transform.position
                };

                SendStateClientRpc(state);
            }
        }
        

#endregion

        public void SetGameplayEnabled(bool enabled)
        {
            _gameplayEnabled = enabled;
            Debug.Log($"SetGameplayEnabled3 _gameplayEnabled={_gameplayEnabled}");
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendInputServerRpc(InputTick input, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;
            
            _lastServerMove = input.Move;
            _lastInputTime = Time.time;
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendStateClientRpc(StateTick state)
        {
            HandleServerState(state);
        }
        
        private void HandleServerState(StateTick state)
        {
            if (IsOwner)
            {
                HandleOwnerReconciliation(state);
                return;
            }

            if (_remoteSmoothing != null)
                _remoteSmoothing.SetTarget(state.Position);
        }

        
        // approximate mapping serverTick - clientTick
        private void HandleOwnerReconciliation(StateTick state)
        {
            int estimatedClientTick =
                state.ServerTick - Mathf.RoundToInt(_serverTickOffsetAvg);

            int idx = estimatedClientTick % BufferSize;

            Vector3 predicted = _predictedPosBuffer[idx];
            float error = Vector3.Distance(predicted, state.Position);

            _lastError = error;

            if (error < _reconcileThreshold)
            {
                UpdateServerTickOffset(state.ServerTick, _clientTick);
                return;
            }

            transform.position = state.Position;

            for (int t = estimatedClientTick + 1; t <= _clientTick; t++)
            {
                int i = t % BufferSize;
                if (_inputBuffer[i].ClientTick != t)
                    continue;

                SimulateServer(_inputBuffer[i].Move);
                _predictedPosBuffer[i] = transform.position;
            }

        }

        
        // Authoritative movement — source of truth
        private void SimulateServer(Vector2 move)
        {
            Vector3 delta = new Vector3(move.x, 0f, move.y) 
                            * (_speed * Time.fixedDeltaTime);

            transform.position += delta;
        }

        
        // Client-side prediction (temporary, may be corrected)
        private void SimulatePrediction(Vector2 move)
        {
            Vector3 delta = new Vector3(move.x, 0f, move.y) 
                            * (_speed * Time.fixedDeltaTime);

            transform.position += delta;
        }

        private void UpdateServerTickOffset(int serverTick, int clientTick)
        {
            float sample = serverTick - clientTick;

            // Exponential Moving Average
            _serverTickOffsetAvg = Mathf.Lerp(
                _serverTickOffsetAvg,
                sample,
                _offsetSmoothing
            );
        }
        
        public override void OnNetworkSpawn()
        {
            _clientTick = 0;
            _serverSimTick = 0;
            _serverTickOffsetAvg = 0f;

            for (int i = 0; i < BufferSize; i++)
            {
                _inputBuffer[i].ClientTick = -1;
                _predictedPosBuffer[i] = Vector3.zero;
            } 
            
            // Late-join safe sync >> this requires a spawned Lobby rather than existing
            var lobby = LobbyManager.Instance;
            if (lobby != null)
            {
                _gameplayEnabled = lobby.GameplayEnabled.Value;
            }
        }
        

    }
}
