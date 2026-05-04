using UnityEngine;
using UnityEngine.Pool;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float _speed = 15f;
        [SerializeField] private Vector2 _direction = Vector2.up;
        [SerializeField] private float _maxLifetimeDistance = 12f;
        [SerializeField] private int _damage = 1;

        private Vector3 _spawnPosition;
        private IObjectPool<Bullet> _pool;
        private bool _isActive;

        public void SetPool(IObjectPool<Bullet> pool)
        {
            _pool = pool;
        }

        public void Spawn(Vector3 position)
        {
            transform.position = position;
            _spawnPosition = position;
            _isActive = true;
        }

        private void Update()
        {
            if (!_isActive) return;

            transform.Translate(_direction * (_speed * Time.deltaTime));

            if (Vector3.Distance(transform.position, _spawnPosition) > _maxLifetimeDistance)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
            }
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (!_isActive) return;
            _isActive = false;

            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
