using UnityEngine;
using Warblade.Data;

namespace Warblade.Systems
{
    public class Formation : MonoBehaviour
    {
        [SerializeField] private bool _drawGizmos = true;

        private WaveData _waveData;
        private Vector3 _anchorPosition;

        public int SlotCount => _waveData == null ? 0 : _waveData.SlotCount;

        /// <summary>
        /// Assigns runtime wave slot data and anchor motion for authored waves.
        /// </summary>
        public void Configure(WaveData waveData)
        {
            _waveData = waveData;
            Vector2 anchorPosition = waveData != null ? waveData.FormationAnchorPosition : Vector2.zero;
            _anchorPosition = anchorPosition;
            transform.position = anchorPosition;
        }

        private void Update()
        {
            if (_waveData == null) return;

            transform.position = _anchorPosition + (Vector3)ResolveWaveMotionOffset();
        }

        public bool HasSlot(int slotIndex)
        {
            return _waveData != null && _waveData.HasSlot(slotIndex);
        }

        public Vector2 GetSlotWorldPosition(int slotIndex)
        {
            if (!HasSlot(slotIndex))
            {
                Debug.LogError($"[{nameof(Formation)}] Slot index {slotIndex} is out of range on '{name}'.");
                return transform.position;
            }

            Vector2 motionOffset = (Vector2)transform.position - (Vector2)_anchorPosition;
            return _waveData.GetSlotWorldPosition(slotIndex) + motionOffset;
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
