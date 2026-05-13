using TMPro;
using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;

namespace Warblade.UI
{
    public class TimedBuffHud : MonoBehaviour
    {
        [SerializeField] private BuffType _buffType;
        [SerializeField] private BuffTimerEventChannel _buffTimerChanged;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private GameObject _root;
        [SerializeField] private string _activeFormat = "{0}: {1:0.0}s";
        [SerializeField] private string _inactiveText = "";
        [SerializeField] private bool _hideWhenInactive = true;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            Refresh();
        }

        private void OnEnable()
        {
            if (_buffTimerChanged != null)
            {
                _buffTimerChanged.OnEventRaised += HandleBuffTimerChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_buffTimerChanged != null)
            {
                _buffTimerChanged.OnEventRaised -= HandleBuffTimerChanged;
            }
        }

        private void Update()
        {
            Refresh();
        }

        private void HandleBuffTimerChanged(BuffType buffType, float remainingSeconds, float durationSeconds)
        {
            if (buffType != _buffType) return;
            SetText(remainingSeconds, durationSeconds);
        }

        private void Refresh()
        {
            BuffManager buffManager = BuffManager.Instance;
            if (buffManager == null)
            {
                SetText(0f, 0f);
                return;
            }

            SetText(
                buffManager.GetRemainingSeconds(_buffType),
                buffManager.GetDurationSeconds(_buffType));
        }

        private void SetText(float remainingSeconds, float durationSeconds)
        {
            bool isActive = remainingSeconds > 0f;

            if (_root != null && _root != gameObject)
            {
                _root.SetActive(isActive || !_hideWhenInactive);
            }

            if (_text == null) return;

            _text.text = isActive
                ? string.Format(_activeFormat, _buffType, remainingSeconds, durationSeconds)
                : _inactiveText;
        }
    }
}
