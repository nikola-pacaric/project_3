using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Entities;
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
        [SerializeField] private Image _fillImage;
        [SerializeField] private PlayerShooting _playerShooting;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private BuffType _timeDurationBuffType = BuffType.Autofire;
        [SerializeField] private string _format = "{0}";
        [SerializeField] private bool _showBar;
        [SerializeField, Min(1)] private int _barWidth = 10;
        [SerializeField] private string _filledBarCharacter = "#";
        [SerializeField] private string _emptyBarCharacter = "-";

        private void Awake()
        {
            Refresh();
        }

        private void Start()
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

            int currentValue = GetCurrentValue(runStats);
            int maxValue = GetMaxValue(runStats);

            SetText(currentValue, maxValue);
            SetFill(currentValue, maxValue);
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
                case RunStatDisplay.Lives:
                    return runStats.MaxLives;
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

        private void SetText(int value, int maxValue)
        {
            if (_text != null)
            {
                string bar = _showBar && maxValue > 0
                    ? BuildBar(value, maxValue)
                    : string.Empty;

                _text.text = string.Format(_format, value, maxValue, bar);
            }
        }

        private void SetFill(int value, int maxValue)
        {
            if (_fillImage == null)
            {
                return;
            }

            GetFillValues(value, maxValue, out float totalValue, out float totalMaxValue);

            _fillImage.fillAmount = totalMaxValue <= 0
                ? 0f
                : Mathf.Clamp01(totalValue / totalMaxValue);
        }

        private void GetFillValues(int value, int maxValue, out float totalValue, out float totalMaxValue)
        {
            switch (_stat)
            {
                case RunStatDisplay.Bullets:
                    GetBulletFillValues(value, maxValue, out totalValue, out totalMaxValue);
                    return;

                case RunStatDisplay.Speed:
                    GetSpeedFillValues(value, maxValue, out totalValue, out totalMaxValue);
                    return;

                case RunStatDisplay.Time:
                    GetTimeFillValues(value, maxValue, out totalValue, out totalMaxValue);
                    return;

                default:
                    totalValue = value;
                    totalMaxValue = maxValue;
                    return;
            }
        }

        private void GetBulletFillValues(int value, int maxValue, out float totalValue, out float totalMaxValue)
        {
            if (_playerShooting == null)
            {
                totalValue = value;
                totalMaxValue = maxValue;
                return;
            }

            int baseMaxActiveBullets = _playerShooting.BaseMaxActiveBullets;
            totalValue = baseMaxActiveBullets + value;
            totalMaxValue = baseMaxActiveBullets + maxValue;
        }

        private void GetSpeedFillValues(int value, int maxValue, out float totalValue, out float totalMaxValue)
        {
            PlayerMovement playerMovement = ResolvePlayerMovement();
            if (playerMovement == null)
            {
                totalValue = value;
                totalMaxValue = maxValue;
                return;
            }

            totalValue = playerMovement.BaseSpeed + value * playerMovement.SpeedPerSpeedLevel;
            totalMaxValue = playerMovement.BaseSpeed + maxValue * playerMovement.SpeedPerSpeedLevel;
        }

        private void GetTimeFillValues(int value, int maxValue, out float totalValue, out float totalMaxValue)
        {
            BuffManager buffManager = BuffManager.Instance;
            if (buffManager == null)
            {
                totalValue = value;
                totalMaxValue = maxValue;
                return;
            }

            float baseDuration = buffManager.GetBaseDurationSeconds(_timeDurationBuffType);
            float secondsPerLevel = buffManager.SecondsPerTimeLevel;
            totalValue = baseDuration + value * secondsPerLevel;
            totalMaxValue = baseDuration + maxValue * secondsPerLevel;
        }

        private PlayerMovement ResolvePlayerMovement()
        {
            if (_playerMovement != null)
            {
                return _playerMovement;
            }

            return _playerShooting != null
                ? _playerShooting.GetComponent<PlayerMovement>()
                : null;
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
