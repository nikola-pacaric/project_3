using System;
using System.Collections;
using UnityEngine;
using Warblade.Data;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class BossMovement : MonoBehaviour
    {
        private Boss _boss;
        private BossData _data;
        private Transform _playerTransform;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;
        private Coroutine _phaseTransitionRoutine;
        private Vector2 _arenaCenterPosition;
        private float _phaseMovementTime;
        private int _patrolDirection = 1;
        private Vector2 _movementTargetPosition;
        private float _movementPauseTimer;
        private int _movementWaypointIndex;
        private bool _hasMovementTarget;

        internal void Initialize(Boss boss)
        {
            _boss = boss;
        }

        internal void Spawn(BossData data, CycleScalingState cycleScaling)
        {
            _data = data;
            _cycleScaling = cycleScaling;
            StopPhaseTransition();

            transform.position = _data.EntryStartPosition;
            _arenaCenterPosition = _data.EntryTargetPosition;
            _phaseMovementTime = 0f;
            _patrolDirection = 1;
            ResetPhaseMovementState();
        }

        internal void SetCycleScaling(CycleScalingState cycleScaling)
        {
            _cycleScaling = cycleScaling;
        }

        internal void SetPlayerTarget(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        internal bool TickEntry()
        {
            if (_data == null)
            {
                return false;
            }

            Vector2 targetPosition = _data.EntryTargetPosition;
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                ResolveBossPressureSpeed(_data.EntrySpeed) * Time.deltaTime);

            if ((Vector2)transform.position != targetPosition)
            {
                return false;
            }

            _arenaCenterPosition = targetPosition;
            return true;
        }

        internal void TickActive(BossPhaseData phase)
        {
            if (phase == null)
            {
                return;
            }

            _phaseMovementTime += Time.deltaTime;

            switch (ResolveMovementBehavior(phase.MovementBehavior))
            {
                case BossMovementBehavior.HorizontalPatrol:
                    UpdateHorizontalPatrol(phase);
                    break;

                case BossMovementBehavior.FigureEight:
                    UpdateFigureEight(phase);
                    break;

                case BossMovementBehavior.DashAndPause:
                    UpdateDashAndPause(phase);
                    break;

                case BossMovementBehavior.DiveSweep:
                    UpdateDiveSweep(phase);
                    break;

                case BossMovementBehavior.LaneSwitch:
                    UpdateLaneSwitch(phase);
                    break;

                case BossMovementBehavior.PlayerShadow:
                    UpdatePlayerShadow(phase);
                    break;

                case BossMovementBehavior.BoxPatrol:
                    UpdateBoxPatrol(phase);
                    break;
            }
        }

        internal void PreparePhase()
        {
            _phaseMovementTime = 0f;
            _patrolDirection = transform.position.x >= _arenaCenterPosition.x ? -1 : 1;
            ResetPhaseMovementState();
        }

        internal void BeginPhaseTransition(BossPhaseData phase, Action onComplete)
        {
            StopPhaseTransition();
            _phaseTransitionRoutine = StartCoroutine(RunPhaseTransition(phase, onComplete));
        }

        internal void StopPhaseTransition()
        {
            if (_phaseTransitionRoutine == null)
            {
                return;
            }

            StopCoroutine(_phaseTransitionRoutine);
            _phaseTransitionRoutine = null;
        }

        public static BossMovementBehavior ResolveMovementBehavior(BossMovementBehavior movementBehavior)
        {
            if (movementBehavior == BossMovementBehavior.HoldPosition)
            {
                return BossMovementBehavior.HorizontalPatrol;
            }

            if (movementBehavior == BossMovementBehavior.SineDrift)
            {
                return BossMovementBehavior.FigureEight;
            }

            return movementBehavior;
        }

        private IEnumerator RunPhaseTransition(BossPhaseData phase, Action onComplete)
        {
            if (phase == null)
            {
                _phaseTransitionRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            float duration = phase.TransitionDuration;
            Vector2 startPosition = transform.position;
            ResetPhaseMovementState();
            Vector2 targetPosition = ResolvePhaseTransitionTarget(phase, startPosition);
            float elapsed = 0f;

            while (_boss != null && _boss.State == BossState.Transitioning && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector2.Lerp(startPosition, targetPosition, easedT);
                yield return null;
            }

            if (_boss == null || _boss.State != BossState.Transitioning)
            {
                _phaseTransitionRoutine = null;
                yield break;
            }

            transform.position = targetPosition;
            _movementTargetPosition = targetPosition;
            _hasMovementTarget =
                ResolveMovementBehavior(phase.MovementBehavior) == BossMovementBehavior.LaneSwitch ||
                ResolveMovementBehavior(phase.MovementBehavior) == BossMovementBehavior.BoxPatrol ||
                ResolveMovementBehavior(phase.MovementBehavior) == BossMovementBehavior.DashAndPause;
            _phaseTransitionRoutine = null;
            onComplete?.Invoke();
        }

        private void UpdateHorizontalPatrol(BossPhaseData phase)
        {
            float amplitude = phase.MovementAmplitude;
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (amplitude <= Mathf.Epsilon || movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 position = transform.position;
            position.x += _patrolDirection * movementSpeed * Time.deltaTime;

            float minX = _arenaCenterPosition.x - amplitude;
            float maxX = _arenaCenterPosition.x + amplitude;
            if (position.x <= minX || position.x >= maxX)
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
                _patrolDirection *= -1;
            }

            position.y = Mathf.MoveTowards(position.y, _arenaCenterPosition.y, movementSpeed * Time.deltaTime);
            transform.position = position;
        }

        private void UpdateFigureEight(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            float xOffset = Mathf.Sin(_phaseMovementTime * movementSpeed) * phase.MovementAmplitude;
            float yOffset = Mathf.Sin(_phaseMovementTime * movementSpeed * 2f) * phase.VerticalMovementAmplitude;
            transform.position = new Vector2(
                _arenaCenterPosition.x + xOffset,
                _arenaCenterPosition.y + yOffset);
        }

        private void UpdateDashAndPause(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed) * 2.5f;
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            if (_movementPauseTimer > 0f)
            {
                _movementPauseTimer -= Time.deltaTime;
                return;
            }

            if (!_hasMovementTarget)
            {
                _movementTargetPosition = ResolveDashTarget(phase);
                _hasMovementTarget = true;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _movementTargetPosition,
                movementSpeed * Time.deltaTime);

            if (Vector2.SqrMagnitude((Vector2)transform.position - _movementTargetPosition) <= 0.0001f)
            {
                _movementWaypointIndex++;
                _movementPauseTimer = phase.MovementPauseDuration;
                _hasMovementTarget = false;
            }
        }

        private Vector2 ResolveDashTarget(BossPhaseData phase)
        {
            float x = phase.MovementAmplitude;
            float y = phase.VerticalMovementAmplitude;
            switch (_movementWaypointIndex % 4)
            {
                case 0:
                    return _arenaCenterPosition + new Vector2(-x, 0f);

                case 1:
                    return _arenaCenterPosition + new Vector2(x, -y);

                case 2:
                    return _arenaCenterPosition + new Vector2(x, y);

                default:
                    return _arenaCenterPosition + new Vector2(-x, -y);
            }
        }

        private void UpdateDiveSweep(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            float progress = Mathf.PingPong(_phaseMovementTime * movementSpeed * 0.25f, 1f);
            float x = Mathf.Lerp(
                _arenaCenterPosition.x - phase.MovementAmplitude,
                _arenaCenterPosition.x + phase.MovementAmplitude,
                progress);
            float y = _arenaCenterPosition.y - Mathf.Sin(progress * Mathf.PI) * phase.VerticalMovementAmplitude;
            transform.position = new Vector2(x, y);
        }

        private void UpdateLaneSwitch(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed) * 2f;
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            if (_movementPauseTimer > 0f)
            {
                _movementPauseTimer -= Time.deltaTime;
                return;
            }

            if (!_hasMovementTarget)
            {
                _movementTargetPosition = ResolveNextLaneTarget(phase);
                _hasMovementTarget = true;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _movementTargetPosition,
                movementSpeed * Time.deltaTime);

            if (Vector2.SqrMagnitude((Vector2)transform.position - _movementTargetPosition) <= 0.0001f)
            {
                _movementPauseTimer = phase.MovementPauseDuration;
                _hasMovementTarget = false;
            }
        }

        private Vector2 ResolveNextLaneTarget(BossPhaseData phase)
        {
            int laneCount = Mathf.Max(2, phase.MovementLaneCount);
            _movementWaypointIndex += _patrolDirection;

            if (_movementWaypointIndex >= laneCount)
            {
                _movementWaypointIndex = laneCount - 2;
                _patrolDirection = -1;
            }
            else if (_movementWaypointIndex < 0)
            {
                _movementWaypointIndex = 1;
                _patrolDirection = 1;
            }

            float laneT = laneCount <= 1 ? 0.5f : _movementWaypointIndex / (float)(laneCount - 1);
            float x = Mathf.Lerp(
                _arenaCenterPosition.x - phase.MovementAmplitude,
                _arenaCenterPosition.x + phase.MovementAmplitude,
                laneT);

            return new Vector2(x, _arenaCenterPosition.y);
        }

        private void UpdatePlayerShadow(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            float targetX = _arenaCenterPosition.x;
            if (_playerTransform != null)
            {
                targetX = Mathf.Lerp(_arenaCenterPosition.x, _playerTransform.position.x, phase.PlayerShadowStrength);
            }

            targetX = Mathf.Clamp(
                targetX,
                _arenaCenterPosition.x - phase.MovementAmplitude,
                _arenaCenterPosition.x + phase.MovementAmplitude);

            Vector2 position = transform.position;
            position.x = Mathf.MoveTowards(position.x, targetX, movementSpeed * Time.deltaTime);
            position.y = Mathf.MoveTowards(position.y, _arenaCenterPosition.y, movementSpeed * Time.deltaTime);
            transform.position = position;
        }

        private void UpdateBoxPatrol(BossPhaseData phase)
        {
            float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
            if (movementSpeed <= Mathf.Epsilon)
            {
                return;
            }

            if (!_hasMovementTarget)
            {
                _movementTargetPosition = ResolveBoxPatrolTarget(phase);
                _hasMovementTarget = true;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _movementTargetPosition,
                movementSpeed * Time.deltaTime);

            if (Vector2.SqrMagnitude((Vector2)transform.position - _movementTargetPosition) <= 0.0001f)
            {
                _movementWaypointIndex++;
                _hasMovementTarget = false;
            }
        }

        private Vector2 ResolveBoxPatrolTarget(BossPhaseData phase)
        {
            float x = phase.MovementAmplitude;
            float y = phase.VerticalMovementAmplitude;
            switch (_movementWaypointIndex % 4)
            {
                case 0:
                    return _arenaCenterPosition + new Vector2(-x, y);

                case 1:
                    return _arenaCenterPosition + new Vector2(x, y);

                case 2:
                    return _arenaCenterPosition + new Vector2(x, -y);

                default:
                    return _arenaCenterPosition + new Vector2(-x, -y);
            }
        }

        private Vector2 ResolvePhaseTransitionTarget(BossPhaseData phase, Vector2 currentPosition)
        {
            if (phase == null || phase.TransitionTarget == BossPhaseTransitionTarget.ArenaCenter)
            {
                return _arenaCenterPosition;
            }

            switch (ResolveMovementBehavior(phase.MovementBehavior))
            {
                case BossMovementBehavior.HorizontalPatrol:
                case BossMovementBehavior.PlayerShadow:
                    return new Vector2(
                        Mathf.Clamp(
                            currentPosition.x,
                            _arenaCenterPosition.x - phase.MovementAmplitude,
                            _arenaCenterPosition.x + phase.MovementAmplitude),
                        _arenaCenterPosition.y);

                case BossMovementBehavior.LaneSwitch:
                    return ResolveClosestLanePoint(phase, currentPosition);

                case BossMovementBehavior.BoxPatrol:
                    return ResolveClosestPoint(
                        currentPosition,
                        out _movementWaypointIndex,
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, -phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, -phase.VerticalMovementAmplitude));

                case BossMovementBehavior.DashAndPause:
                    return ResolveClosestPoint(
                        currentPosition,
                        out _movementWaypointIndex,
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, 0f),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, -phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                        _arenaCenterPosition + new Vector2(-phase.MovementAmplitude, -phase.VerticalMovementAmplitude));

                case BossMovementBehavior.DiveSweep:
                    return ResolveClosestDiveSweepPoint(phase, currentPosition);

                case BossMovementBehavior.FigureEight:
                    return ResolveClosestFigureEightPoint(phase, currentPosition);

                default:
                    return _arenaCenterPosition;
            }
        }

        private Vector2 ResolveClosestLanePoint(BossPhaseData phase, Vector2 currentPosition)
        {
            int laneCount = Mathf.Max(2, phase.MovementLaneCount);
            Vector2 closest = _arenaCenterPosition;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < laneCount; i++)
            {
                float laneT = i / (float)(laneCount - 1);
                Vector2 lanePosition = new Vector2(
                    Mathf.Lerp(
                        _arenaCenterPosition.x - phase.MovementAmplitude,
                        _arenaCenterPosition.x + phase.MovementAmplitude,
                        laneT),
                    _arenaCenterPosition.y);
                float distance = Vector2.SqrMagnitude(currentPosition - lanePosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = lanePosition;
                    _movementWaypointIndex = i;
                }
            }

            return closest;
        }

        private Vector2 ResolveClosestDiveSweepPoint(BossPhaseData phase, Vector2 currentPosition)
        {
            const int sampleCount = 48;
            Vector2 closest = _arenaCenterPosition;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i <= sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                Vector2 point = _arenaCenterPosition + new Vector2(
                    Mathf.Lerp(-phase.MovementAmplitude, phase.MovementAmplitude, progress),
                    -Mathf.Sin(progress * Mathf.PI) * phase.VerticalMovementAmplitude);
                float distance = Vector2.SqrMagnitude(currentPosition - point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = point;
                    float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
                    _phaseMovementTime = movementSpeed <= Mathf.Epsilon
                        ? 0f
                        : progress / (movementSpeed * 0.25f);
                }
            }

            return closest;
        }

        private Vector2 ResolveClosestFigureEightPoint(BossPhaseData phase, Vector2 currentPosition)
        {
            const int sampleCount = 72;
            Vector2 closest = _arenaCenterPosition;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount * Mathf.PI * 2f;
                Vector2 point = _arenaCenterPosition + new Vector2(
                    Mathf.Sin(t) * phase.MovementAmplitude,
                    Mathf.Sin(t * 2f) * phase.VerticalMovementAmplitude);
                float distance = Vector2.SqrMagnitude(currentPosition - point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = point;
                    float movementSpeed = ResolveBossPressureSpeed(phase.MovementSpeed);
                    _phaseMovementTime = movementSpeed <= Mathf.Epsilon
                        ? 0f
                        : t / movementSpeed;
                }
            }

            return closest;
        }

        private static Vector2 ResolveClosestPoint(Vector2 currentPosition, out int closestIndex, params Vector2[] points)
        {
            closestIndex = 0;
            Vector2 closest = points == null || points.Length == 0 ? currentPosition : points[0];
            float closestDistance = float.PositiveInfinity;

            if (points == null)
            {
                return closest;
            }

            for (int i = 0; i < points.Length; i++)
            {
                float distance = Vector2.SqrMagnitude(currentPosition - points[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = points[i];
                    closestIndex = i;
                }
            }

            return closest;
        }

        private void ResetPhaseMovementState()
        {
            _movementTargetPosition = _arenaCenterPosition;
            _movementPauseTimer = 0f;
            _movementWaypointIndex = 0;
            _hasMovementTarget = false;
        }

        private float ResolveBossPressureSpeed(float baseSpeed)
        {
            return baseSpeed * _cycleScaling.BossPressureMultiplier;
        }
    }
}
