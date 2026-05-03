using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Formation Data", fileName = "FormationData")]
    public class FormationData : ScriptableObject
    {
        [System.Serializable]
        public struct SlotDefinition
        {
            public Vector2 LocalPosition;
            public EnemyData EnemyData;
            [Tooltip("Added to the midpoint of (spawn start -> slot world) for Bezier entry shape.")]
            public Vector2 EntryControlOffset;
        }

        [SerializeField] private SlotDefinition[] _slots;
        [SerializeField] private float _breatheAmplitude = 0.5f;
        [SerializeField] private float _breatheSpeed = 1f;

        public int SlotCount => _slots == null ? 0 : _slots.Length;
        public float BreatheAmplitude => _breatheAmplitude;
        public float BreatheSpeed => _breatheSpeed;

        public bool HasSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SlotCount;
        }

        public SlotDefinition GetSlot(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(FormationData)}] Slot index {slotIndex} is out of range on '{name}'.");
                return default;
            }

            return _slots[slotIndex];
        }
    }
}
