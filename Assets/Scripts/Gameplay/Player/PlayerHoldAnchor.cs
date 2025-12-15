using UnityEngine;

namespace NetCodeTest.Gameplay.Player
{
    public sealed class PlayerHoldAnchor : MonoBehaviour
    {
        [SerializeField] private Transform _anchor;
        public Transform Anchor => _anchor != null ? _anchor : transform;

        private void Reset() => _anchor = transform;
    }
}