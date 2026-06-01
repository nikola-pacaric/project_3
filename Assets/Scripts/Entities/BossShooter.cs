using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class BossShooter : MonoBehaviour
    {
        private const string MuzzleFlashChildName = "MuzzleFlash";

        [Header("VFX")]
        [SerializeField, Min(0f)] private float _muzzleFlashCooldown = 0.03f;

        private Boss _boss;
        private Transform _playerTransform;
        private Transform _firePoint;
        private GameObject _bossBulletPrefab;
        private int _bulletPoolDefaultCapacity = 32;
        private int _bulletPoolMaxSize = 128;
        private ParticleSystem[] _muzzleFlashParticles;
        private readonly Dictionary<GameObject, IObjectPool<Bullet>> _bulletPools = new Dictionary<GameObject, IObjectPool<Bullet>>();
        private readonly List<Coroutine> _parallelAttackRoutines = new List<Coroutine>();
        private Coroutine _attackRoutine;
        private float _nextAttackTime;
        private float _nextMuzzleFlashTime;
        private int _volleyIndex;
        private CycleScalingState _cycleScaling = CycleScalingState.Default;

        private sealed class ParallelAttackCounter
        {
            public int Running;
        }

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
            ResolveMuzzleFlashParticles();
        }

        internal void Spawn(CycleScalingState cycleScaling)
        {
            _cycleScaling = cycleScaling;
            _volleyIndex = 0;
            _nextMuzzleFlashTime = 0f;
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

            List<BossPhaseAttackData> attacks = ChooseAttacks(phase);
            if (attacks == null || attacks.Count == 0)
            {
                ScheduleNextAttack(phase);
                return;
            }

            _attackRoutine = StartCoroutine(RunAttacks(attacks, phase));
        }

        internal void StopCurrentAttack()
        {
            for (int i = 0; i < _parallelAttackRoutines.Count; i++)
            {
                Coroutine routine = _parallelAttackRoutines[i];
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            _parallelAttackRoutines.Clear();

            if (_attackRoutine == null)
            {
                return;
            }

            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        private List<BossPhaseAttackData> ChooseAttacks(BossPhaseData phase)
        {
            if (phase == null || phase.Attacks == null || phase.Attacks.Count == 0)
            {
                return null;
            }

            int optionCount = CountAttackOptions(phase);
            if (optionCount == 0)
            {
                return null;
            }

            int selectedOption = Random.Range(0, optionCount);
            int optionIndex = 0;
            List<int> visitedGroupIds = new List<int>();
            for (int i = 0; i < phase.Attacks.Count; i++)
            {
                BossPhaseAttackData attack = phase.Attacks[i];
                if (attack == null || !attack.HasPattern)
                {
                    continue;
                }

                int groupId = attack.SimultaneousGroupId;
                if (groupId <= 0)
                {
                    if (optionIndex == selectedOption)
                    {
                        return new List<BossPhaseAttackData> { attack };
                    }

                    optionIndex++;
                    continue;
                }

                if (visitedGroupIds.Contains(groupId))
                {
                    continue;
                }

                visitedGroupIds.Add(groupId);
                if (optionIndex == selectedOption)
                {
                    return CollectAttackGroup(phase, groupId);
                }

                optionIndex++;
            }

            return null;
        }

        private int CountAttackOptions(BossPhaseData phase)
        {
            int optionCount = 0;
            List<int> visitedGroupIds = new List<int>();

            for (int i = 0; i < phase.Attacks.Count; i++)
            {
                BossPhaseAttackData attack = phase.Attacks[i];
                if (attack == null || !attack.HasPattern)
                {
                    continue;
                }

                int groupId = attack.SimultaneousGroupId;
                if (groupId <= 0)
                {
                    optionCount++;
                    continue;
                }

                if (visitedGroupIds.Contains(groupId))
                {
                    continue;
                }

                visitedGroupIds.Add(groupId);
                optionCount++;
            }

            return optionCount;
        }

        private List<BossPhaseAttackData> CollectAttackGroup(BossPhaseData phase, int groupId)
        {
            List<BossPhaseAttackData> attacks = new List<BossPhaseAttackData>();
            for (int i = 0; i < phase.Attacks.Count; i++)
            {
                BossPhaseAttackData attack = phase.Attacks[i];
                if (attack != null && attack.HasPattern && attack.SimultaneousGroupId == groupId)
                {
                    attacks.Add(attack);
                }
            }

            return attacks;
        }

        private IEnumerator RunAttacks(List<BossPhaseAttackData> attacks, BossPhaseData phase)
        {
            ParallelAttackCounter parallelAttackCounter = new ParallelAttackCounter();

            for (int i = 0; i < attacks.Count; i++)
            {
                BossPhaseAttackData attack = attacks[i];
                if (attack == null || !attack.HasPattern)
                {
                    continue;
                }

                StartAttack(attack, parallelAttackCounter);
            }

            while (parallelAttackCounter.Running > 0 && _boss != null && _boss.State == BossState.Active)
            {
                yield return null;
            }

            _parallelAttackRoutines.Clear();
            ScheduleNextAttack(phase);
            _attackRoutine = null;
        }

        private void StartAttack(BossPhaseAttackData attack, ParallelAttackCounter parallelAttackCounter)
        {
            BossAttackPatternData pattern = attack.Pattern;
            GameObject bulletPrefab = ResolveBulletPrefab(attack);
            bool spinBulletSprite = pattern.SpinBulletSprite;
            if (bulletPrefab == null)
            {
                Debug.LogWarning(
                    $"[{nameof(Boss)}] '{name}' cannot fire because no boss bullet prefab is assigned.",
                    this);
                return;
            }

            switch (pattern.PatternType)
            {
                case BossAttackPatternType.Aimed:
                    FireAimed(pattern, bulletPrefab, spinBulletSprite);
                    break;

                case BossAttackPatternType.Radial:
                    FireRadial(pattern, bulletPrefab, spinBulletSprite);
                    break;

                case BossAttackPatternType.Sweep:
                    parallelAttackCounter.Running++;
                    Coroutine routine = StartCoroutine(RunParallelAttack(
                        FireSweep(pattern, bulletPrefab, spinBulletSprite),
                        () => parallelAttackCounter.Running--));
                    _parallelAttackRoutines.Add(routine);
                    break;
            }
        }

        private IEnumerator RunParallelAttack(IEnumerator attackRoutine, System.Action onComplete)
        {
            try
            {
                while (attackRoutine.MoveNext())
                {
                    yield return attackRoutine.Current;
                }
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator FireSweep(
            BossAttackPatternData pattern,
            GameObject bulletPrefab,
            bool spinBulletSprite)
        {
            float duration = Mathf.Max(0f, pattern.PatternDuration);
            float interval = Mathf.Max(0.01f, pattern.ShotInterval);
            float elapsed = 0f;

            do
            {
                float t = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
                float angle = Mathf.Lerp(pattern.SweepStartAngleDegrees, pattern.SweepEndAngleDegrees, t);
                FireSpread(pattern, bulletPrefab, spinBulletSprite, angle);
                elapsed += interval;

                if (elapsed <= duration)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
            while (elapsed <= duration && _boss != null && _boss.State == BossState.Active);
        }

        private void FireAimed(
            BossAttackPatternData pattern,
            GameObject bulletPrefab,
            bool spinBulletSprite)
        {
            float centerAngle = ResolveAimedAngle(pattern);
            FireSpread(pattern, bulletPrefab, spinBulletSprite, centerAngle);
        }

        private void FireRadial(
            BossAttackPatternData pattern,
            GameObject bulletPrefab,
            bool spinBulletSprite)
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
            bool firedAnyBullet = false;

            for (int i = 0; i < bulletCount; i++)
            {
                firedAnyBullet |= FireBulletAtAngle(
                    bulletPrefab,
                    spinBulletSprite,
                    startAngle + step * i,
                    ResolveBossPressureSpeed(pattern.BulletSpeed));
            }

            PlayMuzzleFlashIfNeeded(firedAnyBullet);
            _volleyIndex++;
        }

        private void FireSpread(
            BossAttackPatternData pattern,
            GameObject bulletPrefab,
            bool spinBulletSprite,
            float centerAngle)
        {
            int bulletCount = Mathf.Max(1, pattern.BulletCount);
            float spread = pattern.SpreadAngle;
            float step = bulletCount <= 1 ? 0f : spread / (bulletCount - 1);
            float startAngle = centerAngle - spread * 0.5f;
            bool firedAnyBullet = false;

            for (int i = 0; i < bulletCount; i++)
            {
                firedAnyBullet |= FireBulletAtAngle(
                    bulletPrefab,
                    spinBulletSprite,
                    startAngle + step * i,
                    ResolveBossPressureSpeed(pattern.BulletSpeed));
            }

            PlayMuzzleFlashIfNeeded(firedAnyBullet);
        }

        private bool FireBulletAtAngle(
            GameObject bulletPrefab,
            bool spinBulletSprite,
            float angleDegrees,
            float speed)
        {
            IObjectPool<Bullet> bulletPool = GetOrCreateBulletPool(bulletPrefab);
            if (bulletPool == null)
            {
                return false;
            }

            Bullet bullet = bulletPool.Get();
            if (bullet == null)
            {
                return false;
            }

            Vector2 firePosition = ResolveFirePosition();
            Vector2 direction = DirectionFromAngle(angleDegrees);
            bullet.SetSpriteSpin(spinBulletSprite);
            bullet.Spawn(firePosition, direction, speed);
            return true;
        }

        private float ResolveAimedAngle(BossAttackPatternData pattern)
        {
            Transform playerTransform = ResolvePlayerTarget();
            if (!pattern.AimAtPlayer || playerTransform == null)
            {
                return pattern.BaseAngleDegrees;
            }

            Vector2 direction = (Vector2)playerTransform.position - ResolveFirePosition();
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return pattern.BaseAngleDegrees;
            }

            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private Transform ResolvePlayerTarget()
        {
            if (_playerTransform != null)
            {
                return _playerTransform;
            }

            _playerTransform = LevelManager.Instance == null ? null : LevelManager.Instance.PlayerTransform;
            return _playerTransform;
        }

        private Vector2 ResolveFirePosition()
        {
            return _firePoint == null ? transform.position : _firePoint.position;
        }

        private void PlayMuzzleFlashIfNeeded(bool firedAnyBullet)
        {
            if (!firedAnyBullet || Time.time < _nextMuzzleFlashTime)
            {
                return;
            }

            ResolveMuzzleFlashParticles();
            if (_muzzleFlashParticles == null || _muzzleFlashParticles.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _muzzleFlashParticles.Length; i++)
            {
                ParticleSystem muzzleFlashParticle = _muzzleFlashParticles[i];
                if (muzzleFlashParticle == null)
                {
                    continue;
                }

                muzzleFlashParticle.Clear(true);
                muzzleFlashParticle.Play(true);
            }

            _nextMuzzleFlashTime = Time.time + _muzzleFlashCooldown;
        }

        private void ResolveMuzzleFlashParticles()
        {
            Transform muzzleFlashRoot = _firePoint == null ? null : _firePoint.Find(MuzzleFlashChildName);
            _muzzleFlashParticles = muzzleFlashRoot == null
                ? null
                : muzzleFlashRoot.GetComponentsInChildren<ParticleSystem>(true);
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

        private GameObject ResolveBulletPrefab(BossPhaseAttackData attack)
        {
            return attack.BulletPrefab != null ? attack.BulletPrefab : _bossBulletPrefab;
        }

        private IObjectPool<Bullet> GetOrCreateBulletPool(GameObject bulletPrefab)
        {
            if (bulletPrefab == null)
            {
                return null;
            }

            if (_bulletPools.TryGetValue(bulletPrefab, out IObjectPool<Bullet> bulletPool))
            {
                return bulletPool;
            }

            if (!bulletPrefab.TryGetComponent(out Bullet _))
            {
                Debug.LogError(
                    $"[{nameof(Boss)}] Boss bullet prefab '{bulletPrefab.name}' is missing {nameof(Bullet)}.",
                    this);
                return null;
            }

            ObjectPool<Bullet> newPool = null;
            newPool = new ObjectPool<Bullet>(
                createFunc: () => CreateBullet(bulletPrefab, newPool),
                actionOnGet: bullet => bullet.gameObject.SetActive(true),
                actionOnRelease: bullet => bullet.gameObject.SetActive(false),
                actionOnDestroy: bullet => Destroy(bullet.gameObject),
                collectionCheck: true,
                defaultCapacity: _bulletPoolDefaultCapacity,
                maxSize: _bulletPoolMaxSize);

            _bulletPools.Add(bulletPrefab, newPool);
            PoolPrewarmer.Prewarm(newPool, _bulletPoolDefaultCapacity);
            return newPool;
        }

        private Bullet CreateBullet(GameObject bulletPrefab, IObjectPool<Bullet> bulletPool)
        {
            GameObject go = Instantiate(bulletPrefab);
            Bullet bullet = go.GetComponent<Bullet>();
            bullet.SetPool(bulletPool);
            return bullet;
        }

        private float ResolveBossPressureSpeed(float baseSpeed)
        {
            return baseSpeed * _cycleScaling.BossPressureMultiplier;
        }
    }
}
