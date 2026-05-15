using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class Enemy : MonoBehaviour, IDamageable
    {
        private enum State { Entering, InFormation, Diving, Lingering, Returning }

        [SerializeField] private EnemyData _data;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private int _contactDamage = 1;
        [SerializeField] private Vector2 _formationPosition;
        [Tooltip("Offset added to the midpoint of (start -> formation) to form the Bezier control point. Zero = straight line.")]
        [SerializeField] private Vector2 _entryControlOffset = Vector2.zero;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private GameObject _enemyBulletPrefab;
        [SerializeField] private int _bulletPoolDefaultCapacity = 5;
        [SerializeField] private int _bulletPoolMaxSize = 20;

        private State _state = State.Entering;
        private int _currentHealth;
        private Vector2 _diveTarget;
        private float _nextDiveTime;
        private float _nextFireTime;
        private float _lingerEndTime;
        private bool _isPassThroughDive;
        private IObjectPool<Bullet> _bulletPool;
        private IObjectPool<Enemy> _enemyPool;
        private EnemySpawner _spawner;
        private Formation _formation;
        private int _formationSlotIndex = -1;
        private bool _hasDespawned;

        private Vector2 _entryStart;
        private Vector2 _entryControlPoint;
        private Vector2 _entryEnd;
        private Vector2[] _entryPathPoints;
        private Vector2[] _entryPathControlPoints;
        private Vector2[] _entryPathControlOffsets;
        private Vector2 _defaultEntryControlOffset;
        private float _entryElapsed;
        private float _entryDuration;
        private bool _isReturningToSpawnForDespawn;

        public Vector2 EntryControlOffset => _entryControlOffset;
        public EnemyData Data => _data;
        public bool CanForceDive =>
            !_hasDespawned &&
            _data != null &&
            _data.BehaviorMode == EnemyBehaviorMode.Formation &&
            (_state == State.InFormation || _state == State.Returning);
        public event System.Action<Enemy, bool> Released;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _defaultEntryControlOffset = _entryControlOffset;

            if (_data == null)
            {
                Debug.LogError($"[{nameof(Enemy)}] Missing {nameof(EnemyData)} on '{name}'.");
            }

            if (_enemyBulletPrefab != null)
            {
                _bulletPool = new ObjectPool<Bullet>(
                    createFunc: CreateBullet,
                    actionOnGet: b => b.gameObject.SetActive(true),
                    actionOnRelease: b => b.gameObject.SetActive(false),
                    actionOnDestroy: b => Destroy(b.gameObject),
                    collectionCheck: true,
                    defaultCapacity: _bulletPoolDefaultCapacity,
                    maxSize: _bulletPoolMaxSize);

                PoolPrewarmer.Prewarm(_bulletPool, _bulletPoolDefaultCapacity);
            }
        }

        private void OnValidate()
        {
            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(Enemy)}] Assign {nameof(EnemyData)} on '{name}'.");
            }

            _bulletPoolDefaultCapacity = Mathf.Max(1, _bulletPoolDefaultCapacity);
            _bulletPoolMaxSize = Mathf.Max(_bulletPoolDefaultCapacity, _bulletPoolMaxSize);
        }

        public void SetPool(IObjectPool<Enemy> pool)
        {
            _enemyPool = pool;
        }

        public void SetSpawner(EnemySpawner spawner)
        {
            _spawner = spawner;
        }

        public void Spawn(Vector2 startPosition, Vector2 formationPosition, Transform playerTransform)
        {
            Spawn(startPosition, formationPosition, playerTransform, null, -1, _defaultEntryControlOffset, null);
        }

        public void Spawn(
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex)
        {
            Spawn(
                startPosition,
                formationPosition,
                playerTransform,
                formation,
                formationSlotIndex,
                _defaultEntryControlOffset,
                null);
        }

        public void Spawn(
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset)
        {
            Spawn(startPosition, formationPosition, playerTransform, formation, formationSlotIndex, entryControlOffset, null);
        }

        public void Spawn(
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints)
        {
            Spawn(
                startPosition,
                formationPosition,
                playerTransform,
                formation,
                formationSlotIndex,
                entryControlOffset,
                entryPathPoints,
                null);
        }

        public void Spawn(
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints,
            Vector2[] entryPathControlPoints)
        {
            _hasDespawned = false;

            if (_data == null)
            {
                Debug.LogError($"[{nameof(Enemy)}] Cannot spawn '{name}' without {nameof(EnemyData)}.");
                Release(killed: false);
                return;
            }

            transform.position = startPosition;
            _formationPosition = formationPosition;
            _playerTransform = playerTransform;
            _formation = formation;
            _formationSlotIndex = formationSlotIndex;
            _entryControlOffset = entryControlOffset;
            bool hasSegmentedEntryPath =
                entryPathPoints != null &&
                entryPathPoints.Length >= 2 &&
                entryPathControlPoints != null &&
                entryPathControlPoints.Length >= entryPathPoints.Length - 1;
            _entryPathPoints = hasSegmentedEntryPath ? entryPathPoints : null;
            _entryPathControlPoints = hasSegmentedEntryPath ? entryPathControlPoints : null;
            _entryPathControlOffsets = hasSegmentedEntryPath
                ? BuildControlOffsets(entryPathPoints, entryPathControlPoints)
                : null;
            _currentHealth = _data.MaxHealth;
            _isReturningToSpawnForDespawn = false;
            ApplyVisualsFromData();
            BeginEntry();
        }

        private void ApplyVisualsFromData()
        {
            if (_spriteRenderer == null || _data == null) return;
            _spriteRenderer.color = _data.SpriteColor;
        }

        private void BeginEntry()
        {
            _entryStart = transform.position;
            _entryEnd = ResolveFormationPosition();
            float entryPathLength;
            if (_entryPathPoints != null)
            {
                _entryPathPoints[0] = _entryStart;
                _entryPathPoints[_entryPathPoints.Length - 1] = _entryEnd;
                RefreshSegmentedPathControlPoints();
                entryPathLength = BezierPath.ApproximateSegmentedQuadraticPathLength(_entryPathPoints, _entryPathControlPoints);
                _entryControlPoint = _entryPathControlPoints != null && _entryPathControlPoints.Length > 0
                    ? _entryPathControlPoints[0]
                    : (_entryStart + _entryEnd) * 0.5f;
            }
            else
            {
                Vector2 midpoint = (_entryStart + _entryEnd) * 0.5f;
                _entryControlPoint = midpoint + _entryControlOffset;
                entryPathLength = BezierPath.ApproximateQuadraticLength(
                    _entryStart, _entryControlPoint, _entryEnd);
            }

            _entryDuration = entryPathLength / Mathf.Max(_data.EntrySpeed, 0.01f);
            _entryElapsed = 0f;
            _state = State.Entering;
        }

        private Vector2 ResolveFormationPosition()
        {
            if (_formation != null && _formation.HasSlot(_formationSlotIndex))
            {
                return _formation.GetSlotWorldPosition(_formationSlotIndex);
            }

            return _formationPosition;
        }

        private Bullet CreateBullet()
        {
            GameObject go = Instantiate(_enemyBulletPrefab);
            Bullet bullet = go.GetComponent<Bullet>();
            bullet.SetPool(_bulletPool);
            return bullet;
        }

        private void Update()
        {
            if (_state == State.InFormation || _state == State.Returning)
            {
                _formationPosition = ResolveFormationPosition();
            }

            switch (_state)
            {
                case State.Entering:
                    if (_entryDuration <= Mathf.Epsilon)
                    {
                        EnterFormation();
                        break;
                    }

                    _entryElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(_entryElapsed / _entryDuration);

                    if (_entryPathPoints != null)
                    {
                        _entryPathPoints[_entryPathPoints.Length - 1] = ResolveFormationPosition();
                        RefreshSegmentedPathControlPoints();
                        transform.position = BezierPath.EvaluateSegmentedQuadraticPath(_entryPathPoints, _entryPathControlPoints, t);
                    }
                    else
                    {
                        // Track a moving formation slot during entry to avoid end-of-path snapping.
                        _entryEnd = ResolveFormationPosition();
                        Vector2 midpoint = (_entryStart + _entryEnd) * 0.5f;
                        _entryControlPoint = midpoint + _entryControlOffset;

                        transform.position = BezierPath.EvaluateQuadratic(
                            _entryStart, _entryControlPoint, _entryEnd, t);
                    }

                    if (t >= 1f)
                    {
                        EnterFormation();
                    }
                    break;

                case State.InFormation:
                    transform.position = _formationPosition;
                    if (_data.CanFire && Time.time >= _nextFireTime)
                    {
                        FireBullet();
                    }
                    if (Time.time >= _nextDiveTime)
                    {
                        TryStartDive();
                    }
                    break;

                case State.Diving:
                    MoveToward(_diveTarget, _data.DiveSpeed);
                    if (Reached(_diveTarget))
                    {
                        StartLinger();
                    }
                    break;

                case State.Lingering:
                    if (Time.time >= _lingerEndTime)
                    {
                        StartReturn();
                    }
                    break;

                case State.Returning:
                    MoveToward(_formationPosition, _data.DiveSpeed);
                    if (Reached(_formationPosition))
                    {
                        if (_isReturningToSpawnForDespawn)
                        {
                            Release(killed: false);
                            break;
                        }

                        EnterFormation();
                    }
                    break;
            }
        }

        public void TakeDamage(int amount)
        {
            if (_hasDespawned) return;

            _currentHealth -= amount;
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        public void ForceDive()
        {
            if (!CanForceDive) return;

            StartDive();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasDespawned) return;

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(_contactDamage);
            }
        }

        private void MoveToward(Vector2 target, float speed)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime);
        }

        private bool Reached(Vector2 target) => (Vector2)transform.position == target;

        private void EnterFormation()
        {
            _formationPosition = ResolveFormationPosition();

            switch (_data.BehaviorMode)
            {
                case EnemyBehaviorMode.Formation:
                case EnemyBehaviorMode.Mother:
                    StartFormationIdle();
                    break;

                case EnemyBehaviorMode.KamikazeReturn:
                    StartReturnToSpawnForDespawn();
                    break;

                case EnemyBehaviorMode.BonusSnake:
                    Release(killed: false);
                    break;

                default:
                    StartFormationIdle();
                    break;
            }
        }

        private void StartFormationIdle()
        {
            _spawner?.EndLimitedDive(this);
            _state = State.InFormation;
            _nextDiveTime = Time.time + Random.Range(_data.DiveCooldownMin, _data.DiveCooldownMax);
            _nextFireTime = Time.time + Random.Range(_data.FireCooldownMin, _data.FireCooldownMax);
        }

        private void FireBullet()
        {
            if (_bulletPool != null)
            {
                Bullet bullet = _bulletPool.Get();
                bullet.Spawn(transform.position);
            }
            _nextFireTime = Time.time + Random.Range(_data.FireCooldownMin, _data.FireCooldownMax);
        }

        private void TryStartDive()
        {
            if (_spawner != null && !_spawner.TryBeginLimitedDive(this))
            {
                _nextDiveTime = Time.time + Random.Range(0.5f, 1.25f);
                return;
            }

            StartDive();
        }

        private void StartDive()
        {
            Vector2 enemyPos = transform.position;

            if (_playerTransform == null)
            {
                _diveTarget = new Vector2(enemyPos.x, _data.DiveBottomY);
            }
            else
            {
                Vector2 playerPos = _playerTransform.position;
                Vector2 direction = playerPos - enemyPos;

                if (direction.y >= 0f)
                {
                    _diveTarget = new Vector2(playerPos.x, _data.DiveBottomY);
                }
                else
                {
                    float t = (_data.DiveBottomY - enemyPos.y) / direction.y;
                    _diveTarget = enemyPos + direction * t;
                }
            }

            _isPassThroughDive = Random.value < _data.PassThroughChance;
            _state = State.Diving;
        }

        private void StartLinger()
        {
            if (_data.DiesAtDiveBottom)
            {
                Release(killed: false);
                return;
            }
            _lingerEndTime = Time.time + Random.Range(_data.LingerDurationMin, _data.LingerDurationMax);
            _state = State.Lingering;
        }

        private void StartReturn()
        {
            if (_isReturningToSpawnForDespawn)
            {
                _state = State.Returning;
                return;
            }

            if (_isPassThroughDive)
            {
                Vector2 liveFormationPosition = ResolveFormationPosition();
                transform.position = new Vector2(liveFormationPosition.x, _data.RespawnTopY);
                _entryPathPoints = null;
                _entryPathControlPoints = null;
                _entryPathControlOffsets = null;
                BeginEntry();
            }
            else
            {
                _state = State.Returning;
            }
        }

        private void StartReturnToSpawnForDespawn()
        {
            _formationPosition = _entryStart;
            _isReturningToSpawnForDespawn = true;
            _state = State.Returning;
        }

        private void Die()
        {
            if (_hasDespawned) return;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(_data.ScoreValue);
            }

            PickupDropPool.Instance?.TryDrop(_data.DropTable, transform.position);

            Release(killed: true);
        }

        private void Release(bool killed)
        {
            if (_hasDespawned) return;
            _hasDespawned = true;
            _spawner?.EndLimitedDive(this);
            Released?.Invoke(this, killed);

            if (_enemyPool != null)
            {
                _enemyPool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 start;
            Vector2 control;
            Vector2 end;

            if (Application.isPlaying && _entryDuration > 0f)
            {
                start = _entryStart;
                control = _entryControlPoint;
                end = _entryEnd;
                if (_entryPathPoints != null)
                {
                    Gizmos.color = Color.cyan;
                    DrawSegmentedQuadraticGizmo(_entryPathPoints, _entryPathControlPoints, 32);
                    for (int i = 0; i < _entryPathPoints.Length; i++)
                    {
                        Gizmos.DrawWireSphere(_entryPathPoints[i], 0.10f);
                    }

                    if (_entryPathControlPoints != null)
                    {
                        Gizmos.color = Color.yellow;
                        for (int i = 0; i < _entryPathControlPoints.Length; i++)
                        {
                            Gizmos.DrawWireSphere(_entryPathControlPoints[i], 0.08f);
                            Gizmos.DrawLine(_entryPathPoints[i], _entryPathControlPoints[i]);
                            Gizmos.DrawLine(_entryPathControlPoints[i], _entryPathPoints[i + 1]);
                        }
                    }

                    return;
                }
            }
            else
            {
                start = transform.position;
                Vector2 midpoint = (start + _formationPosition) * 0.5f;
                control = midpoint + _entryControlOffset;
                end = _formationPosition;
            }

            Gizmos.color = Color.cyan;
            Vector2 prev = start;
            const int samples = 24;
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 point = BezierPath.EvaluateQuadratic(start, control, end, t);
                Gizmos.DrawLine(prev, point);
                prev = point;
            }

            Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
            Gizmos.DrawLine(start, control);
            Gizmos.DrawLine(control, end);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(start, 0.15f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(control, 0.12f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(end, 0.15f);
        }

        private static void DrawSegmentedQuadraticGizmo(Vector2[] points, Vector2[] controlPoints, int samples)
        {
            if (points == null || points.Length < 2 || controlPoints == null || controlPoints.Length == 0) return;

            Vector2 previous = points[0];
            int clampedSamples = Mathf.Max(samples, 4);
            for (int i = 1; i <= clampedSamples; i++)
            {
                float t = i / (float)clampedSamples;
                Vector2 point = BezierPath.EvaluateSegmentedQuadraticPath(points, controlPoints, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        private static Vector2[] BuildControlOffsets(Vector2[] points, Vector2[] controlPoints)
        {
            if (points == null || controlPoints == null)
            {
                return null;
            }

            int segmentCount = Mathf.Min(points.Length - 1, controlPoints.Length);
            Vector2[] offsets = new Vector2[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 midpoint = (points[i] + points[i + 1]) * 0.5f;
                offsets[i] = controlPoints[i] - midpoint;
            }

            return offsets;
        }

        private void RefreshSegmentedPathControlPoints()
        {
            if (_entryPathPoints == null || _entryPathControlPoints == null || _entryPathControlOffsets == null)
            {
                return;
            }

            int segmentCount = Mathf.Min(
                _entryPathPoints.Length - 1,
                Mathf.Min(_entryPathControlPoints.Length, _entryPathControlOffsets.Length));
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 midpoint = (_entryPathPoints[i] + _entryPathPoints[i + 1]) * 0.5f;
                _entryPathControlPoints[i] = midpoint + _entryPathControlOffsets[i];
            }
        }
    }
}
