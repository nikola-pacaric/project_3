using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Entities;

namespace Warblade.Systems
{
    /// <summary>
    /// Owns pooled pickup instances that enter play from enemy drops.
    /// </summary>
    public class PickupDropPool : MonoBehaviour
    {
        public static PickupDropPool Instance { get; private set; }

        [SerializeField] private Pickup _pickupPrefab;
        [SerializeField] private int _defaultCapacity = 12;
        [SerializeField] private int _maxSize = 50;

        private IObjectPool<Pickup> _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_pickupPrefab == null)
            {
                Debug.LogError($"[{nameof(PickupDropPool)}] Assign a {nameof(Pickup)} prefab on '{name}'.");
                return;
            }

            _pool = new ObjectPool<Pickup>(
                createFunc: CreatePickup,
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Rolls an enemy drop table and releases a pickup into play when the roll succeeds.
        /// </summary>
        public bool TryDrop(DropTable dropTable, Vector2 position)
        {
            if (dropTable == null) return false;
            if (!dropTable.TryRoll(out PickupData pickupData)) return false;

            Drop(pickupData, position);
            return true;
        }

        private Pickup Drop(PickupData data, Vector2 position)
        {
            if (data == null)
            {
                Debug.LogWarning($"[{nameof(PickupDropPool)}] Cannot drop a pickup without {nameof(PickupData)}.");
                return null;
            }

            if (_pool == null)
            {
                Debug.LogError($"[{nameof(PickupDropPool)}] Cannot drop '{data.name}' because the pool was not initialized.");
                return null;
            }

            Pickup pickup = _pool.Get();
            pickup.Spawn(position, data);
            return pickup;
        }

        private Pickup CreatePickup()
        {
            Pickup pickup = Instantiate(_pickupPrefab, transform);
            pickup.SetPool(_pool);
            return pickup;
        }
    }
}
