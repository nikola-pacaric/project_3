using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Warblade.Data;
using Warblade.Managers;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    internal sealed class UiSelectableAudioFeedback : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IPointerDownHandler
    {
        private const float FeedbackDebounceSeconds = 0.06f;

        [SerializeField] private AudioCue _highlightCue = AudioCue.UiHighlight;
        [SerializeField] private AudioCue _fallbackCue = AudioCue.UiButton;
        [SerializeField] private bool _playHighlight = true;

        private Selectable _selectable;
        private float _lastHighlightTime = -1f;

        public void Configure(Selectable selectable)
        {
            _selectable = selectable;
        }

        private void Awake()
        {
            if (_selectable == null)
            {
                _selectable = GetComponent<Selectable>();
            }

        }

        public void OnSelect(BaseEventData eventData)
        {
            PlayHighlightCue();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHighlightCue();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            PlayHighlightCue();
        }

        private void PlayHighlightCue()
        {
            if (!_playHighlight || !CanPlayFeedback() || IsDebounced(ref _lastHighlightTime))
            {
                return;
            }

            PlayCue(_highlightCue, _fallbackCue);
        }

        private bool CanPlayFeedback()
        {
            if (_selectable == null)
            {
                _selectable = GetComponent<Selectable>();
            }

            return _selectable != null
                && _selectable.IsActive()
                && _selectable.IsInteractable();
        }

        private static bool IsDebounced(ref float lastTime)
        {
            float now = Time.unscaledTime;
            if (now - lastTime < FeedbackDebounceSeconds)
            {
                return true;
            }

            lastTime = now;
            return false;
        }

        private static void PlayCue(AudioCue cue, AudioCue fallbackCue)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            if (audioManager.HasCue(cue))
            {
                audioManager.PlayOneShot(cue);
                return;
            }

            if (fallbackCue != cue && audioManager.HasCue(fallbackCue))
            {
                audioManager.PlayOneShot(fallbackCue);
            }
        }
    }
}
