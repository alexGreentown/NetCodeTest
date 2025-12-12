using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NetCodeTest.Gameplay.Player
{
    public class PlayerNetwork : NetworkBehaviour
    {
#region Fields
        public NetworkVariable<Color> _playerColor = new NetworkVariable<Color>(Color.white);
        [SerializeField]private Renderer _renderer;

        private bool _isControlsEnabled;
        
        private PlayerInput _input;
        private Vector2 _moveInput;
#endregion



#region Unity Methods

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _playerColor.OnValueChanged += OnColorChanged;
        }

        private void OnDisable()
        {
            _playerColor.OnValueChanged -= OnColorChanged;
        }

        private void Update()
        {
            if (!IsOwner) return;
            //if (!IsOwner || !_isControlsEnabled) return;

            HandleMovement(_moveInput.x, _moveInput.y);
        }
#endregion



#region Methods

        public void OnMove(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
        }

        public void OnColor(InputAction.CallbackContext ctx)
        {
            if (!IsOwner) return;

            if (ctx.performed)
            {
                Color newColor = Random.ColorHSV();
                ChangeColorServerRpc(newColor);
            }
        }

        private void HandleMovement(float moveX, float moveZ)
        {
            Vector3 move = new Vector3(moveX, 0f, moveZ);   
            
            float speed = 5f;
            transform.Translate(move * speed * Time.deltaTime);     
            // SendMoveInputServerRpc(move);  
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"OnSceneLoaded() {scene.name}");
            if (scene.name == "Game")
            {                
                EnableGameplayControls(); // your input or movement scripts
            }
        }

        private void EnableGameplayControls()
        {
            _isControlsEnabled = true;
        }

        private void OnColorChanged(Color oldColor, Color newColor)
        {
            if (_renderer != null)
            {
                _renderer.material.color = newColor;
            }
        }

        

        public void SendColorToClient(ulong targetClientId)
        {
            ApplyColorClientRpc(_playerColor.Value, new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new[] { targetClientId }
                }
            });
        }
#endregion



#region NetCode methods

        public override void OnNetworkSpawn()
        {
            _input.enabled = IsOwner;

            Debug.Log($"OnNetworkSpawn() {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} IsServer ={IsServer} | IsOwner={IsOwner} | OwnerClientId={OwnerClientId} | LocalClientId={NetworkManager.Singleton.LocalClientId}");
            if (IsServer && _playerColor.Value == Color.white)
            {
                _playerColor.Value = Random.ColorHSV();
            }
            OnColorChanged(Color.white, _playerColor.Value); 

            if (IsOwner && IsClient && !IsServer)
            {
                RequestAllPlayerColorsServerRpc(); // ask for existing player colors
            }

            if (IsOwner)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        // public override void OnNetworkDespawn()
        // {
        // }
        
        [ServerRpc]
        private void SendMoveInputServerRpc(Vector3 moveInput, ServerRpcParams rpcParams = default)
        {
            float moveSpeed = 5f;
            transform.Translate(moveInput * moveSpeed * Time.deltaTime);
        }
           
        [ServerRpc]
        private void ChangeColorServerRpc(Color newColor)
        {
            _playerColor.Value = newColor;
        }

        [ServerRpc]
        public void RequestAllPlayerColorsServerRpc()
        {
            foreach (var player in FindObjectsOfType<PlayerNetwork>())
            {
                player.SendColorToClient(OwnerClientId); // owner = the joining player
            }
        }

        [ClientRpc]
        private void ApplyColorClientRpc(Color color, ClientRpcParams rpcParams = default)
        {
            if (_renderer != null)
                _renderer.material.color = color;
        }


#endregion
    }
}
