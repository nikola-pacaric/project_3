using UnityEngine;
using Warblade.Managers;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class PlayerShieldVisual : MonoBehaviour
    {
        [SerializeField] private GameObject _shieldVisualRoot;

        private bool _isVisible;

        private void OnEnable()
        {
            RefreshVisual(true);
        }

        private void LateUpdate()
        {
            RefreshVisual(false);
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void RefreshVisual(bool force)
        {
            bool shouldShow = BuffManager.Instance != null && BuffManager.Instance.IsShieldActive;
            if (!force && shouldShow == _isVisible) return;

            SetVisible(shouldShow);
        }

        private void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            if (_shieldVisualRoot != null)
            {
                _shieldVisualRoot.SetActive(isVisible);
            }
        }
    }
}
