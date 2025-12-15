using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NetCodeTest.Lobby
{
    public class LobbyPlayerRow : MonoBehaviour
    {
        #region Fields
        [SerializeField] private TMP_Text _userIdText;
        [SerializeField] private Image _readyIndicator;
        [SerializeField] private Button _kickButton;
        [SerializeField] private TMP_Text _hostLabel;

        private ulong _clientId;
        [SerializeField] private TMP_Text _pingText;
        #endregion  

        public void Bind(PlayerLobbyData data, bool isHost)
        {
            _clientId = data.ClientId;
            _userIdText.text = data.UserId.ToString();
            _readyIndicator.color = data.IsReady ? Color.green : Color.red;

            _pingText.text = $"Ping: {data.PingMs} ms";
            
            // hide kick button for all clients
            _kickButton.gameObject.SetActive(isHost && data.ClientId != 0);
            
            // the host see "host" label instead of kick button on his player data
            // show "host" label in clients too
            _hostLabel.gameObject.SetActive(data.ClientId == 0);
            
            _kickButton.onClick.RemoveAllListeners();
            _kickButton.onClick.AddListener(OnKick);
        }

        private void OnKick()
        {
            LobbyManager.Instance.KickPlayerServerRpc(_clientId);
        }
    }
}