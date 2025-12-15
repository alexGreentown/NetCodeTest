using NetCodeTest.Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

public sealed class SharedPhysicsObject : NetworkBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Collider _col;

    private readonly NetworkVariable<ulong> _heldBy =
        new NetworkVariable<ulong>(ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public bool IsHeld => _heldBy.Value != ulong.MaxValue;
    public ulong HeldByClientId => _heldBy.Value;

    private void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (_col == null) _col = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        _heldBy.OnValueChanged += OnHeldChanged;
        ApplyHeldState(_heldBy.Value);
    }

    public override void OnNetworkDespawn()
    {
        _heldBy.OnValueChanged -= OnHeldChanged;
    }

    private void OnHeldChanged(ulong _, ulong nowHeldBy) => ApplyHeldState(nowHeldBy);

    private void ApplyHeldState(ulong heldBy)
    {
        bool held = heldBy != ulong.MaxValue;

        if (_rb != null)
        {
            _rb.isKinematic = held;
            _rb.useGravity = !held;
            if (held)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        if (_col != null)
        {
            // to avoid clipping into player
            _col.isTrigger = held;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        if (!IsHeld) return;

        if (!NetworkManager.ConnectedClients.TryGetValue(_heldBy.Value, out var client) ||
            client.PlayerObject == null)
        {
            // if owner disappeared, then let go
            _heldBy.Value = ulong.MaxValue;
            return;
        }

        // hold object by player (from server)
        var player = client.PlayerObject;
        var anchor = player.GetComponentInChildren<PlayerHoldAnchor>();
        var targetPos = (anchor != null ? anchor.Anchor.position : (player.transform.position + player.transform.forward * 1.5f));

        if (_rb != null) _rb.MovePosition(targetPos);
        else transform.position = targetPos;
    }

#region server-side API

    public bool TryGrabServer(ulong senderId)
    {
        if (!IsServer) return false;
        if (IsHeld) return false;

        _heldBy.Value = senderId;
        return true;
    }

    public bool DropServer(ulong senderId)
    {
        if (!IsServer) return false;
        if (_heldBy.Value != senderId) return false;

        _heldBy.Value = ulong.MaxValue;
        return true;
    }

    public bool ThrowServer(ulong clientId, Vector3 dir, float impulse)
    {
        if (!IsServer) return false;
        if (_heldBy.Value != clientId) return false;

        _heldBy.Value = ulong.MaxValue;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.AddForce(dir.normalized * impulse, ForceMode.Impulse);
        }
        return true;
    }

    public bool DeleteServer(ulong senderClientId)
    {
        if (!IsServer) return false;

        // 1) object must be held
        if (_heldBy.Value == ulong.MaxValue) return false;

        // 2) only player who holds object can delete
        if (_heldBy.Value != senderClientId) return false;

        NetworkObject.Despawn(true);
        return true;
    }
    
#endregion
}
