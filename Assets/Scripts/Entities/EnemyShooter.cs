using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class EnemyShooter : MonoBehaviour
    {
        private GameObject _enemyBulletPrefab;
        private int _bulletPoolDefaultCapacity = 5;
        private int _bulletPoolMaxSize = 20;
        private IObjectPool<Bullet> _bulletPool;
        private Transform _playerTransform;
        private float _nextFireTime;

        internal void Initialize(GameObject enemyBulletPrefab, int bulletPoolDefaultCapacity, int bulletPoolMaxSize)
        {
            _enemyBulletPrefab = enemyBulletPrefab;
            _bulletPoolDefaultCapacity = Mathf.Max(1, bulletPoolDefaultCapacity);
            _bulletPoolMaxSize = Mathf.Max(_bulletPoolDefaultCapacity, bulletPoolMaxSize);

            if (_bulletPool == null && _enemyBulletPrefab != null)
            {
                _bulletPool = new ObjectPool<Bullet>(
                    createFunc: CreateBullet,
                    actionOnGet: bullet => bullet.gameObject.SetActive(true),
                    actionOnRelease: bullet => bullet.gameObject.SetActive(false),
                    actionOnDestroy: bullet => Destroy(bullet.gameObject),
                    collectionCheck: true,
                    defaultCapacity: _bulletPoolDefaultCapacity,
                    maxSize: _bulletPoolMaxSize);

                PoolPrewarmer.Prewarm(_bulletPool, _bulletPoolDefaultCapacity);
            }
        }

        internal void Spawn(EnemyData data, Transform playerTransform)
        {
            _playerTransform = playerTransform;
            ScheduleNextFire(data);
        }

        internal void Tick(EnemyState state, EnemyData data)
        {
            if (!CanFireInState(state) ||
                data == null ||
                !data.CanFire ||
                Time.time < _nextFireTime)
            {
                return;
            }

            if (state == EnemyState.Entering && data.BehaviorMode != EnemyBehaviorMode.Formation)
            {
                return;
            }

            FireBullet(data);
        }

        private static bool CanFireInState(EnemyState state)
        {
            return state == EnemyState.Entering ||
                   state == EnemyState.InFormation ||
                   state == EnemyState.MotherRoaming;
        }

        private void FireBullet(EnemyData data)
        {
            if (_bulletPool != null)
            {
                Bullet bullet = _bulletPool.Get();
                float bulletSpeed = Random.Range(data.BulletSpeedMin, data.BulletSpeedMax);
                bullet.Spawn(transform.position, ResolveFireDirection(data), bulletSpeed);
                AudioManager.Instance?.PlayOneShot(AudioCue.EnemyShoot);
            }

            ScheduleNextFire(data);
        }

        private void ScheduleNextFire(EnemyData data)
        {
            if (data == null)
            {
                return;
            }

            _nextFireTime = Time.time + Random.Range(data.FireCooldownMin, data.FireCooldownMax);
        }

        private Vector2 ResolveFireDirection(EnemyData data)
        {
            if (data == null || data.BehaviorMode != EnemyBehaviorMode.Mother || _playerTransform == null)
            {
                return Vector2.down;
            }

            Vector2 direction = _playerTransform.position - transform.position;
            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.down;
        }

        private Bullet CreateBullet()
        {
            GameObject bulletObject = Instantiate(_enemyBulletPrefab);
            Bullet bullet = bulletObject.GetComponent<Bullet>();
            bullet.SetPool(_bulletPool);
            return bullet;
        }
    }
}
