using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Boss Attack Pattern", fileName = "BossAttackPatternData")]
    public class BossAttackPatternData : ScriptableObject
    {
        [Header("Pattern")]
        [SerializeField] private BossAttackPatternType _patternType = BossAttackPatternType.Aimed;
        [SerializeField, Min(1)] private int _bulletCount = 1;
        [SerializeField, Range(0f, 360f)] private float _spreadAngle = 0f;
        [SerializeField, Min(0f)] private float _patternDuration = 0f;
        [SerializeField, Min(0.01f)] private float _shotInterval = 0.25f;

        [Header("Bullet Direction")]
        [SerializeField] private float _baseAngleDegrees = 270f;
        [SerializeField] private bool _aimAtPlayer = true;
        [SerializeField] private bool _rotateEachVolley;
        [SerializeField] private float _volleyRotationDegrees = 12f;

        [Header("Sweep")]
        [SerializeField] private float _sweepStartAngleDegrees = 210f;
        [SerializeField] private float _sweepEndAngleDegrees = 330f;

        [Header("Projectile")]
        [SerializeField, Min(0f)] private float _bulletSpeed = 5f;
        [SerializeField] private bool _spinBulletSprite;

        public BossAttackPatternType PatternType => _patternType;
        public int BulletCount => _bulletCount;
        public float SpreadAngle => _spreadAngle;
        public float PatternDuration => _patternDuration;
        public float ShotInterval => _shotInterval;
        public float BaseAngleDegrees => _baseAngleDegrees;
        public bool AimAtPlayer => _aimAtPlayer;
        public bool RotateEachVolley => _rotateEachVolley;
        public float VolleyRotationDegrees => _volleyRotationDegrees;
        public float SweepStartAngleDegrees => _sweepStartAngleDegrees;
        public float SweepEndAngleDegrees => _sweepEndAngleDegrees;
        public float BulletSpeed => _bulletSpeed;
        public bool SpinBulletSprite => _spinBulletSprite;

        private void OnValidate()
        {
            _bulletCount = Mathf.Max(1, _bulletCount);
            _shotInterval = Mathf.Max(0.01f, _shotInterval);
            _bulletSpeed = Mathf.Max(0f, _bulletSpeed);
        }
    }
}
