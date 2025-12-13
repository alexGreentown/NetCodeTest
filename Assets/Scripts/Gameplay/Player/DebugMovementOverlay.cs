using Unity.Netcode;
using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    /// <summary>
    /// On-screen debug overlay for movement & networking.
    /// Shows RTT, movement mode, ticks and reconciliation error.
    /// Visible only for the owning player.
    /// </summary>
    public sealed class DebugMovementOverlay : NetworkBehaviour
    {
        [Header("Debug Overlay")]
        [SerializeField] private bool _showOverlay = true;

        [Header("References")]
        [SerializeField] private PlayerMovementMode _movementMode;
        [SerializeField] private ServerAuthMovement _serverAuthMovement;

        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;

        private void Awake()
        {
            _labelStyle = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            _headerStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnGUI()
        {
            if (!_showOverlay || !IsOwner)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 360, 260), GUI.skin.box);

            GUILayout.Label("Movement Debug Overlay", _headerStyle);
            GUILayout.Space(6);

            
            // Movement mode
            if (_movementMode != null)
            {
                GUILayout.Label(
                    $"Mode: {_movementMode.CurrentMode}",
                    _labelStyle
                );
            }

            // Network RTT
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.NetworkConfig?.NetworkTransport != null)
            {
                ulong rttMs =
                    NetworkManager.Singleton.NetworkConfig.NetworkTransport
                        .GetCurrentRtt(NetworkManager.Singleton.LocalClientId);

                GUILayout.Label(
                    $"RTT: {rttMs} ms",
                    _labelStyle
                );
            }
            
            // Server-authoritative details
            if (_movementMode != null &&
                _movementMode.CurrentMode == MovementMode.ServerAuthoritative &&
                _serverAuthMovement != null)
            {
                GUILayout.Space(8);
                GUILayout.Label("Server-Auth Details", _headerStyle);

                GUILayout.Label(
                    $"Client Tick: {_serverAuthMovement.ClientTick}",
                    _labelStyle
                );

                GUILayout.Label(
                    $"Reconcile Error: {_serverAuthMovement.LastReconciliationError:0.000}",
                    _labelStyle
                );

                GUILayout.Label(
                    $"Tick Offset Avg: {_serverAuthMovement.ServerTickOffsetAvg:0.00}",
                    _labelStyle
                );
            }

            GUILayout.EndArea();
        }
    }
}
