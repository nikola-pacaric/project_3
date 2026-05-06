using UnityEngine;
using Warblade.Data;

namespace Warblade.Managers
{
    /// <summary>
    /// Owns all mutable stats for the current run, including lives, cash, armour, weapon tier,
    /// upgrade levels, and temporary current-level sucker penalties.
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
        [SerializeField, Min(0)] private int _maxArmour = 2;
        [SerializeField, Min(0)] private int _maxSpeedLevel = 5;
        [SerializeField, Min(0)] private int _maxBulletsLevel = 5;
        [SerializeField, Min(0)] private int _maxTimeLevel = 5;

        private int _cash;
        private int _lives;
        private int _armour;
        private WeaponTier _weaponTier;
        private int _speedLevel;
        private int _bulletsLevel;
        private int _timeLevel;
        private int _temporarySpeedDebuff;
        private int _temporaryBulletsDebuff;
        private int _temporaryTimeDebuff;

        public int Cash => _cash;
        public int Lives => _lives;
        public int Armour => _armour;
        public int MaxArmour => _maxArmour;
        public WeaponTier WeaponTier => _weaponTier;
        public int SpeedLevel => _speedLevel;
        public int BulletsLevel => _bulletsLevel;
        public int TimeLevel => _timeLevel;
        public int TemporarySpeedDebuff => _temporarySpeedDebuff;
        public int TemporaryBulletsDebuff => _temporaryBulletsDebuff;
        public int TemporaryTimeDebuff => _temporaryTimeDebuff;
        public int EffectiveSpeedLevel => Mathf.Max(0, _speedLevel - _temporarySpeedDebuff);
        public int EffectiveBulletsLevel => Mathf.Max(0, _bulletsLevel - _temporaryBulletsDebuff);
        public int EffectiveTimeLevel => Mathf.Max(0, _timeLevel - _temporaryTimeDebuff);

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
            _cash += amount;
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
            return true;
        }

        /// <summary>
        /// Adds one or more lives to the current run.
        /// </summary>
        public void AddLife(int amount = 1)
        {
            if (amount <= 0) return;
            _lives += amount;
        }

        /// <summary>
        /// Removes lives and returns true when the player is out of lives.
        /// </summary>
        public bool LoseLife(int amount = 1)
        {
            if (amount <= 0) return _lives <= 0;

            _lives = Mathf.Max(0, _lives - amount);
            return _lives <= 0;
        }

        /// <summary>
        /// Adds armour up to the configured armour cap.
        /// </summary>
        public void AddArmour(int amount = 1)
        {
            if (amount <= 0) return;
            _armour = Mathf.Min(_maxArmour, _armour + amount);
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
            return true;
        }

        /// <summary>
        /// Sets the current weapon tier, clamped to the supported Warblade-style tiers.
        /// </summary>
        public void SetWeaponTier(WeaponTier weaponTier)
        {
            _weaponTier = ClampWeaponTier(weaponTier);
        }

        /// <summary>
        /// Upgrades the weapon by one tier, up to Quad.
        /// </summary>
        public void UpgradeWeaponTier()
        {
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
            _speedLevel = Mathf.Min(_maxSpeedLevel, _speedLevel + amount);
        }

        /// <summary>
        /// Increases the Bullets upgrade level up to its cap.
        /// </summary>
        public void IncreaseBullets(int amount = 1)
        {
            if (amount <= 0) return;
            _bulletsLevel = Mathf.Min(_maxBulletsLevel, _bulletsLevel + amount);
        }

        /// <summary>
        /// Increases the Time upgrade level up to its cap.
        /// </summary>
        public void IncreaseTime(int amount = 1)
        {
            if (amount <= 0) return;
            _timeLevel = Mathf.Min(_maxTimeLevel, _timeLevel + amount);
        }

        /// <summary>
        /// Applies one random current-level sucker penalty and downgrades the weapon tier.
        /// </summary>
        public RunStatType ApplySuckerPenalty()
        {
            DowngradeWeaponTier();
            RunStatType statType = (RunStatType)Random.Range(0, 3);
            ApplyTemporaryDebuff(statType);
            return statType;
        }

        /// <summary>
        /// Applies one temporary current-level debuff to a specific stat.
        /// </summary>
        public void ApplyTemporaryDebuff(RunStatType statType, int amount = 1)
        {
            if (amount <= 0) return;

            switch (statType)
            {
                case RunStatType.Speed:
                    _temporarySpeedDebuff += amount;
                    break;
                case RunStatType.Bullets:
                    _temporaryBulletsDebuff += amount;
                    break;
                case RunStatType.Time:
                    _temporaryTimeDebuff += amount;
                    break;
                default:
                    Debug.LogWarning($"[{nameof(RunStatsManager)}] Unknown run stat type: {statType}.");
                    break;
            }
        }

        /// <summary>
        /// Clears sucker penalties that only last for the current level.
        /// </summary>
        public void ClearCurrentLevelDebuffs()
        {
            _temporarySpeedDebuff = 0;
            _temporaryBulletsDebuff = 0;
            _temporaryTimeDebuff = 0;
        }

        /// <summary>
        /// Resets all run-only stats to their configured starting values.
        /// </summary>
        public void ResetRun()
        {
            _cash = Mathf.Max(0, _startingCash);
            _lives = Mathf.Max(1, _startingLives);
            _armour = Mathf.Clamp(_startingArmour, 0, _maxArmour);
            _weaponTier = ClampWeaponTier(_startingWeaponTier);
            _speedLevel = Mathf.Clamp(_startingSpeedLevel, 0, _maxSpeedLevel);
            _bulletsLevel = Mathf.Clamp(_startingBulletsLevel, 0, _maxBulletsLevel);
            _timeLevel = Mathf.Clamp(_startingTimeLevel, 0, _maxTimeLevel);
            ClearCurrentLevelDebuffs();
        }

        private WeaponTier ClampWeaponTier(WeaponTier weaponTier)
        {
            int tier = Mathf.Clamp((int)weaponTier, (int)WeaponTier.Single, (int)WeaponTier.Quad);
            return (WeaponTier)tier;
        }
    }
}
