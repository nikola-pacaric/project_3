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
            Transitioning,
            Active,
            Defeated
        }

        [Header("Data")]
        [SerializeField] private BossData _data;
        [SerializeField] private bool _spawnOnEnable = true;
        [SerializeField] private SpriteRenderer[] _spriteRenderers;

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
        private Coroutine _phaseTransitionRoutine;
        private float _nextAttackTime;
        private int _volleyIndex;
        private bool _isSpawning;
        private Vector2 _arenaCenterPosition;
        private float _phaseMovementTime;
        private int _patrolDirection = 1;
        private Vector2 _movementTargetPosition;
        private float _movementPauseTimer;
        private int _movementWaypointIndex;
        private bool _hasMovementTarget;
        private Color[] _baseSpriteColors;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;

        public BossData Data => _data;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => ResolveMaxHealth();
        public BossPhaseData CurrentPhase => HasCurrentPhase ? _data.Phases[_currentPhaseIndex] : null;
        public int CurrentPhaseIndex => _currentPhaseIndex;
        public bool IsActive => _state == State.Entering || _state == State.Intro || _state == State.Transitioning || _state == State.Active;
        public bool HasEnteredArena => _state == State.Transitioning || _state == State.Active;
        public bool IsDefeated => _state == State.Defeated;
        public bool IsEncounterRunning => IsActive;

        private bool HasCurrentPhase =>
            _data != null &&
            _data.Phases != null &&
            _currentPhaseIndex >= 0 &&
            _currentPhaseIndex < _data.Phases.Count;

        private void Awake()
        {
            CacheSpriteRenderers();

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

                PoolPrewarmer.Prewarm(_bulletPool, _bulletPoolDefaultCapacity);
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
            StopPhaseTransition();
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
                UpdateMovement();
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
            _arenaCenterPosition = _data.EntryTargetPosition;
            _currentHealth = MaxHealth;
            _currentPhaseIndex = ResolvePhaseIndex();
            _phaseMovementTime = 0f;
            _patrolDirection = 1;
            ResetPhaseMovementState();
            _volleyIndex = 0;
            _state = State.Entering;
            StopCurrentAttack();
            StopIntro();
            StopPhaseTransition();
            ApplyCycleVisuals();

            _bossSpawned?.Raise(_data);
            RaiseHealthChanged();
            RaisePhaseChanged();
        }

        /// <summary>
        /// Assigns runtime cycle scaling before the boss encounter starts.
        /// </summary>
        public void SetCycleScaling(CycleScalingState cycleScaling)
        {
            int previousMaxHealth = MaxHealth;
            float previousHealthPercent = previousMaxHealth <= 0
                ? 1f
                : _currentHealth / (float)previousMaxHealth;

            _cycleScaling = cycleScaling;

            if (!IsEncounterRunning)
            {
                return;
            }

            _currentHealth = Mathf.Clamp(
                Mathf.RoundToInt(MaxHealth * previousHealthPercent),
                1,
                MaxHealth);
            ApplyCycleVisuals();
            RaiseHealthChanged();
            RaisePhaseChanged();
        }

        /// <summary>
        /// Assigns the current player target used by aimed boss attack patterns.
        /// </summary>
        public void SetPlayerTarget(Transform playerTransform)
        {
            _playerTransform = playerTransform;
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
                ResolveBossPressureSpeed(_data.EntrySpeed) * Time.deltaTime);

            if ((Vector2)transform.position == targetPosition)
            {
                _arenaCenterPosition = targetPosition;
                BeginIntro();
            }
        }

        private void UpdateMovement()
        {
            BossPhaseData phase = CurrentPhase;
            if (phase == null)
            {
                return;
            }

            _phaseMovementTime += Time.deltaTime;

            switch (ResolveMovementBehavior(phase.MovementBehavior))
            {
                case BossMovementBehavior.HorizontalPatrol:
                    UpdateHorizontalPatrol(phase);
                    break;

                case BossMovementBehavior.FigureEight:
                    UpdateFigureEight(phase);
                    break;

                case BossMovementBehavior.DashAndPause:
                    UpdateDashAndPause(phase);
                    break;

                case BossMovementBehavior.DiveSweep:
                    UpdateDiveSweep(phase);
                    break;

                case BossMovementBehavior.LaneSwitch:
                    UpdateLaneSwitch(phase);
                    break;

                case BossMovementBehavior.PlayerShadow:
                    UpdatePlayerShadow(phase);
                    break;

                case BossMovementBehavior.BoxPatrol:
                    UpdateBoxPatrol(phase);
                    break;
            }
        }

        private void UpdateHorizontalPatrol(BossPhaseData phase)
        {
            float amplitude = phase.MovementAmplitude;
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (amplitude <= Mathf.Epsilon || movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 position = transform.position;
            position.x += _patrolDirection * movementSpeed * Time.deltaTime;

            float minX = _arenaCenterPosition.x - amplitude;
            float maxX = _arenaCenterPosition.x + amplitude;
            if (position.x <= minX || position.x >= maxX)
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
                _patrolDirection *= -1;
            }

            position.y = Mathf.MoveTowards(position.y, _arenaCenterPosition.y, movementSpeed * Time.deltaTime);
            transform.position = position;
        }

        private void UpdateFigureEight(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            float xOffset = Mathf.Sin(_phaseMovementTime * movementSpeed) * phase.MovementAmplitude;
            float yOffset = Mathf.Sin(_phaseMovementTime * movementSpeed * 2f) * phase.VerticalMovementAmplitude;
            transform.position = new Vector2(
                _arenaCenterPosition.x + xOffset,
                _arenaCenterPosition.y + yOffset);
        }

        private void UpdateDashAndPause(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed) * 2.5f;
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            if (_movementPauseTimer > 0f)
            {
                _movementPauseTimer -= Time.deltaTime;
                return;
            }

            if (!_hasMovementTarget)
            {
                _movementTargetPosition = ResolveDashTarget(phase);
                _hasMovementTarget = true;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _movementTargetPosition,
                movementSpeed * Time.deltaTime);

            if (Vector2.SqrMagnitude((Vector2)transform.position - _movementTargetPosition) <= 0.0001f)
            {
                _movementWaypointIndex++;
                _movementPauseTimer = phase.MovementPauseDuration;
                _hasMovementTarget = false;
            }
        }

        private Vector2 ResolveDashTarget(BossPhaseData phase)
        {
            float x = phase.MovementAmplitude;
            float y = phase.VerticalMovementAmplitude;
            switch (_movementWaypointIndex % 4)
            {
                case 0:
                    return _arenaCenterPosition + new Vector2(-x, 0f);
                case 1:
                    return _arenaCenterPosition + new Vector2(x, -y);
                case 2:
                    return _arenaCenterPosition + new Vector2(x, y);
                default:
                    return _arenaCenterPosition + new Vector2(-x, -y);
            }
        }

        private void UpdateDiveSweep(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            float progress = Mathf.PingPong(_phaseMovementTime * movementSpeed * 0.25f, 1f);
            float x = Mathf.Lerp(
                _arenaCenterPosition.x - phase.MovementAmplitude,
                _arenaCenterPosition.x + phase.MovementAmplitude,
                progress);
            float y = _arenaCenterPosition.y - Mathf.Sin(progress * Mathf.PI) * phase.VerticalMovementAmplitude;
            transform.position = new Vector2(x, y);
        }

        private void UpdateLaneSwitch(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed) * 2f;
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            if (_movementPauseTimer > 0f)
            {
                _movementPauseTimer -= Time.deltaTime;
                return;
            }

            if (!_hasMovementTarget)
            {
                _movementTargetPosition = ResolveNextLaneTarget(phase);
                _hasMovementTarget = true;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _movementTargetPosition,
                movementSpeed * Time.deltaTime);

            if (Vector2.SqrMagnitude((Vector2)transform.position - _movementTargetPosition) <= 0.0001f)
            {
                _movementPauseTimer = phase.MovementPauseDuration;
                _hasMovementTarget = false;
            }
        }

        private Vector2 ResolveNextLaneTarget(BossPhaseData phase)
        {
            int laneCount = Mathf.Max(2, phase.MovementLaneCount);
            _movementWaypointIndex += _patrolDirection;

            if (_movementWaypointIndex >= laneCount)
            {
                _movementWaypointIndex = laneCount - 2;
                _patrolDirection = -1;
            }
            else if (_movementWaypointIndex < 0)
            {
                _movementWaypointIndex = 1;
                _patrolDirection = 1;
            }

            float laneT = laneCount <= 1 ? 0.5f : _movementWaypointIndex / (float)(laneCount - 1);
            float x = Mathf.Lerp(
                _arenaCenterPosition.x - phase.MovementAmplitude,
                _arenaCenterPosition.x + phase.MovementAmplitude,
                laneT);

            return new Vector2(x, _arenaCenterPosition.y);
        }

        private void UpdatePlayerShadow(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            float targetX = _arenaCenterPosition.x;
            if (_playerTransform != null)
            {
                targetX = Mathf.Lerp(_arenaCenterPosition.x, _playerTransform.position.x, phase.PlayerShadowStrength);
            }

            targetX = Mathf.Clamp(
                targetX,
                _arenaCenterPosition.x - phase.MovementAmplitude,
                _arenaCenterPosition.x + phase.MovementAmplitude);

            Vector2 position = transform.position;
            position.x = Mathf.MoveTowards(position.x, targetX, movementSpeed * Time.deltaTime);
            position.y = Mathf.MoveTowards(position.y, _arenaCenterPosition.y, movementSpeed * Time.deltaTime);
            transform.position = position;
        }

        private void UpdateBoxPatrol(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            if (!_hasMovementTarget)
            {
                _movementTargetPosition = ResolveBoxPatrolTarget(phase);
                _hasMovementTarget = true;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _movementTargetPosition,
                movementSpeed * Time.deltaTime);

            if (Vector2.SqrMagnitude((Vector2)transform.position - _movementTargetPosition) <= 0.0001f)
            {
                _movementWaypointIndex++;
                _hasMovementTarget = false;
            }
        }

        private Vector2 ResolveBoxPatrolTarget(BossPhaseData phase)
        {
            float x = phase.MovementAmplitude;
            float y = phase.VerticalMovementAmplitude;
            switch (_movementWaypointIndex % 4)
            {
                case 0:
                    return _arenaCenterPosition + new Vector2(-x, y);
                case 1:
                    return _arenaCenterPosition + new Vector2(x, y);
                case 2:
                    return _arenaCenterPosition + new Vector2(x, -y);
                default:
                    return _arenaCenterPosition + new Vector2(-x, -y);
            }
        }

        private static BossMovementBehavior ResolveMovementBehavior(BossMovementBehavior movementBehavior)
        {
            if (movementBehavior == BossMovementBehavior.HoldPosition)
            {
                return BossMovementBehavior.HorizontalPatrol;
            }

            if (movementBehavior == BossMovementBehavior.SineDrift)
            {
                return BossMovementBehavior.FigureEight;
            }

            return movementBehavior;
        }

        private void ResetPhaseMovementState()
        {
            _movementTargetPosition = _arenaCenterPosition;
            _movementPauseTimer = 0f;
            _movementWaypointIndex = 0;
            _hasMovementTarget = false;
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
            _phaseMovementTime = 0f;
            _patrolDirection = transform.position.x >= _arenaCenterPosition.x ? -1 : 1;
            ResetPhaseMovementState();
            StopCurrentAttack();
            RaisePhaseChanged();

            BossPhaseData nextPhase = CurrentPhase;
            if (nextPhase == null || nextPhase.TransitionDuration <= Mathf.Epsilon)
            {
                BeginAttacks();
                return;
            }

            StopPhaseTransition();
            _phaseTransitionRoutine = StartCoroutine(RunPhaseTransition(nextPhase));
        }

        private IEnumerator RunPhaseTransition(BossPhaseData phase)
        {
            float duration = phase.TransitionDuration;
            _state = State.Transitioning;

            Vector2 startPosition = transform.position;
            ResetPhaseMovementState();
            Vector2 targetPosition = ResolvePhaseTransitionTarget(phase, startPosition);
            float elapsed = 0f;

            while (_state == State.Transitioning && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector2.Lerp(startPosition, targetPosition, easedT);
                yield return null;
            }

            if (_state != State.Transitioning)
            {
                _phaseTransitionRoutine = null;
                yield break;
            }

            transform.position = targetPosition;
            _movementTargetPosition = targetPosition;
            _hasMovementTarget = ResolveMovementBehavior(phase.MovementBehavior) == BossMovementBehavior.LaneSwitch ||
                ResolveMovementBehavior(phase.MovementBehavior) == BossMovementBehavior.BoxPatrol ||
                ResolveMovementBehavior(phase.MovementBehavior) == BossMovementBehavior.DashAndPause;
            _phaseTransitionRoutine = null;
            BeginAttacks();
        }

        private Vector2 ResolvePhaseTransitionTarget(BossPhaseData phase, Vector2 currentPosition)
        {
            if (phase == null || phase.TransitionTarget == BossPhaseTransitionTarget.ArenaCenter)
            {
                return _arenaCenterPosition;
            }

            switch (ResolveMovementBehavior(phase.MovementBehavior))
            {
                case BossMovementBehavior.HorizontalPatrol:
                case BossMovementBehavior.PlayerShadow:
                    return new Vector2(
                        Mathf.Clamp(
                            currentPosition.x,
                            _arenaCenterPosition.x - phase.MovementAmplitude,
                            _arenaCenterPosition.x + phase.MovementAmplitude),
                        _arenaCenterPosition.y);

                case BossMovementBehavior.LaneSwitch:
                    return ResolveClosestLanePoint(phase, currentPosition);

                case BossMovementBehavior.BoxPatrol:
                    return ResolveClosestPoint(
                        currentPosition,
                        out _movementWaypointIndex,
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, -phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, -phase.VerticalMovementAmplitude));

                case BossMovementBehavior.DashAndPause:
                    return ResolveClosestPoint(
                        currentPosition,
                        out _movementWaypointIndex,
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, 0f),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, -phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, -phase.VerticalMovementAmplitude));

                case BossMovementBehavior.DiveSweep:
                    return ResolveClosestDiveSweepPoint(phase, currentPosition);

                case BossMovementBehavior.FigureEight:
                    return ResolveClosestFigureEightPoint(phase, currentPosition);

                default:
                    return _arenaCenterPosition;
            }
        }

        private Vector2 ResolveClosestLanePoint(BossPhaseData phase, Vector2 currentPosition)
        {
            int laneCount = Mathf.Max(2, phase.MovementLaneCount);
            Vector2 closest = _arenaCenterPosition;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < laneCount; i++)
            {
                float laneT = i / (float)(laneCount - 1);
                Vector2 lanePosition = new Vector2(
                    Mathf.Lerp(
                        _arenaCenterPosition.x - phase.MovementAmplitude,
                        _arenaCenterPosition.x + phase.MovementAmplitude,
                        laneT),
                    _arenaCenterPosition.y);
                float distance = Vector2.SqrMagnitude(currentPosition - lanePosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = lanePosition;
                    _movementWaypointIndex = i;
                }
            }

            return closest;
        }

        private Vector2 ResolveClosestDiveSweepPoint(BossPhaseData phase, Vector2 currentPosition)
        {
            const int sampleCount = 48;
            Vector2 closest = _arenaCenterPosition;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i <= sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                Vector2 point = _arenaCenterPosition + new Vector2(
                    Mathf.Lerp(-phase.MovementAmplitude, phase.MovementAmplitude, progress),
                    -Mathf.Sin(progress * Mathf.PI) * phase.VerticalMovementAmplitude);
                float distance = Vector2.SqrMagnitude(currentPosition - point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = point;
                    float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
                    _phaseMovementTime = movementSpeed <= Mathf.Epsilon
                        ? 0f
                        : progress / (movementSpeed * 0.25f);
                }
            }

            return closest;
        }

        private Vector2 ResolveClosestFigureEightPoint(BossPhaseData phase, Vector2 currentPosition)
        {
            const int sampleCount = 72;
            Vector2 closest = _arenaCenterPosition;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount * Mathf.PI * 2f;
                Vector2 point = _arenaCenterPosition + new Vector2(
                    Mathf.Sin(t) * phase.MovementAmplitude,
                    Mathf.Sin(t * 2f) * phase.VerticalMovementAmplitude);
                float distance = Vector2.SqrMagnitude(currentPosition - point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = point;
                    _phaseMovementTime = ResolveBossPressureSpeed(phase.MovementSpeed) <= Mathf.Epsilon
                        ? 0f
                        : t / ResolveBossPressureSpeed(phase.MovementSpeed);
                }
            }

            return closest;
        }

        private static Vector2 ResolveClosestPoint(Vector2 currentPosition, out int closestIndex, params Vector2[] points)
        {
            closestIndex = 0;
            Vector2 closest = points == null || points.Length == 0 ? currentPosition : points[0];
            float closestDistance = float.PositiveInfinity;

            if (points == null)
            {
                return closest;
            }

            for (int i = 0; i < points.Length; i++)
            {
                float distance = Vector2.SqrMagnitude(currentPosition - points[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = points[i];
                    closestIndex = i;
                }
            }

            return closest;
        }

        private int ResolvePhaseIndex()
        {
            if (_data == null || _data.Phases == null || _data.Phases.Count == 0)
            {
                return -1;
            }

            int maxHealth = MaxHealth;
            float healthPercent = maxHealth <= 0
                ? 0f
                : _currentHealth / (float)maxHealth;

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
            StopPhaseTransition();
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
                FireBulletAtAngle(startAngle + step * i, ResolveBossPressureSpeed(pattern.BulletSpeed));
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
                FireBulletAtAngle(startAngle + step * i, ResolveBossPressureSpeed(pattern.BulletSpeed));
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

        private void StopPhaseTransition()
        {
            if (_phaseTransitionRoutine == null)
            {
                return;
            }

            StopCoroutine(_phaseTransitionRoutine);
            _phaseTransitionRoutine = null;
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

        private int ResolveMaxHealth()
        {
            return _data == null ? 0 : Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * _cycleScaling.BossHealthMultiplier));
        }

        private float ResolveBossPressureSpeed(float baseSpeed)
        {
            return baseSpeed * _cycleScaling.BossPressureMultiplier;
        }

        private void CacheSpriteRenderers()
        {
            if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            {
                _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            _baseSpriteColors = new Color[_spriteRenderers == null ? 0 : _spriteRenderers.Length];
            for (int i = 0; i < _baseSpriteColors.Length; i++)
            {
                _baseSpriteColors[i] = _spriteRenderers[i] == null ? Color.white : _spriteRenderers[i].color;
            }
        }

        private void ApplyCycleVisuals()
        {
            if (_spriteRenderers == null || _baseSpriteColors == null)
            {
                return;
            }

            int count = Mathf.Min(_spriteRenderers.Length, _baseSpriteColors.Length);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer spriteRenderer = _spriteRenderers[i];
                if (spriteRenderer == null) continue;

                Color color = Color.Lerp(_baseSpriteColors[i], _cycleScaling.TintColor, _cycleScaling.TintStrength);
                color.a = _baseSpriteColors[i].a;
                spriteRenderer.color = color;
            }
        }
    }
}
