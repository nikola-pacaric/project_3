using UnityEngine;
using Warblade.Data;
using Warblade.Managers;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }
        }

        public void Close()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (_root != null)
            {
                _root.SetActive(isVisible);
            }
        }
    }
}
