using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float _speed = 15f;
        [SerializeField] private Vector2 _direction = Vector2.up;
        [SerializeField] private float _maxLifetimeDistance = 12f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private VfxCue _impactVfxCue = VfxCue.BulletImpact;
        [SerializeField, Min(0f)] private float _spriteRotationSpeedDegreesPerSecond = 180f;

        private Vector3 _spawnPosition;
        private IObjectPool<Bullet> _pool;
        private SpriteRenderer _spriteRenderer;
        private Quaternion _defaultSpriteLocalRotation;
        private bool _spinSprite;
        private bool _isActive;

        public static int ActiveBulletCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveBulletCount()
        {
            ActiveBulletCount = 0;
        }

        public void SetPool(IObjectPool<Bullet> pool)
        {
            _pool = pool;
        }

        public void SetSpriteSpin(bool shouldSpin)
        {
            ResolveSpriteRenderer();

            _spinSprite = shouldSpin;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.transform.localRotation = _defaultSpriteLocalRotation;
            }
        }

        public void Spawn(Vector3 position)
        {
            Spawn(position, _direction);
        }

        public void Spawn(Vector3 position, Vector2 direction)
        {
            transform.position = position;
            _direction = direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized
                : Vector2.up;
            transform.up = _direction;
            _spawnPosition = position;
            SetActiveState(true);
        }

        public void Spawn(Vector3 position, Vector2 direction, float speed)
        {
            _speed = Mathf.Max(0f, speed);
            Spawn(position, direction);
        }

        private void OnDestroy()
        {
            SetActiveState(false);
        }

        private void Awake()
        {
            ResolveSpriteRenderer();
        }

        private void Update()
        {
            if (!_isActive) return;

            transform.Translate(_direction * (_speed * Time.deltaTime), Space.World);
            TickSpriteSpin();

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
                Vector3 hitPoint = other.ClosestPoint(transform.position);

                if (damageable is IHitPointDamageable hitPointDamageable)
                {
                    hitPointDamageable.TakeDamage(_damage, hitPoint);
                }
                else
                {
                    damageable.TakeDamage(_damage);
                }

                VfxManager.Instance?.Play(_impactVfxCue, hitPoint, -_direction);
            }
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (!_isActive) return;
            SetActiveState(false);

            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetActiveState(bool isActive)
        {
            if (_isActive == isActive) return;

            _isActive = isActive;
            ActiveBulletCount = Mathf.Max(0, ActiveBulletCount + (isActive ? 1 : -1));
        }

        private void ResolveSpriteRenderer()
        {
            if (_spriteRenderer != null)
            {
                return;
            }

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _defaultSpriteLocalRotation = _spriteRenderer == null ? Quaternion.identity : _spriteRenderer.transform.localRotation;
        }

        private void TickSpriteSpin()
        {
            if (!_spinSprite || _spriteRenderer == null || _spriteRotationSpeedDegreesPerSecond <= 0f)
            {
                return;
            }

            _spriteRenderer.transform.Rotate(
                0f,
                0f,
                _spriteRotationSpeedDegreesPerSecond * Time.deltaTime,
                Space.Self);
        }
    }
}
