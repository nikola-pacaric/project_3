using UnityEngine;
using UnityEngine.Pool;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private float _spawnYOffset = 0.5f;
        [SerializeField] private int _poolDefaultCapacity = 10;
        [SerializeField] private int _poolMaxSize = 50;

        private bool _wasFireHeld;
        private IObjectPool<Bullet> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<Bullet>(
                createFunc: CreateBullet,
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
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

            if (firePressedThisFrame)
            {
                Bullet bullet = _pool.Get();
                bullet.Spawn(transform.position + Vector3.up * _spawnYOffset);
            }

            _wasFireHeld = fireHeld;
        }
    }
}
