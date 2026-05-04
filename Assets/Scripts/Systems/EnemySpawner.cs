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
            public Vector2 EntryStartCenter;
            [Min(0f)] public float EntryStartSpacingX;
            [Min(0f)] public float Delay;
            [Min(0f)] public float PerSlotDelay;
        }

        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Enemy[] _enemyPrefabs;
        [SerializeField] private TestFormationSpawn[] _testFormationSpawns;
        [SerializeField, Min(0)] private int _debugFormationIndex;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxSize = 50;
        [SerializeField] private bool _drawGizmos = true;

        private readonly Dictionary<Enemy, IObjectPool<Enemy>> _pools =
            new Dictionary<Enemy, IObjectPool<Enemy>>();
        private int _activeEnemyCount;

        public int ActiveEnemyCount => _activeEnemyCount;

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
            return Spawn(prefab, startPosition, formationPosition, null, -1, prefab != null ? prefab.EntryControlOffset : Vector2.zero);
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
                prefab != null ? prefab.EntryControlOffset : Vector2.zero);
        }

        public Enemy Spawn(
            Enemy prefab,
            Vector2 startPosition,
            Vector2 formationPosition,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset)
        {
            if (prefab == null) return null;

            IObjectPool<Enemy> pool = GetOrCreatePool(prefab);
            Enemy enemy = pool.Get();
            enemy.Spawn(
                startPosition,
                formationPosition,
                _playerTransform,
                formation,
                formationSlotIndex,
                entryControlOffset);
            return enemy;
        }

        /// <summary>
        /// Spawns a full formation using a start center and a per-slot spacing step vector.
        /// </summary>
        public Coroutine SpawnFormation(
            Formation formation,
            Vector2 entryStartCenter,
            Vector2 entryStartSpacingStep,
            float perSlotDelay,
            float delay = 0f)
        {
            return StartCoroutine(SpawnFormationAfterDelay(
                formation,
                entryStartCenter,
                entryStartSpacingStep,
                perSlotDelay,
                delay));
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
                debugSpawn.EntryStartCenter,
                Vector2.right * debugSpawn.EntryStartSpacingX,
                debugSpawn.PerSlotDelay));
        }

        private IEnumerator SpawnFormationAfterDelay(TestFormationSpawn spawn)
        {
            if (spawn.Delay > 0f) yield return new WaitForSeconds(spawn.Delay);
            yield return SpawnFormationRoutine(
                spawn.Formation,
                spawn.EntryStartCenter,
                Vector2.right * spawn.EntryStartSpacingX,
                spawn.PerSlotDelay);
        }

        private IEnumerator SpawnFormationAfterDelay(
            Formation formation,
            Vector2 entryStartCenter,
            Vector2 entryStartSpacingStep,
            float perSlotDelay,
            float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return SpawnFormationRoutine(
                formation,
                entryStartCenter,
                entryStartSpacingStep,
                perSlotDelay);
        }

        private IEnumerator SpawnFormationRoutine(
            Formation formation,
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

            float halfSpan = (formation.SlotCount - 1) * 0.5f;
            for (int slotIndex = 0; slotIndex < formation.SlotCount; slotIndex++)
            {
                EnemyData enemyData = formation.GetSlotEnemyData(slotIndex);
                if (enemyData == null)
                {
                    Debug.LogError(
                        $"[{nameof(EnemySpawner)}] Slot {slotIndex} in formation '{formation.name}' has no EnemyData.");
                    if (perSlotDelay > 0f) yield return new WaitForSeconds(perSlotDelay);
                    continue;
                }

                Enemy prefab = FindPrefabForData(enemyData);
                if (prefab == null)
                {
                    Debug.LogError(
                        $"[{nameof(EnemySpawner)}] No prefab found for EnemyData '{enemyData.name}'. " +
                        "Assign matching prefabs in _enemyPrefabs.");
                    if (perSlotDelay > 0f) yield return new WaitForSeconds(perSlotDelay);
                    continue;
                }

                Vector2 startPosition = entryStartCenter + entryStartSpacingStep * (slotIndex - halfSpan);
                Vector2 formationPosition = formation.GetSlotWorldPosition(slotIndex);
                Vector2 entryControlOffset = formation.GetSlotEntryControlOffset(slotIndex);

                Spawn(prefab, startPosition, formationPosition, formation, slotIndex, entryControlOffset);

                if (perSlotDelay > 0f)
                {
                    yield return new WaitForSeconds(perSlotDelay);
                }
            }
        }

        private Enemy FindPrefabForData(EnemyData enemyData)
        {
            if (enemyData == null || _enemyPrefabs == null) return null;

            for (int i = 0; i < _enemyPrefabs.Length; i++)
            {
                Enemy prefab = _enemyPrefabs[i];
                if (prefab == null) continue;
                if (prefab.Data == enemyData) return prefab;
            }

            return null;
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
                    return enemy;
                },
                actionOnGet: e =>
                {
                    _activeEnemyCount++;
                    e.gameObject.SetActive(true);
                },
                actionOnRelease: e =>
                {
                    _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
                    e.gameObject.SetActive(false);
                },
                actionOnDestroy: e => Destroy(e.gameObject),
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize);

            _pools[prefab] = pool;
            return pool;
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
