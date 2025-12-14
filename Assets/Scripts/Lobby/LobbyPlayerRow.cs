using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NetCodeTest.Lobby
{
    public class LobbyPlayerRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _userIdText;
        [SerializeField] private Image _readyIndicator;
        [SerializeField] private Button _kickButton;

        private ulong _clientId;

        public void Bind(PlayerLobbyData data, bool isHost)
        {
            _clientId = data.ClientId;
            _userIdText.text = data.UserId.ToString();
            _readyIndicator.color = data.IsReady ? Color.green : Color.red;

            _kickButton.gameObject.SetActive(isHost && data.ClientId != 0);
            _kickButton.onClick.RemoveAllListeners();
            _kickButton.onClick.AddListener(OnKick);
        }

        private void OnKick()
        {
            LobbyManager.Instance.KickPlayerServerRpc(_clientId);
        }
    }
}