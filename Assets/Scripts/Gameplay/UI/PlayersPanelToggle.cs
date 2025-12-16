using UnityEngine;
using UnityEngine.InputSystem;

namespace NetCodeTest.Gameplay.UI
{
    public sealed class PlayersPanelToggle : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private bool _startHidden = false;

        private void Awake()
        {
            if (_panel != null && _startHidden)
                _panel.SetActive(false);
        }

        private void Update()
        {
            if (_panel == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.tabKey.wasPressedThisFrame)
                _panel.SetActive(!_panel.activeSelf);
        }
    }
}