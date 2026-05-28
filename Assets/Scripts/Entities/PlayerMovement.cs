using UnityEngine;
using UnityEngine.Serialization;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [FormerlySerializedAs("_speed")]
        [SerializeField, Min(0f)] private float _baseSpeed = 8f;
        [SerializeField, Min(0f)] private float _speedPerSpeedLevel = 0.75f;
        [SerializeField] private float _minX = -8.5f;
        [SerializeField] private float _maxX = 8.5f;

        [Header("Runtime Debug")]
        [SerializeField] private float _currentMovementSpeed;

        public float BaseSpeed => _baseSpeed;
        public float SpeedPerSpeedLevel => _speedPerSpeedLevel;

        private void Update()
        {
            if (_input == null) return;

            _currentMovementSpeed = GetMovementSpeed();

            Vector3 position = transform.position;
            position.x += _input.MoveAxis * _currentMovementSpeed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, _minX, _maxX);
            transform.position = position;
        }

        private float GetMovementSpeed()
        {
            int speedLevel = RunStatsManager.Instance != null
                ? RunStatsManager.Instance.EffectiveSpeedLevel
                : 0;

            return _baseSpeed + speedLevel * _speedPerSpeedLevel;
        }
    }
}
