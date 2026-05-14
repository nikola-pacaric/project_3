using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warblade.Data
{
    [Serializable]
    public class BossPhaseData
    {
        [SerializeField] private string _phaseName = "Phase";
        [SerializeField, Range(0f, 1f)] private float _healthThreshold = 1f;
        [SerializeField] private BossMovementBehavior _movementBehavior = BossMovementBehavior.HoldPosition;
        [SerializeField, Min(0f)] private float _movementAmplitude = 1.5f;
        [SerializeField, Min(0f)] private float _movementSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _attackCooldownMin = 1f;
        [SerializeField, Min(0f)] private float _attackCooldownMax = 2f;
        [SerializeField] private List<BossAttackPatternData> _attackPatterns = new List<BossAttackPatternData>();

        public string PhaseName => _phaseName;
        public float HealthThreshold => _healthThreshold;
        public BossMovementBehavior MovementBehavior => _movementBehavior;
        public float MovementAmplitude => _movementAmplitude;
        public float MovementSpeed => _movementSpeed;
        public float AttackCooldownMin => _attackCooldownMin;
        public float AttackCooldownMax => _attackCooldownMax;
        public IReadOnlyList<BossAttackPatternData> AttackPatterns => _attackPatterns;

        internal void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_phaseName))
            {
                _phaseName = "Phase";
            }

            _attackCooldownMin = Mathf.Max(0f, _attackCooldownMin);
            _attackCooldownMax = Mathf.Max(_attackCooldownMin, _attackCooldownMax);
            _movementAmplitude = Mathf.Max(0f, _movementAmplitude);
            _movementSpeed = Mathf.Max(0f, _movementSpeed);
        }
    }
}
