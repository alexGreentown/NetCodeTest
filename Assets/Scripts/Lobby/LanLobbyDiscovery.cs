using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    [Serializable]
    public sealed class LobbyBeacon
    {
        public int v = 1;
        public string lobbyName;
        public int maxPlayers;
        public int currentPlayers;
        public int privacy;     // 0 public, 1 private
        public int gamePort;    // порт UnityTransport
    }
    
    [Serializable]
    public enum DiscoveryMode
    {
        None,
        AdvertiseOnly,
        ListenOnly,
        AdvertiseAndListen
    }
    
    public sealed class LanLobbyDiscovery : MonoBehaviour
    {
        [Header("Discovery UDP")]
        [SerializeField] private int _discoveryPort = 47777;
        [SerializeField] private float _broadcastInterval = 1.0f;
        [SerializeField] private float _entryTtlSeconds = 3.0f;

        private UdpClient _tx;
        private UdpClient _rx;

        private float _nextBroadcastTime;
        private bool _advertise;
        private LobbyManager _lobby;

        private readonly ConcurrentQueue<(IPEndPoint ep, string json)> _inbox = new();
        private readonly Dictionary<string, (LobbyBeacon b, float lastSeen)> _seen = new();

        [SerializeField]
        private DiscoveryMode _mode = DiscoveryMode.ListenOnly;

        public IReadOnlyDictionary<string, LobbyBeacon> PublicLobbies
        {
            get
            {
                Cleanup();
                // возвращаем только public
                var dict = new Dictionary<string, LobbyBeacon>();
                foreach (var kv in _seen)
                    if (kv.Value.b.privacy == (int)LobbyPrivacy.Public)
                        dict[kv.Key] = kv.Value.b;
                return dict;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void StartAdvertising(LobbyManager lobby)
        {
            StopListening();
            
            if (_mode == DiscoveryMode.ListenOnly || _mode == DiscoveryMode.None)
                return;
            
            _lobby = lobby;
            _advertise = true;

            _tx ??= new UdpClient { EnableBroadcast = true };
            _nextBroadcastTime = 0f;
        }

        public void StopAdvertising()
        {
            _advertise = false;
        }

        public void StartListening()
        {
            if (_mode == DiscoveryMode.AdvertiseOnly || _mode == DiscoveryMode.None)
                return;
            
            if (_rx != null) return;

            _rx = new UdpClient(_discoveryPort);
            _rx.EnableBroadcast = true;
            BeginReceive();
        }

        public void StopListening()
        {
            _mode = DiscoveryMode.AdvertiseOnly;
            
            try { _rx?.Close(); } catch { }
            _rx = null;
            _seen.Clear();
        }

        private void Update()
        {
            // consume incoming
            while (_inbox.TryDequeue(out var msg))
            {
                try
                {
                    var b = JsonUtility.FromJson<LobbyBeacon>(msg.json);
                    if (b == null) continue;

                    string ip = msg.ep.Address.ToString();
                    _seen[ip] = (b, Time.unscaledTime);
                }
                catch { /* ignore */ }
            }

            if (_advertise && _lobby != null && _lobby.IsServer)
            {
                // Рекламируем ТОЛЬКО публичные лобби (по ТЗ список публичных)
                if (_lobby.Privacy.Value == LobbyPrivacy.Public && Time.unscaledTime >= _nextBroadcastTime)
                {
                    _nextBroadcastTime = Time.unscaledTime + _broadcastInterval;
                    BroadcastNow();
                }
            }

            Cleanup();
        }

        private void Cleanup()
        {
            if (_seen.Count == 0) return;

            var dead = new List<string>();
            foreach (var kv in _seen)
                if (Time.unscaledTime - kv.Value.lastSeen > _entryTtlSeconds)
                    dead.Add(kv.Key);

            foreach (var k in dead) _seen.Remove(k);
        }

        private void BroadcastNow()
        {
            int port = TryGetGamePort(out var p) ? p : 7777;

            var beacon = new LobbyBeacon
            {
                v = 1,
                lobbyName = _lobby.LobbyName.Value.ToString(),
                maxPlayers = _lobby.MaxPlayers.Value,
                currentPlayers = _lobby.Players.Count,
                privacy = (int)_lobby.Privacy.Value,
                gamePort = port
            };

            string json = JsonUtility.ToJson(beacon);
            byte[] data = Encoding.UTF8.GetBytes(json);

            var ep = new IPEndPoint(IPAddress.Broadcast, _discoveryPort);
            _tx.Send(data, data.Length, ep);
        }

        private bool TryGetGamePort(out int port)
        {
            port = 0;
#if UNITY_NETCODE_GAMEOBJECTS
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm != null && nm.NetworkConfig.NetworkTransport is Unity.Netcode.Transports.UTP.UnityTransport utp)
            {
                port = (int)utp.ConnectionData.Port;
                return true;
            }
#endif
            return false;
        }

        private void BeginReceive()
        {
            try
            {
                _rx.BeginReceive(OnReceive, null);
            }
            catch { /* closed */ }
        }

        private void OnReceive(IAsyncResult ar)
        {
            if (_rx == null) return;

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = null;

            try { data = _rx.EndReceive(ar, ref ep); }
            catch { /* closed */ return; }

            try
            {
                string json = Encoding.UTF8.GetString(data);
                _inbox.Enqueue((ep, json));
            }
            catch { Debug.LogError("JSON error"); }

            BeginReceive();
        }

        private void OnDestroy()
        {
            try { _tx?.Close(); } catch { }
            try { _rx?.Close(); } catch { }
        }
    }
}
