using UnityEngine;
using UnityEngine.UI;
using Warblade.Data;
using Warblade.Managers;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    internal sealed class UiButtonClickSound : MonoBehaviour
    {
        [SerializeField] private AudioCue _clickCue = AudioCue.UiClick;
        [SerializeField] private AudioCue _fallbackCue = AudioCue.UiButton;

        private Button _button;

        public void Configure(Button button)
        {
            if (_button == button)
            {
                return;
            }

            RemoveListener();
            _button = button;
            AddListener();
        }

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            AddListener();
        }

        private void OnDestroy()
        {
            RemoveListener();
        }

        private void AddListener()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClickSound);
                _button.onClick.AddListener(PlayClickSound);
            }
        }

        private void RemoveListener()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClickSound);
            }
        }

        private void PlayClickSound()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            if (audioManager.HasCue(_clickCue))
            {
                audioManager.PlayOneShot(_clickCue);
                return;
            }

            if (_fallbackCue != _clickCue && audioManager.HasCue(_fallbackCue))
            {
                audioManager.PlayOneShot(_fallbackCue);
            }
        }
    }
}
