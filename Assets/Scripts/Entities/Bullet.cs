using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
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
        [Tooltip("When enabled, the bullet visually rotates to face its travel direction. Disable this for bullets that should keep their prefab rotation.")]
        [SerializeField] private bool _alignRotationToDirection = true;
        [SerializeField, Min(0f)] private float _spriteRotationSpeedDegreesPerSecond = 180f;

        private Vector3 _spawnPosition;
        private IObjectPool<Bullet> _pool;
        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer[] _spriteRenderers;
        private Color[] _defaultSpriteColors;
        private Light2D[] _lights;
        private Color[] _defaultLightColors;
        private Quaternion _defaultRootRotation;
        private Quaternion _defaultSpriteLocalRotation;
        private int _defaultDamage;
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

        public void SetDamage(int damage)
        {
            _damage = Mathf.Max(1, damage);
        }

        public void ResetDamage()
        {
            _damage = _defaultDamage;
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

        public void ResetPresentationColors()
        {
            ResolvePresentationRenderers();

            if (_spriteRenderers != null && _defaultSpriteColors != null)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] != null && i < _defaultSpriteColors.Length)
                    {
                        _spriteRenderers[i].color = _defaultSpriteColors[i];
                    }
                }
            }

            if (_lights != null && _defaultLightColors != null)
            {
                for (int i = 0; i < _lights.Length; i++)
                {
                    if (_lights[i] != null && i < _defaultLightColors.Length)
                    {
                        _lights[i].color = _defaultLightColors[i];
                    }
                }
            }
        }

        public void SetPresentationColor(Color color)
        {
            ResolvePresentationRenderers();

            if (_spriteRenderers != null && _defaultSpriteColors != null)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] == null || i >= _defaultSpriteColors.Length)
                    {
                        continue;
                    }

                    Color defaultColor = _defaultSpriteColors[i];
                    float intensity = Mathf.Max(defaultColor.r, defaultColor.g, defaultColor.b);
                    _spriteRenderers[i].color = new Color(
                        color.r * intensity,
                        color.g * intensity,
                        color.b * intensity,
                        defaultColor.a);
                }
            }

            if (_lights != null && _defaultLightColors != null)
            {
                for (int i = 0; i < _lights.Length; i++)
                {
                    if (_lights[i] == null || i >= _defaultLightColors.Length)
                    {
                        continue;
                    }

                    Color defaultColor = _defaultLightColors[i];
                    _lights[i].color = new Color(color.r, color.g, color.b, defaultColor.a);
                }
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
            ApplySpawnRotation();
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
            _defaultDamage = Mathf.Max(1, _damage);
            _damage = _defaultDamage;
            _defaultRootRotation = transform.rotation;
            ResolveSpriteRenderer();
            ResolvePresentationRenderers();
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

        private void ResolvePresentationRenderers()
        {
            if (_spriteRenderers == null || _defaultSpriteColors == null)
            {
                _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                _defaultSpriteColors = new Color[_spriteRenderers.Length];

                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    _defaultSpriteColors[i] = _spriteRenderers[i] == null
                        ? Color.white
                        : _spriteRenderers[i].color;
                }
            }

            if (_lights == null || _defaultLightColors == null)
            {
                _lights = GetComponentsInChildren<Light2D>(true);
                _defaultLightColors = new Color[_lights.Length];

                for (int i = 0; i < _lights.Length; i++)
                {
                    _defaultLightColors[i] = _lights[i] == null
                        ? Color.white
                        : _lights[i].color;
                }
            }
        }

        private void ApplySpawnRotation()
        {
            if (_alignRotationToDirection)
            {
                transform.up = _direction;
            }
            else
            {
                transform.rotation = _defaultRootRotation;
            }
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
