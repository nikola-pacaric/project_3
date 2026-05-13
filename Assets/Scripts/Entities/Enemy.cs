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
        private Formation _formation;
        private int _formationSlotIndex = -1;
        private bool _hasDespawned;

        private Vector2 _entryStart;
        private Vector2 _entryControlPoint;
        private Vector2 _entryEnd;
        private Vector2 _defaultEntryControlOffset;
        private float _entryElapsed;
        private float _entryDuration;

        public Vector2 EntryControlOffset => _entryControlOffset;
        public EnemyData Data => _data;

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
            }
        }

        private void OnValidate()
        {
            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(Enemy)}] Assign {nameof(EnemyData)} on '{name}'.");
            }
        }

        public void SetPool(IObjectPool<Enemy> pool)
        {
            _enemyPool = pool;
        }

        public void Spawn(Vector2 startPosition, Vector2 formationPosition, Transform playerTransform)
        {
            Spawn(startPosition, formationPosition, playerTransform, null, -1, _defaultEntryControlOffset);
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
                _defaultEntryControlOffset);
        }

        public void Spawn(
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset)
        {
            _hasDespawned = false;

            if (_data == null)
            {
                Debug.LogError($"[{nameof(Enemy)}] Cannot spawn '{name}' without {nameof(EnemyData)}.");
                Despawn();
                return;
            }

            transform.position = startPosition;
            _formationPosition = formationPosition;
            _playerTransform = playerTransform;
            _formation = formation;
            _formationSlotIndex = formationSlotIndex;
            _entryControlOffset = entryControlOffset;
            _currentHealth = _data.MaxHealth;
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
            Vector2 midpoint = (_entryStart + _entryEnd) * 0.5f;
            _entryControlPoint = midpoint + _entryControlOffset;
            float entryPathLength = BezierPath.ApproximateQuadraticLength(
                _entryStart, _entryControlPoint, _entryEnd);
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

                    // Track a moving formation slot during entry to avoid end-of-path snapping.
                    _entryEnd = ResolveFormationPosition();
                    Vector2 midpoint = (_entryStart + _entryEnd) * 0.5f;
                    _entryControlPoint = midpoint + _entryControlOffset;

                    transform.position = BezierPath.EvaluateQuadratic(
                        _entryStart, _entryControlPoint, _entryEnd, t);
                    if (t >= 1f)
                    {
                        EnterFormation();
                    }
                    break;

                case State.InFormation:
                    MoveToward(_formationPosition, _data.EntrySpeed);
                    if (_data.CanFire && Time.time >= _nextFireTime)
                    {
                        FireBullet();
                    }
                    if (Time.time >= _nextDiveTime)
                    {
                        StartDive();
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

            if (!_data.SitsInFormation)
            {
                StartDive();
                return;
            }
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
                Despawn();
                return;
            }
            _lingerEndTime = Time.time + Random.Range(_data.LingerDurationMin, _data.LingerDurationMax);
            _state = State.Lingering;
        }

        private void StartReturn()
        {
            if (_isPassThroughDive)
            {
                Vector2 liveFormationPosition = ResolveFormationPosition();
                transform.position = new Vector2(liveFormationPosition.x, _data.RespawnTopY);
                BeginEntry();
            }
            else
            {
                _state = State.Returning;
            }
        }

        private void Die()
        {
            if (_hasDespawned) return;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(_data.ScoreValue);
            }

            PickupDropPool.Instance?.TryDrop(_data.DropTable, transform.position);

            Despawn();
        }

        private void Despawn()
        {
            if (_hasDespawned) return;
            _hasDespawned = true;

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
    }
}
