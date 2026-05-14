using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class Boss : MonoBehaviour, IDamageable
    {
        private enum State
        {
            Inactive,
            Entering,
            Intro,
            Active,
            Defeated
        }

        [Header("Data")]
        [SerializeField] private BossData _data;
        [SerializeField] private bool _spawnOnEnable = true;

        [Header("Intro")]
        [SerializeField, Min(0f)] private float _introDuration = 1.5f;

        [Header("Attacks")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private GameObject _bossBulletPrefab;
        [SerializeField] private int _bulletPoolDefaultCapacity = 32;
        [SerializeField] private int _bulletPoolMaxSize = 128;

        [Header("Events")]
        [SerializeField] private BossDataEventChannel _bossSpawned;
        [SerializeField] private BossDataEventChannel _bossIntroStarted;
        [SerializeField] private BossHealthEventChannel _bossHealthChanged;
        [SerializeField] private BossPhaseEventChannel _bossPhaseChanged;
        [SerializeField] private BossDataEventChannel _bossDefeated;

        private State _state = State.Inactive;
        private int _currentHealth;
        private int _currentPhaseIndex = -1;
        private IObjectPool<Bullet> _bulletPool;
        private Coroutine _attackRoutine;
        private Coroutine _introRoutine;
        private float _nextAttackTime;
        private int _volleyIndex;
        private bool _isSpawning;

        public BossData Data => _data;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _data == null ? 0 : _data.MaxHealth;
        public BossPhaseData CurrentPhase => HasCurrentPhase ? _data.Phases[_currentPhaseIndex] : null;
        public int CurrentPhaseIndex => _currentPhaseIndex;
        public bool IsActive => _state == State.Entering || _state == State.Intro || _state == State.Active;
        public bool HasEnteredArena => _state == State.Active;
        public bool IsDefeated => _state == State.Defeated;
        public bool IsEncounterRunning => IsActive;

        private bool HasCurrentPhase =>
            _data != null &&
            _data.Phases != null &&
            _currentPhaseIndex >= 0 &&
            _currentPhaseIndex < _data.Phases.Count;

        private void Awake()
        {
            if (_bossBulletPrefab != null)
            {
                if (!_bossBulletPrefab.TryGetComponent(out Bullet _))
                {
                    Debug.LogError(
                        $"[{nameof(Boss)}] Boss bullet prefab '{_bossBulletPrefab.name}' is missing {nameof(Bullet)}.",
                        this);
                    return;
                }

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

        private void OnEnable()
        {
            if (_spawnOnEnable && !_isSpawning)
            {
                Spawn();
            }
        }

        private void OnDisable()
        {
            StopCurrentAttack();
            StopIntro();
        }

        private void OnValidate()
        {
            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(Boss)}] Assign {nameof(BossData)} on '{name}'.");
            }

            _bulletPoolDefaultCapacity = Mathf.Max(1, _bulletPoolDefaultCapacity);
            _bulletPoolMaxSize = Mathf.Max(_bulletPoolDefaultCapacity, _bulletPoolMaxSize);
        }

        private void Update()
        {
            if (_state == State.Entering)
            {
                UpdateEntry();
            }
            else if (_state == State.Intro && _introRoutine == null)
            {
                BeginAttacks();
            }
            else if (_state == State.Active)
            {
                UpdateAttacks();
            }
        }

        [ContextMenu("Spawn Boss")]
        public void Spawn()
        {
            if (_data == null)
            {
                Debug.LogError($"[{nameof(Boss)}] Cannot spawn '{name}' without {nameof(BossData)}.");
                _state = State.Inactive;
                return;
            }

            _isSpawning = true;
            gameObject.SetActive(true);
            _isSpawning = false;

            transform.position = _data.EntryStartPosition;
            _currentHealth = _data.MaxHealth;
            _currentPhaseIndex = ResolvePhaseIndex();
            _volleyIndex = 0;
            _state = State.Entering;
            StopCurrentAttack();
            StopIntro();

            _bossSpawned?.Raise(_data);
            RaiseHealthChanged();
            RaisePhaseChanged();
        }

        public void TakeDamage(int amount)
        {
            if (_state == State.Inactive || _state == State.Defeated || amount <= 0)
            {
                return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            RaiseHealthChanged();

            if (_currentHealth <= 0)
            {
                Defeat();
                return;
            }

            UpdatePhase();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state == State.Inactive || _state == State.Defeated || _data == null)
            {
                return;
            }

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(_data.ContactDamage);
            }
        }

        private void UpdateEntry()
        {
            Vector2 targetPosition = _data.EntryTargetPosition;
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                _data.EntrySpeed * Time.deltaTime);

            if ((Vector2)transform.position == targetPosition)
            {
                BeginIntro();
            }
        }

        private void BeginIntro()
        {
            _state = State.Intro;
            _bossIntroStarted?.Raise(_data);

            if (_introDuration <= Mathf.Epsilon)
            {
                BeginAttacks();
                return;
            }

            _introRoutine = StartCoroutine(RunIntro());
        }

        private IEnumerator RunIntro()
        {
            yield return new WaitForSeconds(_introDuration);
            _introRoutine = null;
            BeginAttacks();
        }

        private void BeginAttacks()
        {
            if (_state == State.Defeated || _state == State.Inactive)
            {
                return;
            }

            _state = State.Active;
            ScheduleNextAttack();
        }

        private void UpdatePhase()
        {
            int nextPhaseIndex = ResolvePhaseIndex();
            if (nextPhaseIndex == _currentPhaseIndex)
            {
                return;
            }

            _currentPhaseIndex = nextPhaseIndex;
            StopCurrentAttack();
            ScheduleNextAttack();
            RaisePhaseChanged();
        }

        private int ResolvePhaseIndex()
        {
            if (_data == null || _data.Phases == null || _data.Phases.Count == 0)
            {
                return -1;
            }

            float healthPercent = _data.MaxHealth <= 0
                ? 0f
                : _currentHealth / (float)_data.MaxHealth;

            int selectedIndex = 0;
            float selectedThreshold = float.MaxValue;

            for (int i = 0; i < _data.Phases.Count; i++)
            {
                BossPhaseData phase = _data.Phases[i];
                if (phase == null)
                {
                    continue;
                }

                float threshold = phase.HealthThreshold;
                if (healthPercent <= threshold && threshold < selectedThreshold)
                {
                    selectedIndex = i;
                    selectedThreshold = threshold;
                }
            }

            return selectedIndex;
        }

        private void Defeat()
        {
            _state = State.Defeated;
            StopCurrentAttack();
            StopIntro();
            AwardDefeatRewards();
            _bossDefeated?.Raise(_data);
            gameObject.SetActive(false);
        }

        private void AwardDefeatRewards()
        {
            if (_data == null)
            {
                return;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(_data.ScoreValue);
            }

            PickupDropPool.Instance?.TryDrop(_data.RewardDropTable, transform.position);
        }

        private Bullet CreateBullet()
        {
            GameObject go = Instantiate(_bossBulletPrefab);
            Bullet bullet = go.GetComponent<Bullet>();
            bullet.SetPool(_bulletPool);
            return bullet;
        }

        private void UpdateAttacks()
        {
            if (_attackRoutine != null || Time.time < _nextAttackTime)
            {
                return;
            }

            BossAttackPatternData pattern = ChooseAttackPattern();
            if (pattern == null)
            {
                ScheduleNextAttack();
                return;
            }

            _attackRoutine = StartCoroutine(RunAttackPattern(pattern));
        }

        private BossAttackPatternData ChooseAttackPattern()
        {
            BossPhaseData phase = CurrentPhase;
            if (phase == null || phase.AttackPatterns == null || phase.AttackPatterns.Count == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, phase.AttackPatterns.Count);
            for (int i = 0; i < phase.AttackPatterns.Count; i++)
            {
                int index = (startIndex + i) % phase.AttackPatterns.Count;
                BossAttackPatternData pattern = phase.AttackPatterns[index];
                if (pattern != null)
                {
                    return pattern;
                }
            }

            return null;
        }

        private IEnumerator RunAttackPattern(BossAttackPatternData pattern)
        {
            if (_bulletPool == null)
            {
                Debug.LogWarning($"[{nameof(Boss)}] '{name}' cannot fire because no boss bullet prefab is assigned.", this);
                ScheduleNextAttack();
                _attackRoutine = null;
                yield break;
            }

            switch (pattern.PatternType)
            {
                case BossAttackPatternType.Aimed:
                    FireAimed(pattern);
                    break;

                case BossAttackPatternType.Radial:
                    FireRadial(pattern);
                    break;

                case BossAttackPatternType.Sweep:
                    yield return FireSweep(pattern);
                    break;
            }

            ScheduleNextAttack();
            _attackRoutine = null;
        }

        private IEnumerator FireSweep(BossAttackPatternData pattern)
        {
            float duration = Mathf.Max(0f, pattern.PatternDuration);
            float interval = Mathf.Max(0.01f, pattern.ShotInterval);
            float elapsed = 0f;

            do
            {
                float t = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
                float angle = Mathf.Lerp(pattern.SweepStartAngleDegrees, pattern.SweepEndAngleDegrees, t);
                FireSpread(pattern, angle);
                elapsed += interval;

                if (elapsed <= duration)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
            while (elapsed <= duration && _state == State.Active);
        }

        private void FireAimed(BossAttackPatternData pattern)
        {
            float centerAngle = ResolveAimedAngle(pattern);
            FireSpread(pattern, centerAngle);
        }

        private void FireRadial(BossAttackPatternData pattern)
        {
            float rotationOffset = pattern.RotateEachVolley
                ? _volleyIndex * pattern.VolleyRotationDegrees
                : 0f;

            float spread = pattern.SpreadAngle >= 360f ? 360f : pattern.SpreadAngle;
            int bulletCount = Mathf.Max(1, pattern.BulletCount);
            float step = spread >= 360f
                ? 360f / bulletCount
                : bulletCount <= 1 ? 0f : spread / (bulletCount - 1);
            float startAngle = pattern.BaseAngleDegrees + rotationOffset - (spread >= 360f ? 0f : spread * 0.5f);

            for (int i = 0; i < bulletCount; i++)
            {
                FireBulletAtAngle(startAngle + step * i, pattern.BulletSpeed);
            }

            _volleyIndex++;
        }

        private void FireSpread(BossAttackPatternData pattern, float centerAngle)
        {
            int bulletCount = Mathf.Max(1, pattern.BulletCount);
            float spread = pattern.SpreadAngle;
            float step = bulletCount <= 1 ? 0f : spread / (bulletCount - 1);
            float startAngle = centerAngle - spread * 0.5f;

            for (int i = 0; i < bulletCount; i++)
            {
                FireBulletAtAngle(startAngle + step * i, pattern.BulletSpeed);
            }
        }

        private void FireBulletAtAngle(float angleDegrees, float speed)
        {
            Bullet bullet = _bulletPool.Get();
            if (bullet == null)
            {
                return;
            }

            bullet.Spawn(ResolveFirePosition(), DirectionFromAngle(angleDegrees), speed);
        }

        private float ResolveAimedAngle(BossAttackPatternData pattern)
        {
            if (!pattern.AimAtPlayer || _playerTransform == null)
            {
                return pattern.BaseAngleDegrees;
            }

            Vector2 direction = (Vector2)_playerTransform.position - ResolveFirePosition();
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return pattern.BaseAngleDegrees;
            }

            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private Vector2 ResolveFirePosition()
        {
            return _firePoint == null ? transform.position : _firePoint.position;
        }

        private static Vector2 DirectionFromAngle(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }

        private void ScheduleNextAttack()
        {
            BossPhaseData phase = CurrentPhase;
            if (phase == null)
            {
                _nextAttackTime = float.PositiveInfinity;
                return;
            }

            _nextAttackTime = Time.time + Random.Range(phase.AttackCooldownMin, phase.AttackCooldownMax);
        }

        private void StopCurrentAttack()
        {
            if (_attackRoutine == null)
            {
                return;
            }

            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        private void StopIntro()
        {
            if (_introRoutine == null)
            {
                return;
            }

            StopCoroutine(_introRoutine);
            _introRoutine = null;
        }

        private void RaiseHealthChanged()
        {
            _bossHealthChanged?.Raise(_currentHealth, MaxHealth);
        }

        private void RaisePhaseChanged()
        {
            if (CurrentPhase != null)
            {
                _bossPhaseChanged?.Raise(CurrentPhase);
            }
        }
    }
}
