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
        [SerializeField] private bool _showBar;
        [SerializeField, Min(1)] private int _barWidth = 10;
        [SerializeField] private string _filledBarCharacter = "#";
        [SerializeField] private string _emptyBarCharacter = "-";

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
            Refresh();
        }

        private void Refresh()
        {
            RunStatsManager runStats = RunStatsManager.Instance;
            if (runStats == null) return;

            SetText(runStats, GetCurrentValue(runStats));
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

        private int GetMaxValue(RunStatsManager runStats)
        {
            switch (_stat)
            {
                case RunStatDisplay.Armour:
                    return runStats.MaxArmour;
                case RunStatDisplay.Speed:
                    return runStats.MaxSpeedLevel;
                case RunStatDisplay.Bullets:
                    return runStats.MaxBulletsLevel;
                case RunStatDisplay.Time:
                    return runStats.MaxTimeLevel;
                default:
                    return 0;
            }
        }

        private void SetText(RunStatsManager runStats, int value)
        {
            if (_text != null)
            {
                int maxValue = GetMaxValue(runStats);
                string bar = _showBar && maxValue > 0
                    ? BuildBar(value, maxValue)
                    : string.Empty;

                _text.text = string.Format(_format, value, maxValue, bar);
            }
        }

        private string BuildBar(int value, int maxValue)
        {
            int clampedMax = Mathf.Max(1, maxValue);
            int filledCount = Mathf.RoundToInt(Mathf.Clamp01((float)value / clampedMax) * _barWidth);

            return new string(GetBarCharacter(_filledBarCharacter), filledCount)
                + new string(GetBarCharacter(_emptyBarCharacter), _barWidth - filledCount);
        }

        private char GetBarCharacter(string value)
        {
            return string.IsNullOrEmpty(value) ? '-' : value[0];
        }
    }
}
