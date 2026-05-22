using UnityEngine;
using Warblade.Data;
using Warblade.Systems;

namespace Warblade.Entities
{
    internal sealed class EnemyDivePath
    {
        private Vector2 _target;
        private Vector2 _start;
        private Vector2 _controlPoint;
        private float _elapsed;
        private float _duration;
        private float _aimOffsetX;
        private float _targetVelocityX;

        public void Begin(Vector2 start, EnemyData data, Transform playerTransform, float speed)
        {
            _start = start;
            _aimOffsetX = Random.Range(data.DiveAimOffsetXMin, data.DiveAimOffsetXMax);
            _target = ResolveCurrentTarget(data, playerTransform, start);
            _targetVelocityX = 0f;

            float upAmount = Random.Range(data.DiveCurveUpMin, data.DiveCurveUpMax);
            float sideAmount = Random.Range(data.DiveCurveSideMin, data.DiveCurveSideMax);
            float sideSign = Random.value < 0.5f ? -1f : 1f;
            Vector2 midpoint = (_start + _target) * 0.5f;
            _controlPoint = new Vector2(
                midpoint.x + sideAmount * sideSign,
                _start.y + upAmount);

            float pathLength = BezierPath.ApproximateQuadraticLength(_start, _controlPoint, _target);
            _duration = pathLength / Mathf.Max(speed, 0.01f);
            _elapsed = 0f;
        }

        public bool Tick(Transform enemyTransform, EnemyData data, Transform playerTransform, float deltaTime)
        {
            if (_duration <= Mathf.Epsilon)
            {
                enemyTransform.position = _target;
                return true;
            }

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            if (t < data.DiveTrackingPortion && playerTransform != null)
            {
                Vector2 liveTarget = ResolveCurrentTarget(data, playerTransform, enemyTransform.position);
                _target.x = Mathf.SmoothDamp(
                    _target.x,
                    liveTarget.x,
                    ref _targetVelocityX,
                    0.12f);
            }

            enemyTransform.position = BezierPath.EvaluateQuadratic(_start, _controlPoint, _target, t);
            return t >= 1f;
        }

        private Vector2 ResolveCurrentTarget(EnemyData data, Transform playerTransform, Vector2 enemyPosition)
        {
            if (playerTransform == null)
            {
                return new Vector2(enemyPosition.x + _aimOffsetX, data.DiveBottomY);
            }

            Vector2 playerPosition = playerTransform.position;
            Vector2 direction = playerPosition - enemyPosition;
            Vector2 target;

            if (direction.y >= 0f)
            {
                target = new Vector2(playerPosition.x, data.DiveBottomY);
            }
            else
            {
                float t = (data.DiveBottomY - enemyPosition.y) / direction.y;
                target = enemyPosition + direction * t;
            }

            return new Vector2(target.x + _aimOffsetX, target.y);
        }
    }
}
