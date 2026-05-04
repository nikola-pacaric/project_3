using UnityEngine;

namespace Warblade.Systems
{
    public class Formation : MonoBehaviour
    {
        [SerializeField] private Warblade.Data.FormationData _data;
        [SerializeField] private bool _drawGizmos = true;

        private Vector3 _anchorPosition;

        public int SlotCount => _data == null ? 0 : _data.SlotCount;

        /// <summary>
        /// Assigns runtime formation data and anchor position for waves.
        /// </summary>
        public void Configure(Warblade.Data.FormationData data, Vector2 anchorPosition)
        {
            _data = data;
            _anchorPosition = anchorPosition;
            transform.position = anchorPosition;
        }

        private void Start()
        {
            _anchorPosition = transform.position;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(Formation)}] Assign FormationData on '{name}'.");
            }
        }

        private void Update()
        {
            if (_data == null) return;

            float offsetX = Mathf.Sin(Time.time * _data.BreatheSpeed) * _data.BreatheAmplitude;
            transform.position = _anchorPosition + Vector3.right * offsetX;
        }

        public bool HasSlot(int slotIndex)
        {
            return _data != null && _data.HasSlot(slotIndex);
        }

        public Vector2 GetSlotWorldPosition(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(Formation)}] Slot index {slotIndex} is out of range on '{name}'.");
                return transform.position;
            }

            return (Vector2)transform.position + _data.GetSlot(slotIndex).LocalPosition;
        }

        public Warblade.Data.EnemyData GetSlotEnemyData(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(Formation)}] Slot index {slotIndex} is out of range on '{name}'.");
                return null;
            }

            return _data.GetSlot(slotIndex).EnemyData;
        }

        public Vector2 GetSlotEntryControlOffset(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(Formation)}] Slot index {slotIndex} is out of range on '{name}'.");
                return Vector2.zero;
            }

            return _data.GetSlot(slotIndex).EntryControlOffset;
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _data == null || _data.SlotCount == 0) return;

            Gizmos.color = new Color(0.2f, 1f, 1f, 0.75f);
            for (int i = 0; i < _data.SlotCount; i++)
            {
                Vector2 slotWorld = (Vector2)transform.position + _data.GetSlot(i).LocalPosition;
                Gizmos.DrawWireSphere(slotWorld, 0.16f);
                Gizmos.DrawLine(transform.position, slotWorld);
            }
        }
    }
}
