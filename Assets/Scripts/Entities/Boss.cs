using System.Collections;
using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossHealth))]
    [RequireComponent(typeof(BossMovement))]
    [RequireComponent(typeof(BossShooter))]
    [RequireComponent(typeof(BossVisuals))]
    public class Boss : MonoBehaviour, IDamageable
    {
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

        private BossState _state = BossState.Inactive;
        private int _currentPhaseIndex = -1;
        private bool _isSpawning;
        private Coroutine _introRoutine;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;
        private BossHealth _health;
        private BossMovement _movement;
        private BossShooter _shooter;
        private BossVisuals _visuals;

        public BossData Data => _data;
        public int CurrentHealth => _health == null ? 0 : _health.CurrentHealth;
        public int MaxHealth => ResolveMaxHealth();
        public BossPhaseData CurrentPhase => HasCurrentPhase ? _data.Phases[_currentPhaseIndex] : null;
        public int CurrentPhaseIndex => _currentPhaseIndex;
        public bool IsActive =>
            _state == BossState.Entering ||
            _state == BossState.Intro ||
            _state == BossState.Transitioning ||
            _state == BossState.Active;
        public bool HasEnteredArena =>
            _state == BossState.Intro ||
            _state == BossState.Transitioning ||
            _state == BossState.Active;
        public bool IsDefeated => _state == BossState.Defeated;
        public bool IsEncounterRunning => IsActive;

        internal BossState State => _state;
        internal bool CanTakeDamage => HasEnteredArena;
        internal bool CanDealContactDamage => CanTakeDamage && _data != null;

        private bool HasCurrentPhase =>
            _data != null &&
            _data.Phases != null &&
            _currentPhaseIndex >= 0 &&
            _currentPhaseIndex < _data.Phases.Count;

        private void Awake()
        {
            ResolveCollaborators();
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
            StopIntro();
            _movement?.StopPhaseTransition();
            _shooter?.StopCurrentAttack();
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
            switch (_state)
            {
                case BossState.Entering:
                    if (_movement != null && _movement.TickEntry())
                    {
                        BeginIntro();
                    }
                    break;

                case BossState.Intro:
                    if (_introRoutine == null)
                    {
                        BeginAttacks();
                    }
                    break;

                case BossState.Active:
                    _movement?.TickActive(CurrentPhase);
                    _shooter?.Tick(CurrentPhase);
                    break;
            }
        }

        [ContextMenu("Spawn Boss")]
        public void Spawn()
        {
            ResolveCollaborators();

            if (_data == null)
            {
                Debug.LogError($"[{nameof(Boss)}] Cannot spawn '{name}' without {nameof(BossData)}.");
                _state = BossState.Inactive;
                return;
            }

            _isSpawning = true;
            gameObject.SetActive(true);
            _isSpawning = false;

            StopIntro();
            _movement.StopPhaseTransition();
            _shooter.StopCurrentAttack();

            _health.Spawn(MaxHealth);
            _currentPhaseIndex = ResolvePhaseIndex();
            _movement.Spawn(_data, _cycleScaling);
            _shooter.Spawn(_cycleScaling);
            _visuals.PauseAnimationAtFirstFrame();
            _visuals.ApplyCycleVisuals(_cycleScaling);
            _state = BossState.Entering;

            RaiseHealthChanged();
            RaisePhaseChanged();
        }

        /// <summary>
        /// Assigns runtime cycle scaling before the boss encounter starts.
        /// </summary>
        public void SetCycleScaling(CycleScalingState cycleScaling)
        {
            ResolveCollaborators();

            int previousMaxHealth = MaxHealth;
            float previousHealthPercent = previousMaxHealth <= 0
                ? 1f
                : CurrentHealth / (float)previousMaxHealth;

            _cycleScaling = cycleScaling;
            _movement.SetCycleScaling(cycleScaling);
            _shooter.SetCycleScaling(cycleScaling);

            if (!IsEncounterRunning)
            {
                return;
            }

            int scaledHealth = Mathf.Clamp(
                Mathf.RoundToInt(MaxHealth * previousHealthPercent),
                1,
                MaxHealth);
            _health.SetCurrentHealth(scaledHealth, MaxHealth);
            _visuals.ApplyCycleVisuals(_cycleScaling);
            RaiseHealthChanged();
            RaisePhaseChanged();
        }

        /// <summary>
        /// Assigns the current player target used by aimed boss attack patterns.
        /// </summary>
        public void SetPlayerTarget(Transform playerTransform)
        {
            ResolveCollaborators();

            _playerTransform = playerTransform;
            _movement.SetPlayerTarget(playerTransform);
            _shooter.SetPlayerTarget(playerTransform);
        }

        public void TakeDamage(int amount)
        {
            ResolveCollaborators();
            _health.TakeDamage(amount);
        }

        internal void UpdatePhase()
        {
            int nextPhaseIndex = ResolvePhaseIndex();
            if (nextPhaseIndex == _currentPhaseIndex)
            {
                return;
            }

            _currentPhaseIndex = nextPhaseIndex;
            BossPhaseData nextPhase = CurrentPhase;
            _movement.PreparePhase(nextPhase);
            _shooter.StopCurrentAttack();
            VfxManager.Instance?.Play(VfxCue.BossPhaseChange, transform.position);
            RaisePhaseChanged();

            if (nextPhase == null || nextPhase.TransitionDuration <= Mathf.Epsilon)
            {
                BeginAttacks();
                return;
            }

            _state = BossState.Transitioning;
            _movement.BeginPhaseTransition(nextPhase, BeginAttacks);
        }

        internal void Defeat()
        {
            if (_state == BossState.Defeated)
            {
                return;
            }

            _state = BossState.Defeated;
            VfxManager.Instance?.Play(VfxCue.BossDeath, transform.position);
            VfxManager.Instance?.Play(VfxCue.BossDefeat, transform.position);
            StopIntro();
            _movement?.StopPhaseTransition();
            _shooter?.StopCurrentAttack();
            AwardDefeatRewards();
            _bossDefeated?.Raise(_data);
            gameObject.SetActive(false);
        }

        internal void RaiseHealthChanged()
        {
            _bossHealthChanged?.Raise(CurrentHealth, MaxHealth);
        }

        private void BeginIntro()
        {
            _state = BossState.Intro;
            _visuals.PlayAnimation();
            VfxManager.Instance?.Play(VfxCue.BossWarning, transform.position);
            _bossSpawned?.Raise(_data);
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
            if (_state == BossState.Defeated || _state == BossState.Inactive)
            {
                return;
            }

            _state = BossState.Active;
            _shooter?.BeginAttacks(CurrentPhase);
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

        private void RaisePhaseChanged()
        {
            if (CurrentPhase != null)
            {
                _bossPhaseChanged?.Raise(CurrentPhase);
            }
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
                : CurrentHealth / (float)maxHealth;

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

        private int ResolveMaxHealth()
        {
            return _data == null
                ? 0
                : Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * _cycleScaling.BossHealthMultiplier));
        }

        private void ResolveCollaborators()
        {
            _health = GetOrAddComponent<BossHealth>();
            _movement = GetOrAddComponent<BossMovement>();
            _shooter = GetOrAddComponent<BossShooter>();
            _visuals = GetOrAddComponent<BossVisuals>();

            _health.Initialize(this);
            _movement.Initialize(this);
            _movement.SetPlayerTarget(_playerTransform);
            _movement.SetCycleScaling(_cycleScaling);
            _shooter.Initialize(this, _firePoint, _bossBulletPrefab, _bulletPoolDefaultCapacity, _bulletPoolMaxSize);
            _shooter.SetPlayerTarget(_playerTransform);
            _shooter.SetCycleScaling(_cycleScaling);
            _visuals.Initialize(_spriteRenderers);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            if (TryGetComponent(out T component))
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }
    }
}
