using System;
using UnityEngine;
using Warblade.Data;

namespace Warblade.Managers
{
    /// <summary>
    /// Owns active timed buffs for the current run.
    /// </summary>
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        [Header("Duration")]
        [SerializeField, Min(0f)] private float _autofireBaseDurationSeconds = 8f;
        [SerializeField, Min(0f)] private float _rapidFireBaseDurationSeconds = 8f;
        [SerializeField, Min(0f)] private float _shieldBaseDurationSeconds = 6f;
        [SerializeField, Min(0f)] private float _secondsPerTimeLevel = 2f;

        private readonly float[] _remainingSeconds = new float[Enum.GetValues(typeof(BuffType)).Length];
        private readonly float[] _durationSeconds = new float[Enum.GetValues(typeof(BuffType)).Length];

        public bool IsAutofireActive => IsActive(BuffType.Autofire);
        public bool IsRapidFireActive => IsActive(BuffType.RapidFire);
        public bool IsShieldActive => IsActive(BuffType.Shield);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ClearAllBuffs();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _remainingSeconds.Length; i++)
            {
                if (_remainingSeconds[i] <= 0f) continue;

                _remainingSeconds[i] = Mathf.Max(0f, _remainingSeconds[i] - deltaTime);

                if (_remainingSeconds[i] <= 0f)
                {
                    _durationSeconds[i] = 0f;
                    SyncShieldState();
                }
            }
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
            _autofireBaseDurationSeconds = Mathf.Max(0f, _autofireBaseDurationSeconds);
            _rapidFireBaseDurationSeconds = Mathf.Max(0f, _rapidFireBaseDurationSeconds);
            _shieldBaseDurationSeconds = Mathf.Max(0f, _shieldBaseDurationSeconds);
            _secondsPerTimeLevel = Mathf.Max(0f, _secondsPerTimeLevel);
        }

        /// <summary>
        /// Activates or refreshes a timed buff using the current effective Time level.
        /// </summary>
        public void ActivateBuff(BuffType buffType)
        {
            int index = GetIndex(buffType);
            float duration = CalculateDuration(buffType);
            if (duration <= 0f)
            {
                Debug.LogWarning($"[{nameof(BuffManager)}] '{buffType}' has no positive duration.");
                return;
            }

            _durationSeconds[index] = duration;
            _remainingSeconds[index] = duration;
            SyncShieldState();
        }

        /// <summary>
        /// Returns true while the requested timed buff has remaining duration.
        /// </summary>
        public bool IsActive(BuffType buffType)
        {
            return _remainingSeconds[GetIndex(buffType)] > 0f;
        }

        /// <summary>
        /// Returns remaining seconds for a timed buff.
        /// </summary>
        public float GetRemainingSeconds(BuffType buffType)
        {
            return _remainingSeconds[GetIndex(buffType)];
        }

        /// <summary>
        /// Returns the duration assigned when the current timed buff instance was activated.
        /// </summary>
        public float GetDurationSeconds(BuffType buffType)
        {
            return _durationSeconds[GetIndex(buffType)];
        }

        /// <summary>
        /// Clears every active timed buff, usually when the run restarts.
        /// </summary>
        public void ClearAllBuffs()
        {
            for (int i = 0; i < _remainingSeconds.Length; i++)
            {
                _remainingSeconds[i] = 0f;
                _durationSeconds[i] = 0f;
            }

            SyncShieldState();
        }

        private float CalculateDuration(BuffType buffType)
        {
            int effectiveTimeLevel = RunStatsManager.Instance != null
                ? RunStatsManager.Instance.EffectiveTimeLevel
                : 0;

            return GetBaseDuration(buffType) + _secondsPerTimeLevel * effectiveTimeLevel;
        }

        private float GetBaseDuration(BuffType buffType)
        {
            switch (buffType)
            {
                case BuffType.Autofire:
                    return _autofireBaseDurationSeconds;
                case BuffType.RapidFire:
                    return _rapidFireBaseDurationSeconds;
                case BuffType.Shield:
                    return _shieldBaseDurationSeconds;
                default:
                    return 0f;
            }
        }

        private int GetIndex(BuffType buffType)
        {
            return Mathf.Clamp((int)buffType, 0, _remainingSeconds.Length - 1);
        }

        private void SyncShieldState()
        {
            RunStatsManager.Instance?.SetShieldActive(IsShieldActive);
        }
    }
}
