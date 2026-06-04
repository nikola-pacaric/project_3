using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyShooter))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        [Tooltip("Default stats used when this enemy is spawned directly. WaveData can override this per spawn.")]
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

        private EnemyData _defaultData;
        private Vector2 _defaultEntryControlOffset;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;
        private IObjectPool<Enemy> _enemyPool;
        private EnemySpawner _spawner;
        private EnemyHealth _health;
        private EnemyMovement _movement;
        private EnemyShooter _shooter;
        private bool _hasDespawned;

        public Vector2 EntryControlOffset => _entryControlOffset;
        public EnemyData Data => _data;
        public bool CanForceDive => _movement != null && _movement.CanForceDive;
        public event System.Action<Enemy, bool> Released;

        internal bool HasDespawned => _hasDespawned;
        internal bool IsFinalDivePressureActive => _spawner != null && _spawner.IsFinalDivePressureActive;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _defaultEntryControlOffset = _entryControlOffset;
            _defaultData = _data;
            ResolveCollaborators();

            if (_data == null)
            {
                Debug.LogError($"[{nameof(Enemy)}] Missing {nameof(EnemyData)} on '{name}'.");
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

        private void Update()
        {
            if (_hasDespawned) return;

            _shooter.Tick(_movement.State, _data);
            _movement.Tick();
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
            Spawn(
                startPosition,
                formationPosition,
                playerTransform,
                formation,
                formationSlotIndex,
                entryControlOffset,
                entryPathPoints,
                entryPathControlPoints,
                CycleScalingState.Default);
        }

        public void Spawn(
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints,
            Vector2[] entryPathControlPoints,
            CycleScalingState cycleScaling)
        {
            Spawn(
                startPosition,
                formationPosition,
                playerTransform,
                formation,
                formationSlotIndex,
                entryControlOffset,
                entryPathPoints,
                entryPathControlPoints,
                null,
                cycleScaling);
        }

        public void Spawn(
            Vector2 startPosition,
            Vector2 formationPosition,
            Transform playerTransform,
            Formation formation,
            int formationSlotIndex,
            Vector2 entryControlOffset,
            Vector2[] entryPathPoints,
            Vector2[] entryPathControlPoints,
            EnemyData dataOverride,
            CycleScalingState cycleScaling)
        {
            ResolveCollaborators();
            _hasDespawned = false;
            _data = dataOverride != null ? dataOverride : _defaultData;

            if (_data == null)
            {
                Debug.LogError($"[{nameof(Enemy)}] Cannot spawn '{name}' without {nameof(EnemyData)}.");
                Release(killed: false);
                return;
            }

            transform.position = startPosition;
            _formationPosition = formationPosition;
            _playerTransform = playerTransform;
            _entryControlOffset = entryControlOffset;
            _cycleScaling = cycleScaling;

            ApplyVisualsFromData();
            _health.Spawn(ResolveMaxHealth(), _contactDamage);
            _shooter.Spawn(_data);
            _movement.Spawn(
                _data,
                _cycleScaling,
                startPosition,
                formationPosition,
                _playerTransform,
                formation,
                formationSlotIndex,
                entryControlOffset,
                entryPathPoints,
                entryPathControlPoints);
        }

        public void TakeDamage(int amount)
        {
            _health.TakeDamage(amount);
        }

        public void ForceDive()
        {
            _movement.ForceDive();
        }

        public void DespawnForLevelReset()
        {
            Release(killed: false);
        }

        internal bool TryBeginLimitedDive()
        {
            return _spawner == null || _spawner.TryBeginLimitedDive(this);
        }

        internal void EndLimitedDive()
        {
            _spawner?.EndLimitedDive(this);
        }

        internal void Die()
        {
            if (_hasDespawned) return;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(_data.ScoreValue);
            }

            AudioManager.Instance?.PlayOneShot(ResolveDeathAudioCue());
            VfxManager.Instance?.Play(VfxCue.EnemyDeath, transform.position);
            PickupDropPool.Instance?.TryDrop(_data.DropTable, transform.position);

            Release(killed: true);
        }

        private AudioCue ResolveDeathAudioCue()
        {
            if (_data == null || _data.BehaviorMode != EnemyBehaviorMode.Mother)
            {
                return AudioCue.EnemyDeath;
            }

            return AudioManager.Instance != null && AudioManager.Instance.HasCue(AudioCue.EnemyMotherDeath)
                ? AudioCue.EnemyMotherDeath
                : AudioCue.EnemyDeath;
        }

        internal void Release(bool killed)
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

        private void ApplyVisualsFromData()
        {
            if (_spriteRenderer == null || _data == null) return;

            Color color = Color.Lerp(_data.SpriteColor, _cycleScaling.TintColor, _cycleScaling.TintStrength);
            color.a = _data.SpriteColor.a;
            _spriteRenderer.color = color;
        }

        private int ResolveMaxHealth()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * _cycleScaling.EnemyHealthMultiplier));
        }

        private void ResolveCollaborators()
        {
            // Existing prefabs keep their serialized Enemy fields; this guard wires the split components without a prefab YAML migration.
            _health = GetOrAddComponent<EnemyHealth>();
            _movement = GetOrAddComponent<EnemyMovement>();
            _shooter = GetOrAddComponent<EnemyShooter>();

            _health.Initialize(this, _contactDamage);
            _movement.Initialize(this);
            _shooter.Initialize(_enemyBulletPrefab, _bulletPoolDefaultCapacity, _bulletPoolMaxSize);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            if (TryGetComponent(out T component))
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }

        private void OnDrawGizmosSelected()
        {
            EnemyMovement movement = _movement != null ? _movement : GetComponent<EnemyMovement>();
            movement?.DrawGizmosSelected(_data, _formationPosition, _entryControlOffset);
        }
    }
}
