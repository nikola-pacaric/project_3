using UnityEngine;

namespace Warblade.Data
{
    public readonly struct CycleScalingState
    {
        public CycleScalingState(
            int cycleNumber,
            int completedCycles,
            float enemyHealthMultiplier,
            float enemySpeedMultiplier,
            float bossHealthMultiplier,
            float bossPressureMultiplier,
            Color tintColor,
            float tintStrength)
        {
            CycleNumber = cycleNumber;
            CompletedCycles = completedCycles;
            EnemyHealthMultiplier = enemyHealthMultiplier;
            EnemySpeedMultiplier = enemySpeedMultiplier;
            BossHealthMultiplier = bossHealthMultiplier;
            BossPressureMultiplier = bossPressureMultiplier;
            TintColor = tintColor;
            TintStrength = tintStrength;
        }

        public static CycleScalingState Default => CycleScalingData.ResolveDefault(1);

        public int CycleNumber { get; }
        public int CompletedCycles { get; }
        public float EnemyHealthMultiplier { get; }
        public float EnemySpeedMultiplier { get; }
        public float BossHealthMultiplier { get; }
        public float BossPressureMultiplier { get; }
        public Color TintColor { get; }
        public float TintStrength { get; }
        public bool IsScaledCycle => CompletedCycles > 0;
    }

    [CreateAssetMenu(menuName = "Warblade/Data/Cycle Scaling Data", fileName = "CycleScalingData")]
    public class CycleScalingData : ScriptableObject
    {
        public const int LevelsPerCycle = 100;

        private const float DefaultEnemyHealthIncreasePerCycle = 0.25f;
        private const float DefaultEnemySpeedIncreasePerCycle = 0.10f;
        private const float DefaultBossHealthIncreasePerCycle = 0.35f;
        private const float DefaultBossPressureIncreasePerCycle = 0.10f;
        private const float DefaultTintStrengthPerCycle = 0.18f;
        private const float DefaultMaxTintStrength = 0.45f;

        [Header("Enemy Scaling")]
        [SerializeField, Min(0f)] private float _enemyHealthIncreasePerCompletedCycle = DefaultEnemyHealthIncreasePerCycle;
        [SerializeField, Min(0f)] private float _enemySpeedIncreasePerCompletedCycle = DefaultEnemySpeedIncreasePerCycle;

        [Header("Boss Scaling")]
        [SerializeField, Min(0f)] private float _bossHealthIncreasePerCompletedCycle = DefaultBossHealthIncreasePerCycle;
        [SerializeField, Min(0f)] private float _bossPressureIncreasePerCompletedCycle = DefaultBossPressureIncreasePerCycle;

        [Header("Visuals")]
        [SerializeField] private Color _cycleTint = new Color(0.35f, 0.85f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float _tintStrengthPerCompletedCycle = DefaultTintStrengthPerCycle;
        [SerializeField, Range(0f, 1f)] private float _maxTintStrength = DefaultMaxTintStrength;

        /// <summary>
        /// Returns cycle scaling for the provided absolute campaign level.
        /// </summary>
        public CycleScalingState Resolve(int levelNumber)
        {
            return Resolve(
                levelNumber,
                _enemyHealthIncreasePerCompletedCycle,
                _enemySpeedIncreasePerCompletedCycle,
                _bossHealthIncreasePerCompletedCycle,
                _bossPressureIncreasePerCompletedCycle,
                _cycleTint,
                _tintStrengthPerCompletedCycle,
                _maxTintStrength);
        }

        public static CycleScalingState ResolveDefault(int levelNumber)
        {
            return Resolve(
                levelNumber,
                DefaultEnemyHealthIncreasePerCycle,
                DefaultEnemySpeedIncreasePerCycle,
                DefaultBossHealthIncreasePerCycle,
                DefaultBossPressureIncreasePerCycle,
                new Color(0.35f, 0.85f, 1f, 1f),
                DefaultTintStrengthPerCycle,
                DefaultMaxTintStrength);
        }

        public static int GetCycleIndexForLevel(int levelNumber)
        {
            return (Mathf.Max(1, levelNumber) - 1) / LevelsPerCycle;
        }

        public static int GetCycleNumberForLevel(int levelNumber)
        {
            return GetCycleIndexForLevel(levelNumber) + 1;
        }

        public static int GetCampaignLevelForCycle(int levelNumber)
        {
            return ((Mathf.Max(1, levelNumber) - 1) % LevelsPerCycle) + 1;
        }

        private static CycleScalingState Resolve(
            int levelNumber,
            float enemyHealthIncreasePerCompletedCycle,
            float enemySpeedIncreasePerCompletedCycle,
            float bossHealthIncreasePerCompletedCycle,
            float bossPressureIncreasePerCompletedCycle,
            Color cycleTint,
            float tintStrengthPerCompletedCycle,
            float maxTintStrength)
        {
            int completedCycles = GetCycleIndexForLevel(levelNumber);
            int cycleNumber = completedCycles + 1;
            float tintStrength = Mathf.Min(maxTintStrength, completedCycles * tintStrengthPerCompletedCycle);

            return new CycleScalingState(
                cycleNumber,
                completedCycles,
                1f + completedCycles * enemyHealthIncreasePerCompletedCycle,
                1f + completedCycles * enemySpeedIncreasePerCompletedCycle,
                1f + completedCycles * bossHealthIncreasePerCompletedCycle,
                1f + completedCycles * bossPressureIncreasePerCompletedCycle,
                cycleTint,
                tintStrength);
        }

        private void OnValidate()
        {
            _enemyHealthIncreasePerCompletedCycle = Mathf.Max(0f, _enemyHealthIncreasePerCompletedCycle);
            _enemySpeedIncreasePerCompletedCycle = Mathf.Max(0f, _enemySpeedIncreasePerCompletedCycle);
            _bossHealthIncreasePerCompletedCycle = Mathf.Max(0f, _bossHealthIncreasePerCompletedCycle);
            _bossPressureIncreasePerCompletedCycle = Mathf.Max(0f, _bossPressureIncreasePerCompletedCycle);
            _maxTintStrength = Mathf.Max(_tintStrengthPerCompletedCycle, _maxTintStrength);
        }
    }
}
