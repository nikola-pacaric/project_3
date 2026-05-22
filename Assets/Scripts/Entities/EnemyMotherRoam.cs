using UnityEngine;
using Warblade.Data;

namespace Warblade.Entities
{
    internal sealed class EnemyMotherRoam
    {
        private Vector2 _velocity;
        private Vector2 _target;
        private float _nextRetargetTime;

        public void Start(Transform enemyTransform, EnemyData data)
        {
            ClampPositionToBounds(enemyTransform, data);
            ScheduleRetarget(enemyTransform, data, immediate: true);
        }

        public void Tick(Transform enemyTransform, EnemyData data, float speed)
        {
            if (Time.time >= _nextRetargetTime || _velocity.sqrMagnitude <= 0.0001f)
            {
                ScheduleRetarget(enemyTransform, data, immediate: false);
            }

            Vector2 nextPosition = (Vector2)enemyTransform.position + _velocity * speed * Time.deltaTime;
            Vector2 boundsMin = data.MotherRoamBoundsMin;
            Vector2 boundsMax = data.MotherRoamBoundsMax;

            if (nextPosition.x < boundsMin.x || nextPosition.x > boundsMax.x)
            {
                nextPosition.x = Mathf.Clamp(nextPosition.x, boundsMin.x, boundsMax.x);
                _velocity.x *= -1f;
            }

            if (nextPosition.y < boundsMin.y || nextPosition.y > boundsMax.y)
            {
                nextPosition.y = Mathf.Clamp(nextPosition.y, boundsMin.y, boundsMax.y);
                _velocity.y *= -1f;
            }

            enemyTransform.position = nextPosition;
        }

        public void DrawRuntimeGizmos(Transform enemyTransform)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_target, 0.18f);
            Gizmos.DrawLine(enemyTransform.position, _target);

            if (_velocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector2 position = enemyTransform.position;
            Vector2 directionEnd = position + _velocity.normalized * 0.85f;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(position, directionEnd);
            Gizmos.DrawWireSphere(directionEnd, 0.08f);
        }

        private static void ClampPositionToBounds(Transform enemyTransform, EnemyData data)
        {
            Vector2 boundsMin = data.MotherRoamBoundsMin;
            Vector2 boundsMax = data.MotherRoamBoundsMax;
            enemyTransform.position = new Vector2(
                Mathf.Clamp(enemyTransform.position.x, boundsMin.x, boundsMax.x),
                Mathf.Clamp(enemyTransform.position.y, boundsMin.y, boundsMax.y));
        }

        private void ScheduleRetarget(Transform enemyTransform, EnemyData data, bool immediate)
        {
            Vector2 currentPosition = enemyTransform.position;
            Vector2 targetPosition = new Vector2(
                Random.Range(data.MotherRoamBoundsMin.x, data.MotherRoamBoundsMax.x),
                Random.Range(data.MotherRoamBoundsMin.y, data.MotherRoamBoundsMax.y));
            Vector2 direction = targetPosition - currentPosition;
            _target = targetPosition;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            _velocity = direction.normalized;
            float retargetDelay = immediate
                ? 0f
                : Random.Range(data.MotherRoamRetargetIntervalMin, data.MotherRoamRetargetIntervalMax);
            _nextRetargetTime = Time.time + retargetDelay;
        }
    }
}
