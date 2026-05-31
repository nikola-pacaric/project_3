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
        [Tooltip("Time spent easing into this phase after its health threshold is crossed. During this time attacks are interrupted.")]
        [SerializeField, Min(0f)] private float _transitionDuration = 0.75f;
        [Tooltip("Where the boss should move before this phase starts.")]
        [SerializeField] private BossPhaseTransitionTarget _transitionTarget = BossPhaseTransitionTarget.ClosestPointOnNextMovement;
        [SerializeField] private BossMovementBehavior _movementBehavior = BossMovementBehavior.HoldPosition;
        [Tooltip("Moves this phase's movement center up or down from the boss entry target without changing path size.")]
        [SerializeField] private float _movementCenterYOffset;
        [Tooltip("Horizontal movement range from the boss entry target.")]
        [SerializeField, Min(0f)] private float _movementAmplitude = 1.5f;
        [Tooltip("Vertical movement range from the boss entry target for patterns that move on Y.")]
        [SerializeField, Min(0f)] private float _verticalMovementAmplitude = 0.75f;
        [SerializeField, Min(0f)] private float _movementSpeed = 1.5f;
        [Tooltip("Pause time used by step-based movement patterns such as Dash And Pause and Lane Switch.")]
        [SerializeField, Min(0f)] private float _movementPauseDuration = 0.5f;
        [Tooltip("Number of horizontal lanes used by Lane Switch.")]
        [SerializeField, Min(2)] private int _movementLaneCount = 3;
        [Tooltip("How strongly Player Shadow follows the player's X position. 0 stays centered, 1 follows fully within amplitude limits.")]
        [SerializeField, Range(0f, 1f)] private float _playerShadowStrength = 0.65f;
        [SerializeField, Min(0f)] private float _attackCooldownMin = 1f;
        [SerializeField, Min(0f)] private float _attackCooldownMax = 2f;
        [SerializeField] private List<BossPhaseAttackData> _attacks = new List<BossPhaseAttackData>();
        [SerializeField, HideInInspector] private List<BossAttackPatternData> _attackPatterns = new List<BossAttackPatternData>();

        public string PhaseName => _phaseName;
        public float HealthThreshold => _healthThreshold;
        public float TransitionDuration => _transitionDuration;
        public BossPhaseTransitionTarget TransitionTarget => _transitionTarget;
        public BossMovementBehavior MovementBehavior => _movementBehavior;
        public float MovementCenterYOffset => _movementCenterYOffset;
        public float MovementAmplitude => _movementAmplitude;
        public float VerticalMovementAmplitude => _verticalMovementAmplitude;
        public float MovementSpeed => _movementSpeed;
        public float MovementPauseDuration => _movementPauseDuration;
        public int MovementLaneCount => _movementLaneCount;
        public float PlayerShadowStrength => _playerShadowStrength;
        public float AttackCooldownMin => _attackCooldownMin;
        public float AttackCooldownMax => _attackCooldownMax;
        public IReadOnlyList<BossPhaseAttackData> Attacks
        {
            get
            {
                MigrateLegacyAttackPatterns();
                return _attacks;
            }
        }

        internal void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_phaseName))
            {
                _phaseName = "Phase";
            }

            _transitionDuration = Mathf.Max(0f, _transitionDuration);
            _attackCooldownMin = Mathf.Max(0f, _attackCooldownMin);
            _attackCooldownMax = Mathf.Max(_attackCooldownMin, _attackCooldownMax);
            _movementAmplitude = Mathf.Max(0f, _movementAmplitude);
            _verticalMovementAmplitude = Mathf.Max(0f, _verticalMovementAmplitude);
            _movementSpeed = Mathf.Max(0f, _movementSpeed);
            _movementPauseDuration = Mathf.Max(0f, _movementPauseDuration);
            _movementLaneCount = Mathf.Max(2, _movementLaneCount);

            if (_movementBehavior == BossMovementBehavior.HoldPosition)
            {
                _movementBehavior = BossMovementBehavior.HorizontalPatrol;
            }
            else if (_movementBehavior == BossMovementBehavior.SineDrift)
            {
                _movementBehavior = BossMovementBehavior.FigureEight;
            }

            MigrateLegacyAttackPatterns();
        }

        private void MigrateLegacyAttackPatterns()
        {
            if ((_attacks != null && _attacks.Count > 0) || _attackPatterns == null || _attackPatterns.Count == 0)
            {
                return;
            }

            _attacks = new List<BossPhaseAttackData>(_attackPatterns.Count);
            for (int i = 0; i < _attackPatterns.Count; i++)
            {
                _attacks.Add(new BossPhaseAttackData(_attackPatterns[i]));
            }
        }
    }
}
