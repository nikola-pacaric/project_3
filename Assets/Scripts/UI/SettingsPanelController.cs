using UnityEngine;

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

            UiSelectionHelper.ApplySelectableAudioFeedback(_root);
        }

        public void Close()
        {
            SetVisible(false);
            UiSelectionHelper.RestorePreviousSelectionNextFrame(this);
        }

        private void SetVisible(bool isVisible)
        {
            if (_root != null)
            {
                _root.SetActive(isVisible);
                UiSelectionHelper.ApplySelectableAudioFeedback(_root);
            }
        }
    }
}
