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

        [Header("Super Shot")]
        [SerializeField] private Vector2 _superShotSideOffset = new Vector2(0.24f, 0f);
        [SerializeField, Range(0f, 60f)] private float _superShotSideAngle = 35f;

        [Header("FireBall")]
        [SerializeField] private GameObject _fireballBulletPrefab;
        [SerializeField, Min(1)] private int _fireballBulletDamage = 6;
        [SerializeField, Min(1)] private int _fireballActiveBulletCost = 4;

        [Header("Pooling")]
        [SerializeField] private int _poolDefaultCapacity = 10;
        [SerializeField] private int _poolMaxSize = 50;

        [Header("Bullet Colors")]
        [SerializeField] private Color _doubleShotBulletColor = new Color(0.455f, 1f, 0.173f, 1f);
        [SerializeField] private Color _tripleShotBulletColor = new Color(0.059f, 0.776f, 1f, 1f);
        [SerializeField] private Color _quadShotBulletColor = new Color(1f, 0.808f, 0.18f, 1f);
        [SerializeField] private Color _superShotBulletColor = new Color(1f, 0.2f, 0.9f, 1f);

        [Header("Bullet Damage")]
        [SerializeField, Min(1)] private int _doubleShotBulletDamage = 1;
        [SerializeField, Min(1)] private int _tripleShotBulletDamage = 1;
        [SerializeField, Min(1)] private int _quadShotBulletDamage = 1;
        [SerializeField, Min(1)] private int _superShotBulletDamage = 3;

        [Header("Debug")]
        [SerializeField] private bool _debugAutofireActive;
        [SerializeField] private bool _debugRapidFireActive;

        private readonly Dictionary<Bullet, int> _activePlayerBulletCosts = new Dictionary<Bullet, int>();
        private readonly Vector2[] _volleyOffsets = new Vector2[4];
        private readonly Vector2[] _volleyDirections = new Vector2[4];
        private int _activePlayerBulletSlots;
        private bool _wasFireHeld;
        private bool _canShoot = true;
        private bool _suppressFireUntilReleased;
        private float _nextFireTime;
        private int _queuedRapidFireVolleys;
        private float _nextRapidFireVolleyTime;
        private IObjectPool<Bullet> _regularBulletPool;
        private IObjectPool<Bullet> _fireballBulletPool;

        public int BaseMaxActiveBullets => _baseMaxActiveBullets;

        private void Awake()
        {
            _regularBulletPool = new ObjectPool<Bullet>(
                createFunc: () => CreateBullet(_bulletPrefab, _regularBulletPool),
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: HandleBulletReleased,
                actionOnDestroy: HandleBulletDestroyed,
                collectionCheck: true,
                defaultCapacity: _poolDefaultCapacity,
                maxSize: _poolMaxSize);

            PoolPrewarmer.Prewarm(_regularBulletPool, _poolDefaultCapacity);

            if (_fireballBulletPrefab != null)
            {
                _fireballBulletPool = new ObjectPool<Bullet>(
                    createFunc: () => CreateBullet(_fireballBulletPrefab, _fireballBulletPool),
                    actionOnGet: b => b.gameObject.SetActive(true),
                    actionOnRelease: HandleBulletReleased,
                    actionOnDestroy: HandleBulletDestroyed,
                    collectionCheck: true,
                    defaultCapacity: _poolDefaultCapacity,
                    maxSize: _poolMaxSize);

                PoolPrewarmer.Prewarm(_fireballBulletPool, _poolDefaultCapacity);
            }
        }

        private void OnValidate()
        {
            _poolDefaultCapacity = Mathf.Max(1, _poolDefaultCapacity);
            _poolMaxSize = Mathf.Max(_poolDefaultCapacity, _poolMaxSize);
            _rapidFireBurstVolleyCount = Mathf.Max(1, _rapidFireBurstVolleyCount);
            _rapidFireBurstInterval = Mathf.Max(0f, _rapidFireBurstInterval);
            _baseFireCooldown = Mathf.Max(0f, _baseFireCooldown);
            _doubleShotBulletDamage = Mathf.Max(1, _doubleShotBulletDamage);
            _tripleShotBulletDamage = Mathf.Max(1, _tripleShotBulletDamage);
            _quadShotBulletDamage = Mathf.Max(1, _quadShotBulletDamage);
            _superShotBulletDamage = Mathf.Max(1, _superShotBulletDamage);
            _fireballBulletDamage = Mathf.Max(1, _fireballBulletDamage);
            _fireballActiveBulletCost = Mathf.Max(1, _fireballActiveBulletCost);
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

        private Bullet CreateBullet(GameObject bulletPrefab, IObjectPool<Bullet> owningPool)
        {
            GameObject go = Instantiate(bulletPrefab);
            Bullet bullet = go.GetComponent<Bullet>();
            bullet.SetPool(owningPool);
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

            WeaponTier weaponTier = GetWeaponTier();
            int volleySize = PopulateVolleyOffsets(weaponTier);
            if (!HasRoomForVolley(GetVolleyActiveBulletCost(weaponTier, volleySize)))
            {
                return false;
            }

            int burstVolleyCount = GetRequestedBurstVolleyCount();
            SpawnCurrentVolley(volleySize, weaponTier);
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

        private void SpawnBullet(Vector3 spawnPosition, Vector2 direction, WeaponTier weaponTier)
        {
            IObjectPool<Bullet> bulletPool = ResolveBulletPool(weaponTier);
            Bullet bullet = bulletPool.Get();
            int activeBulletCost = GetActiveBulletCost(weaponTier);
            _activePlayerBulletCosts[bullet] = activeBulletCost;
            _activePlayerBulletSlots += activeBulletCost;
            ApplyBulletPresentationColor(bullet, weaponTier);
            ApplyBulletDamage(bullet, weaponTier);
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

                case WeaponTier.Super:
                    bullet.SetPresentationColor(_superShotBulletColor);
                    break;

                case WeaponTier.FireBall:
                    bullet.ResetPresentationColors();
                    break;

                case WeaponTier.Single:
                default:
                    bullet.ResetPresentationColors();
                    break;
            }
        }

        private void ApplyBulletDamage(Bullet bullet, WeaponTier weaponTier)
        {
            if (bullet == null)
            {
                return;
            }

            switch (weaponTier)
            {
                case WeaponTier.Double:
                    bullet.SetDamage(_doubleShotBulletDamage);
                    break;

                case WeaponTier.Triple:
                    bullet.SetDamage(_tripleShotBulletDamage);
                    break;

                case WeaponTier.Quad:
                    bullet.SetDamage(_quadShotBulletDamage);
                    break;

                case WeaponTier.Super:
                    bullet.SetDamage(_superShotBulletDamage);
                    break;

                case WeaponTier.FireBall:
                    bullet.SetDamage(_fireballBulletDamage);
                    break;

                case WeaponTier.Single:
                default:
                    bullet.ResetDamage();
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
            int bulletsLevel = RunStatsManager.Instance != null
                ? RunStatsManager.Instance.EffectiveBulletsLevel
                : 0;

            int bulletsPerLevel = BuffManager.Instance != null
                ? BuffManager.Instance.NumberOfBulletsPerBulletLevel
                : 0;

            return Mathf.Max(1, _baseMaxActiveBullets + bulletsLevel * bulletsPerLevel);
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
            || (BuffManager.Instance != null && BuffManager.Instance.IsAutofireActive)
            || (RunStatsManager.Instance != null && RunStatsManager.Instance.IsShopAutofireActive);

        private bool IsRapidFireActive => _debugRapidFireActive
            || (BuffManager.Instance != null && BuffManager.Instance.IsRapidFireActive);

        private int GetRequestedBurstVolleyCount()
        {
            return IsRapidFireActive ? _rapidFireBurstVolleyCount : 1;
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

            WeaponTier weaponTier = GetWeaponTier();
            int volleySize = PopulateVolleyOffsets(weaponTier);
            if (!HasRoomForVolley(GetVolleyActiveBulletCost(weaponTier, volleySize)))
            {
                return;
            }

            SpawnCurrentVolley(volleySize, weaponTier);
            _queuedRapidFireVolleys--;
            _nextRapidFireVolleyTime = Time.time + _rapidFireBurstInterval;
        }

        private void SpawnCurrentVolley(int volleySize, WeaponTier weaponTier)
        {
            Vector3 baseSpawnPosition = transform.position + Vector3.up * _spawnYOffset;
            for (int i = 0; i < volleySize; i++)
            {
                SpawnBullet(baseSpawnPosition + (Vector3)_volleyOffsets[i], _volleyDirections[i], weaponTier);
            }

            AudioManager.Instance?.PlayOneShot(AudioCue.PlayerShoot);
        }

        private bool HasRoomForVolley(int requiredActiveBulletSlots)
        {
            return GetMaxActiveBullets() - _activePlayerBulletSlots >= requiredActiveBulletSlots;
        }

        private int GetVolleyActiveBulletCost(WeaponTier weaponTier, int volleySize)
        {
            return Mathf.Max(1, volleySize) * GetActiveBulletCost(weaponTier);
        }

        private int GetActiveBulletCost(WeaponTier weaponTier)
        {
            return weaponTier == WeaponTier.FireBall
                ? _fireballActiveBulletCost
                : 1;
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

                case WeaponTier.Super:
                    _volleyOffsets[0] = -_superShotSideOffset;
                    _volleyOffsets[1] = Vector2.zero;
                    _volleyOffsets[2] = _superShotSideOffset;
                    _volleyDirections[0] = DirectionFromAngle(-_superShotSideAngle);
                    _volleyDirections[1] = Vector2.up;
                    _volleyDirections[2] = DirectionFromAngle(_superShotSideAngle);
                    return 3;

                case WeaponTier.FireBall:
                    _volleyOffsets[0] = Vector2.zero;
                    _volleyDirections[0] = Vector2.up;
                    return 1;

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

        private IObjectPool<Bullet> ResolveBulletPool(WeaponTier weaponTier)
        {
            if (weaponTier == WeaponTier.FireBall && _fireballBulletPool != null)
            {
                return _fireballBulletPool;
            }

            return _regularBulletPool;
        }

        private void HandleBulletReleased(Bullet bullet)
        {
            RemoveActiveBulletCost(bullet);
            bullet.gameObject.SetActive(false);
        }

        private void HandleBulletDestroyed(Bullet bullet)
        {
            RemoveActiveBulletCost(bullet);
            Destroy(bullet.gameObject);
        }

        private void RemoveActiveBulletCost(Bullet bullet)
        {
            if (!_activePlayerBulletCosts.Remove(bullet, out int activeBulletCost))
            {
                return;
            }

            _activePlayerBulletSlots = Mathf.Max(0, _activePlayerBulletSlots - activeBulletCost);
        }
    }
}
