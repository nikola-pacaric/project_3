using UnityEngine;
using UnityEngine.Pool;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class Enemy : MonoBehaviour, IDamageable
    {
        private enum State { Entering, InFormation, Diving, Lingering, Returning }

        [SerializeField] private int _maxHealth = 1;
        [SerializeField] private int _scoreValue = 100;
        [SerializeField] private int _contactDamage = 1;
        [SerializeField] private float _entrySpeed = 3f;
        [SerializeField] private Vector2 _formationPosition;
        [Tooltip("Offset added to the midpoint of (start -> formation) to form the Bezier control point. Zero = straight line.")]
        [SerializeField] private Vector2 _entryControlOffset = Vector2.zero;
        [Header("Behavior")]
        [Tooltip("If false, enemy dives immediately on reaching formation slot (kamikaze).")]
        [SerializeField] private bool _sitsInFormation = true;
        [Tooltip("If true, enemy despawns at the bottom of its dive instead of returning (kamikaze).")]
        [SerializeField] private bool _diesAtDiveBottom = false;
        [Tooltip("If false, enemy never fires bullets while in formation.")]
        [SerializeField] private bool _canFire = true;
        [SerializeField] private float _diveSpeed = 6f;
        [SerializeField] private float _diveBottomY = -6f;
        [SerializeField] private float _diveCooldownMin = 2f;
        [SerializeField] private float _diveCooldownMax = 5f;
        [SerializeField] private float _lingerDurationMin = 0.5f;
        [SerializeField] private float _lingerDurationMax = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _passThroughChance = 0.5f;
        [SerializeField] private float _respawnTopY = 6f;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private GameObject _enemyBulletPrefab;
        [SerializeField] private float _fireCooldownMin = 1.5f;
        [SerializeField] private float _fireCooldownMax = 4f;
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

        private Vector2 _entryStart;
        private Vector2 _entryControlPoint;
        private float _entryDistanceTraveled;
        private float _entryPathLength;

        public Vector2 EntryControlOffset => _entryControlOffset;

        private void Awake()
        {
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

        public void SetPool(IObjectPool<Enemy> pool)
        {
            _enemyPool = pool;
        }

        public void Spawn(Vector2 startPosition, Vector2 formationPosition, Transform playerTransform)
        {
            transform.position = startPosition;
            _formationPosition = formationPosition;
            _playerTransform = playerTransform;
            _currentHealth = _maxHealth;
            BeginEntry();
        }

        private void BeginEntry()
        {
            _entryStart = transform.position;
            Vector2 midpoint = (_entryStart + _formationPosition) * 0.5f;
            _entryControlPoint = midpoint + _entryControlOffset;
            _entryPathLength = BezierPath.ApproximateQuadraticLength(
                _entryStart, _entryControlPoint, _formationPosition);
            _entryDistanceTraveled = 0f;
            _state = State.Entering;
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
            switch (_state)
            {
                case State.Entering:
                    if (_entryPathLength <= Mathf.Epsilon)
                    {
                        EnterFormation();
                        break;
                    }
                    _entryDistanceTraveled += _entrySpeed * Time.deltaTime;
                    float t = Mathf.Clamp01(_entryDistanceTraveled / _entryPathLength);
                    transform.position = BezierPath.EvaluateQuadratic(
                        _entryStart, _entryControlPoint, _formationPosition, t);
                    if (t >= 1f)
                    {
                        EnterFormation();
                    }
                    break;

                case State.InFormation:
                    if (_canFire && Time.time >= _nextFireTime)
                    {
                        FireBullet();
                    }
                    if (Time.time >= _nextDiveTime)
                    {
                        StartDive();
                    }
                    break;

                case State.Diving:
                    MoveToward(_diveTarget, _diveSpeed);
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
                    MoveToward(_formationPosition, _diveSpeed);
                    if (Reached(_formationPosition))
                    {
                        EnterFormation();
                    }
                    break;
            }
        }

        public void TakeDamage(int amount)
        {
            _currentHealth -= amount;
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
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
            if (!_sitsInFormation)
            {
                StartDive();
                return;
            }
            _state = State.InFormation;
            _nextDiveTime = Time.time + Random.Range(_diveCooldownMin, _diveCooldownMax);
            _nextFireTime = Time.time + Random.Range(_fireCooldownMin, _fireCooldownMax);
        }

        private void FireBullet()
        {
            if (_bulletPool != null)
            {
                Bullet bullet = _bulletPool.Get();
                bullet.Spawn(transform.position);
            }
            _nextFireTime = Time.time + Random.Range(_fireCooldownMin, _fireCooldownMax);
        }

        private void StartDive()
        {
            Vector2 enemyPos = transform.position;

            if (_playerTransform == null)
            {
                _diveTarget = new Vector2(enemyPos.x, _diveBottomY);
            }
            else
            {
                Vector2 playerPos = _playerTransform.position;
                Vector2 direction = playerPos - enemyPos;

                if (direction.y >= 0f)
                {
                    _diveTarget = new Vector2(playerPos.x, _diveBottomY);
                }
                else
                {
                    float t = (_diveBottomY - enemyPos.y) / direction.y;
                    _diveTarget = enemyPos + direction * t;
                }
            }

            _isPassThroughDive = Random.value < _passThroughChance;
            _state = State.Diving;
        }

        private void StartLinger()
        {
            if (_diesAtDiveBottom)
            {
                Despawn();
                return;
            }
            _lingerEndTime = Time.time + Random.Range(_lingerDurationMin, _lingerDurationMax);
            _state = State.Lingering;
        }

        private void StartReturn()
        {
            if (_isPassThroughDive)
            {
                transform.position = new Vector2(_formationPosition.x, _respawnTopY);
                BeginEntry();
            }
            else
            {
                _state = State.Returning;
            }
        }

        private void Die()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(_scoreValue);
            }
            Despawn();
        }

        private void Despawn()
        {
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

            if (Application.isPlaying && _entryPathLength > 0f)
            {
                start = _entryStart;
                control = _entryControlPoint;
                end = _formationPosition;
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
