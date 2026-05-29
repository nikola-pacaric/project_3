using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupData _data;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField, Min(0f)] private float _fallSpeedMin = 2f;
        [SerializeField, Min(0f)] private float _fallSpeedMax = 3.25f;
        [SerializeField] private float _rotationSpeedDegreesPerSecond = 180f;
        [SerializeField] private float _despawnY = -6.25f;

        private IObjectPool<Pickup> _pool;
        private bool _hasResolved;
        private float _currentFallSpeed;

        public static int ActivePickupCount { get; private set; }
        public PickupData Data => _data;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActivePickupCount()
        {
            ActivePickupCount = 0;
        }

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            ApplyVisualsFromData();
        }

        private void OnEnable()
        {
            _hasResolved = false;
        }

        private void OnValidate()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _fallSpeedMin = Mathf.Max(0f, _fallSpeedMin);
            _fallSpeedMax = Mathf.Max(_fallSpeedMin, _fallSpeedMax);
            ApplyVisualsFromData();
        }

        public void SetPool(IObjectPool<Pickup> pool)
        {
            _pool = pool;
        }

        public void Spawn(Vector2 position, PickupData data)
        {
            transform.position = position;
            _data = data;
            _hasResolved = false;
            _currentFallSpeed = Random.Range(_fallSpeedMin, _fallSpeedMax);
            ActivePickupCount++;
            ApplyVisualsFromData();
        }

        private void OnDestroy()
        {
            ResolveActivePickup();
        }

        private void Update()
        {
            if (_hasResolved) return;

            transform.Translate(Vector3.down * (_currentFallSpeed * Time.deltaTime), Space.World);
            transform.Rotate(0f, 0f, _rotationSpeedDegreesPerSecond * Time.deltaTime, Space.Self);

            if (transform.position.y <= _despawnY)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasResolved) return;
            if (other.GetComponentInParent<PlayerHealth>() == null) return;

            ApplyEffect();
            VfxManager.Instance?.Play(VfxCue.PickupCollect, transform.position);
            ReturnToPool();
        }

        private void ApplyEffect()
        {
            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(Pickup)}] Pickup '{name}' has no {nameof(PickupData)} assigned.");
                return;
            }

            RunStatsManager runStats = RunStatsManager.Instance;
            if (runStats == null)
            {
                Debug.LogError($"[{nameof(Pickup)}] Cannot apply '{_data.DisplayName}' without a {nameof(RunStatsManager)}.");
                return;
            }

            switch (_data.EffectType)
            {
                case PickupEffectType.Cash10:
                case PickupEffectType.Cash50:
                case PickupEffectType.Cash100:
                case PickupEffectType.Cash200:
                    runStats.AddCash(_data.Amount);
                    break;

                case PickupEffectType.WeaponSingle:
                    runStats.EquipWeaponTierFromPickup(WeaponTier.Single);
                    break;

                case PickupEffectType.WeaponDouble:
                    runStats.EquipWeaponTierFromPickup(WeaponTier.Double);
                    break;

                case PickupEffectType.WeaponTriple:
                    runStats.EquipWeaponTierFromPickup(WeaponTier.Triple);
                    break;

                case PickupEffectType.WeaponQuad:
                    runStats.EquipWeaponTierFromPickup(WeaponTier.Quad);
                    break;

                case PickupEffectType.SpeedUp:
                    runStats.IncreaseSpeed(_data.Amount);
                    break;

                case PickupEffectType.BulletsUp:
                    runStats.IncreaseBullets(_data.Amount);
                    break;

                case PickupEffectType.TimeUp:
                    runStats.IncreaseTime(_data.Amount);
                    break;

                case PickupEffectType.Armour:
                    runStats.AddArmour(_data.Amount);
                    break;

                case PickupEffectType.ExtraLife:
                    runStats.AddLife(_data.Amount);
                    break;

                case PickupEffectType.SuckerSpeed:
                    runStats.ApplySuckerPenalty(RunStatType.Speed);
                    break;

                case PickupEffectType.SuckerBullets:
                    runStats.ApplySuckerPenalty(RunStatType.Bullets);
                    break;

                case PickupEffectType.SuckerTime:
                    runStats.ApplySuckerPenalty(RunStatType.Time);
                    break;

                case PickupEffectType.Autofire:
                    ActivateTimedBuff(BuffType.Autofire);
                    break;

                case PickupEffectType.RapidFire:
                    ActivateTimedBuff(BuffType.RapidFire);
                    break;

                case PickupEffectType.Shield:
                    ActivateTimedBuff(BuffType.Shield);
                    break;

                default:
                    Debug.LogWarning($"[{nameof(Pickup)}] Unsupported pickup effect: {_data.EffectType}.");
                    break;
            }
        }

        private void ActivateTimedBuff(BuffType buffType)
        {
            BuffManager buffManager = BuffManager.Instance;
            if (buffManager == null)
            {
                Debug.LogError($"[{nameof(Pickup)}] Cannot apply '{_data.DisplayName}' without a {nameof(BuffManager)}.");
                return;
            }

            buffManager.ActivateBuff(buffType);
        }

        private void ApplyVisualsFromData()
        {
            if (_spriteRenderer == null || _data == null) return;

            if (_data.Sprite != null)
            {
                _spriteRenderer.sprite = _data.Sprite;
            }

            _spriteRenderer.color = _data.SpriteColor;
        }

        private void ReturnToPool()
        {
            if (_hasResolved) return;
            ResolveActivePickup();

            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ResolveActivePickup()
        {
            if (_hasResolved) return;

            _hasResolved = true;
            ActivePickupCount = Mathf.Max(0, ActivePickupCount - 1);
        }
    }
}
