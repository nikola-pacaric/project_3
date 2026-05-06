using TMPro;
using UnityEngine;
using Warblade.Data.Events;
using Warblade.Managers;

namespace Warblade.UI
{
    public class RunStatHud : MonoBehaviour
    {
        private enum RunStatDisplay
        {
            Cash,
            Lives,
            Armour,
            Speed,
            Bullets,
            Time
        }

        [SerializeField] private RunStatDisplay _stat;
        [SerializeField] private IntEventChannel _changed;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private string _format = "{0}";

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            if (_changed != null)
            {
                _changed.OnEventRaised += HandleChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_changed != null)
            {
                _changed.OnEventRaised -= HandleChanged;
            }
        }

        private void HandleChanged(int value)
        {
            SetText(value);
        }

        private void Refresh()
        {
            RunStatsManager runStats = RunStatsManager.Instance;
            if (runStats == null) return;

            SetText(GetCurrentValue(runStats));
        }

        private int GetCurrentValue(RunStatsManager runStats)
        {
            switch (_stat)
            {
                case RunStatDisplay.Cash:
                    return runStats.Cash;
                case RunStatDisplay.Lives:
                    return runStats.Lives;
                case RunStatDisplay.Armour:
                    return runStats.Armour;
                case RunStatDisplay.Speed:
                    return runStats.EffectiveSpeedLevel;
                case RunStatDisplay.Bullets:
                    return runStats.EffectiveBulletsLevel;
                case RunStatDisplay.Time:
                    return runStats.EffectiveTimeLevel;
                default:
                    return 0;
            }
        }

        private void SetText(int value)
        {
            if (_text != null)
            {
                _text.text = string.Format(_format, value);
            }
        }
    }
}
