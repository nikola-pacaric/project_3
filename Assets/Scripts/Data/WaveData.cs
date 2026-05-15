using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Wave Data", fileName = "WaveData")]
    public class WaveData : ScriptableObject
    {
        public enum EntrySide
        {
            Top,
            Left,
            Right
        }

        [SerializeField] private FormationData _formationData;
        [SerializeField] private EnemyData[] _enemyComposition;
        [SerializeField] private Vector2 _formationAnchorPosition = new Vector2(0f, 0f);
        [SerializeField, Min(0f)] private float _spawnDelay = 0f;
        [SerializeField] private EntrySide _entrySide = EntrySide.Top;
        [SerializeField, Min(0f)] private float _entryDistance = 8f;
        [SerializeField, Min(0f)] private float _entrySpacing = 1.5f;
        [SerializeField, Min(0f)] private float _perSlotDelay = 0f;

        public FormationData FormationData => _formationData;
        public int EnemyCompositionCount => _enemyComposition == null ? 0 : _enemyComposition.Length;
        public Vector2 FormationAnchorPosition => _formationAnchorPosition;
        public float SpawnDelay => _spawnDelay;
        public EntrySide Side => _entrySide;
        public float EntryDistance => _entryDistance;
        public float EntrySpacing => _entrySpacing;
        public float PerSlotDelay => _perSlotDelay;

        public EnemyData GetEnemyDataForSlot(int slotIndex)
        {
            if (_enemyComposition == null || slotIndex < 0 || slotIndex >= _enemyComposition.Length)
            {
                Debug.LogError($"[{nameof(WaveData)}] Slot index {slotIndex} is out of range on '{name}'.");
                return null;
            }

            return _enemyComposition[slotIndex];
        }

        private void OnValidate()
        {
            if (_formationData == null)
            {
                Debug.LogWarning($"[{nameof(WaveData)}] Assign {nameof(FormationData)} on '{name}'.");
                return;
            }

            if (_enemyComposition == null || _enemyComposition.Length == 0)
            {
                Debug.LogError($"[{nameof(WaveData)}] Wave '{name}' has no enemy composition.");
                return;
            }

            if (_enemyComposition.Length != _formationData.SlotCount)
            {
                Debug.LogError(
                    $"[{nameof(WaveData)}] Wave '{name}' has {_enemyComposition.Length} enemy entries, " +
                    $"but formation '{_formationData.name}' has {_formationData.SlotCount} slots.");
            }

            for (int i = 0; i < _enemyComposition.Length; i++)
            {
                if (_enemyComposition[i] != null) continue;

                Debug.LogError($"[{nameof(WaveData)}] Wave '{name}' has missing EnemyData at slot {i}.");
            }
        }
    }
}
