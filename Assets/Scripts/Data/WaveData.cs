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
        [SerializeField] private Vector2 _formationAnchorPosition = new Vector2(0f, 0f);
        [SerializeField, Min(0f)] private float _spawnDelay = 0f;
        [SerializeField] private EntrySide _entrySide = EntrySide.Top;
        [SerializeField, Min(0f)] private float _entryDistance = 8f;
        [SerializeField, Min(0f)] private float _entrySpacing = 1.5f;
        [SerializeField, Min(0f)] private float _perSlotDelay = 0f;

        public FormationData FormationData => _formationData;
        public Vector2 FormationAnchorPosition => _formationAnchorPosition;
        public float SpawnDelay => _spawnDelay;
        public EntrySide Side => _entrySide;
        public float EntryDistance => _entryDistance;
        public float EntrySpacing => _entrySpacing;
        public float PerSlotDelay => _perSlotDelay;
    }
}
