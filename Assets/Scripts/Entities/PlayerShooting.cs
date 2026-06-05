using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private float _spawnYOffset = 0.5f;
        [SerializeField, Min(1)] private int _baseMaxActiveBullets = 5;
        [SerializeField, Min(0f)] private float _baseFireCooldown = 0.2f;
        [SerializeField, Min(1)] private int _rapidFireBurstVolleyCount = 3;
        [SerializeField, Min(0f)] private float _rapidFireBurstInterval = 0.06f;
        [SerializeField] private Vector2 _doubleShotOffset = new Vector2(0.18f, 0f);
        [SerializeField] private Vector2 _tripleShotSideOffset = new Vector2(0.24f, 0f);
        [SerializeField, Range(0f, 60f)] private float _tripleShotSideAngle = 35f;
        [SerializeField] private Vector2 _quadShotInnerOffset = new Vector2(0.18f, 0f);
        [SerializeField] private Vector2 _quadShotOuterOffset = new Vector2(0.48f, 0f);
        [SerializeField, Range(0f, 45f)] private float _quadShotOuterAngle = 12f;
        [SerializeField] private int _poolDefaultCapacity = 10;
        [SerializeField] private int _poolMaxSize = 50;

        [Header("Bullet Colors")]
        [SerializeField] private Color _doubleShotBulletColor = new Color(0.455f, 1f, 0.173f, 1f);
        [SerializeField] private Color _tripleShotBulletColor = new Color(0.059f, 0.776f, 1f, 1f);
        [SerializeField] private Color _quadShotBulletColor = new Color(1f, 0.808f, 0.18f, 1f);

        [Header("Debug")]
        [SerializeField] private bool _debugAutofireActive;
        [SerializeField] private bool _debugRapidFireActive;

        private readonly HashSet<Bullet> _activePlayerBullets = new HashSet<Bullet>();
        private readonly Vector2[] _volleyOffsets = new Vector2[4];
        private readonly Vector2[] _volleyDirections = new Vector2[4];
        private bool _wasFireHeld;
        private bool _canShoot = true;
        private bool _suppressFireUntilReleased;
        private float _nextFireTime;
        private int _queuedRapidFireVolleys;
        private float _nextRapidFireVolleyTime;
        private IObjectPool<Bullet> _pool;

        public int BaseMaxActiveBullets => _baseMaxActiveBullets;

        private void Awake()
        {
            _pool = new ObjectPool<Bullet>(
                createFunc: CreateBullet,
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: HandleBulletReleased,
                actionOnDestroy: HandleBulletDestroyed,
                collectionCheck: true,
                defaultCapacity: _poolDefaultCapacity,
                maxSize: _poolMaxSize);

            PoolPrewarmer.Prewarm(_pool, _poolDefaultCapacity);
        }

        private void OnValidate()
        {
            _poolDefaultCapacity = Mathf.Max(1, _poolDefaultCapacity);
            _poolMaxSize = Mathf.Max(_poolDefaultCapacity, _poolMaxSize);
            _rapidFireBurstVolleyCount = Mathf.Max(1, _rapidFireBurstVolleyCount);
            _rapidFireBurstInterval = Mathf.Max(0f, _rapidFireBurstInterval);
            _baseFireCooldown = Mathf.Max(0f, _baseFireCooldown);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= HandleGameStateChanged;
            }
        }

        private Bullet CreateBullet()
        {
            GameObject go = Instantiate(_bulletPrefab);
            Bullet bullet = go.GetComponent<Bullet>();
            bullet.SetPool(_pool);
            return bullet;
        }

        private void Update()
        {
            if (_input == null) return;
            if (!_canShoot || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                _wasFireHeld = false;
                ClearQueuedRapidFire();
                return;
            }

            ProcessQueuedRapidFire();

            bool fireHeld = _input.FireHeld;
            if (_suppressFireUntilReleased)
            {
                if (!fireHeld)
                {
                    _suppressFireUntilReleased = false;
                }

                _wasFireHeld = fireHeld;
                return;
            }

            bool firePressedThisFrame = fireHeld && !_wasFireHeld;

            if (firePressedThisFrame || (IsAutofireActive && fireHeld))
            {
                TryFire();
            }

            _wasFireHeld = fireHeld;
        }

        private bool TryFire()
        {
            if (Time.time < _nextFireTime) return false;
            if (_queuedRapidFireVolleys > 0) return false;

            int volleySize = PopulateVolleyOffsets(GetWeaponTier());
            int burstVolleyCount = CalculateAllowedBurstVolleyCount(volleySize);
            if (burstVolleyCount <= 0)
            {
                return false;
            }

            SpawnCurrentVolley(volleySize);
            QueueRapidFireVolleys(burstVolleyCount - 1);

            _nextFireTime = Time.time + GetShotCooldown(burstVolleyCount);
            return true;
        }

        public void SetShootingEnabled(bool isEnabled)
        {
            _canShoot = isEnabled;
            if (!isEnabled)
            {
                _wasFireHeld = false;
                ClearQueuedRapidFire();
            }
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            if (gameState == GameState.Playing && _input != null && _input.FireHeld)
            {
                _suppressFireUntilReleased = true;
                _wasFireHeld = true;
            }
        }

        private void SpawnBullet(Vector3 spawnPosition, Vector2 direction)
        {
            Bullet bullet = _pool.Get();
            _activePlayerBullets.Add(bullet);
            ApplyBulletPresentationColor(bullet, GetWeaponTier());
            bullet.Spawn(spawnPosition, direction);
            VfxManager.Instance?.Play(VfxCue.PlayerMuzzleFlash, spawnPosition, direction);
        }

        private void ApplyBulletPresentationColor(Bullet bullet, WeaponTier weaponTier)
        {
            if (bullet == null)
            {
                return;
            }

            switch (weaponTier)
            {
                case WeaponTier.Double:
                    bullet.SetPresentationColor(_doubleShotBulletColor);
                    break;

                case WeaponTier.Triple:
                    bullet.SetPresentationColor(_tripleShotBulletColor);
                    break;

                case WeaponTier.Quad:
                    bullet.SetPresentationColor(_quadShotBulletColor);
                    break;

                case WeaponTier.Single:
                default:
                    bullet.ResetPresentationColors();
                    break;
            }
        }

        private WeaponTier GetWeaponTier()
        {
            return RunStatsManager.Instance != null
                ? RunStatsManager.Instance.WeaponTier
                : WeaponTier.Single;
        }

        private int GetMaxActiveBullets()
        {
            int bonusBullets = RunStatsManager.Instance != null
                ? RunStatsManager.Instance.EffectiveBulletsLevel
                : 0;

            return Mathf.Max(1, _baseMaxActiveBullets + bonusBullets);
        }

        private float GetFireCooldown()
        {
            return Mathf.Max(0f, _baseFireCooldown);
        }

        private float GetShotCooldown(int burstVolleyCount)
        {
            float burstDuration = Mathf.Max(0, burstVolleyCount - 1) * _rapidFireBurstInterval;
            return Mathf.Max(GetFireCooldown(), burstDuration);
        }

        private bool IsAutofireActive => _debugAutofireActive
            || (BuffManager.Instance != null && BuffManager.Instance.IsAutofireActive);

        private bool IsRapidFireActive => _debugRapidFireActive
            || (BuffManager.Instance != null && BuffManager.Instance.IsRapidFireActive);

        private int CalculateAllowedBurstVolleyCount(int volleySize)
        {
            int requestedVolleyCount = IsRapidFireActive ? _rapidFireBurstVolleyCount : 1;
            int availableBulletSlots = GetMaxActiveBullets() - _activePlayerBullets.Count;
            int availableFullVolleys = availableBulletSlots / Mathf.Max(1, volleySize);

            return Mathf.Min(requestedVolleyCount, availableFullVolleys);
        }

        private void QueueRapidFireVolleys(int volleyCount)
        {
            _queuedRapidFireVolleys = Mathf.Max(0, volleyCount);
            if (_queuedRapidFireVolleys > 0)
            {
                _nextRapidFireVolleyTime = Time.time + _rapidFireBurstInterval;
            }
        }

        private void ProcessQueuedRapidFire()
        {
            if (_queuedRapidFireVolleys <= 0 || Time.time < _nextRapidFireVolleyTime)
            {
                return;
            }

            int volleySize = PopulateVolleyOffsets(GetWeaponTier());
            if (HasRoomForVolley(volleySize))
            {
                SpawnCurrentVolley(volleySize);
            }

            _queuedRapidFireVolleys--;
            _nextRapidFireVolleyTime = Time.time + _rapidFireBurstInterval;
        }

        private void SpawnCurrentVolley(int volleySize)
        {
            Vector3 baseSpawnPosition = transform.position + Vector3.up * _spawnYOffset;
            for (int i = 0; i < volleySize; i++)
            {
                SpawnBullet(baseSpawnPosition + (Vector3)_volleyOffsets[i], _volleyDirections[i]);
            }

            AudioManager.Instance?.PlayOneShot(AudioCue.PlayerShoot);
        }

        private bool HasRoomForVolley(int volleySize)
        {
            return GetMaxActiveBullets() - _activePlayerBullets.Count >= volleySize;
        }

        private void ClearQueuedRapidFire()
        {
            _queuedRapidFireVolleys = 0;
            _nextRapidFireVolleyTime = 0f;
        }

        private int PopulateVolleyOffsets(WeaponTier weaponTier)
        {
            switch (weaponTier)
            {
                case WeaponTier.Double:
                    _volleyOffsets[0] = -_doubleShotOffset;
                    _volleyOffsets[1] = _doubleShotOffset;
                    _volleyDirections[0] = Vector2.up;
                    _volleyDirections[1] = Vector2.up;
                    return 2;

                case WeaponTier.Triple:
                    _volleyOffsets[0] = -_tripleShotSideOffset;
                    _volleyOffsets[1] = Vector2.zero;
                    _volleyOffsets[2] = _tripleShotSideOffset;
                    _volleyDirections[0] = DirectionFromAngle(-_tripleShotSideAngle);
                    _volleyDirections[1] = Vector2.up;
                    _volleyDirections[2] = DirectionFromAngle(_tripleShotSideAngle);
                    return 3;

                case WeaponTier.Quad:
                    _volleyOffsets[0] = -_quadShotOuterOffset;
                    _volleyOffsets[1] = -_quadShotInnerOffset;
                    _volleyOffsets[2] = _quadShotInnerOffset;
                    _volleyOffsets[3] = _quadShotOuterOffset;
                    _volleyDirections[0] = DirectionFromAngle(-_quadShotOuterAngle);
                    _volleyDirections[1] = Vector2.up;
                    _volleyDirections[2] = Vector2.up;
                    _volleyDirections[3] = DirectionFromAngle(_quadShotOuterAngle);
                    return 4;

                case WeaponTier.Single:
                default:
                    _volleyOffsets[0] = Vector2.zero;
                    _volleyDirections[0] = Vector2.up;
                    return 1;
            }
        }

        private Vector2 DirectionFromAngle(float degreesFromUp)
        {
            float radians = degreesFromUp * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
        }

        private void HandleBulletReleased(Bullet bullet)
        {
            _activePlayerBullets.Remove(bullet);
            bullet.gameObject.SetActive(false);
        }

        private void HandleBulletDestroyed(Bullet bullet)
        {
            _activePlayerBullets.Remove(bullet);
            Destroy(bullet.gameObject);
        }
    }
}
