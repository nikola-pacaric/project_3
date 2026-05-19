using UnityEngine;
using Warblade.Data;

namespace Warblade.Systems
{
    public class Formation : MonoBehaviour
    {
        [SerializeField] private FormationData _data;
        [SerializeField] private WaveData _waveData;
        [SerializeField] private bool _drawGizmos = true;

        private Vector3 _anchorPosition;

        public int SlotCount
        {
            get
            {
                if (_waveData != null) return _waveData.SlotCount;
                return _data == null ? 0 : _data.SlotCount;
            }
        }

        /// <summary>
        /// Assigns runtime formation data and anchor position for waves.
        /// </summary>
        public void Configure(FormationData data, Vector2 anchorPosition)
        {
            _data = data;
            _waveData = null;
            _anchorPosition = anchorPosition;
            transform.position = anchorPosition;
        }

        /// <summary>
        /// Assigns runtime wave slot data and anchor motion for authored waves.
        /// </summary>
        public void Configure(WaveData waveData)
        {
            _data = null;
            _waveData = waveData;
            Vector2 anchorPosition = waveData != null ? waveData.FormationAnchorPosition : Vector2.zero;
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

            if (_data == null && _waveData == null)
            {
                Debug.LogWarning($"[{nameof(Formation)}] Assign FormationData on '{name}'.");
            }
        }

        private void Update()
        {
            if (_waveData != null)
            {
                transform.position = _anchorPosition + (Vector3)ResolveWaveMotionOffset();
                return;
            }

            if (_data == null) return;

            float offsetX = Mathf.Sin(Time.time * _data.BreatheSpeed) * _data.BreatheAmplitude;
            transform.position = _anchorPosition + Vector3.right * offsetX;
        }

        public void SetStaticAnchor(Vector2 anchorPosition)
        {
            _anchorPosition = anchorPosition;
            transform.position = anchorPosition;
        }

        public bool HasSlot(int slotIndex)
        {
            if (_waveData != null) return _waveData.HasSlot(slotIndex);
            return _data != null && _data.HasSlot(slotIndex);
        }

        public Vector2 GetSlotWorldPosition(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(Formation)}] Slot index {slotIndex} is out of range on '{name}'.");
                return transform.position;
            }

            if (_waveData != null)
            {
                Vector2 motionOffset = (Vector2)transform.position - (Vector2)_anchorPosition;
                return _waveData.GetSlotWorldPosition(slotIndex) + motionOffset;
            }

            return (Vector2)transform.position + _data.GetSlot(slotIndex).LocalPosition;
        }

        public Vector2 GetSlotEntryControlOffset(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(Formation)}] Slot index {slotIndex} is out of range on '{name}'.");
                return Vector2.zero;
            }

            if (_waveData != null)
            {
                return _waveData.GetSlot(slotIndex).EntryControlOffset;
            }

            return _data.GetSlot(slotIndex).EntryControlOffset;
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || SlotCount == 0) return;

            Gizmos.color = new Color(0.2f, 1f, 1f, 0.75f);
            for (int i = 0; i < SlotCount; i++)
            {
                Vector2 slotWorld = GetSlotWorldPosition(i);
                Gizmos.DrawWireSphere(slotWorld, 0.16f);
                Gizmos.DrawLine(transform.position, slotWorld);
            }
        }

        private Vector2 ResolveWaveMotionOffset()
        {
            if (_waveData == null)
            {
                return Vector2.zero;
            }

            switch (_waveData.MotionMode)
            {
                case WaveData.FormationMotionMode.HorizontalSway:
                    float offsetX = Mathf.Sin((Time.time * _waveData.FormationSwaySpeed) + _waveData.FormationSwayPhase) *
                        _waveData.FormationSwayAmplitude;
                    return Vector2.right * offsetX;
                default:
                    return Vector2.zero;
            }
        }
    }
}
