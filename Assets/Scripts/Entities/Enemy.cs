using System.Collections.Generic;
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
        private const int MaxSpriteColorSamples = 512;
        private static readonly Dictionary<int, Color> SpriteVfxColorCache = new();

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
            _shooter.Spawn(_data, _playerTransform);
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
            VfxManager.Instance?.Play(VfxCue.EnemyDeath, transform.position, ResolveDeathVfxColor());
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

        private Color ResolveDeathVfxColor()
        {
            if (_spriteRenderer == null)
            {
                return Color.white;
            }

            if (!TryResolveSpriteVfxColor(_spriteRenderer.sprite, out Color spriteColor))
            {
                return _spriteRenderer.color;
            }

            Color rendererTint = _spriteRenderer.color;
            spriteColor.r *= rendererTint.r;
            spriteColor.g *= rendererTint.g;
            spriteColor.b *= rendererTint.b;
            spriteColor.a = 1f;
            return spriteColor;
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

        private static bool TryResolveSpriteVfxColor(Sprite sprite, out Color color)
        {
            color = Color.white;

            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            int spriteId = sprite.GetInstanceID();
            if (SpriteVfxColorCache.TryGetValue(spriteId, out color))
            {
                return true;
            }

            if (!TrySampleSpriteTextureColor(sprite, out color))
            {
                return false;
            }

            SpriteVfxColorCache[spriteId] = color;
            return true;
        }

        private static bool TrySampleSpriteTextureColor(Sprite sprite, out Color color)
        {
            color = Color.white;

            Texture2D texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            int startX = Mathf.Clamp(Mathf.FloorToInt(textureRect.x), 0, texture.width - 1);
            int startY = Mathf.Clamp(Mathf.FloorToInt(textureRect.y), 0, texture.height - 1);
            int width = Mathf.Clamp(Mathf.CeilToInt(textureRect.width), 1, texture.width - startX);
            int height = Mathf.Clamp(Mathf.CeilToInt(textureRect.height), 1, texture.height - startY);
            int endX = startX + width;
            int endY = startY + height;
            int stride = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(width * height / (float)MaxSpriteColorSamples)));

            Color SamplePixel(int x, int y)
            {
                return texture.GetPixel(x, y);
            }

            if (!texture.isReadable)
            {
                return TrySampleUnreadableSpriteTextureColor(
                    texture,
                    startX,
                    startY,
                    width,
                    height,
                    out color);
            }

            return TryAverageSpritePixels(
                startX,
                startY,
                endX,
                endY,
                stride,
                SamplePixel,
                out color);
        }

        private static bool TrySampleUnreadableSpriteTextureColor(
            Texture2D texture,
            int startX,
            int startY,
            int width,
            int height,
            out Color color)
        {
            color = Color.white;

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D readableTexture = null;

            try
            {
                renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;

                readableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readableTexture.ReadPixels(new Rect(startX, startY, width, height), 0, 0, false);
                readableTexture.Apply(false, false);

                int stride = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(width * height / (float)MaxSpriteColorSamples)));

                Color SamplePixel(int x, int y)
                {
                    return readableTexture.GetPixel(x, y);
                }

                return TryAverageSpritePixels(0, 0, width, height, stride, SamplePixel, out color);
            }
            catch (UnityException)
            {
                return false;
            }
            finally
            {
                RenderTexture.active = previousActive;

                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (readableTexture != null)
                {
                    Destroy(readableTexture);
                }
            }
        }

        private static bool TryAverageSpritePixels(
            int startX,
            int startY,
            int endX,
            int endY,
            int stride,
            System.Func<int, int, Color> samplePixel,
            out Color color)
        {
            color = Color.white;

            double weightedRed = 0;
            double weightedGreen = 0;
            double weightedBlue = 0;
            double totalWeight = 0;
            double fallbackRed = 0;
            double fallbackGreen = 0;
            double fallbackBlue = 0;
            double totalFallbackWeight = 0;

            for (int y = startY; y < endY; y += stride)
            {
                for (int x = startX; x < endX; x += stride)
                {
                    Color pixel = samplePixel(x, y);
                    if (pixel.a < 0.1f)
                    {
                        continue;
                    }

                    float maxChannel = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                    float minChannel = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
                    float saturation = maxChannel - minChannel;
                    double fallbackWeight = pixel.a;
                    double colorWeight = pixel.a
                        * Mathf.Clamp01(saturation * 2f)
                        * Mathf.Clamp01(maxChannel);

                    weightedRed += pixel.r * colorWeight;
                    weightedGreen += pixel.g * colorWeight;
                    weightedBlue += pixel.b * colorWeight;
                    totalWeight += colorWeight;

                    fallbackRed += pixel.r * fallbackWeight;
                    fallbackGreen += pixel.g * fallbackWeight;
                    fallbackBlue += pixel.b * fallbackWeight;
                    totalFallbackWeight += fallbackWeight;
                }
            }

            if (totalWeight > 0.001)
            {
                color = new Color(
                    (float)(weightedRed / totalWeight),
                    (float)(weightedGreen / totalWeight),
                    (float)(weightedBlue / totalWeight),
                    1f);
                return true;
            }

            if (totalFallbackWeight <= 0.001)
            {
                return false;
            }

            color = new Color(
                (float)(fallbackRed / totalFallbackWeight),
                (float)(fallbackGreen / totalFallbackWeight),
                (float)(fallbackBlue / totalFallbackWeight),
                1f);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            EnemyMovement movement = _movement != null ? _movement : GetComponent<EnemyMovement>();
            movement?.DrawGizmosSelected(_data, _formationPosition, _entryControlOffset);
        }
    }
}
