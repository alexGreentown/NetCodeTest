using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NetCodeTest.Lobby
{
    public sealed class LobbyFoundRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _ip;
        [SerializeField] private TMP_Text _players;
        [SerializeField] private Button _join;

        public void Bind(string lobbyName, string ip, string players, Action onJoin)
        {
            _title.text = lobbyName;
            _ip.text = ip;
            _players.text = players;

            _join.onClick.RemoveAllListeners();
            _join.onClick.AddListener(() => onJoin?.Invoke());
        }
    }
}