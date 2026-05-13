using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Drop Table", fileName = "DropTable")]
    public class DropTable : ScriptableObject
    {
        [System.Serializable]
        private struct WeightedPickup
        {
            [SerializeField] private PickupData _pickupData;
            [SerializeField, Min(0f)] private float _weight;

            public PickupData PickupData => _pickupData;
            public float Weight => _weight;
        }

        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.2f;
        [SerializeField] private WeightedPickup[] _weightedPickups;

        public float DropChance => _dropChance;

        public bool TryRoll(out PickupData pickupData)
        {
            pickupData = null;

            if (_weightedPickups == null || _weightedPickups.Length == 0)
            {
                return false;
            }

            if (Random.value > _dropChance)
            {
                return false;
            }

            float totalWeight = 0f;
            for (int i = 0; i < _weightedPickups.Length; i++)
            {
                if (_weightedPickups[i].PickupData == null) continue;
                totalWeight += Mathf.Max(0f, _weightedPickups[i].Weight);
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < _weightedPickups.Length; i++)
            {
                WeightedPickup entry = _weightedPickups[i];
                if (entry.PickupData == null) continue;

                roll -= Mathf.Max(0f, entry.Weight);
                if (roll <= 0f)
                {
                    pickupData = entry.PickupData;
                    return true;
                }
            }

            return false;
        }
    }
}
