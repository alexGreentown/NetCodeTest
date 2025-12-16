using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NetCodeTest.Lobby
{
    public sealed class LobbyBrowserUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private LobbyManager _lobby;
        [SerializeField] private LanLobbyDiscovery _discovery;

        [Header("Common")]
        [SerializeField] private TMP_InputField _userId;

        [Header("Public list")]
        [SerializeField] private Transform _listRoot;
        [SerializeField] private LobbyFoundRow _rowPrefab;
        [SerializeField] private Button _refreshBtn;

        [Header("Private join")]
        [SerializeField] private TMP_InputField _hostIp;
        [SerializeField] private TMP_InputField _code;
        [SerializeField] private TMP_InputField _password;
        [SerializeField] private TMP_InputField _port; // optional, default 7777
        [SerializeField] private Button _joinPrivateBtn;

        private readonly List<LobbyFoundRow> _rows = new();

        private void Awake()
        {
            if (_lobby == null) _lobby = LobbyManager.Instance;
            if (_discovery == null) _discovery = FindFirstObjectByType<LanLobbyDiscovery>();
        }

        private void OnEnable()
        {
            _refreshBtn.onClick.AddListener(Rebuild);
            _joinPrivateBtn.onClick.AddListener(JoinPrivate);

            // start listening LAN
            _discovery.StartListening();
        }

        private void OnDisable()
        {
            _refreshBtn.onClick.RemoveAllListeners();
            _joinPrivateBtn.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            // можно авто-обновлять раз в N секунд, но для тестового ок просто кнопка
        }

        public void Rebuild()
        {
            foreach (var r in _rows) Destroy(r.gameObject);
            _rows.Clear();

            foreach (var kv in _discovery.PublicLobbies)
            {
                string ip = kv.Key;
                var b = kv.Value;

                var row = Instantiate(_rowPrefab, _listRoot);
                row.Bind(
                    lobbyName: b.lobbyName,
                    ip: ip,
                    players: $"{b.currentPlayers}/{b.maxPlayers}",
                    onJoin: () => JoinPublic(ip, (ushort)b.gamePort)
                );

                _rows.Add(row);
            }
        }

        private void JoinPublic(string ip, ushort port)
        {
            Debug.Log($"JoinPublic() ip={ip}, port={port}");
            string userId = _userId.text;
            _lobby.StartClientToHost(ip, port, userId, code: "", password: "");
        }

        private void JoinPrivate()
        {
            string userId = _userId.text;
            string ip = _hostIp.text;
            string code = _code.text;
            string pass = _password.text;

            ushort port = 7777;
            if (ushort.TryParse(_port.text, out var p)) port = p;

            _lobby.StartClientToHost(ip, port, userId, code, pass);
        }
    }
}
