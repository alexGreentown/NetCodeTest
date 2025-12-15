using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private string gameSceneName = "Game";

    private readonly HashSet<ulong> _spawned = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        DontDestroyOnLoad(gameObject);
        
        NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnLoadEventCompleted(
        string sceneName,
        LoadSceneMode mode,
        IReadOnlyList<ulong> clientsCompleted,
        IReadOnlyList<ulong> clientsTimedOut)
    {
        if (sceneName != gameSceneName) return;

        // start again
        _spawned.Clear();

        foreach (var clientId in clientsCompleted)
            SpawnIfNeeded(clientId);
    }

    private void OnClientConnected(ulong clientId)
    {
        // Late-join: client joined when game scene is active
        if (SceneManager.GetActiveScene().name != gameSceneName)
            return;

        SpawnIfNeeded(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _spawned.Remove(clientId);
    }

    private void SpawnIfNeeded(ulong clientId)
    {
        if (_spawned.Contains(clientId))
            return;

        var obj = Instantiate(playerPrefab);

        // create PlayerObject for client (ownership)
        obj.SpawnAsPlayerObject(clientId, destroyWithScene: true);

        _spawned.Add(clientId);
        Debug.Log($"[Spawner][Server] Spawned player for {clientId}");
    }
}
