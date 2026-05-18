using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Entities;

namespace Warblade.Systems
{
    /// <summary>
    /// Spawns enemies from formation data and pools instances per enemy prefab.
    /// Phase 7+: data-driven path only.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct TestFormationSpawn
        {
            public Formation Formation;
            public Enemy EnemyPrefab;
            public EnemyData[] EnemyComposition;
            public Vector2 EntryStartCenter;
            [Min(0f)] public float EntryStartSpacingX;
            [Min(0f)] public float Delay;
            [Min(0f)] public float PerSlotDelay;
        }

        [SerializeField] private Transform _playerTransform;
        [SerializeField] private TestFormationSpawn[] _testFormationSpawns;
        [SerializeField, Min(0)] private int _debugFormationIndex;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxSize = 50;
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField, Min(1f)] private float _specialPerfectClearScoreMultiplier = 2f;
        [Header("Dive Pacing")]
        [SerializeField, Range(0f, 1f)] private float _concurrentDiveLimitRemainingRatio = 0.1f;
        [SerializeField, Min(0f)] private float _limitedDiveCooldownMin = 1.25f;
        [SerializeField, Min(0f)] private float _limitedDiveCooldownMax = 3.5f;

        private readonly Dictionary<Enemy, IObjectPool<Enemy>> _pools =
            new Dictionary<Enemy, IObjectPool<Enemy>>();
        private readonly List<Enemy> _activeEnemies = new List<Enemy>();
        private int _activeEnemyCount;
        private int _spawnedEnemyCount;
        private bool _finalDiveTriggered;
        private int _specialEnemyCount;
        private int _specialKilledScore;
        private bool _specialEnemyEscaped;
        private bool _specialPerfectClearBonusConsumed;
        private Enemy _limitedDiveEnemy;
        private float _nextLimitedDiveAllowedTime;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;

        public int ActiveEnemyCount => _activeEnemyCount;
        public int SpawnedEnemyCount => _spawnedEnemyCount;
        public bool IsFinalDivePressureActive => _finalDiveTriggered;

        private void Start()
        {
            if (_testFormationSpawns == null) return;

            for (int i = 0; i < _testFormationSpawns.Length; i++)
            {
                StartCoroutine(SpawnFormationAfterDelay(_testFormationSpawns[i]));
            }
        }

        public Enemy Spawn(Enemy prefab, Vector2 startPosition, Vector2 formationPosition)
        {
            return Spawn(prefab, startPosition, formationPosition, null, -1, prefab != null ? prefab.EntryControlOffset : Vector2.zero, null);
        }

        public Enemy Spawn(
            Enemy prefab,
            Vector2 startPosition,
            Vector2 formationPosition,
            Formation formation,
            int formationSlotIndex)
        {
            return Spawn(
                prefab,
                startPosition,
                formationPosition,
                formation,
                formationSlotIndex,
                prefab != null ? prefab.EntryControlOffset : Vector2.zero,
                null);
        }

        public Enemy Spawn(
            Enemy prefab,
            Vector2 startPosition,
            Vector2 formationPosition,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset)
        {
            return Spawn(prefab, startPosition, formationPosition, formation, formationSlotIndex, entryControlOffset, null);
        }

        public Enemy Spawn(
            Enemy prefab,
            Vector2 startPosition,
            Vector2 formationPosition,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints)
        {
            return Spawn(prefab, startPosition, formationPosition, formation, formationSlotIndex, entryControlOffset, entryPathPoints, null);
        }

        public Enemy Spawn(
            Enemy prefab,
            Vector2 startPosition,
            Vector2 formationPosition,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints,
            Vector2[] entryPathControlPoints)
        {
            return Spawn(
                prefab,
                null,
                startPosition,
                formationPosition,
                formation,
                formationSlotIndex,
                entryControlOffset,
                entryPathPoints,
                entryPathControlPoints);
        }

        public Enemy Spawn(
            Enemy prefab,
            EnemyData enemyData,
            Vector2 startPosition,
            Vector2 formationPosition,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints,
            Vector2[] entryPathControlPoints)
        {
            if (prefab == null) return null;

            IObjectPool<Enemy> pool = GetOrCreatePool(prefab);
            Enemy enemy = pool.Get();
            _spawnedEnemyCount++;
            EnemyData resolvedEnemyData = enemyData != null ? enemyData : prefab.Data;
            if (resolvedEnemyData != null && resolvedEnemyData.CountsForPerfectClearBonus)
            {
                _specialEnemyCount++;
            }

            enemy.Spawn(
                startPosition,
                formationPosition,
                _playerTransform,
                formation,
                formationSlotIndex,
                entryControlOffset,
                entryPathPoints,
                entryPathControlPoints,
                resolvedEnemyData,
                _cycleScaling);
            return enemy;
        }

        /// <summary>
        /// Spawns a full formation using a start center and a per-slot spacing step vector.
        /// </summary>
        public Coroutine SpawnFormation(
            WaveData wave,
            Vector2 entryStartCenter,
            Vector2 entryStartSpacingStep,
            float perSlotDelay,
            float delay = 0f,
            Formation runtimeFormation = null)
        {
            return StartCoroutine(SpawnFormationAfterDelay(
                wave,
                entryStartCenter,
                entryStartSpacingStep,
                perSlotDelay,
                delay,
                runtimeFormation));
        }

        [ContextMenu("Spawn Debug Formation")]
        public void SpawnDebugFormation()
        {
            if (_testFormationSpawns == null || _testFormationSpawns.Length == 0)
            {
                Debug.LogWarning($"[{nameof(EnemySpawner)}] No formation test spawns configured.");
                return;
            }

            int clampedIndex = Mathf.Clamp(_debugFormationIndex, 0, _testFormationSpawns.Length - 1);
            TestFormationSpawn debugSpawn = _testFormationSpawns[clampedIndex];
            StartCoroutine(SpawnFormationRoutine(
                debugSpawn.Formation,
                debugSpawn.EnemyPrefab,
                debugSpawn.EnemyComposition,
                debugSpawn.EntryStartCenter,
                Vector2.right * debugSpawn.EntryStartSpacingX,
                debugSpawn.PerSlotDelay));
        }

        private IEnumerator SpawnFormationAfterDelay(TestFormationSpawn spawn)
        {
            if (spawn.Delay > 0f) yield return new WaitForSeconds(spawn.Delay);
            yield return SpawnFormationRoutine(
                spawn.Formation,
                spawn.EnemyPrefab,
                spawn.EnemyComposition,
                spawn.EntryStartCenter,
                Vector2.right * spawn.EntryStartSpacingX,
                spawn.PerSlotDelay);
        }

        private IEnumerator SpawnFormationAfterDelay(
            WaveData wave,
            Vector2 entryStartCenter,
            Vector2 entryStartSpacingStep,
            float perSlotDelay,
            float delay,
            Formation runtimeFormation)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return SpawnFormationRoutine(
                wave,
                entryStartCenter,
                entryStartSpacingStep,
                perSlotDelay,
                runtimeFormation);
        }

        private IEnumerator SpawnFormationRoutine(
            WaveData wave,
            Vector2 entryStartCenter,
            Vector2 entryStartSpacingStep,
            float perSlotDelay,
            Formation runtimeFormation = null)
        {
            if (wave == null) yield break;
            if (wave.SlotCount <= 0)
            {
                Debug.LogWarning($"[{nameof(EnemySpawner)}] Wave '{wave.name}' has no slots.");
                yield break;
            }

            Enemy prefab = wave.EnemyPrefab;
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(EnemySpawner)}] Wave '{wave.name}' has no enemy prefab.");
                yield break;
            }

            float halfSpan = (wave.SlotCount - 1) * 0.5f;
            for (int slotIndex = 0; slotIndex < wave.SlotCount; slotIndex++)
            {
                WaveData.WaveSlot slot = wave.GetSlot(slotIndex);
                EnemyData enemyData = slot.EnemyData;
                if (enemyData == null)
                {
                    Debug.LogError(
                        $"[{nameof(EnemySpawner)}] Slot {slotIndex} in wave '{wave.name}' has no EnemyData.");
                    if (perSlotDelay > 0f) yield return new WaitForSeconds(perSlotDelay);
                    continue;
                }

                Vector2 spacedStartPosition = entryStartCenter + entryStartSpacingStep * (slotIndex - halfSpan);
                Vector2 startPosition = wave.UsesWaypointEntryPath ? entryStartCenter : spacedStartPosition;
                Vector2 formationPosition = runtimeFormation != null && runtimeFormation.HasSlot(slotIndex)
                    ? runtimeFormation.GetSlotWorldPosition(slotIndex)
                    : wave.GetSlotWorldPosition(slotIndex);
                Vector2 entryControlOffset = slot.EntryControlOffset;
                Vector2[] entryPathPoints = wave.BuildEntryPathWorldPoints(startPosition, formationPosition);
                Vector2[] entryPathControlPoints = wave.BuildEntryPathWorldControlPoints(entryPathPoints);

                Spawn(prefab, enemyData, startPosition, formationPosition, runtimeFormation, slotIndex, entryControlOffset, entryPathPoints, entryPathControlPoints);

                if (perSlotDelay > 0f)
                {
                    yield return new WaitForSeconds(perSlotDelay);
                }
            }
        }

        private IEnumerator SpawnFormationRoutine(
            Formation formation,
            Enemy prefab,
            EnemyData[] enemyComposition,
            Vector2 entryStartCenter,
            Vector2 entryStartSpacingStep,
            float perSlotDelay)
        {
            if (formation == null) yield break;
            if (formation.SlotCount <= 0)
            {
                Debug.LogWarning($"[{nameof(EnemySpawner)}] Formation '{formation.name}' has no slots.");
                yield break;
            }

            if (prefab == null)
            {
                Debug.LogError($"[{nameof(EnemySpawner)}] Formation '{formation.name}' has no enemy prefab.");
                yield break;
            }

            if (enemyComposition == null || enemyComposition.Length == 0)
            {
                Debug.LogError($"[{nameof(EnemySpawner)}] Formation '{formation.name}' has no enemy composition.");
                yield break;
            }

            int slotCount = Mathf.Min(formation.SlotCount, enemyComposition.Length);
            float halfSpan = (slotCount - 1) * 0.5f;
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                EnemyData enemyData = enemyComposition[slotIndex];
                if (enemyData == null)
                {
                    Debug.LogError(
                        $"[{nameof(EnemySpawner)}] Slot {slotIndex} in formation '{formation.name}' has no EnemyData.");
                    if (perSlotDelay > 0f) yield return new WaitForSeconds(perSlotDelay);
                    continue;
                }

                Vector2 startPosition = entryStartCenter + entryStartSpacingStep * (slotIndex - halfSpan);
                Vector2 formationPosition = formation.GetSlotWorldPosition(slotIndex);
                Vector2 entryControlOffset = formation.GetSlotEntryControlOffset(slotIndex);

                Spawn(prefab, enemyData, startPosition, formationPosition, formation, slotIndex, entryControlOffset, null, null);

                if (perSlotDelay > 0f)
                {
                    yield return new WaitForSeconds(perSlotDelay);
                }
            }
        }

        private IObjectPool<Enemy> GetOrCreatePool(Enemy prefab)
        {
            if (_pools.TryGetValue(prefab, out IObjectPool<Enemy> existing)) return existing;

            Enemy capturedPrefab = prefab;
            IObjectPool<Enemy> pool = null;
            pool = new ObjectPool<Enemy>(
                createFunc: () =>
                {
                    Enemy enemy = Instantiate(capturedPrefab);
                    enemy.SetPool(pool);
                    enemy.SetSpawner(this);
                    enemy.Released += HandleEnemyReleased;
                    return enemy;
                },
                actionOnGet: e =>
                {
                    _activeEnemyCount++;
                    _activeEnemies.Add(e);
                    e.gameObject.SetActive(true);
                },
                actionOnRelease: e =>
                {
                    _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
                    _activeEnemies.Remove(e);
                    e.gameObject.SetActive(false);
                },
                actionOnDestroy: e => Destroy(e.gameObject),
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize);

            _pools[prefab] = pool;
            return pool;
        }

        public void BeginLevelEnemyTracking()
        {
            ClearActiveEnemies();
            _spawnedEnemyCount = 0;
            _finalDiveTriggered = false;
            _specialEnemyCount = 0;
            _specialKilledScore = 0;
            _specialEnemyEscaped = false;
            _specialPerfectClearBonusConsumed = false;
            _limitedDiveEnemy = null;
            _nextLimitedDiveAllowedTime = 0f;
            _activeEnemies.Clear();
            _activeEnemyCount = 0;
        }

        /// <summary>
        /// Despawns every currently tracked enemy before a forced level change or reset.
        /// </summary>
        public void ClearActiveEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _activeEnemies[i];
                if (enemy != null)
                {
                    enemy.DespawnForLevelReset();
                }
            }

            _activeEnemies.Clear();
            _activeEnemyCount = 0;
            _limitedDiveEnemy = null;
            _nextLimitedDiveAllowedTime = 0f;
        }

        /// <summary>
        /// Applies runtime cycle modifiers to enemies spawned after this call.
        /// </summary>
        public void SetCycleScaling(CycleScalingState cycleScaling)
        {
            _cycleScaling = cycleScaling;
        }

        public int ConsumeSpecialPerfectClearBonusScore()
        {
            if (_specialPerfectClearBonusConsumed ||
                _specialEnemyCount <= 0 ||
                _specialEnemyEscaped ||
                _activeEnemyCount > 0)
            {
                return 0;
            }

            _specialPerfectClearBonusConsumed = true;
            return Mathf.RoundToInt(_specialKilledScore * (_specialPerfectClearScoreMultiplier - 1f));
        }

        public void ForceFinalEnemiesToDive(float remainingRatioThreshold)
        {
            if (_finalDiveTriggered || _spawnedEnemyCount <= 0 || _activeEnemyCount <= 0)
            {
                return;
            }

            if (remainingRatioThreshold <= 0f)
            {
                return;
            }

            int remainingEnemyThreshold = Mathf.CeilToInt(_spawnedEnemyCount * remainingRatioThreshold);
            if (_activeEnemyCount > remainingEnemyThreshold)
            {
                return;
            }

            _finalDiveTriggered = true;
            _limitedDiveEnemy = null;

            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _activeEnemies[i];
                if (enemy == null || !enemy.CanForceDive)
                {
                    continue;
                }

                enemy.ForceDive();
            }
        }

        public bool TryBeginLimitedDive(Enemy enemy)
        {
            if (enemy == null || enemy.Data == null)
            {
                return true;
            }

            if (!enemy.Data.LimitConcurrentDivesAboveFinalThreshold ||
                _finalDiveTriggered ||
                _spawnedEnemyCount <= 0 ||
                _activeEnemyCount <= GetConcurrentDiveLimitThreshold())
            {
                return true;
            }

            if (_limitedDiveEnemy != null && _limitedDiveEnemy != enemy)
            {
                return false;
            }

            if (_limitedDiveEnemy == null && Time.time < _nextLimitedDiveAllowedTime)
            {
                return false;
            }

            _limitedDiveEnemy = enemy;
            return true;
        }

        public void EndLimitedDive(Enemy enemy)
        {
            if (_limitedDiveEnemy == enemy)
            {
                _limitedDiveEnemy = null;
                _nextLimitedDiveAllowedTime = Time.time + Random.Range(_limitedDiveCooldownMin, _limitedDiveCooldownMax);
            }
        }

        private void OnValidate()
        {
            _defaultCapacity = Mathf.Max(1, _defaultCapacity);
            _maxSize = Mathf.Max(_defaultCapacity, _maxSize);
            _debugFormationIndex = Mathf.Max(0, _debugFormationIndex);
            _specialPerfectClearScoreMultiplier = Mathf.Max(1f, _specialPerfectClearScoreMultiplier);
            _limitedDiveCooldownMin = Mathf.Max(0f, _limitedDiveCooldownMin);
            _limitedDiveCooldownMax = Mathf.Max(_limitedDiveCooldownMin, _limitedDiveCooldownMax);
        }

        private int GetConcurrentDiveLimitThreshold()
        {
            return Mathf.CeilToInt(_spawnedEnemyCount * _concurrentDiveLimitRemainingRatio);
        }

        private void HandleEnemyReleased(Enemy enemy, bool killed)
        {
            EndLimitedDive(enemy);

            if (enemy == null || enemy.Data == null || !enemy.Data.CountsForPerfectClearBonus)
            {
                return;
            }

            if (killed)
            {
                _specialKilledScore += enemy.Data.ScoreValue;
            }
            else
            {
                _specialEnemyEscaped = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _testFormationSpawns == null) return;

            for (int i = 0; i < _testFormationSpawns.Length; i++)
            {
                TestFormationSpawn formationSpawn = _testFormationSpawns[i];
                Formation formation = formationSpawn.Formation;
                if (formation == null || formation.SlotCount <= 0) continue;

                float halfSpan = (formation.SlotCount - 1) * 0.5f;
                for (int slotIndex = 0; slotIndex < formation.SlotCount; slotIndex++)
                {
                    Vector2 start = formationSpawn.EntryStartCenter + Vector2.right * ((slotIndex - halfSpan) * formationSpawn.EntryStartSpacingX);
                    Vector2 end = formation.GetSlotWorldPosition(slotIndex);
                    Vector2 control = ((start + end) * 0.5f) + formation.GetSlotEntryControlOffset(slotIndex);

                    Color hue = Color.HSVToRGB(((i + slotIndex * 0.31f) * 0.137f) % 1f, 0.6f, 1f);
                    Gizmos.color = hue;

                    Vector2 prev = start;
                    const int samples = 24;
                    for (int k = 1; k <= samples; k++)
                    {
                        float t = k / (float)samples;
                        Vector2 point = BezierPath.EvaluateQuadratic(start, control, end, t);
                        Gizmos.DrawLine(prev, point);
                        prev = point;
                    }
                }
            }
        }
    }
}
