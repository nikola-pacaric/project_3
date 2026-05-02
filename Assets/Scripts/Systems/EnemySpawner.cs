using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Warblade.Entities;

namespace Warblade.Systems
{
    /// <summary>
    /// Pools and spawns Enemy instances. M3 Phase 2 scaffolding — drives test spawns
    /// from a serialized list. Will be replaced/wrapped by WaveRunner + LevelManager
    /// in later phases, but the per-prefab pool dictionary stays.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct TestSpawn
        {
            public Enemy Prefab;
            public Vector2 StartPosition;
            public Vector2 FormationPosition;
            [Min(0f)] public float Delay;
        }

        [SerializeField] private Transform _playerTransform;
        [SerializeField] private TestSpawn[] _testSpawns;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxSize = 50;
        [SerializeField] private bool _drawGizmos = true;

        private readonly Dictionary<Enemy, IObjectPool<Enemy>> _pools =
            new Dictionary<Enemy, IObjectPool<Enemy>>();

        private void Start()
        {
            if (_testSpawns == null) return;
            for (int i = 0; i < _testSpawns.Length; i++)
            {
                StartCoroutine(SpawnAfterDelay(_testSpawns[i]));
            }
        }

        private IEnumerator SpawnAfterDelay(TestSpawn s)
        {
            if (s.Delay > 0f) yield return new WaitForSeconds(s.Delay);
            Spawn(s.Prefab, s.StartPosition, s.FormationPosition);
        }

        public Enemy Spawn(Enemy prefab, Vector2 startPosition, Vector2 formationPosition)
        {
            if (prefab == null) return null;
            IObjectPool<Enemy> pool = GetOrCreatePool(prefab);
            Enemy enemy = pool.Get();
            enemy.Spawn(startPosition, formationPosition, _playerTransform);
            return enemy;
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
                actionOnGet: e => e.gameObject.SetActive(true),
                actionOnRelease: e => e.gameObject.SetActive(false),
                actionOnDestroy: e => Destroy(e.gameObject),
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize);

            _pools[prefab] = pool;
            return pool;
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _testSpawns == null) return;

            for (int i = 0; i < _testSpawns.Length; i++)
            {
                TestSpawn s = _testSpawns[i];
                if (s.Prefab == null) continue;

                Vector2 start = s.StartPosition;
                Vector2 end = s.FormationPosition;
                Vector2 midpoint = (start + end) * 0.5f;
                Vector2 control = midpoint + s.Prefab.EntryControlOffset;

                Color hue = Color.HSVToRGB((i * 0.137f) % 1f, 0.6f, 1f);
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

                Gizmos.color = new Color(hue.r, hue.g, hue.b, 0.35f);
                Gizmos.DrawLine(start, control);
                Gizmos.DrawLine(control, end);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(start, 0.15f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(control, 0.12f);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(end, 0.15f);
            }
        }
    }
}
