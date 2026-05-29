using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class BossShooter : MonoBehaviour
    {
        private Boss _boss;
        private Transform _playerTransform;
        private Transform _firePoint;
        private GameObject _bossBulletPrefab;
        private int _bulletPoolDefaultCapacity = 32;
        private int _bulletPoolMaxSize = 128;
        private IObjectPool<Bullet> _bulletPool;
        private Coroutine _attackRoutine;
        private float _nextAttackTime;
        private int _volleyIndex;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;

        internal void Initialize(
            Boss boss,
            Transform firePoint,
            GameObject bossBulletPrefab,
            int bulletPoolDefaultCapacity,
            int bulletPoolMaxSize)
        {
            _boss = boss;
            _firePoint = firePoint;
            _bossBulletPrefab = bossBulletPrefab;
            _bulletPoolDefaultCapacity = Mathf.Max(1, bulletPoolDefaultCapacity);
            _bulletPoolMaxSize = Mathf.Max(_bulletPoolDefaultCapacity, bulletPoolMaxSize);

            if (_bulletPool != null || _bossBulletPrefab == null)
            {
                return;
            }

            if (!_bossBulletPrefab.TryGetComponent(out Bullet _))
            {
                Debug.LogError(
                    $"[{nameof(Boss)}] Boss bullet prefab '{_bossBulletPrefab.name}' is missing {nameof(Bullet)}.",
                    this);
                return;
            }

            _bulletPool = new ObjectPool<Bullet>(
                createFunc: CreateBullet,
                actionOnGet: bullet => bullet.gameObject.SetActive(true),
                actionOnRelease: bullet => bullet.gameObject.SetActive(false),
                actionOnDestroy: bullet => Destroy(bullet.gameObject),
                collectionCheck: true,
                defaultCapacity: _bulletPoolDefaultCapacity,
                maxSize: _bulletPoolMaxSize);

            PoolPrewarmer.Prewarm(_bulletPool, _bulletPoolDefaultCapacity);
        }

        internal void Spawn(CycleScalingState cycleScaling)
        {
            _cycleScaling = cycleScaling;
            _volleyIndex = 0;
            StopCurrentAttack();
        }

        internal void SetCycleScaling(CycleScalingState cycleScaling)
        {
            _cycleScaling = cycleScaling;
        }

        internal void SetPlayerTarget(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        internal void BeginAttacks(BossPhaseData phase)
        {
            ScheduleNextAttack(phase);
        }

        internal void Tick(BossPhaseData phase)
        {
            if (_attackRoutine != null || Time.time < _nextAttackTime)
            {
                return;
            }

            BossAttackPatternData pattern = ChooseAttackPattern(phase);
            if (pattern == null)
            {
                ScheduleNextAttack(phase);
                return;
            }

            _attackRoutine = StartCoroutine(RunAttackPattern(pattern, phase));
        }

        internal void StopCurrentAttack()
        {
            if (_attackRoutine == null)
            {
                return;
            }

            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        private BossAttackPatternData ChooseAttackPattern(BossPhaseData phase)
        {
            if (phase == null || phase.AttackPatterns == null || phase.AttackPatterns.Count == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, phase.AttackPatterns.Count);
            for (int i = 0; i < phase.AttackPatterns.Count; i++)
            {
                int index = (startIndex + i) % phase.AttackPatterns.Count;
                BossAttackPatternData pattern = phase.AttackPatterns[index];
                if (pattern != null)
                {
                    return pattern;
                }
            }

            return null;
        }

        private IEnumerator RunAttackPattern(BossAttackPatternData pattern, BossPhaseData phase)
        {
            if (_bulletPool == null)
            {
                Debug.LogWarning(
                    $"[{nameof(Boss)}] '{name}' cannot fire because no boss bullet prefab is assigned.",
                    this);
                ScheduleNextAttack(phase);
                _attackRoutine = null;
                yield break;
            }

            switch (pattern.PatternType)
            {
                case BossAttackPatternType.Aimed:
                    FireAimed(pattern);
                    break;

                case BossAttackPatternType.Radial:
                    FireRadial(pattern);
                    break;

                case BossAttackPatternType.Sweep:
                    yield return FireSweep(pattern);
                    break;
            }

            ScheduleNextAttack(phase);
            _attackRoutine = null;
        }

        private IEnumerator FireSweep(BossAttackPatternData pattern)
        {
            float duration = Mathf.Max(0f, pattern.PatternDuration);
            float interval = Mathf.Max(0.01f, pattern.ShotInterval);
            float elapsed = 0f;

            do
            {
                float t = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
                float angle = Mathf.Lerp(pattern.SweepStartAngleDegrees, pattern.SweepEndAngleDegrees, t);
                FireSpread(pattern, angle);
                elapsed += interval;

                if (elapsed <= duration)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
            while (elapsed <= duration && _boss != null && _boss.State == BossState.Active);
        }

        private void FireAimed(BossAttackPatternData pattern)
        {
            float centerAngle = ResolveAimedAngle(pattern);
            FireSpread(pattern, centerAngle);
        }

        private void FireRadial(BossAttackPatternData pattern)
        {
            float rotationOffset = pattern.RotateEachVolley
                ? _volleyIndex * pattern.VolleyRotationDegrees
                : 0f;

            float spread = pattern.SpreadAngle >= 360f ? 360f : pattern.SpreadAngle;
            int bulletCount = Mathf.Max(1, pattern.BulletCount);
            float step = spread >= 360f
                ? 360f / bulletCount
                : bulletCount <= 1 ? 0f : spread / (bulletCount - 1);
            float startAngle = pattern.BaseAngleDegrees + rotationOffset - (spread >= 360f ? 0f : spread * 0.5f);

            for (int i = 0; i < bulletCount; i++)
            {
                FireBulletAtAngle(startAngle + step * i, ResolveBossPressureSpeed(pattern.BulletSpeed));
            }

            _volleyIndex++;
        }

        private void FireSpread(BossAttackPatternData pattern, float centerAngle)
        {
            int bulletCount = Mathf.Max(1, pattern.BulletCount);
            float spread = pattern.SpreadAngle;
            float step = bulletCount <= 1 ? 0f : spread / (bulletCount - 1);
            float startAngle = centerAngle - spread * 0.5f;

            for (int i = 0; i < bulletCount; i++)
            {
                FireBulletAtAngle(startAngle + step * i, ResolveBossPressureSpeed(pattern.BulletSpeed));
            }
        }

        private void FireBulletAtAngle(float angleDegrees, float speed)
        {
            Bullet bullet = _bulletPool.Get();
            if (bullet == null)
            {
                return;
            }

            Vector2 firePosition = ResolveFirePosition();
            Vector2 direction = DirectionFromAngle(angleDegrees);
            bullet.Spawn(firePosition, direction, speed);
            VfxManager.Instance?.Play(VfxCue.BossMuzzleFlash, firePosition, direction);
        }

        private float ResolveAimedAngle(BossAttackPatternData pattern)
        {
            if (!pattern.AimAtPlayer || _playerTransform == null)
            {
                return pattern.BaseAngleDegrees;
            }

            Vector2 direction = (Vector2)_playerTransform.position - ResolveFirePosition();
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return pattern.BaseAngleDegrees;
            }

            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private Vector2 ResolveFirePosition()
        {
            return _firePoint == null ? transform.position : _firePoint.position;
        }

        private static Vector2 DirectionFromAngle(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }

        private void ScheduleNextAttack(BossPhaseData phase)
        {
            if (phase == null)
            {
                _nextAttackTime = float.PositiveInfinity;
                return;
            }

            _nextAttackTime = Time.time + Random.Range(phase.AttackCooldownMin, phase.AttackCooldownMax);
        }

        private Bullet CreateBullet()
        {
            GameObject go = Instantiate(_bossBulletPrefab);
            Bullet bullet = go.GetComponent<Bullet>();
            bullet.SetPool(_bulletPool);
            return bullet;
        }

        private float ResolveBossPressureSpeed(float baseSpeed)
        {
            return baseSpeed * _cycleScaling.BossPressureMultiplier;
        }
    }
}
