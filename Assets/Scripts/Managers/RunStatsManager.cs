using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;

namespace Warblade.Managers
{
    /// <summary>
    /// Owns all mutable stats for the current run, including lives, cash, armour, weapon tier,
    /// and upgrade levels.
    /// </summary>
    public class RunStatsManager : MonoBehaviour
    {
        public static RunStatsManager Instance { get; private set; }

        [Header("Starting Values")]
        [SerializeField, Min(0)] private int _startingCash;
        [SerializeField, Min(1)] private int _startingLives = 3;
        [SerializeField, Min(0)] private int _startingArmour;
        [SerializeField] private WeaponTier _startingWeaponTier = WeaponTier.Single;
        [SerializeField, Min(0)] private int _startingSpeedLevel;
        [SerializeField, Min(0)] private int _startingBulletsLevel;
        [SerializeField, Min(0)] private int _startingTimeLevel;

        [Header("Caps")]
        [SerializeField, Min(0)] private int _maxCash = 100000;
        [SerializeField, Min(1)] private int _maxLives = 4;
        [SerializeField, Min(0)] private int _maxArmour = 2;
        [SerializeField, Min(0)] private int _maxSpeedLevel = 10;
        [SerializeField, Min(0)] private int _maxBulletsLevel = 10;
        [SerializeField, Min(0)] private int _maxTimeLevel = 10;

        [Header("Event Channels")]
        [SerializeField] private IntEventChannel _cashChanged;
        [SerializeField] private IntEventChannel _livesChanged;
        [SerializeField] private IntEventChannel _armourChanged;
        [SerializeField] private IntEventChannel _effectiveSpeedLevelChanged;
        [SerializeField] private IntEventChannel _effectiveBulletsLevelChanged;
        [SerializeField] private IntEventChannel _effectiveTimeLevelChanged;
        [SerializeField] private VoidEventChannel _runReset;

        [Header("Debug")]
        [SerializeField] private bool _debugShieldActive;

        private int _cash;
        private int _lives;
        private int _armour;
        private WeaponTier _weaponTier;
        private int _speedLevel;
        private int _bulletsLevel;
        private int _timeLevel;
        private bool _shieldActive;
        private bool _shopAutofireActive;

        public int Cash => _cash;
        public int MaxCash => _maxCash;
        public int Lives => _lives;
        public int MaxLives => _maxLives;
        public int Armour => _armour;
        public int MaxArmour => _maxArmour;
        public int MaxSpeedLevel => _maxSpeedLevel;
        public int MaxBulletsLevel => _maxBulletsLevel;
        public int MaxTimeLevel => _maxTimeLevel;
        public WeaponTier WeaponTier => _weaponTier;
        public int SpeedLevel => _speedLevel;
        public int BulletsLevel => _bulletsLevel;
        public int TimeLevel => _timeLevel;
        public int EffectiveSpeedLevel => _speedLevel;
        public int EffectiveBulletsLevel => _bulletsLevel;
        public int EffectiveTimeLevel => _timeLevel;
        public bool IsShieldActive => _shieldActive || _debugShieldActive;
        public bool IsShopAutofireActive => _shopAutofireActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResetRun();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            _maxCash = Mathf.Max(0, _maxCash);
            _startingCash = Mathf.Clamp(_startingCash, 0, _maxCash);
            _maxLives = Mathf.Max(1, _maxLives);
            _startingLives = Mathf.Clamp(_startingLives, 1, _maxLives);
            _maxArmour = Mathf.Max(0, _maxArmour);
            _startingArmour = Mathf.Clamp(_startingArmour, 0, _maxArmour);
            _startingSpeedLevel = Mathf.Clamp(_startingSpeedLevel, 0, _maxSpeedLevel);
            _startingBulletsLevel = Mathf.Clamp(_startingBulletsLevel, 0, _maxBulletsLevel);
            _startingTimeLevel = Mathf.Clamp(_startingTimeLevel, 0, _maxTimeLevel);
        }

        /// <summary>
        /// Adds cash to the current run.
        /// </summary>
        public void AddCash(int amount)
        {
            if (amount <= 0) return;
            int newCash = (int)System.Math.Min(_maxCash, (long)_cash + amount);
            if (_cash == newCash) return;

            _cash = newCash;
            RaiseCashChanged();
        }

        /// <summary>
        /// Attempts to spend cash, returning true only when the run has enough cash.
        /// </summary>
        public bool TrySpendCash(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[{nameof(RunStatsManager)}] Cannot spend a negative cash amount.");
                return false;
            }

            if (_cash < amount)
            {
                return false;
            }

            _cash -= amount;
            RaiseCashChanged();
            return true;
        }

        /// <summary>
        /// Adds one or more lives to the current run.
        /// </summary>
        public void AddLife(int amount = 1)
        {
            if (amount <= 0) return;
            int newLives = Mathf.Min(_maxLives, _lives + amount);
            if (_lives == newLives) return;

            _lives = newLives;
            RaiseLivesChanged();
        }

        /// <summary>
        /// Removes lives and returns true when the player is out of lives.
        /// </summary>
        public bool LoseLife(int amount = 1)
        {
            if (amount <= 0) return _lives <= 0;

            _lives = Mathf.Max(0, _lives - amount);
            RaiseLivesChanged();
            return _lives <= 0;
        }

        /// <summary>
        /// Adds armour up to the configured armour cap.
        /// </summary>
        public void AddArmour(int amount = 1)
        {
            if (amount <= 0) return;
            int newArmour = Mathf.Min(_maxArmour, _armour + amount);
            if (_armour == newArmour) return;

            _armour = newArmour;
            RaiseArmourChanged();
        }

        /// <summary>
        /// Consumes one armour point when available.
        /// </summary>
        public bool TryConsumeArmour()
        {
            if (_armour <= 0)
            {
                return false;
            }

            _armour--;
            RaiseArmourChanged();
            return true;
        }

        /// <summary>
        /// Sets the current weapon tier, clamped to the supported Warblade-style tiers.
        /// </summary>
        public void SetWeaponTier(WeaponTier weaponTier)
        {
            WeaponTier clampedTier = ClampWeaponTier(weaponTier);
            if (_weaponTier == clampedTier) return;

            _weaponTier = clampedTier;
        }

        /// <summary>
        /// Equips an exact weapon tier from a pickup. Duplicate pickups for the active tier grant one Bullets level.
        /// </summary>
        public void EquipWeaponTierFromPickup(WeaponTier weaponTier)
        {
            WeaponTier clampedTier = ClampPickupWeaponTier(weaponTier);
            if (_weaponTier == clampedTier)
            {
                IncreaseBullets();
                return;
            }

            SetWeaponTier(clampedTier);
        }

        /// <summary>
        /// Upgrades the weapon by one pickup tier, up to Quad. Shop-exclusive tiers are not granted here.
        /// </summary>
        public void UpgradeWeaponTier()
        {
            if (_weaponTier >= WeaponTier.Quad)
            {
                return;
            }

            SetWeaponTier((WeaponTier)((int)_weaponTier + 1));
        }

        /// <summary>
        /// Downgrades the weapon by one tier, never below Single.
        /// </summary>
        public void DowngradeWeaponTier()
        {
            SetWeaponTier((WeaponTier)((int)_weaponTier - 1));
        }

        /// <summary>
        /// Increases the Speed upgrade level up to its cap.
        /// </summary>
        public void IncreaseSpeed(int amount = 1)
        {
            if (amount <= 0) return;
            int previousEffectiveLevel = EffectiveSpeedLevel;
            int newLevel = Mathf.Min(_maxSpeedLevel, _speedLevel + amount);
            if (_speedLevel == newLevel) return;

            _speedLevel = newLevel;
            RaiseEffectiveSpeedLevelChangedIfNeeded(previousEffectiveLevel);
        }

        /// <summary>
        /// Reduces the Speed upgrade level without allowing movement below the player's base speed.
        /// </summary>
        public void DecreaseSpeed(int amount = 1)
        {
            if (amount <= 0) return;

            int previousEffectiveLevel = EffectiveSpeedLevel;
            _speedLevel = Mathf.Max(0, _speedLevel - amount);
            RaiseEffectiveSpeedLevelChangedIfNeeded(previousEffectiveLevel);
        }

        /// <summary>
        /// Increases the Bullets upgrade level up to its cap.
        /// </summary>
        public void IncreaseBullets(int amount = 1)
        {
            if (amount <= 0) return;
            int previousEffectiveLevel = EffectiveBulletsLevel;
            int newLevel = Mathf.Min(_maxBulletsLevel, _bulletsLevel + amount);
            if (_bulletsLevel == newLevel) return;

            _bulletsLevel = newLevel;
            RaiseEffectiveBulletsLevelChangedIfNeeded(previousEffectiveLevel);
        }

        /// <summary>
        /// Increases the Time upgrade level up to its cap.
        /// </summary>
        public void IncreaseTime(int amount = 1)
        {
            if (amount <= 0) return;
            int previousEffectiveLevel = EffectiveTimeLevel;
            int newLevel = Mathf.Min(_maxTimeLevel, _timeLevel + amount);
            if (_timeLevel == newLevel) return;

            _timeLevel = newLevel;
            RaiseEffectiveTimeLevelChangedIfNeeded(previousEffectiveLevel);
        }

        /// <summary>
        /// Applies one random permanent sucker penalty and downgrades the weapon tier.
        /// </summary>
        public RunStatType ApplySuckerPenalty()
        {
            RunStatType statType = (RunStatType)Random.Range(0, 3);
            ApplySuckerPenalty(statType);
            return statType;
        }

        /// <summary>
        /// Applies a specific permanent sucker penalty and downgrades the weapon tier.
        /// </summary>
        public void ApplySuckerPenalty(RunStatType statType)
        {
            DowngradeWeaponTier();
            DecreaseStatLevel(statType);
        }

        /// <summary>
        /// Sets whether the timed shield buff should currently block player damage.
        /// </summary>
        public void SetShieldActive(bool isActive)
        {
            _shieldActive = isActive;
        }

        /// <summary>
        /// Enables shop-purchased autofire for the player's current life.
        /// </summary>
        public void EnableShopAutofire()
        {
            _shopAutofireActive = true;
        }

        /// <summary>
        /// Removes shop-purchased autofire when the player dies.
        /// </summary>
        public void ClearShopAutofire()
        {
            _shopAutofireActive = false;
        }

        private void DecreaseStatLevel(RunStatType statType, int amount = 1)
        {
            if (amount <= 0) return;

            switch (statType)
            {
                case RunStatType.Speed:
                    int previousEffectiveSpeedLevel = EffectiveSpeedLevel;
                    _speedLevel = Mathf.Max(0, _speedLevel - amount);
                    RaiseEffectiveSpeedLevelChangedIfNeeded(previousEffectiveSpeedLevel);
                    break;
                case RunStatType.Bullets:
                    int previousEffectiveBulletsLevel = EffectiveBulletsLevel;
                    _bulletsLevel = Mathf.Max(0, _bulletsLevel - amount);
                    RaiseEffectiveBulletsLevelChangedIfNeeded(previousEffectiveBulletsLevel);
                    break;
                case RunStatType.Time:
                    int previousEffectiveTimeLevel = EffectiveTimeLevel;
                    _timeLevel = Mathf.Max(0, _timeLevel - amount);
                    RaiseEffectiveTimeLevelChangedIfNeeded(previousEffectiveTimeLevel);
                    break;
                default:
                    Debug.LogWarning($"[{nameof(RunStatsManager)}] Unknown run stat type: {statType}.");
                    break;
            }
        }

        /// <summary>
        /// Resets all run-only stats to their configured starting values.
        /// </summary>
        public void ResetRun()
        {
            _cash = Mathf.Clamp(_startingCash, 0, _maxCash);
            _lives = Mathf.Clamp(_startingLives, 1, _maxLives);
            _armour = Mathf.Clamp(_startingArmour, 0, _maxArmour);
            _weaponTier = ClampWeaponTier(_startingWeaponTier);
            _speedLevel = Mathf.Clamp(_startingSpeedLevel, 0, _maxSpeedLevel);
            _bulletsLevel = Mathf.Clamp(_startingBulletsLevel, 0, _maxBulletsLevel);
            _timeLevel = Mathf.Clamp(_startingTimeLevel, 0, _maxTimeLevel);
            _shieldActive = false;
            _shopAutofireActive = false;
            RaiseAllChanged();
            _runReset?.Raise();
        }

        private WeaponTier ClampWeaponTier(WeaponTier weaponTier)
        {
            int tier = Mathf.Clamp((int)weaponTier, (int)WeaponTier.Single, (int)WeaponTier.FireBall);
            return (WeaponTier)tier;
        }

        private WeaponTier ClampPickupWeaponTier(WeaponTier weaponTier)
        {
            int tier = Mathf.Clamp((int)weaponTier, (int)WeaponTier.Single, (int)WeaponTier.Quad);
            return (WeaponTier)tier;
        }

        private void RaiseAllChanged()
        {
            RaiseCashChanged();
            RaiseLivesChanged();
            RaiseArmourChanged();
            RaiseEffectiveSpeedLevelChanged();
            RaiseEffectiveBulletsLevelChanged();
            RaiseEffectiveTimeLevelChanged();
        }

        private void RaiseCashChanged() => _cashChanged?.Raise(_cash);

        private void RaiseLivesChanged() => _livesChanged?.Raise(_lives);

        private void RaiseArmourChanged() => _armourChanged?.Raise(_armour);

        private void RaiseEffectiveSpeedLevelChanged() => _effectiveSpeedLevelChanged?.Raise(EffectiveSpeedLevel);

        private void RaiseEffectiveBulletsLevelChanged() => _effectiveBulletsLevelChanged?.Raise(EffectiveBulletsLevel);

        private void RaiseEffectiveTimeLevelChanged() => _effectiveTimeLevelChanged?.Raise(EffectiveTimeLevel);

        private void RaiseEffectiveSpeedLevelChangedIfNeeded(int previousEffectiveLevel)
        {
            if (EffectiveSpeedLevel != previousEffectiveLevel)
            {
                RaiseEffectiveSpeedLevelChanged();
            }
        }

        private void RaiseEffectiveBulletsLevelChangedIfNeeded(int previousEffectiveLevel)
        {
            if (EffectiveBulletsLevel != previousEffectiveLevel)
            {
                RaiseEffectiveBulletsLevelChanged();
            }
        }

        private void RaiseEffectiveTimeLevelChangedIfNeeded(int previousEffectiveLevel)
        {
            if (EffectiveTimeLevel != previousEffectiveLevel)
            {
                RaiseEffectiveTimeLevelChanged();
            }
        }
    }
}
