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
        [SerializeField, Range(0.05f, 1f)] private float _rapidFireCooldownMultiplier = 0.5f;
        [SerializeField] private Vector2 _doubleShotOffset = new Vector2(0.18f, 0f);
        [SerializeField] private Vector2 _tripleShotSideOffset = new Vector2(0.24f, 0f);
        [SerializeField, Range(0f, 60f)] private float _tripleShotSideAngle = 35f;
        [SerializeField] private Vector2 _quadShotInnerOffset = new Vector2(0.18f, 0f);
        [SerializeField] private Vector2 _quadShotOuterOffset = new Vector2(0.48f, 0f);
        [SerializeField, Range(0f, 45f)] private float _quadShotOuterAngle = 12f;
        [SerializeField] private int _poolDefaultCapacity = 10;
        [SerializeField] private int _poolMaxSize = 50;

        [Header("Debug")]
        [SerializeField] private bool _debugAutofireActive;
        [SerializeField] private bool _debugRapidFireActive;

        private readonly HashSet<Bullet> _activePlayerBullets = new HashSet<Bullet>();
        private readonly Vector2[] _volleyOffsets = new Vector2[4];
        private readonly Vector2[] _volleyDirections = new Vector2[4];
        private bool _wasFireHeld;
        private float _nextFireTime;
        private IObjectPool<Bullet> _pool;

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

            bool fireHeld = _input.FireHeld;
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

            int volleySize = PopulateVolleyOffsets(GetWeaponTier());
            if (_activePlayerBullets.Count + volleySize > GetMaxActiveBullets())
            {
                return false;
            }

            Vector3 baseSpawnPosition = transform.position + Vector3.up * _spawnYOffset;
            for (int i = 0; i < volleySize; i++)
            {
                SpawnBullet(baseSpawnPosition + (Vector3)_volleyOffsets[i], _volleyDirections[i]);
            }

            _nextFireTime = Time.time + GetFireCooldown();
            return true;
        }

        private void SpawnBullet(Vector3 spawnPosition, Vector2 direction)
        {
            Bullet bullet = _pool.Get();
            _activePlayerBullets.Add(bullet);
            bullet.Spawn(spawnPosition, direction);
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
            float cooldown = _baseFireCooldown;
            if (IsRapidFireActive)
            {
                cooldown *= _rapidFireCooldownMultiplier;
            }

            return Mathf.Max(0f, cooldown);
        }

        private bool IsAutofireActive => _debugAutofireActive
            || (BuffManager.Instance != null && BuffManager.Instance.IsAutofireActive);

        private bool IsRapidFireActive => _debugRapidFireActive
            || (BuffManager.Instance != null && BuffManager.Instance.IsRapidFireActive);

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
